import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import {
  fetchCurrentUser,
  logoutRemote,
  userHasActivePartnerMembership,
  type AuthUser,
} from "../api/auth";
import { getAccessToken } from "../api/client";
import { setSessionUser } from "../session";
import { hydrateWishlistFromServer } from "../wishlist";

export type AuthKind = "registered" | "guest_request" | string;

type AuthContextValue = {
  user: AuthUser | null;
  authenticated: boolean;
  loading: boolean;
  authKind: AuthKind | null;
  roles: string[];
  partnerMemberships: AuthUser["partnerMemberships"];
  refreshUser: () => Promise<AuthUser | null>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

function kindOf(user: AuthUser | null): AuthKind | null {
  if (!user) return null;
  if (user.authKind) return user.authKind;
  if (user.accountStatus === "guest") return "guest_request";
  return "registered";
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  const refreshUser = useCallback(async () => {
    if (!getAccessToken()) {
      setUser(null);
      setSessionUser(null);
      return null;
    }
    try {
      const next = await fetchCurrentUser();
      setUser(next);
      setSessionUser(next);
      if (next.accountStatus !== "guest" && next.authKind !== "guest_request") {
        try {
          await hydrateWishlistFromServer();
        } catch {
          /* wishlist is not session-critical */
        }
      }
      return next;
    } catch {
      setUser(null);
      setSessionUser(null);
      return null;
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      await refreshUser();
      if (!cancelled) setLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [refreshUser]);

  useEffect(() => {
    const onAuth = () => {
      void refreshUser();
    };
    window.addEventListener("healingram-auth", onAuth);
    return () => window.removeEventListener("healingram-auth", onAuth);
  }, [refreshUser]);

  const logout = useCallback(async () => {
    await logoutRemote();
    setUser(null);
    setSessionUser(null);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      authenticated: Boolean(user),
      loading,
      authKind: kindOf(user),
      roles: user?.roles ?? (user ? [user.role] : []),
      partnerMemberships: user?.partnerMemberships ?? [],
      refreshUser,
      logout,
    }),
    [user, loading, refreshUser, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return ctx;
}

export function useAuthOptional(): AuthContextValue | null {
  return useContext(AuthContext);
}

export function userCanUseVendorPortal(user: AuthUser | null): boolean {
  if (!user) return false;
  const roles = user.roles ?? [user.role];
  return roles.some((r) => r.toLowerCase() === "admin")
    || (roles.some((r) => r.toLowerCase() === "partner") && userHasActivePartnerMembership(user));
}
