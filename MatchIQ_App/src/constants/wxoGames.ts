/**
 * Shared WXO multi-game rules (mirror of web assets/wxo-games.js).
 * Winner reward = 2 × entry. One wallet / one user for all games.
 */
export type WxoRoom = {
  id: string;
  label: string;
  players: number;
  entry: number;
  live: boolean;
  free: boolean;
};

export type WxoGame = {
  id: string;
  name: string;
  engine: 'unity' | 'soon' | string;
  rooms: WxoRoom[];
};

export const WXO_WIN_MULTIPLIER = 2;

export function wxoWinPrize(entry: number): number {
  return Math.max(0, Math.floor(Number(entry) || 0) * WXO_WIN_MULTIPLIER);
}

export const WXO_GAMES: Record<string, WxoGame> = {
  'iq-match': {
    id: 'iq-match',
    name: 'IQ Match',
    engine: 'unity',
    rooms: [
      { id: 'duel', label: '2 Players', players: 2, entry: 25, live: false, free: false },
      { id: 'squad5', label: '5 Players', players: 5, entry: 50, live: false, free: false },
      { id: 'room10', label: '10 Players', players: 10, entry: 100, live: false, free: false },
      { id: 'room20', label: '20 Players', players: 20, entry: 200, live: false, free: false },
      { id: 'live5', label: 'Live Stream • 5', players: 5, entry: 75, live: true, free: false },
      { id: 'live10', label: 'Live Stream • 10', players: 10, entry: 150, live: true, free: false },
      { id: 'free', label: 'Free Practice', players: 2, entry: 0, live: false, free: true },
    ],
  },
  ludo: {
    id: 'ludo',
    name: 'Ludo',
    engine: 'soon',
    rooms: [
      { id: 'duel', label: '2 Players', players: 2, entry: 25, live: false, free: false },
      { id: 'squad4', label: '4 Players', players: 4, entry: 50, live: false, free: false },
      { id: 'free', label: 'Free Practice', players: 2, entry: 0, live: false, free: true },
    ],
  },
  racing: {
    id: 'racing',
    name: 'Car Racing',
    engine: 'soon',
    rooms: [
      { id: 'duel', label: '2 Players', players: 2, entry: 30, live: false, free: false },
      { id: 'room8', label: '8 Players', players: 8, entry: 80, live: false, free: false },
      { id: 'live', label: 'Live Stream', players: 8, entry: 100, live: true, free: false },
      { id: 'free', label: 'Free Practice', players: 2, entry: 0, live: false, free: true },
    ],
  },
};
