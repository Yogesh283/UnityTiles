import React from 'react';
import { StyleSheet, Text, View, Pressable, ScrollView, Dimensions } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { LinearGradient } from 'expo-linear-gradient';
import {
  Screen,
  GameHeader,
  PrimaryButton,
  SecondaryButton,
  TournamentCard,
  GameCard,
  Loader,
} from '../../components';
import { colors, spacing, typography, radius } from '../../theme';
import { ROUTES } from '../../constants';
import { STRUCTURE_ROWS } from '../../constants/poolRules';
import { formatRupee } from '../../utils';
import { useTournaments, useEvents, useMissions } from '../../hooks';
import { useUiStore } from '../../store';
import { dummyApi } from '../../api';
import { useQuery } from '@tanstack/react-query';
import { goTo } from '../../navigation/nav';

type Props = NativeStackScreenProps<any>;

export function HomeScreen({ navigation }: Props) {
  const { data: tournaments, isLoading } = useTournaments();
  const { data: missions } = useMissions();
  const setSidebar = useUiStore((s) => s.setSidebarOpen);
  const banners = useQuery({ queryKey: ['banners'], queryFn: () => dummyApi.getBanners() });
  const width = Dimensions.get('window').width - 32;

  return (
    <Screen scroll>
      <GameHeader />
      <Pressable onPress={() => setSidebar(true)}>
        <Text style={[typography.label, { marginBottom: spacing.sm }]}>☰ Menu</Text>
      </Pressable>

      <ScrollView horizontal pagingEnabled showsHorizontalScrollIndicator={false} style={{ marginBottom: spacing.md }}>
        {(banners.data || []).map((b) => (
          <LinearGradient
            key={b.id}
            colors={[colors.purple, colors.blue]}
            start={{ x: 0, y: 0 }}
            end={{ x: 1, y: 1 }}
            style={[styles.banner, { width }]}
          >
            <View style={styles.badge}>
              <Text style={styles.badgeText}>{b.badge}</Text>
            </View>
            <Text style={typography.h2}>{b.title}</Text>
            <Text style={{ color: 'rgba(255,255,255,0.8)', marginTop: 4 }}>{b.subtitle}</Text>
          </LinearGradient>
        ))}
      </ScrollView>

      <PrimaryButton
        title="▶ PLAY IQ MATCH"
        onPress={() => goTo(navigation, ROUTES.GameplayLoader, { mode: 'practice' })}
        style={{ marginBottom: spacing.sm }}
      />
      <SecondaryButton
        title="All Games"
        onPress={() => goTo(navigation, ROUTES.Games)}
        style={{ marginBottom: spacing.md }}
      />

      <Text style={[typography.h2, { marginBottom: spacing.sm }]}>Top Games</Text>
      <View style={styles.gamesRow}>
        <Pressable
          style={styles.gameTile}
          onPress={() => goTo(navigation, ROUTES.GameplayLoader, { mode: 'practice' })}
        >
          <View style={[styles.gameArt, { backgroundColor: '#0F766E' }]}>
            <Text style={{ fontSize: 28 }}>🧠</Text>
            <View style={styles.liveBadge}><Text style={styles.liveBadgeText}>LIVE</Text></View>
          </View>
          <Text style={styles.gameTitle}>IQ Match</Text>
          <Text style={styles.playMini}>Play</Text>
        </Pressable>
        <Pressable style={styles.gameTile} onPress={() => goTo(navigation, ROUTES.Games)}>
          <View style={[styles.gameArt, { backgroundColor: '#C2410C' }]}>
            <Text style={{ fontSize: 28 }}>🎲</Text>
            <View style={[styles.liveBadge, { backgroundColor: 'rgba(0,0,0,0.5)' }]}>
              <Text style={styles.liveBadgeText}>SOON</Text>
            </View>
          </View>
          <Text style={styles.gameTitle}>Ludo</Text>
          <Text style={styles.soonMini}>Soon</Text>
        </Pressable>
        <Pressable style={styles.gameTile} onPress={() => goTo(navigation, ROUTES.Games)}>
          <View style={[styles.gameArt, { backgroundColor: '#334155' }]}>
            <Text style={{ fontSize: 28 }}>♟️</Text>
            <View style={[styles.liveBadge, { backgroundColor: 'rgba(0,0,0,0.5)' }]}>
              <Text style={styles.liveBadgeText}>SOON</Text>
            </View>
          </View>
          <Text style={styles.gameTitle}>Chess</Text>
          <Text style={styles.soonMini}>Soon</Text>
        </Pressable>
      </View>

      <View style={styles.quickRow}>
        {[
          { label: 'Pool Play', route: ROUTES.Tournament, icon: '👥' },
          { label: 'Battle', route: ROUTES.GameplayLoader, icon: '⚡', params: { mode: 'tournament' as const } },
          { label: 'Wallet', route: ROUTES.Wallet, icon: '👛' },
          { label: 'Income', route: ROUTES.Income, icon: '💎' },
        ].map((item) => (
          <Pressable
            key={item.label}
            style={styles.quick}
            onPress={() => {
              if (item.route === ROUTES.Tournament) {
                const nav = navigation as { jumpTo?: (n: string) => void };
                if (typeof nav.jumpTo === 'function') {
                  nav.jumpTo(ROUTES.Tournament);
                  return;
                }
              }
              if (item.route === ROUTES.Wallet) {
                const nav = navigation as { jumpTo?: (n: string) => void };
                if (typeof nav.jumpTo === 'function') {
                  nav.jumpTo(ROUTES.Wallet);
                  return;
                }
              }
              goTo(navigation, item.route, (item as { params?: object }).params);
            }}
          >
            <Text style={{ fontSize: 18 }}>{item.icon}</Text>
            <Text style={styles.quickText}>{item.label}</Text>
          </Pressable>
        ))}
      </View>

      <Text style={[typography.h2, { marginBottom: spacing.sm }]}>Live Tournaments</Text>
      {isLoading ? <Loader /> : null}
      {tournaments?.map((t) => (
        <TournamentCard
          key={t.id}
          tournament={t}
          onPress={() => goTo(navigation, ROUTES.TournamentDetails, { id: t.id })}
          onJoin={() =>
            goTo(navigation, ROUTES.GameplayLoader, {
              tournamentId: t.id,
              mode: 'tournament',
            })
          }
        />
      ))}

      <Text style={[typography.h3, { marginTop: spacing.md }]}>Missions</Text>
      {missions?.slice(0, 2).map((m) => (
        <GameCard
          key={m.id}
          title={m.title}
          subtitle={`${m.progress}/${m.target}`}
          badge={m.completed ? 'READY' : 'LIVE'}
          onPress={() => goTo(navigation, ROUTES.Missions)}
          style={{ marginTop: spacing.sm }}
        />
      ))}
    </Screen>
  );
}

