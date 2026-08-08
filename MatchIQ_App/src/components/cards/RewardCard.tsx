import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { colors, radius, spacing, typography } from '../../theme';

type Props = {
  title: string;
  subtitle?: string;
  amountLabel: string;
};

export function RewardCard({ title, subtitle, amountLabel }: Props) {
  return (
    <LinearGradient colors={['#3A2A14', '#1E1810']} style={styles.card}>
      <View style={styles.chest}>
        <Text style={styles.chestIcon}>◆</Text>
      </View>
      <Text style={typography.h3}>{title}</Text>
      {subtitle ? <Text style={typography.caption}>{subtitle}</Text> : null}
      <Text style={styles.amount}>{amountLabel}</Text>
    </LinearGradient>
  );
}

const styles = StyleSheet.create({
  card: {
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.borderGold,
    padding: spacing.md,
    alignItems: 'center',
    gap: 6,
    minWidth: 140,
  },
  chest: {
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: colors.secondaryBlack,
    borderWidth: 2,
    borderColor: colors.primaryGold,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 4,
  },
  chestIcon: { color: colors.primaryGold, fontSize: 22 },
  amount: { color: colors.accentGreen, fontWeight: '700', fontSize: 16 },
});
