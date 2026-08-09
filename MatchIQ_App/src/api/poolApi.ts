import { apiClient } from './client';

export type PoolStatus = 'open' | 'running' | 'finished' | 'cancelled';
export type PoolEntryStatus = 'joined' | 'played' | 'won' | 'lost' | 'refunded';

export type PoolEntry = {
  pool_id: number;
  user_id: number;
  score: number;
  rank: number | null;
  prize: number;
  status: PoolEntryStatus;
  submitted_at: string | null;
};

export type Pool = {
  id: number;
  name: string;
  icon: string;
  entry_fee: number;
  max_players: number;
  players_joined: number;
  winners_count: number;
  prize_share: number;
  prize_pool: number;
  live_prize_pool: number;
  distribution: number[];
  prize_breakdown: number[];
  status: PoolStatus;
  level_index: number;
  level_seed: number;
  created_at: string | null;
  finished_at: string | null;
  my_entry?: PoolEntry | null;
};

export type PoolRules = {
  prize_share: number;
  platform_fee_share: number;
  entry_fees: number[];
  player_sizes: number[];
  sizes: {
    players: number;
    winners: number;
    distribution: number[];
    prizes: Record<string, number>;
  }[];
  notes: string[];
};

export type PoolHistoryItem = {
  pool_id: number;
  name: string;
  icon: string;
  entry_fee: number;
  status: PoolStatus;
  my_status: PoolEntryStatus;
  score: number;
  rank: number | null;
  prize: number;
  joined_at: string | null;
};

/** IQFX Pro Tournament — Pool Play */
export const poolApi = {
  async rules(): Promise<PoolRules> {
    const { data } = await apiClient.get<PoolRules>('/pools/rules');
    return data;
  },

  async list(): Promise<{ wallet_balance: number; pools: Pool[] }> {
    const { data } = await apiClient.get('/pools');
    return data;
  },

  async get(poolId: number): Promise<Pool> {
    const { data } = await apiClient.get<Pool>(`/pools/${poolId}`);
    return data;
  },

  async join(poolId: number): Promise<{ wallet_balance: number; pool: Pool }> {
    const { data } = await apiClient.post('/pools/join', { pool_id: poolId });
    return data;
  },

  async submitScore(
    poolId: number,
    score: number,
    moves = 0,
    elapsedSeconds = 0,
  ): Promise<{ wallet_balance: number; pool: Pool }> {
    const { data } = await apiClient.post('/pools/submit-score', {
      pool_id: poolId,
      score,
      moves,
      elapsed_seconds: elapsedSeconds,
    });
    return data;
  },

  async history(limit = 50): Promise<PoolHistoryItem[]> {
    const { data } = await apiClient.get<{ history: PoolHistoryItem[] }>('/pools/history', {
      params: { limit },
    });
    return data.history;
  },

  async create(payload: {
    entry_fee: number;
    max_players: number;
    name?: string;
    icon?: string;
    winners_count?: number;
    distribution?: number[];
  }): Promise<Pool> {
    const { data } = await apiClient.post<Pool>('/pools', payload);
    return data;
  },
};
