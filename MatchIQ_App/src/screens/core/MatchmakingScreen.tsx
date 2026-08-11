import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { radius, spacing, typography } from '../../theme';
import { ROUTES, WXO_ROOM_TOURNAMENTS } from '../../constants';
import { useAuthStore, usePlayerStore, useUiStore } from '../../store';
import { tournamentApi, type Room } from '../../api/tournamentApi';
import { openMatchSocket, type MatchSocket } from '../../services/matchSocket';
import { WXO_GAMES } from '../../constants/wxoGames';

type Props = NativeStackScreenProps<any>;

const POLL_INTERVAL_MS = 1500;

// WXO website "white" palette — applied only on this screen so the match room mirrors the
// site's light look (white surfaces, crimson-red primary, gold coins). Global dark theme
// and the shared SecondaryButton stay untouched.
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

/** WXO-styled secondary action for this white screen (red outline, red label on white). */
function LeaveButton({ title, onPress, style }: { title: string; onPress?: () => void; style?: any }) {
  return (
    <Pressable
      onPress={onPress}
      style={({ pressed }) => [styles.leaveBtnBase, pressed && styles.leaveBtnPressed, style]}
    >
      <Text style={styles.leaveBtnText}>{title}</Text>
    </Pressable>
  );
}

function statusLabel(room: Room | null): string {
  if (!room) return 'Joining room…';
  switch (room.search_status) {
    case 'match_found':
    case 'starting':
      return 'Match found';
    case 'players_connected':
      return 'All players connected';
    case 'player_joined':
      return 'Waiting for the room to fill';
    default:
      return 'Finding players…';
  }
}

/**
 * Pre-match room. Everything a player sees before the board appears lives here: who has
 * connected, the entry that was charged, and the 3-2-1 start. Unity is only opened once the
 * server says the match is active, so both devices begin on the same tick.
 */
