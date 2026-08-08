import 'package:flutter/material.dart';
import '../../../core/theme/app_colors.dart';

class GradientButton extends StatelessWidget {
  const GradientButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.height = 52,
    this.gradient = AppColors.purpleBlueGradient,
    this.textColor = AppColors.white,
    this.icon,
  });

  final String label;
  final VoidCallback? onPressed;
  final double height;
  final Gradient gradient;
  final Color textColor;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onPressed,
        borderRadius: BorderRadius.circular(16),
        child: Ink(
          height: height,
          decoration: BoxDecoration(
            gradient: onPressed == null ? null : gradient,
            color: onPressed == null ? AppColors.border : null,
            borderRadius: BorderRadius.circular(16),
            boxShadow: onPressed == null
                ? null
                : [
                    BoxShadow(
                      color: AppColors.purple.withValues(alpha: 0.35),
                      blurRadius: 16,
                      offset: const Offset(0, 6),
                    ),
                  ],
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              if (icon != null) ...[
                Icon(icon, color: textColor, size: 20),
                const SizedBox(width: 8),
              ],
              Text(
                label,
                style: TextStyle(
                  color: textColor,
                  fontWeight: FontWeight.w800,
                  fontSize: 15,
                  letterSpacing: 0.4,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class AppButton extends StatelessWidget {
  const AppButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.outlined = false,
    this.color = AppColors.purple,
  });

  final String label;
  final VoidCallback? onPressed;
  final bool outlined;
  final Color color;

  @override
  Widget build(BuildContext context) {
    if (outlined) {
      return OutlinedButton(
        onPressed: onPressed,
        style: OutlinedButton.styleFrom(
          foregroundColor: color,
          side: BorderSide(color: color.withValues(alpha: 0.7)),
          minimumSize: const Size.fromHeight(52),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        ),
        child: Text(label, style: const TextStyle(fontWeight: FontWeight.w700)),
      );
    }
    return ElevatedButton(
      onPressed: onPressed,
      style: ElevatedButton.styleFrom(
        backgroundColor: color,
        foregroundColor: AppColors.white,
        minimumSize: const Size.fromHeight(52),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
      child: Text(label, style: const TextStyle(fontWeight: FontWeight.w700)),
    );
  }
}
