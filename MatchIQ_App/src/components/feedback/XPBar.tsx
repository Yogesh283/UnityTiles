import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { ProgressBar } from './ProgressBar';
import { colors, spacing, typography } from '../../theme';

type Props = { xp: number; xpToNext: number };

export function XPBar({ xp, xpToNext }: Props) {
  const progress = xpToNext > 0 ? xp / xpToNext : 0;
  return (
    <View style={styles.wrap}>
      <View style={styles.row}>
        <Text style={typography.caption}>XP</Text>
        <Text style={styles.value}>
          {xp} / {xpToNext}
        </Text>
      </View>
      <ProgressBar progress={progress} />
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { gap: spacing.xs },
  row: { flexDirection: 'row', justifyContent: 'space-between' },
  value: { color: colors.goldLight, fontSize: 12, fontWeight: '600' },
});
