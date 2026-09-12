import { mergeWishlistOnLogin } from "../wishlist";
import { ApiError, apiFetch, setAccessToken } from "./client";

const REFRESH_KEY = "healingram_refresh_token";

export type AuthUser = {
  id: string;
  email: string;
  fullName: string | null;
  role: string;
};

export type TokenResponse = {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
};

function getRefreshToken(): string | null {
  try {
    return sessionStorage.getItem(REFRESH_KEY);
  } catch {
    return null;
  }
}

function persistSession(tokens: TokenResponse): void {
  setAccessToken(tokens.accessToken);
  try {
    sessionStorage.setItem(REFRESH_KEY, tokens.refreshToken);
  } catch {
    /* ignore */
  }
}

export function clearSession(): void {
  setAccessToken(null);
  try {
    sessionStorage.removeItem(REFRESH_KEY);
  } catch {
    /* ignore */
  }
}

export async function loginWithPassword(email: string, password: string): Promise<TokenResponse> {
  const tokens = await apiFetch<TokenResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
  persistSession(tokens);
  await mergeWishlistOnLogin();
  return tokens;
}

export async function registerAccount(input: {
  email: string;
  password: string;
  fullName: string;
}): Promise<TokenResponse> {
  const tokens = await apiFetch<TokenResponse>("/api/auth/register", {
    method: "POST",
    body: JSON.stringify(input),
  });
  persistSession(tokens);
  await mergeWishlistOnLogin();
  return tokens;
}

export async function logoutRemote(): Promise<void> {
  const refreshToken = getRefreshToken();
  try {
    if (refreshToken) {
      await apiFetch("/api/auth/logout", {
        method: "POST",
        body: JSON.stringify({ refreshToken }),
      });
    }
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
  } finally {
    clearSession();
  }
}

export function authErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 401) return "Email or password is not right.";
    if (error.status === 409) return "That email is already registered.";
    if (error.status === 400) return error.message || "Check the form and try again.";
    return error.message;
  }
  return "Cannot reach the server. Is the gateway running on port 5000?";
}
