import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { SplashScreen, OnboardingScreen } from '../screens/auth/SplashOnboarding';
import {
  LoginScreen,
  RegisterScreen,
  OTPVerificationScreen,
  ForgotPasswordScreen,
} from '../screens/auth/AuthScreens';
import { MainTabs } from './MainTabs';
import {
  TournamentDetailsScreen,
  MatchSelectionScreen,
  GameplayLoaderScreen,
} from '../screens/core/MatchFlowScreens';
import { UnityGameplayScreen } from '../screens/core/UnityGameplayScreen';
import { MatchmakingScreen } from '../screens/core/MatchmakingScreen';
import { WXOLobbyScreen } from '../screens/core/WXOLobbyScreen';
import { GamesScreen } from '../screens/core/GamesScreen';
import { CreatePoolScreen } from '../screens/core/CreatePoolScreen';
import { PoolsScreen } from '../screens/core/PoolsScreen';
import { IncomeScreen } from '../screens/core/IncomeScreen';
import { MatchResultScreen, VictoryScreen, DefeatScreen } from '../screens/results/ResultScreens';
import {
  DailyRewardScreen,
  MissionsScreen,
  LeaderboardScreen,
  FriendsScreen,
  ClanScreen,
} from '../screens/meta/MetaScreens';
import {
  EditProfileScreen,
  AvatarSelectionScreen,
  FrameSelectionScreen,
  StatisticsScreen,
  MatchHistoryScreen,
} from '../screens/profile/ProfileScreens';
import {
  DepositScreen,
  WithdrawScreen,
  TransactionHistoryScreen,
  StoreScreen,
  InventoryScreen,
} from '../screens/economy/EconomyScreens';
import {
  NotificationsScreen,
  MailScreen,
  ReferralScreen,
  InviteFriendsScreen,
} from '../screens/social/SocialScreens';
import {
  LuckySpinScreen,
  AchievementsScreen,
  BattlePassScreen,
  SeasonRewardsScreen,
  RankRewardsScreen,
} from '../screens/progression/ProgressionScreens';
import {
  SettingsScreen,
  LanguageScreen,
  PrivacyPolicyScreen,
  TermsScreen,
  HelpCenterScreen,
  ContactSupportScreen,
  AboutScreen,
  LoadingScreen,
  NoInternetScreen,
} from '../screens/system/SystemScreens';
import { ROUTES } from '../constants';
import { colors } from '../theme';

const Stack = createNativeStackNavigator();

const screenOptions = {
  headerShown: false,
  contentStyle: { backgroundColor: colors.background },
  animation: 'fade' as const,
};

/**
 * WXO shipping app: website lobby first, Unity in same APK on Play.
 * Native MatchIQ screens remain available for deep links / rematch.
 */
export function RootNavigator() {
  return (
    <Stack.Navigator screenOptions={screenOptions} initialRouteName={ROUTES.WXOLobby}>
      <Stack.Screen name={ROUTES.WXOLobby} component={WXOLobbyScreen} />
      <Stack.Screen name={ROUTES.Matchmaking} component={MatchmakingScreen} />
      <Stack.Screen name={ROUTES.UnityGameplay} component={UnityGameplayScreen} />
      <Stack.Screen name={ROUTES.Victory} component={VictoryScreen} />
      <Stack.Screen name={ROUTES.Defeat} component={DefeatScreen} />
      <Stack.Screen name={ROUTES.MatchResult} component={MatchResultScreen} />
      <Stack.Screen name={ROUTES.GameplayLoader} component={GameplayLoaderScreen} />
      <Stack.Screen name={ROUTES.Splash} component={SplashScreen} />
      <Stack.Screen name={ROUTES.Onboarding} component={OnboardingScreen} />
      <Stack.Screen name={ROUTES.Login} component={LoginScreen} />
      <Stack.Screen name={ROUTES.Register} component={RegisterScreen} />
      <Stack.Screen name={ROUTES.OTPVerification} component={OTPVerificationScreen} />
      <Stack.Screen name={ROUTES.ForgotPassword} component={ForgotPasswordScreen} />
      <Stack.Screen name={ROUTES.MainTabs} component={MainTabs} />
      <Stack.Screen name={ROUTES.TournamentDetails} component={TournamentDetailsScreen} />
      <Stack.Screen name={ROUTES.MatchSelection} component={MatchSelectionScreen} />
      <Stack.Screen name={ROUTES.Games} component={GamesScreen} />
      <Stack.Screen name={ROUTES.CreatePool} component={CreatePoolScreen} />
      <Stack.Screen name={ROUTES.Pools} component={PoolsScreen} />
      <Stack.Screen name={ROUTES.DailyReward} component={DailyRewardScreen} />
      <Stack.Screen name={ROUTES.Missions} component={MissionsScreen} />
      <Stack.Screen name={ROUTES.Leaderboard} component={LeaderboardScreen} />
      <Stack.Screen name={ROUTES.Friends} component={FriendsScreen} />
      <Stack.Screen name={ROUTES.Clan} component={ClanScreen} />
      <Stack.Screen name={ROUTES.EditProfile} component={EditProfileScreen} />
      <Stack.Screen name={ROUTES.AvatarSelection} component={AvatarSelectionScreen} />
      <Stack.Screen name={ROUTES.FrameSelection} component={FrameSelectionScreen} />
      <Stack.Screen name={ROUTES.Statistics} component={StatisticsScreen} />
      <Stack.Screen name={ROUTES.MatchHistory} component={MatchHistoryScreen} />
      <Stack.Screen name={ROUTES.Deposit} component={DepositScreen} />
      <Stack.Screen name={ROUTES.Withdraw} component={WithdrawScreen} />
      <Stack.Screen name={ROUTES.TransactionHistory} component={TransactionHistoryScreen} />
      <Stack.Screen name={ROUTES.Store} component={StoreScreen} />
      <Stack.Screen name={ROUTES.Inventory} component={InventoryScreen} />
      <Stack.Screen name={ROUTES.Notifications} component={NotificationsScreen} />
      <Stack.Screen name={ROUTES.Mail} component={MailScreen} />
      <Stack.Screen name={ROUTES.Income} component={IncomeScreen} />
      <Stack.Screen name={ROUTES.Referral} component={ReferralScreen} />
      <Stack.Screen name={ROUTES.InviteFriends} component={InviteFriendsScreen} />
      <Stack.Screen name={ROUTES.LuckySpin} component={LuckySpinScreen} />
      <Stack.Screen name={ROUTES.Achievements} component={AchievementsScreen} />
      <Stack.Screen name={ROUTES.BattlePass} component={BattlePassScreen} />
      <Stack.Screen name={ROUTES.SeasonRewards} component={SeasonRewardsScreen} />
      <Stack.Screen name={ROUTES.RankRewards} component={RankRewardsScreen} />
      <Stack.Screen name={ROUTES.Settings} component={SettingsScreen} />
      <Stack.Screen name={ROUTES.Language} component={LanguageScreen} />
      <Stack.Screen name={ROUTES.PrivacyPolicy} component={PrivacyPolicyScreen} />
      <Stack.Screen name={ROUTES.Terms} component={TermsScreen} />
      <Stack.Screen name={ROUTES.HelpCenter} component={HelpCenterScreen} />
      <Stack.Screen name={ROUTES.ContactSupport} component={ContactSupportScreen} />
      <Stack.Screen name={ROUTES.About} component={AboutScreen} />
      <Stack.Screen name={ROUTES.Loading} component={LoadingScreen} />
      <Stack.Screen name={ROUTES.NoInternet} component={NoInternetScreen} />
    </Stack.Navigator>
  );
}
