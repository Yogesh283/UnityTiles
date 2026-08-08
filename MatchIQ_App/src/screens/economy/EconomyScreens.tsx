import React, { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  Screen, GameHeader, PrimaryButton, SecondaryButton, CurrencyCard, GameCard, PremiumInput, Loader,
} from '../../components';
import { colors, spacing, typography } from '../../theme';
import { ROUTES } from '../../constants';
import { usePlayerStore, useUiStore } from '../../store';
import { useTransactions, useStore, useInventory } from '../../hooks';
import { formatCoins, formatTimeAgo } from '../../utils';

type Props = NativeStackScreenProps<any>;

export function WalletScreen({ navigation }: Props) {
  const balances = usePlayerStore(s => s.balances);
  return (
    <Screen scroll>
      <GameHeader title="Wallet" />
      <View style={styles.row}>
        <CurrencyCard label="Coins" amount={balances.coins} />
        <CurrencyCard label="Diamonds" amount={balances.diamonds} tone="diamond" />
      </View>
      <View style={[styles.row, { marginTop: spacing.sm }]}>
        <CurrencyCard label="Energy" amount={balances.energy} tone="energy" />
      </View>
      <PrimaryButton title="Deposit" onPress={() => navigation.navigate(ROUTES.Deposit)} style={{ marginTop: spacing.lg }} />
      <SecondaryButton title="Withdraw" onPress={() => navigation.navigate(ROUTES.Withdraw)} style={{ marginTop: spacing.sm }} />
      <SecondaryButton title="Transactions" onPress={() => navigation.navigate(ROUTES.TransactionHistory)} style={{ marginTop: spacing.sm }} />
      <SecondaryButton title="Store" onPress={() => navigation.navigate(ROUTES.Store)} style={{ marginTop: spacing.sm }} />
    </Screen>
  );
}

export function DepositScreen({ navigation }: Props) {
  const [amount, setAmount] = useState('1000');
  const setBalances = usePlayerStore(s => s.setBalances);
  const balances = usePlayerStore(s => s.balances);
  const showToast = useUiStore(s => s.showToast);
  return (
    <Screen scroll>
      <GameHeader title="Deposit" showBack compact />
      <Text style={typography.body}>Add coins to your temple vault.</Text>
      <PremiumInput label="Amount" value={amount} onChangeText={setAmount} keyboardType="number-pad" />
      <View style={styles.row}>
        {['500', '1000', '5000'].map(v => (
          <PrimaryButton key={v} title={v} onPress={() => setAmount(v)} style={{ flex: 1, minHeight: 44 }} />
        ))}
      </View>
      <PrimaryButton title="Confirm Deposit" onPress={() => {
        setBalances({ coins: balances.coins + Number(amount || 0) });
        showToast('Deposit complete', 'success');
        navigation.goBack();
      }} style={{ marginTop: spacing.md }} />
    </Screen>
  );
}

export function WithdrawScreen({ navigation }: Props) {
  const [amount, setAmount] = useState('100');
  const balances = usePlayerStore(s => s.balances);
  const setBalances = usePlayerStore(s => s.setBalances);
  const showToast = useUiStore(s => s.showToast);
  return (
    <Screen scroll>
      <GameHeader title="Withdraw" showBack compact />
      <Text style={typography.body}>Withdraw diamonds to your linked payout method.</Text>
      <PremiumInput label="Diamonds" value={amount} onChangeText={setAmount} keyboardType="number-pad" />
      <PrimaryButton title="Request Withdrawal" onPress={() => {
        const n = Number(amount || 0);
        if (n > balances.diamonds) { showToast('Insufficient diamonds', 'danger'); return; }
        setBalances({ diamonds: balances.diamonds - n });
        showToast('Withdrawal pending', 'info');
        navigation.goBack();
      }} />
    </Screen>
  );
}

export function TransactionHistoryScreen() {
  const { data, isLoading } = useTransactions();
  return (
    <Screen scroll>
      <GameHeader title="Transactions" showBack compact />
      {isLoading ? <Loader /> : null}
      {data?.map(tx => (
        <View key={tx.id} style={styles.panel}>
          <Text style={typography.bodyStrong}>{tx.title}</Text>
          <Text style={{ color: tx.amount >= 0 ? colors.accentGreen : colors.danger, fontWeight: '700' }}>
            {tx.amount >= 0 ? '+' : ''}{formatCoins(tx.amount)} {tx.currency}
          </Text>
          <Text style={typography.caption}>{tx.status} · {formatTimeAgo(tx.createdAt)}</Text>
        </View>
      ))}
    </Screen>
  );
}

export function StoreScreen({ navigation }: Props) {
  const { data, isLoading } = useStore();
  const setBalances = usePlayerStore(s => s.setBalances);
  const balances = usePlayerStore(s => s.balances);
  const showToast = useUiStore(s => s.showToast);
  return (
    <Screen scroll>
      <GameHeader title="Store" showBack compact />
      {isLoading ? <Loader /> : null}
      {data?.map(item => (
        <GameCard key={item.id} title={item.name} subtitle={item.description}
          badge={item.realPriceLabel || String(item.price)}
          onPress={() => {
            if (item.category === 'coins' && item.amount) setBalances({ coins: balances.coins + item.amount });
            if (item.category === 'diamonds' && item.amount) setBalances({ diamonds: balances.diamonds + item.amount });
            if (item.category === 'energy' && item.amount) setBalances({ energy: balances.energy + item.amount });
            showToast(item.name + ' purchased', 'success');
          }}
          style={{ marginBottom: spacing.sm }} />
      ))}
      <SecondaryButton title="Inventory" onPress={() => navigation.navigate(ROUTES.Inventory)} />
    </Screen>
  );
}

export function InventoryScreen() {
  const { data, isLoading } = useInventory();
  return (
    <Screen scroll>
      <GameHeader title="Inventory" showBack compact />
      {isLoading ? <Loader /> : null}
      {data?.map(item => (
        <View key={item.id} style={styles.panel}>
          <Text style={typography.bodyStrong}>{item.name}</Text>
          <Text style={typography.caption}>{item.type} · x{item.quantity}{item.equipped ? ' · Equipped' : ''}</Text>
        </View>
      ))}
    </Screen>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', gap: spacing.sm },
  panel: { backgroundColor: colors.surface, borderRadius: 12, borderWidth: 1, borderColor: colors.border, padding: spacing.md, marginBottom: spacing.sm, gap: 4 },
});
