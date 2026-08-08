import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/constants/app_constants.dart';
import '../../../core/theme/app_colors.dart';
import '../../../data/api/auth_api.dart';
import '../../providers/app_providers.dart';
import '../../widgets/brand.dart';
import '../../widgets/buttons.dart';
import '../../widgets/containers.dart';
import '../../widgets/common.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _emailCtrl = TextEditingController(text: 'player@matchiq.fun');
  final _passwordCtrl = TextEditingController(text: 'temple123');
  bool _loading = false;
  bool _isRegister = false;
  final _nameCtrl = TextEditingController();

  @override
  void dispose() {
    _emailCtrl.dispose();
    _passwordCtrl.dispose();
    _nameCtrl.dispose();
    super.dispose();
  }

  Future<void> _apply(AuthToken token) async {
    ref.read(authTokenProvider.notifier).state = token;
    ref.read(authSessionProvider.notifier).state = true;
    if (mounted) context.go('/home');
  }

  void _toast(String msg) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  Future<void> _submit() async {
    final email = _emailCtrl.text.trim();
    final password = _passwordCtrl.text;
    if (!email.contains('@') || password.length < 6) {
      _toast('Valid email + password (min 6) required');
      return;
    }
    setState(() => _loading = true);
    try {
      final api = ref.read(authApiProvider);
      final token = _isRegister
          ? await api.register(
              email: email,
              password: password,
              displayName: _nameCtrl.text.trim().isEmpty
                  ? 'Player'
                  : _nameCtrl.text.trim(),
            )
          : await api.login(email: email, password: password);
      await _apply(token);
      _toast(_isRegister ? 'Registered in Game DB' : 'Game DB login successful');
    } catch (e) {
      _toast(e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _guest() async {
    setState(() => _loading = true);
    try {
      final token = await ref.read(authApiProvider).guest(displayName: 'Guest Player');
      await _apply(token);
      _toast('Guest login · Game DB');
    } catch (e) {
      _toast(e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _google() async {
    setState(() => _loading = true);
    try {
      final id = DateTime.now().millisecondsSinceEpoch;
      final token = await ref.read(authApiProvider).google(
            googleId: 'google-demo-$id',
            email: 'google.$id@matchiq.fun',
            displayName: 'Google Player',
          );
      await _apply(token);
      _toast('Google login · Game DB');
    } catch (e) {
      _toast(e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      title: _isRegister ? 'Register' : 'Welcome',
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Center(child: WxoLogo(size: 120)),
            const SizedBox(height: 16),
            Text('WXO', style: Theme.of(context).textTheme.displayMedium),
            const SizedBox(height: 8),
            Text(
              _isRegister
                  ? 'New user Game database में save होगा'
                  : 'Login with Game database (Backend API)',
              style: const TextStyle(color: AppColors.textSecondary),
            ),
            const SizedBox(height: 4),
            Text(
              AppConstants.apiBaseUrl,
              style: const TextStyle(color: AppColors.textMuted, fontSize: 11),
            ),
            const SizedBox(height: 28),
            GlassCard(
              child: Column(
                children: [
                  if (_isRegister) ...[
                    TextField(
                      controller: _nameCtrl,
                      decoration: const InputDecoration(labelText: 'Display Name'),
                    ),
                    const SizedBox(height: 12),
                  ],
                  TextField(
                    controller: _emailCtrl,
                    keyboardType: TextInputType.emailAddress,
                    autocorrect: false,
                    decoration: const InputDecoration(labelText: 'Email'),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _passwordCtrl,
                    obscureText: true,
                    decoration: const InputDecoration(
                      labelText: 'Password (min 6)',
                    ),
                  ),
                  const SizedBox(height: 16),
                  GradientButton(
                    label: _loading
                        ? 'Please wait…'
                        : (_isRegister ? 'CREATE ACCOUNT' : 'LOGIN'),
                    onPressed: _loading ? null : _submit,
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            TextButton(
              onPressed: _loading
                  ? null
                  : () => setState(() => _isRegister = !_isRegister),
              child: Text(
                _isRegister
                    ? 'पहले से अकाउंट है? Login'
                    : 'नया अकाउंट बनाएं · Register',
                style: const TextStyle(color: AppColors.gold),
              ),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(child: Divider(color: AppColors.border)),
                const Padding(
                  padding: EdgeInsets.symmetric(horizontal: 12),
                  child: Text('OR', style: TextStyle(color: AppColors.textMuted)),
                ),
                Expanded(child: Divider(color: AppColors.border)),
              ],
            ),
            const SizedBox(height: 20),
            AppButton(
              label: 'Continue with Google',
              outlined: true,
              color: AppColors.blue,
              onPressed: _loading ? null : _google,
            ),
            const SizedBox(height: 12),
            AppButton(
              label: 'Continue as Guest',
              outlined: true,
              color: AppColors.gold,
              onPressed: _loading ? null : _guest,
            ),
          ],
        ),
      ),
    );
  }
}
