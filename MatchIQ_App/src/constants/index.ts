export const APP_NAME = 'WXO';
export const APP_TAGLINE = 'Play. Win. Earn.';
export const UNITY_SCHEME = 'matchiqunity://play';
export const RESULT_SCHEME = 'matchiq://match-result';
/** Host app package (Unity embedded in same APK) */
export const WXO_ANDROID_PACKAGE = 'fun.wxo.app';
/** Legacy standalone Unity APK (not used when embed is present) */
export const UNITY_ANDROID_PACKAGE = 'com.matchiq.game';
export const API_BASE_URL =
  process.env.EXPO_PUBLIC_API_URL ?? 'https://rmsurveyai.com/api/v1';
export const WXO_SITE_URL =
  process.env.EXPO_PUBLIC_SITE_URL ?? 'https://rmsurveyai.com/';
/** Live match socket lives at the server root, outside the /api/v1 prefix. */
export const WS_BASE_URL =
  process.env.EXPO_PUBLIC_WS_URL ??
  API_BASE_URL.replace(/^http/, 'ws').replace(/\/api\/v1\/?$/, '');

/**
 * Match lobby room id → backend tournament id. The web lobby and the app offer the same
 * rooms, so both sides join the exact same server-side matchmaking pool.
 */
export const WXO_ROOM_TOURNAMENTS: Record<string, string> = {
  free: 'wxo_free',
  duel: 'wxo_duel2',
  squad5: 'wxo_squad5',
  room10: 'wxo_room10',
  room20: 'wxo_room20',
  live5: 'wxo_live5',
  live10: 'wxo_live10',
};

export const ROUTES = {
  WXOLobby: 'WXOLobby',
  Splash: 'Splash',
  Onboarding: 'Onboarding',
  Login: 'Login',
  Register: 'Register',
  OTPVerification: 'OTPVerification',
  ForgotPassword: 'ForgotPassword',
  MainTabs: 'MainTabs',
  Home: 'Home',
  Tournament: 'Tournament',
  Events: 'Events',
  Wallet: 'Wallet',
  Profile: 'Profile',
  TournamentDetails: 'TournamentDetails',
  MatchSelection: 'MatchSelection',
  Games: 'Games',
  GameplayLoader: 'GameplayLoader',
  Matchmaking: 'Matchmaking',
  UnityGameplay: 'UnityGameplay',
  CreatePool: 'CreatePool',
  MatchResult: 'MatchResult',
  Victory: 'Victory',
  Defeat: 'Defeat',
  DailyReward: 'DailyReward',
  Missions: 'Missions',
  Leaderboard: 'Leaderboard',
  Friends: 'Friends',
  Clan: 'Clan',
  EditProfile: 'EditProfile',
  Deposit: 'Deposit',
  Withdraw: 'Withdraw',
  TransactionHistory: 'TransactionHistory',
  Store: 'Store',
  Inventory: 'Inventory',
  Notifications: 'Notifications',
  Mail: 'Mail',
  Settings: 'Settings',
  Language: 'Language',
  PrivacyPolicy: 'PrivacyPolicy',
  Terms: 'Terms',
  HelpCenter: 'HelpCenter',
  ContactSupport: 'ContactSupport',
  About: 'About',
  Referral: 'Referral',
  Income: 'Income',
  InviteFriends: 'InviteFriends',
  LuckySpin: 'LuckySpin',
  Achievements: 'Achievements',
  BattlePass: 'BattlePass',
  SeasonRewards: 'SeasonRewards',
  RankRewards: 'RankRewards',
  AvatarSelection: 'AvatarSelection',
  FrameSelection: 'FrameSelection',
  Statistics: 'Statistics',
  MatchHistory: 'MatchHistory',
  Loading: 'Loading',
  NoInternet: 'NoInternet',
} as const;

export type RouteName = (typeof ROUTES)[keyof typeof ROUTES];

export * from './poolRules';
