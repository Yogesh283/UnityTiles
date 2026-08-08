import { apiClient } from './client';
import type { AuthSession, PlayerProfile } from '../types';

export type TokenResponse = {
  access_token: string;
  token_type: string;
  user_id: number;
  user_uuid: string;
  display_name: string;
};

function toSession(token: TokenResponse, email?: string): AuthSession {
  const user: PlayerProfile = {
    id: String(token.user_id),
    username: email?.split('@')[0] || token.display_name.toLowerCase().replace(/\s+/g, '_'),
    displayName: token.display_name || 'Player',
    avatarId: 'avatar_default',
    frameId: 'frame_gold',
    level: 1,
    xp: 0,
    xpToNext: 1000,
    rank: 'Rookie',
    referralCode: token.user_uuid.slice(0, 8).toUpperCase(),
    wins: 0,
    losses: 0,
    winRate: 0,
  };
  return {
    token: token.access_token,
    refreshToken: token.access_token,
    user,
  };
}

function apiError(error: unknown, fallback: string): Error {
  const ax = error as { response?: { data?: { detail?: string | { msg?: string }[] } }; message?: string };
  const detail = ax.response?.data?.detail;
  if (typeof detail === 'string') return new Error(detail);
  if (Array.isArray(detail) && detail[0]?.msg) return new Error(detail[0].msg);
  return new Error(ax.message || fallback);
}

/** Real Game DB auth via Backend FastAPI `/api/v1/auth/*` */
export const authApi = {
  async register(email: string, password: string, displayName: string): Promise<AuthSession> {
    try {
      const { data } = await apiClient.post<TokenResponse>('/auth/register', {
        email,
        password,
        display_name: displayName || 'Player',
      });
      return toSession(data, email);
    } catch (e) {
      throw apiError(e, 'Register failed');
    }
  },

  async login(email: string, password: string): Promise<AuthSession> {
    try {
      const { data } = await apiClient.post<TokenResponse>('/auth/login', { email, password });
      return toSession(data, email);
    } catch (e) {
      throw apiError(e, 'Login failed');
    }
  },

  async guest(displayName = 'Guest'): Promise<AuthSession> {
    try {
      const guestId = `guest-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
      const { data } = await apiClient.post<TokenResponse>('/auth/guest', {
        guest_id: guestId,
        display_name: displayName,
      });
      return toSession(data);
    } catch (e) {
      throw apiError(e, 'Guest login failed');
    }
  },

  async google(googleId: string, email: string, displayName: string): Promise<AuthSession> {
    try {
      const { data } = await apiClient.post<TokenResponse>('/auth/google', {
        google_id: googleId,
        email,
        display_name: displayName,
      });
      return toSession(data, email);
    } catch (e) {
      throw apiError(e, 'Google login failed');
    }
  },

  async me(): Promise<{
    id: number;
    user_uuid: string;
    email: string | null;
    display_name: string;
    is_guest: boolean;
    avatar_url: string | null;
  }> {
    const { data } = await apiClient.get('/auth/me');
    return data;
  },
};
