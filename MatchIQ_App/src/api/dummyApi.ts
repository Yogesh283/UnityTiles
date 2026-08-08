import type {
  Achievement,
  AuthSession,
  BattlePassTier,
  Clan,
  EventItem,
  Friend,
  InventoryItem,
  LeaderboardEntry,
  MailItem,
  MatchHistoryItem,
  Mission,
  NotificationItem,
  PlayerProfile,
  StoreItem,
  Tournament,
  Transaction,
} from '../types';
import { prizeAmounts, prizePool, winnersFor } from '../constants/poolRules';

const delay = (ms = 400) => new Promise((resolve) => setTimeout(resolve, ms));

const demoUser: PlayerProfile = {
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

function makePool(opts: {
  id: string;
  name: string;
  maxPlayers: number;
  entryFee: number;
  players: number;
  status: Tournament['status'];
  startsInMinutes: number;
  icon: string;
  rules: string[];
}): Tournament {
  const winners = winnersFor(opts.maxPlayers);
  const pool = prizePool(opts.maxPlayers, opts.entryFee);
  const dist = prizeAmounts(opts.maxPlayers, opts.entryFee);
  return {
    id: opts.id,
    name: opts.name,
    status: opts.status,
    players: opts.players,
    maxPlayers: opts.maxPlayers,
    winners,
    entryFee: opts.entryFee,
    prizePool: pool,
    topWin: dist['1st'] ?? pool,
    icon: opts.icon,
    description: `${opts.maxPlayers} players · ${winners} winner${winners > 1 ? 's' : ''} · IQFX Pro`,
    startsInMinutes: opts.startsInMinutes,
    prizeDistribution: dist,
    rules: opts.rules,
  };
}

export const dummyApi = {
  async login(email: string, _password: string): Promise<AuthSession> {
    await delay();
    return {
      token: 'demo-token',
      refreshToken: 'demo-refresh',
      user: { ...demoUser, username: email.split('@')[0] || demoUser.username },
    };
  },

  async loginMobile(mobile: string): Promise<boolean> {
    await delay(500);
    return mobile.replace(/\D/g, '').length >= 10;
  },

  async register(name: string, email: string, _password: string): Promise<AuthSession> {
    await delay();
    return {
      token: 'demo-token',
      refreshToken: 'demo-refresh',
      user: { ...demoUser, displayName: name, username: email.split('@')[0] || 'player' },
    };
  },

  async verifyOtp(_code: string): Promise<boolean> {
    await delay(300);
    return true;
  },

  async forgotPassword(_email: string): Promise<boolean> {
    await delay();
    return true;
  },

  async getTournaments(): Promise<Tournament[]> {
    await delay();
    return [
      makePool({
        id: 't10-10',
        name: 'Blitz Pool · 10',
        maxPlayers: 10,
        entryFee: 10,
        players: 7,
        status: 'open',
        startsInMinutes: 4,
        icon: '10P',
        rules: ['IQFX Pro · 1 winner takes 100%', 'Prize pool = 70% of collection', 'Fair play · same board'],
      }),
      makePool({
        id: 't50-50',
        name: 'Storm Cup · 50',
        maxPlayers: 50,
        entryFee: 50,
        players: 38,
        status: 'filling',
        startsInMinutes: 12,
        icon: '50P',
        rules: ['IQFX Pro · 1 winner takes 100%', 'Entry non-refundable after join'],
      }),
      makePool({
        id: 't100-100',
        name: 'Crown Arena · 100',
        maxPlayers: 100,
        entryFee: 100,
        players: 81,
        status: 'filling',
        startsInMinutes: 28,
        icon: '100P',
        rules: ['Top 2 · 🥇 70% • 🥈 30%', 'Anti-cheat on'],
      }),
      makePool({
        id: 't500-100',
        name: 'Jade Inferno · 500',
        maxPlayers: 500,
        entryFee: 100,
        players: 412,
        status: 'starting',
        startsInMinutes: 65,
        icon: '500P',
        rules: ['Top 3 · 🥇 60% • 🥈 25% • 🥉 15%', 'Season rank points apply'],
      }),
      makePool({
        id: 't1000-100',
        name: 'Empire Finals · 1000',
        maxPlayers: 1000,
        entryFee: 100,
        players: 876,
        status: 'filling',
        startsInMinutes: 202,
        icon: '1K',
        rules: ['Top 3 · 🥇 60% • 🥈 25% • 🥉 15%', 'KYC required for payout'],
      }),
      makePool({
        id: 't10-50',
        name: 'Blitz Pool · 10 · ₹50',
        maxPlayers: 10,
        entryFee: 50,
        players: 4,
        status: 'open',
        startsInMinutes: 8,
        icon: '10P',
        rules: ['1 winner · 100% of prize pool'],
      }),
      makePool({
        id: 't10-100',
        name: 'Blitz Pool · 10 · ₹100',
        maxPlayers: 10,
        entryFee: 100,
        players: 3,
        status: 'open',
        startsInMinutes: 15,
        icon: '10P',
        rules: ['1 winner · 100% of prize pool'],
      }),
      makePool({
        id: 't50-10',
        name: 'Storm Cup · 50 · ₹10',
        maxPlayers: 50,
        entryFee: 10,
        players: 22,
        status: 'filling',
        startsInMinutes: 18,
        icon: '50P',
        rules: ['1 winner · 100% of prize pool'],
      }),
    ];
  },

  async getTournament(id: string): Promise<Tournament | undefined> {
    const list = await this.getTournaments();
    return list.find((t) => t.id === id);
  },

  async getBanners() {
    await delay(200);
    return [
      { id: 'b1', title: 'IQFX Pro Mega Clash', subtitle: '1000 players · ₹70,000 prize vault', badge: 'HOT' },
      { id: 'b2', title: 'Entry ₹10 · ₹50 · ₹100', subtitle: '70% prize pool · fair esports pools', badge: 'POOL' },
      { id: 'b3', title: 'Refer & Earn', subtitle: 'Invite friends · earn ₹50 each', badge: 'REF' },
    ];
  },

  async getLeaderboard(): Promise<LeaderboardEntry[]> {
    await delay();
    return [
      { rank: 1, playerId: 'p2', name: 'SakuraBlade', avatarId: 'a1', score: 128400 },
      { rank: 2, playerId: 'p3', name: 'NeonFox', avatarId: 'a2', score: 98200 },
      { rank: 3, playerId: 'player-1', name: 'Golden Ronin', avatarId: 'a3', score: 76450, isCurrentUser: true },
      { rank: 4, playerId: 'p4', name: 'PixelKing', avatarId: 'a4', score: 55100 },
      { rank: 5, playerId: 'p5', name: 'VoidRunner', avatarId: 'a5', score: 48900 },
    ];
  },

  async getMissions(): Promise<Mission[]> {
    await delay();
    return [
      { id: 'm1', title: 'Win 3 Pools', description: 'Complete 3 tournament wins', progress: 2, target: 3, rewardCoins: 150, completed: false, claimed: false },
      { id: 'm2', title: 'Deposit Once', description: 'Add funds to wallet', progress: 1, target: 1, rewardCoins: 50, completed: true, claimed: false },
    ];
  },

  async getEvents(): Promise<EventItem[]> {
    await delay();
    return [
      { id: 'e1', title: 'Weekend Mega Pool', description: 'Extra prizes on 500+ rooms', endsInHours: 18, rewardLabel: '2x Bonus' },
      { id: 'e2', title: 'Referral Rush', description: '₹50 per friend join', endsInHours: 42, rewardLabel: 'REF' },
    ];
  },

  async getFriends(): Promise<Friend[]> {
    await delay();
    return [
      { id: 'f1', name: 'SakuraBlade', avatarId: 'a1', online: true, level: 31 },
      { id: 'f2', name: 'NeonFox', avatarId: 'a2', online: true, level: 27 },
    ];
  },

  async getClan(): Promise<Clan> {
    await delay();
    return {
      id: 'clan-1',
      name: 'Neon Legion',
      tag: 'NEON',
      members: 28,
      maxMembers: 40,
      trophies: 18420,
      description: 'Elite esports clan. Daily wars at peak hour.',
    };
  },

  async getTransactions(): Promise<Transaction[]> {
    await delay();
    return [
      { id: 'tx1', type: 'prize', amount: 1750, currency: 'coins', title: 'Prize · Storm Cup', createdAt: '2026-08-06T10:00:00Z', status: 'completed' },
      { id: 'tx2', type: 'entry', amount: -10, currency: 'coins', title: 'Entry · Blitz Pool', createdAt: '2026-08-06T09:00:00Z', status: 'completed' },
      { id: 'tx3', type: 'deposit', amount: 500, currency: 'coins', title: 'Deposit UPI', createdAt: '2026-08-05T12:00:00Z', status: 'completed' },
      { id: 'tx4', type: 'withdraw', amount: -300, currency: 'coins', title: 'Withdraw Bank', createdAt: '2026-08-04T09:10:00Z', status: 'pending' },
    ];
  },

  async getStore(): Promise<StoreItem[]> {
    await delay();
    return [
      { id: 's1', name: 'Starter Pack', description: '₹100 wallet credit', price: 99, currency: 'real', realPriceLabel: '₹99', category: 'coins', amount: 100 },
      { id: 's2', name: 'Pro Pack', description: '₹500 + bonus', price: 499, currency: 'real', realPriceLabel: '₹499', category: 'coins', amount: 550 },
    ];
  },

  async getInventory(): Promise<InventoryItem[]> {
    await delay();
    return [
      { id: 'i1', name: 'Pro Avatar', type: 'avatar', quantity: 1, equipped: true },
      { id: 'i2', name: 'Gold Frame', type: 'frame', quantity: 1, equipped: true },
    ];
  },

  async getNotifications(): Promise<NotificationItem[]> {
    await delay();
    return [
      { id: 'n1', title: 'Pool Filling Fast', body: 'Storm Cup is almost full.', read: false, createdAt: '2026-08-06T04:00:00Z', type: 'tournament' },
      { id: 'n2', title: 'Bonus Credited', body: '₹50 referral bonus added.', read: false, createdAt: '2026-08-06T02:00:00Z', type: 'reward' },
    ];
  },

  async getMail(): Promise<MailItem[]> {
    await delay();
    return [
      { id: 'mail1', from: 'MATCH IQ', subject: 'Welcome Bonus', body: 'Claim ₹50 bonus for joining.', read: false, rewardCoins: 50, createdAt: '2026-08-01T10:00:00Z' },
    ];
  },

  async getAchievements(): Promise<Achievement[]> {
    await delay();
    return [
      { id: 'a1', title: 'First Win', description: 'Win your first pool', progress: 1, target: 1, unlocked: true },
      { id: 'a2', title: 'High Roller', description: 'Join a ₹100 entry room', progress: 1, target: 1, unlocked: true },
    ];
  },

  async getBattlePass(): Promise<BattlePassTier[]> {
    await delay();
    return Array.from({ length: 8 }).map((_, i) => ({
      level: i + 1,
      freeReward: `₹${20 * (i + 1)} Bonus`,
      premiumReward: i % 3 === 0 ? 'Frame' : `₹${50 * (i + 1)}`,
      claimed: i < 2,
      locked: i > 4,
    }));
  },

  async getMatchHistory(): Promise<MatchHistoryItem[]> {
    await delay();
    return [
      { id: 'h1', opponent: 'Pool #50', won: true, score: 1240, playedAt: '2026-08-05T18:00:00Z', mode: 'Storm Cup' },
      { id: 'h2', opponent: 'Pool #10', won: false, score: 980, playedAt: '2026-08-05T16:20:00Z', mode: 'Blitz' },
    ];
  },

  async getDailyRewardDay(): Promise<{ day: number; rewards: { day: number; label: string; claimed: boolean }[] }> {
    await delay();
    return {
      day: 3,
      rewards: [
        { day: 1, label: '₹10', claimed: true },
        { day: 2, label: '₹20', claimed: true },
        { day: 3, label: '₹50', claimed: false },
        { day: 4, label: '₹25', claimed: false },
        { day: 5, label: 'Chest', claimed: false },
        { day: 6, label: '₹30', claimed: false },
        { day: 7, label: '₹100', claimed: false },
      ],
    };
  },
};
