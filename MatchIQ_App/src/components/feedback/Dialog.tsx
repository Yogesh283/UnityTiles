import React from 'react';
import { Modal, StyleSheet, Text, View, Pressable } from 'react-native';
import { colors, radius, spacing, typography } from '../../theme';
import { PrimaryButton } from '../buttons/PrimaryButton';
import { SecondaryButton } from '../buttons/SecondaryButton';

type Props = {
  visible: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm?: () => void;
  onCancel?: () => void;
};

export function Dialog({
  visible,
  title,
  message,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  onConfirm,
  onCancel,
}: Props) {
  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onCancel}>
      <Pressable style={styles.overlay} onPress={onCancel}>
        <View style={styles.card}>
          <Text style={typography.h2}>{title}</Text>
          <Text style={[typography.body, { marginVertical: spacing.md }]}>{message}</Text>
          <View style={styles.actions}>
            {onCancel ? <SecondaryButton title={cancelLabel} onPress={onCancel} style={{ flex: 1 }} /> : null}
            {onConfirm ? <PrimaryButton title={confirmLabel} onPress={onConfirm} style={{ flex: 1 }} /> : null}
          </View>
        </View>
      </Pressable>
    </Modal>
  );
}

export function Popup({
  visible,
  children,
  onClose,
}: {
  visible: boolean;
  children: React.ReactNode;
  onClose?: () => void;
}) {
  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onClose}>
      <Pressable style={styles.overlay} onPress={onClose}>
        <View style={styles.card}>{children}</View>
      </Pressable>
    </Modal>
  );
}

export function PremiumModal({
  visible,
  title,
  children,
  onClose,
}: {
  visible: boolean;
  title: string;
  children: React.ReactNode;
  onClose?: () => void;
}) {
  return (
    <Modal visible={visible} transparent animationType="slide" onRequestClose={onClose}>
      <View style={styles.overlay}>
        <View style={[styles.card, styles.modal]}>
          <Text style={typography.h2}>{title}</Text>
          <View style={{ marginTop: spacing.md }}>{children}</View>
          {onClose ? (
            <SecondaryButton title="Close" onPress={onClose} style={{ marginTop: spacing.lg }} />
          ) : null}
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: colors.overlay,
    justifyContent: 'center',
    padding: spacing.lg,
  },
  card: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: radius.xl,
    borderWidth: 1.5,
    borderColor: colors.borderGold,
    padding: spacing.lg,
  },
  modal: { maxHeight: '80%' },
  actions: { flexDirection: 'row', gap: spacing.sm },
});
