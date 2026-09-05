/**
 * How the services reach the signed-in session without importing the store.
 *
 * The store imports the services (to sign in), so a service importing the store back is a
 * cycle. Both the axios interceptor and the SSE client used to work around that with a
 * `require('@/store/authStore')` inside the function body - which does defer the resolution,
 * but hides the dependency from every tool that reads imports, defeats bundler tree-shaking,
 * and is a plain `any` at the far end.
 *
 * The store registers itself here instead, once, at module load. The dependency now points
 * one way and is visible in the import graph.
 */
export interface AuthBridge {
  /** The bearer token for an API call, or null when nobody is signed in. */
  getToken(): string | null;
  /** Ends the session. Called when the server says the token is no longer good. */
  logout(): void;
}

let bridge: AuthBridge | null = null;

export function setAuthBridge(next: AuthBridge): void {
  bridge = next;
}

export function getAuthToken(): string | null {
  return bridge?.getToken() ?? null;
}

export function logoutFromApi(): void {
  bridge?.logout();
}
