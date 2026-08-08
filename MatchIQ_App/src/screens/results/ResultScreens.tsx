import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Screen, PrimaryButton, SecondaryButton, RewardCard } from '../../components';
import { colors, spacing, typography } from '../../theme';
import { ROUTES } from '../../constants';
import type { MatchResultPayload } from '../../types';
import { useUiStore } from '../../store';
import { goHome, goTo } from '../../navigation/nav';

type Props = NativeStackScreenProps<any>;

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
      opponentName: 'TempleFox',
    }
  );
}

export function MatchResultScreen({ navigation, route }: Props) {
  const result = useResult(route);
  return (
    <Screen>
      <Text style={[typography.hero, { marginTop: 40 }]}>Match Report</Text>
      <Text style={typography.body}>vs {result.opponentName || 'Opponent'}</Text>
      <View style={styles.grid}>
        <Stat label="Score" value={String(result.score)} />
        <Stat label="Time" value={`${result.timeSeconds}s`} />
        <Stat label="Accuracy" value={`${result.accuracy}%`} />
        <Stat label="Coins" value={`+${result.coinsEarned}`} />
      </View>
      <PrimaryButton
        title={result.won ? 'View Victory' : 'View Defeat'}
        onPress={() => navigation.replace(result.won ? ROUTES.Victory : ROUTES.Defeat, { result })}
      />
      <SecondaryButton title="Home" onPress={() => goHome(navigation)} style={{ marginTop: spacing.sm }} />
    </Screen>
  );
}

export function VictoryScreen({ navigation, route }: Props) {
  const result = useResult(route);
  return (
    <Screen>
      <View style={styles.center}>
        <Text style={styles.banner}>EXCELLENT</Text>
        <Text style={typography.hero}>Victory</Text>
        <Text style={typography.body}>You outplayed {result.opponentName || 'your rival'}.</Text>
        <View style={styles.grid}>
          <Stat label="Score" value={String(result.score)} />
          <Stat label="Time" value={`${result.timeSeconds}s`} />
          <Stat label="XP" value={`+${result.xpEarned}`} />
          <Stat label="Coins" value={`+${result.coinsEarned}`} />
        </View>
        <RewardCard title="Temple Chest" subtitle="Victory bounty" amountLabel={`+${result.coinsEarned}`} />
        <PrimaryButton title="Continue" onPress={() => goHome(navigation)} style={{ marginTop: spacing.lg, alignSelf: 'stretch' }} />
        <SecondaryButton
          title="Play Again"
          onPress={() => goHome(navigation)}
          style={{ marginTop: spacing.sm, alignSelf: 'stretch' }}
        />
      </View>
    </Screen>
  );
}

export function DefeatScreen({ navigation, route }: Props) {
  const result = useResult(route);
  return (
    <Screen>
      <View style={styles.center}>
        <Text style={[styles.banner, { backgroundColor: colors.danger }]}>DEFEATED</Text>
        <Text style={typography.hero}>Defeat</Text>
        <Text style={typography.body}>The lantern fades — train and return stronger.</Text>
        <View style={styles.grid}>
          <Stat label="Score" value={String(result.score)} />
          <Stat label="Time" value={`${result.timeSeconds}s`} />
          <Stat label="XP" value={`+${result.xpEarned}`} />
          <Stat label="Coins" value={`+${result.coinsEarned}`} />
        </View>
        <PrimaryButton
          title="Rematch"
          onPress={() => goHome(navigation)}
          style={{ alignSelf: 'stretch' }}
        />
        <SecondaryButton title="Home" onPress={() => goHome(navigation)} style={{ marginTop: spacing.sm, alignSelf: 'stretch' }} />
      </View>
    </Screen>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <View style={styles.stat}>
      <Text style={typography.caption}>{label}</Text>
      <Text style={styles.statValue}>{value}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  center: { flex: 1, justifyContent: 'center', alignItems: 'center', gap: spacing.md },
  banner: {
    backgroundColor: colors.primaryGold,
    color: colors.secondaryBlack,
    fontWeight: '800',
    paddingHorizontal: 16,
    paddingVertical: 6,
    borderRadius: 8,
    overflow: 'hidden',
    letterSpacing: 2,
  },
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm, width: '100%', marginVertical: spacing.md },
  stat: {
    width: '47%',
    backgroundColor: colors.surface,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.borderGold,
    padding: spacing.md,
  },
  statValue: { color: colors.goldLight, fontSize: 20, fontWeight: '700', marginTop: 4 },
});
