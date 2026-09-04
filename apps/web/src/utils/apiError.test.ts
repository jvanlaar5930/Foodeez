import { describe, expect, it } from 'vitest';

import { AIStreamError } from '@/services/aiStream';

import { extractErrorMessage } from './apiError';

/**
 * This is the canonical error-message reader. Four more private copies of it currently live
 * inside the Pinia stores, and they do not all behave the same - notably the profile store's
 * copy ignores `message`, so an AI 503 reads as "An error occurred" there while the meal-plan
 * page shows the real reason.
 *
 * These tests pin what the canonical version does, so replacing those copies with it is a
 * verifiable change rather than a hopeful one. See `stores/errorMessages.test.ts`, which
 * records the divergence itself.
 */
describe('extractErrorMessage', () => {
  it('prefers `message`, which is what this API sends for its own plain-object errors', () => {
    const err = { response: { data: { message: 'The AI service is unavailable.' } } };
    expect(extractErrorMessage(err)).toBe('The AI service is unavailable.');
  });

  it('falls back to ProblemDetails `detail`, then `title`', () => {
    expect(extractErrorMessage({ response: { data: { detail: 'Detail text' } } })).toBe('Detail text');
    expect(extractErrorMessage({ response: { data: { title: 'Title text' } } })).toBe('Title text');
  });

  it('orders message over detail over title when several are present', () => {
    const err = {
      response: { data: { message: 'from message', detail: 'from detail', title: 'from title' } },
    };
    expect(extractErrorMessage(err)).toBe('from message');
    expect(extractErrorMessage({ response: { data: { detail: 'from detail', title: 'from title' } } }))
      .toBe('from detail');
  });

  it('shows a stream failure as-is, because it already carries a reader-facing sentence', () => {
    const err = new AIStreamError('The AI service stopped before finishing. Please try again.');
    expect(extractErrorMessage(err)).toBe('The AI service stopped before finishing. Please try again.');
  });

  it('uses the fallback for a network error, which has no response at all', () => {
    const err = Object.assign(new Error('Network Error'), { code: 'ERR_NETWORK' });
    expect(extractErrorMessage(err, 'Could not reach the server.')).toBe('Could not reach the server.');
  });

  it('uses the fallback for a response whose body carries none of the three fields', () => {
    expect(extractErrorMessage({ response: { data: {} } })).toBe('An error occurred');
    expect(extractErrorMessage({ response: {} })).toBe('An error occurred');
  });

  it('never throws on the shapes a catch block can actually receive', () => {
    for (const value of [null, undefined, 'a string', 42, {}, [], new Error('plain')]) {
      expect(() => extractErrorMessage(value)).not.toThrow();
      expect(typeof extractErrorMessage(value)).toBe('string');
    }
  });

  it('defaults the fallback to "An error occurred"', () => {
    expect(extractErrorMessage(null)).toBe('An error occurred');
  });
});
