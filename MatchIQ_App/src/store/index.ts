import { create } from 'zustand';
import type { AuthSession, CurrencyBalances, MatchResultPayload, PlayerProfile } from '../types';

type AuthState = {
  session: AuthSession | null;
  hasOnboarded: boolean;
  isHydrated: boolean;
  setSession: (session: AuthSession | null) => void;
  setOnboarded: (value: boolean) => void;
  setHydrated: (value: boolean) => void;
  logout: () => void;
};

export const useAuthStore = create<AuthState>((set) => ({
  session: null,
  hasOnboarded: false,
  isHydrated: true,
  setSession: (session) => set({ session }),
  setOnboarded: (hasOnboarded) => set({ hasOnboarded }),
  setHydrated: (isHydrated) => set({ isHydrated }),
  logout: () => set({ session: null }),
}));

type PlayerState = {
  profile: PlayerProfile;
  balances: CurrencyBalances;
  unreadNotifications: number;
  setProfile: (profile: Partial<PlayerProfile>) => void;
  setBalances: (balances: Partial<CurrencyBalances>) => void;
  setUnread: (count: number) => void;
};

const defaultProfile: PlayerProfile = {
  id: 'player-1',
  username: 'ronin_tile',
  displayName: 'Golden Ronin',
  avatarId: 'avatar_sage',
  frameId: 'frame_gold',
  level: 24,
  xp: 1860,
  xpToNext: 2500,
    rank: 'Diamond Elite',
  clanId: 'clan-1',
  clanName: 'Neon Legion',
  referralCode: 'MATCHIQ24',
  wins: 241,
  losses: 96,
  winRate: 71.5,
};

export const usePlayerStore = create<PlayerState>((set) => ({
  profile: defaultProfile,
  balances: { coins: 2450, diamonds: 210, energy: 18 },
  unreadNotifications: 3,
  setProfile: (profile) =>
    set((state) => ({ profile: { ...state.profile, ...profile } })),
  setBalances: (balances) =>
    set((state) => ({ balances: { ...state.balances, ...balances } })),
  setUnread: (unreadNotifications) => set({ unreadNotifications }),
}));

type UiState = {
  toast: { message: string; tone: 'info' | 'success' | 'danger' } | null;
  sidebarOpen: boolean;
  lastMatchResult: MatchResultPayload | null;
  showToast: (message: string, tone?: 'info' | 'success' | 'danger') => void;
  clearToast: () => void;
  setSidebarOpen: (open: boolean) => void;
  setLastMatchResult: (result: MatchResultPayload | null) => void;
};

export const useUiStore = create<UiState>((set) => ({
  toast: null,
  sidebarOpen: false,
  lastMatchResult: null,
  showToast: (message, tone = 'info') => set({ toast: { message, tone } }),
  clearToast: () => set({ toast: null }),
  setSidebarOpen: (sidebarOpen) => set({ sidebarOpen }),
  setLastMatchResult: (lastMatchResult) => set({ lastMatchResult }),
}));
