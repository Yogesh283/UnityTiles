import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import type { LeaderboardEntry } from '../../types';
import { colors, radius, spacing, typography } from '../../theme';
import { formatCoins } from '../../utils';

type Props = { entry: LeaderboardEntry };

export function LeaderboardItem({ entry }: Props) {
  const medal =
    entry.rank === 1 ? '#FFD700' : entry.rank === 2 ? '#C0C0C0' : entry.rank === 3 ? '#CD7F32' : colors.border;

  return (
    <View style={[styles.row, entry.isCurrentUser && styles.current]}>
      <View style={[styles.rank, { borderColor: medal }]}>
        <Text style={styles.rankText}>{entry.rank}</Text>
      </View>
      <View style={styles.avatar}>
        <Text style={styles.avatarText}>{entry.name.slice(0, 1)}</Text>
      </View>
      <Text style={[typography.bodyStrong, { flex: 1 }]}>{entry.name}</Text>
      <Text style={styles.score}>{formatCoins(entry.score)}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.sm,
    marginBottom: spacing.sm,
  },
  current: { borderColor: colors.primaryGold, backgroundColor: '#2A2214' },
  rank: {
    width: 32,
    height: 32,
    borderRadius: 16,
    borderWidth: 2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  rankText: { color: colors.textPrimary, fontWeight: '700' },
  avatar: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: colors.secondaryBlack,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarText: { color: colors.goldLight, fontWeight: '700' },
  score: { color: colors.primaryGold, fontWeight: '700' },
});
