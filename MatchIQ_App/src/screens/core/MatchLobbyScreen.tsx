import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ROUTES, WXO_ROOM_TOURNAMENTS } from '../../constants';
import { WXO_GAMES, wxoWinPrize, type WxoRoom } from '../../constants/wxoGames';
import { tournamentApi, type MatchHistoryRow } from '../../api/tournamentApi';
import { useUiStore } from '../../store';
import { radius, spacing, typography } from '../../theme';

type Props = NativeStackScreenProps<any>;

const wxo = {
  bg: '#FFFFFF',
  surface: '#FFFFFF',
  surfaceMuted: '#F4F5F8',
  border: '#E6E8EF',
  ink: '#111214',
  inkSoft: '#4B5162',
  muted: '#8A90A2',
  red: '#E31C23',
  redSoft: 'rgba(227, 28, 35, 0.10)',
  gold: '#C99213',
  success: '#1AA260',
} as const;

/**
 * Full-page room picker after Play. Two Player (and other rooms) open the shared
 * Matchmaking members screen — same entry / level / start clock for every device.
 */
export function MatchLobbyScreen({ navigation, route }: Props) {
  const insets = useSafeAreaInsets();
  const gameId = (route.params?.gameId as string) || 'iq-match';
  const gameName =
    (route.params?.game as string) || WXO_GAMES[gameId]?.name || 'IQ Match';
  const game = WXO_GAMES[gameId] || WXO_GAMES['iq-match'];
  const rooms = game?.rooms ?? [];

  const lastRoomWinner = useUiStore((s) => s.lastRoomWinner);
  const [history, setHistory] = useState<MatchHistoryRow[]>([]);
  const [loadingHistory, setLoadingHistory] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const rows = await tournamentApi.history();
        if (!cancelled) setHistory(rows);
      } catch {
        if (!cancelled) setHistory([]);
      } finally {
        if (!cancelled) setLoadingHistory(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const lastForGame = useMemo(() => {
    const tournamentIds = new Set(
      rooms.map((r) => WXO_ROOM_TOURNAMENTS[r.id]).filter(Boolean),
    );
    return history.find((row) => tournamentIds.has(row.tournament_id)) || null;
  }, [history, rooms]);

  const openRoom = useCallback(
    (room: WxoRoom) => {
      if (game?.engine && game.engine !== 'unity' && !room.free) {
        return;
      }
      const entry = room.entry;
      const prize = wxoWinPrize(entry);
      navigation.navigate(ROUTES.Matchmaking, {
        game: gameName,
        gameId,
        roomKey: room.id,
        tournamentId: WXO_ROOM_TOURNAMENTS[room.id] || WXO_ROOM_TOURNAMENTS.duel,
        entry,
        prize,
        players: room.players,
        live: room.live,
      });
    },
    [game?.engine, gameId, gameName, navigation],
  );

  const comingSoon = game?.engine !== 'unity';

  return (
    <View style={[styles.root, { paddingTop: insets.top + spacing.md }]}>
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} hitSlop={12} style={styles.backBtn}>
          <Text style={styles.backText}>←</Text>
        </Pressable>
        <View style={styles.headerText}>
          <Text style={styles.eyebrow}>CHOOSE ROOM</Text>
          <Text style={styles.title}>{gameName}</Text>
        </View>
      </View>

      <ScrollView
        contentContainerStyle={styles.scroll}
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.actionCard}>
          <Text style={styles.actionLabel}>LAST RESULT</Text>
          {loadingHistory && !lastRoomWinner ? (
            <ActivityIndicator color={wxo.red} style={{ marginVertical: 8 }} />
          ) : lastRoomWinner ? (
            <>
              <Text style={styles.actionTitle}>
                {lastRoomWinner.won ? 'You won' : `${lastRoomWinner.winnerName} won`}
              </Text>
              <Text style={styles.actionMeta}>
                {lastRoomWinner.roomLabel}
                {lastRoomWinner.prize > 0 ? ` · +${lastRoomWinner.prize} WXO` : ''}
                {lastRoomWinner.level != null ? ` · Level ${lastRoomWinner.level}` : ''}
              </Text>
            </>
          ) : lastForGame ? (
            <>
              <Text style={styles.actionTitle}>
                {lastForGame.rank === 1 ? 'You won last match' : `Last match · Rank #${lastForGame.rank}`}
              </Text>
              <Text style={styles.actionMeta}>
                Score {lastForGame.score}
                {lastForGame.prize > 0 ? ` · +${lastForGame.prize} WXO` : ' · No prize'}
              </Text>
            </>
          ) : (
            <>
              <Text style={styles.actionTitle}>No matches yet</Text>
              <Text style={styles.actionMeta}>
                Pick 2 Players — everyone in the room gets the same level, timer start, and exit settle.
              </Text>
            </>
          )}
        </View>

        {comingSoon ? (
          <View style={styles.soonBox}>
            <Text style={styles.soonTitle}>Coming soon</Text>
            <Text style={styles.actionMeta}>{gameName} rooms open when the engine ships.</Text>
          </View>
        ) : (
          <View style={styles.roomList}>
            {rooms.map((room) => {
              const prize = wxoWinPrize(room.entry);
              const isDuel = room.id === 'duel';
              return (
                <Pressable
                  key={room.id}
                  onPress={() => openRoom(room)}
                  style={({ pressed }) => [
                    styles.roomRow,
                    isDuel && styles.roomRowHero,
                    pressed && styles.roomRowPressed,
                  ]}
                >
                  <View style={styles.roomInfo}>
                    <Text style={[styles.roomLabel, isDuel && styles.roomLabelHero]}>
                      {room.label}
                    </Text>
                    <Text style={styles.roomMeta}>
                      {room.players} seats · {room.live ? 'Live stream' : 'Standard'}
                    </Text>
                  </View>
                  <View style={styles.roomFees}>
                    <Text style={styles.entryText}>
                      {room.entry > 0 ? `${room.entry} WXO` : 'Free'}
                    </Text>
                    <Text style={styles.prizeText}>
                      {prize > 0 ? `Win ${prize}` : 'Practice'}
                    </Text>
                  </View>
                </Pressable>
              );
            })}
          </View>
        )}

        <Text style={styles.footerNote}>
          Connected players share one level, one seed, one start countdown, and one final settle when anyone exits or finishes.
        </Text>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: wxo.bg },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: spacing.lg,
    gap: spacing.sm,
    marginBottom: spacing.md,
  },
  backBtn: {
    width: 40,
    height: 40,
    borderRadius: radius.pill,
    borderWidth: 1,
    borderColor: wxo.border,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: wxo.surfaceMuted,
  },
  backText: { fontSize: 20, color: wxo.ink, fontWeight: '700' },
  headerText: { flex: 1 },
  eyebrow: { ...typography.caption, color: wxo.muted, letterSpacing: 1.2 },
  title: { ...typography.hero, color: wxo.ink, marginTop: 2 },
  scroll: { paddingHorizontal: spacing.lg, paddingBottom: spacing.xxxl, gap: spacing.md },
  actionCard: {
    backgroundColor: wxo.redSoft,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: 'rgba(227, 28, 35, 0.25)',
    padding: spacing.md,
    gap: 4,
  },
  actionLabel: { ...typography.caption, color: wxo.red, fontWeight: '800', letterSpacing: 1 },
  actionTitle: { ...typography.h3, color: wxo.ink },
  actionMeta: { ...typography.body, color: wxo.inkSoft },
  soonBox: {
    padding: spacing.lg,
    borderRadius: radius.lg,
    backgroundColor: wxo.surfaceMuted,
    borderWidth: 1,
    borderColor: wxo.border,
  },
  soonTitle: { ...typography.h3, color: wxo.ink, marginBottom: 4 },
  roomList: { gap: spacing.sm },
  roomRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: spacing.md,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: wxo.border,
    backgroundColor: wxo.surface,
  },
  roomRowHero: {
    borderColor: wxo.red,
    backgroundColor: wxo.redSoft,
    borderWidth: 1.5,
  },
  roomRowPressed: { opacity: 0.88 },
  roomInfo: { flex: 1, paddingRight: spacing.sm },
  roomLabel: { ...typography.bodyStrong, color: wxo.ink, fontSize: 17 },
  roomLabelHero: { color: wxo.red, fontWeight: '800' },
  roomMeta: { ...typography.caption, color: wxo.muted, marginTop: 2 },
  roomFees: { alignItems: 'flex-end' },
  entryText: { ...typography.bodyStrong, color: wxo.ink },
  prizeText: { ...typography.caption, color: wxo.gold, fontWeight: '700', marginTop: 2 },
  footerNote: {
    ...typography.caption,
    color: wxo.muted,
    textAlign: 'center',
    marginTop: spacing.sm,
    lineHeight: 18,
  },
});
