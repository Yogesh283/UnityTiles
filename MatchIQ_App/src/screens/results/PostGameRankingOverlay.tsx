import React, { useEffect, useMemo, useState } from 'react';
import {
  ActivityIndicator,
  Image,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import type { RoomPlayer } from '../../api/tournamentApi';
import type { MatchResultPayload } from '../../types';

const C = {
  bg: 'rgba(4, 6, 8, 0.94)',
  panel: '#0B1220',
  row: 'rgba(16, 24, 40, 0.92)',
  me: 'rgba(8, 48, 42, 0.95)',
  emerald: '#0FA958',
  gold: '#D4AF37',
  goldHi: '#FFE08A',
  white: '#FFFFFF',
  mute: 'rgba(255,255,255,0.65)',
  border: 'rgba(212, 175, 55, 0.5)',
} as const;

export type RankingRow = {
  rank: number;
  name: string;
  score: number;
  avatarUrl?: string | null;
  flag?: string | null;
  isMe?: boolean;
  reward?: number;
};

type Props = {
  result: MatchResultPayload;
  rows: RankingRow[];
  loading?: boolean;
  leagueName: string;
  leagueEndsAtMs?: number | null;
  onContinue: () => void;
};

function ordinal(n: number): string {
  const v = n % 100;
  if (v >= 11 && v <= 13) return `${n}th`;
  switch (n % 10) {
    case 1:
      return `${n}st`;
    case 2:
      return `${n}nd`;
    case 3:
      return `${n}rd`;
    default:
      return `${n}th`;
  }
}

function formatCountdown(msLeft: number): string {
  const total = Math.max(0, Math.floor(msLeft / 1000));
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;
  return `${h}h ${String(m).padStart(2, '0')}m ${String(s).padStart(2, '0')}s`;
}

/**
 * Post-win ranking overlay. Rows must come from room/leaderboard APIs — never hardcoded.
 */
export function PostGameRankingOverlay({
  result,
  rows,
  loading,
  leagueName,
  leagueEndsAtMs,
  onContinue,
}: Props) {
  const insets = useSafeAreaInsets();
  const myRank = useMemo(() => {
    const fromResult = result.rank != null ? Number(result.rank) : null;
    if (fromResult && fromResult > 0) return fromResult;
    const me = rows.find((r) => r.isMe);
    return me?.rank || 1;
  }, [result.rank, rows]);

  const [now, setNow] = useState(Date.now());
  useEffect(() => {
    if (!leagueEndsAtMs) return undefined;
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, [leagueEndsAtMs]);

  const timerLabel =
    leagueEndsAtMs && leagueEndsAtMs > now
      ? formatCountdown(leagueEndsAtMs - now)
      : null;

  const top10 = myRank <= 10;

  return (
    <View
      style={[styles.root, { paddingTop: insets.top + 8, paddingBottom: insets.bottom + 10 }]}
      pointerEvents="box-none"
    >
      <View style={styles.backdrop} />
      <ScrollView
        contentContainerStyle={styles.scroll}
        bounces={false}
        showsVerticalScrollIndicator={false}
      >
        <Text style={styles.headline}>
          Well done! You reached the {ordinal(myRank)} position.
        </Text>

        <View style={styles.leagueCard}>
          <Text style={styles.leagueLabel}>Current League</Text>
          <Text style={styles.leagueName}>{leagueName || 'LEAGUE'}</Text>
          {timerLabel ? <Text style={styles.leagueTimer}>{timerLabel}</Text> : null}
        </View>

        <View style={styles.listCard}>
          {loading ? (
            <ActivityIndicator color={C.gold} style={{ marginVertical: 24 }} />
          ) : rows.length === 0 ? (
            <Text style={styles.empty}>Ranking will appear when room results settle.</Text>
          ) : (
            rows.map((row) => (
              <View
                key={`${row.rank}-${row.name}`}
                style={[styles.row, row.isMe && styles.rowMe]}
              >
                <Text style={[styles.rank, row.isMe && styles.rankMe]}>{row.rank}</Text>
                <View style={styles.avatar}>
                  {row.avatarUrl ? (
                    <Image source={{ uri: row.avatarUrl }} style={styles.avatarImg} />
                  ) : (
                    <Text style={styles.avatarLetter}>
                      {(row.name || '?').charAt(0).toUpperCase()}
                    </Text>
                  )}
                </View>
                <View style={styles.nameCol}>
                  <Text style={[styles.name, row.isMe && styles.nameMe]} numberOfLines={1}>
                    {row.isMe ? `${row.name} (You)` : row.name}
                  </Text>
                  {!!row.flag && <Text style={styles.flag}>{row.flag}</Text>}
                </View>
                <Text style={styles.points}>{row.reward ?? row.score}</Text>
              </View>
            ))
          )}
        </View>

        <Text style={styles.topMsg}>
          {top10
            ? "You're in the TOP 10! Keep your position!"
            : `Keep climbing — ${myRank - 10} places to the TOP 10.`}
        </Text>
      </ScrollView>

      <Pressable
        style={({ pressed }) => [styles.continueBtn, pressed && { opacity: 0.9 }]}
        onPress={onContinue}
      >
        <Text style={styles.continueTxt}>CONTINUE</Text>
      </Pressable>
    </View>
  );
}

/** Map tournament room players into ranking rows (server ranks). */
export function roomPlayersToRankingRows(
  players: RoomPlayer[],
  myUuid?: string | null,
  myName?: string,
): RankingRow[] {
  const sorted = [...players].sort((a, b) => {
    const ra = a.rank ?? a.current_rank ?? 9999;
    const rb = b.rank ?? b.current_rank ?? 9999;
    if (ra !== rb) return ra - rb;
    return (b.score || 0) - (a.score || 0);
  });
  return sorted.map((p, i) => {
    const rank = p.rank ?? p.current_rank ?? i + 1;
    const isMe = !!(myUuid && p.user_uuid && p.user_uuid === myUuid);
    return {
      rank,
      name: isMe ? myName || p.display_name || 'You' : p.display_name || p.username || 'Player',
      score: p.score || 0,
      avatarUrl: p.avatar_url,
      isMe,
      reward: p.score || 0,
    };
  });
}

const styles = StyleSheet.create({
  root: {
    ...StyleSheet.absoluteFillObject,
    zIndex: 85,
    elevation: 85,
    justifyContent: 'space-between',
  },
  backdrop: { ...StyleSheet.absoluteFillObject, backgroundColor: C.bg },
  scroll: { paddingHorizontal: 16, paddingBottom: 12 },
  headline: {
    color: C.white,
    fontSize: 20,
    fontWeight: '800',
    textAlign: 'center',
    marginBottom: 14,
    lineHeight: 28,
  },
  leagueCard: {
    backgroundColor: C.panel,
    borderRadius: 16,
    borderWidth: 1.5,
    borderColor: C.border,
    padding: 14,
    alignItems: 'center',
    marginBottom: 12,
  },
  leagueLabel: { color: C.mute, fontSize: 12, fontWeight: '700', letterSpacing: 0.6 },
  leagueName: {
    color: C.goldHi,
    fontSize: 22,
    fontWeight: '900',
    letterSpacing: 1.2,
    marginTop: 4,
  },
  leagueTimer: { color: C.emerald, fontSize: 14, fontWeight: '800', marginTop: 6 },
  listCard: {
    backgroundColor: C.panel,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.08)',
    overflow: 'hidden',
    minHeight: 120,
  },
  empty: { color: C.mute, textAlign: 'center', padding: 20 },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 10,
    paddingHorizontal: 12,
    backgroundColor: C.row,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: 'rgba(255,255,255,0.06)',
    gap: 10,
  },
  rowMe: {
    backgroundColor: C.me,
    borderWidth: 1.5,
    borderColor: C.gold,
    marginHorizontal: 4,
    marginVertical: 2,
    borderRadius: 12,
  },
  rank: { width: 28, color: C.mute, fontWeight: '800', fontSize: 16 },
  rankMe: { color: C.goldHi },
  avatar: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: 'rgba(15,169,88,0.25)',
    borderWidth: 1,
    borderColor: C.border,
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  },
  avatarImg: { width: 36, height: 36 },
  avatarLetter: { color: C.white, fontWeight: '900' },
  nameCol: { flex: 1 },
  name: { color: C.white, fontWeight: '700', fontSize: 14 },
  nameMe: { color: C.goldHi },
  flag: { color: C.mute, fontSize: 11, marginTop: 1 },
  points: { color: C.gold, fontWeight: '900', fontSize: 15, minWidth: 36, textAlign: 'right' },
  topMsg: {
    marginTop: 14,
    textAlign: 'center',
    color: C.white,
    fontWeight: '700',
    fontSize: 14,
  },
  continueBtn: {
    marginHorizontal: 16,
    minHeight: 56,
    borderRadius: 16,
    backgroundColor: C.emerald,
    borderWidth: 2,
    borderColor: C.gold,
    alignItems: 'center',
    justifyContent: 'center',
    ...Platform.select({
      ios: {
        shadowColor: C.gold,
        shadowOpacity: 0.4,
        shadowRadius: 12,
        shadowOffset: { width: 0, height: 0 },
      },
      android: { elevation: 10, shadowColor: C.gold },
    }),
  },
  continueTxt: {
    color: C.white,
    fontSize: 18,
    fontWeight: '900',
    letterSpacing: 1.2,
  },
});
