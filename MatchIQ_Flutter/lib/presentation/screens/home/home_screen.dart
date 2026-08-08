import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../providers/app_providers.dart';
import '../../widgets/cards.dart';
import '../../widgets/common.dart';
import '../../widgets/containers.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profile = ref.watch(profileProvider);
    final wallet = ref.watch(walletProvider);
    final banners = ref.watch(bannersProvider);
    final tournaments = ref.watch(tournamentsProvider);

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: RefreshIndicator(
          color: AppColors.purple,
          onRefresh: () async {
            ref.invalidate(profileProvider);
            ref.invalidate(walletProvider);
            ref.invalidate(bannersProvider);
            ref.invalidate(tournamentsProvider);
          },
          child: CustomScrollView(
            slivers: [
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
                  child: profile.when(
                    loading: () => const SizedBox(height: 56),
                    error: (e, _) => Text('$e'),
                    data: (p) => Row(
                      children: [
                        CircleAvatar(
                          radius: 24,
                          backgroundColor: AppColors.purple,
                          child: Text(p.name.isNotEmpty ? p.name[0] : '?', style: const TextStyle(fontWeight: FontWeight.w900)),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(p.name, style: Theme.of(context).textTheme.titleMedium),
                              Text('Level ${p.level}', style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
                            ],
                          ),
                        ),
                        wallet.when(
                          loading: () => const SizedBox.shrink(),
                          error: (_, __) => const SizedBox.shrink(),
                          data: (w) => GestureDetector(
                            onTap: () => context.go('/wallet'),
                            child: Container(
                              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                              decoration: BoxDecoration(
                                color: AppColors.gold.withValues(alpha: 0.12),
                                borderRadius: BorderRadius.circular(20),
                                border: Border.all(color: AppColors.gold.withValues(alpha: 0.4)),
                              ),
                              child: Text(
                                formatRupee(w.total),
                                style: const TextStyle(color: AppColors.gold, fontWeight: FontWeight.w800),
                              ),
                            ),
                          ),
                        ),
                        IconButton(
                          onPressed: () {},
                          icon: const Icon(Icons.notifications_none_rounded),
                        ),
                        IconButton(
                          onPressed: () => context.go('/profile'),
                          icon: const Icon(Icons.settings_outlined),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
              SliverToBoxAdapter(
                child: SizedBox(
                  height: 140,
                  child: banners.when(
                    loading: () => const SizedBox.shrink(),
                    error: (_, __) => const SizedBox.shrink(),
                    data: (items) => PageView.builder(
                      controller: PageController(viewportFraction: 0.9),
                      itemCount: items.length,
                      itemBuilder: (_, i) {
                        final b = items[i];
                        return Padding(
                          padding: const EdgeInsets.only(right: 10),
                          child: GradientContainer(
                            glow: true,
                            gradient: AppColors.purpleBlueGradient,
                            borderColor: Colors.transparent,
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                  decoration: BoxDecoration(
                                    color: Colors.black26,
                                    borderRadius: BorderRadius.circular(8),
                                  ),
                                  child: Text(b.accentLabel, style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 11)),
                                ),
                                const Spacer(),
                                Text(b.title, style: Theme.of(context).textTheme.titleLarge),
                                Text(b.subtitle, style: const TextStyle(color: Colors.white70)),
                              ],
                            ),
                          ),
                        );
                      },
                    ),
                  ),
                ),
              ),
              const SliverToBoxAdapter(child: SectionHeader(title: 'Quick Actions')),
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  child: Row(
                    children: [
                      _QuickAction(icon: Icons.groups_rounded, label: 'Pool Play', onTap: () => context.go('/tournaments')),
                      _QuickAction(icon: Icons.flash_on_rounded, label: 'Battle', onTap: () => context.go('/tournaments')),
                      _QuickAction(icon: Icons.add_box_rounded, label: 'Create', onTap: () => context.push('/create-pool')),
                      _QuickAction(icon: Icons.leaderboard_rounded, label: 'Ranks', onTap: () => context.go('/leaderboard')),
                    ],
                  ),
                ),
              ),
              const SliverToBoxAdapter(child: SectionHeader(title: 'Live Tournaments')),
              tournaments.when(
                loading: () => const SliverFillRemaining(child: SkeletonList()),
                error: (e, _) => SliverToBoxAdapter(child: Text('$e')),
                data: (list) => SliverPadding(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                  sliver: SliverList.separated(
                    itemCount: list.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 12),
                    itemBuilder: (_, i) => TournamentCard(tournament: list[i]),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _QuickAction extends StatelessWidget {
  const _QuickAction({required this.icon, required this.label, required this.onTap});
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 4),
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(16),
          child: GlassCard(
            padding: const EdgeInsets.symmetric(vertical: 14),
            child: Column(
              children: [
                Icon(icon, color: AppColors.neonPurple),
                const SizedBox(height: 6),
                Text(label, style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700)),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
