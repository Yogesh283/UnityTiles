import React from 'react';
import { StyleSheet, TextInput, TextInputProps, View, Text } from 'react-native';
import { colors, radius, spacing, typography } from '../../theme';

type Props = TextInputProps & {
  label?: string;
  error?: string;
};

export function PremiumInput({ label, error, style, ...rest }: Props) {
  return (
    <View style={styles.wrap}>
      {label ? <Text style={typography.label}>{label}</Text> : null}
      <TextInput
        placeholderTextColor={colors.textMuted}
        style={[styles.input, style, error ? styles.errorBorder : null]}
        {...rest}
      />
      {error ? <Text style={styles.error}>{error}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { gap: spacing.xs, marginBottom: spacing.md },
  input: {
    backgroundColor: colors.secondaryBlack,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.md,
    color: colors.textPrimary,
    paddingHorizontal: spacing.md,
    paddingVertical: 14,
    fontFamily: typography.body.fontFamily,
    fontSize: 15,
  },
  errorBorder: { borderColor: colors.danger },
  error: { color: colors.danger, fontSize: 12 },
});
