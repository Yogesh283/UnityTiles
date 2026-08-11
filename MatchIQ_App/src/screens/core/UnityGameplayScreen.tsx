import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  ActivityIndicator,
  Dimensions,
  Modal,
  Platform,
  Pressable,
  StyleSheet,
  Text,
  UIManager,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useFocusEffect } from '@react-navigation/native';
import Constants, { ExecutionEnvironment } from 'expo-constants';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { PrimaryButton, SecondaryButton, Loader } from '../../components';
import { spacing, typography, radius } from '../../theme';
import { ROUTES } from '../../constants';
import { useAuthStore, usePlayerStore, useUiStore } from '../../store';
import {
  unityBridge,
  sendUnityCommand,
  parseUnityEvent,
  type UnityCommand,
} from '../../services/unityBridge';
import { setPendingWxoMatchResult } from '../../services/wxoMatchBridge';
import { tournamentApi } from '../../api/tournamentApi';
import { leaderboardApi } from '../../api/leaderboardApi';
import type { MatchResultPayload } from '../../types';
import { openMatchSocket, type MatchSocket } from '../../services/matchSocket';
import { WXO_GAMES } from '../../constants/wxoGames';
import type { RoomPlayer } from '../../api/tournamentApi';
import { goHome } from '../../navigation/nav';
import {
  PostGameWinOverlay,
} from '../results/PostGameWinOverlay';
import {
  PostGameRankingOverlay,
  roomPlayersToRankingRows,
  type RankingRow,
} from '../results/PostGameRankingOverlay';

type Props = NativeStackScreenProps<any>;

type UnityViewComponent = React.ComponentType<{
  style?: object;
  ref?: React.Ref<any>;
  onUnityMessage?: (event: { nativeEvent: { message: string } }) => void;
  androidKeepPlayerMounted?: boolean;
  fullScreen?: boolean;
}>;

type BoardStats = {
  won: boolean;
  score: number;
  moves: number;
  elapsedSeconds: number;
  iq?: number;
  combo?: number;
  message?: string;
  level?: number;
  accuracy?: number;
};
type BoosterKind = 'hint' | 'shuffle' | 'undo';
type PostPhase = 'none' | 'win' | 'ranking';

const HUD = {
  bg: '#080A08',
  black: '#050505',
  emerald: '#0FA958',
  emeraldDark: '#087A3F',
  gold: '#D4AF37',
  goldHi: '#FFE08A',
  white: '#FFFFFF',
  overlay: 'rgba(0,0,0,0.72)',
  disabled: 'rgba(80,80,80,0.55)',
  /** In-match menu uses WXO white/red (site brand), not board gold. */
  wxoRed: '#E31C23',
  wxoRedDark: '#A50E14',
  wxoRedSoft: 'rgba(227, 28, 35, 0.10)',
  wxoInk: '#111214',
  wxoSoft: '#4B5162',
  wxoBorder: '#E6E8EF',
} as const;

/** Cooldown so Unity can finish shuffle/hint/undo before another tap. */
const BOOSTER_COOLDOWN_MS = 900;

function parseBoardStats(raw: string): BoardStats | null {
  const parsed = unityBridge.parseMatchResultUrl(raw) || unityBridge.parseMatchResultJson(raw);
  if (!parsed) return null;

  let moves = 0;
  let iq: number | undefined;
  let combo: number | undefined;
  let message: string | undefined;
  let level: number | undefined;
  try {
    const json = JSON.parse(raw);
    moves = Number(json?.moves) || 0;
    if (json?.iq != null) iq = Number(json.iq);
    if (json?.combo != null) combo = Number(json.combo);
    if (json?.message) message = String(json.message);
    if (json?.level != null) level = Number(json.level);
  } catch {
    const match = /[?&]moves=(\d+)/.exec(raw);
    moves = match ? Number(match[1]) : 0;
  }

  return {
    won: !!parsed.won,
    score: Number(parsed.score) || 0,
    moves: Math.max(1, moves || Number(parsed.moves) || 1),
    elapsedSeconds: Math.max(1, Number(parsed.timeSeconds) || 1),
    iq: iq ?? parsed.iq,
    combo: combo ?? parsed.combo,
    message: message ?? parsed.message,
    level: level ?? parsed.level,
    accuracy: parsed.accuracy,
  };
}

function formatMmSs(totalSec: number): string {
  const s = Math.max(0, Math.floor(totalSec));
  const m = Math.floor(s / 60);
  const r = s % 60;
  return `${String(m).padStart(2, '0')}:${String(r).padStart(2, '0')}`;
}

/**
 * Embedded Unity gameplay. Unity owns board / tiles / timer / score / matches math.
 * React Native owns the only HUD and sends one command per action via sendUnityCommand.
 */
