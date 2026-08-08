import React from 'react';
import { Image, StyleSheet, View, ViewStyle } from 'react-native';
import { colors } from '../../theme';

export const WXO_LOGO = require('../../../assets/wxo-logo.png');

type Props = {
  size?: number;
  glow?: boolean;
  style?: ViewStyle;
};

export function Logo({ size = 110, glow = true, style }: Props) {
  return (
    <View
      style={[
        { width: size, height: size },
        glow && { ...styles.glow, shadowRadius: size * 0.22, borderRadius: size / 2 },
        style,
      ]}
    >
      <Image source={WXO_LOGO} style={{ width: size, height: size }} resizeMode="contain" />
    </View>
  );
}

const styles = StyleSheet.create({
  glow: {
    shadowColor: colors.neonBlue,
    shadowOpacity: 0.6,
    shadowOffset: { width: 0, height: 0 },
    elevation: 12,
  },
});
