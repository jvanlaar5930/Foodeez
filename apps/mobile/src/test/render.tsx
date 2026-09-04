import React, { type ReactElement, type ReactNode } from 'react';
import { render, type RenderResult } from '@testing-library/react-native';
import { ThemeProvider } from '@/theme';

function Providers({ children }: { children: ReactNode }) {
  return <ThemeProvider>{children}</ThemeProvider>;
}

/**
 * Renders a component inside the providers it needs.
 *
 * Everything on screen reads the palette, so nothing renders without ThemeProvider - and a
 * test that wired one up by hand each time would be a test about wiring.
 *
 * Passed as `wrapper` rather than wrapped by hand, so the `rerender` that comes back keeps
 * the providers. Wrapping by hand loses them on the second render, which unmounts the tree
 * under test.
 */
export function renderWithTheme(ui: ReactElement): RenderResult {
  return render(ui, { wrapper: Providers });
}

export * from '@testing-library/react-native';
