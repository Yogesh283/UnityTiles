import * as Linking from 'expo-linking';
import { Platform } from 'react-native';
import * as IntentLauncher from 'expo-intent-launcher';
import { RESULT_SCHEME, UNITY_ANDROID_PACKAGE, UNITY_SCHEME } from '../constants';
import type { MatchResultPayload, UnityLaunchPayload } from '../types';

function buildQuery(payload: UnityLaunchPayload): string {
  const params = new URLSearchParams({
    matchId: payload.matchId,
    mode: payload.mode,
    token: payload.token,
  });
  if (payload.tournamentId) params.set('tournamentId', payload.tournamentId);
  if (payload.levelId) params.set('levelId', payload.levelId);
  if (payload.seed) params.set('seed', payload.seed);
  return params.toString();
}

export function buildUnityLaunchUrl(payload: UnityLaunchPayload): string {
  return `${UNITY_SCHEME}?${buildQuery(payload)}`;
}

export function parseMatchResultUrl(url: string): MatchResultPayload | null {
  try {
    const parsed = Linking.parse(url);
    if (parsed.scheme !== 'matchiq') return null;
    if (parsed.hostname !== 'match-result' && parsed.path !== 'match-result') {
      const path = (parsed.path || '').replace(/^\//, '');
      if (path !== 'match-result' && parsed.hostname !== 'match-result') return null;
    }
    const q = parsed.queryParams || {};
    const score = Number(q.score ?? 0);
    const won = String(q.won ?? 'false') === 'true' || String(q.won) === '1';
    return {
      matchId: String(q.matchId ?? `match-${Date.now()}`),
      tournamentId: q.tournamentId ? String(q.tournamentId) : undefined,
      roomId: q.roomId ? String(q.roomId) : undefined,
      won,
      score,
      timeSeconds: Number(q.timeSeconds ?? 0),
      accuracy: Number(q.accuracy ?? q.iq ?? 0),
      iq: q.iq != null ? Number(q.iq) : q.accuracy != null ? Number(q.accuracy) : undefined,
      combo: q.combo != null ? Number(q.combo) : undefined,
      message: q.message ? String(q.message) : undefined,
      level: q.level != null ? Number(q.level) : undefined,
      coinsEarned: Number(q.coinsEarned ?? 0),
      xpEarned: Number(q.xpEarned ?? 0),
      opponentName: q.opponentName ? String(q.opponentName) : undefined,
    };
  } catch {
    return null;
  }
}

/** Parse JSON payload sent from embedded Unity via onUnityMessage. */
export function parseMatchResultJson(raw: string): MatchResultPayload | null {
  try {
    const q = JSON.parse(raw) as Record<string, unknown>;
    if (q == null || typeof q !== 'object') return null;
    const type = q.type ? String(q.type) : '';
    if (
      type &&
      type !== 'match-result' &&
      type !== 'LevelCompleted' &&
      type !== 'GameOver'
    ) {
      // allow plain result objects without type
      if (q.won == null && q.score == null) return null;
    }
    const won = String(q.won ?? 'false') === 'true' || q.won === true || String(q.won) === '1';
    return {
      matchId: String(q.matchId ?? `match-${Date.now()}`),
      tournamentId: q.tournamentId ? String(q.tournamentId) : undefined,
      roomId: q.roomId ? String(q.roomId) : undefined,
      won,
      score: Number(q.score ?? 0),
      timeSeconds: Number(q.timeSeconds ?? 0),
      accuracy: Number(q.accuracy ?? q.iq ?? 0),
      iq: q.iq != null ? Number(q.iq) : q.accuracy != null ? Number(q.accuracy) : undefined,
      combo: q.combo != null ? Number(q.combo) : undefined,
      message: q.message ? String(q.message) : undefined,
      level: q.level != null ? Number(q.level) : undefined,
      coinsEarned: Number(q.coinsEarned ?? 0),
      xpEarned: Number(q.xpEarned ?? 0),
      opponentName: q.opponentName ? String(q.opponentName) : undefined,
    };
  } catch {
    return null;
  }
}

async function isUnityInstalled(): Promise<boolean> {
  if (Platform.OS !== 'android') return false;
  try {
    // Throws / fails when package is missing
    await IntentLauncher.getApplicationIconAsync(UNITY_ANDROID_PACKAGE);
    return true;
  } catch {
    return false;
  }
}

/**
 * Opens Unity APK only — never bare matchiqunity:// (that shows Expo Go chooser).
 */
async function tryAndroidUnityLaunch(payload: UnityLaunchPayload): Promise<boolean> {
  if (Platform.OS !== 'android') return false;

  const installed = await isUnityInstalled();
  if (!installed) return false;

  const deepLink = buildUnityLaunchUrl(payload);

  // 1) Deep link locked to Unity package (new builds with matchiqunity://)
  try {
    await IntentLauncher.startActivityAsync('android.intent.action.VIEW', {
      data: deepLink,
      packageName: UNITY_ANDROID_PACKAGE,
    });
    return true;
  } catch {
    // continue — older APK may not have the scheme
  }

  // 2) Open Unity by package name (no chooser, no Expo Go)
  try {
    IntentLauncher.openApplication(UNITY_ANDROID_PACKAGE);
    return true;
  } catch {
    // continue
  }

  // 3) MAIN launcher activity on Unity package
  try {
    await IntentLauncher.startActivityAsync('android.intent.action.MAIN', {
      category: 'android.intent.category.LAUNCHER',
      packageName: UNITY_ANDROID_PACKAGE,
    });
    return true;
  } catch {
    return false;
  }
}

/**
 * Opens the installed Unity gameplay APK.
 * Returns 'opened' if Unity was launched, otherwise 'fallback'.
 */
export async function launchUnityMatch(payload: UnityLaunchPayload): Promise<'opened' | 'fallback'> {
  if (Platform.OS === 'android') {
    // Do NOT Linking.openURL(matchiqunity://) — Android shows Expo Go chooser.
    if (await tryAndroidUnityLaunch(payload)) return 'opened';
    return 'fallback';
  }

  // iOS: scheme only if Unity registered it
  try {
    const url = buildUnityLaunchUrl(payload);
    const can = await Linking.canOpenURL(url);
    if (can) {
      await Linking.openURL(url);
      return 'opened';
    }
  } catch {
    // fallback
  }
  return 'fallback';
}

export function createSimulatedResult(matchId: string, tournamentId?: string): MatchResultPayload {
  const won = Math.random() > 0.35;
  return {
    matchId,
    tournamentId,
    won,
    score: won ? 1200 + Math.floor(Math.random() * 600) : 700 + Math.floor(Math.random() * 400),
    timeSeconds: 90 + Math.floor(Math.random() * 120),
    accuracy: won ? 88 + Math.floor(Math.random() * 12) : 60 + Math.floor(Math.random() * 25),
    coinsEarned: won ? 150 + Math.floor(Math.random() * 200) : 20,
    xpEarned: won ? 80 + Math.floor(Math.random() * 40) : 25,
    opponentName: 'TempleFox',
  };
}

export type UnityCommand =
  | 'PauseGame'
  | 'ResumeGame'
  | 'RestartGame'
  | 'UseHint'
  | 'ShuffleTiles'
  | 'UndoMove'
  | 'ExitGame'
  | 'GrantShuffle'
  | 'GrantUndo'
  | 'WatchRewardedAd';

export type UnityEventType =
  | 'GameStarted'
  | 'GamePaused'
  | 'ScoreUpdated'
  | 'MatchesUpdated'
  | 'TimerUpdated'
  | 'LevelCompleted'
  | 'GameOver'
  | 'ExitRequested'
  | 'RewardGranted'
  | 'match-result';

/** postMessage target on the Unity side (DontDestroyOnLoad GO). */
export const UNITY_BRIDGE_GO = 'MatchIQShellBridge';

export function sendUnityCommand(
  unity: { postMessage: (go: string, method: string, message: string) => void } | null | undefined,
  command: UnityCommand,
  opts?: { amount?: number },
): boolean {
  if (!unity?.postMessage) return false;
  try {
    const amount = opts?.amount;
    const msg = amount != null ? String(amount) : command;
    // Non-empty message — some Android UnitySendMessage builds drop "" payloads.
    unity.postMessage(UNITY_BRIDGE_GO, command, msg);
    unity.postMessage(
      UNITY_BRIDGE_GO,
      'OnReactNativeCommand',
      JSON.stringify({ command, amount: amount ?? 1 }),
    );
    return true;
  } catch {
    return false;
  }
}

export function parseUnityEvent(raw: string): { type: string; payload: Record<string, unknown> } | null {
  if (!raw) return null;
  try {
    const json = JSON.parse(raw) as Record<string, unknown>;
    if (json && typeof json === 'object' && json.type) {
      return { type: String(json.type), payload: json };
    }
  } catch {
    // not JSON
  }
  if (raw.startsWith('matchiq://') || raw.includes('match-result')) {
    return { type: 'match-result', payload: { raw } };
  }
  return null;
}

export const unityBridge = {
  launchUnityMatch,
  parseMatchResultUrl,
  parseMatchResultJson,
  createSimulatedResult,
  sendUnityCommand,
  parseUnityEvent,
  resultScheme: RESULT_SCHEME,
  isUnityInstalled,
};
