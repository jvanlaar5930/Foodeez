import 'react-native-url-polyfill/auto';
import { NavigationContainer } from '@react-navigation/native';
import { StatusBar } from 'expo-status-bar';
import { useEffect } from 'react';
import { RootNavigator } from './src/navigation/RootNavigator';
import { useAuthStore } from './src/store/authStore';
import { ThemeProvider, useThemeMode } from './src/theme';

function AppShell() {
  const loadStoredAuth = useAuthStore((state) => state.loadStoredAuth);
  const { scheme } = useThemeMode();

  useEffect(() => {
    loadStoredAuth();
  }, [loadStoredAuth]);

  return (
    <NavigationContainer>
      {/* Invert the status bar text against the app's own scheme, which may not match the OS. */}
      <StatusBar style={scheme === 'dark' ? 'light' : 'dark'} />
      <RootNavigator />
    </NavigationContainer>
  );
}

export default function App() {
  return (
    <ThemeProvider>
      <AppShell />
    </ThemeProvider>
  );
}
