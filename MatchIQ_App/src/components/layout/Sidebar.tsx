import React from 'react';
import { Modal, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useUiStore, useAuthStore } from '../../store';
import { ROUTES } from '../../constants';
import { colors, radius, spacing, typography } from '../../theme';
import { goTo } from '../../navigation/nav';

const LINKS = [
  { label: 'Games', route: ROUTES.Games },
  { label: 'Tournaments', route: ROUTES.Tournament },
  { label: 'Wallet', route: ROUTES.Wallet },
  { label: 'My Income · WXO', route: ROUTES.Income },
  { label: 'Referral', route: ROUTES.Referral },
  { label: 'Create Pool', route: ROUTES.CreatePool },
  { label: 'Missions', route: ROUTES.Missions },
  { label: 'Achievements', route: ROUTES.Achievements },
  { label: 'Battle Pass', route: ROUTES.BattlePass },
  { label: 'Clan', route: ROUTES.Clan },
  { label: 'Friends', route: ROUTES.Friends },
  { label: 'Mail', route: ROUTES.Mail },
  { label: 'Lucky Spin', route: ROUTES.LuckySpin },
  { label: 'Settings', route: ROUTES.Settings },
];

export function Sidebar() {
  const open = useUiStore((s) => s.sidebarOpen);
  const setOpen = useUiStore((s) => s.setSidebarOpen);
  const logout = useAuthStore((s) => s.logout);
  const navigation = useNavigation<any>();

  return (
    <Modal visible={open} transparent animationType="fade" onRequestClose={() => setOpen(false)}>
      <Pressable style={styles.overlay} onPress={() => setOpen(false)}>
        <View style={styles.panel}>
          <Text style={typography.h2}>Temple Menu</Text>
          {LINKS.map((link) => (
            <Pressable
              key={link.route}
              style={styles.link}
              onPress={() => {
                setOpen(false);
                goTo(navigation, link.route);
              }}
            >
              <Text style={typography.bodyStrong}>{link.label}</Text>
            </Pressable>
          ))}
          <Pressable
            style={[styles.link, styles.logout]}
            onPress={() => {
              setOpen(false);
              logout();
            }}
          >
            <Text style={[typography.bodyStrong, { color: colors.danger }]}>Logout</Text>
          </Pressable>
        </View>
      </Pressable>
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: { flex: 1, backgroundColor: colors.overlay, flexDirection: 'row' },
  panel: {
    width: '78%',
    maxWidth: 320,
    backgroundColor: colors.surfaceElevated,
    borderRightWidth: 1,
    borderColor: colors.borderGold,
    paddingTop: 64,
    paddingHorizontal: spacing.md,
    gap: spacing.xs,
  },
  link: {
    paddingVertical: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  logout: { marginTop: spacing.lg, borderBottomWidth: 0 },
});
