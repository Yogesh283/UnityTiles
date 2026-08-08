import React, { useEffect } from 'react';
import { StyleSheet, Text, Pressable, Animated } from 'react-native';
import { useUiStore } from '../../store';
import { colors, radius, spacing, typography } from '../../theme';

export function Toast() {
  const toast = useUiStore((s) => s.toast);
  const clearToast = useUiStore((s) => s.clearToast);
  const opacity = React.useRef(new Animated.Value(0)).current;
  const translateY = React.useRef(new Animated.Value(-12)).current;

  useEffect(() => {
    if (!toast) return;
    Animated.parallel([
      Animated.timing(opacity, { toValue: 1, duration: 220, useNativeDriver: true }),
      Animated.timing(translateY, { toValue: 0, duration: 220, useNativeDriver: true }),
    ]).start();
    const t = setTimeout(clearToast, 2800);
    return () => clearTimeout(t);
  }, [toast, clearToast, opacity, translateY]);

  if (!toast) return null;

  const bg =
    toast.tone === 'success'
      ? colors.accentGreen
      : toast.tone === 'danger'
        ? colors.danger
        : colors.surfaceElevated;

  return (
    <Animated.View style={[styles.toast, { backgroundColor: bg, opacity, transform: [{ translateY }] }]}>
      <Pressable onPress={clearToast}>
        <Text style={[typography.bodyStrong, { color: colors.white }]}>{toast.message}</Text>
      </Pressable>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  toast: {
    position: 'absolute',
    top: 56,
    left: spacing.md,
    right: spacing.md,
    zIndex: 100,
    borderRadius: radius.md,
    padding: spacing.md,
    borderWidth: 1,
    borderColor: colors.borderGold,
  },
});