export function MatchmakingScreen({ navigation, route }: Props) {
  const gameName = (route.params?.game as string) || 'IQ Match';
  const gameId = (route.params?.gameId as string) || 'iq-match';
  const roomKey = (route.params?.roomKey as string) || 'duel';
  const live = !!route.params?.live;
  const tournamentId =
    (route.params?.tournamentId as string) || WXO_ROOM_TOURNAMENTS[roomKey] || 'wxo_duel2';

  const token = useAuthStore((s) => s.session?.token);
  const setBalances = usePlayerStore((s) => s.setBalances);
  const lastRoomWinner = useUiStore((s) => s.lastRoomWinner);

  const [room, setRoom] = useState<Room | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [countdown, setCountdown] = useState<number | null>(null);

  const socketRef = useRef<MatchSocket | null>(null);
  const launchedRef = useRef(false);
  const leftRef = useRef(false);

  const entryFee = useMemo(() => Number(route.params?.entry) || 0, [route.params?.entry]);
  const winPrize = useMemo(
    () => Number(route.params?.prize) || entryFee * 2,
    [entryFee, route.params?.prize],
  );

  const launchMatch = useCallback(
    (active: Room) => {
      if (launchedRef.current || leftRef.current) return;
      launchedRef.current = true;
      // Keep the socket briefly; UnityGameplay reconnects on the same room so every
      // device still receives match_finished / player_left while the board is open.
      socketRef.current?.close();
      socketRef.current = null;

      navigation.replace(ROUTES.UnityGameplay, {
        roomId: active.room_id,
        tournamentId: active.tournament_id,
        levelIndex: active.level_index,
        levelSeed: active.level_seed,
        game: gameName,
        gameId,
        roomKey,
        entry: entryFee,
        prize: winPrize,
        mode: 'tournament',
        players: active.max_players,
        live,
      });
    },
    [entryFee, gameId, gameName, live, navigation, roomKey, winPrize],
  );

  const applyRoom = useCallback(
    (next: Room) => {
      setRoom(next);
      if (next.wallet_balance != null) setBalances({ coins: next.wallet_balance });

      if (next.status === 'active' || next.status === 'locked') {
        launchMatch(next);
        return;
      }

      if (next.status === 'starting' && next.match_start_at_ms) {
        const serverNow = next.server_now_ms ?? Date.now();
        const remaining = Math.ceil((next.match_start_at_ms - serverNow) / 1000);
        setCountdown(Math.max(0, remaining));
      }
    },
    [launchMatch, setBalances],
  );

  /** Free practice has no pot and no opponent to wait for — count down and start solo. */
  const soloPractice = entryFee <= 0;

  useEffect(() => {
    if (!soloPractice || launchedRef.current) return undefined;
    setCountdown(3);
    const timer = setTimeout(() => {
      if (launchedRef.current || leftRef.current) return;
      launchedRef.current = true;
      navigation.replace(ROUTES.UnityGameplay, {
        game: gameName,
        gameId,
        entry: 0,
        prize: 0,
        mode: 'practice',
        players: 1,
      });
    }, 3200);
    return () => clearTimeout(timer);
  }, [gameId, gameName, navigation, soloPractice]);

  // Join once, then keep the room live over the socket with polling as a safety net.
  useEffect(() => {
    if (soloPractice) return undefined;
    let cancelled = false;

    (async () => {
      try {
        const joined = await tournamentApi.join(tournamentId);
        if (cancelled || !joined.room_id) return;
        applyRoom(joined);

        if (token) {
          socketRef.current = openMatchSocket(joined.room_id, token, {
            onEvent: (event) => {
              if ('room' in event && event.room) applyRoom(event.room);
            },
          });
        }
      } catch (err: any) {
        if (cancelled) return;
        const detail = err?.response?.data?.detail;
        setError(typeof detail === 'string' ? detail : 'Could not join the room. Try again.');
      }
    })();

    return () => {
      cancelled = true;
      socketRef.current?.close();
    };
  }, [applyRoom, soloPractice, token, tournamentId]);

  useEffect(() => {
    const roomId = room?.room_id;
    if (!roomId || launchedRef.current) return undefined;

    const timer = setInterval(async () => {
      try {
        const snapshot = await tournamentApi.room(roomId);
        applyRoom(snapshot);
      } catch {
        // transient — the socket or the next poll will catch up
      }
    }, POLL_INTERVAL_MS);

    return () => clearInterval(timer);
  }, [applyRoom, room?.room_id]);

  // Local ticking so the 3-2-1 stays smooth between server frames.
  useEffect(() => {
    if (countdown == null || countdown <= 0) return undefined;
    const timer = setTimeout(() => setCountdown((value) => (value == null ? null : value - 1)), 1000);
    return () => clearTimeout(timer);
  }, [countdown]);

  const leave = useCallback(() => {
    leftRef.current = true;
    socketRef.current?.close();
    navigation.goBack();
  }, [navigation]);

  if (error) {
    return (
      <View style={styles.center}>
        <Text style={[typography.h2, styles.inkText]}>Could not start</Text>
        <Text style={[typography.body, styles.errorText]}>{error}</Text>
        <LeaveButton title="Back to lobby" onPress={leave} style={styles.leaveBtn} />
      </View>
    );
  }

  const players = room?.players ?? [];
  const seats = soloPractice ? 1 : room?.max_players ?? (Number(route.params?.players) || 2);
  const showCountdown = countdown != null && (soloPractice || room?.status === 'starting');
  const sharedLevel =
    room != null && Number.isFinite(room.level_index) ? room.level_index + 1 : null;
  const roomLabel =
    WXO_GAMES[gameId]?.rooms.find((r) => r.id === roomKey)?.label ||
    (roomKey === 'duel' ? '2 Players' : roomKey);

  return (
    <View style={styles.root}>
      <Text style={[typography.caption, styles.metaLabel]}>
        {live ? 'LIVE STREAM ROOM' : 'MATCH ROOM'} · {roomLabel}
      </Text>
      <Text style={[typography.hero, styles.title]}>{gameName}</Text>

      {lastRoomWinner ? (
        <View style={styles.lastWinnerBox}>
          <Text style={styles.lastWinnerLabel}>LAST WHO WON</Text>
          <Text style={styles.lastWinnerName}>
            {lastRoomWinner.won ? 'You' : lastRoomWinner.winnerName}
          </Text>
          <Text style={[typography.caption, styles.metaLabel]}>
            {lastRoomWinner.roomLabel}
            {lastRoomWinner.prize > 0 ? ` · +${lastRoomWinner.prize} WXO` : ''}
          </Text>
        </View>
      ) : null}

      <View style={styles.pillRow}>
        <View style={styles.pill}>
          <Text style={styles.pillLabel}>Entry</Text>
          <Text style={styles.pillValue}>{entryFee ? `${entryFee} WXO` : 'Free'}</Text>
        </View>
        <View style={styles.pill}>
          <Text style={styles.pillLabel}>Winner gets</Text>
          <Text style={[styles.pillValue, styles.pillValueGold]}>
            {winPrize ? `${winPrize} WXO` : '—'}
          </Text>
        </View>
        <View style={styles.pill}>
          <Text style={styles.pillLabel}>Shared level</Text>
          <Text style={styles.pillValue}>
            {sharedLevel != null ? `Lv ${sharedLevel}` : '—'}
          </Text>
        </View>
      </View>

      {showCountdown ? (
        <View style={styles.countdownBox}>
          <Text style={styles.countdownNumber}>{countdown === 0 ? 'GO' : countdown}</Text>
          <Text style={[typography.body, styles.softText]}>Get ready</Text>
        </View>
      ) : (
        <View style={styles.searchBox}>
          <ActivityIndicator color={wxo.red} size="large" />
          <Text style={[typography.h3, styles.searchText]}>{statusLabel(room)}</Text>
          <Text style={[typography.caption, styles.metaLabel]}>
            {players.length} / {seats} players connected
          </Text>
        </View>
      )}

      <View style={styles.playerList}>
        {Array.from({ length: seats }).map((_, index) => {
          const player = players[index];
          return (
            <View key={index} style={[styles.playerRow, player && styles.playerRowFilled]}>
              <View style={[styles.avatar, player && styles.avatarFilled]}>
                <Text style={[styles.avatarText, player && styles.avatarTextFilled]}>
                  {player ? (player.display_name || 'P').slice(0, 1).toUpperCase() : '?'}
                </Text>
              </View>
              <View style={styles.playerInfo}>
                <Text style={[typography.bodyStrong, styles.inkText]} numberOfLines={1}>
                  {player ? player.display_name : 'Waiting…'}
                </Text>
                {player ? (
                  <Text style={[typography.caption, styles.metaLabel]}>
                    {player.rank_tier} · Level {player.game_level}
                  </Text>
                ) : null}
              </View>
              {player ? <Text style={styles.readyTick}>●</Text> : null}
            </View>
          );
        })}
      </View>

      {!showCountdown ? (
        <LeaveButton title="Leave room" onPress={leave} style={styles.leaveBtn} />
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: wxo.bg,
    padding: spacing.lg,
    paddingTop: spacing.xxxl,
  },
  center: {
    flex: 1,
    backgroundColor: wxo.bg,
    alignItems: 'center',
    justifyContent: 'center',
    padding: spacing.lg,
  },
  title: { marginTop: spacing.xxs, marginBottom: spacing.md, color: wxo.ink },
  lastWinnerBox: {
    backgroundColor: wxo.redSoft,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: 'rgba(227, 28, 35, 0.25)',
    padding: spacing.sm,
    marginBottom: spacing.md,
  },
  lastWinnerLabel: { ...typography.caption, color: wxo.red, fontWeight: '800', letterSpacing: 1 },
  lastWinnerName: { ...typography.h3, color: wxo.ink, marginTop: 2 },
  metaLabel: { color: wxo.muted },
  inkText: { color: wxo.ink },
  softText: { color: wxo.inkSoft },
  errorText: { textAlign: 'center', marginVertical: spacing.md, color: wxo.inkSoft },
  pillRow: { flexDirection: 'row', gap: spacing.sm },
  pill: {
    flex: 1,
    backgroundColor: wxo.surfaceMuted,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: wxo.border,
    padding: spacing.sm,
  },
  pillLabel: { ...typography.caption, color: wxo.muted },
  pillValue: { ...typography.h3, color: wxo.ink },
  pillValueGold: { color: wxo.gold },
  searchBox: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: spacing.xxl,
    gap: spacing.xs,
  },
  searchText: { marginTop: spacing.sm, color: wxo.ink },
  countdownBox: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: spacing.xl,
  },
  countdownNumber: {
    ...typography.hero,
    fontSize: 88,
    lineHeight: 96,
    color: wxo.red,
  },
  playerList: { gap: spacing.xs },
  playerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    backgroundColor: wxo.surface,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: wxo.border,
    padding: spacing.sm,
    opacity: 0.7,
  },
  playerRowFilled: { opacity: 1, borderColor: wxo.red, backgroundColor: wxo.redSoft },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: radius.pill,
    backgroundColor: wxo.surfaceMuted,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarFilled: { backgroundColor: wxo.red },
  avatarText: { ...typography.bodyStrong, color: wxo.muted },
  avatarTextFilled: { color: '#FFFFFF' },
  playerInfo: { flex: 1 },
  readyTick: { color: wxo.success, fontSize: 14 },
  leaveBtn: { marginTop: spacing.lg, alignSelf: 'stretch' },
  leaveBtnBase: {
    minHeight: 52,
    borderRadius: radius.lg,
    borderWidth: 1.5,
    borderColor: wxo.red,
    backgroundColor: wxo.surface,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 20,
  },
  leaveBtnPressed: { backgroundColor: wxo.redSoft },
  leaveBtnText: { ...typography.button, color: wxo.red },
});
