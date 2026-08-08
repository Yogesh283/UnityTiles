import React from 'react';
import { Pressable, StyleSheet, Text, View, Animated } from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import type { Tournament } from '../../types';
import { colors, radius, spacing, typography } from '../../theme';
import { formatRupee, formatCountdown, statusLabel } from '../../utils';
import { PrimaryButton } from '../buttons/PrimaryButton';

type Props = {
  tournament: Tournament;
  onPress?: () => void;
  onJoin?: () => void;
};

export function TournamentCard({ tournament, onPress, onJoin }: Props) {
  const scale = React.useRef(new Animated.Value(1)).current;
  const fill = tournament.maxPlayers > 0 ? tournament.players / tournament.maxPlayers : 0;

  return (
    <Animated.View style={[{ transform: [{ scale }] }, styles.wrap]}>
      <Pressable
        onPress={onPress}
        onPressIn={() => Animated.spring(scale, { toValue: 0.985, useNativeDriver: true }).start()}
        onPressOut={() => Animated.spring(scale, { toValue: 1, useNativeDriver: true }).start()}
      >
        <LinearGradient colors={['#1A1E38', '#0E1122']} style={styles.inner}>
          <View style={styles.top}>
            <LinearGradient colors={[colors.purple, colors.blue]} style={styles.icon}>
              <Text style={styles.iconText}>{tournament.icon}</Text>
            </LinearGradient>
            <View style={{ flex: 1 }}>
              <Text style={typography.bodyStrong}>{tournament.name}</Text>
              <Text style={typography.caption}>
                {tournament.players}/{tournament.maxPlayers} joined · {tournament.winners} winner
                {tournament.winners > 1 ? 's' : ''}
              </Text>
              <View style={styles.status}>
                <Text style={styles.statusText}>{statusLabel(tournament.status)}</Text>
              </View>
            </View>
            <View style={styles.timer}>
              <Text style={styles.timerText}>{formatCountdown(tournament.startsInMinutes)}</Text>
            </View>
          </View>

          <View style={styles.track}>
            <View style={[styles.fill, { width: `${Math.min(100, fill * 100)}%` }]} />
          </View>

          <View style={styles.stats}>
            <Stat label="Entry" value={formatRupee(tournament.entryFee)} />
            <Stat label="Prize" value={formatRupee(tournament.prizePool)} highlight />
            <Stat label="Slots" value={String(tournament.maxPlayers - tournament.players)} />
          </View>

          {onJoin ? <PrimaryButton title="PLAY" onPress={onJoin} style={{ marginTop: spacing.sm }} /> : null}
        </LinearGradient>
      </Pressable>
    </Animated.View>
  );
}

function Stat({ label, value, highlight }: { label: string; value: string; highlight?: boolean }) {
  return (
    <View style={styles.stat}>
      <Text style={typography.caption}>{label}</Text>
      <Text style={[styles.statValue, highlight && { color: colors.primaryGold }]}>{value}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    borderRadius: radius.xl,
    marginBottom: spacing.md,
    shadowColor: colors.purple,
    shadowOpacity: 0.25,
    shadowRadius: 16,
    shadowOffset: { width: 0, height: 8 },
    elevation: 8,
  },
  inner: {
    borderRadius: radius.xl,
    borderWidth: 1,
    borderColor: colors.borderPurple,
    padding: spacing.md,
    gap: spacing.sm,
  },
  top: { flexDirection: 'row', gap: spacing.sm, alignItems: 'center' },
  icon: {
    width: 56,
    height: 56,
    borderRadius: 14,
    alignItems: 'center',
    justifyContent: 'center',
  },
  iconText: { color: colors.white, fontWeight: '900', fontSize: 13 },
  status: {
    alignSelf: 'flex-start',
    marginTop: 6,
    backgroundColor: colors.purple,
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: radius.pill,
  },
  statusText: { color: colors.white, fontSize: 10, fontWeight: '700' },
  timer: {
    backgroundColor: 'rgba(245,183,0,0.12)',
    borderWidth: 1,
    borderColor: 'rgba(245,183,0,0.4)',
    borderRadius: 20,
    paddingHorizontal: 10,
    paddingVertical: 6,
  },
  timerText: { color: colors.primaryGold, fontWeight: '800', fontSize: 12 },
  track: {
    height: 6,
    borderRadius: 8,
    backgroundColor: colors.border,
    overflow: 'hidden',
  },
  fill: { height: '100%', backgroundColor: colors.neonPurple },
  stats: { flexDirection: 'row', gap: spacing.sm },
  stat: { flex: 1 },
  statValue: { color: colors.white, fontWeight: '800', fontSize: 15, marginTop: 2 },
});
