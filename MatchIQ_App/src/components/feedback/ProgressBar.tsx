import React from 'react';
import { StyleSheet, View, Animated } from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { colors, radius } from '../../theme';

type Props = { progress: number; height?: number };

export function ProgressBar({ progress, height = 10 }: Props) {
  const width = React.useRef(new Animated.Value(0)).current;

  React.useEffect(() => {
    Animated.timing(width, {
      toValue: Math.max(0, Math.min(1, progress)) * 100,
      duration: 600,
      useNativeDriver: false,
    }).start();
  }, [progress, width]);

  return (
    <View style={[styles.track, { height }]}>
      <Animated.View style={[styles.fillWrap, { width: width.interpolate({ inputRange: [0, 100], outputRange: ['0%', '100%'] }) }]}>
        <LinearGradient colors={[colors.purple, colors.blue]} style={styles.fill} />
      </Animated.View>
    </View>
  );
}

const styles = StyleSheet.create({
  track: {
    width: '100%',
    backgroundColor: colors.secondaryBlack,
    borderRadius: radius.pill,
    overflow: 'hidden',
    borderWidth: 1,
    borderColor: colors.border,
  },
  fillWrap: { height: '100%' },
  fill: { flex: 1 },
});
