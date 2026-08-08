import { Dimensions } from 'react-native';
import { layout } from '../theme';

export function formatCoins(value: number): string {
  if (value >= 1_000_000) return `₹${(value / 1_000_000).toFixed(1)}M`;
  if (value >= 10_000) return `₹${(value / 1000).toFixed(1)}K`;
  return `₹${value.toLocaleString('en-IN')}`;
}

export function formatRupee(value: number): string {
  return `₹${value.toLocaleString('en-IN')}`;
}

export function formatTimeAgo(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 60) return `${Math.max(1, mins)}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

export function useIsTablet(): boolean {
  const { width } = Dimensions.get('window');
  return width >= 768;
}

export function contentMaxWidth(width: number): number {
  return Math.min(width, layout.maxContentWidth);
}

export function statusLabel(status: string): string {
  return status.replace(/_/g, ' ').toUpperCase();
}

export function formatCountdown(minutes?: number): string {
  const m = minutes ?? 0;
  const h = Math.floor(m / 60);
  const mm = m % 60;
  if (h > 0) return `${h}h ${mm}m`;
  return `${mm}m`;
}
