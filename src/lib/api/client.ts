const TOKEN_KEY = "healingram_access_token";
const REFRESH_KEY = "healingram_refresh_token";

export function apiBaseUrl(): string {
  const raw = import.meta.env.VITE_API_BASE_URL as string | undefined;
  if (typeof raw === "string" && raw.trim().length > 0) {
    return raw.replace(/\/$/, "");
  }
  if (import.meta.env.DEV) {
    return "";
  }
  throw new Error("VITE_API_BASE_URL is required in a production build.");
}

export function getAccessToken(): string | null {
  try {
    return sessionStorage.getItem(TOKEN_KEY);
  } catch {
    return null;
  }
}

export function setAccessToken(token: string | null): void {
  try {
    if (token) sessionStorage.setItem(TOKEN_KEY, token);
    else sessionStorage.removeItem(TOKEN_KEY);
  } catch {
    /* ignore */
  }
}

export function getRefreshToken(): string | null {
  try {
    return sessionStorage.getItem(REFRESH_KEY);
  } catch {
    return null;
  }
}

export function setRefreshToken(token: string | null): void {
  try {
    if (token) sessionStorage.setItem(REFRESH_KEY, token);
    else sessionStorage.removeItem(REFRESH_KEY);
  } catch {
    /* ignore */
  }
}

export class ApiError extends Error {
  readonly status: number;
  readonly body: unknown;

  constructor(status: number, message: string, body: unknown) {
    super(message);
    this.status = status;
    this.body = body;
  }
}

export async function apiFetch<T>(path: string, init: RequestInit = {}, retry = true): Promise<T> {
  const headers = new Headers(init.headers);
  if (!headers.has("Accept")) headers.set("Accept", "application/json");
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  const token = getAccessToken();
  if (token && !headers.has("Authorization")) headers.set("Authorization", `Bearer ${token}`);

  const response = await fetch(`${apiBaseUrl()}${path}`, { ...init, headers });
  if (response.status === 401 && retry && shouldAttemptRefresh(path)) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      return apiFetch<T>(path, init, false);
    }
  }

  const text = await response.text();
  const parsed = text ? safeJson(text) : null;
  if (!response.ok) {
    const message =
      parsed && typeof parsed === "object" && "error" in parsed && typeof parsed.error === "string"
        ? parsed.error
        : `Request failed (${response.status})`;
    throw new ApiError(response.status, message, parsed);
  }
  return parsed as T;
}

async function refreshAccessToken(): Promise<boolean> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) return false;
  try {
    const headers = new Headers({ Accept: "application/json", "Content-Type": "application/json" });
    const response = await fetch(`${apiBaseUrl()}/api/auth/refresh`, {
      method: "POST",
      headers,
      body: JSON.stringify({ refreshToken }),
    });
    if (!response.ok) {
      setAccessToken(null);
      setRefreshToken(null);
      return false;
    }
    const parsed = (await response.json()) as { accessToken?: string; refreshToken?: string };
    if (!parsed.accessToken) {
      setAccessToken(null);
      setRefreshToken(null);
      return false;
    }
    setAccessToken(parsed.accessToken);
    if (parsed.refreshToken) setRefreshToken(parsed.refreshToken);
    return true;
  } catch {
    return false;
  }
}

function shouldAttemptRefresh(path: string): boolean {
  return !path.startsWith("/api/auth/login")
    && !path.startsWith("/api/auth/refresh")
    && !path.startsWith("/api/auth/register");
}

function safeJson(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}
