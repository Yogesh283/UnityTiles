import React, { useCallback, useEffect, useState } from 'react';
import { Pressable, Share, StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Screen, GameHeader, PrimaryButton, SecondaryButton, Loader } from '../../components';
import { colors, spacing, typography } from '../../theme';
import { formatRupee } from '../../utils';
import { useUiStore } from '../../store';
import { referralApi } from '../../api/referralApi';
import type { IncomePeriod, ReferralEarning, ReferralMe } from '../../api/referralApi';

type Props = NativeStackScreenProps<any>;

const PERIODS: { key: IncomePeriod; label: string }[] = [
  { key: 'today', label: 'Today' },
  { key: 'week', label: '7 Days' },
  { key: 'month', label: '30 Days' },
  { key: 'total', label: 'All Time' },
];

const LEVEL_LABEL: Record<number, string> = {
  1: 'Direct Referrals',
  2: '2nd-Level Team',
  3: '3rd-Level Team',
  4: '4th-Level Team',
  5: '5th-Level Team',
  6: '6th-Level Team',
};

export function IncomeScreen({ navigation }: Props) {
  const showToast = useUiStore((s) => s.showToast);
  const [data, setData] = useState<ReferralMe | null>(null);
  const [earnings, setEarnings] = useState<ReferralEarning[]>([]);
  const [period, setPeriod] = useState<IncomePeriod>('today');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      const [me, recent] = await Promise.all([referralApi.me(), referralApi.earnings(20)]);
      setData(me);
      setEarnings(recent);
    } catch (e) {
      showToast((e as Error).message || 'Income load failed', 'danger');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [showToast]);

  useEffect(() => {
    load();
  }, [load]);

  const onRefresh = () => {
    setRefreshing(true);
    load();
  };

  const income = data?.income;
  const periodPoints = income ? income[period] : 0;
  const teamCounts = data?.team_counts ?? [];

  const share = () => {
    if (!data) return;
    Share.share({
      message: `Join WXO with my referral code ${data.referral_code}\n${data.referral_link}`,
    });
  };

  return (
    <Screen scroll refreshing={refreshing} onRefresh={onRefresh}>
      <GameHeader title="My Income" showBack compact />

      <Text style={typography.h2}>WXO Points</Text>
      <Text style={[typography.caption, { marginBottom: spacing.md }]}>
        6-Level Tournament Rewards · 10% total distribution
      </Text>

      {loading ? <Loader label="Loading income…" /> : null}

      <View style={styles.heroCard}>
        <Text style={styles.heroLabel}>{PERIODS.find((p) => p.key === period)?.label} Earnings</Text>
        <Text style={styles.heroValue}>{formatRupee(periodPoints)}</Text>
        <View style={styles.periodRow}>
          {PERIODS.map((p) => {
            const on = p.key === period;
            return (
              <Pressable
                key={p.key}
                onPress={() => setPeriod(p.key)}
                style={[styles.periodChip, on && styles.periodChipOn]}
              >
                <Text style={[styles.periodText, on && styles.periodTextOn]}>{p.label}</Text>
              </Pressable>
            );
          })}
        </View>
      </View>

      <View style={styles.statRow}>
        <Stat label="Today" value={formatRupee(income?.today ?? 0)} highlight />
        <Stat label="All Time" value={formatRupee(income?.total ?? 0)} />
        <Stat label="Team" value={String(data?.team_size ?? 0)} />
      </View>

      <View style={styles.panel}>
        <Text style={[typography.h3, { marginBottom: spacing.sm }]}>Level Breakdown</Text>
        <View style={styles.tableHead}>
          <Text style={[styles.th, { flex: 2.2 }]}>Level</Text>
          <Text style={styles.th}>Rate</Text>
          <Text style={styles.th}>Team</Text>
          <Text style={[styles.th, { textAlign: 'right' }]}>Points</Text>
        </View>
        {(income?.by_level ?? []).map((row) => {
          const members = teamCounts.find((t) => t.level === row.level)?.members ?? 0;
          return (
            <View key={row.level} style={styles.tableRow}>
              <View style={{ flex: 2.2 }}>
                <Text style={styles.levelName}>L{row.level}</Text>
                <Text style={styles.levelSub}>{LEVEL_LABEL[row.level]}</Text>
              </View>
              <Text style={styles.td}>{row.percent}%</Text>
              <Text style={styles.td}>{members}</Text>
              <Text style={[styles.td, styles.tdGold, { textAlign: 'right' }]}>
                {formatRupee(row.points)}
              </Text>
            </View>
          );
        })}
        <View style={styles.totalRow}>
          <Text style={typography.bodyStrong}>Total · 10%</Text>
          <Text style={styles.totalValue}>{formatRupee(income?.total ?? 0)}</Text>
        </View>
      </View>

      <View style={styles.panel}>
        <Text style={[typography.h3, { marginBottom: spacing.xs }]}>Your Referral Code</Text>
        <Text style={styles.code}>{data?.referral_code ?? '—'}</Text>
        <Text style={typography.caption}>{data?.referral_link ?? ''}</Text>
        <PrimaryButton title="Share Invite" onPress={share} style={{ marginTop: spacing.sm }} />
        <SecondaryButton
          title="Copy Code"
          onPress={() => showToast(`Code ${data?.referral_code ?? ''} copied`, 'success')}
          style={{ marginTop: spacing.sm }}
        />
      </View>

      <View style={styles.panel}>
        <Text style={[typography.h3, { marginBottom: spacing.sm }]}>Recent Earnings</Text>
        {earnings.length === 0 ? (
          <Text style={typography.caption}>
            Abhi koi income nahi. Team member ke tournament entry fee pay karte hi WXO Points milenge.
          </Text>
        ) : (
          earnings.map((e) => (
            <View key={e.id} style={styles.earnRow}>
              <View style={{ flex: 1 }}>
                <Text style={typography.bodyStrong}>{e.from_name}</Text>
                <Text style={typography.caption}>
                  L{e.level} · {e.percent}% of {formatRupee(e.entry_fee)}
                </Text>
              </View>
              <Text style={styles.earnPoints}>+{formatRupee(e.points)}</Text>
            </View>
          ))
        )}
      </View>

      <View style={styles.panel}>
        <Text style={[typography.h3, { marginBottom: spacing.sm }]}>How It Works</Text>
        <Text style={styles.notes}>
          01 — Refer: apna referral link share karo{'\n'}
          02 — Build Team: network 6 level tak grow hota hai{'\n'}
          03 — Play: team members eligible tournaments khelte hain{'\n'}
          04 — Earn: entry fee pay hote hi WXO Points milte hain
        </Text>
        <Text style={[styles.notes, { marginTop: spacing.sm }]}>
          Sirf registration se reward nahi milta — eligible tournament entry fee successfully pay honi
          chahiye.
        </Text>
      </View>
    </Screen>
  );
}

