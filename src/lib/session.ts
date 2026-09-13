import type { AuthUser } from "./api/auth";

let currentUser: AuthUser | null = null;
const listeners = new Set<() => void>();

export function setSessionUser(user: AuthUser | null): void {
  currentUser = user;
  listeners.forEach((fn) => fn());
}

export function getSessionUser(): AuthUser | null {
  return currentUser;
}

export function subscribeSession(onChange: () => void): () => void {
  listeners.add(onChange);
  return () => listeners.delete(onChange);
}
