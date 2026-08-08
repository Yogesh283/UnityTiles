import { ViewStyle } from 'react-native';
import { colors } from './colors';

export const shadows = {
  card: {
    shadowColor: colors.black,
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.45,
    shadowRadius: 16,
    elevation: 8,
  } as ViewStyle,
  goldGlow: {
    shadowColor: colors.primaryGold,
    shadowOffset: { width: 0, height: 0 },
    shadowOpacity: 0.55,
    shadowRadius: 12,
    elevation: 10,
  } as ViewStyle,
  soft: {
    shadowColor: colors.black,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 4,
  } as ViewStyle,
} as const;
