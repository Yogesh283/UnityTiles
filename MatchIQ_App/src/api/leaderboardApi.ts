import { apiClient } from './client';

export type LeaderboardRow = {
  user_id: number;
  display_name: string;
  total_wins: number;
  total_prize: number;
  tournaments_played: number;
  best_rank: number | null;
};

async function list(limit = 50): Promise<LeaderboardRow[]> {
  const { data } = await apiClient.get<LeaderboardRow[]>('/leaderboard', {
    params: { limit },
  });
  return Array.isArray(data) ? data : [];
}

export const leaderboardApi = { list };
