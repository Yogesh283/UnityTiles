class UserProfile {
  const UserProfile({
    required this.id,
    required this.name,
    required this.mobile,
    required this.avatarUrl,
    required this.level,
    required this.kycVerified,
  });

  final String id;
  final String name;
  final String mobile;
  final String avatarUrl;
  final int level;
  final bool kycVerified;
}

class WalletBalance {
  const WalletBalance({
    required this.total,
    required this.deposit,
    required this.winnings,
    required this.bonus,
    required this.referral,
  });

  final double total;
  final double deposit;
  final double winnings;
  final double bonus;
  final double referral;
}

class BannerItem {
  const BannerItem({
    required this.id,
    required this.title,
    required this.subtitle,
    required this.accentLabel,
  });

  final String id;
  final String title;
  final String subtitle;
  final String accentLabel;
}

class Tournament {
  const Tournament({
    required this.id,
    required this.name,
    required this.gameImageLabel,
    required this.entryFee,
    required this.playersJoined,
    required this.maxPlayers,
    required this.winners,
    required this.prizePool,
    required this.startsIn,
    required this.rules,
    required this.prizeDistribution,
  });

  final String id;
  final String name;
  final String gameImageLabel;
  final double entryFee;
  final int playersJoined;
  final int maxPlayers;
  final int winners;
  final double prizePool;
  final Duration startsIn;
  final List<String> rules;
  final Map<String, double> prizeDistribution;

  int get remainingSlots => maxPlayers - playersJoined;
  double get fillPercent => playersJoined / maxPlayers;
}

class LeaderboardEntry {
  const LeaderboardEntry({
    required this.rank,
    required this.name,
    required this.wins,
    required this.winRate,
    required this.prizeEarned,
    this.isCurrentUser = false,
  });

  final int rank;
  final String name;
  final int wins;
  final double winRate;
  final double prizeEarned;
  final bool isCurrentUser;
}

class TransactionItem {
  const TransactionItem({
    required this.id,
    required this.title,
    required this.amount,
    required this.type,
    required this.createdAt,
    required this.status,
  });

  final String id;
  final String title;
  final double amount;
  final String type; // deposit | withdraw | entry | prize | bonus | referral
  final DateTime createdAt;
  final String status;
}

class CreatePoolForm {
  CreatePoolForm({
    this.players = 10,
    this.winnerCount = 1,
    this.entryFee = 10,
    this.firstPercent = 100,
    this.secondPercent = 0,
    this.thirdPercent = 0,
    this.othersPercent = 0,
    this.notes = '',
  });

  int players;
  int winnerCount;
  double entryFee;
  double firstPercent;
  double secondPercent;
  double thirdPercent;
  double othersPercent;
  String notes;

  /// Prize Pool = 70% of total collection (IQFX Pro).
  double get prizePool => players * entryFee * 0.7;

  double get totalCollection => players * entryFee;

  factory CreatePoolForm.fromPreset({required int players, required double entryFee}) {
    final winners = players <= 50
        ? 1
        : players <= 100
            ? 2
            : 3;
    final percents = winners == 1
        ? [100.0, 0.0, 0.0]
        : winners == 2
            ? [70.0, 30.0, 0.0]
            : [60.0, 25.0, 15.0];
    return CreatePoolForm(
      players: players,
      winnerCount: winners,
      entryFee: entryFee,
      firstPercent: percents[0],
      secondPercent: percents[1],
      thirdPercent: percents[2],
    );
  }
}
