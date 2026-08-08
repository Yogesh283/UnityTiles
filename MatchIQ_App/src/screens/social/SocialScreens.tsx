import React from 'react';
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
            <PrimaryButton title={`Claim ${m.rewardCoins} Coins`} onPress={() => {
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
      <SecondaryButton title="Invite Friends" onPress={() => Share.share({ message: `Join Match IQ with my code ${code} — matchiq://invite` })} style={{ marginTop: spacing.sm }} />
    </Screen>
  );
}

export function InviteFriendsScreen() {
  const code = usePlayerStore(s => s.profile.referralCode);
  return (
    <Screen scroll>
      <GameHeader title="Invite Friends" showBack compact />
      <Text style={typography.body}>Share the temple gates with your circle.</Text>
      <GameCard title="Invite Link" subtitle={`matchiq://invite?code=${code}`} badge="SHARE"
        onPress={() => Share.share({ message: `Play Match IQ with me! Code ${code}` })}
        style={{ marginVertical: spacing.md }} />
      <PrimaryButton title="Share Now" onPress={() => Share.share({ message: `Play Match IQ! ${code}` })} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  panel: { backgroundColor: colors.surface, borderRadius: 14, borderWidth: 1, borderColor: colors.border, padding: spacing.md, marginBottom: spacing.sm, gap: 4 },
  unread: { borderColor: colors.primaryGold, backgroundColor: '#2A2214' },
  code: { color: colors.goldLight, fontSize: 28, fontWeight: '800', letterSpacing: 3, marginVertical: spacing.sm },
});
