import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../providers/app_providers.dart';
import '../../widgets/buttons.dart';
import '../../widgets/cards.dart';
import '../../widgets/common.dart';
import '../../widgets/containers.dart';

class WalletScreen extends ConsumerWidget {
  const WalletScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final wallet = ref.watch(walletProvider);
    final tx = ref.watch(transactionsProvider);

    return AppScaffold(
      title: 'Wallet',
      body: wallet.when(
        loading: () => const SkeletonList(),
        error: (e, _) => Center(child: Text('$e')),
        data: (w) => ListView(
          padding: const EdgeInsets.all(16),
          children: [
            GradientContainer(
              glow: true,
              gradient: AppColors.purpleBlueGradient,
              borderColor: Colors.transparent,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('Total Balance', style: TextStyle(color: Colors.white70)),
                  const SizedBox(height: 6),
                  Text(
                    formatRupee(w.total),
                    style: Theme.of(context).textTheme.displayMedium,
                  ),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      Expanded(
                        child: GradientButton(
                          label: 'Deposit',
                          height: 44,
                          gradient: AppColors.goldGradient,
                          textColor: Colors.black,
                          onPressed: () {
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(content: Text('Deposit flow ready for API')),
                            );
                          },
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: AppButton(
                          label: 'Withdraw',
                          outlined: true,
                          color: Colors.white,
                          onPressed: () {
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(content: Text('Withdraw flow ready for API')),
                            );
                          },
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            WalletCard(title: 'Deposit Wallet', amount: w.deposit, icon: Icons.account_balance_wallet_rounded, color: AppColors.blue),
            const SizedBox(height: 8),
            WalletCard(title: 'Winning Wallet', amount: w.winnings, icon: Icons.emoji_events_rounded, color: AppColors.gold),
            const SizedBox(height: 8),
            WalletCard(title: 'Bonus Wallet', amount: w.bonus, icon: Icons.card_giftcard_rounded, color: AppColors.purple),
            const SizedBox(height: 8),
            WalletCard(title: 'Referral Income', amount: w.referral, icon: Icons.people_alt_rounded, color: AppColors.green),
            const SizedBox(height: 16),
            Text('Transaction History', style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 8),
            tx.when(
              loading: () => const SkeletonList(count: 2),
              error: (e, _) => Text('$e'),
              data: (items) => Column(
                children: items
                    .map(
                      (t) => Padding(
                        padding: const EdgeInsets.only(bottom: 8),
                        child: GlassCard(
                          child: Row(
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(t.title, style: const TextStyle(fontWeight: FontWeight.w700)),
                                    Text(
                                      t.status,
                                      style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
                                    ),
                                  ],
                                ),
                              ),
                              Text(
                                '${t.amount >= 0 ? '+' : ''}${formatRupee(t.amount.abs())}',
                                style: TextStyle(
                                  color: t.amount >= 0 ? AppColors.green : AppColors.danger,
                                  fontWeight: FontWeight.w900,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    )
                    .toList(),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
