import React from 'react';
import { Pressable, StyleSheet, Text, ViewStyle, Animated } from 'react-native';
import { colors, radius, typography } from '../../theme';

type Props = {
  title: string;
  onPress?: () => void;
  style?: ViewStyle;
  tone?: 'purple' | 'gold' | 'danger' | 'blue';
};

export function SecondaryButton({ title, onPress, style, tone = 'purple' }: Props) {
  const scale = React.useRef(new Animated.Value(1)).current;
  const color =
    tone === 'gold' ? colors.primaryGold : tone === 'danger' ? colors.danger : tone === 'blue' ? colors.blue : colors.neonPurple;

  return (
    <Animated.View style={[{ transform: [{ scale }] }, style]}>
      <Pressable
        onPressIn={() => Animated.spring(scale, { toValue: 0.97, useNativeDriver: true }).start()}
        onPressOut={() => Animated.spring(scale, { toValue: 1, useNativeDriver: true }).start()}
        onPress={onPress}
        style={[styles.btn, { borderColor: color }]}
      >
        <Text style={[typography.button, { color }]}>{title}</Text>
      </Pressable>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  btn: {
    minHeight: 52,
    borderRadius: radius.lg,
    borderWidth: 1.5,
    backgroundColor: colors.surface,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 20,
  },
});
