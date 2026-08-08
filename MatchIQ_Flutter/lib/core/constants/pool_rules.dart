/// IQFX Pro Tournament – Pool Play rules.
/// Prize Pool = 70% of total collection; 30% platform/ops.
class PoolRules {
  PoolRules._();

  static const prizeShare = 0.70;
  static const platformFeeShare = 0.30;

  static const entryFees = [10.0, 50.0, 100.0];
  static const playerSizes = [10, 50, 100, 500, 1000];

  /// Default winner count by tournament size.
  static int winnersFor(int players) {
    if (players <= 50) return 1;
    if (players <= 100) return 2;
    return 3;
  }

  /// Winner split of prize pool (sums to 100).
  static List<double> distributionPercents(int winners) {
    switch (winners) {
      case 1:
        return [100];
      case 2:
        return [70, 30];
      case 3:
        return [60, 25, 15];
      default:
        return [100];
    }
  }

  static String distributionLabel(int winners) {
    final p = distributionPercents(winners);
    if (winners == 1) return '🥇 100%';
    if (winners == 2) return '🥇 ${p[0].toInt()}% • 🥈 ${p[1].toInt()}%';
    return '🥇 ${p[0].toInt()}% • 🥈 ${p[1].toInt()}% • 🥉 ${p[2].toInt()}%';
  }

  static double prizePool(int players, double entryFee) =>
      players * entryFee * prizeShare;

  static double totalCollection(int players, double entryFee) =>
      players * entryFee;

  /// Absolute prize amounts keyed 1st / 2nd / 3rd.
  static Map<String, double> prizeAmounts(int players, double entryFee) {
    final pool = prizePool(players, entryFee);
    final winners = winnersFor(players);
    final percents = distributionPercents(winners);
    final keys = ['1st', '2nd', '3rd'];
    final map = <String, double>{};
    for (var i = 0; i < percents.length; i++) {
      map[keys[i]] = pool * percents[i] / 100;
    }
    return map;
  }

  /// Official structure rows for UI tables.
  static List<PoolStructureRow> get structureRows => playerSizes
      .map(
        (p) => PoolStructureRow(
          players: p,
          winners: winnersFor(p),
          prize10: prizePool(p, 10),
          prize50: prizePool(p, 50),
          prize100: prizePool(p, 100),
          distributionLabel: distributionLabel(winnersFor(p)),
        ),
      )
      .toList();
}

class PoolStructureRow {
  const PoolStructureRow({
    required this.players,
    required this.winners,
    required this.prize10,
    required this.prize50,
    required this.prize100,
    required this.distributionLabel,
  });

  final int players;
  final int winners;
  final double prize10;
  final double prize50;
  final double prize100;
  final String distributionLabel;
}
