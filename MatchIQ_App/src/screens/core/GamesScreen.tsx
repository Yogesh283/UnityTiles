import React from 'react';
import { StyleSheet, Text, View, Pressable, ScrollView } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Screen, GameHeader, PrimaryButton } from '../../components';
import { colors, spacing, typography, radius } from '../../theme';
import { ROUTES } from '../../constants';
import { goTo } from '../../navigation/nav';

type Props = NativeStackScreenProps<any>;

type GameItem = {
  id: string;
  title: string;
  subtitle: string;
  badge: 'LIVE' | 'SOON';
  emoji: string;
  accent: string;
  playable: boolean;
};

const GAMES: GameItem[] = [
  {
    id: 'iq-match',
    title: 'IQ Match',
    subtitle: 'Unity skill board · play inside this app',
    badge: 'LIVE',
    emoji: '🧠',
    accent: '#0F766E',
    playable: true,
  },
  {
    id: 'ludo',
    title: 'Ludo',
    subtitle: 'Coming soon in the same APK',
    badge: 'SOON',
    emoji: '🎲',
    accent: '#C2410C',
    playable: false,
  },
  {
    id: 'chess',
    title: 'Chess',
    subtitle: 'Coming soon in the same APK',
    badge: 'SOON',
    emoji: '♟️',
    accent: '#334155',
    playable: false,
  },
  {
    id: 'racing',
    title: 'Car Racing',
    subtitle: 'Coming soon in the same APK',
    badge: 'SOON',
    emoji: '🏎️',
    accent: '#7C3AED',
    playable: false,
  },
  {
    id: 'carrom',
    title: 'Carrom',
    subtitle: 'Coming soon in the same APK',
    badge: 'SOON',
    emoji: '🟡',
    accent: '#B45309',
    playable: false,
  },
];

/**
 * One APK · all games list.
 * IQ Match → GameplayLoader → UnityGameplay (embedded, stays inside app).
 */
export function GamesScreen({ navigation }: Props) {
  const playIqMatch = () =>
    goTo(navigation, ROUTES.GameplayLoader, { mode: 'practice' });

  return (
    <Screen>
      <GameHeader title="Games" showBack />
      <Text style={[typography.body, { marginBottom: spacing.md, color: colors.textMuted }]}>
        One app · play here. No leaving to another APK.
      </Text>

      <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ paddingBottom: 40 }}>
        {GAMES.map((g) => (
          <Pressable
            key={g.id}
            style={styles.card}
            onPress={() => {
              if (g.playable) playIqMatch();
            }}
            disabled={!g.playable}
          >
            <View style={[styles.thumb, { backgroundColor: g.accent }]}>
              <Text style={styles.emoji}>{g.emoji}</Text>
              <View style={[styles.badge, g.badge === 'SOON' && styles.badgeSoon]}>
                <Text style={styles.badgeText}>{g.badge}</Text>
              </View>
            </View>
            <View style={styles.body}>
              <Text style={typography.h3}>{g.title}</Text>
              <Text style={styles.sub}>{g.subtitle}</Text>
              {g.playable ? (
                <PrimaryButton title="Play" onPress={playIqMatch} style={{ marginTop: spacing.sm }} />
              ) : (
                <View style={styles.soonBtn}>
                  <Text style={styles.soonText}>Coming Soon</Text>
                </View>
              )}
            </View>
          </Pressable>
        ))}

        <View style={styles.links}>
          <PrimaryButton
            title="Tournaments"
            onPress={() => goTo(navigation, ROUTES.Tournament)}
          />
          <PrimaryButton
            title="Wallet"
            onPress={() => goTo(navigation, ROUTES.Wallet)}
            style={{ marginTop: spacing.sm }}
          />
          <PrimaryButton
            title="Referral"
            onPress={() => goTo(navigation, ROUTES.Referral)}
            style={{ marginTop: spacing.sm }}
          />
        </View>
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: radius.lg ?? 16,
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: spacing.md,
    overflow: 'hidden',
  },
  thumb: {
    height: 100,
    justifyContent: 'center',
    alignItems: 'center',
    position: 'relative',
  },
  emoji: { fontSize: 40 },
  badge: {
    position: 'absolute',
    left: 10,
    top: 10,
                backgroundColor: colors.danger,
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 6,
  },
  badgeSoon: { backgroundColor: 'rgba(0,0,0,0.55)' },
  badgeText: { color: '#fff', fontWeight: '800', fontSize: 10 },
  body: { padding: spacing.md },
  sub: { color: colors.textMuted, fontSize: 12, marginTop: 4 },
  soonBtn: {
    marginTop: spacing.sm,
    backgroundColor: colors.surface,
    borderRadius: 10,
    paddingVertical: 12,
    alignItems: 'center',
  },
  soonText: { color: colors.textMuted, fontWeight: '700', fontSize: 13 },
  links: { marginTop: spacing.sm },
});
