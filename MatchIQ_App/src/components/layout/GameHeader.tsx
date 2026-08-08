import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { usePlayerStore } from '../../store';
import { colors, radius, spacing, typography } from '../../theme';
import { formatCoins } from '../../utils';
import { NotificationBadge } from '../feedback/NotificationBadge';
import { XPBar } from '../feedback/XPBar';
import { Logo } from '../brand/Logo';
import { ROUTES } from '../../constants';
import { goTo } from '../../navigation/nav';

type Props = {
  title?: string;
  showBack?: boolean;
  compact?: boolean;
};

export function GameHeader({ title, showBack, compact }: Props) {
  const navigation = useNavigation<any>();
  const profile = usePlayerStore((s) => s.profile);
  const balances = usePlayerStore((s) => s.balances);
  const unread = usePlayerStore((s) => s.unreadNotifications);

  return (
    <View style={styles.wrap}>
      <View style={styles.topRow}>
        <View style={styles.left}>
          {showBack ? (
            <Pressable onPress={() => navigation.goBack()} style={styles.backBtn}>
              <Text style={styles.backText}>‹</Text>
            </Pressable>
          ) : (
            <Pressable onPress={() => goTo(navigation, ROUTES.Profile)} style={styles.avatar}>
              <Text style={styles.avatarText}>{profile.displayName.slice(0, 1)}</Text>
            </Pressable>
          )}
          <View style={{ flex: 1 }}>
            <Text style={typography.bodyStrong} numberOfLines={1}>
              {title || profile.displayName}
            </Text>
            {!compact ? (
              <Text style={typography.caption}>
                Lv {profile.level} · {profile.rank}
              </Text>
            ) : null}
          </View>
        </View>
        <View style={styles.actions}>
          <Pressable onPress={() => goTo(navigation, ROUTES.Notifications)} style={styles.iconBtn}>
            <Text style={styles.icon}>🔔</Text>
            <NotificationBadge count={unread} />
          </Pressable>
          <Pressable onPress={() => goTo(navigation, ROUTES.Settings)} style={styles.iconBtn}>
            <Text style={styles.icon}>⚙</Text>
          </Pressable>
          <Logo size={30} glow={false} />
        </View>
      </View>

      {!compact ? (
        <>
          <XPBar xp={profile.xp} xpToNext={profile.xpToNext} />
          <View style={styles.currencyRow}>
            <CurrencyChip label="Wallet" value={formatCoins(balances.coins)} color={colors.primaryGold} onPress={() => goTo(navigation, ROUTES.Wallet)} />
            <CurrencyChip label="Bonus" value={formatCoins(balances.diamonds)} color={colors.neonPurple} onPress={() => goTo(navigation, ROUTES.Store)} />
            <CurrencyChip label="Energy" value={String(balances.energy)} color={colors.blue} onPress={() => goTo(navigation, ROUTES.Store)} />
          </View>
        </>
      ) : null}
    </View>
  );
}

function CurrencyChip({
  label,
  value,
  color,
  onPress,
}: {
  label: string;
  value: string;
  color: string;
  onPress: () => void;
}) {
  return (
    <Pressable onPress={onPress} style={[styles.chip, { borderColor: color }]}>
      <Text style={[styles.chipLabel, { color }]}>{label}</Text>
      <Text style={styles.chipValue}>{value}</Text>
    </Pressable>
  );
}

/** Alias used by plan naming */
export const Header = GameHeader;

const styles = StyleSheet.create({
  wrap: { gap: spacing.sm, marginBottom: spacing.md },
  topRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  left: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, flex: 1 },
  avatar: {
    width: 44,
    height: 44,
    borderRadius: 22,
    borderWidth: 2,
    borderColor: colors.neonPurple,
    backgroundColor: colors.surface,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarText: { color: colors.goldLight, fontWeight: '700', fontSize: 18 },
  backBtn: {
    width: 40,
    height: 40,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: colors.borderPurple,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.surface,
  },
  backText: { color: colors.goldLight, fontSize: 28, marginTop: -2 },
  actions: { flexDirection: 'row', gap: spacing.xs },
  iconBtn: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: 'center',
    justifyContent: 'center',
  },
  icon: { fontSize: 16 },
  currencyRow: { flexDirection: 'row', gap: spacing.xs },
  chip: {
    flex: 1,
    borderRadius: radius.md,
    borderWidth: 1,
    backgroundColor: colors.secondaryBlack,
    paddingVertical: 8,
    paddingHorizontal: 8,
  },
  chipLabel: { fontSize: 10, fontWeight: '600' },
  chipValue: { color: colors.textPrimary, fontWeight: '700', marginTop: 2 },
});
