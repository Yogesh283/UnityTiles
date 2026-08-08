import 'package:flutter/material.dart';

/// MATCH IQ brand colors — premium dark esports palette.
class AppColors {
  AppColors._();

  static const background = Color(0xFF090B18);
  static const surface = Color(0xFF12152A);
  static const surfaceElevated = Color(0xFF1A1E38);
  static const purple = Color(0xFF7B2FF7);
  static const blue = Color(0xFF2F80ED);
  static const gold = Color(0xFFF5B700);
  static const green = Color(0xFF22C55E);
  static const white = Color(0xFFFFFFFF);
  static const textSecondary = Color(0xFFB8BDD4);
  static const textMuted = Color(0xFF6B7190);
  static const danger = Color(0xFFEF4444);
  static const border = Color(0xFF2A2F4A);
  static const neonPurple = Color(0xFF9B5CFF);
  static const neonBlue = Color(0xFF4DA3FF);

  static const purpleBlueGradient = LinearGradient(
    colors: [purple, blue],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  static const goldGradient = LinearGradient(
    colors: [Color(0xFFFFD54F), gold, Color(0xFFC49000)],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  static const darkCardGradient = LinearGradient(
    colors: [Color(0xFF1A1E38), Color(0xFF0E1122)],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );
}