export function TournamentScreen({ navigation }: Props) {
  const { data, isLoading, refetch, isRefetching } = useTournaments();

  return (
    <Screen scroll refreshing={isRefetching} onRefresh={refetch}>
      <GameHeader title="IQFX Pro Pools" />
      <View style={styles.rowBetween}>
        <Text style={typography.body}>Entry ₹10 · ₹50 · ₹100</Text>
        <Pressable onPress={() => goTo(navigation, ROUTES.CreatePool)}>
          <Text style={typography.label}>+ Create Pool</Text>
        </Pressable>
      </View>

      <View style={styles.iqfxCard}>
        <Text style={typography.h3}>🏆 IQFX Pro Tournament</Text>
        <Text style={[typography.caption, { marginBottom: spacing.sm }]}>
          Prize Pool = 70% of collection
        </Text>
        <View style={styles.tableHead}>
          <Text style={[styles.th, { width: 40 }]}>P</Text>
          <Text style={[styles.th, { width: 24 }]}>W</Text>
          <Text style={styles.th}>₹10</Text>
          <Text style={styles.th}>₹50</Text>
          <Text style={styles.th}>₹100</Text>
        </View>
        {STRUCTURE_ROWS.map((r) => (
          <View key={r.players} style={styles.tableRow}>
            <Text style={[styles.td, { width: 40 }]}>{r.players}</Text>
            <Text style={[styles.td, { width: 24 }]}>{r.winners}</Text>
            <Text style={styles.td}>{formatRupee(r.prize10)}</Text>
            <Text style={styles.td}>{formatRupee(r.prize50)}</Text>
            <Text style={styles.td}>{formatRupee(r.prize100)}</Text>
          </View>
        ))}
        <PrimaryButton
          title="➕ Create New Pool"
          onPress={() => goTo(navigation, ROUTES.CreatePool)}
          style={{ marginTop: spacing.sm }}
        />
      </View>

      <View style={{ height: spacing.md }} />
      {isLoading ? <Loader /> : null}
      {data?.map((t) => (
        <TournamentCard
          key={t.id}
          tournament={t}
          onPress={() => goTo(navigation, ROUTES.TournamentDetails, { id: t.id })}
          onJoin={() =>
            goTo(navigation, ROUTES.GameplayLoader, {
              tournamentId: t.id,
              mode: 'tournament',
            })
          }
        />
      ))}
    </Screen>
  );
}

