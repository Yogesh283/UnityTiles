import React from 'react';
import { Pressable, StatusBar, StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ROUTES } from '../../constants';
import type { MatchResultPayload } from '../../types';
import { useUiStore } from '../../store';
import { goHome } from '../../navigation/nav';
import { radius, spacing, typography } from '../../theme';

type Props = NativeStackScreenProps<any>;

const wxo = {
  bg: '#FFFFFF',
  ink: '#111214',
  soft: '#4B5162',
  muted: '#8A90A2',
  border: '#E6E8EF',
  red: '#E31C23',
  redDark: '#A50E14',
  redSoft: 'rgba(227, 28, 35, 0.10)',
  gold: '#C99213',
} as const;

function useResult(route: Props['route']): MatchResultPayload {
  const stored = useUiStore((s) => s.lastMatchResult);
  return (
    route.params?.result ||
    stored || {
      matchId: 'demo',
      won: true,
      score: 1400,
      timeSeconds: 145,
      accuracy: 92,
      coinsEarned: 220,
      xpEarned: 95,
      opponentName: 'Opponent',
    }
  );
}

function WxoMark() {
  return (
    <View style={styles.markRow}>
      {(['W', 'X', 'O'] as const).map((letter) => (
        <View key={letter} style={[styles.markBox, letter === 'X' && styles.markBoxX]}>
          <Text style={styles.markLetter}>{letter}</Text>
        </View>
      ))}
    </View>
  );
}

function ResultShell({
  children,
  won,
}: {
  children: React.ReactNode;
  won: boolean;
}) {
  const insets = useSafeAreaInsets();
  return (
    <View style={[styles.root, { paddingTop: insets.top + spacing.lg, paddingBottom: insets.bottom + spacing.lg }]}>
      <StatusBar barStyle="dark-content" backgroundColor={wxo.bg} />
      <WxoMark />
      <Text style={[styles.banner, !won && styles.bannerLose]}>{won ? 'YOU WON' : 'DEFEATED'}</Text>
      {children}
    </View>
  );
}

function WxoButton({
  title,
  onPress,
  primary,
  outline,
}: {
  title: string;
  onPress: () => void;
  primary?: boolean;
  outline?: boolean;
}) {
  return (
    <Pressable
      onPress={onPress}
      style={({ pressed }) => [
        styles.btn,
        primary && styles.btnPrimary,
        outline && styles.btnOutline,
        pressed && { opacity: 0.88 },
      ]}
    >
      <Text style={[styles.btnTxt, primary && styles.btnTxtPrimary, outline && styles.btnTxtOutline]}>
        {title}
      </Text>
    </Pressable>
  );
}

/**
 * Native end-of-match screens (Unity only reports board end; prize / rank / WXO UI are RN).
 * Win rule (server): fastest finish → higher score → fewer moves → earlier submit.
 */
export function MatchResultScreen({ navigation, route }: Props) {
  const result = useResult(route);
  return (
    <ResultShell won={!!result.won}>
      <Text style={styles.subtitle}>vs {result.opponentName || 'Opponent'}</Text>
      <View style={styles.grid}>
        <Stat label="Score" value={String(result.score)} />
        <Stat label="Time" value={`${result.timeSeconds}s`} />
        <Stat label="Prize" value={result.coinsEarned > 0 ? `+${result.coinsEarned}` : '0'} />
        <Stat label="XP" value={`+${result.xpEarned}`} />
      </View>
      <WxoButton
        title={result.won ? 'View Victory' : 'View Defeat'}
        primary
        onPress={() => navigation.replace(result.won ? ROUTES.Victory : ROUTES.Defeat, { result })}
      />
      <WxoButton title="Home" outline onPress={() => goHome(navigation)} />
    </ResultShell>
  );
}

export function VictoryScreen({ navigation, route }: Props) {
  const result = useResult(route);
  return (
    <ResultShell won>
      <Text style={styles.title}>Victory</Text>
      <Text style={styles.subtitle}>
        Fastest clear wins the pot. Prize credited to your WXO wallet.
      </Text>
      <View style={styles.prizeCard}>
        <Text style={styles.prizeLabel}>WXO PRIZE</Text>
        <Text style={styles.prizeValue}>+{result.coinsEarned} WXO</Text>
      </View>
      <View style={styles.grid}>
        <Stat label="Score" value={String(result.score)} />
        <Stat label="Time" value={`${result.timeSeconds}s`} />
        <Stat label="XP" value={`+${result.xpEarned}`} />
        <Stat label="Rival" value={result.opponentName || '—'} />
      </View>
      <WxoButton title="Continue" primary onPress={() => goHome(navigation)} />
      <WxoButton
        title="Play Again"
        outline
        onPress={() => navigation.replace(ROUTES.MatchLobby, { gameId: 'iq-match', game: 'IQ Match' })}
      />
    </ResultShell>
  );
}

