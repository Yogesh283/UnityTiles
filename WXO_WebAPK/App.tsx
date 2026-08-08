import React, { useCallback, useMemo, useRef, useState } from 'react';
import {
  ActivityIndicator,
  BackHandler,
  Platform,
  Pressable,
  StatusBar,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
import { WebView } from 'react-native-webview';

/** Live WXO website — same UI as rmsurveyai.com, runs inside this APK */
const SITE_URL = 'https://rmsurveyai.com/';

const INJECT = `
(function(){
  try {
    window.WXO_IN_APP = true;
    window.WXO_APP_PACKAGE = 'fun.wxo.app';
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

export default function App() {
  const webRef = useRef(null);
  const [loading, setLoading] = useState(true);
  const [canGoBack, setCanGoBack] = useState(false);
  const [error, setError] = useState(null);

  const onAndroidBack = useCallback(() => {
    if (canGoBack && webRef.current) {
      webRef.current.goBack();
      return true;
    }
    return false;
  }, [canGoBack]);

  React.useEffect(() => {
    if (Platform.OS !== 'android') return;
    const sub = BackHandler.addEventListener('hardwareBackPress', onAndroidBack);
    return () => sub.remove();
  }, [onAndroidBack]);

  const source = useMemo(() => ({ uri: SITE_URL }), []);

  const onShouldStart = useCallback((req) => {
    const url = req.url || '';

    // Never leave WXO WebView for intents / other apps — play stays in /play.html
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
  }, []);

  const reinject = useCallback(() => {
    webRef.current?.injectJavaScript(INJECT);
  }, []);

  return (
    <SafeAreaProvider>
      <SafeAreaView style={styles.root} edges={['top', 'bottom']}>
        <StatusBar barStyle="dark-content" backgroundColor="#FFFFFF" />
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
          <WebView
            ref={webRef}
            source={source}
            style={styles.web}
            onLoadStart={() => setLoading(true)}
            onLoadEnd={() => {
              setLoading(false);
              reinject();
            }}
            onError={() => {
              setLoading(false);
              setError('Could not load WXO. Check internet and try again.');
            }}
            onHttpError={() => {
              setLoading(false);
              setError('Server error. Please retry.');
            }}
            onNavigationStateChange={(nav) => setCanGoBack(!!nav.canGoBack)}
            onShouldStartLoadWithRequest={onShouldStart}
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
        )}
        {loading && !error ? (
          <View style={styles.loader} pointerEvents="none">
            <ActivityIndicator size="large" color="#E31C23" />
            <Text style={styles.loaderText}>Loading WXO…</Text>
          </View>
        ) : null}
      </SafeAreaView>
    </SafeAreaProvider>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: '#FFFFFF' },
  web: { flex: 1, backgroundColor: '#FFFFFF' },
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
