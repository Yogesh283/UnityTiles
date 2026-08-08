import React, { useState } from 'react';
import { StyleSheet, Text, View, Pressable } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Screen, PrimaryButton, SecondaryButton, PremiumInput, Logo } from '../../components';
import { colors, spacing, typography } from '../../theme';
import { ROUTES, API_BASE_URL } from '../../constants';
import { authApi } from '../../api/authApi';
import { useAuthStore, usePlayerStore, useUiStore } from '../../store';

type Props = NativeStackScreenProps<any>;

export function LoginScreen({ navigation }: Props) {
  const [email, setEmail] = useState('player@matchiq.fun');
  const [password, setPassword] = useState('temple123');
  const [loading, setLoading] = useState(false);
  const setSession = useAuthStore((s) => s.setSession);
  const setProfile = usePlayerStore((s) => s.setProfile);
  const showToast = useUiStore((s) => s.showToast);

  const applySession = (session: Awaited<ReturnType<typeof authApi.login>>) => {
    setSession(session);
    setProfile(session.user);
  };

  const onLogin = async () => {
    setLoading(true);
    try {
      const session = await authApi.login(email.trim(), password);
      applySession(session);
      showToast('Game DB login successful', 'success');
    } catch (e) {
      showToast((e as Error).message || 'Login failed', 'danger');
    } finally {
      setLoading(false);
    }
  };

  const onGuest = async () => {
    setLoading(true);
    try {
      const session = await authApi.guest('Guest Player');
      applySession(session);
      showToast('Guest login · Game DB', 'success');
    } catch (e) {
      showToast((e as Error).message || 'Guest failed', 'danger');
    } finally {
      setLoading(false);
    }
  };

  const onGoogle = async () => {
    setLoading(true);
    try {
      // Demo Google identity until Google Sign-In SDK is wired
      const session = await authApi.google(
        `google-demo-${Date.now()}`,
        `google.${Date.now()}@matchiq.fun`,
        'Google Player',
      );
      applySession(session);
      showToast('Google login · Game DB', 'success');
    } catch (e) {
      showToast((e as Error).message || 'Google login failed', 'danger');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Screen scroll>
      <Logo size={124} style={{ alignSelf: 'center', marginTop: 32 }} />
      <Text style={[typography.hero, { marginTop: spacing.md, textAlign: 'center' }]}>WXO</Text>
      <Text style={[typography.body, { marginBottom: spacing.sm, textAlign: 'center' }]}>
        Login with Game database (Backend API)
      </Text>
      <Text style={[typography.caption, { marginBottom: spacing.xl }]}>{API_BASE_URL}</Text>

      <View style={styles.card}>
        <PremiumInput
          label="Email"
          value={email}
          onChangeText={setEmail}
          autoCapitalize="none"
          keyboardType="email-address"
        />
        <PremiumInput
          label="Password"
          value={password}
          onChangeText={setPassword}
          secureTextEntry
        />
        <Pressable onPress={() => navigation.navigate(ROUTES.ForgotPassword)}>
          <Text style={[typography.label, { marginBottom: spacing.md }]}>Forgot password?</Text>
        </Pressable>
        <PrimaryButton title="LOGIN" loading={loading} onPress={onLogin} />
      </View>

      <Text style={[typography.caption, { textAlign: 'center', marginVertical: spacing.md }]}>OR</Text>

      <SecondaryButton title="Continue with Google" tone="blue" onPress={onGoogle} />
      <SecondaryButton
        title="Continue as Guest"
        tone="gold"
        onPress={onGuest}
        style={{ marginTop: spacing.sm }}
      />

      <Pressable onPress={() => navigation.navigate(ROUTES.Register)} style={{ marginTop: spacing.lg }}>
        <Text style={[typography.label, { textAlign: 'center' }]}>नया अकाउंट बनाएं · Register</Text>
      </Pressable>
    </Screen>
  );
}

export function RegisterScreen({ navigation }: Props) {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const setSession = useAuthStore((s) => s.setSession);
  const setProfile = usePlayerStore((s) => s.setProfile);
  const showToast = useUiStore((s) => s.showToast);

  const onRegister = async () => {
    if (!email.includes('@') || password.length < 6) {
      showToast('Valid email + password (min 6) required', 'danger');
      return;
    }
    setLoading(true);
    try {
      const session = await authApi.register(email.trim(), password, name || 'Player');
      setSession(session);
      setProfile(session.user);
      showToast('Registered in Game DB', 'success');
    } catch (e) {
      showToast((e as Error).message || 'Register failed', 'danger');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Screen scroll>
      <Logo size={88} style={{ alignSelf: 'center', marginTop: 20 }} />
      <Text style={[typography.hero, { marginTop: spacing.sm }]}>Register</Text>
      <Text style={[typography.body, { marginBottom: spacing.xl }]}>
        New user Game database में save होगा
      </Text>
      <PremiumInput label="Display Name" value={name} onChangeText={setName} />
      <PremiumInput label="Email" value={email} onChangeText={setEmail} autoCapitalize="none" keyboardType="email-address" />
      <PremiumInput label="Password (min 6)" value={password} onChangeText={setPassword} secureTextEntry />
      <PrimaryButton title="CREATE ACCOUNT" loading={loading} onPress={onRegister} />
      <SecondaryButton title="Back to Login" onPress={() => navigation.goBack()} style={{ marginTop: spacing.md }} />
    </Screen>
  );
}

export function OTPVerificationScreen({ navigation }: Props) {
  return (
    <Screen>
      <Text style={[typography.hero, { marginTop: 40 }]}>OTP</Text>
      <Text style={typography.body}>Email login Game DB इस्तेमाल करता है। Login स्क्रीन पर जाएं।</Text>
      <PrimaryButton title="Go to Login" onPress={() => navigation.navigate(ROUTES.Login)} style={{ marginTop: spacing.xl }} />
    </Screen>
  );
}

export function ForgotPasswordScreen({ navigation }: Props) {
  const showToast = useUiStore((s) => s.showToast);
  return (
    <Screen>
      <Text style={[typography.hero, { marginTop: 40 }]}>Password Reset</Text>
      <Text style={[typography.body, { marginBottom: spacing.xl }]}>
        Reset API जल्द जुड़ेगी। अभी Admin / DB से password अपडेट करें।
      </Text>
      <PrimaryButton
        title="Back"
        onPress={() => {
          showToast('Contact support for reset', 'info');
          navigation.goBack();
        }}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 18,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
  },
});
