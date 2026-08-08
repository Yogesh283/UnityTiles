import React from 'react';
import { Pressable, StyleSheet, Text, View, ViewStyle, Animated } from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { colors, radius, shadows, spacing, typography } from '../../theme';

type Props = {
  title: string;
  subtitle?: string;
  badge?: string;
  onPress?: () => void;
  style?: ViewStyle;
  children?: React.ReactNode;
};

export function GameCard({ title, subtitle, badge, onPress, style, children }: Props) {
  const scale = React.useRef(new Animated.Value(1)).current;
  const pressIn = () => Animated.spring(scale, { toValue: 0.98, useNativeDriver: true }).start();
  const pressOut = () => Animated.spring(scale, { toValue: 1, useNativeDriver: true }).start();

  return (
    <Animated.View style={[{ transform: [{ scale }] }, style]}>
      <Pressable onPress={onPress} onPressIn={pressIn} onPressOut={pressOut} style={styles.wrap}>
        <LinearGradient colors={['#2C241A', colors.surface, '#15110D']} style={styles.inner}>
          <View style={styles.row}>
            <Text style={typography.h3}>{title}</Text>
            {badge ? (
              <View style={styles.badge}>
                <Text style={styles.badgeText}>{badge}</Text>
              </View>
            ) : null}
          </View>
          {subtitle ? <Text style={typography.body}>{subtitle}</Text> : null}
          {children}
        </LinearGradient>
      </Pressable>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  wrap: { borderRadius: radius.lg, ...shadows.card },
  inner: {
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.borderPurple,
    padding: spacing.md,
    gap: spacing.xs,
  },
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  badge: {
    backgroundColor: colors.lanternOrange,
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: radius.pill,
  },
  badgeText: { color: colors.white, fontSize: 11, fontWeight: '700' },
});
