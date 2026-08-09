import React, { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Screen, GameHeader, PrimaryButton } from '../../components';
import { colors, spacing, typography } from '../../theme';
import { formatRupee } from '../../utils';
import { useUiStore } from '../../store';
import { poolApi, type Pool } from '../../api';

type Props = NativeStackScreenProps<any>;

function distLabel(dist: number[]): string {
  const medals = ['🥇', '🥈', '🥉'];
  return dist.map((p, i) => `${medals[i] ?? '🏅'} ${p}%`).join(' • ');
}

export function PoolsScreen(_props: Props) {
  const showToast = useUiStore((s) => s.showToast);
  const [pools, setPools] = useState<Pool[]>([]);
  const [balance, setBalance] = useState(0);
  const [loading, setLoading] = useState(true);
  const [joiningId, setJoiningId] = useState<number | null>(null);

  const load = useCallback(async () => {
    try {
      const { pools: list, wallet_balance } = await poolApi.list();
      setPools(list);
      setBalance(wallet_balance);
    } catch (e) {
      showToast((e as Error).message || 'Could not load pools', 'danger');
    } finally {
      setLoading(false);
    }
  }, [showToast]);

  useEffect(() => {
    load();
  }, [load]);

  const join = async (pool: Pool) => {
    if (joiningId) return;
    setJoiningId(pool.id);
    try {
      const { wallet_balance, pool: updated } = await poolApi.join(pool.id);
      setBalance(wallet_balance);
      setPools((prev) => prev.map((p) => (p.id === updated.id ? updated : p)));
      showToast(`Joined ${updated.name}`, 'success');
    } catch (err: any) {
      const detail = err?.response?.data?.detail || 'Could not join pool';
      showToast(detail, 'danger');
    } finally {
      setJoiningId(null);
    }
  };

  return (
    <Screen>
      <GameHeader title="IQFX Pro Pools" showBack compact />
      <View style={styles.balanceRow}>
        <Text style={typography.body}>Wallet</Text>
        <Text style={[typography.h3, { color: colors.primaryGold }]}>{formatRupee(balance)}</Text>
      </View>

      {loading ? (
        <ActivityIndicator color={colors.primaryGold} style={{ marginTop: spacing.xl }} />
      ) : (
        <ScrollView
          contentContainerStyle={{ paddingBottom: spacing.xl }}
          refreshControl={<RefreshControl refreshing={false} onRefresh={load} tintColor={colors.primaryGold} />}
        >
          {pools.length === 0 ? (
            <Text style={[typography.body, { textAlign: 'center', marginTop: spacing.xl }]}>
              No open pools right now. Check back soon.
            </Text>
          ) : (
            pools.map((pool) => {
              const joined = !!pool.my_entry;
              const full = pool.players_joined >= pool.max_players;
              return (
                <View key={pool.id} style={styles.card}>
                  <View style={styles.cardHead}>
                    <Text style={styles.icon}>{pool.icon}</Text>
                    <View style={{ flex: 1 }}>
                      <Text style={typography.h3}>{pool.name}</Text>
                      <Text style={typography.caption}>
                        {pool.players_joined}/{pool.max_players} players · {pool.winners_count} winner
                        {pool.winners_count > 1 ? 's' : ''}
                      </Text>
                    </View>
                  </View>

                  <View style={styles.statsRow}>
                    <Stat label="Entry" value={formatRupee(pool.entry_fee)} />
                    <Stat label="Prize Pool" value={formatRupee(pool.prize_pool)} gold />
                    <Stat label="Top Win" value={formatRupee(pool.prize_breakdown[0] ?? pool.prize_pool)} />
                  </View>

                  <Text style={styles.dist}>{distLabel(pool.distribution)}</Text>

                  {joined ? (
                    <View style={styles.joinedTag}>
                      <Text style={styles.joinedText}>
                        ✓ Joined · {pool.my_entry?.status?.toUpperCase()}
                        {pool.my_entry?.rank ? ` · Rank ${pool.my_entry.rank}` : ''}
                        {pool.my_entry?.prize ? ` · Won ${formatRupee(pool.my_entry.prize)}` : ''}
                      </Text>
                    </View>
                  ) : (
                    <PrimaryButton
                      title={full ? 'Pool Full' : `Join · ${formatRupee(pool.entry_fee)}`}
                      disabled={full || joiningId === pool.id}
                      loading={joiningId === pool.id}
                      onPress={() => join(pool)}
                    />
                  )}
                </View>
              );
            })
          )}

          <Text style={styles.note}>
            Prize Pool = 70% of total collection. Top scores share the pool per the distribution.
          </Text>
        </ScrollView>
      )}
    </Screen>
  );
}

function Stat({ label, value, gold }: { label: string; value: string; gold?: boolean }) {
  return (
    <View style={styles.stat}>
      <Text style={styles.statLabel}>{label}</Text>
      <Text style={[styles.statValue, gold && { color: colors.primaryGold }]}>{value}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  balanceRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.md,
  },
  card: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.md,
  },
  cardHead: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, marginBottom: spacing.sm },
  icon: { fontSize: 26 },
  statsRow: { flexDirection: 'row', justifyContent: 'space-between', marginBottom: spacing.sm },
  stat: { flex: 1 },
  statLabel: { color: colors.textMuted, fontSize: 11, fontWeight: '700' },
  statValue: { color: colors.textPrimary, fontWeight: '800', fontSize: 15 },
  dist: { color: colors.primaryGold, fontSize: 13, marginBottom: spacing.md },
  joinedTag: {
    borderWidth: 1,
    borderColor: colors.neonBlue,
    borderRadius: 12,
    padding: spacing.sm,
    alignItems: 'center',
  },
  joinedText: { color: colors.neonBlue, fontWeight: '700' },
  note: { color: colors.textMuted, fontSize: 12, textAlign: 'center', marginTop: spacing.sm },
});
