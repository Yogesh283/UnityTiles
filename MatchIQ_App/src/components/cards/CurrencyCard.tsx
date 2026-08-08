import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors, radius, spacing, typography } from '../../theme';
import { formatCoins } from '../../utils';

type Props = {
  label: string;
  amount: number;
  tone?: 'gold' | 'diamond' | 'energy';
};

const toneColor = {
  gold: colors.primaryGold,
  diamond: colors.neonPurple,
  energy: colors.blue,
};

export function CurrencyCard({ label, amount, tone = 'gold' }: Props) {
  return (
    <View style={[styles.card, { borderColor: toneColor[tone] }]}>
      <Text style={typography.caption}>{label}</Text>
      <Text style={[styles.amount, { color: toneColor[tone] }]}>{formatCoins(amount)}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    flex: 1,
    backgroundColor: colors.secondaryBlack,
    borderRadius: radius.md,
    borderWidth: 1,
    padding: spacing.sm,
    minWidth: 100,
  },
  amount: { fontSize: 18, fontWeight: '700', marginTop: 4 },
});
