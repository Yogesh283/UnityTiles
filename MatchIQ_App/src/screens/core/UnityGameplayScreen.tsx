import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Platform, StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { PrimaryButton, SecondaryButton, Loader } from '../../components';
import { colors, spacing, typography } from '../../theme';
import { ROUTES } from '../../constants';
import { useAuthStore, usePlayerStore, useUiStore } from '../../store';
import { unityBridge } from '../../services';
import { setPendingWxoMatchResult } from '../../services/wxoMatchBridge';
import { tournamentApi } from '../../api/tournamentApi';
import type { MatchResultPayload } from '../../types';

type Props = NativeStackScreenProps<any>;

type UnityViewComponent = React.ComponentType<{
  style?: object;
  ref?: React.Ref<any>;
  onUnityMessage?: (event: { nativeEvent: { message: string } }) => void;
  androidKeepPlayerMounted?: boolean;
  fullScreen?: boolean;
}>;

/** Unity reports raw board stats; ranking and rewards are the server's call. */
type BoardStats = { won: boolean; score: number; moves: number; elapsedSeconds: number };

function parseBoardStats(raw: string): BoardStats | null {
  const parsed = unityBridge.parseMatchResultUrl(raw) || unityBridge.parseMatchResultJson(raw);
  if (!parsed) return null;

  let moves = 0;
  try {
    const json = JSON.parse(raw);
    moves = Number(json?.moves) || 0;
  } catch {
    const match = /[?&]moves=(\d+)/.exec(raw);
    moves = match ? Number(match[1]) : 0;
  }

  return {
    won: !!parsed.won,
    score: Number(parsed.score) || 0,
    moves: Math.max(1, moves),
    elapsedSeconds: Math.max(1, Number(parsed.timeSeconds) || 1),
  };
}

/**
 * Embedded Unity gameplay (single APK). Unity ships the board and nothing else — matchmaking,
 * countdown and the result screen all live in React Native, and the winner's coins are credited
 * by the backend when the score is submitted.
 */
