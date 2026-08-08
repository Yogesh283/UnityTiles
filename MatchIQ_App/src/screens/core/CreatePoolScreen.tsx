import React, { useMemo, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Screen, GameHeader, PrimaryButton, SecondaryButton, PremiumInput } from '../../components';
import { colors, spacing, typography } from '../../theme';
import { formatRupee } from '../../utils';
import { useUiStore } from '../../store';
import {
  ENTRY_FEES,
  PLAYER_SIZES,
  STRUCTURE_ROWS,
  distributionLabel,
  prizeAmounts,
  prizePool,
  winnersFor,
} from '../../constants/poolRules';

type Props = NativeStackScreenProps<any>;

function applyPreset(players: number, entry: number) {
  const w = winnersFor(players);
  const percents =
    w === 1 ? [100, 0, 0] : w === 2 ? [70, 30, 0] : [60, 25, 15];
  return {
    players: String(players),
    winners: String(w),
    entry: String(entry),
    first: String(percents[0]),
    second: String(percents[1]),
    third: String(percents[2]),
    others: '0',
  };
}

export function CreatePoolScreen({ navigation }: Props) {
  const showToast = useUiStore((s) => s.showToast);
  const initial = applyPreset(10, 10);
  const [players, setPlayers] = useState(initial.players);
  const [winners, setWinners] = useState(initial.winners);
  const [entry, setEntry] = useState(initial.entry);
  const [first, setFirst] = useState(initial.first);
  const [second, setSecond] = useState(initial.second);
  const [third, setThird] = useState(initial.third);
  const [others, setOthers] = useState(initial.others);
  const [notes, setNotes] = useState('');

  const pNum = Number(players) || 0;
  const eNum = Number(entry) || 0;
  const pool = useMemo(() => prizePool(pNum, eNum), [pNum, eNum]);
  const amounts = useMemo(() => prizeAmounts(pNum || 10, eNum || 10), [pNum, eNum]);
  const wNum = Number(winners) || winnersFor(pNum || 10);

  const pickPlayers = (size: number) => {
    const next = applyPreset(size, eNum || 10);
    setPlayers(next.players);
    setWinners(next.winners);
    setFirst(next.first);
    setSecond(next.second);
    setThird(next.third);
    setOthers(next.others);
  };

  const pickEntry = (fee: number) => {
    const next = applyPreset(pNum || 10, fee);
    setEntry(next.entry);
    setWinners(next.winners);
    setFirst(next.first);
    setSecond(next.second);
    setThird(next.third);
  };

  const reset = () => {
    const next = applyPreset(10, 10);
    setPlayers(next.players);
    setWinners(next.winners);
    setEntry(next.entry);
    setFirst(next.first);
    setSecond(next.second);
    setThird(next.third);
    setOthers(next.others);
    setNotes('');
  };

  return (
    <Screen scroll>
      <GameHeader title="Create Pool" showBack compact />
      <Text style={typography.h2}>🏆 IQFX Pro Tournament</Text>
      <Text style={[typography.body, { marginBottom: spacing.md }]}>
        Prize Pool = 70% of total collection
      </Text>

      <Text style={[typography.label, { marginBottom: spacing.xs }]}>Entry Fee</Text>
      <View style={styles.chips}>
        {ENTRY_FEES.map((fee) => {
          const selected = eNum === fee;
          return (
            <Pressable
              key={fee}
              onPress={() => pickEntry(fee)}
              style={[styles.chip, selected && styles.chipOn]}
            >
              <Text style={[styles.chipText, selected && styles.chipTextOn]}>₹{fee}</Text>
            </Pressable>
          );
        })}
      </View>

      <Text style={[typography.label, { marginBottom: spacing.xs }]}>Players</Text>
      <View style={styles.chips}>
        {PLAYER_SIZES.map((size) => {
          const selected = pNum === size;
          return (
            <Pressable
              key={size}
              onPress={() => pickPlayers(size)}
              style={[styles.chip, selected && styles.chipBlue]}
            >
              <Text style={[styles.chipText, selected && styles.chipTextOn]}>{size}</Text>
            </Pressable>
          );
        })}
      </View>

      <View style={styles.panel}>
        <Row label="👥 Players" value={String(pNum)} />
        <Row label="🏆 Winners" value={String(wNum)} />
        <Row label="💰 Entry Fee" value={formatRupee(eNum)} />
        <Row label="🎁 Prize Pool" value={formatRupee(Math.round(pool))} highlight />
      </View>

      <View style={styles.panel}>
        <Text style={[typography.h3, { marginBottom: spacing.sm }]}>📈 Prize Distribution</Text>
        <Text style={styles.distLabel}>{distributionLabel(wNum)}</Text>
        {Object.entries(amounts).map(([k, v]) => (
          <Row key={k} label={k} value={formatRupee(Math.round(v))} />
        ))}
        <PremiumInput label="🥇 1st %" value={first} onChangeText={setFirst} keyboardType="number-pad" />
        {wNum >= 2 ? (
          <PremiumInput label="🥈 2nd %" value={second} onChangeText={setSecond} keyboardType="number-pad" />
        ) : null}
        {wNum >= 3 ? (
          <PremiumInput label="🥉 3rd %" value={third} onChangeText={setThird} keyboardType="number-pad" />
        ) : null}
        <PremiumInput label="🏅 Others %" value={others} onChangeText={setOthers} keyboardType="number-pad" />
      </View>

      <View style={styles.panel}>
        <Text style={[typography.h3, { marginBottom: spacing.sm }]}>📊 Pool Play Structure</Text>
        <View style={styles.tableHead}>
          <Text style={[styles.th, { width: 48 }]}>P</Text>
          <Text style={[styles.th, { width: 28 }]}>W</Text>
          <Text style={styles.th}>₹10</Text>
          <Text style={styles.th}>₹50</Text>
          <Text style={styles.th}>₹100</Text>
        </View>
        {STRUCTURE_ROWS.map((r) => (
          <View key={r.players} style={styles.tableRow}>
            <Text style={[styles.td, { width: 48 }]}>{r.players}</Text>
            <Text style={[styles.td, { width: 28 }]}>{r.winners}</Text>
            <Text style={styles.td}>{formatRupee(r.prize10)}</Text>
            <Text style={styles.td}>{formatRupee(r.prize50)}</Text>
            <Text style={styles.td}>{formatRupee(r.prize100)}</Text>
          </View>
        ))}
      </View>

      <View style={styles.panel}>
        <Text style={[typography.h3, { marginBottom: spacing.sm }]}>🏅 Winner Distribution</Text>
        {STRUCTURE_ROWS.map((r) => (
          <View key={`d-${r.players}`} style={styles.distRow}>
            <Text style={styles.distP}>{r.players}P</Text>
            <Text style={styles.distW}>{r.winners}W</Text>
            <Text style={styles.distLabelFlex}>{r.distributionLabel}</Text>
          </View>
        ))}
      </View>

      <View style={styles.panel}>
        <Text style={[typography.h3, { marginBottom: spacing.sm }]}>📌 Notes</Text>
        <Text style={styles.notes}>
          • Prize Pool = 70% of Total Collection{'\n'}
          • Remaining 30% = Platform Fee + Payment Gateway + Operations + Promotions{'\n'}
          • Winners की संख्या Tournament Size के अनुसार बढ़ाई या घटाई जा सकती है।
        </Text>
        <PremiumInput label="Custom notes" value={notes} onChangeText={setNotes} />
      </View>

      <View style={styles.row}>
        <SecondaryButton title="Reset" onPress={reset} style={{ flex: 1 }} />
        <PrimaryButton
          title="Create Pool"
          style={{ flex: 1 }}
          onPress={() => {
            showToast(`Pool created · ${pNum}P · ${formatRupee(eNum)} · ${formatRupee(Math.round(pool))}`, 'success');
            navigation.goBack();
          }}
        />
      </View>
    </Screen>
  );
}