export function UnityGameplayScreen({ navigation, route }: Props) {
  const insets = useSafeAreaInsets();
  const roomId = route.params?.roomId as string | undefined;
  const tournamentId = route.params?.tournamentId as string | undefined;
  const levelIndex = route.params?.levelIndex as number | undefined;
  const levelSeed = route.params?.levelSeed as number | undefined;
  const roomKey = (route.params?.roomKey as string) || 'duel';
  const mode = (route.params?.mode as 'tournament' | 'campaign' | 'practice') || 'tournament';
  const gameName = (route.params?.game as string) || 'IQ Match';
  const gameId = (route.params?.gameId as string) || 'iq-match';
  const entryFee = Number(route.params?.entry) || 0;
  const winPrize = Number(route.params?.prize) || entryFee * 2;
  const token = useAuthStore((s) => s.session?.token) || 'demo-token';
  const sessionUser = useAuthStore((s) => s.session?.user);
  const setLast = useUiStore((s) => s.setLastMatchResult);
  const setLastRoomWinner = useUiStore((s) => s.setLastRoomWinner);
  const setBalances = usePlayerStore((s) => s.setBalances);
  const profile = usePlayerStore((s) => s.profile);
  const finished = useRef(false);
  const matchIdRef = useRef(roomId || `match-${Date.now()}`);
  const roomPlayersRef = useRef<RoomPlayer[]>([]);
  const socketRef = useRef<MatchSocket | null>(null);
  const unityRef = useRef<{
    postMessage: (go: string, method: string, message: string) => void;
    unloadUnity?: () => void;
  } | null>(null);

  const leaveOpenRef = useRef(false);
  const menuOpenRef = useRef(false);
  const settingsNavRef = useRef(false);
  const boosterLockUntil = useRef(0);
  const commandLock = useRef(false);

  const [UnityView, setUnityView] = useState<UnityViewComponent | null>(null);
  const [nativeMissing, setNativeMissing] = useState(false);
  const [gameReady, setGameReady] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const [leaveOpen, setLeaveOpen] = useState(false);
  const [leaving, setLeaving] = useState(false);
  const [restarting, setRestarting] = useState(false);
  const [busyBooster, setBusyBooster] = useState<BoosterKind | null>(null);
  const [liveScore, setLiveScore] = useState(0);
  const [liveLevel, setLiveLevel] = useState(
    levelIndex != null ? Math.max(1, levelIndex + 1) : 1,
  );
  const [liveMatches, setLiveMatches] = useState(0);
  const [liveTimer, setLiveTimer] = useState('00:00');
  const [timerUrgent, setTimerUrgent] = useState(false);
  const [postPhase, setPostPhase] = useState<PostPhase>('none');
  const [postResult, setPostResult] = useState<MatchResultPayload | null>(null);
  const [rankingRows, setRankingRows] = useState<RankingRow[]>([]);
  const [rankingLoading, setRankingLoading] = useState(false);
  const [luckyBusy, setLuckyBusy] = useState(false);
  const [luckyNote, setLuckyNote] = useState<string | null>(null);

  const windowW = Dimensions.get('window').width;
  const boosterSize = Math.min(120, Math.max(92, Math.round(windowW * 0.22)));
  const menuBtnSize = Math.max(48, Math.min(56, Math.round(windowW * 0.12)));

  const setMenu = useCallback((open: boolean) => {
    menuOpenRef.current = open;
    setMenuOpen(open);
  }, []);

  const setLeave = useCallback((open: boolean) => {
    leaveOpenRef.current = open;
    setLeaveOpen(open);
  }, []);

  useEffect(() => {
    let mounted = true;
    const inExpoGo = Constants.executionEnvironment === ExecutionEnvironment.StoreClient;
    const getConfig = (UIManager as any).getViewManagerConfig?.bind(UIManager);
    const legacyHasUnity = !!getConfig?.('RNUnityView') || !!getConfig?.('RCTRNUnityView');
    const hasNativeUnity = !inExpoGo || legacyHasUnity;
    if (!hasNativeUnity) {
      setNativeMissing(true);
      return () => {
        mounted = false;
      };
    }

    (async () => {
      try {
        const mod = require('@azesmway/react-native-unity');
        if (mounted) setUnityView(() => mod.default as UnityViewComponent);
      } catch {
        if (mounted) setNativeMissing(true);
      }
    })();
    return () => {
      mounted = false;
    };
  }, []);

  // Returning from Settings: keep game paused and show Menu again.
  useFocusEffect(
    useCallback(() => {
      if (settingsNavRef.current) {
        settingsNavRef.current = false;
        setMenu(true);
        sendUnityCommand(unityRef.current, 'PauseGame');
      }
    }, [setMenu]),
  );

  const launchPayload = useMemo(
    () => ({
      matchId: matchIdRef.current,
      roomId,
      tournamentId,
      levelId: levelIndex != null ? String(levelIndex) : undefined,
      seed: levelSeed != null ? String(levelSeed) : undefined,
      mode,
      token,
    }),
    [levelIndex, levelSeed, mode, roomId, token, tournamentId],
  );

  const cmd = useCallback((command: UnityCommand, opts?: { amount?: number }): boolean => {
    const lucky =
      command === 'GrantShuffle' || command === 'GrantUndo' || command === 'WatchRewardedAd';
    if (finished.current && command !== 'ExitGame' && !lucky) return false;
    return sendUnityCommand(unityRef.current, command, opts);
  }, []);

  const loadRankingRows = useCallback(
    async (result: MatchResultPayload) => {
      setRankingLoading(true);
      try {
        if (roomId) {
          const snap = await tournamentApi.room(roomId);
          roomPlayersRef.current = snap.players || roomPlayersRef.current;
          const rows = roomPlayersToRankingRows(
            snap.players || [],
            sessionUser?.id,
            sessionUser?.displayName || 'You',
          );
          if (rows.length) {
            setRankingRows(rows);
            return;
          }
        }

        const board = await leaderboardApi.list(20);
        if (board.length) {
          setRankingRows(
            board.map((row, i) => ({
              rank: i + 1,
              name: row.display_name || `Player ${row.user_id}`,
              score: row.total_prize || row.total_wins || 0,
              reward: row.total_prize,
              isMe: false,
            })),
          );
          // Ensure current player appears highlighted with actual match rank when known.
          if (result.rank != null) {
            setRankingRows((prev) => {
              const me: RankingRow = {
                rank: result.rank || prev.length + 1,
                name: sessionUser?.displayName || profile.displayName || 'You',
                score: result.score,
                reward: result.coinsEarned,
                isMe: true,
              };
              const withoutDup = prev.filter((r) => !r.isMe && r.rank !== me.rank);
              return [...withoutDup, me].sort((a, b) => a.rank - b.rank).slice(0, 15);
            });
          }
          return;
        }

        setRankingRows([
          {
            rank: result.rank || 1,
            name: sessionUser?.displayName || profile.displayName || 'You',
            score: result.score,
            reward: result.coinsEarned,
            isMe: true,
          },
        ]);
      } catch {
        setRankingRows([
          {
            rank: result.rank || 1,
            name: sessionUser?.displayName || profile.displayName || 'You',
            score: result.score,
            reward: result.coinsEarned,
            isMe: true,
          },
        ]);
      } finally {
        setRankingLoading(false);
      }
    },
    [profile.displayName, roomId, sessionUser?.displayName, sessionUser?.id],
  );

  const recordLastWinner = useCallback(
    (opts: { won: boolean; prize: number; winnerName: string; level?: number }) => {
      const roomLabel =
        WXO_GAMES[gameId]?.rooms.find((r) => r.id === roomKey)?.label ||
        (roomKey === 'duel' ? '2 Players' : roomKey);
      setLastRoomWinner({
        roomKey,
        roomLabel,
        winnerName: opts.winnerName,
        won: opts.won,
        prize: opts.prize,
        level: opts.level ?? (levelIndex != null ? levelIndex + 1 : undefined),
        at: Date.now(),
      });
    },
    [gameId, levelIndex, roomKey, setLastRoomWinner],
  );

  const finishWithResult = useCallback(
    async (won?: boolean, raw?: string) => {
      if (finished.current) return;
      finished.current = true;
      setMenu(false);
      setLeave(false);
      setLeaving(false);
      setRestarting(false);
      setBusyBooster(null);
      cmd('ResumeGame');
      socketRef.current?.close();
      socketRef.current = null;

      const stats: BoardStats = (raw && parseBoardStats(raw)) || {
        won: !!won,
        score: liveScore,
        moves: 1,
        elapsedSeconds: 1,
      };
      if (won != null) stats.won = won;

      let prize = stats.won ? winPrize : 0;
      let rank: number | null = stats.won ? 1 : null;

      if (roomId) {
        try {
          const settled = await tournamentApi.submitScore({
            roomId,
            score: stats.score,
            moves: stats.moves,
            elapsedSeconds: stats.elapsedSeconds,
          });
          prize = settled.prize;
          rank = settled.rank;
          stats.won = settled.prize > 0 || settled.rank === 1;
          if (settled.wallet_balance != null) setBalances({ coins: settled.wallet_balance });
        } catch {
          // Keep optimistic result
        }
      }

      const opponent =
        roomPlayersRef.current.find(
          (p) => p.user_uuid && p.user_uuid !== sessionUser?.id,
        )?.display_name || 'Opponent';

      recordLastWinner({
        won: stats.won,
        prize,
        winnerName: stats.won ? sessionUser?.displayName || 'You' : opponent,
      });

      const result: MatchResultPayload = {
        matchId: matchIdRef.current,
        tournamentId,
        roomId,
        won: stats.won,
        score: stats.score,
        timeSeconds: stats.elapsedSeconds,
        accuracy: stats.accuracy ?? stats.iq ?? 0,
        iq: stats.iq ?? stats.accuracy,
        combo: stats.combo,
        message: stats.message,
        level: stats.level ?? liveLevel,
        rank,
        moves: stats.moves,
        coinsEarned: prize,
        xpEarned: stats.won ? 80 : 25,
        opponentName: rank ? `Rank #${rank}` : opponent,
        league: profile.rank,
      };

      setPendingWxoMatchResult({
        won: result.won,
        prize,
        game: gameName,
        matchId: matchIdRef.current,
      });

      setLast(result);

      if (result.won) {
        setPostResult(result);
        setPostPhase('win');
        void loadRankingRows(result);
        return;
      }

      navigation.replace(ROUTES.Defeat, { result });
    },
    [
      cmd,
      gameName,
      liveLevel,
      liveScore,
      loadRankingRows,
      navigation,
      profile.rank,
      recordLastWinner,
      roomId,
      sessionUser?.displayName,
      sessionUser?.id,
      setBalances,
      setLast,
      setLeave,
      setMenu,
      tournamentId,
      winPrize,
    ],
  );

  /** Opponent finished / room settled — every connected client exits on the same result. */
  const finishFromSharedResults = useCallback(
    (results: Array<{ user_id?: number; user_uuid?: string | null; rank?: number; prize?: number; score?: number }>) => {
      if (finished.current || !results?.length) return;
      const myUuid = sessionUser?.id;
      const mine =
        results.find((r) => myUuid && r.user_uuid === myUuid) ||
        results.find((r) => r.rank != null);
      const winner = results.find((r) => r.rank === 1) || results[0];
      const winnerPlayer = roomPlayersRef.current.find(
        (p) =>
          (winner?.user_uuid && p.user_uuid === winner.user_uuid) ||
          (winner?.user_id != null && p.user_id === winner.user_id),
      );
      const won = !!(mine && (mine.prize || 0) > 0) || mine?.rank === 1;
      const prize = Number(mine?.prize) || 0;
      const winnerName =
        winnerPlayer?.display_name ||
        (won ? sessionUser?.displayName || 'You' : 'Opponent');

      finished.current = true;
      setMenu(false);
      setLeave(false);
      socketRef.current?.close();
      socketRef.current = null;
      cmd('ExitGame');

      recordLastWinner({ won, prize, winnerName });

      const result: MatchResultPayload = {
        matchId: matchIdRef.current,
        tournamentId,
        roomId,
        won,
        score: Number(mine?.score) || liveScore,
        timeSeconds: 1,
        accuracy: 0,
        level: liveLevel,
        rank: mine?.rank ?? null,
        coinsEarned: prize,
        xpEarned: won ? 80 : 25,
        opponentName: winnerName,
        league: profile.rank,
      };
      if (prize > 0) {
        setPendingWxoMatchResult({
          won,
          prize,
          game: gameName,
          matchId: matchIdRef.current,
        });
      }
      setLast(result);
      if (won) {
        setPostResult(result);
        setPostPhase('win');
        void loadRankingRows(result);
        return;
      }
      navigation.replace(ROUTES.Defeat, { result });
    },
    [
      cmd,
      gameName,
      liveLevel,
      liveScore,
      loadRankingRows,
      navigation,
      profile.rank,
      recordLastWinner,
      roomId,
      sessionUser?.displayName,
      sessionUser?.id,
      setLast,
      setLeave,
      setMenu,
      tournamentId,
    ],
  );

  // Live room socket while Unity is open — shared finish / exit for all connected players.
  useEffect(() => {
    if (!roomId || mode !== 'tournament') return undefined;
    if (!token || token === 'demo-token') return undefined;

    const sock = openMatchSocket(roomId, token, {
      onEvent: (event) => {
        if ('room' in event && event.room?.players) {
          roomPlayersRef.current = event.room.players;
        }
        if (event.event === 'match_finished' && Array.isArray(event.results)) {
          finishFromSharedResults(event.results as any);
        }
        if (event.event === 'player_left' && event.room?.status === 'finished') {
          // Room already closed — poll settle via results if present later.
        }
      },
    });
    socketRef.current = sock;

    // Seed player list once so last-winner names work even before the first WS frame.
    void tournamentApi
      .room(roomId)
      .then((snap) => {
        roomPlayersRef.current = snap.players || [];
      })
      .catch(() => undefined);

    return () => {
      sock.close();
      if (socketRef.current === sock) socketRef.current = null;
    };
  }, [finishFromSharedResults, mode, roomId, token]);

  useEffect(() => {
    if (!UnityView || !unityRef.current) return;
    const json = JSON.stringify(launchPayload);
    try {
      unityRef.current.postMessage('MatchIQShellBridge', 'OnReactNativeLaunch', json);
    } catch {
      // Unity scene may not have the GO yet
    }
  }, [UnityView, launchPayload]);

  const onUnityMessage = useCallback(
    (event: { nativeEvent: { message: string } }) => {
      const message = event.nativeEvent.message || '';
      if (!message) return;

      const ev = parseUnityEvent(message);
      if (ev) {
        switch (ev.type) {
          case 'TapDiag': {
            // Development-only Unity probe — never paint on-screen chrome in release.
            if (__DEV__) console.log('[WXO TapDiag]', ev.payload);
            return;
          }
          case 'GameStarted': {
            const lvl = Number(ev.payload.level);
            if (Number.isFinite(lvl) && lvl > 0) setLiveLevel(Math.floor(lvl));
            setGameReady(true);
            setRestarting(false);
            setBusyBooster(null);
            console.log('[WXO] GameStarted', ev.payload);
            return;
          }
          case 'ScoreUpdated':
            setLiveScore(Number(ev.payload.score) || 0);
            setGameReady(true);
            return;
          case 'MatchesUpdated':
            setLiveMatches(Math.max(0, Number(ev.payload.matches) || 0));
            setGameReady(true);
            return;
          case 'TimerUpdated': {
            const formatted = String(ev.payload.formatted || '');
            const remaining = Number(ev.payload.remainingSeconds);
            if (formatted) setLiveTimer(formatted);
            else if (Number.isFinite(remaining)) setLiveTimer(formatMmSs(remaining));
            setTimerUrgent(Number.isFinite(remaining) && remaining <= 10);
            // GameStarted can be missed if it fired before the listener attached — timer means playable.
            setGameReady(true);
            return;
          }
          case 'ExitRequested':
            setMenu(false);
            setLeave(true);
            cmd('PauseGame');
            return;
          case 'GamePaused':
            // Echo from our own PauseGame — don't open a second menu/popup.
            // Only react if neither menu nor leave is already showing (e.g. external pause).
            if (ev.payload.paused === true) {
              if (!leaveOpenRef.current && !menuOpenRef.current) setMenu(true);
            }
            return;
          case 'RewardGranted': {
            const kind = String(ev.payload.kind || '');
            const amount = Number(ev.payload.amount) || 0;
            setLuckyBusy(false);
            if (kind === 'video' && amount <= 0) {
              setLuckyNote('Ad unavailable — try again later.');
              return;
            }
            if (kind === 'video') setLuckyNote('Reward claimed — Shuffle +1, Undo +1');
            else if (kind === 'shuffle') setLuckyNote(`Shuffle +${amount} granted`);
            else if (kind === 'undo') setLuckyNote(`Undo +${amount} granted`);
            else setLuckyNote('Reward granted');
            return;
          }
          case 'LevelCompleted':
            void finishWithResult(true, message);
            return;
          case 'GameOver':
            void finishWithResult(false, message);
            return;
          case 'match-result':
            void finishWithResult(
              undefined,
              typeof ev.payload.raw === 'string' ? String(ev.payload.raw) : message,
            );
            return;
          default:
            break;
        }
      }

      if (
        message.startsWith('matchiq://') ||
        message.includes('match-result') ||
        (message.includes('"won"') && message.includes('"score"'))
      ) {
        void finishWithResult(undefined, message);
      }
    },
    [cmd, finishWithResult, setLeave, setMenu],
  );

  const openMenu = useCallback(() => {
    if (finished.current || leaveOpenRef.current || menuOpenRef.current) return;
    setMenu(true);
    cmd('PauseGame');
  }, [cmd, setMenu]);

  const closeMenuResume = useCallback(() => {
    if (commandLock.current) return;
    commandLock.current = true;
    setMenu(false);
    cmd('ResumeGame');
    setTimeout(() => {
      commandLock.current = false;
    }, 300);
  }, [cmd, setMenu]);

  const onRestart = useCallback(() => {
    // Tournament rooms share one level/seed — restart would desync connected players.
    if (mode === 'tournament' && roomId) return;
    if (commandLock.current || restarting) return;
    commandLock.current = true;
    setRestarting(true);
    setMenu(false);
    // Reset displayed stats; Unity GameStarted / ScoreUpdated / TimerUpdated refill them.
    setLiveScore(0);
    setLiveMatches(0);
    setLiveTimer('00:00');
    setTimerUrgent(false);
    setBusyBooster(null);
    cmd('RestartGame');
    setTimeout(() => {
      commandLock.current = false;
    }, 500);
  }, [cmd, mode, restarting, roomId, setMenu]);

  const onSettings = useCallback(() => {
    setMenu(false);
    settingsNavRef.current = true;
    // Stay paused while Settings is open.
    cmd('PauseGame');
    navigation.navigate(ROUTES.Settings as never);
  }, [cmd, navigation, setMenu]);

  const onLeaveFromMenu = useCallback(() => {
    setMenu(false);
    setLeave(true);
    cmd('PauseGame');
  }, [cmd, setLeave, setMenu]);

  const confirmLeave = useCallback(() => {
    if (leaving || finished.current) return;
    setLeaving(true);
    setLeave(false);
    setMenu(false);
    // ExitGame → Unity ReturnMatchResult(false) → GameOver / match-result → finishWithResult.
    // Also call finishWithResult as a fallback if the message never arrives.
    const ok = cmd('ExitGame');
    if (!ok) {
      void finishWithResult(false);
      return;
    }
    setTimeout(() => {
      if (!finished.current) void finishWithResult(false);
    }, 800);
  }, [cmd, finishWithResult, leaving, setLeave, setMenu]);

  const cancelLeave = useCallback(() => {
    if (leaving) return;
    setLeave(false);
    cmd('ResumeGame');
  }, [cmd, leaving, setLeave]);

  const runBooster = useCallback(
    (kind: BoosterKind, command: UnityCommand) => {
      if (finished.current) return;
      if (menuOpenRef.current || leaveOpenRef.current) return;
      const now = Date.now();
      if (now < boosterLockUntil.current) return;
      if (busyBooster) return;

      boosterLockUntil.current = now + BOOSTER_COOLDOWN_MS;
      setBusyBooster(kind);
      // Ensure Unity is unpaused before gameplay commands.
      cmd('ResumeGame');
      const ok = cmd(command);
      if (!ok) {
        setBusyBooster(null);
        boosterLockUntil.current = 0;
        return;
      }
      setTimeout(() => {
        setBusyBooster((cur) => (cur === kind ? null : cur));
      }, BOOSTER_COOLDOWN_MS);
    },
    [busyBooster, cmd],
  );

  const boostersDisabled =
    restarting || leaving || finished.current || menuOpen || leaveOpen || postPhase !== 'none';
  const menuDisabled = leaving || finished.current || restarting || postPhase !== 'none';

  const onWinContinue = useCallback(() => {
    setPostPhase('ranking');
  }, []);

  const onRankingContinue = useCallback(() => {
    setPostPhase('none');
    const result = postResult;
    setPostResult(null);
    goHome(navigation);
    if (result) {
      // Keep Victory route available for deep-links / rematch from store.
      setLast(result);
    }
  }, [navigation, postResult, setLast]);

  const onWatchVideo = useCallback(() => {
    setLuckyBusy(true);
    setLuckyNote(null);
    if (!cmd('WatchRewardedAd')) {
      setLuckyBusy(false);
      setLuckyNote('Unity not ready for rewards.');
    }
  }, [cmd]);

  const onGrantShuffle = useCallback(() => {
    setLuckyBusy(true);
    setLuckyNote(null);
    if (!cmd('GrantShuffle', { amount: 2 })) {
      setLuckyBusy(false);
      setLuckyNote('Could not grant Shuffle.');
    } else {
      setTimeout(() => setLuckyBusy(false), 400);
    }
  }, [cmd]);

  const onGrantUndo = useCallback(() => {
    setLuckyBusy(true);
    setLuckyNote(null);
    if (!cmd('GrantUndo', { amount: 2 })) {
      setLuckyBusy(false);
      setLuckyNote('Could not grant Undo.');
    } else {
      setTimeout(() => setLuckyBusy(false), 400);
    }
  }, [cmd]);

  if (Platform.OS === 'web' || nativeMissing) {
    return (
      <View style={styles.center}>
        <Text style={typography.hero}>Unity Embed</Text>
        <Text style={[typography.body, styles.body]}>
          Expo Go mein Unity embed nahi chalta. Ek native APK banao:
          {'\n\n'}1) Unity → Export Android Library → MatchIQ_App/unity/builds/android
          {'\n'}2) npx expo prebuild
          {'\n'}3) npx expo run:android
        </Text>
        <PrimaryButton
          title="Simulate Victory (Dev)"
          onPress={() => void finishWithResult(true)}
          style={styles.btn}
        />
        <SecondaryButton
          title="Simulate Defeat (Dev)"
          onPress={() => void finishWithResult(false)}
          style={styles.btn}
        />
        <SecondaryButton title="Back" onPress={() => navigation.goBack()} style={styles.btn} />
      </View>
    );
  }

  if (!UnityView) {
    return <Loader fullScreen label="Loading match…" />;
  }

  return (
    <View style={styles.root} pointerEvents="box-none">
      <UnityView
        ref={unityRef as any}
        style={styles.unity}
        fullScreen
        androidKeepPlayerMounted
        onUnityMessage={onUnityMessage}
        pointerEvents="auto"
      />

      {/*
        Do NOT wrap HUD in a full-screen elevated View — on Android that steals centre taps
        from the native Unity surface even with pointerEvents="box-none". Only pin top/bottom.
      */}
      {postPhase === 'none' ? (
      <>
      <View
        style={[styles.topBar, { paddingTop: Math.max(insets.top, 10) }]}
        pointerEvents="box-none"
        collapsable={false}
      >
        {/*
          Visual HUD card is pointerEvents="none" so Android never steals board taps
          through the gold bar / stats. ONLY the Menu Pressable is interactive.
        */}
        <View style={styles.topBarInner} pointerEvents="none">
          <HudStat label="LEVEL" value={String(liveLevel)} />
          <HudStat label="SCORE" value={String(liveScore)} />
          <HudStat
            label="TIME"
            value={liveTimer}
            valueStyle={timerUrgent ? styles.timerUrgent : styles.timerValue}
            accent
          />
          <HudStat label="MATCHES" value={String(liveMatches)} />
          {/* Spacer keeps layout width for the menu button sitting on top */}
          <View style={{ width: menuBtnSize, height: menuBtnSize }} />
        </View>
        <Pressable
          style={({ pressed }) => [
            styles.menuBtn,
            styles.menuBtnOverlay,
            { width: menuBtnSize, height: menuBtnSize, borderRadius: menuBtnSize / 2 },
            pressed && styles.pressed,
            (menuDisabled || menuOpen) && styles.btnDisabled,
          ]}
          onPress={openMenu}
          disabled={menuDisabled || menuOpen}
          accessibilityLabel="Menu"
          accessibilityState={{ disabled: menuDisabled || menuOpen }}
          hitSlop={8}
          pointerEvents="auto"
        >
          <View style={styles.menuBtnInner} pointerEvents="none">
            <Text style={[styles.menuWxoLetter, { fontSize: Math.round(menuBtnSize * 0.22) }]}>W</Text>
            <Text
              style={[
                styles.menuWxoLetter,
                styles.menuWxoX,
                { fontSize: Math.round(menuBtnSize * 0.22) },
              ]}
            >
              X
            </Text>
            <Text style={[styles.menuWxoLetter, { fontSize: Math.round(menuBtnSize * 0.22) }]}>O</Text>
          </View>
        </Pressable>
      </View>

      <View
        style={[styles.bottomBar, { paddingBottom: Math.max(insets.bottom, 16) }]}
        pointerEvents="box-none"
        collapsable={false}
      >
        <EmeraldAction
          size={boosterSize}
          icon="⇄"
          label="Shuffle"
          busy={busyBooster === 'shuffle'}
          disabled={boostersDisabled || !!busyBooster}
          onPress={() => runBooster('shuffle', 'ShuffleTiles')}
        />
        <EmeraldAction
          size={boosterSize}
          icon="✦"
          label="Hint"
          busy={busyBooster === 'hint'}
          disabled={boostersDisabled || !!busyBooster}
          onPress={() => runBooster('hint', 'UseHint')}
        />
        <EmeraldAction
          size={boosterSize}
          icon="↶"
          label="Undo"
          busy={busyBooster === 'undo'}
          disabled={boostersDisabled || !!busyBooster}
          onPress={() => runBooster('undo', 'UndoMove')}
        />
      </View>
      </>
      ) : null}

      {postPhase === 'win' && postResult ? (
        <PostGameWinOverlay
          result={postResult}
          onContinue={onWinContinue}
          onWatchVideo={onWatchVideo}
          onGrantShuffle={onGrantShuffle}
          onGrantUndo={onGrantUndo}
          luckyBusy={luckyBusy}
          luckyNote={luckyNote}
        />
      ) : null}

      {postPhase === 'ranking' && postResult ? (
        <PostGameRankingOverlay
          result={postResult}
          rows={rankingRows}
          loading={rankingLoading}
          leagueName={(postResult.league || profile.rank || 'LEAGUE').toUpperCase()}
          leagueEndsAtMs={null}
          onContinue={onRankingContinue}
        />
      ) : null}

      <Modal
        visible={menuOpen && !leaveOpen && postPhase === 'none'}
        transparent
        animationType="fade"
        onRequestClose={closeMenuResume}
        statusBarTranslucent
      >
        <View style={styles.modalOverlay}>
          <View style={styles.menuCard}>
            <View style={styles.menuBrandRow}>
              <Text style={styles.menuBrandLetter}>W</Text>
              <Text style={[styles.menuBrandLetter, styles.menuBrandX]}>X</Text>
              <Text style={styles.menuBrandLetter}>O</Text>
            </View>
            <Text style={styles.menuTitle}>MENU</Text>
            <>
              <MenuRow title="Resume" primary onPress={closeMenuResume} disabled={leaving} />
              {!(mode === 'tournament' && roomId) ? (
                <MenuRow
                  title={restarting ? 'Restarting…' : 'Restart'}
                  onPress={onRestart}
                  disabled={leaving || restarting}
                  busy={restarting}
                />
              ) : null}
              <MenuRow title="Settings" onPress={onSettings} disabled={leaving} />
              <MenuRow title="Leave" danger onPress={onLeaveFromMenu} disabled={leaving} />
              <MenuRow title="Close" onPress={closeMenuResume} disabled={leaving} />
            </>
          </View>
        </View>
      </Modal>

      <Modal
        visible={leaveOpen && postPhase === 'none'}
        transparent
        animationType="fade"
        onRequestClose={cancelLeave}
        statusBarTranslucent
      >
        <View style={styles.modalOverlay}>
          <View style={styles.menuCard}>
            <View style={styles.menuBrandRow}>
              <Text style={styles.menuBrandLetter}>W</Text>
              <Text style={[styles.menuBrandLetter, styles.menuBrandX]}>X</Text>
              <Text style={styles.menuBrandLetter}>O</Text>
            </View>
            <Text style={styles.menuTitle}>Leave Game?</Text>
            <Text style={styles.menuHint}>You will forfeit this match.</Text>
            <MenuRow
              title={leaving ? 'Leaving…' : 'LEAVE'}
              danger
              onPress={confirmLeave}
              disabled={leaving}
              busy={leaving}
            />
            <MenuRow title="CANCEL" primary onPress={cancelLeave} disabled={leaving} />
          </View>
        </View>
      </Modal>
    </View>
  );
}

