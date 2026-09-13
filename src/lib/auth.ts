import { nationalPhone } from "./accountValidation";
import { getAccessToken } from "./api/client";
import type { AuthUser } from "./api/auth";
import { getSessionUser, setSessionUser } from "./session";

export type CustomerProfile = {
  name: string;
  email: string;
  phone: string;
  countryCode: string;
  firstName: string;
  lastName: string;
  address: string;
  role?: string;
  accountStatus?: string;
};

export function homePathForRole(role?: string | null): string {
  const normalized = (role ?? "").trim().toLowerCase();
  if (normalized === "partner") return "/vendor";
  if (normalized === "admin") return "/admin";
  return "/dashboard";
}

export function isLoggedIn(): boolean {
  if (!getAccessToken()) return false;
  const user = getSessionUser();
  if (!user) return Boolean(getAccessToken());
  return user.accountStatus !== "guest" && user.authKind !== "guest_request";
}

export function hasRequestSession(): boolean {
  return Boolean(getAccessToken());
}

export function isGuestAccountStatus(status?: string | null): boolean {
  return (status ?? "registered") === "guest";
}

export function getUserRole(): string {
  return (getSessionUser()?.role ?? "customer").toLowerCase();
}

export function getUserName(): string {
  const user = getSessionUser();
  return user?.fullName?.trim() || user?.email || "Guest";
}

export function getCustomerProfile(): CustomerProfile {
  const user = getSessionUser();
  if (!user) {
    return {
      name: "",
      email: "",
      phone: "",
      countryCode: "+91",
      firstName: "",
      lastName: "",
      address: "",
      accountStatus: "registered",
    };
  }
  return {
    name: user.fullName?.trim() || user.email,
    email: user.email,
    phone: nationalPhone(user.phone),
    countryCode: user.phoneCountryCode || "+91",
    firstName: user.firstName ?? "",
    lastName: user.lastName ?? "",
    address: user.address ?? "",
    role: user.role,
    accountStatus: user.accountStatus ?? "registered",
  };
}

export function applyAuthUser(user: AuthUser): void {
  setSessionUser(user);
}

export function isRegisteredAccount(): boolean {
  return isLoggedIn();
}

export function logOut(): void {
  setSessionUser(null);
}

/** Server user id only. Never invent a customer key. */
export function getCustomerId(): string | null {
  return getSessionUser()?.id ?? null;
}
