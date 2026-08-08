import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/api/auth_api.dart';
import '../../data/models/models.dart';
import '../../data/repositories/match_iq_repository.dart';

final authApiProvider = Provider<AuthApi>((ref) => AuthApi());

/// JWT session from Game DB auth endpoints.
final authTokenProvider = StateProvider<AuthToken?>((ref) => null);

final repositoryProvider = Provider<MatchIqRepository>((ref) => matchIqRepository);

final profileProvider = FutureProvider<UserProfile>((ref) {
  return ref.watch(repositoryProvider).getProfile();
});

final walletProvider = FutureProvider<WalletBalance>((ref) {
  return ref.watch(repositoryProvider).getWallet();
});

final bannersProvider = FutureProvider<List<BannerItem>>((ref) {
  return ref.watch(repositoryProvider).getBanners();
});

final tournamentsProvider = FutureProvider<List<Tournament>>((ref) {
  return ref.watch(repositoryProvider).getTournaments();
});

final tournamentProvider = FutureProvider.family<Tournament?, String>((ref, id) {
  return ref.watch(repositoryProvider).getTournament(id);
});

final leaderboardProvider = FutureProvider<List<LeaderboardEntry>>((ref) {
  return ref.watch(repositoryProvider).getLeaderboard();
});

final transactionsProvider = FutureProvider<List<TransactionItem>>((ref) {
  return ref.watch(repositoryProvider).getTransactions();
});

/// Auth session flag for routing.
final authSessionProvider = StateProvider<bool>((ref) => false);

final createPoolFormProvider = StateProvider<CreatePoolForm>(
  (ref) => CreatePoolForm.fromPreset(players: 10, entryFee: 10),
);
