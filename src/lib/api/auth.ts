import { applyAuthUser } from "../auth";
import { hydrateWishlistFromServer } from "../wishlist";
import { ApiError, apiErrorMessage, apiFetch, getRefreshToken, setAccessToken, setRefreshToken } from "./client";

export type PartnerMembership = {
  partnerId: string;
  partnerName: string;
  role: string;
  status: string;
};

export type AuthUser = {
  id: string;
  email: string;
  fullName: string | null;
  role: string;
  roles?: string[];
  firstName?: string | null;
  lastName?: string | null;
  phone?: string | null;
  address?: string | null;
  phoneCountryCode?: string | null;
  accountStatus?: string | null;
  partnerMemberships?: PartnerMembership[];
  authKind?: string | null;
};

export function userHasRole(user: Pick<AuthUser, "role" | "roles">, role: string): boolean {
  const wanted = role.toLowerCase();
  if (user.role.toLowerCase() === wanted) return true;
  return (user.roles ?? []).some((item) => item.toLowerCase() === wanted);
}

export function userHasActivePartnerMembership(user: Pick<AuthUser, "partnerMemberships">): boolean {
  return (user.partnerMemberships ?? []).some((item) => item.status.toLowerCase() === "active");
}

export type TokenResponse = {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
};

export function persistSession(tokens: TokenResponse): void {
  setAccessToken(tokens.accessToken);
  setRefreshToken(tokens.refreshToken);
}

export function clearSession(): void {
  setAccessToken(null);
  setRefreshToken(null);
}

export async function loginWithPassword(
  email: string,
  password: string,
  persist = true,
  portal?: "customer" | "vendor" | "admin",
): Promise<TokenResponse> {
  const tokens = await apiFetch<TokenResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password, portal }),
  });
  if (persist) {
    await persistAuthenticatedSession(tokens);
  }
  return tokens;
}

export async function persistAuthenticatedSession(tokens: TokenResponse): Promise<void> {
  persistSession(tokens);
  applyAuthUser(tokens.user);
  if (tokens.user.accountStatus !== "guest") {
    try {
      await hydrateWishlistFromServer();
    } catch {
      /* wishlist is not session-critical */
    }
  }
  window.dispatchEvent(new Event("healingram-auth"));
}

export async function registerAccount(input: {
  firstName: string;
  lastName: string;
  phone: string;
  email: string;
  password: string;
  confirmPassword: string;
  address?: string;
}): Promise<TokenResponse> {
  const tokens = await apiFetch<TokenResponse>("/api/auth/register", {
    method: "POST",
    body: JSON.stringify(input),
  });
  persistSession(tokens);
  applyAuthUser(tokens.user);
  try {
    await hydrateWishlistFromServer();
  } catch {
    /* wishlist is not session-critical */
  }
  return tokens;
}

export async function startGuestVerification(input: {
  email?: string;
  phone?: string;
  channel: "email" | "phone";
  publicId?: string;
  purpose?: string;
}): Promise<{ sent: boolean; demoCode?: string }> {
  return apiFetch("/api/auth/guest/verify-start", {
    method: "POST",
    body: JSON.stringify({ ...input, purpose: input.purpose ?? "REQUEST_ACCESS" }),
  });
}

export async function verifyGuestRequest(input: {
  email?: string;
  phone?: string;
  code: string;
  publicId?: string;
  purpose?: string;
}): Promise<TokenResponse | null> {
  const result = await apiFetch<TokenResponse & { matched?: boolean }>("/api/auth/guest/verify", {
    method: "POST",
    body: JSON.stringify({ ...input, purpose: input.purpose ?? "REQUEST_ACCESS" }),
  });
  if (result.matched === false || !result.accessToken) {
    return null;
  }

  persistSession(result);
  applyAuthUser(result.user);
  if (result.user.accountStatus !== "guest") {
    try {
      await hydrateWishlistFromServer();
    } catch {
      /* wishlist is not session-critical */
    }
  }
  return result;
}

export async function fetchCurrentUser(): Promise<AuthUser> {
  return apiFetch<AuthUser>("/api/users/me");
}

export async function updateProfile(input: {
  firstName: string;
  lastName: string;
  phone: string;
  email: string;
  address?: string;
}): Promise<AuthUser> {
  return apiFetch<AuthUser>("/api/users/me", {
    method: "PATCH",
    body: JSON.stringify(input),
  });
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
    if (error.status === 401) {
      if (error.message === "No request found for that email or mobile") return error.message;
      if (error.message === "This account is not an admin.") return error.message;
      if (error.message === "This account is not a retreat partner.") return error.message;
      return "Email or password is not right.";
    }
    if (error.status === 409) return "That email is already registered.";
    if (error.status === 400) {
      const details = validationDetails(error.body);
      if (details.length > 0) return details.join(". ");
      return error.message || "Check the form and try again.";
    }
    if (error.status === 429) return apiErrorMessage(error);
    return error.message;
  }
  return apiErrorMessage(error);
}

function validationDetails(body: unknown): string[] {
  if (body && typeof body === "object" && "details" in body && Array.isArray(body.details)) {
    return body.details.filter((item): item is string => typeof item === "string");
  }
  return [];
}
