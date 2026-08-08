import '../models/models.dart';
import '../../core/constants/pool_rules.dart';

/// Dummy API repository — swap with real HTTP later.
class MatchIqRepository {
  Future<void> delay([int ms = 400]) => Future.delayed(Duration(milliseconds: ms));

  Future<UserProfile> getProfile() async {
    await delay();
    return const UserProfile(
      id: 'u1',
      name: 'Golden Ronin',
      mobile: '+91 98765 43210',
      avatarUrl: '',
      level: 24,
      kycVerified: true,
    );
  }

  Future<WalletBalance> getWallet() async {
    await delay();
    return const WalletBalance(
      total: 2450,
      deposit: 1200,
      winnings: 890,
      bonus: 210,
      referral: 150,
    );
  }

  Future<List<BannerItem>> getBanners() async {
    await delay(200);
    return const [
      BannerItem(
        id: 'b1',
        title: 'IQFX Pro Mega Clash',
        subtitle: '1000 players · ₹70,000 prize vault',
        accentLabel: 'HOT',
      ),
      BannerItem(
        id: 'b2',
        title: 'Entry ₹10 · ₹50 · ₹100',
        subtitle: '70% prize pool · fair esports pools',
        accentLabel: 'POOL',
      ),
      BannerItem(
        id: 'b3',
        title: 'Refer & Earn',
        subtitle: 'Invite friends · earn ₹50 each',
        accentLabel: 'REF',
      ),
    ];
  }

  Tournament _pool({
    required String id,
    required String name,
    required String label,
    required int maxPlayers,
    required double entryFee,
    required int joined,
    required Duration startsIn,
    required List<String> rules,
  }) {
    final winners = PoolRules.winnersFor(maxPlayers);
    final pool = PoolRules.prizePool(maxPlayers, entryFee);
    final amounts = PoolRules.prizeAmounts(maxPlayers, entryFee);
    return Tournament(
      id: id,
      name: name,
      gameImageLabel: label,
      entryFee: entryFee,
      playersJoined: joined,
      maxPlayers: maxPlayers,
      winners: winners,
      prizePool: pool,
      startsIn: startsIn,
      rules: rules,
      prizeDistribution: amounts,
    );
  }

  Future<List<Tournament>> getTournaments() async {
    await delay();
    // Showcase each size × popular entry (IQFX Pro table).
    return [
      _pool(
        id: 't10-10',
        name: 'Blitz Pool · 10',
        label: '10P',
        maxPlayers: 10,
        entryFee: 10,
        joined: 7,
        startsIn: const Duration(minutes: 4, seconds: 32),
        rules: const [
          'IQFX Pro · 1 winner takes 100%',
          'Prize pool = 70% of collection',
          'Fair play · same board for all',
        ],
      ),
      _pool(
        id: 't50-50',
        name: 'Storm Cup · 50',
        label: '50P',
        maxPlayers: 50,
        entryFee: 50,
        joined: 38,
        startsIn: const Duration(minutes: 12, seconds: 10),
        rules: const [
          'IQFX Pro · 1 winner takes 100%',
          'Entry non-refundable after join',
        ],
      ),
      _pool(
        id: 't100-100',
        name: 'Crown Arena · 100',
        label: '100P',
        maxPlayers: 100,
        entryFee: 100,
        joined: 81,
        startsIn: const Duration(minutes: 28),
        rules: const [
          'Top 2 share · 🥇 70% • 🥈 30%',
          'Anti-cheat monitoring on',
        ],
      ),
      _pool(
        id: 't500-100',
        name: 'Jade Inferno · 500',
        label: '500P',
        maxPlayers: 500,
        entryFee: 100,
        joined: 412,
        startsIn: const Duration(hours: 1, minutes: 5),
        rules: const [
          'Top 3 · 🥇 60% • 🥈 25% • 🥉 15%',
          'Season rank points apply',
        ],
      ),
      _pool(
        id: 't1000-100',
        name: 'Empire Finals · 1000',
        label: '1K',
        maxPlayers: 1000,
        entryFee: 100,
        joined: 876,
        startsIn: const Duration(hours: 3, minutes: 22),
        rules: const [
          'Top 3 · 🥇 60% • 🥈 25% • 🥉 15%',
          'KYC required for payout',
        ],
      ),
      // Extra entry-fee variants
      _pool(
        id: 't10-50',
        name: 'Blitz Pool · 10 · ₹50',
        label: '10P',
        maxPlayers: 10,
        entryFee: 50,
        joined: 4,
        startsIn: const Duration(minutes: 8),
        rules: const ['1 winner · 100% of prize pool'],
      ),
      _pool(
        id: 't10-100',
        name: 'Blitz Pool · 10 · ₹100',
        label: '10P',
        maxPlayers: 10,
        entryFee: 100,
        joined: 3,
        startsIn: const Duration(minutes: 15),
        rules: const ['1 winner · 100% of prize pool'],
      ),
      _pool(
        id: 't50-10',
        name: 'Storm Cup · 50 · ₹10',
        label: '50P',
        maxPlayers: 50,
        entryFee: 10,
        joined: 22,
        startsIn: const Duration(minutes: 18),
        rules: const ['1 winner · 100% of prize pool'],
      ),
    ];
  }

  Future<Tournament?> getTournament(String id) async {
    final list = await getTournaments();
    try {
      return list.firstWhere((t) => t.id == id);
    } catch (_) {
      return null;
    }
  }

  Future<List<LeaderboardEntry>> getLeaderboard() async {
    await delay();
    return const [
      LeaderboardEntry(rank: 1, name: 'SakuraBlade', wins: 312, winRate: 78.2, prizeEarned: 128400),
      LeaderboardEntry(rank: 2, name: 'NeonFox', wins: 288, winRate: 74.1, prizeEarned: 98200),
      LeaderboardEntry(rank: 3, name: 'Golden Ronin', wins: 241, winRate: 71.5, prizeEarned: 76450, isCurrentUser: true),
      LeaderboardEntry(rank: 4, name: 'PixelKing', wins: 210, winRate: 68.0, prizeEarned: 55100),
      LeaderboardEntry(rank: 5, name: 'VoidRunner', wins: 198, winRate: 66.4, prizeEarned: 48900),
    ];
  }

  Future<List<TransactionItem>> getTransactions() async {
    await delay();
    final now = DateTime.now();
    return [
      TransactionItem(id: 'tx1', title: 'Prize · Storm Cup', amount: 1750, type: 'prize', createdAt: now.subtract(const Duration(hours: 2)), status: 'completed'),
      TransactionItem(id: 'tx2', title: 'Entry · Blitz Pool', amount: -10, type: 'entry', createdAt: now.subtract(const Duration(hours: 3)), status: 'completed'),
      TransactionItem(id: 'tx3', title: 'Deposit UPI', amount: 500, type: 'deposit', createdAt: now.subtract(const Duration(days: 1)), status: 'completed'),
      TransactionItem(id: 'tx4', title: 'Referral Bonus', amount: 50, type: 'referral', createdAt: now.subtract(const Duration(days: 2)), status: 'completed'),
      TransactionItem(id: 'tx5', title: 'Withdraw Bank', amount: -300, type: 'withdraw', createdAt: now.subtract(const Duration(days: 3)), status: 'pending'),
    ];
  }
}

final matchIqRepository = MatchIqRepository();
