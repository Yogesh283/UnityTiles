import React, { useEffect } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  Screen,
  GameHeader,
  PrimaryButton,
  SecondaryButton,
  Loader,
  CurrencyCard,
  Dialog,
} from '../../components';
import { colors, spacing, typography } from '../../theme';
import { ROUTES } from '../../constants';
import { goTo } from '../../navigation/nav';
import { useTournament } from '../../hooks';
import { formatCoins } from '../../utils';

type Props = NativeStackScreenProps<any>;

export function TournamentDetailsScreen({ navigation, route }: Props) {
  const id = route.params?.id as string;
  const { data, isLoading } = useTournament(id);
  const [confirm, setConfirm] = React.useState(false);

  if (isLoading || !data) return <Loader fullScreen />;

  const slots = data.maxPlayers - data.players;

  return (
    <Screen scroll>
      <GameHeader title={data.name} showBack compact />
      <View style={styles.panel}>
        <Text style={typography.h1}>{data.name}</Text>
        <Text style={typography.body}>{data.description}</Text>
        <View style={styles.row}>
          <CurrencyCard label="Entry" amount={data.entryFee} />
          <CurrencyCard label="Prize Pool" amount={data.prizePool} />
        </View>
        <View style={styles.row}>
          <CurrencyCard label="Joined" amount={data.players} tone="diamond" />
          <CurrencyCard label="Slots Left" amount={slots} tone="energy" />
        </View>
        <Text style={[typography.caption, { marginTop: spacing.sm }]}>
          {data.winners} winner{data.winners > 1 ? 's' : ''} · Fair play enabled
        </Text>
      </View>

      {data.prizeDistribution ? (
        <View style={styles.panel}>
          <Text style={typography.h3}>Prize Distribution</Text>
          {Object.entries(data.prizeDistribution).map(([k, v]) => (
            <Text key={k} style={[typography.bodyStrong, { marginTop: 8, color: colors.primaryGold }]}>
              {k}: {formatCoins(v)}
            </Text>
          ))}
        </View>
      ) : null}

      {data.rules?.length ? (
        <View style={styles.panel}>
          <Text style={typography.h3}>Game Rules</Text>
          {data.rules.map((r) => (
            <Text key={r} style={[typography.body, { marginTop: 6 }]}>
              ✓ {r}
            </Text>
          ))}
        </View>
      ) : null}

      <PrimaryButton title={`PLAY · ${formatCoins(data.entryFee)}`} onPress={() => setConfirm(true)} />
      <SecondaryButton
        title="Back"
        onPress={() => navigation.goBack()}
        style={{ marginTop: spacing.sm }}
      />
      <Dialog
        visible={confirm}
        title="Start Unity Match?"
        message={`Entry fee ${formatCoins(data.entryFee)}. This opens the Match IQ Unity game.`}
        confirmLabel="Play"
        onCancel={() => setConfirm(false)}
        onConfirm={() => {
          setConfirm(false);
          goTo(navigation, ROUTES.GameplayLoader, {
            tournamentId: data.id,
            mode: 'tournament',
          });
        }}
      />
    </Screen>
  );
}

export function MatchSelectionScreen({ navigation, route }: Props) {
  const tournamentId = route.params?.tournamentId as string | undefined;
  const modes = [
    { id: 'ranked', title: 'Ranked Duel', body: 'Official rating match' },
    { id: 'quick', title: 'Quick Match', body: 'Fast queue, same rewards tier' },
    { id: 'practice', title: 'Practice Gate', body: 'No entry fee · training board' },
  ];

  return (
    <Screen scroll>
      <GameHeader title="Match Selection" showBack compact />
      <Text style={typography.body}>Choose mode — Play opens the Unity game instantly.</Text>
      <View style={{ height: spacing.md }} />
      {modes.map((m) => (
        <View key={m.id} style={styles.mode}>
          <Text style={typography.h3}>{m.title}</Text>
          <Text style={typography.caption}>{m.body}</Text>
          <PrimaryButton
            title="PLAY"
            style={{ marginTop: spacing.sm }}
            onPress={() =>
              goTo(navigation, ROUTES.GameplayLoader, {
                tournamentId,
                mode: m.id === 'practice' ? 'practice' : 'tournament',
              })
            }
          />
        </View>
      ))}
    </Screen>
  );
}

export function GameplayLoaderScreen({ navigation, route }: Props) {
  const tournamentId = route.params?.tournamentId as string | undefined;
  const mode = (route.params?.mode as 'tournament' | 'campaign' | 'practice') || 'tournament';

  // Prefer embedded Unity inside the same RN APK (not a second APK).
  useEffect(() => {
    navigation.replace(ROUTES.UnityGameplay, { tournamentId, mode });
  }, [mode, navigation, tournamentId]);

  return (
    <Screen edges={['top', 'bottom', 'left', 'right']}>
      <View style={styles.loaderCenter}>
        <Text style={typography.hero}>Starting Unity</Text>
        <Text style={[typography.body, { textAlign: 'center', marginVertical: spacing.md }]}>
          Loading embedded Unity gameplay inside Match IQ…
        </Text>
        <Loader label="Opening Unity view…" />
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  panel: {
    backgroundColor: colors.surface,
    borderRadius: 18,
    borderWidth: 1,
    borderColor: colors.borderGold,
    padding: spacing.md,
    gap: spacing.sm,
    marginBottom: spacing.lg,
  },
  row: { flexDirection: 'row', gap: spacing.sm, marginTop: spacing.sm },
  mode: {
    backgroundColor: colors.surface,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.md,
    gap: 4,
  },
  loaderCenter: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: spacing.lg },
});
