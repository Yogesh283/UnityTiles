const fs = require('fs');
const path = require('path');
const root = path.join(__dirname, '..', 'src', 'screens');
function write(rel, content) {
  const full = path.join(root, rel);
  fs.mkdirSync(path.dirname(full), { recursive: true });
  fs.writeFileSync(full, content, 'utf8');
  console.log('wrote', rel);
}

write(
  'social/SocialScreens.tsx',
  `import React from 'react';
import { StyleSheet, Text, View, Share } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  Screen, GameHeader, PrimaryButton, SecondaryButton, GameCard, Loader, PremiumInput,
} from '../../components';
import { colors, spacing, typography } from '../../theme';
import { ROUTES } from '../../constants';
import { useNotifications, useMail } from '../../hooks';
import { usePlayerStore, useUiStore } from '../../store';
import { formatTimeAgo } from '../../utils';

type Props = NativeStackScreenProps<any>;

export function NotificationsScreen() {
  const { data, isLoading } = useNotifications();
  const setUnread = usePlayerStore(s => s.setUnread);
  React.useEffect(() => { setUnread(0); }, [setUnread]);
  return (
    <Screen scroll>
      <GameHeader title="Notifications" showBack compact />
      {isLoading ? <Loader /> : null}
      {data?.map(n => (
        <View key={n.id} style={[styles.panel, !n.read && styles.unread]}>
          <Text style={typography.bodyStrong}>{n.title}</Text>
          <Text style={typography.body}>{n.body}</Text>
          <Text style={typography.caption}>{n.type} · {formatTimeAgo(n.createdAt)}</Text>
        </View>
      ))}
    </Screen>
  );
}

export function MailScreen() {
  const { data, isLoading } = useMail();
  const showToast = useUiStore(s => s.showToast);
  const setBalances = usePlayerStore(s => s.setBalances);
  const balances = usePlayerStore(s => s.balances);
  return (
    <Screen scroll>
      <GameHeader title="Mail" showBack compact />
      {isLoading ? <Loader /> : null}
      {data?.map(m => (
        <View key={m.id} style={styles.panel}>
          <Text style={typography.h3}>{m.subject}</Text>
          <Text style={typography.caption}>From {m.from} · {formatTimeAgo(m.createdAt)}</Text>
          <Text style={[typography.body, { marginVertical: 8 }]}>{m.body}</Text>
          {m.rewardCoins ? (
            <PrimaryButton title={\`Claim \${m.rewardCoins} Coins\`} onPress={() => {
              setBalances({ coins: balances.coins + (m.rewardCoins || 0) });
              showToast('Mail reward claimed', 'success');
            }} />
          ) : null}
        </View>
      ))}
    </Screen>
  );
}

export function ReferralScreen() {
  const code = usePlayerStore(s => s.profile.referralCode);
  const showToast = useUiStore(s => s.showToast);
  return (
    <Screen scroll>
      <GameHeader title="Referral" showBack compact />
      <View style={styles.panel}>
        <Text style={typography.h2}>Your Seal</Text>
        <Text style={styles.code}>{code}</Text>
        <Text style={typography.body}>Invite friends. Earn coins when they join a tournament.</Text>
      </View>
      <PrimaryButton title="Copy Code" onPress={() => showToast('Referral code copied', 'success')} />
      <SecondaryButton title="Invite Friends" onPress={() => Share.share({ message: \`Join Match IQ with my code \${code} — matchiq://invite\` })} style={{ marginTop: spacing.sm }} />
    </Screen>
  );
}

export function InviteFriendsScreen() {
  const code = usePlayerStore(s => s.profile.referralCode);
  return (
    <Screen scroll>
      <GameHeader title="Invite Friends" showBack compact />
      <Text style={typography.body}>Share the temple gates with your circle.</Text>
      <GameCard title="Invite Link" subtitle={\`matchiq://invite?code=\${code}\`} badge="SHARE"
        onPress={() => Share.share({ message: \`Play Match IQ with me! Code \${code}\` })}
        style={{ marginVertical: spacing.md }} />
      <PrimaryButton title="Share Now" onPress={() => Share.share({ message: \`Play Match IQ! \${code}\` })} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  panel: { backgroundColor: colors.surface, borderRadius: 14, borderWidth: 1, borderColor: colors.border, padding: spacing.md, marginBottom: spacing.sm, gap: 4 },
  unread: { borderColor: colors.primaryGold, backgroundColor: '#2A2214' },
  code: { color: colors.goldLight, fontSize: 28, fontWeight: '800', letterSpacing: 3, marginVertical: spacing.sm },
});
`
);

write(
  'progression/ProgressionScreens.tsx',
  `import React, { useState } from 'react';
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
`
);

