import { TextStyle } from 'react-native';
import { colors } from './colors';

export const fonts = {
  display: 'Orbitron_700Bold',
  displayRegular: 'Orbitron_400Regular',
  displaySemi: 'Orbitron_600SemiBold',
  body: 'Inter_400Regular',
  bodyMedium: 'Inter_500Medium',
  bodySemi: 'Inter_600SemiBold',
  bodyBold: 'Inter_700Bold',
} as const;

export const typography = {
  hero: {
    fontFamily: fonts.display,
    fontSize: 32,
    lineHeight: 40,
    color: colors.textPrimary,
    letterSpacing: 1.4,
  } as TextStyle,
  h1: {
    fontFamily: fonts.display,
    fontSize: 26,
    lineHeight: 34,
    color: colors.textPrimary,
    letterSpacing: 0.8,
  } as TextStyle,
  h2: {
    fontFamily: fonts.displaySemi,
    fontSize: 20,
    lineHeight: 28,
    color: colors.textPrimary,
  } as TextStyle,
  h3: {
    fontFamily: fonts.displaySemi,
    fontSize: 16,
    lineHeight: 22,
    color: colors.goldLight,
  } as TextStyle,
  body: {
    fontFamily: fonts.body,
    fontSize: 15,
    lineHeight: 22,
    color: colors.textSecondary,
  } as TextStyle,
  bodyStrong: {
    fontFamily: fonts.bodySemi,
    fontSize: 15,
    lineHeight: 22,
    color: colors.textPrimary,
  } as TextStyle,
  caption: {
    fontFamily: fonts.bodyMedium,
    fontSize: 12,
    lineHeight: 16,
    color: colors.textMuted,
  } as TextStyle,
  button: {
    fontFamily: fonts.bodyBold,
    fontSize: 15,
    lineHeight: 20,
    letterSpacing: 0.8,
    color: colors.white,
  } as TextStyle,
  label: {
    fontFamily: fonts.bodyMedium,
    fontSize: 13,
    lineHeight: 18,
    color: colors.neonPurple,
  } as TextStyle,
} as const;
