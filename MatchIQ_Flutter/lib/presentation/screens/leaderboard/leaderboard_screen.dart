import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../providers/app_providers.dart';
import '../../widgets/common.dart';
import '../../widgets/containers.dart';

class LeaderboardScreen extends ConsumerWidget {
  const LeaderboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final board = ref.watch(leaderboardProvider);
    return AppScaffold(
      title: 'Leaderboard',
      body: board.when(
        loading: () => const SkeletonList(),
        error: (e, _) => Center(child: Text('$e')),
        data: (list) => ListView.separated(
          padding: const EdgeInsets.all(16),
          itemCount: list.length,
          separatorBuilder: (_, __) => const SizedBox(height: 10),
          itemBuilder: (_, i) {
            final e = list[i];
            final medal = e.rank == 1
                ? AppColors.gold
                : e.rank == 2
                    ? const Color(0xFFC0C0C0)
                    : e.rank == 3
                        ? const Color(0xFFCD7F32)
                        : AppColors.border;
            return GlassCard(
              child: Row(
                children: [
                  Container(
                    width: 36,
                    height: 36,
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      border: Border.all(color: medal, width: 2),
                    ),
                    child: Text('${e.rank}', style: const TextStyle(fontWeight: FontWeight.w900)),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          e.name + (e.isCurrentUser ? ' (You)' : ''),
                          style: TextStyle(
                            fontWeight: FontWeight.w800,
                            color: e.isCurrentUser ? AppColors.gold : AppColors.white,
                          ),
                        ),
                        Text(
                          '${e.wins} wins · ${e.winRate.toStringAsFixed(1)}% WR',
                          style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
                        ),
                      ],
                    ),
                  ),
                  Text(
                    formatCompact(e.prizeEarned),
                    style: const TextStyle(color: AppColors.green, fontWeight: FontWeight.w900),
                  ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}
