/** IQFX Pro Tournament – Pool Play rules. Prize pool = 70% of collection. */

export const PRIZE_SHARE = 0.7;
export const PLATFORM_FEE_SHARE = 0.3;
export const ENTRY_FEES = [10, 50, 100] as const;
export const PLAYER_SIZES = [10, 50, 100, 500, 1000] as const;

export function winnersFor(players: number): number {
  if (players <= 50) return 1;
  if (players <= 100) return 2;
  return 3;
}

export function distributionPercents(winners: number): number[] {
  if (winners === 1) return [100];
  if (winners === 2) return [70, 30];
  if (winners === 3) return [60, 25, 15];
  return [100];
}

export function distributionLabel(winners: number): string {
  const p = distributionPercents(winners);
  if (winners === 1) return '🥇 100%';
  if (winners === 2) return `🥇 ${p[0]}% • 🥈 ${p[1]}%`;
  return `🥇 ${p[0]}% • 🥈 ${p[1]}% • 🥉 ${p[2]}%`;
}

export function prizePool(players: number, entryFee: number): number {
  return players * entryFee * PRIZE_SHARE;
}

export function prizeAmounts(players: number, entryFee: number): Record<string, number> {
  const pool = prizePool(players, entryFee);
  const winners = winnersFor(players);
  const percents = distributionPercents(winners);
  const keys = ['1st', '2nd', '3rd'];
  const map: Record<string, number> = {};
  percents.forEach((pct, i) => {
    map[keys[i]] = pool * (pct / 100);
  });
  return map;
}

export type PoolStructureRow = {
  players: number;
  winners: number;
  prize10: number;
  prize50: number;
  prize100: number;
  distributionLabel: string;
};

export const STRUCTURE_ROWS: PoolStructureRow[] = PLAYER_SIZES.map((p) => ({
  players: p,
  winners: winnersFor(p),
  prize10: prizePool(p, 10),
  prize50: prizePool(p, 50),
  prize100: prizePool(p, 100),
  distributionLabel: distributionLabel(winnersFor(p)),
}));
