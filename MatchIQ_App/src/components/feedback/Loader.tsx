import React from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../../theme';

type Props = { label?: string; fullScreen?: boolean };

export function Loader({ label = 'Loading temple…', fullScreen }: Props) {
  return (
    <View style={[styles.wrap, fullScreen && styles.full]}>
      <ActivityIndicator size="large" color={colors.primaryGold} />
      <Text style={[typography.body, { marginTop: 12 }]}>{label}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { alignItems: 'center', justifyContent: 'center', padding: 24 },
  full: { flex: 1, backgroundColor: colors.background },
});
