/** Bridge: Unity match result → WebView shared WXO wallet */
export type PendingWxoMatchResult = {
  won: boolean;
  prize: number;
  game: string;
  matchId: string;
};

let pending: PendingWxoMatchResult | null = null;

export function setPendingWxoMatchResult(result: PendingWxoMatchResult | null) {
  pending = result;
}

export function consumePendingWxoMatchResult(): PendingWxoMatchResult | null {
  const v = pending;
  pending = null;
  return v;
}
