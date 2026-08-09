import React, { useCallback, useMemo, useRef, useState } from 'react';
import {
  ActivityIndicator,
  BackHandler,
  Platform,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { WebView, type WebViewMessageEvent } from 'react-native-webview';
import type { ShouldStartLoadRequest } from 'react-native-webview/lib/WebViewTypes';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useFocusEffect } from '@react-navigation/native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ROUTES, WXO_ROOM_TOURNAMENTS, WXO_SITE_URL } from '../../constants';
import { consumePendingWxoMatchResult } from '../../services/wxoMatchBridge';
import { useAuthStore, usePlayerStore } from '../../store';

type Props = NativeStackScreenProps<any>;

const INJECT = `
(function(){
  try {
    window.WXO_IN_APP = true;
    window.WXO_APP_PACKAGE = 'fun.wxo.app';
    window.WXO_NATIVE_UNITY = true;

    // Hand the signed-in web session to the native shell so both share one account.
    var sendSession = function () {
      try {
        var token = localStorage.getItem('wxo_token');
        if (!token) return;
        var user = null;
        try { user = JSON.parse(localStorage.getItem('wxo_user_v1') || 'null'); } catch (e) {}
        window.ReactNativeWebView.postMessage(
          JSON.stringify({ type: 'WXO_SESSION', token: token, user: user })
        );
      } catch (e) {}
    };
    sendSession();
    window.addEventListener('wxo:auth', sendSession);

    document.documentElement.classList.add('wxo-in-app');
    var style = document.createElement('style');
    style.id = 'wxo-in-app-css';
    if (!document.getElementById('wxo-in-app-css')) {
      style.textContent = '.apk-banner{display:none!important} a[download][href$=".apk"],a[href*="MatchIQ-Unity"],a[href*="WXO-release.apk"]{display:none!important}';
      (document.head || document.documentElement).appendChild(style);
    }
  } catch(e) {}
  true;
})();
`;

function parsePlayParams(url: string): { game: string; entry: number; prize: number } {
  try {
    const u = new URL(url);
    return {
      game: u.searchParams.get('game') || 'IQ Match',
      entry: Number(u.searchParams.get('entry') || 0) || 0,
      prize: Number(u.searchParams.get('prize') || 100) || 100,
    };
  } catch {
    return { game: 'IQ Match', entry: 0, prize: 100 };
  }
}

function isPlayUrl(url: string): boolean {
  return /\/play\.html(\?|$)/i.test(url) || /wxo:\/\/play/i.test(url);
}

function gameIdFromName(game?: string) {
  const n = String(game || '').toLowerCase();
  if (n.includes('ludo')) return 'ludo';
  if (n.includes('rac')) return 'racing';
  return 'iq-match';
}

/**
 * Website lobby (rmsurveyai.com) inside WXO APK.
 * Play → match-lobby (fee/rooms) → native Unity. One wallet, one APK.
 */
