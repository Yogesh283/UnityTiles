/**
 * Generates remaining MatchIQ screens with premium dark-fantasy UI.
 * Run once during project setup.
 */
const fs = require('fs');
const path = require('path');

const root = path.join(__dirname, '..', 'src', 'screens');

function write(rel, content) {
  const full = path.join(root, rel);
  fs.mkdirSync(path.dirname(full), { recursive: true });
  fs.writeFileSync(full, content, 'utf8');
  console.log('wrote', rel);
}

const screenTemplate = (exportName, title, body, extras = '') => `import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  Screen,
  GameHeader,
  PrimaryButton,
  SecondaryButton,
  GameCard,
  Loader,
} from '../../components';
import { colors, spacing, typography } from '../../theme';
import { ROUTES } from '../../constants';
${extras}

type Props = NativeStackScreenProps<any>;

export function ${exportName}({ navigation }: Props) {
  return (
    <Screen scroll>
      <GameHeader title="${title}" showBack compact />
      <Text style={[typography.body, { marginBottom: spacing.md }]}>${body}</Text>
      CONTENT_PLACEHOLDER
    </Screen>
  );
}

const styles = StyleSheet.create({
  panel: {
    backgroundColor: colors.surface,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.borderGold,
    padding: spacing.md,
    marginBottom: spacing.sm,
    gap: 6,
  },
  row: { flexDirection: 'row', gap: spacing.sm, flexWrap: 'wrap' },
  chip: {
    backgroundColor: colors.secondaryBlack,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    paddingVertical: 12,
    paddingHorizontal: 14,
    marginBottom: spacing.sm,
  },
  chipText: { color: colors.goldLight, fontWeight: '700' },
});
`;

// Meta screens
write(
  'meta/MetaScreens.tsx',
  `import React from 'react';
import { StyleSheet, Text, View, Pressable } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  Screen, GameHeader, PrimaryButton, RewardCard, GameCard, PlayerCard, Loader, LeaderboardItem, ProgressBar,
} from '../../components';
import { colors, spacing, typography } from '../../theme';
import { ROUTES } from '../../constants';
import { useDailyReward, useMissions, useLeaderboard, useFriends, useClan } from '../../hooks';
import { useUiStore } from '../../store';

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
      <GameCard title="Clan War" subtitle="Starts at lantern hour" badge="SOON" onPress={() => navigation.navigate(ROUTES.Events)} />
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
`
);

write(
  'profile/ProfileScreens.tsx',
  `import React, { useState } from 'react';
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
`
);

