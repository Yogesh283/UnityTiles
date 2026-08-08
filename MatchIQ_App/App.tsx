import 'react-native-gesture-handler';
import React, { useEffect, useRef } from 'react';
import { NavigationContainer, NavigationContainerRef } from '@react-navigation/native';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { StatusBar } from 'expo-status-bar';
import * as SplashScreen from 'expo-splash-screen';
import * as Linking from 'expo-linking';
import {
  useFonts,
  Orbitron_400Regular,
  Orbitron_600SemiBold,
  Orbitron_700Bold,
} from '@expo-google-fonts/orbitron';
import {
  Inter_400Regular,
  Inter_500Medium,
  Inter_600SemiBold,
  Inter_700Bold,
} from '@expo-google-fonts/inter';
import { StyleSheet } from 'react-native';
import { RootNavigator } from './src/navigation';
import { Toast, Sidebar } from './src/components';
import { unityBridge } from './src/services';
import { useUiStore, usePlayerStore } from './src/store';
import { ROUTES } from './src/constants';
import { colors } from './src/theme';

SplashScreen.preventAutoHideAsync().catch(() => undefined);

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 30_000 },
  },
});

const linking = {
  prefixes: [Linking.createURL('/'), 'wxo://', 'matchiq://'],
  config: {
    screens: {
      WXOLobby: '',
      MainTabs: {
        path: 'native',
        screens: {
          Home: '',
          Tournament: 'pools',
          Events: 'events',
          Wallet: 'wallet',
          Profile: 'profile',
        },
      },
      UnityGameplay: 'unity',
      GameplayLoader: 'play',
      Games: 'games',
      Victory: 'match-result',
      Defeat: 'match-defeat',
    },
  },
};

export default function App() {
  const navigationRef = useRef<NavigationContainerRef<any>>(null);
  const setLast = useUiStore((s) => s.setLastMatchResult);
  const setBalances = usePlayerStore((s) => s.setBalances);
  const balances = usePlayerStore((s) => s.balances);

  const [fontsLoaded] = useFonts({
    Orbitron_400Regular,
    Orbitron_600SemiBold,
    Orbitron_700Bold,
    Inter_400Regular,
    Inter_500Medium,
    Inter_600SemiBold,
    Inter_700Bold,
  });

  useEffect(() => {
    if (fontsLoaded) SplashScreen.hideAsync().catch(() => undefined);
  }, [fontsLoaded]);

  useEffect(() => {
    const handleUrl = (url: string) => {
      const result = unityBridge.parseMatchResultUrl(url);
      if (!result) return;
      setLast(result);
      setBalances({ coins: balances.coins + result.coinsEarned });
      navigationRef.current?.navigate(
        result.won ? ROUTES.Victory : ROUTES.Defeat,
        { result },
      );
    };

    const sub = Linking.addEventListener('url', ({ url }) => handleUrl(url));
    Linking.getInitialURL().then((url) => {
      if (url) handleUrl(url);
    });
    return () => sub.remove();
  }, [balances.coins, setBalances, setLast]);

  if (!fontsLoaded) return null;

  return (
    <GestureHandlerRootView style={styles.root}>
      <QueryClientProvider client={queryClient}>
        <NavigationContainer ref={navigationRef} linking={linking} theme={{
          dark: true,
          colors: {
            primary: colors.purple,
            background: colors.background,
            card: colors.secondaryBlack,
            text: colors.textPrimary,
            border: colors.border,
            notification: colors.danger,
          },
          fonts: {
            regular: { fontFamily: 'Inter_400Regular', fontWeight: '400' },
            medium: { fontFamily: 'Inter_500Medium', fontWeight: '500' },
            bold: { fontFamily: 'Inter_700Bold', fontWeight: '700' },
            heavy: { fontFamily: 'Inter_700Bold', fontWeight: '800' },
          },
        }}>
          <StatusBar style="light" />
          <RootNavigator />
          <Toast />
          <Sidebar />
        </NavigationContainer>
      </QueryClientProvider>
    </GestureHandlerRootView>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.background },
});