write(
  'system/SystemScreens.tsx',
  `import React, { useState } from 'react';
import { StyleSheet, Text, View, Pressable, Linking, Switch } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  Screen, GameHeader, PrimaryButton, SecondaryButton, PremiumInput,
} from '../../components';
import { colors, spacing, typography } from '../../theme';
import { APP_NAME, ROUTES } from '../../constants';
import { useAuthStore, useUiStore } from '../../store';

type Props = NativeStackScreenProps<any>;

export function SettingsScreen({ navigation }: Props) {
  const [sound, setSound] = useState(true);
  const [music, setMusic] = useState(true);
  const logout = useAuthStore(s => s.logout);
  return (
    <Screen scroll>
      <GameHeader title="Settings" showBack compact />
      <Row label="Sound Effects" right={<Switch value={sound} onValueChange={setSound} thumbColor={colors.primaryGold} />} />
      <Row label="Music" right={<Switch value={music} onValueChange={setMusic} thumbColor={colors.primaryGold} />} />
      {[
        ['Language', ROUTES.Language],
        ['Privacy Policy', ROUTES.PrivacyPolicy],
        ['Terms', ROUTES.Terms],
        ['Help Center', ROUTES.HelpCenter],
        ['Contact Support', ROUTES.ContactSupport],
        ['About', ROUTES.About],
      ].map(([label, route]) => (
        <Pressable key={route} style={styles.row} onPress={() => navigation.navigate(route)}>
          <Text style={typography.bodyStrong}>{label}</Text>
          <Text style={{ color: colors.primaryGold }}>›</Text>
        </Pressable>
      ))}
      <PrimaryButton title="Logout" onPress={logout} style={{ marginTop: spacing.lg }} />
    </Screen>
  );
}

function Row({ label, right }: { label: string; right: React.ReactNode }) {
  return (
    <View style={styles.row}>
      <Text style={typography.bodyStrong}>{label}</Text>
      {right}
    </View>
  );
}

export function LanguageScreen({ navigation }: Props) {
  const langs = ['English', '日本語', 'हिन्दी', '中文', 'Español'];
  const showToast = useUiStore(s => s.showToast);
  return (
    <Screen scroll>
      <GameHeader title="Language" showBack compact />
      {langs.map(l => (
        <Pressable key={l} style={styles.row} onPress={() => { showToast(l + ' selected', 'success'); navigation.goBack(); }}>
          <Text style={typography.bodyStrong}>{l}</Text>
        </Pressable>
      ))}
    </Screen>
  );
}

export function PrivacyPolicyScreen() {
  return (
    <Screen scroll>
      <GameHeader title="Privacy Policy" showBack compact />
      <Text style={typography.body}>
        Match IQ collects account, device, and gameplay metadata to operate tournaments, prevent fraud, and improve fair play. We do not sell personal data. Contact support to request deletion.
      </Text>
    </Screen>
  );
}

export function TermsScreen() {
  return (
    <Screen scroll>
      <GameHeader title="Terms of Service" showBack compact />
      <Text style={typography.body}>
        By entering Match IQ tournaments you agree to fair-play rules, entry fees, and prize distribution policies. Cheating, multi-accounting, or exploiting voids winnings.
      </Text>
    </Screen>
  );
}

export function HelpCenterScreen({ navigation }: Props) {
  const topics = ['Account & Login', 'Tournaments', 'Wallet & Payments', 'Unity Gameplay', 'Technical Issues'];
  return (
    <Screen scroll>
      <GameHeader title="Help Center" showBack compact />
      {topics.map(t => (
        <Pressable key={t} style={styles.row} onPress={() => navigation.navigate(ROUTES.ContactSupport)}>
          <Text style={typography.bodyStrong}>{t}</Text>
          <Text style={{ color: colors.primaryGold }}>›</Text>
        </Pressable>
      ))}
    </Screen>
  );
}

export function ContactSupportScreen({ navigation }: Props) {
  const [msg, setMsg] = useState('');
  const showToast = useUiStore(s => s.showToast);
  return (
    <Screen scroll>
      <GameHeader title="Contact Support" showBack compact />
      <PremiumInput label="Message" value={msg} onChangeText={setMsg} multiline style={{ minHeight: 120, textAlignVertical: 'top' }} />
      <PrimaryButton title="Send" onPress={() => { showToast('Support ticket sent', 'success'); navigation.goBack(); }} />
      <SecondaryButton title="Email Us" onPress={() => Linking.openURL('mailto:support@matchiq.fun')} style={{ marginTop: spacing.sm }} />
    </Screen>
  );
}

export function AboutScreen() {
  return (
    <Screen scroll>
      <GameHeader title="About" showBack compact />
      <View style={styles.about}>
        <Text style={typography.hero}>{APP_NAME}</Text>
        <Text style={typography.body}>Fast Tile Matching Tournament</Text>
        <Text style={[typography.caption, { marginTop: spacing.md }]}>Version 1.0.0</Text>
        <Text style={typography.caption}>UI by React Native · Gameplay by Unity</Text>
      </View>
    </Screen>
  );
}

export function LoadingScreen() {
  return (
    <Screen>
      <View style={styles.center}>
        <Text style={typography.h2}>Loading…</Text>
        <Text style={typography.caption}>Preparing temple assets</Text>
      </View>
    </Screen>
  );
}

export function NoInternetScreen({ navigation }: Props) {
  return (
    <Screen>
      <View style={styles.center}>
        <Text style={typography.hero}>No Connection</Text>
        <Text style={[typography.body, { textAlign: 'center', marginVertical: spacing.md }]}>
          The lantern network is unreachable. Check your connection and try again.
        </Text>
        <PrimaryButton title="Retry" onPress={() => navigation.goBack()} />
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingVertical: 16, borderBottomWidth: 1, borderBottomColor: colors.border },
  about: { alignItems: 'center', marginTop: 40, gap: 8 },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: spacing.lg },
});
`
);

console.log('batch2 done');
