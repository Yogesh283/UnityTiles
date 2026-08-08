class AppConstants {
  AppConstants._();

  static const appName = 'MATCH IQ';
  static const tagline = 'Premium Esports Tournaments';
  static const currencySymbol = '₹';

  /// Backend FastAPI → MySQL `game` DB. Phone/emulator same WiFi: use LAN IP.
  /// Android emulator localhost = http://10.0.2.2:8000/api/v1
  static const apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://192.168.31.210:8000/api/v1',
  );
}
