import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';
import { useColorScheme } from 'react-native';
import * as SecureStore from 'expo-secure-store';
import { DarkColors, LightColors, type Palette } from '@/constants/theme';

export type ThemeMode = 'light' | 'dark' | 'system';
export type ColorScheme = 'light' | 'dark';

const STORAGE_KEY = 'foodeez-theme-mode';

interface ThemeContextValue {
  colors: Palette;
  /** What the user picked, which may be 'system'. */
  mode: ThemeMode;
  /** What that resolves to right now. */
  scheme: ColorScheme;
  setMode: (mode: ThemeMode) => void;
}

const ThemeContext = createContext<ThemeContextValue>({
  colors: LightColors,
  mode: 'system',
  scheme: 'light',
  setMode: () => {},
});

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const systemScheme = useColorScheme();
  const [mode, setModeState] = useState<ThemeMode>('system');

  // Load the stored preference once. Until it arrives we follow the OS, which is the same
  // thing the default resolves to, so there is no flash on the common path.
  useEffect(() => {
    let cancelled = false;
    SecureStore.getItemAsync(STORAGE_KEY)
      .then((stored) => {
        if (!cancelled && (stored === 'light' || stored === 'dark' || stored === 'system')) {
          setModeState(stored);
        }
      })
      .catch(() => {
        // A missing or unreadable preference just means "follow the system".
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const setMode = useCallback((next: ThemeMode) => {
    setModeState(next);
    SecureStore.setItemAsync(STORAGE_KEY, next).catch(() => {
      // Non-fatal: the choice still applies for this session.
    });
  }, []);

  const scheme: ColorScheme = mode === 'system' ? (systemScheme === 'dark' ? 'dark' : 'light') : mode;

  const value = useMemo<ThemeContextValue>(
    () => ({
      colors: scheme === 'dark' ? DarkColors : LightColors,
      mode,
      scheme,
      setMode,
    }),
    [scheme, mode, setMode],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

/** The active palette. */
export function useTheme(): Palette {
  return useContext(ThemeContext).colors;
}

/** The active mode plus the setter, for the theme picker. */
export function useThemeMode() {
  const { mode, scheme, setMode } = useContext(ThemeContext);
  return { mode, scheme, setMode };
}

/**
 * Builds a StyleSheet from the active palette, rebuilding it only when the palette changes.
 *
 * Screens declare `const makeStyles = (C: Palette) => StyleSheet.create({...})` at module
 * level, so the factory identity is stable and the memo actually holds.
 */
export function useThemedStyles<T>(factory: (colors: Palette) => T): T {
  const colors = useTheme();
  return useMemo(() => factory(colors), [factory, colors]);
}

export type { Palette };