export function DefeatScreen({ navigation, route }: Props) {
  const result = useResult(route);
  return (
    <ResultShell won={false}>
      <Text style={styles.title}>Defeat</Text>
      <Text style={styles.subtitle}>
        Opponent finished faster or scored higher. Entry stays in the pot for the winner.
      </Text>
      <View style={[styles.prizeCard, styles.prizeCardLose]}>
        <Text style={[styles.prizeLabel, { color: wxo.muted }]}>PRIZE</Text>
        <Text style={[styles.prizeValue, { color: wxo.soft }]}>
          {result.coinsEarned > 0 ? `+${result.coinsEarned} WXO` : 'No prize'}
        </Text>
      </View>
      <View style={styles.grid}>
        <Stat label="Score" value={String(result.score)} />
        <Stat label="Time" value={`${result.timeSeconds}s`} />
        <Stat label="XP" value={`+${result.xpEarned}`} />
        <Stat label="Rival" value={result.opponentName || '—'} />
      </View>
      <WxoButton
        title="Rematch"
        primary
        onPress={() => navigation.replace(ROUTES.MatchLobby, { gameId: 'iq-match', game: 'IQ Match' })}
      />
      <WxoButton title="Home" outline onPress={() => goHome(navigation)} />
    </ResultShell>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <View style={styles.stat}>
      <Text style={styles.statLabel}>{label}</Text>
      <Text style={styles.statValue} numberOfLines={1}>
        {value}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: wxo.bg,
    paddingHorizontal: spacing.lg,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
  },
  markRow: { flexDirection: 'row', alignItems: 'center', gap: 6, marginBottom: spacing.sm },
  markBox: {
    width: 36,
    height: 36,
    borderRadius: 10,
    backgroundColor: wxo.red,
    alignItems: 'center',
    justifyContent: 'center',
  },
  markBoxX: { backgroundColor: wxo.redDark },
  markLetter: {
    color: '#fff',
    fontSize: 18,
    fontWeight: '900',
  },
  banner: {
    backgroundColor: wxo.red,
    color: '#fff',
    fontWeight: '900',
    letterSpacing: 2,
    paddingHorizontal: 18,
    paddingVertical: 8,
    borderRadius: 8,
    overflow: 'hidden',
    marginBottom: 4,
  },
  bannerLose: { backgroundColor: wxo.soft },
  title: { ...typography.hero, color: wxo.ink, textAlign: 'center' },
  subtitle: {
    ...typography.body,
    color: wxo.soft,
    textAlign: 'center',
    marginBottom: spacing.sm,
    paddingHorizontal: spacing.md,
  },
  prizeCard: {
    width: '100%',
    backgroundColor: wxo.redSoft,
    borderWidth: 1.5,
    borderColor: wxo.red,
    borderRadius: radius.lg,
    paddingVertical: spacing.md,
    alignItems: 'center',
    marginBottom: spacing.sm,
  },
  prizeCardLose: {
    backgroundColor: '#F4F5F8',
    borderColor: wxo.border,
  },
  prizeLabel: { ...typography.caption, color: wxo.red, fontWeight: '800', letterSpacing: 1.2 },
  prizeValue: { fontSize: 32, fontWeight: '900', color: wxo.red, marginTop: 4 },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.sm,
    width: '100%',
    marginVertical: spacing.sm,
  },
  stat: {
    width: '47%',
    backgroundColor: '#F4F5F8',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: wxo.border,
    padding: spacing.md,
  },
  statLabel: { ...typography.caption, color: wxo.muted },
  statValue: { color: wxo.ink, fontSize: 18, fontWeight: '800', marginTop: 4 },
  btn: {
    alignSelf: 'stretch',
    minHeight: 52,
    borderRadius: radius.lg,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: spacing.xs,
  },
  btnPrimary: { backgroundColor: wxo.red },
  btnOutline: {
    backgroundColor: wxo.bg,
    borderWidth: 1.5,
    borderColor: wxo.red,
  },
  btnTxt: { ...typography.button, color: wxo.ink },
  btnTxtPrimary: { color: '#FFFFFF' },
  btnTxtOutline: { color: wxo.red },
});
