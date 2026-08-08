import { useQuery } from '@tanstack/react-query';
import { dummyApi } from '../api';

export const queryKeys = {
  tournaments: ['tournaments'] as const,
  tournament: (id: string) => ['tournament', id] as const,
  leaderboard: ['leaderboard'] as const,
  missions: ['missions'] as const,
  events: ['events'] as const,
  friends: ['friends'] as const,
  clan: ['clan'] as const,
  transactions: ['transactions'] as const,
  store: ['store'] as const,
  inventory: ['inventory'] as const,
  notifications: ['notifications'] as const,
  mail: ['mail'] as const,
  achievements: ['achievements'] as const,
  battlePass: ['battlePass'] as const,
  matchHistory: ['matchHistory'] as const,
  dailyReward: ['dailyReward'] as const,
};

export function useTournaments() {
  return useQuery({ queryKey: queryKeys.tournaments, queryFn: () => dummyApi.getTournaments() });
}

export function useTournament(id: string) {
  return useQuery({
    queryKey: queryKeys.tournament(id),
    queryFn: () => dummyApi.getTournament(id),
    enabled: !!id,
  });
}

export function useLeaderboard() {
  return useQuery({ queryKey: queryKeys.leaderboard, queryFn: () => dummyApi.getLeaderboard() });
}

export function useMissions() {
  return useQuery({ queryKey: queryKeys.missions, queryFn: () => dummyApi.getMissions() });
}

export function useEvents() {
  return useQuery({ queryKey: queryKeys.events, queryFn: () => dummyApi.getEvents() });
}

export function useFriends() {
  return useQuery({ queryKey: queryKeys.friends, queryFn: () => dummyApi.getFriends() });
}

export function useClan() {
  return useQuery({ queryKey: queryKeys.clan, queryFn: () => dummyApi.getClan() });
}

export function useTransactions() {
  return useQuery({ queryKey: queryKeys.transactions, queryFn: () => dummyApi.getTransactions() });
}

export function useStore() {
  return useQuery({ queryKey: queryKeys.store, queryFn: () => dummyApi.getStore() });
}

export function useInventory() {
  return useQuery({ queryKey: queryKeys.inventory, queryFn: () => dummyApi.getInventory() });
}

export function useNotifications() {
  return useQuery({ queryKey: queryKeys.notifications, queryFn: () => dummyApi.getNotifications() });
}

export function useMail() {
  return useQuery({ queryKey: queryKeys.mail, queryFn: () => dummyApi.getMail() });
}

export function useAchievements() {
  return useQuery({ queryKey: queryKeys.achievements, queryFn: () => dummyApi.getAchievements() });
}

export function useBattlePass() {
  return useQuery({ queryKey: queryKeys.battlePass, queryFn: () => dummyApi.getBattlePass() });
}

export function useMatchHistory() {
  return useQuery({ queryKey: queryKeys.matchHistory, queryFn: () => dummyApi.getMatchHistory() });
}

export function useDailyReward() {
  return useQuery({ queryKey: queryKeys.dailyReward, queryFn: () => dummyApi.getDailyRewardDay() });
}