function Row({ label, value, highlight }: { label: string; value: string; highlight?: boolean }) {
  return (
    <View style={styles.kv}>
      <Text style={styles.k}>{label}</Text>
      <Text style={[styles.v, highlight && styles.vGold]}>{value}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  chips: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: spacing.md },
  chip: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 12,
    paddingHorizontal: 14,
    paddingVertical: 8,
    backgroundColor: colors.surfaceElevated,
  },
  chipOn: { borderColor: colors.neonPurple, backgroundColor: '#2A1850' },
  chipBlue: { borderColor: colors.neonBlue, backgroundColor: '#13284A' },
  chipText: { color: colors.textSecondary, fontWeight: '700' },
  chipTextOn: { color: colors.white },
  panel: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.md,
  },
  kv: { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 8 },
  k: { color: colors.textSecondary },
  v: { color: colors.textPrimary, fontWeight: '800' },
  vGold: { color: colors.primaryGold, fontSize: 18 },
  distLabel: { color: colors.primaryGold, fontWeight: '600', marginBottom: spacing.sm },
  tableHead: { flexDirection: 'row', marginBottom: 6 },
  tableRow: { flexDirection: 'row', marginBottom: 4 },
  th: { flex: 1, color: colors.textMuted, fontSize: 11, fontWeight: '700' },
  td: { flex: 1, color: colors.textPrimary, fontSize: 12, fontWeight: '600' },
  distRow: { flexDirection: 'row', alignItems: 'center', marginBottom: 8 },
  distP: { width: 48, fontWeight: '700', color: colors.textPrimary },
  distW: { width: 36, color: colors.textSecondary },
  distLabelFlex: { flex: 1, color: colors.primaryGold, fontSize: 13 },
  notes: { color: colors.textSecondary, lineHeight: 20, fontSize: 13, marginBottom: spacing.sm },
  row: { flexDirection: 'row', gap: spacing.sm, marginBottom: spacing.xl },
});