write(
  'economy/EconomyScreens.tsx',
  `import React, { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  Screen, GameHeader, PrimaryButton, SecondaryButton, CurrencyCard, GameCard, PremiumInput, Loader,
} from '../../components';
import { colors, spacing, typography } from '../../theme';
import { ROUTES } from '../../constants';
import { usePlayerStore, useUiStore } from '../../store';
import { useTransactions, useStore, useInventory } from '../../hooks';
import { formatCoins, formatTimeAgo } from '../../utils';

type Props = NativeStackScreenProps<any>;

export function WalletScreen({ navigation }: Props) {
  const balances = usePlayerStore(s => s.balances);
  return (
    <Screen scroll>
      <GameHeader title="Wallet" />
      <View style={styles.row}>
        <CurrencyCard label="Coins" amount={balances.coins} />
        <CurrencyCard label="Diamonds" amount={balances.diamonds} tone="diamond" />
      </View>
      <View style={[styles.row, { marginTop: spacing.sm }]}>
        <CurrencyCard label="Energy" amount={balances.energy} tone="energy" />
      </View>
      <PrimaryButton title="Deposit" onPress={() => navigation.navigate(ROUTES.Deposit)} style={{ marginTop: spacing.lg }} />
      <SecondaryButton title="Withdraw" onPress={() => navigation.navigate(ROUTES.Withdraw)} style={{ marginTop: spacing.sm }} />
      <SecondaryButton title="Transactions" onPress={() => navigation.navigate(ROUTES.TransactionHistory)} style={{ marginTop: spacing.sm }} />
      <SecondaryButton title="Store" onPress={() => navigation.navigate(ROUTES.Store)} style={{ marginTop: spacing.sm }} />
    </Screen>
  );
}

export function DepositScreen({ navigation }: Props) {
  const [amount, setAmount] = useState('1000');
  const setBalances = usePlayerStore(s => s.setBalances);
  const balances = usePlayerStore(s => s.balances);
  const showToast = useUiStore(s => s.showToast);
  return (
    <Screen scroll>
      <GameHeader title="Deposit" showBack compact />
      <Text style={typography.body}>Add coins to your temple vault.</Text>
      <PremiumInput label="Amount" value={amount} onChangeText={setAmount} keyboardType="number-pad" />
      <View style={styles.row}>
        {['500', '1000', '5000'].map(v => (
          <PrimaryButton key={v} title={v} onPress={() => setAmount(v)} style={{ flex: 1, minHeight: 44 }} />
        ))}
      </View>
      <PrimaryButton title="Confirm Deposit" onPress={() => {
        setBalances({ coins: balances.coins + Number(amount || 0) });
        showToast('Deposit complete', 'success');
        navigation.goBack();
      }} style={{ marginTop: spacing.md }} />
    </Screen>
  );
}

export function WithdrawScreen({ navigation }: Props) {
  const [amount, setAmount] = useState('100');
  const balances = usePlayerStore(s => s.balances);
  const setBalances = usePlayerStore(s => s.setBalances);
  const showToast = useUiStore(s => s.showToast);
  return (
    <Screen scroll>
      <GameHeader title="Withdraw" showBack compact />
      <Text style={typography.body}>Withdraw diamonds to your linked payout method.</Text>
      <PremiumInput label="Diamonds" value={amount} onChangeText={setAmount} keyboardType="number-pad" />
      <PrimaryButton title="Request Withdrawal" onPress={() => {
        const n = Number(amount || 0);
        if (n > balances.diamonds) { showToast('Insufficient diamonds', 'danger'); return; }
        setBalances({ diamonds: balances.diamonds - n });
        showToast('Withdrawal pending', 'info');
        navigation.goBack();
      }} />
    </Screen>
  );
}

export function TransactionHistoryScreen() {
  const { data, isLoading } = useTransactions();
  return (
    <Screen scroll>
      <GameHeader title="Transactions" showBack compact />
      {isLoading ? <Loader /> : null}
      {data?.map(tx => (
        <View key={tx.id} style={styles.panel}>
          <Text style={typography.bodyStrong}>{tx.title}</Text>
          <Text style={{ color: tx.amount >= 0 ? colors.accentGreen : colors.danger, fontWeight: '700' }}>
            {tx.amount >= 0 ? '+' : ''}{formatCoins(tx.amount)} {tx.currency}
          </Text>
          <Text style={typography.caption}>{tx.status} · {formatTimeAgo(tx.createdAt)}</Text>
        </View>
      ))}
    </Screen>
  );
}

export function StoreScreen({ navigation }: Props) {
  const { data, isLoading } = useStore();
  const setBalances = usePlayerStore(s => s.setBalances);
  const balances = usePlayerStore(s => s.balances);
  const showToast = useUiStore(s => s.showToast);
  return (
    <Screen scroll>
      <GameHeader title="Store" showBack compact />
      {isLoading ? <Loader /> : null}
      {data?.map(item => (
        <GameCard key={item.id} title={item.name} subtitle={item.description}
          badge={item.realPriceLabel || String(item.price)}
          onPress={() => {
            if (item.category === 'coins' && item.amount) setBalances({ coins: balances.coins + item.amount });
            if (item.category === 'diamonds' && item.amount) setBalances({ diamonds: balances.diamonds + item.amount });
            if (item.category === 'energy' && item.amount) setBalances({ energy: balances.energy + item.amount });
            showToast(item.name + ' purchased', 'success');
          }}
          style={{ marginBottom: spacing.sm }} />
      ))}
      <SecondaryButton title="Inventory" onPress={() => navigation.navigate(ROUTES.Inventory)} />
    </Screen>
  );
}

export function InventoryScreen() {
  const { data, isLoading } = useInventory();
  return (
    <Screen scroll>
      <GameHeader title="Inventory" showBack compact />
      {isLoading ? <Loader /> : null}
      {data?.map(item => (
        <View key={item.id} style={styles.panel}>
          <Text style={typography.bodyStrong}>{item.name}</Text>
          <Text style={typography.caption}>{item.type} · x{item.quantity}{item.equipped ? ' · Equipped' : ''}</Text>
        </View>
      ))}
    </Screen>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', gap: spacing.sm },
  panel: { backgroundColor: colors.surface, borderRadius: 12, borderWidth: 1, borderColor: colors.border, padding: spacing.md, marginBottom: spacing.sm, gap: 4 },
});
`
);

console.log('batch1 done');