export function EventsScreen({ navigation }: Props) {
  const { data, isLoading } = useEvents();
  return (
    <Screen scroll>
      <GameHeader title="Events" />
      {isLoading ? <Loader /> : null}
      {data?.map((e) => (
        <GameCard
          key={e.id}
          title={e.title}
          subtitle={`${e.description} · ends in ${e.endsInHours}h`}
          badge={e.rewardLabel}
          onPress={() => goTo(navigation, ROUTES.SeasonRewards)}
          style={{ marginBottom: spacing.sm }}
        />
      ))}
      <PrimaryButton title="Season Rewards" onPress={() => goTo(navigation, ROUTES.SeasonRewards)} />
      <SecondaryButton title="Create Pool" onPress={() => goTo(navigation, ROUTES.CreatePool)} style={{ marginTop: spacing.sm }} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  iqfxCard: {
    marginTop: spacing.md,
    backgroundColor: colors.surfaceElevated,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
  },
  tableHead: { flexDirection: 'row', marginBottom: 6 },
  tableRow: { flexDirection: 'row', marginBottom: 4 },
  th: { flex: 1, color: colors.textMuted, fontSize: 11, fontWeight: '700' },
  td: { flex: 1, color: colors.textPrimary, fontSize: 12, fontWeight: '600' },
  banner: {
    borderRadius: 18,
    padding: spacing.md,
    marginRight: 10,
    minHeight: 130,
    justifyContent: 'flex-end',
  },
  badge: {
    alignSelf: 'flex-start',
    backgroundColor: 'rgba(0,0,0,0.25)',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 8,
    marginBottom: 8,
  },
  badgeText: { color: colors.white, fontWeight: '800', fontSize: 11 },
  quickRow: { flexDirection: 'row', gap: spacing.xs, marginBottom: spacing.lg },
  quick: {
    flex: 1,
    backgroundColor: colors.surfaceElevated,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    paddingVertical: 14,
    alignItems: 'center',
    gap: 6,
  },
  quickText: { color: colors.textPrimary, fontWeight: '700', fontSize: 11 },
  rowBetween: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  gamesRow: { flexDirection: 'row', gap: 10, marginBottom: spacing.lg },
  gameTile: {
    flex: 1,
    backgroundColor: colors.surfaceElevated,
    borderRadius: 14,
    borderWidth: 1,
    borderColor: colors.border,
    overflow: 'hidden',
  },
  gameArt: {
    height: 72,
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative',
  },
  liveBadge: {
    position: 'absolute',
    left: 6,
    top: 6,
    backgroundColor: colors.danger,
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 5,
  },
  liveBadgeText: { color: '#fff', fontSize: 8, fontWeight: '800' },
  gameTitle: {
    color: colors.textPrimary,
    fontWeight: '800',
    fontSize: 11,
    textAlign: 'center',
    marginTop: 6,
  },
  playMini: {
    margin: 6,
    marginTop: 4,
    backgroundColor: colors.danger,
    color: '#fff',
    textAlign: 'center',
    overflow: 'hidden',
    borderRadius: 8,
    paddingVertical: 6,
    fontWeight: '800',
    fontSize: 11,
  },
  soonMini: {
    margin: 6,
    marginTop: 4,
    backgroundColor: colors.surface,
    color: colors.textMuted,
    textAlign: 'center',
    borderRadius: 8,
    paddingVertical: 6,
    fontWeight: '700',
    fontSize: 10,
  },
});
