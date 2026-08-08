import React from 'react';
import {
  ScrollView,
  StyleSheet,
  View,
  ViewStyle,
  StatusBar,
  RefreshControl,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { SafeAreaView } from 'react-native-safe-area-context';
import { colors, layout, spacing } from '../../theme';

type Props = {
  children: React.ReactNode;
  scroll?: boolean;
  style?: ViewStyle;
  contentStyle?: ViewStyle;
  edges?: ('top' | 'right' | 'bottom' | 'left')[];
  refreshing?: boolean;
  onRefresh?: () => void;
  padded?: boolean;
};

export function Screen({
  children,
  scroll,
  style,
  contentStyle,
  edges = ['top', 'left', 'right'],
  refreshing,
  onRefresh,
  padded = true,
}: Props) {
  const body = scroll ? (
    <ScrollView
      contentContainerStyle={[padded && styles.pad, contentStyle]}
      showsVerticalScrollIndicator={false}
      refreshControl={
        onRefresh ? (
          <RefreshControl refreshing={!!refreshing} onRefresh={onRefresh} tintColor={colors.purple} />
        ) : undefined
      }
    >
      {children}
    </ScrollView>
  ) : (
    <View style={[styles.flex, padded && styles.pad, contentStyle]}>{children}</View>
  );

  return (
    <View style={[styles.root, style]}>
      <StatusBar barStyle="light-content" />
      <LinearGradient colors={['#12081F', colors.background, '#07101F']} style={StyleSheet.absoluteFill} />
      <View style={styles.glowTop} />
      <SafeAreaView style={styles.flex} edges={edges}>
        {body}
      </SafeAreaView>
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.background },
  flex: { flex: 1 },
  pad: {
    paddingHorizontal: layout.screenPadding,
    paddingBottom: spacing.xl,
  },
  glowTop: {
    position: 'absolute',
    top: -60,
    alignSelf: 'center',
    width: 220,
    height: 160,
    borderRadius: 120,
    backgroundColor: colors.glowPurple,
    opacity: 0.35,
  },
});
