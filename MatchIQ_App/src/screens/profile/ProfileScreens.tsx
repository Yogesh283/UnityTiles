import React, { useState } from 'react';
import { StyleSheet, Text, View, Pressable } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  Screen, GameHeader, ProfileCard, PrimaryButton, SecondaryButton, PremiumInput, GameCard, Loader,
} from '../../components';
import { colors, spacing, typography } from '../../theme';
import { ROUTES } from '../../constants';
import { usePlayerStore, useUiStore } from '../../store';
import { useMatchHistory } from '../../hooks';
import { formatTimeAgo } from '../../utils';

type Props = NativeStackScreenProps<any>;

export function ProfileScreen({ navigation }: Props) {
  const profile = usePlayerStore(s => s.profile);
  return (
    <Screen scroll>
      <GameHeader title="Profile" />
      <ProfileCard profile={profile} onPress={() => navigation.navigate(ROUTES.EditProfile)} />
      <View style={styles.links}>
        {[
          ['Edit Profile', ROUTES.EditProfile],
          ['Avatar', ROUTES.AvatarSelection],
          ['Frame', ROUTES.FrameSelection],
          ['Statistics', ROUTES.Statistics],
          ['Match History', ROUTES.MatchHistory],
          ['Achievements', ROUTES.Achievements],
          ['Referral', ROUTES.Referral],
        ].map(([label, route]) => (
          <Pressable key={route} style={styles.link} onPress={() => navigation.navigate(route)}>
            <Text style={typography.bodyStrong}>{label}</Text>
            <Text style={{ color: colors.primaryGold }}>›</Text>
          </Pressable>
        ))}
      </View>
    </Screen>
  );
}

export function EditProfileScreen({ navigation }: Props) {
  const profile = usePlayerStore(s => s.profile);
  const setProfile = usePlayerStore(s => s.setProfile);
  const showToast = useUiStore(s => s.showToast);
  const [name, setName] = useState(profile.displayName);
  return (
    <Screen scroll>
      <GameHeader title="Edit Profile" showBack compact />
      <PremiumInput label="Display Name" value={name} onChangeText={setName} />
      <PrimaryButton title="Save" onPress={() => { setProfile({ displayName: name }); showToast('Profile updated', 'success'); navigation.goBack(); }} />
      <SecondaryButton title="Change Avatar" onPress={() => navigation.navigate(ROUTES.AvatarSelection)} style={{ marginTop: spacing.sm }} />
    </Screen>
  );
}

export function AvatarSelectionScreen({ navigation }: Props) {
  const setProfile = usePlayerStore(s => s.setProfile);
  const showToast = useUiStore(s => s.showToast);
  const avatars = ['Sage', 'Prince', 'Princess', 'Panda', 'Geisha', 'Turtle', 'Fox', 'Monk'];
  return (
    <Screen scroll>
      <GameHeader title="Avatar Selection" showBack compact />
      <View style={styles.grid}>
        {avatars.map(a => (
          <Pressable key={a} style={styles.avatar} onPress={() => { setProfile({ avatarId: 'avatar_' + a.toLowerCase() }); showToast(a + ' selected', 'success'); navigation.goBack(); }}>
            <Text style={styles.avatarLetter}>{a[0]}</Text>
            <Text style={typography.caption}>{a}</Text>
          </Pressable>
        ))}
      </View>
    </Screen>
  );
}

export function FrameSelectionScreen({ navigation }: Props) {
  const setProfile = usePlayerStore(s => s.setProfile);
  const showToast = useUiStore(s => s.showToast);
  const frames = ['Gold', 'Jade', 'Lantern', 'Obsidian', 'Maple', 'Royal'];
  return (
    <Screen scroll>
      <GameHeader title="Frame Selection" showBack compact />
      {frames.map(f => (
        <GameCard key={f} title={f + ' Frame'} subtitle="Premium profile border" badge="EQUIP"
          onPress={() => { setProfile({ frameId: 'frame_' + f.toLowerCase() }); showToast(f + ' frame equipped', 'success'); navigation.goBack(); }}
          style={{ marginBottom: spacing.sm }} />
      ))}
    </Screen>
  );
}

export function StatisticsScreen() {
  const profile = usePlayerStore(s => s.profile);
  return (
    <Screen scroll>
      <GameHeader title="Statistics" showBack compact />
      <View style={styles.stats}>
        {[
          ['Wins', profile.wins],
          ['Losses', profile.losses],
          ['Win Rate', profile.winRate + '%'],
          ['Level', profile.level],
          ['Rank', profile.rank],
          ['XP', profile.xp],
        ].map(([k, v]) => (
          <View key={String(k)} style={styles.stat}>
            <Text style={typography.caption}>{k}</Text>
            <Text style={styles.statValue}>{v}</Text>
          </View>
        ))}
      </View>
    </Screen>
  );
}

export function MatchHistoryScreen() {
  const { data, isLoading } = useMatchHistory();
  return (
    <Screen scroll>
      <GameHeader title="Match History" showBack compact />
      {isLoading ? <Loader /> : null}
      {data?.map(m => (
        <View key={m.id} style={styles.panel}>
          <Text style={typography.bodyStrong}>{m.won ? 'Victory' : 'Defeat'} vs {m.opponent}</Text>
          <Text style={typography.caption}>{m.mode} · Score {m.score} · {formatTimeAgo(m.playedAt)}</Text>
        </View>
      ))}
    </Screen>
  );
}

const styles = StyleSheet.create({
  links: { marginTop: spacing.lg, gap: 4 },
  link: { flexDirection: 'row', justifyContent: 'space-between', paddingVertical: 14, borderBottomWidth: 1, borderBottomColor: colors.border },
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  avatar: { width: '22%', aspectRatio: 1, backgroundColor: colors.surface, borderRadius: 16, borderWidth: 1, borderColor: colors.borderGold, alignItems: 'center', justifyContent: 'center', gap: 4 },
  avatarLetter: { color: colors.goldLight, fontSize: 22, fontWeight: '700' },
  stats: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  stat: { width: '47%', backgroundColor: colors.surface, borderRadius: 12, borderWidth: 1, borderColor: colors.borderGold, padding: spacing.md },
  statValue: { color: colors.goldLight, fontSize: 18, fontWeight: '700', marginTop: 4 },
  panel: { backgroundColor: colors.surface, borderRadius: 12, borderWidth: 1, borderColor: colors.border, padding: spacing.md, marginBottom: spacing.sm },
});
