import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/constants/pool_rules.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../providers/app_providers.dart';
import '../../widgets/buttons.dart';
import '../../widgets/cards.dart';
import '../../widgets/common.dart';
import '../../widgets/containers.dart';

class TournamentListScreen extends ConsumerWidget {
  const TournamentListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tournaments = ref.watch(tournamentsProvider);
    return AppScaffold(
      title: 'IQFX Pro Pools',
      actions: [
        IconButton(
          onPressed: () => context.push('/create-pool'),
          icon: const Icon(Icons.add_circle_outline),
        ),
      ],
      body: tournaments.when(
        loading: () => const SkeletonList(),
        error: (e, _) => Center(child: Text('$e')),
        data: (list) => ListView.builder(
          padding: const EdgeInsets.all(16),
          itemCount: list.length + 1,
          itemBuilder: (_, i) {
            if (i == 0) {
              return const Padding(
                padding: EdgeInsets.only(bottom: 16),
                child: _IqfxIntroCard(),
              );
            }
            return Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: TournamentCard(tournament: list[i - 1]),
            );
          },
        ),
      ),
    );
  }
}

class _IqfxIntroCard extends StatelessWidget {
  const _IqfxIntroCard();

  @override
  Widget build(BuildContext context) {
    return GlassCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            '🏆 IQFX Pro Tournament',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 4),
          const Text(
            'Entry ₹10 · ₹50 · ₹100  ·  Prize = 70% of collection',
            style: TextStyle(color: AppColors.textSecondary, fontSize: 13),
          ),
          const SizedBox(height: 12),
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: DataTable(
              headingRowHeight: 32,
              dataRowMinHeight: 32,
              dataRowMaxHeight: 36,
              columnSpacing: 12,
              headingTextStyle: const TextStyle(
                color: AppColors.textMuted,
                fontSize: 11,
                fontWeight: FontWeight.w700,
              ),
              dataTextStyle: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
              columns: const [
                DataColumn(label: Text('P')),
                DataColumn(label: Text('W')),
                DataColumn(label: Text('₹10')),
                DataColumn(label: Text('₹50')),
                DataColumn(label: Text('₹100')),
              ],
              rows: PoolRules.structureRows
                  .map(
                    (r) => DataRow(
                      cells: [
                        DataCell(Text('${r.players}')),
                        DataCell(Text('${r.winners}')),
                        DataCell(Text(formatRupee(r.prize10))),
                        DataCell(Text(formatRupee(r.prize50))),
                        DataCell(Text(formatRupee(r.prize100))),
                      ],
                    ),
                  )
                  .toList(),
            ),
          ),
          const SizedBox(height: 8),
          GradientButton(
            label: '➕ Create New Pool',
            onPressed: () => context.push('/create-pool'),
          ),
        ],
      ),
    );
  }
}

class TournamentDetailScreen extends ConsumerWidget {
  const TournamentDetailScreen({super.key, required this.id});
  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(tournamentProvider(id));
    return AppScaffold(
      title: 'Tournament',
      showBack: true,
      body: async.when(
        loading: () => const SkeletonList(count: 2),
        error: (e, _) => Center(child: Text('$e')),
        data: (t) {
          if (t == null) return const Center(child: Text('Not found'));
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              GradientContainer(
                glow: true,
                gradient: AppColors.purpleBlueGradient,
                borderColor: Colors.transparent,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(t.name, style: Theme.of(context).textTheme.displayMedium),
                    const SizedBox(height: 8),
                    Text('Starts in ${formatCountdown(t.startsIn)}', style: const TextStyle(fontWeight: FontWeight.w700)),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(child: StatisticCard(label: 'Entry', value: formatRupee(t.entryFee))),
                  const SizedBox(width: 8),
                  Expanded(child: StatisticCard(label: 'Prize Pool', value: formatRupee(t.prizePool))),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(child: StatisticCard(label: 'Joined', value: '${t.playersJoined}/${t.maxPlayers}')),
                  const SizedBox(width: 8),
                  Expanded(child: StatisticCard(label: 'Slots Left', value: '${t.remainingSlots}')),
                ],
              ),
              const SizedBox(height: 16),
              Text('Prize Distribution', style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: 8),
              ...t.prizeDistribution.entries.map(
                (e) => Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: PrizeCard(label: e.key, amount: e.value),
                ),
              ),
              const SizedBox(height: 8),
              Text('Game Rules', style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: 8),
              GlassCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: t.rules
                      .map(
                        (r) => Padding(
                          padding: const EdgeInsets.only(bottom: 8),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Icon(Icons.check_circle, color: AppColors.green, size: 18),
                              const SizedBox(width: 8),
                              Expanded(child: Text(r)),
                            ],
                          ),
                        ),
                      )
                      .toList(),
                ),
              ),
              const SizedBox(height: 20),
              GradientButton(
                label: 'JOIN · ${formatRupee(t.entryFee)}',
                onPressed: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text('Joined ${t.name} · Unity gameplay launches next')),
                  );
                },
              ),
            ],
          );
        },
      ),
    );
  }
}
