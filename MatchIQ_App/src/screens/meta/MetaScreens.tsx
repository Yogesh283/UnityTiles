import React from 'react';
import { StyleSheet, Text, View, Pressable } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  Screen, GameHeader, PrimaryButton, RewardCard, GameCard, PlayerCard, Loader, LeaderboardItem, ProgressBar,
} from '../../components';
import { colors, spacing, typography } from '../../theme';
import { ROUTES } from '../../constants';
import { useDailyReward, useMissions, useLeaderboard, useFriends, useClan } from '../../hooks';
import { useUiStore } from '../../store';
import { goTo } from '../../navigation/nav';

type Props = NativeStackScreenProps<any>;

export function DailyRewardScreen({ navigation }: Props) {
  const { data, isLoading } = useDailyReward();
  const showToast = useUiStore(s => s.showToast);
  if (isLoading || !data) return <Loader fullScreen />;
  return (
    <Screen scroll>
      <GameHeader title="Daily Reward" showBack compact />
      <Text style={typography.body}>Day {data.day} of the lantern calendar</Text>
      <View style={styles.grid}>
        {data.rewards.map(r => (
          <View key={r.day} style={[styles.day, r.claimed && styles.claimed, r.day === data.day && styles.today]}>
            <Text style={styles.dayNum}>D{r.day}</Text>
            <Text style={styles.dayLabel}>{r.label}</Text>
          </View>
        ))}
      </View>
      <PrimaryButton title="Claim Today" onPress={() => showToast('Daily reward claimed', 'success')} />
      <RewardCard title="7-Day Jackpot" subtitle="Gold Frame" amountLabel="Day 7" />
    </Screen>
  );
}

export function MissionsScreen() {
  const { data, isLoading } = useMissions();
  const showToast = useUiStore(s => s.showToast);
  return (
    <Screen scroll>
      <GameHeader title="Missions" showBack compact />
      {isLoading ? <Loader /> : null}
      {data?.map(m => (
        <View key={m.id} style={styles.panel}>
          <Text style={typography.h3}>{m.title}</Text>
          <Text style={typography.caption}>{m.description}</Text>
          <ProgressBar progress={m.progress / m.target} />
          <Text style={typography.caption}>{m.progress}/{m.target} · {m.rewardCoins} coins</Text>
          <PrimaryButton
            title={m.claimed ? 'Claimed' : m.completed ? 'Claim' : 'In Progress'}
            disabled={!m.completed || m.claimed}
            onPress={() => showToast('Mission reward claimed', 'success')}
            style={{ marginTop: spacing.sm }}
          />
        </View>
      ))}
    </Screen>
  );
}

export function LeaderboardScreen() {
  const { data, isLoading } = useLeaderboard();
  return (
    <Screen scroll>
      <GameHeader title="Leaderboard" showBack compact />
      {isLoading ? <Loader /> : null}
      {data?.map(e => <LeaderboardItem key={e.playerId} entry={e} />)}
    </Screen>
  );
}

export function FriendsScreen({ navigation }: Props) {
  const { data, isLoading } = useFriends();
  return (
    <Screen scroll>
      <GameHeader title="Friends" showBack compact />
      {isLoading ? <Loader /> : null}
      {data?.map(f => (
        <PlayerCard key={f.id} name={f.name} level={f.level} online={f.online}
          right={<PrimaryButton title="Invite" onPress={() => navigation.navigate(ROUTES.InviteFriends)} style={{ minWidth: 90, minHeight: 40 }} />}
        />
      ))}
      <PrimaryButton title="Invite Friends" onPress={() => navigation.navigate(ROUTES.InviteFriends)} />
    </Screen>
  );
}

export function ClanScreen({ navigation }: Props) {
  const { data, isLoading } = useClan();
  if (isLoading || !data) return <Loader fullScreen />;
  return (
    <Screen scroll>
      <GameHeader title="Clan" showBack compact />
      <View style={styles.panel}>
        <Text style={typography.h1}>{data.name}</Text>
        <Text style={typography.label}>[{data.tag}] · {data.trophies} trophies</Text>
        <Text style={typography.body}>{data.description}</Text>
        <Text style={typography.caption}>{data.members}/{data.maxMembers} members</Text>
      </View>
      <GameCard title="Clan War" subtitle="Starts at lantern hour" badge="SOON" onPress={() => goTo(navigation, ROUTES.Events)} />
      <PrimaryButton title="Clan Chat / Mail" onPress={() => navigation.navigate(ROUTES.Mail)} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginVertical: spacing.md },
  day: { width: '30%', backgroundColor: colors.surface, borderRadius: 12, borderWidth: 1, borderColor: colors.border, padding: 12, alignItems: 'center' },
  claimed: { opacity: 0.5 },
  today: { borderColor: colors.primaryGold, backgroundColor: '#2A2214' },
  dayNum: { color: colors.goldLight, fontWeight: '700' },
  dayLabel: { color: colors.textSecondary, fontSize: 11, marginTop: 4, textAlign: 'center' },
  panel: { backgroundColor: colors.surface, borderRadius: 16, borderWidth: 1, borderColor: colors.borderGold, padding: spacing.md, marginBottom: spacing.sm, gap: 8 },
});
