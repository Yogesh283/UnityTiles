import 'dart:convert';
import 'package:http/http.dart' as http;
import '../../core/constants/app_constants.dart';

class AuthToken {
  AuthToken({
    required this.accessToken,
    required this.userId,
    required this.userUuid,
    required this.displayName,
    this.email,
  });

  final String accessToken;
  final int userId;
  final String userUuid;
  final String displayName;
  final String? email;

  factory AuthToken.fromJson(Map<String, dynamic> json, {String? email}) {
    return AuthToken(
      accessToken: json['access_token'] as String,
      userId: json['user_id'] as int,
      userUuid: json['user_uuid'] as String,
      displayName: (json['display_name'] as String?) ?? 'Player',
      email: email,
    );
  }
}

/// Real Game DB auth via Backend FastAPI `/api/v1/auth/*`
class AuthApi {
  AuthApi({http.Client? client}) : _client = client ?? http.Client();

  final http.Client _client;

  Uri _uri(String path) => Uri.parse('${AppConstants.apiBaseUrl}$path');

  Future<AuthToken> register({
    required String email,
    required String password,
    required String displayName,
  }) async {
    final res = await _client.post(
      _uri('/auth/register'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'email': email,
        'password': password,
        'display_name': displayName,
      }),
    );
    return _parse(res, email: email, fallback: 'Register failed');
  }

  Future<AuthToken> login({
    required String email,
    required String password,
  }) async {
    final res = await _client.post(
      _uri('/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email, 'password': password}),
    );
    return _parse(res, email: email, fallback: 'Login failed');
  }

  Future<AuthToken> guest({String displayName = 'Guest'}) async {
    final guestId =
        'guest-${DateTime.now().millisecondsSinceEpoch}-${DateTime.now().microsecond}';
    final res = await _client.post(
      _uri('/auth/guest'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'guest_id': guestId,
        'display_name': displayName,
      }),
    );
    return _parse(res, fallback: 'Guest login failed');
  }

  Future<AuthToken> google({
    required String googleId,
    required String email,
    required String displayName,
  }) async {
    final res = await _client.post(
      _uri('/auth/google'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'google_id': googleId,
        'email': email,
        'display_name': displayName,
      }),
    );
    return _parse(res, email: email, fallback: 'Google login failed');
  }

  AuthToken _parse(
    http.Response res, {
    String? email,
    required String fallback,
  }) {
    final body = res.body.isEmpty ? <String, dynamic>{} : jsonDecode(res.body);
    if (res.statusCode >= 200 && res.statusCode < 300) {
      return AuthToken.fromJson(Map<String, dynamic>.from(body as Map), email: email);
    }
    final detail = body is Map ? body['detail'] : null;
    if (detail is String) throw Exception(detail);
    if (detail is List && detail.isNotEmpty) {
      final first = detail.first;
      if (first is Map && first['msg'] != null) {
        throw Exception(first['msg'].toString());
      }
    }
    throw Exception(fallback);
  }
}
