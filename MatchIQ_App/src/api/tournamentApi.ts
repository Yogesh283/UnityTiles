import { apiClient } from './client';

export type RoomPlayer = {
  user_id: number;
  user_uuid: string | null;
  username: string | null;
  display_name: string;
  avatar_url: string | null;
  current_rank: number | null;
  game_level: number | null;
  rank_tier: string | null;
  score: number;
  moves: number;
  elapsed_seconds: number;
  rank: number | null;
  is_connected: boolean;
  has_submitted: boolean;
};

export type RoomSearchStatus =
  | 'searching'
  | 'player_joined'
  | 'players_connected'
  | 'match_found'
  | 'starting';

export type Room = {
  room_id: string | null;
  tournament_id: string;
  tournament_name: string | null;
  level_index: number;
  level_seed: number;
  status: 'waiting' | 'starting' | 'active' | 'locked' | 'finished';
  player_count: number;
  max_players: number;
  waiting_seconds: number;
  waiting_seconds_remaining: number | null;
  start_countdown_seconds: number | null;
  match_start_at_ms: number | null;
  server_now_ms: number | null;
  search_status: RoomSearchStatus | null;
  wallet_balance: number | null;
  players: RoomPlayer[];
};

export type SubmitScoreResult = {
  ok: boolean;
  finalized: boolean;
  rank: number | null;
  prize: number;
  room_status: string;
  wallet_balance: number | null;
};

/**
 * Joining is what charges the entry fee — the server debits the shared wallet inside
 * matchmaking, so no client may deduct coins on its own.
 */
async function join(tournamentId: string): Promise<Room> {
  const { data } = await apiClient.post<Room>('/tournaments/join', {
    tournament_id: tournamentId,
  });
  return data;
}

async function room(roomId: string): Promise<Room> {
  const { data } = await apiClient.get<Room>(`/tournaments/rooms/${roomId}`);
  return data;
}

async function submitScore(params: {
  roomId: string;
  score: number;
  moves: number;
  elapsedSeconds: number;
}): Promise<SubmitScoreResult> {
  const { data } = await apiClient.post<SubmitScoreResult>('/tournaments/submit-score', {
    room_id: params.roomId,
    score: params.score,
    moves: params.moves,
    elapsed_seconds: params.elapsedSeconds,
  });
  return data;
}

async function balance(): Promise<number> {
  const { data } = await apiClient.get<{ balance: number }>('/wallet/balance');
  return data.balance;
}

export const tournamentApi = { join, room, submitScore, balance };
