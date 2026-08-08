import 'package:intl/intl.dart';
import '../constants/app_constants.dart';

String formatRupee(num value) {
  final fmt = NumberFormat.decimalPattern('en_IN');
  return '${AppConstants.currencySymbol}${fmt.format(value)}';
}

String formatCompact(num value) {
  if (value >= 100000) return '${AppConstants.currencySymbol}${(value / 100000).toStringAsFixed(1)}L';
  if (value >= 1000) return '${AppConstants.currencySymbol}${(value / 1000).toStringAsFixed(1)}K';
  return formatRupee(value);
}

String twoDigits(int n) => n.toString().padLeft(2, '0');

String formatCountdown(Duration d) {
  final h = d.inHours;
  final m = d.inMinutes.remainder(60);
  final s = d.inSeconds.remainder(60);
  if (h > 0) return '${twoDigits(h)}:${twoDigits(m)}:${twoDigits(s)}';
  return '${twoDigits(m)}:${twoDigits(s)}';
}
