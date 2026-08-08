import React from 'react';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { HomeScreen, TournamentScreen, EventsScreen } from '../screens/core/HomeTournamentEvents';
import { WalletScreen } from '../screens/economy/EconomyScreens';
import { ProfileScreen } from '../screens/profile/ProfileScreens';
import { ROUTES } from '../constants';
import { BottomNavigation } from './BottomNavigation';
import { colors } from '../theme';

const Tab = createBottomTabNavigator();

export function MainTabs() {
  return (
    <Tab.Navigator
      tabBar={(props) => <BottomNavigation {...props} />}
      screenOptions={{
        headerShown: false,
        sceneStyle: { backgroundColor: colors.background },
      }}
    >
      <Tab.Screen name={ROUTES.Home} component={HomeScreen} options={{ title: 'Home' }} />
      <Tab.Screen name={ROUTES.Tournament} component={TournamentScreen} options={{ title: 'Tournament' }} />
      <Tab.Screen name={ROUTES.Events} component={EventsScreen} options={{ title: 'Events' }} />
      <Tab.Screen name={ROUTES.Wallet} component={WalletScreen} options={{ title: 'Wallet' }} />
      <Tab.Screen name={ROUTES.Profile} component={ProfileScreen} options={{ title: 'Profile' }} />
    </Tab.Navigator>
  );
}
