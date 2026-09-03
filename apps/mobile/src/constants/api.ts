import { Platform } from 'react-native';
import Constants from 'expo-constants';

const API_PORT = 5000;

/**
 * Where the API lives, as seen *from the device running this app* - which is not the same
 * address the dev machine uses for itself:
 *
 *   Android emulator   10.0.2.2 is the emulator's alias for the host's loopback
 *   iOS simulator      shares the host's network stack, so localhost works
 *   Physical device    neither of the above resolves; it needs the machine's LAN IP
 *
 * For a physical device we take the host out of the Expo dev-server URI. The bundle already
 * reached this device from that address, so it is by definition an address the device can
 * reach the dev machine on. Set EXPO_PUBLIC_API_URL to override the whole thing (a staging
 * or production API, or a tunnelled dev server).
 */
function resolveApiUrl(): string {
  const override = process.env.EXPO_PUBLIC_API_URL;
  if (override) return override;

  // `hostUri` is "192.168.1.20:8081" in dev; absent in a production build.
  const hostUri =
    Constants.expoConfig?.hostUri ??
    // Older/Expo Go shape, kept as a fallback.
    (Constants as { expoGoConfig?: { debuggerHost?: string } }).expoGoConfig?.debuggerHost;

  const devHost = hostUri?.split(':')[0];
  const isLoopback = devHost === 'localhost' || devHost === '127.0.0.1';

  if (devHost && !isLoopback) {
    return `http://${devHost}:${API_PORT}/api`;
  }

  const emulatorHost = Platform.OS === 'android' ? '10.0.2.2' : 'localhost';
  return `http://${emulatorHost}:${API_PORT}/api`;
}

export const API_URL = resolveApiUrl();

/**
 * Timeout for a request that makes one blocking (non-streamed) AI call - quick-add, photo
 * parsing, nutrition estimates. The app's ordinary requests use a much shorter timeout, but
 * these wait on a model that may be self-hosted and slow, and cutting the request off before
 * it answers discards a response the server was about to deliver.
 */
export const AI_REQUEST_TIMEOUT = 120000;
