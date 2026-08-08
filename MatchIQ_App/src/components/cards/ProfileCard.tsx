import React from 'react';
import { StyleSheet, Text, View, Pressable } from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import type { PlayerProfile } from '../../types';
import { colors, radius, spacing, typography } from '../../theme';
import { XPBar } from '../feedback/XPBar';

type Props = {
  profile: PlayerProfile;
  onPress?: () => void;
};

export function ProfileCard({ profile, onPress }: Props) {
  return (
    <Pressable onPress={onPress}>
      <LinearGradient colors={['#3A2E1A', '#1A1510']} style={styles.card}>
        <View style={styles.row}>
          <View style={styles.avatar}>
            <Text style={styles.avatarText}>{profile.displayName.slice(0, 1)}</Text>
          </View>
          <View style={{ flex: 1 }}>
            <Text style={typography.h2}>{profile.displayName}</Text>
            <Text style={typography.caption}>
              Lv {profile.level} · {profile.rank}
            </Text>
            <Text style={[typography.caption, { color: colors.primaryGold }]}>
              {profile.clanName || 'No Clan'}
            </Text>
          </View>
        </View>
        <XPBar xp={profile.xp} xpToNext={profile.xpToNext} />
        <View style={styles.stats}>
          <Stat label="Wins" value={String(profile.wins)} />
          <Stat label="Losses" value={String(profile.losses)} />
          <Stat label="Win %" value={`${profile.winRate}%`} />
        </View>
      </LinearGradient>
    </Pressable>
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
  card: {
    borderRadius: radius.xl,
    borderWidth: 1,
    borderColor: colors.borderGold,
    padding: spacing.md,
    gap: spacing.md,
  },
  row: { flexDirection: 'row', gap: spacing.md, alignItems: 'center' },
  avatar: {
    width: 72,
    height: 72,
    borderRadius: 36,
    borderWidth: 3,
    borderColor: colors.primaryGold,
    backgroundColor: colors.secondaryBlack,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarText: { color: colors.goldLight, fontSize: 28, fontWeight: '700' },
  stats: { flexDirection: 'row', gap: spacing.sm },
  stat: {
    flex: 1,
    backgroundColor: colors.secondaryBlack,
    borderRadius: radius.md,
    padding: spacing.sm,
    alignItems: 'center',
  },
  statValue: { color: colors.textPrimary, fontWeight: '700', marginTop: 2 },
});
