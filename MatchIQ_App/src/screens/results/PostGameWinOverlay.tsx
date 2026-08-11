import React, { useEffect, useRef } from 'react';
import {
  Animated,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import type { MatchResultPayload } from '../../types';

const C = {
  bg: 'rgba(4, 6, 8, 0.92)',
  panel: '#0B0F0C',
  emerald: '#0FA958',
  emeraldDark: '#087A3F',
  gold: '#D4AF37',
  goldHi: '#FFE08A',
  orange: '#FF9F1C',
  white: '#FFFFFF',
  mute: 'rgba(255,255,255,0.62)',
  card: 'rgba(18, 28, 22, 0.95)',
  border: 'rgba(212, 175, 55, 0.55)',
} as const;

function formatTime(sec: number): string {
  const s = Math.max(0, Math.floor(sec));
  const m = Math.floor(s / 60);
  const r = s % 60;
  return `${String(m).padStart(2, '0')}:${String(r).padStart(2, '0')}`;
}

function formatIq(v: number | undefined, accuracy: number, score: number): string {
  const n = Number.isFinite(v as number) ? (v as number) : accuracy || score;
  return Number.isFinite(n) ? (Math.round(n * 10) / 10).toFixed(n % 1 ? 1 : 0) : '0';
}

type Props = {
  result: MatchResultPayload;
  onContinue: () => void;
  onWatchVideo: () => void;
  onGrantShuffle: () => void;
  onGrantUndo: () => void;
  luckyBusy?: boolean;
  luckyNote?: string | null;
};

/**
 * Full-screen post-win overlay above Unity. Stats come from the Unity bridge payload.
 */
export function PostGameWinOverlay({
  result,
  onContinue,
  onWatchVideo,
  onGrantShuffle,
  onGrantUndo,
  luckyBusy,
  luckyNote,
}: Props) {
  const insets = useSafeAreaInsets();
  const glow = useRef(new Animated.Value(0.55)).current;
  const scale = useRef(new Animated.Value(0.92)).current;
  const sparkle = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    Animated.parallel([
      Animated.spring(scale, { toValue: 1, friction: 6, tension: 80, useNativeDriver: true }),
      Animated.loop(
        Animated.sequence([
          Animated.timing(glow, { toValue: 1, duration: 1200, useNativeDriver: true }),
          Animated.timing(glow, { toValue: 0.45, duration: 1200, useNativeDriver: true }),
        ]),
      ),
      Animated.loop(
        Animated.timing(sparkle, { toValue: 1, duration: 2400, useNativeDriver: true }),
      ),
    ]).start();
  }, [glow, scale, sparkle]);

  const level = Math.max(1, Number(result.level) || 1);
  const nextTarget = level + Math.max(5, 10 - (level % 10));
  const progress = Math.min(1, (level % 10) / 10 || 0.35);
  const message =
    result.message ||
    'Not one fumble, you identified\nlocked tiles accurately!';

  return (
    <View style={[styles.root, { paddingTop: insets.top + 8, paddingBottom: insets.bottom + 10 }]} pointerEvents="box-none">
      <View style={styles.backdrop} />
      <ScrollView
        contentContainerStyle={styles.scroll}
        bounces={false}
        showsVerticalScrollIndicator={false}
      >
        <Animated.View style={[styles.hero, { transform: [{ scale }], opacity: glow }]}>
          <View style={styles.rays} />
          <View style={styles.artWell}>
            {[0, 1, 2, 3, 4, 5].map((i) => (
              <Animated.View
                key={i}
                style={[
                  styles.spark,
                  {
                    left: 20 + i * 22,
                    top: 18 + (i % 3) * 22,
                    opacity: sparkle.interpolate({
                      inputRange: [0, 0.5, 1],
                      outputRange: [0.2, 1, 0.25],
                    }),
                    transform: [
                      {
                        translateY: sparkle.interpolate({
                          inputRange: [0, 1],
                          outputRange: [0, -10 - i * 2],
                        }),
                      },
                    ],
                  },
                ]}
              />
            ))}
            <Text style={styles.trophy}>✦</Text>
          </View>
          <Text style={styles.title}>BRILLIANT!</Text>
        </Animated.View>

        <View style={styles.statsRow}>
          <StatCard label="TIME" value={formatTime(result.timeSeconds)} />
          <StatCard label="IQ" value={formatIq(result.iq, result.accuracy, result.score)} accent />
          <StatCard label="COMBO" value={String(result.combo ?? 0)} />
        </View>

        <Text style={styles.message}>{message}</Text>

        <View style={styles.levelBlock}>
          <View style={styles.levelTrack}>
            {Array.from({ length: 10 }).map((_, i) => (
              <View
                key={i}
                style={[styles.levelSeg, i / 10 < progress && styles.levelSegOn]}
              />
            ))}
          </View>
          <Text style={styles.levelHint}>Reach Level {nextTarget}</Text>
        </View>

        <View style={styles.luckyCard}>
          <Text style={styles.luckyLabel}>LUCKY</Text>
          <View style={styles.luckyRow}>
            <LuckyBtn title="Watch Video" onPress={onWatchVideo} disabled={!!luckyBusy} />
            <LuckyBtn title="Shuffle x2" onPress={onGrantShuffle} disabled={!!luckyBusy} />
            <LuckyBtn title="Undo x2" onPress={onGrantUndo} disabled={!!luckyBusy} />
          </View>
          {!!luckyNote && <Text style={styles.luckyNote}>{luckyNote}</Text>}
        </View>
      </ScrollView>

      <Pressable
        style={({ pressed }) => [styles.continueBtn, pressed && { opacity: 0.9 }]}
        onPress={onContinue}
      >
        <Text style={styles.continueTxt}>CONTINUE</Text>
      </Pressable>
    </View>
  );
}

