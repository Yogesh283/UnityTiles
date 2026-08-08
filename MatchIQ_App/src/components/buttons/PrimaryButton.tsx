import React from 'react';
import {
  Pressable,
  StyleSheet,
  Text,
  ViewStyle,
  TextStyle,
  ActivityIndicator,
  Animated,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { colors, radius, typography } from '../../theme';

type Props = {
  title: string;
  onPress?: () => void;
  loading?: boolean;
  disabled?: boolean;
  style?: ViewStyle;
  textStyle?: TextStyle;
};

export function PrimaryButton({ title, onPress, loading, disabled, style, textStyle }: Props) {
  const scale = React.useRef(new Animated.Value(1)).current;
  const pressIn = () => Animated.spring(scale, { toValue: 0.97, useNativeDriver: true }).start();
  const pressOut = () => Animated.spring(scale, { toValue: 1, useNativeDriver: true }).start();

  return (
    <Animated.View style={[{ transform: [{ scale }] }, style, (disabled || loading) && styles.disabled]}>
      <Pressable disabled={disabled || loading} onPressIn={pressIn} onPressOut={pressOut} onPress={onPress}>
        <LinearGradient
          colors={[colors.purple, colors.blue]}
          start={{ x: 0, y: 0 }}
          end={{ x: 1, y: 1 }}
          style={styles.gradient}
        >
          {loading ? (
            <ActivityIndicator color={colors.white} />
          ) : (
            <Text style={[typography.button, textStyle]}>{title}</Text>
          )}
        </LinearGradient>
      </Pressable>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  gradient: {
    minHeight: 52,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 20,
    borderRadius: radius.lg,
  },
  disabled: { opacity: 0.5 },
});
