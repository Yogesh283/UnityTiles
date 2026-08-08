import React from 'react';
import { StyleSheet, TextInput, View } from 'react-native';
import { colors, radius, spacing, typography } from '../../theme';

type Props = {
  value: string;
  onChangeText: (text: string) => void;
  placeholder?: string;
};

export function SearchBar({ value, onChangeText, placeholder = 'Search the temple…' }: Props) {
  return (
    <View style={styles.wrap}>
      <TextInput
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder}
        placeholderTextColor={colors.textMuted}
        style={styles.input}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    backgroundColor: colors.surface,
    borderRadius: radius.pill,
    borderWidth: 1,
    borderColor: colors.borderGold,
    paddingHorizontal: spacing.md,
  },
  input: {
    color: colors.textPrimary,
    fontFamily: typography.body.fontFamily,
    fontSize: 14,
    paddingVertical: 12,
  },
});
