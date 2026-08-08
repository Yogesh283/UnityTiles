import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors } from '../../theme';

type Props = { count: number };

export function NotificationBadge({ count }: Props) {
  if (!count) return null;
  return (
    <View style={styles.badge}>
      <Text style={styles.text}>{count > 9 ? '9+' : count}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    minWidth: 18,
    height: 18,
    borderRadius: 9,
    backgroundColor: colors.danger,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 4,
    position: 'absolute',
    top: -4,
    right: -6,
  },
  text: { color: colors.white, fontSize: 10, fontWeight: '700' },
});
