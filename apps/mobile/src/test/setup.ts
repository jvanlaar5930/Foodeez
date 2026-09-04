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
