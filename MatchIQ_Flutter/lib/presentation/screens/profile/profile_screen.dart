import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../providers/app_providers.dart';
import '../../widgets/buttons.dart';
import '../../widgets/common.dart';
import '../../widgets/containers.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profile = ref.watch(profileProvider);
    return AppScaffold(
      title: 'Profile',
      body: profile.when(
        loading: () => const SkeletonList(count: 3),
        error: (e, _) => Center(child: Text('$e')),
        data: (p) => ListView(
          padding: const EdgeInsets.all(16),
          children: [
            GradientContainer(
              glow: true,
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 34,
                    backgroundColor: AppColors.purple,
                    child: Text(
                      p.name.isNotEmpty ? p.name[0] : '?',
                      style: const TextStyle(fontSize: 28, fontWeight: FontWeight.w900),
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(p.name, style: Theme.of(context).textTheme.titleLarge),
                        Text(p.mobile, style: const TextStyle(color: AppColors.textMuted)),
                        const SizedBox(height: 4),
                        Row(
                          children: [
                            Icon(
                              p.kycVerified ? Icons.verified : Icons.warning_amber_rounded,
                              size: 16,
                              color: p.kycVerified ? AppColors.green : AppColors.gold,
                            ),
                            const SizedBox(width: 4),
                            Text(
                              p.kycVerified ? 'KYC Verified' : 'KYC Pending',
                              style: TextStyle(
                                color: p.kycVerified ? AppColors.green : AppColors.gold,
                                fontSize: 12,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            ...[
              ('Edit Profile', Icons.edit_outlined),
              ('KYC', Icons.badge_outlined),
              ('Bank Details', Icons.account_balance_outlined),
              ('UPI', Icons.qr_code_2_rounded),
              ('Security', Icons.lock_outline_rounded),
              ('Refer & Earn', Icons.card_giftcard_outlined),
              ('Support', Icons.support_agent_rounded),
            ].map(
              (item) => Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: GlassCard(
                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
                  child: ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: Icon(item.$2, color: AppColors.neonBlue),
                    title: Text(item.$1, style: const TextStyle(fontWeight: FontWeight.w700)),
                    trailing: const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted),
                    onTap: () {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(content: Text('${item.$1} screen ready for API')),
                      );
                    },
                  ),
                ),
              ),
            ),
            const SizedBox(height: 8),
            AppButton(
              label: 'Logout',
              outlined: true,
              color: AppColors.danger,
              onPressed: () {
                ref.read(authTokenProvider.notifier).state = null;
                ref.read(authSessionProvider.notifier).state = false;
                context.go('/login');
              },
            ),
          ],
        ),
      ),
    );
  }
}
