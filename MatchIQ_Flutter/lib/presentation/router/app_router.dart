import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/app_providers.dart';
import '../screens/auth/login_screen.dart';
import '../screens/auth/splash_screen.dart';
import '../screens/home/home_screen.dart';
import '../screens/leaderboard/leaderboard_screen.dart';
import '../screens/profile/profile_screen.dart';
import '../screens/shell/main_shell.dart';
import '../screens/tournament/create_pool_screen.dart';
import '../screens/tournament/tournament_screens.dart';
import '../screens/wallet/wallet_screen.dart';

final _rootKey = GlobalKey<NavigatorState>();

final appRouterProvider = Provider<GoRouter>((ref) {
  final loggedIn = ref.watch(authSessionProvider);

  return GoRouter(
    navigatorKey: _rootKey,
    initialLocation: '/splash',
    refreshListenable: _AuthRefresh(ref),
    redirect: (context, state) {
      final path = state.uri.path;
      final isAuthFlow = path == '/splash' || path == '/login';
      if (!loggedIn && !isAuthFlow) return '/login';
      if (loggedIn && path == '/login') return '/home';
      return null;
    },
    routes: [
      GoRoute(path: '/splash', builder: (_, __) => const SplashScreen()),
      GoRoute(path: '/login', builder: (_, __) => const LoginScreen()),
      GoRoute(
        path: '/tournament/:id',
        parentNavigatorKey: _rootKey,
        builder: (_, state) => TournamentDetailScreen(id: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/create-pool',
        parentNavigatorKey: _rootKey,
        builder: (_, __) => const CreatePoolScreen(),
      ),
      StatefulShellRoute.indexedStack(
        builder: (_, __, shell) => MainShell(navigationShell: shell),
        branches: [
          StatefulShellBranch(routes: [
            GoRoute(path: '/home', builder: (_, __) => const HomeScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/tournaments', builder: (_, __) => const TournamentListScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/wallet', builder: (_, __) => const WalletScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/leaderboard', builder: (_, __) => const LeaderboardScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/profile', builder: (_, __) => const ProfileScreen()),
          ]),
        ],
      ),
    ],
  );
});

/// Notifies GoRouter when auth session changes.
class _AuthRefresh extends ChangeNotifier {
  _AuthRefresh(this.ref) {
    ref.listen<bool>(authSessionProvider, (_, __) => notifyListeners());
  }
  final Ref ref;
}
