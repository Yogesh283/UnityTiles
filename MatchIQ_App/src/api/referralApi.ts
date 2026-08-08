import { apiClient } from './client';

export type IncomePeriod = 'today' | 'week' | 'month' | 'total';

export type LevelIncome = {
  level: number;
  percent: number;
  points: number;
  entries: number;
};

export type IncomeSummary = {
  today: number;
  week: number;
  month: number;
  total: number;
  by_level: LevelIncome[];
};

export type ReferralMe = {
  referral_code: string;
  referral_link: string;
  team_counts: { level: number; members: number }[];
  team_size: number;
  income: IncomeSummary;
};

export type ReferralEarning = {
  id: number;
  level: number;
  percent: number;
  entry_fee: number;
  points: number;
  from_name: string;
  room_id: string | null;
  tournament_id: string | null;
  created_at: string;
};

export type TeamLevel = {
  level: number;
  percent: number;
  members: number;
  points: number;
  entries: number;
};

/** WXO 6-Level Tournament Rewards Program */
export const referralApi = {
  async me(): Promise<ReferralMe> {
    const { data } = await apiClient.get<ReferralMe>('/referral/me');
    return data;
  },

  async income(period: IncomePeriod = 'today') {
    const { data } = await apiClient.get('/referral/income', { params: { period } });
    return data as IncomeSummary & { period: IncomePeriod; points: number };
  },

  async earnings(limit = 50): Promise<ReferralEarning[]> {
    const { data } = await apiClient.get<{ items: ReferralEarning[] }>('/referral/earnings', {
      params: { limit },
    });
    return data.items;
  },

  async team(): Promise<{ levels: TeamLevel[]; total_members: number; total_points: number }> {
    const { data } = await apiClient.get('/referral/team');
    return data;
  },
};
