/**
 * Two palettes with identical keys, so a screen can swap between them without knowing which
 * one it has. Dark values follow the same rules used on the web side: surfaces step by
 * elevation, text never gets darker than its light-mode counterpart, and saturated accents
 * move to a lighter step so they still clear AA against a dark ground.
 */
export const LightColors = {
  primary: '#4CAF50',
  primaryDark: '#388E3C',
  primaryLight: '#C8E6C9',
  secondary: '#FF9800',
  secondaryDark: '#F57C00',
  background: '#F5F5F5',
  surface: '#FFFFFF',
  surfaceAlt: '#EEEEEE',
  text: '#212121',
  textSecondary: '#757575',
  textHint: '#BDBDBD',
  error: '#F44336',
  success: '#4CAF50',
  warning: '#FF9800',
  info: '#2196F3',
  divider: '#E0E0E0',
  overlay: 'rgba(0,0,0,0.5)',
};

export type Palette = typeof LightColors;

export const DarkColors: Palette = {
  // Brightened one step: #4CAF50 on a dark card is dim, #66BB6A reads at ~7.9:1.
  primary: '#66BB6A',
  // Used as text on primaryLight tints, so on dark it has to be the light end.
  primaryDark: '#A5D6A7',
  // Used as a tint fill behind text, so on dark it has to be the dark end.
  primaryLight: '#1F3D26',
  secondary: '#FFB74D',
  secondaryDark: '#FFA726',
  background: '#121212',
  surface: '#1E1E1E',
  surfaceAlt: '#2A2A2A',
  text: '#ECEDEE',
  textSecondary: '#A1A5AB',
  textHint: '#868B94',
  error: '#EF5350',
  success: '#66BB6A',
  warning: '#FFB74D',
  info: '#64B5F6',
  divider: '#2C2F33',
  overlay: 'rgba(0,0,0,0.7)',
};

/**
 * @deprecated Static light palette. Use `useTheme()` from '@/theme' so the value follows the
 * active colour scheme; this export only remains for code that cannot use hooks.
 */
export const Colors = LightColors;

export const Spacing = {
  xs: 4,
  sm: 8,
  md: 16,
  lg: 24,
  xl: 32,
  xxl: 48,
};

export const BorderRadius = {
  sm: 4,
  md: 8,
  lg: 16,
  xl: 24,
  full: 9999,
};

export const FontSize = {
  xs: 10,
  sm: 12,
  md: 14,
  lg: 16,
  xl: 18,
  xxl: 24,
  xxxl: 32,
};

export const FontWeight = {
  regular: '400' as const,
  medium: '500' as const,
  semibold: '600' as const,
  bold: '700' as const,
};

export const Shadows = {
  sm: {
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  md: {
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.15,
    shadowRadius: 4,
    elevation: 4,
  },
};
