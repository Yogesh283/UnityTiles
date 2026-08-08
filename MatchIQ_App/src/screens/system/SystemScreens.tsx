import React, { useState } from 'react';
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
