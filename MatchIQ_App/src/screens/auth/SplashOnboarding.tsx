import React, { useEffect } from 'react';
import { StyleSheet, Text, View, Animated } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Screen, PrimaryButton, Logo } from '../../components';
import { colors, spacing, typography } from '../../theme';
import { APP_NAME, APP_TAGLINE, ROUTES } from '../../constants';
import { useAuthStore } from '../../store';

type Props = NativeStackScreenProps<any>;

export function SplashScreen({ navigation }: Props) {
  const session = useAuthStore((s) => s.session);
  const hasOnboarded = useAuthStore((s) => s.hasOnboarded);
  const scale = React.useRef(new Animated.Value(0.9)).current;
  const opacity = React.useRef(new Animated.Value(0)).current;

  useEffect(() => {
    Animated.parallel([
      Animated.spring(scale, { toValue: 1, useNativeDriver: true }),
      Animated.timing(opacity, { toValue: 1, duration: 500, useNativeDriver: true }),
    ]).start();
    const t = setTimeout(() => {
      if (session) return;
      if (hasOnboarded) navigation.replace(ROUTES.Login);
      else navigation.replace(ROUTES.Onboarding);
    }, 2200);
    return () => clearTimeout(t);
  }, [navigation, session, hasOnboarded, scale, opacity]);

  return (
    <Screen padded={false} edges={['top', 'bottom', 'left', 'right']}>
      <View style={styles.center}>
        <Animated.View style={{ opacity, transform: [{ scale }] }}>
          <Logo size={190} />
        </Animated.View>
        <Text style={[typography.hero, { marginTop: spacing.lg }]}>{APP_NAME}</Text>
        <Text style={typography.caption}>{APP_TAGLINE}</Text>
        <Text style={[typography.caption, { marginTop: spacing.xl }]}>Loading arena…</Text>
      </View>
    </Screen>
  );
}

export function OnboardingScreen({ navigation }: Props) {
  const setOnboarded = useAuthStore((s) => s.setOnboarded);
  const slides = [
    { title: 'Pool Play', body: 'Join premium cash tournaments with fair boards.' },
    { title: 'Win Big', body: 'Climb leaderboards and cash out your winnings.' },
    { title: 'Create Rooms', body: 'Host custom pools with auto prize split.' },
  ];
  const [index, setIndex] = React.useState(0);
  const slide = slides[index];

  return (
    <Screen>
      <View style={styles.onboard}>
        <Logo size={96} style={{ alignSelf: 'center' }} />
        <Text style={typography.hero}>{slide.title}</Text>
        <Text style={[typography.body, styles.body]}>{slide.body}</Text>
        <View style={styles.dots}>
          {slides.map((_, i) => (
            <View key={i} style={[styles.dot, i === index && styles.dotActive]} />
          ))}
        </View>
        <PrimaryButton
          title={index === slides.length - 1 ? `Enter ${APP_NAME}` : 'Continue'}
          onPress={() => {
            if (index < slides.length - 1) setIndex(index + 1);
            else {
              setOnboarded(true);
              navigation.replace(ROUTES.Login);
            }
          }}
        />
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  center: { flex: 1, alignItems: 'center', justifyContent: 'center' },
  onboard: { flex: 1, justifyContent: 'center', gap: spacing.lg, paddingTop: 80 },
  body: { fontSize: 17, lineHeight: 26 },
  dots: { flexDirection: 'row', gap: 8, marginVertical: spacing.md },
  dot: { width: 8, height: 8, borderRadius: 4, backgroundColor: colors.border },
  dotActive: { backgroundColor: colors.purple, width: 22 },
});