function Stat({ label, value, highlight }: { label: string; value: string; highlight?: boolean }) {
  return (
    <View style={styles.stat}>
      <Text style={styles.statLabel}>{label}</Text>
      <Text style={[styles.statValue, highlight && styles.statGold]}>{value}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  heroCard: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 18,
    borderWidth: 1,
    borderColor: colors.borderPurple,
    padding: spacing.md,
    marginBottom: spacing.md,
  },
  heroLabel: { color: colors.textSecondary, fontSize: 13 },
  heroValue: {
    color: colors.primaryGold,
    fontSize: 36,
    fontWeight: '900',
    marginVertical: spacing.xs,
  },
  periodRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginTop: spacing.sm },
  periodChip: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 6,
  },
  periodChipOn: { borderColor: colors.neonPurple, backgroundColor: '#3A1013' },
  periodText: { color: colors.textSecondary, fontSize: 12, fontWeight: '700' },
  periodTextOn: { color: colors.white },
  statRow: { flexDirection: 'row', gap: spacing.sm, marginBottom: spacing.md },
  stat: {
    flex: 1,
    backgroundColor: colors.surface,
    borderRadius: 14,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.sm,
  },
  statLabel: { color: colors.textMuted, fontSize: 11, fontWeight: '700' },
  statValue: { color: colors.textPrimary, fontSize: 16, fontWeight: '800', marginTop: 2 },
  statGold: { color: colors.primaryGold },
  panel: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.md,
  },
  tableHead: { flexDirection: 'row', marginBottom: 8 },
  th: { flex: 1, color: colors.textMuted, fontSize: 11, fontWeight: '700' },
  tableRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 8,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  td: { flex: 1, color: colors.textPrimary, fontSize: 13, fontWeight: '600' },
  tdGold: { color: colors.primaryGold, fontWeight: '800' },
  levelName: { color: colors.textPrimary, fontWeight: '800' },
  levelSub: { color: colors.textMuted, fontSize: 11 },
  totalRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: spacing.sm,
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: colors.borderGold,
  },
  totalValue: { color: colors.primaryGold, fontSize: 18, fontWeight: '900' },
  code: {
    color: colors.goldLight,
    fontSize: 26,
    fontWeight: '900',
    letterSpacing: 3,
    marginVertical: spacing.xs,
  },
  earnRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 8,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  earnPoints: { color: colors.accentGreen, fontWeight: '800' },
  notes: { color: colors.textSecondary, fontSize: 13, lineHeight: 20 },
});
