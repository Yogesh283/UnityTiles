import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../data/models/models.dart';
import 'buttons.dart';
import 'containers.dart';

class TournamentCard extends StatelessWidget {
  const TournamentCard({super.key, required this.tournament});

  final Tournament tournament;

  @override
  Widget build(BuildContext context) {
    return GradientContainer(
      glow: true,
      borderColor: AppColors.purple.withValues(alpha: 0.35),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 56,
                height: 56,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  gradient: AppColors.purpleBlueGradient,
                  borderRadius: BorderRadius.circular(14),
                  boxShadow: [
                    BoxShadow(
                      color: AppColors.blue.withValues(alpha: 0.4),
                      blurRadius: 12,
                    ),
                  ],
                ),
                child: Text(
                  tournament.gameImageLabel,
                  style: const TextStyle(
                    fontWeight: FontWeight.w900,
                    fontSize: 14,
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      tournament.name,
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${tournament.playersJoined}/${tournament.maxPlayers} joined · ${tournament.winners} winner${tournament.winners > 1 ? 's' : ''}',
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                  ],
                ),
              ),
              _TimerChip(duration: tournament.startsIn),
            ],
          ),
          const SizedBox(height: 14),
          ClipRRect(
            borderRadius: BorderRadius.circular(8),
            child: LinearProgressIndicator(
              value: tournament.fillPercent,
              minHeight: 6,
              backgroundColor: AppColors.border,
              valueColor: const AlwaysStoppedAnimation(AppColors.neonPurple),
            ),
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              _Stat(label: 'Entry', value: formatRupee(tournament.entryFee)),
              _Stat(label: 'Prize', value: formatRupee(tournament.prizePool), highlight: true),
              _Stat(label: 'Slots', value: '${tournament.remainingSlots}'),
            ],
          ),
          const SizedBox(height: 14),
          GradientButton(
            label: 'JOIN NOW',
            height: 46,
            onPressed: () => context.push('/tournament/${tournament.id}'),
          ),
        ],
      ),
    ).animate().fadeIn(duration: 350.ms).slideY(begin: 0.08, end: 0);
  }
}

class _TimerChip extends StatelessWidget {
  const _TimerChip({required this.duration});
  final Duration duration;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: AppColors.gold.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppColors.gold.withValues(alpha: 0.4)),
      ),
      child: Text(
        formatCountdown(duration),
        style: const TextStyle(
          color: AppColors.gold,
          fontWeight: FontWeight.w800,
          fontSize: 12,
        ),
      ),
    );
  }
}

class _Stat extends StatelessWidget {
  const _Stat({required this.label, required this.value, this.highlight = false});
  final String label;
  final String value;
  final bool highlight;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: const TextStyle(color: AppColors.textMuted, fontSize: 11)),
          const SizedBox(height: 2),
          Text(
            value,
            style: TextStyle(
              color: highlight ? AppColors.gold : AppColors.white,
              fontWeight: FontWeight.w800,
              fontSize: 15,
            ),
          ),
        ],
      ),
    );
  }
}

class PrizeCard extends StatelessWidget {
  const PrizeCard({super.key, required this.label, required this.amount});
  final String label;
  final double amount;

  @override
  Widget build(BuildContext context) {
    return GlassCard(
      padding: const EdgeInsets.all(14),
      child: Column(
        children: [
          Text(label, style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
          const SizedBox(height: 6),
          Text(
            formatRupee(amount),
            style: const TextStyle(
              color: AppColors.gold,
              fontWeight: FontWeight.w900,
              fontSize: 16,
            ),
          ),
        ],
      ),
    );
  }
}

class WalletCard extends StatelessWidget {
  const WalletCard({
    super.key,
    required this.title,
    required this.amount,
    required this.icon,
    this.color = AppColors.purple,
  });

  final String title;
  final double amount;
  final IconData icon;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return GlassCard(
      child: Row(
        children: [
          Container(
            width: 42,
            height: 42,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.15),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(icon, color: color),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
                Text(
                  formatRupee(amount),
                  style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class StatisticCard extends StatelessWidget {
  const StatisticCard({
    super.key,
    required this.label,
    required this.value,
  });

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return GradientContainer(
      padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 12),
      child: Column(
        children: [
          Text(value, style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 18, color: AppColors.gold)),
          const SizedBox(height: 4),
          Text(label, style: const TextStyle(color: AppColors.textMuted, fontSize: 11)),
        ],
      ),
    );
  }
}
