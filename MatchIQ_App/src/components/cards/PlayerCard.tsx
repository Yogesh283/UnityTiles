import React from 'react';
import { StyleSheet, Text, View, Pressable } from 'react-native';
import { colors, radius, spacing, typography } from '../../theme';

type Props = {
  name: string;
  subtitle?: string;
  level?: number;
  online?: boolean;
  onPress?: () => void;
  right?: React.ReactNode;
};

export function PlayerCard({ name, subtitle, level, online, onPress, right }: Props) {
  return (
    <Pressable onPress={onPress} style={styles.card}>
      <View style={styles.avatar}>
        <Text style={styles.avatarText}>{name.slice(0, 1).toUpperCase()}</Text>
        {online != null ? (
          <View style={[styles.dot, { backgroundColor: online ? colors.accentGreen : colors.textMuted }]} />
        ) : null}
      </View>
      <View style={{ flex: 1 }}>
        <Text style={typography.bodyStrong}>{name}</Text>
        <Text style={typography.caption}>
          {subtitle || (level != null ? `Level ${level}` : 'Challenger')}
        </Text>
      </View>
      {right}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    backgroundColor: colors.surface,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.sm,
  },
  avatar: {
    width: 46,
    height: 46,
    borderRadius: 23,
    backgroundColor: colors.secondaryBlack,
    borderWidth: 2,
    borderColor: colors.primaryGold,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarText: { color: colors.goldLight, fontWeight: '700', fontSize: 18 },
  dot: {
    position: 'absolute',
    right: -1,
    bottom: -1,
    width: 12,
    height: 12,
    borderRadius: 6,
    borderWidth: 2,
    borderColor: colors.surface,
  },
});
