import React, { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  Screen, GameHeader, PrimaryButton, GameCard, RewardCard, ProgressBar, Loader,
} from '../../components';
import { colors, spacing, typography } from '../../theme';
import { useAchievements, useBattlePass } from '../../hooks';
import { useUiStore, usePlayerStore } from '../../store';

type Props = NativeStackScreenProps<any>;

export function LuckySpinScreen() {
  const [spinning, setSpinning] = useState(false);
  const [result, setResult] = useState<string | null>(null);
  const showToast = useUiStore(s => s.showToast);
  const setBalances = usePlayerStore(s => s.setBalances);
  const balances = usePlayerStore(s => s.balances);
  const prizes = ['50 Coins', '100 Coins', '1 Diamond', 'Energy +2', 'Hint Boost', 'Jackpot 500'];

  return (
    <Screen scroll>
      <GameHeader title="Lucky Spin" showBack compact />
      <View style={styles.wheel}>
        <Text style={styles.wheelText}>{result || 'SPIN'}</Text>
      </View>
      <Text style={[typography.body, { textAlign: 'center', marginBottom: spacing.md }]}>
        Lantern wheel · 1 energy per spin
      </Text>
      <PrimaryButton title={spinning ? 'Spinning…' : 'Spin'} loading={spinning} onPress={() => {
        if (balances.energy < 1) { showToast('Not enough energy', 'danger'); return; }
        setSpinning(true);
        setBalances({ energy: balances.energy - 1 });
        setTimeout(() => {
          const prize = prizes[Math.floor(Math.random() * prizes.length)];
          setResult(prize);
          setSpinning(false);
          if (prize.includes('Coins')) setBalances({ coins: balances.coins + parseInt(prize, 10) || 50 });
          showToast(prize, 'success');
        }, 1400);
      }} />
    </Screen>
  );
}

export function AchievementsScreen() {
  const { data, isLoading } = useAchievements();
  return (
    <Screen scroll>
      <GameHeader title="Achievements" showBack compact />
      {isLoading ? <Loader /> : null}
      {data?.map(a => (
        <View key={a.id} style={styles.panel}>
          <Text style={typography.h3}>{a.title}</Text>
          <Text style={typography.caption}>{a.description}</Text>
          <ProgressBar progress={a.progress / a.target} />
          <Text style={typography.caption}>{a.progress}/{a.target} {a.unlocked ? '· Unlocked' : ''}</Text>
        </View>
      ))}
    </Screen>
  );
}

export function BattlePassScreen() {
  const { data, isLoading } = useBattlePass();
  const showToast = useUiStore(s => s.showToast);
  return (
    <Screen scroll>
      <GameHeader title="Battle Pass" showBack compact />
      <Text style={typography.body}>Season of the Maple Gate</Text>
      {isLoading ? <Loader /> : null}
      {data?.map(tier => (
        <View key={tier.level} style={[styles.panel, tier.locked && { opacity: 0.5 }]}>
          <Text style={typography.bodyStrong}>Tier {tier.level}</Text>
          <Text style={typography.caption}>Free: {tier.freeReward}</Text>
          <Text style={[typography.caption, { color: colors.primaryGold }]}>Premium: {tier.premiumReward}</Text>
          <PrimaryButton
            title={tier.claimed ? 'Claimed' : tier.locked ? 'Locked' : 'Claim'}
            disabled={tier.claimed || tier.locked}
            onPress={() => showToast('Tier reward claimed', 'success')}
            style={{ marginTop: 8, minHeight: 44 }}
          />
        </View>
      ))}
    </Screen>
  );
}

export function SeasonRewardsScreen() {
  return (
    <Screen scroll>
      <GameHeader title="Season Rewards" showBack compact />
      <RewardCard title="Jade Crown" subtitle="Top 100 finish" amountLabel="Exclusive" />
      <View style={{ height: spacing.md }} />
      <GameCard title="Season XP Track" subtitle="Earn XP in tournaments to unlock frames" badge="LIVE" />
      <GameCard title="Maple Title" subtitle="Reach Jade Master" badge="RANK" style={{ marginTop: spacing.sm }} />
    </Screen>
  );
}

export function RankRewardsScreen() {
  const ranks = [
    { name: 'Bronze Disciple', reward: '50 Coins' },
    { name: 'Silver Adept', reward: '120 Coins' },
    { name: 'Gold Ronin', reward: 'Jade Shard' },
    { name: 'Jade Master', reward: 'Gold Frame' },
    { name: 'Obsidian Legend', reward: 'Legendary Title' },
  ];
  return (
    <Screen scroll>
      <GameHeader title="Rank Rewards" showBack compact />
      {ranks.map(r => (
        <GameCard key={r.name} title={r.name} subtitle={r.reward} badge="RANK" style={{ marginBottom: spacing.sm }} />
      ))}
    </Screen>
  );
}

const styles = StyleSheet.create({
  wheel: {
    alignSelf: 'center', width: 220, height: 220, borderRadius: 110,
    borderWidth: 4, borderColor: colors.primaryGold, backgroundColor: colors.surface,
    alignItems: 'center', justifyContent: 'center', marginVertical: spacing.xl,
  },
  wheelText: { color: colors.goldLight, fontSize: 28, fontWeight: '800', textAlign: 'center', paddingHorizontal: 12 },
  panel: { backgroundColor: colors.surface, borderRadius: 14, borderWidth: 1, borderColor: colors.borderGold, padding: spacing.md, marginBottom: spacing.sm, gap: 6 },
});