export function WXOLobbyScreen({ navigation }: Props) {
  const webRef = useRef<WebView>(null);
  const insets = useSafeAreaInsets();
  const [loading, setLoading] = useState(true);
  const [canGoBack, setCanGoBack] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Pull-to-refresh: only armed while the page is scrolled to the very top so a hard downward
  // pull reloads the WebView, but scrolling within the page stays untouched.
  const [refreshing, setRefreshing] = useState(false);
  const [atTop, setAtTop] = useState(true);
  const [webHeight, setWebHeight] = useState(0);

  const onRefresh = useCallback(() => {
    setRefreshing(true);
    webRef.current?.reload();
  }, []);
  const setSession = useAuthStore((s) => s.setSession);
  const profile = usePlayerStore((s) => s.profile);

  /**
   * The website holds the signed-in session. Mirror its token into the native store so the
   * matchmaking API and match socket act as the same account the player sees on the page.
   */
  const adoptWebSession = useCallback(
    (payload?: { token?: string; user?: { user_uuid?: string; display_name?: string } }) => {
      const token = payload?.token;
      if (!token || useAuthStore.getState().session?.token === token) return;
      setSession({
        token,
        refreshToken: '',
        user: {
          ...profile,
          id: payload?.user?.user_uuid || profile.id,
          displayName: payload?.user?.display_name || profile.displayName,
        },
      });
    },
    [profile, setSession],
  );

  const openMatchmaking = useCallback(
    (opts?: {
      game?: string;
      gameId?: string;
      roomKey?: string;
      tournamentId?: string;
      entry?: number;
      prize?: number;
      players?: number;
      live?: boolean;
    }) => {
      const entry = opts?.entry ?? 0;
      const roomKey = opts?.roomKey || 'duel';
      navigation.navigate(ROUTES.Matchmaking, {
        game: opts?.game || 'IQ Match',
        gameId: opts?.gameId || gameIdFromName(opts?.game),
        roomKey,
        tournamentId: opts?.tournamentId || WXO_ROOM_TOURNAMENTS[roomKey],
        entry,
        prize: opts?.prize ?? entry * 2,
        players: opts?.players ?? 2,
        live: !!opts?.live,
      });
    },
    [navigation],
  );

  const openMatchLobbyInWeb = useCallback((game?: string) => {
    const id = gameIdFromName(game);
    const url = `https://rmsurveyai.com/match-lobby.html?game=${encodeURIComponent(id)}`;
    webRef.current?.injectJavaScript(
      `window.location.href=${JSON.stringify(url)}; true;`,
    );
  }, []);

  const syncWalletFromNative = useCallback(() => {
    const result = consumePendingWxoMatchResult();
    if (!result) return;
    const js = `
      (function(){
        try {
          if (window.WXOWallet && window.WXOWallet.applyMatchResult) {
            window.WXOWallet.applyMatchResult(${JSON.stringify({ ...result, silent: true })});
          }
        } catch(e) {}
        true;
      })();
    `;
    webRef.current?.injectJavaScript(js);
  }, []);

  useFocusEffect(
    useCallback(() => {
      syncWalletFromNative();
      const onAndroidBack = () => {
        if (canGoBack && webRef.current) {
          webRef.current.goBack();
          return true;
        }
        return false;
      };
      if (Platform.OS !== 'android') return undefined;
      const sub = BackHandler.addEventListener('hardwareBackPress', onAndroidBack);
      return () => sub.remove();
    }, [canGoBack, syncWalletFromNative]),
  );

  const source = useMemo(() => ({ uri: WXO_SITE_URL }), []);

  const onShouldStart = useCallback(
    (req: ShouldStartLoadRequest) => {
      const url = req.url || '';

      // Old play.html links → shared match lobby (fee / rooms), not raw HTML game
      if (isPlayUrl(url)) {
        const p = parsePlayParams(url);
        openMatchLobbyInWeb(p.game);
        return false;
      }

      if (
        url.startsWith('intent:') ||
        url.startsWith('wxo:') ||
        url.startsWith('matchiq:') ||
        url.startsWith('matchiqunity:')
      ) {
        return false;
      }

      if (url.startsWith('http://') || url.startsWith('https://') || url.startsWith('about:')) {
        if (/\.apk(\?|$)/i.test(url)) {
          return false;
        }
        return true;
      }
      return false;
    },
    [openMatchLobbyInWeb],
  );

  const onMessage = useCallback(
    (event: WebViewMessageEvent) => {
      try {
        const data = JSON.parse(event.nativeEvent.data || '{}');
        if (data?.type === 'WXO_SESSION') {
          adoptWebSession(data);
        }
        if (data?.type === 'PLAY_UNITY') {
          adoptWebSession(data);
          openMatchmaking({
            game: data.game,
            gameId: data.gameId,
            roomKey: data.roomKey,
            tournamentId: data.tournamentId,
            entry: Number(data.entry) || 0,
            prize: Number(data.prize) || 0,
            players: Number(data.players) || 2,
            live: !!data.live,
          });
        }
        if (data?.type === 'OPEN_MATCH_LOBBY') {
          openMatchLobbyInWeb(data.game || data.gameId);
        }
      } catch {
        // ignore non-JSON
      }
    },
    [adoptWebSession, openMatchmaking, openMatchLobbyInWeb],
  );

  const reinject = useCallback(() => {
    webRef.current?.injectJavaScript(INJECT);
  }, []);

  return (
    <View style={styles.root}>
      {/* Solid status-bar band (edge-to-edge is forced on Android 15 / SDK 54, so the app always
          draws to the very top). This reserves the status-bar height with the WXO brand colour so
          the OS battery / network / clock icons stay visible, and the website's fixed header and its
          top-left / top-right buttons sit below the status bar instead of under it. */}
      <View style={[styles.statusBand, { height: insets.top }]} />
      {error ? (
        <View style={styles.errorBox}>
          <Text style={styles.errorTitle}>Connection issue</Text>
          <Text style={styles.errorMsg}>{error}</Text>
          <Pressable
            style={styles.retry}
            onPress={() => {
              setError(null);
              setLoading(true);
              webRef.current?.reload();
            }}
          >
            <Text style={styles.retryText}>Retry</Text>
          </Pressable>
        </View>
      ) : (
        <ScrollView
          style={styles.web}
          contentContainerStyle={styles.webContent}
          onLayout={(e) => setWebHeight(e.nativeEvent.layout.height)}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={onRefresh}
              enabled={atTop}
              colors={['#E31C23']}
              tintColor="#E31C23"
              progressBackgroundColor="#FFFFFF"
            />
          }
        >
          <WebView
            ref={webRef}
            source={source}
            style={[styles.web, { height: webHeight }]}
            nestedScrollEnabled
            scrollEventThrottle={16}
            onScroll={(e) => {
              const top = e.nativeEvent.contentOffset.y <= 0;
              setAtTop((prev) => (prev === top ? prev : top));
            }}
            onLoadStart={() => setLoading(true)}
            onLoadEnd={() => {
              setLoading(false);
              setRefreshing(false);
              reinject();
            }}
            onError={() => {
              setLoading(false);
              setRefreshing(false);
              setError('Could not load WXO. Check internet and try again.');
            }}
            onHttpError={() => {
              setLoading(false);
              setRefreshing(false);
              setError('Server error. Please retry.');
            }}
            onNavigationStateChange={(nav) => setCanGoBack(!!nav.canGoBack)}
            onShouldStartLoadWithRequest={onShouldStart}
            onMessage={onMessage}
            injectedJavaScriptBeforeContentLoaded={INJECT}
            injectedJavaScript={INJECT}
            javaScriptEnabled
            domStorageEnabled
            thirdPartyCookiesEnabled
            sharedCookiesEnabled
            allowsBackForwardNavigationGestures
            setSupportMultipleWindows={false}
            mediaPlaybackRequiresUserAction={false}
            originWhitelist={['*']}
            userAgent={
              Platform.OS === 'android'
                ? 'Mozilla/5.0 (Linux; Android 13) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36 WXO-App/1.0 fun.wxo.app'
                : undefined
            }
          />
        </ScrollView>
      )}
      {loading && !error ? (
        <View style={styles.loader} pointerEvents="none">
          <ActivityIndicator size="large" color="#E31C23" />
          <Text style={styles.loaderText}>Loading WXO…</Text>
        </View>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: '#FFFFFF' },
  statusBand: { backgroundColor: '#E31C23' },
  web: { flex: 1, backgroundColor: '#FFFFFF' },
  webContent: { flexGrow: 1 },
  loader: {
    ...StyleSheet.absoluteFillObject,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(255,255,255,0.85)',
  },
  loaderText: { marginTop: 10, color: '#6B7280', fontWeight: '700' },
  errorBox: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 24 },
  errorTitle: { fontSize: 18, fontWeight: '800', color: '#111', marginBottom: 8 },
  errorMsg: { fontSize: 14, color: '#6B7280', textAlign: 'center', marginBottom: 16 },
  retry: {
    backgroundColor: '#E31C23',
    paddingHorizontal: 20,
    paddingVertical: 12,
    borderRadius: 12,
  },
  retryText: { color: '#fff', fontWeight: '800' },
});