export function UnityGameplayScreen({ navigation, route }: Props) {
  const roomId = route.params?.roomId as string | undefined;
  const tournamentId = route.params?.tournamentId as string | undefined;
  const levelIndex = route.params?.levelIndex as number | undefined;
  const mode = (route.params?.mode as 'tournament' | 'campaign' | 'practice') || 'tournament';
  const gameName = (route.params?.game as string) || 'IQ Match';
  const entryFee = Number(route.params?.entry) || 0;
  const winPrize = Number(route.params?.prize) || entryFee * 2;
  const token = useAuthStore((s) => s.session?.token) || 'demo-token';
  const setLast = useUiStore((s) => s.setLastMatchResult);
  const setBalances = usePlayerStore((s) => s.setBalances);
  const finished = useRef(false);
  const matchIdRef = useRef(roomId || `match-${Date.now()}`);
  const unityRef = useRef<{
    postMessage: (go: string, method: string, message: string) => void;
    unloadUnity?: () => void;
  } | null>(null);

  const [UnityView, setUnityView] = useState<UnityViewComponent | null>(null);
  const [nativeMissing, setNativeMissing] = useState(false);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        // Native module — only exists in custom/dev APK, not Expo Go
        const mod = require('@azesmway/react-native-unity');
        if (mounted) setUnityView(() => mod.default as UnityViewComponent);
      } catch {
        if (mounted) setNativeMissing(true);
      }
    })();
    return () => {
      mounted = false;
    };
  }, []);

  const launchPayload = useMemo(
    () => ({
      matchId: matchIdRef.current,
      roomId,
      tournamentId,
      levelId: levelIndex != null ? String(levelIndex) : undefined,
      mode,
      token,
    }),
    [levelIndex, mode, roomId, token, tournamentId],
  );

  const finishWithResult = useCallback(
    async (won?: boolean, raw?: string) => {
      if (finished.current) return;
      finished.current = true;

      const stats: BoardStats = (raw && parseBoardStats(raw)) || {
        won: !!won,
        score: 0,
        moves: 1,
        elapsedSeconds: 1,
      };
      if (won != null) stats.won = won;

      // The server ranks the room and credits the winner. Only fall back to the local 2× rule
      // when there is no server room (practice / dev builds).
      let prize = stats.won ? winPrize : 0;
      let rank: number | null = stats.won ? 1 : null;

      if (roomId) {
        try {
          const settled = await tournamentApi.submitScore({
            roomId,
            score: stats.score,
            moves: stats.moves,
            elapsedSeconds: stats.elapsedSeconds,
          });
          prize = settled.prize;
          rank = settled.rank;
          stats.won = settled.prize > 0;
          if (settled.wallet_balance != null) setBalances({ coins: settled.wallet_balance });
        } catch {
          // Keep the optimistic result; the wallet re-syncs from the server on the next screen.
        }
      }

      const result: MatchResultPayload = {
        matchId: matchIdRef.current,
        won: stats.won,
        score: stats.score,
        timeSeconds: stats.elapsedSeconds,
        accuracy: 0,
        coinsEarned: prize,
        xpEarned: stats.won ? 80 : 25,
        opponentName: rank ? `Rank #${rank}` : 'Opponent',
      };

      setPendingWxoMatchResult({
        won: result.won,
        prize,
        game: gameName,
        matchId: matchIdRef.current,
      });

      setLast(result);
      navigation.replace(result.won ? ROUTES.Victory : ROUTES.Defeat, { result });
    },
    [gameName, navigation, roomId, setBalances, setLast, winPrize],
  );

  useEffect(() => {
    if (!UnityView || !unityRef.current) return;
    // Tell Unity shell to start this match (MatchIQShellBridge.OnReactNativeLaunch)
    const json = JSON.stringify(launchPayload);
    try {
      unityRef.current.postMessage('MatchIQShellBridge', 'OnReactNativeLaunch', json);
    } catch {
      // Unity scene may not have the GO yet — shell also reads deep link / absoluteURL
    }
  }, [UnityView, launchPayload]);

  const onUnityMessage = useCallback(
    (event: { nativeEvent: { message: string } }) => {
      const message = event.nativeEvent.message || '';
      if (!message) return;
      if (
        message.startsWith('matchiq://') ||
        message.includes('match-result') ||
        message.includes('"won"')
      ) {
        void finishWithResult(undefined, message);
      }
    },
    [finishWithResult],
  );

  if (Platform.OS === 'web' || nativeMissing) {
    return (
      <View style={styles.center}>
        <Text style={typography.hero}>Unity Embed</Text>
        <Text style={[typography.body, styles.body]}>
          Expo Go mein Unity embed nahi chalta. Ek native APK banao:
          {'\n\n'}1) Unity → Export Android Library → MatchIQ_App/unity/builds/android
          {'\n'}2) npx expo prebuild
          {'\n'}3) npx expo run:android
        </Text>
        <PrimaryButton
          title="Simulate Victory (Dev)"
          onPress={() => void finishWithResult(true)}
          style={styles.btn}
        />
        <SecondaryButton
          title="Simulate Defeat (Dev)"
          onPress={() => void finishWithResult(false)}
          style={styles.btn}
        />
        <SecondaryButton title="Back" onPress={() => navigation.goBack()} style={styles.btn} />
      </View>
    );
  }

  if (!UnityView) {
    return <Loader fullScreen label="Loading match…" />;
  }

  return (
    <View style={styles.root}>
      <UnityView
        ref={unityRef as any}
        style={styles.unity}
        fullScreen
        androidKeepPlayerMounted
        onUnityMessage={onUnityMessage}
      />
      <View style={styles.overlay}>
        <SecondaryButton title="Forfeit" onPress={() => void finishWithResult(false)} />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.background },
  unity: { flex: 1 },
  overlay: {
    position: 'absolute',
    top: 48,
    right: 16,
  },
  center: {
    flex: 1,
    backgroundColor: colors.background,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.lg,
  },
  body: { textAlign: 'center', marginVertical: spacing.md },
  btn: { alignSelf: 'stretch', marginTop: spacing.sm },
});
