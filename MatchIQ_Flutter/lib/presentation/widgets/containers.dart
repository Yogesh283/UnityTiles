import 'package:flutter/material.dart';
import '../../../core/theme/app_colors.dart';

class GradientContainer extends StatelessWidget {
  const GradientContainer({
    super.key,
    required this.child,
    this.padding,
    this.borderRadius = 18,
    this.gradient = AppColors.darkCardGradient,
    this.borderColor,
    this.glow = false,
  });

  final Widget child;
  final EdgeInsetsGeometry? padding;
  final double borderRadius;
  final Gradient gradient;
  final Color? borderColor;
  final bool glow;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: padding ?? const EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: gradient,
        borderRadius: BorderRadius.circular(borderRadius),
        border: Border.all(color: borderColor ?? AppColors.border),
        boxShadow: glow
            ? [
                BoxShadow(
                  color: AppColors.purple.withValues(alpha: 0.25),
                  blurRadius: 20,
                  spreadRadius: 1,
                ),
              ]
            : null,
      ),
      child: child,
    );
  }
}

class GlassCard extends StatelessWidget {
  const GlassCard({
    super.key,
    required this.child,
    this.padding,
    this.borderRadius = 18,
  });

  final Widget child;
  final EdgeInsetsGeometry? padding;
  final double borderRadius;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: padding ?? const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surfaceElevated.withValues(alpha: 0.72),
        borderRadius: BorderRadius.circular(borderRadius),
        border: Border.all(color: AppColors.white.withValues(alpha: 0.08)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.35),
            blurRadius: 18,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: child,
    );
  }
}
