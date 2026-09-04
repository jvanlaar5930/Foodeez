/**
 * Jest setup for the rendering tests.
 *
 * The theme provider reads a stored preference from expo-secure-store, which has no
 * implementation outside a device build. An in-memory stand-in is enough: what the tests care
 * about is that a preference can be read back, not where it was kept.
 *
 * The map lives inside the factory because jest hoists the mock above everything else in the
 * file, so a variable declared out here does not exist yet when the factory runs.
 */
jest.mock('expo-secure-store', () => {
  const store = new Map<string, string>();

  return {
    getItemAsync: jest.fn(async (key: string) => store.get(key) ?? null),
    setItemAsync: jest.fn(async (key: string, value: string) => {
      store.set(key, value);
    }),
    deleteItemAsync: jest.fn(async (key: string) => {
      store.delete(key);
    }),
  };
});

/**
 * Keeps React Native's Animated on the JS driver in tests.
 *
 * A native-driven animation reaches for the renderer shim bundled inside react-native, which
 * is built against React 19.1 while the app runs React 19.2 - so merely mounting a screen
 * that animates threw "Incompatible React versions". Nothing here is testing an animation;
 * the JS driver renders the same tree without loading that shim.
 */
jest.mock('react-native/src/private/animated/NativeAnimatedHelper', () => ({
  __esModule: true,
  default: {
    API: {
      flushQueue: jest.fn(),
      createAnimatedNode: jest.fn(),
      startListeningToAnimatedNodeValue: jest.fn(),
      stopListeningToAnimatedNodeValue: jest.fn(),
      connectAnimatedNodes: jest.fn(),
      disconnectAnimatedNodes: jest.fn(),
      startAnimatingNode: jest.fn(),
      stopAnimation: jest.fn(),
      setAnimatedNodeValue: jest.fn(),
      setAnimatedNodeOffset: jest.fn(),
      flattenAnimatedNodeOffset: jest.fn(),
      extractAnimatedNodeOffset: jest.fn(),
      connectAnimatedNodeToView: jest.fn(),
      disconnectAnimatedNodeFromView: jest.fn(),
      restoreDefaultValues: jest.fn(),
      dropAnimatedNode: jest.fn(),
      addAnimatedEventToView: jest.fn(),
      removeAnimatedEventFromView: jest.fn(),
    },
    // The one that matters: reporting false keeps every animation on the JS driver.
    isNativeAnimatedAvailable: () => false,
    shouldUseNativeDriver: () => false,
    assertNativeAnimatedModule: jest.fn(),
    generateNewNodeTag: () => 1,
    generateNewAnimationId: () => 1,
    transformDataType: (value: unknown) => value,
  },
}));
