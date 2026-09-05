import React from 'react';
import { LoadingSpinner } from './LoadingSpinner';
import { renderWithTheme } from '@/test/render';

/** A smoke test, mostly proving the rendering setup itself works. */
describe('LoadingSpinner', () => {
  it('renders', () => {
    const { toJSON } = renderWithTheme(<LoadingSpinner />);

    expect(toJSON()).toBeTruthy();
  });
});
