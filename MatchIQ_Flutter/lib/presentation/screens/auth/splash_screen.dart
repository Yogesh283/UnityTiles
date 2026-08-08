import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:go_router/go_router.dart';
import '../../../core/constants/app_constants.dart';
import '../../../core/theme/app_colors.dart';
import '../../widgets/brand.dart';

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> {
  @override
  void initState() {
    super.initState();
    Future.delayed(const Duration(milliseconds: 2400), () {
      if (mounted) context.go('/login');
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Container(
        width: double.infinity,
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            colors: [Color(0xFF12081F), AppColors.background, Color(0xFF07101F)],
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
          ),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const WxoLogo(size: 180)
                .animate(onPlay: (c) => c.repeat(reverse: true))
                .scale(
                  begin: const Offset(0.95, 0.95),
                  end: const Offset(1.05, 1.05),
                  duration: 1200.ms,
                ),
            const SizedBox(height: 28),
            Text(
              AppConstants.appName,
              style: Theme.of(context).textTheme.displayMedium?.copyWith(
                    color: AppColors.white,
                    letterSpacing: 2,
                  ),
            ).animate().fadeIn().slideY(begin: 0.2),
            const SizedBox(height: 8),
            Text(
              AppConstants.tagline,
              style: const TextStyle(color: AppColors.textSecondary),
            ),
            const SizedBox(height: 48),
            const SizedBox(
              width: 36,
              height: 36,
              child: CircularProgressIndicator(
                strokeWidth: 3,
                color: AppColors.gold,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
