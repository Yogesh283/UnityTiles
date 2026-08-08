export type CurrencyBalances = {
  coins: number;
  diamonds: number;
  energy: number;
};

export type PlayerProfile = {
  id: string;
  username: string;
  displayName: string;
  avatarId: string;
  frameId: string;
  level: number;
  xp: number;
  xpToNext: number;
  rank: string;
  clanId?: string;
  clanName?: string;
  referralCode: string;
  wins: number;
  losses: number;
  winRate: number;
};

export type AuthSession = {
  token: string;
  refreshToken: string;
  user: PlayerProfile;
};

export type TournamentStatus = 'open' | 'filling' | 'starting' | 'full' | 'live' | 'ended';

export type Tournament = {
  id: string;
  name: string;
  status: TournamentStatus;
  players: number;
  maxPlayers: number;
  winners: number;
  entryFee: number;
  prizePool: number;
  topWin: number;
  icon: string;
  description: string;
  startsInMinutes?: number;
  prizeDistribution?: Record<string, number>;
  rules?: string[];
};

export type MatchResultPayload = {
  matchId: string;
  tournamentId?: string;
  won: boolean;
  score: number;
  timeSeconds: number;
  accuracy: number;
  coinsEarned: number;
  xpEarned: number;
  opponentName?: string;
};

export type Mission = {
  id: string;
  title: string;
  description: string;
  progress: number;
  target: number;
  rewardCoins: number;
  completed: boolean;
  claimed: boolean;
};

export type LeaderboardEntry = {
  rank: number;
  playerId: string;
  name: string;
  avatarId: string;
  score: number;
  isCurrentUser?: boolean;
};

export type Transaction = {
  id: string;
  type: 'deposit' | 'withdraw' | 'entry' | 'prize' | 'purchase' | 'reward';
  amount: number;
  currency: 'coins' | 'diamonds';
  title: string;
  createdAt: string;
  status: 'completed' | 'pending' | 'failed';
};

export type StoreItem = {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: 'coins' | 'diamonds' | 'real';
  realPriceLabel?: string;
  category: 'coins' | 'diamonds' | 'energy' | 'cosmetic' | 'boost';
  amount?: number;
};

export type InventoryItem = {
  id: string;
  name: string;
  type: 'avatar' | 'frame' | 'boost' | 'ticket';
  quantity: number;
  equipped?: boolean;
};

export type NotificationItem = {
  id: string;
  title: string;
  body: string;
  read: boolean;
  createdAt: string;
  type: 'reward' | 'tournament' | 'social' | 'system';
};

export type MailItem = {
  id: string;
  from: string;
  subject: string;
  body: string;
  read: boolean;
  rewardCoins?: number;
  createdAt: string;
};

export type Friend = {
  id: string;
  name: string;
  avatarId: string;
  online: boolean;
  level: number;
};

export type Clan = {
  id: string;
  name: string;
  tag: string;
  members: number;
  maxMembers: number;
  trophies: number;
  description: string;
};

export type Achievement = {
  id: string;
  title: string;
  description: string;
  progress: number;
  target: number;
  unlocked: boolean;
};

export type BattlePassTier = {
  level: number;
  freeReward: string;
  premiumReward: string;
  claimed: boolean;
  locked: boolean;
};

export type EventItem = {
  id: string;
  title: string;
  description: string;
  endsInHours: number;
  rewardLabel: string;
};

export type MatchHistoryItem = {
  id: string;
  opponent: string;
  won: boolean;
  score: number;
  playedAt: string;
  mode: string;
};

export type UnityLaunchPayload = {
  matchId: string;
  tournamentId?: string;
  levelId?: string;
  mode: 'tournament' | 'campaign' | 'practice';
  token: string;
};
