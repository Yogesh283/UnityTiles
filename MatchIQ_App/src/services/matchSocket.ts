import { WS_BASE_URL } from '../constants';
import type { Room } from '../api/tournamentApi';

export type MatchSocketEvent =
  | { event: 'room_updated'; room_id: string; room: Room }
  | { event: 'player_joined'; room_id: string; room: Room }
  | { event: 'player_left'; room_id: string; room: Room }
  | { event: 'countdown'; room_id: string; room: Room }
  | { event: 'match_start'; room_id: string; room: Room }
  | { event: 'match_finished'; room_id: string; results: unknown[] }
  | { event: 'pong'; room_id?: string };

type Handlers = {
  onEvent: (event: MatchSocketEvent) => void;
  onOpen?: () => void;
  onClose?: () => void;
};

export type MatchSocket = { close: () => void };

/**
 * Live room feed. The server pushes every join, countdown tick and start signal, which is what
 * keeps two devices on the same clock instead of each guessing when the match begins.
 *
 * A dropped socket is not fatal — the matchmaking screen also polls — so we reconnect quietly
 * with a small backoff and never surface transport errors to the player.
 */
export function openMatchSocket(
  roomId: string,
  token: string,
  handlers: Handlers,
): MatchSocket {
  let socket: WebSocket | null = null;
  let closed = false;
  let attempt = 0;
  let retryTimer: ReturnType<typeof setTimeout> | null = null;
  let pingTimer: ReturnType<typeof setInterval> | null = null;

  const clearTimers = () => {
    if (retryTimer) clearTimeout(retryTimer);
    if (pingTimer) clearInterval(pingTimer);
    retryTimer = null;
    pingTimer = null;
  };

  const connect = () => {
    if (closed) return;

    const url = `${WS_BASE_URL}/ws/tournament/${encodeURIComponent(roomId)}?token=${encodeURIComponent(token)}`;
    socket = new WebSocket(url);

    socket.onopen = () => {
      attempt = 0;
      handlers.onOpen?.();
      pingTimer = setInterval(() => {
        try {
          socket?.send(JSON.stringify({ event: 'ping' }));
        } catch {
          // socket is closing; the reconnect path will pick it up
        }
      }, 20000);
    };

    socket.onmessage = (message) => {
      try {
        const payload = JSON.parse(String(message.data));
        if (payload?.event) handlers.onEvent(payload as MatchSocketEvent);
      } catch {
        // ignore malformed frames
      }
    };

    socket.onerror = () => {
      // handled by onclose
    };

    socket.onclose = () => {
      clearTimers();
      handlers.onClose?.();
      if (closed) return;
      attempt += 1;
      retryTimer = setTimeout(connect, Math.min(8000, 500 * 2 ** attempt));
    };
  };

  connect();

  return {
    close: () => {
      closed = true;
      clearTimers();
      try {
        socket?.close();
      } catch {
        // already gone
      }
    },
  };
}
