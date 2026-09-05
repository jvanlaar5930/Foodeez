import { AxiosError, type AxiosResponse } from 'axios';

import { describeApiError } from './apiError';

/**
 * Five of the seven stores currently ignore this helper and use
 * `err instanceof Error ? err.message : '...'`, which for an axios failure yields
 * "Request failed with status code 400". Phase 6.3 switches them onto this; these tests pin
 * what they will be getting.
 *
 * The BUG-tagged case records a gap this helper still has, so closing it is a visible change.
 */

function axiosErrorWithResponse(status: number, data: unknown): AxiosError {
  const err = new AxiosError('Request failed with status code ' + status, 'ERR_BAD_REQUEST');
  err.response = { status, data, statusText: '', headers: {}, config: {} as never } as AxiosResponse;
  err.config = { method: 'get', url: '/api/thing' } as never;
  return err;
}

function networkError(): AxiosError {
  // No `response` at all - the request never reached the server.
  const err = new AxiosError('Network Error', 'ERR_NETWORK');
  err.config = { method: 'get', url: '/api/thing' } as never;
  return err;
}

beforeEach(() => {
  // The helper logs to the Metro console on the way past; keep the test output readable.
  jest.spyOn(console, 'warn').mockImplementation(() => {});
});

afterEach(() => {
  jest.restoreAllMocks();
});

describe('describeApiError', () => {
  it('reads ProblemDetails detail, then title', () => {
    expect(describeApiError(axiosErrorWithResponse(400, { detail: 'the detail' }), 'fallback'))
      .toBe('the detail');
    expect(describeApiError(axiosErrorWithResponse(400, { title: 'the title' }), 'fallback'))
      .toBe('the title');
    expect(
      describeApiError(axiosErrorWithResponse(400, { detail: 'the detail', title: 'the title' }), 'fallback'),
    ).toBe('the detail');
  });

  it('distinguishes an unreachable server from a rejected request', () => {
    const unreachable = describeApiError(networkError(), 'Login failed.');

    // The point of the distinction: telling someone "wrong password" when the API is simply
    // not running sends them off rechecking credentials that were fine.
    expect(unreachable).not.toBe('Login failed.');
    expect(unreachable).toContain("Can't reach the Foodeez server");
  });

  it('falls back when the server answered but said nothing useful', () => {
    expect(describeApiError(axiosErrorWithResponse(500, {}), 'Something went wrong.'))
      .toBe('Something went wrong.');
    expect(describeApiError(axiosErrorWithResponse(500, undefined), 'Something went wrong.'))
      .toBe('Something went wrong.');
  });

  it('falls back for anything that is not an axios error', () => {
    for (const value of [null, undefined, 'a string', 42, {}, new Error('plain')]) {
      expect(describeApiError(value, 'fallback')).toBe('fallback');
    }
  });

  it('always returns a string, whatever it is handed', () => {
    for (const value of [null, undefined, [], { response: null }, networkError()]) {
      expect(typeof describeApiError(value, 'fallback')).toBe('string');
    }
  });

  it('BUG: ignores `message`, hiding the reason an AI request failed', () => {
    // The API sends { message } for its own errors, including the 503 raised when a provider
    // is unavailable - see AIGenerationFailedException. Web's utils/apiError.ts reads it and
    // this one does not, so the same response reads usefully on web and generically here.
    const err = axiosErrorWithResponse(503, {
      message: 'The AI service could not produce a meal plan right now.',
    });

    expect(describeApiError(err, 'Something went wrong.')).toBe('Something went wrong.');
  });
});
