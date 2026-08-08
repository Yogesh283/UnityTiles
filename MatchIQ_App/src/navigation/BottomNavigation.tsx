import React from 'react';
import { StyleSheet, Text, View, Pressable } from 'react-native';
import { BottomTabBarProps } from '@react-navigation/bottom-tabs';
import { TabActions } from '@react-navigation/native';
import { colors, radius, spacing } from '../theme';

const ICONS: Record<string, string> = {
  Home: '🏠',
  Tournament: '🎮',
  Events: '⚡',
  Wallet: '💰',
  Profile: '👤',
};

export function BottomNavigation({ state, descriptors, navigation }: BottomTabBarProps) {
  return (
    <View style={styles.wrap}>
      {state.routes.map((route, index) => {
        const focused = state.index === index;
        const { options } = descriptors[route.key];
        const label = options.title ?? route.name;
        return (
          <Pressable
            key={route.key}
            onPress={() => {
              const event = navigation.emit({ type: 'tabPress', target: route.key, canPreventDefault: true });
              if (!focused && !event.defaultPrevented) {
                navigation.dispatch(TabActions.jumpTo(route.name));
              }
            }}
            style={[styles.tab, focused && styles.tabActive]}
          >
            <Text style={styles.icon}>{ICONS[route.name] || '•'}</Text>
            <Text style={[styles.label, focused && styles.labelActive]}>{label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    flexDirection: 'row',
    backgroundColor: colors.surface,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingBottom: spacing.sm,
    paddingTop: spacing.xs,
    shadowColor: colors.purple,
    shadowOpacity: 0.15,
    shadowRadius: 16,
    shadowOffset: { width: 0, height: -4 },
    elevation: 12,
  },
  tab: {
    flex: 1,
    alignItems: 'center',
    paddingVertical: 8,
    marginHorizontal: 4,
    borderRadius: radius.md,
  },
  tabActive: {
    backgroundColor: 'rgba(123,47,247,0.2)',
  },
  icon: { fontSize: 16 },
  label: { color: colors.textMuted, fontSize: 10, marginTop: 4, fontWeight: '600' },
  labelActive: { color: colors.neonPurple },
});
