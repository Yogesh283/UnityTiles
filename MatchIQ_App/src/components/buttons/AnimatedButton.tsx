import React from 'react';
import { Pressable, StyleSheet, ViewStyle, Animated } from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { colors, radius } from '../../theme';

type Props = {
  children: React.ReactNode;
  onPress?: () => void;
  glow?: boolean;
  style?: ViewStyle;
};

export function AnimatedButton({ children, onPress, glow, style }: Props) {
  const scale = React.useRef(new Animated.Value(1)).current;
  const glowOpacity = React.useRef(new Animated.Value(0.35)).current;

  React.useEffect(() => {
    if (!glow) return;
    const loop = Animated.loop(
      Animated.sequence([
        Animated.timing(glowOpacity, { toValue: 0.8, duration: 1400, useNativeDriver: false }),
        Animated.timing(glowOpacity, { toValue: 0.35, duration: 1400, useNativeDriver: false }),
      ]),
    );
    loop.start();
    return () => loop.stop();
  }, [glow, glowOpacity]);

  const pressIn = () => Animated.spring(scale, { toValue: 0.95, useNativeDriver: true }).start();
  const pressOut = () => Animated.spring(scale, { toValue: 1, useNativeDriver: true }).start();

  return (
    <Animated.View
      style={[
        styles.wrap,
        style,
        { transform: [{ scale }], shadowOpacity: glow ? glowOpacity : 0.25 },
      ]}
    >
      <Pressable onPressIn={pressIn} onPressOut={pressOut} onPress={onPress}>
        <LinearGradient colors={['#2E261C', colors.surfaceElevated, '#1A1510']} style={styles.inner}>
          {children}
        </LinearGradient>
      </Pressable>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    borderRadius: radius.lg,
    shadowColor: colors.primaryGold,
    shadowOffset: { width: 0, height: 0 },
    shadowRadius: 14,
    elevation: 8,
  },
  inner: {
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.borderGold,
    padding: 14,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