function HudStat({
  label,
  value,
  valueStyle,
  accent,
}: {
  label: string;
  value: string;
  valueStyle?: object;
  accent?: boolean;
}) {
  return (
    <View style={[styles.statCell, accent && styles.statCellAccent]} pointerEvents="none">
      <Text style={styles.statLabel} numberOfLines={1}>
        {label}
      </Text>
      <Text style={[styles.statValue, valueStyle]} numberOfLines={1} adjustsFontSizeToFit>
        {value}
      </Text>
    </View>
  );
}

function EmeraldAction({
  size,
  icon,
  label,
  onPress,
  disabled,
  busy,
}: {
  size: number;
  icon: string;
  label: string;
  onPress: () => void;
  disabled?: boolean;
  busy?: boolean;
}) {
  return (
    <Pressable
      style={({ pressed }) => [
        styles.actionBtn,
        {
          width: size,
          height: size,
          borderRadius: size / 2,
        },
        pressed && !disabled && styles.pressed,
        (disabled || busy) && styles.actionDisabled,
      ]}
      onPress={onPress}
      disabled={disabled || busy}
      accessibilityLabel={label}
      accessibilityState={{ disabled: !!(disabled || busy), busy: !!busy }}
    >
      {busy ? (
        <ActivityIndicator color={HUD.goldHi} />
      ) : (
        <>
          <Text style={[styles.actionIcon, { fontSize: Math.round(size * 0.34) }]}>{icon}</Text>
          <Text style={styles.actionTxt}>{label}</Text>
        </>
      )}
    </Pressable>
  );
}

