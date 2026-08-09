export const colors = {
  // WXO brand palette — the whole app uses the WXO red theme (rmsurveyai.com), not the old
  // purple/blue esports gradient. The purple/blue token names are kept for compatibility but now
  // resolve to WXO red shades, so every gradient / accent / border / glow turns red app-wide.
  background: '#090B18',
  secondaryBlack: '#0B0E1C',
  surface: '#12152A',
  surfaceElevated: '#1A1E38',
  // WXO brand red + shades (use these directly for new UI).
  brand: '#E31C23',
  brandDark: '#A50E14',
  brandLight: '#FF4D52',
  // Legacy accent names → remapped to the WXO red family so existing components recolour with no
  // per-file changes. `purple`→brand red, `blue`→dark red make [purple, blue] a red gradient.
  purple: '#E31C23',
  blue: '#A50E14',
  primaryGold: '#F5B700',
  goldLight: '#FFD54F',
  goldDark: '#C49000',
  accentGreen: '#22C55E',
  danger: '#EF4444',
  neonPurple: '#FF4D52',
  neonBlue: '#FF7A7E',
  textPrimary: '#FFFFFF',
  textSecondary: '#B8BDD4',
  textMuted: '#6B7190',
  border: '#2A2F4A',
  borderGold: 'rgba(245, 183, 0, 0.45)',
  borderPurple: 'rgba(227, 28, 35, 0.45)',
  overlay: 'rgba(0, 0, 0, 0.72)',
  glowGold: 'rgba(245, 183, 0, 0.35)',
  glowPurple: 'rgba(227, 28, 35, 0.35)',
  glowOrange: 'rgba(227, 28, 35, 0.28)',
  mapleRed: '#EF4444',
  lanternOrange: '#F5B700',
  templeStone: '#2A2F4A',
  success: '#22C55E',
  warning: '#F5B700',
  white: '#FFFFFF',
  black: '#000000',
} as const;

export type ColorKey = keyof typeof colors;