function StatCard({
  label,
  value,
  accent,
}: {
  label: string;
  value: string;
  accent?: boolean;
}) {
  return (
    <View style={[styles.statCard, accent && styles.statCardAccent]}>
      <Text style={styles.statLabel}>{label}</Text>
      <Text style={styles.statValue} numberOfLines={1} adjustsFontSizeToFit>
        {value}
      </Text>
    </View>
  );
}

function LuckyBtn({
  title,
  onPress,
  disabled,
}: {
  title: string;
  onPress: () => void;
  disabled?: boolean;
}) {
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      style={({ pressed }) => [
        styles.luckyBtn,
        disabled && { opacity: 0.45 },
        pressed && !disabled && { opacity: 0.85 },
      ]}
    >
      <Text style={styles.luckyBtnTxt}>{title}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  root: {
    ...StyleSheet.absoluteFillObject,
    zIndex: 80,
    elevation: 80,
    justifyContent: 'space-between',
  },
  backdrop: {
    ...StyleSheet.absoluteFillObject,
    backgroundColor: C.bg,
  },
  scroll: {
    paddingHorizontal: 16,
    paddingBottom: 12,
    alignItems: 'center',
  },
  hero: { width: '100%', alignItems: 'center', marginBottom: 10 },
  rays: {
    position: 'absolute',
    width: 220,
    height: 220,
    borderRadius: 110,
    backgroundColor: 'rgba(15,169,88,0.12)',
    borderWidth: 1,
    borderColor: 'rgba(212,175,55,0.2)',
  },
  artWell: {
    width: 160,
    height: 120,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 4,
  },
  spark: {
    position: 'absolute',
    width: 6,
    height: 6,
    borderRadius: 3,
    backgroundColor: C.goldHi,
  },
  trophy: {
    fontSize: 64,
    color: C.goldHi,
    textShadowColor: C.orange,
    textShadowRadius: 16,
    textShadowOffset: { width: 0, height: 0 },
  },
  title: {
    fontSize: 42,
    fontWeight: '900',
    letterSpacing: 1.5,
    color: C.orange,
    textShadowColor: C.gold,
    textShadowRadius: 12,
    textShadowOffset: { width: 0, height: 0 },
  },
  statsRow: {
    flexDirection: 'row',
    width: '100%',
    gap: 8,
    marginTop: 8,
  },
  statCard: {
    flex: 1,
    backgroundColor: C.card,
    borderRadius: 14,
    borderWidth: 1.5,
    borderColor: C.border,
    paddingVertical: 12,
    paddingHorizontal: 6,
    alignItems: 'center',
    minHeight: 78,
    justifyContent: 'center',
  },
  statCardAccent: {
    borderColor: C.goldHi,
    backgroundColor: 'rgba(20, 36, 26, 0.98)',
  },
  statLabel: {
    color: C.gold,
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 1,
  },
  statValue: {
    color: C.white,
    fontSize: 22,
    fontWeight: '900',
    marginTop: 4,
  },
  message: {
    marginTop: 16,
    textAlign: 'center',
    color: C.white,
    fontSize: 16,
    lineHeight: 22,
    fontWeight: '600',
    textShadowColor: 'rgba(212,175,55,0.45)',
    textShadowRadius: 8,
    textShadowOffset: { width: 0, height: 0 },
    paddingHorizontal: 12,
  },
  levelBlock: { width: '100%', marginTop: 18, alignItems: 'center', gap: 8 },
  levelTrack: { flexDirection: 'row', gap: 4, width: '100%' },
  levelSeg: {
    flex: 1,
    height: 8,
    borderRadius: 4,
    backgroundColor: 'rgba(255,255,255,0.12)',
  },
  levelSegOn: { backgroundColor: C.emerald },
  levelHint: { color: C.mute, fontSize: 13, fontWeight: '700' },
  luckyCard: {
    width: '100%',
    marginTop: 18,
    backgroundColor: C.panel,
    borderRadius: 16,
    borderWidth: 1.5,
    borderColor: C.border,
    padding: 14,
  },
  luckyLabel: {
    color: C.goldHi,
    fontWeight: '900',
    letterSpacing: 2,
    fontSize: 13,
    marginBottom: 10,
  },
  luckyRow: { flexDirection: 'row', gap: 8 },
  luckyBtn: {
    flex: 1,
    minHeight: 44,
    borderRadius: 12,
    borderWidth: 1.5,
    borderColor: C.gold,
    backgroundColor: 'rgba(15,169,88,0.18)',
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 4,
  },
  luckyBtnTxt: {
    color: C.goldHi,
    fontSize: 11,
    fontWeight: '800',
    textAlign: 'center',
  },
  luckyNote: {
    marginTop: 8,
    color: C.mute,
    fontSize: 12,
    textAlign: 'center',
  },
  continueBtn: {
    marginHorizontal: 16,
    minHeight: 56,
    borderRadius: 16,
    backgroundColor: C.emerald,
    borderWidth: 2,
    borderColor: C.gold,
    alignItems: 'center',
    justifyContent: 'center',
    ...Platform.select({
      ios: {
        shadowColor: C.gold,
        shadowOpacity: 0.45,
        shadowRadius: 12,
        shadowOffset: { width: 0, height: 0 },
      },
      android: { elevation: 10, shadowColor: C.gold },
    }),
  },
  continueTxt: {
    color: C.white,
    fontSize: 18,
    fontWeight: '900',
    letterSpacing: 1.2,
  },
});
