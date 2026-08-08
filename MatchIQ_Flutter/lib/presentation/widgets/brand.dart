import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';

/// WXO brand mark used across splash, auth and headers.
class WxoLogo extends StatelessWidget {
  const WxoLogo({super.key, this.size = 110, this.glow = true});

  final double size;
  final bool glow;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: glow
          ? BoxDecoration(
              shape: BoxShape.circle,
              boxShadow: [
                BoxShadow(
                  color: AppColors.blue.withValues(alpha: 0.45),
                  blurRadius: size * 0.28,
                  spreadRadius: 1,
                ),
              ],
            )
          : null,
      child: Image.asset(
        'assets/images/wxo_logo.png',
        width: size,
        height: size,
        fit: BoxFit.contain,
      ),
    );
  }
}