function MenuRow({
  title,
  onPress,
  primary,
  danger,
  disabled,
  busy,
}: {
  title: string;
  onPress: () => void;
  primary?: boolean;
  danger?: boolean;
  disabled?: boolean;
  busy?: boolean;
}) {
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled || busy}
      style={({ pressed }) => [
        styles.menuRow,
        primary && styles.menuRowPrimary,
        danger && styles.menuRowDanger,
        pressed && !disabled && styles.pressed,
        (disabled || busy) && styles.btnDisabled,
      ]}
      accessibilityState={{ disabled: !!(disabled || busy), busy: !!busy }}
    >
      {busy ? (
        <ActivityIndicator color={primary || danger ? HUD.white : HUD.wxoRed} />
      ) : (
        <Text
          style={[
            styles.menuRowTxt,
            primary && styles.menuRowTxtPrimary,
            danger && styles.menuRowTxtDanger,
          ]}
        >
          {title}
        </Text>
      )}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: 'transparent' },
  unity: { ...StyleSheet.absoluteFillObject, zIndex: 0 },
  topBar: {
    position: 'absolute',
    left: 0,
    right: 0,
    top: 0,
    zIndex: 20,
    elevation: 0,
    paddingHorizontal: 10,
    paddingBottom: 8,
  },
  topBarInner: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 6,
    backgroundColor: HUD.bg,
    borderRadius: 14,
    borderWidth: 1.5,
    borderColor: HUD.gold,
    paddingVertical: 8,
    paddingHorizontal: 10,
  },
  menuBtn: {
    backgroundColor: HUD.white,
    borderWidth: 2,
    borderColor: HUD.wxoRed,
    alignItems: 'center',
    justifyContent: 'center',
    minWidth: 48,
    minHeight: 48,
  },
  menuBtnOverlay: {
    position: 'absolute',
    right: 10,
    bottom: 8,
    zIndex: 30,
    elevation: 0,
  },
  menuBtnInner: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 1,
  },
  menuWxoLetter: {
    color: HUD.wxoRed,
    fontWeight: '900',
    letterSpacing: -0.5,
  },
  menuWxoX: { color: HUD.wxoRedDark },
  menuTxt: { color: HUD.wxoRed, fontWeight: '800' },
  pressed: { opacity: 0.72, transform: [{ scale: 0.96 }] },
  btnDisabled: { opacity: 0.45 },
  actionDisabled: { opacity: 0.45, backgroundColor: HUD.emeraldDark },
  statCell: {
    flex: 1,
    minWidth: 52,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 2,
  },
  statCellAccent: {
    backgroundColor: 'rgba(5,5,5,0.85)',
    borderRadius: 10,
    borderWidth: 1,
    borderColor: HUD.gold,
    paddingVertical: 4,
    paddingHorizontal: 6,
  },
  statLabel: {
    color: HUD.gold,
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 0.8,
  },
  statValue: {
    color: HUD.white,
    fontSize: 18,
    fontWeight: '800',
    marginTop: 2,
  },
  timerValue: {
    color: HUD.goldHi,
    fontSize: 20,
    fontWeight: '800',
  },
  timerUrgent: {
    color: '#FF6B6B',
    fontSize: 20,
    fontWeight: '800',
  },
  bottomBar: {
    position: 'absolute',
    left: 0,
    right: 0,
    bottom: 0,
    zIndex: 20,
    elevation: 0,
    flexDirection: 'row',
    justifyContent: 'space-evenly',
    alignItems: 'center',
    paddingTop: 12,
  },
  actionBtn: {
    backgroundColor: HUD.emerald,
    borderWidth: 2.5,
    borderColor: HUD.gold,
    alignItems: 'center',
    justifyContent: 'center',
  },
  actionIcon: {
    color: HUD.goldHi,
    fontWeight: '700',
    lineHeight: 40,
    textShadowColor: 'rgba(0,0,0,0.45)',
    textShadowOffset: { width: 0, height: 1 },
    textShadowRadius: 2,
  },
  actionTxt: {
    color: HUD.goldHi,
    fontWeight: '800',
    fontSize: 12,
    letterSpacing: 0.4,
    marginTop: -4,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: HUD.overlay,
    justifyContent: 'center',
    padding: spacing.lg,
  },
  menuCard: {
    backgroundColor: HUD.white,
    borderRadius: radius.lg,
    borderWidth: 1.5,
    borderColor: HUD.wxoRed,
    padding: spacing.lg,
    gap: 10,
    ...Platform.select({
      ios: {
        shadowColor: HUD.wxoRed,
        shadowOffset: { width: 0, height: 4 },
        shadowOpacity: 0.25,
        shadowRadius: 16,
      },
      android: { elevation: 20, shadowColor: HUD.wxoRed },
    }),
  },
  menuBrandRow: {
    flexDirection: 'row',
    alignSelf: 'center',
    gap: 5,
    marginBottom: 2,
  },
  menuBrandLetter: {
    width: 28,
    height: 28,
    borderRadius: 8,
    overflow: 'hidden',
    backgroundColor: HUD.wxoRed,
    color: HUD.white,
    textAlign: 'center',
    textAlignVertical: 'center',
    fontSize: 14,
    fontWeight: '900',
    lineHeight: 28,
  },
  menuBrandX: { backgroundColor: HUD.wxoRedDark },
  menuTitle: {
    color: HUD.wxoInk,
    fontSize: 22,
    fontWeight: '800',
    letterSpacing: 2,
    textAlign: 'center',
    marginBottom: 4,
  },
  menuHint: {
    color: HUD.wxoSoft,
    fontSize: 14,
    textAlign: 'center',
    marginBottom: 6,
  },
  menuRow: {
    borderWidth: 1.5,
    borderColor: HUD.wxoBorder,
    borderRadius: 12,
    paddingVertical: 14,
    minHeight: 52,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: HUD.white,
  },
  menuRowPrimary: {
    backgroundColor: HUD.wxoRed,
    borderColor: HUD.wxoRed,
  },
  menuRowDanger: {
    borderColor: HUD.wxoRed,
    backgroundColor: HUD.wxoRedSoft,
  },
  menuRowTxt: {
    color: HUD.wxoInk,
    fontSize: 16,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
  menuRowTxtPrimary: { color: HUD.white },
  menuRowTxtDanger: { color: HUD.wxoRed },
  center: {
    flex: 1,
    backgroundColor: HUD.black,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.lg,
  },
  body: { textAlign: 'center', marginVertical: spacing.md, color: HUD.white },
  btn: { alignSelf: 'stretch', marginTop: spacing.sm },
});
