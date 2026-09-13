import { nationalPhone } from "./accountValidation";
import { getAccessToken } from "./api/client";

const AUTH_KEY = "healingram_logged_in";
const NAME_KEY = "healingram_user_name";
const EMAIL_KEY = "healingram_user_email";
const PHONE_KEY = "healingram_user_phone";
const COUNTRY_KEY = "healingram_user_country";
const ROLE_KEY = "healingram_user_role";
const FIRST_NAME_KEY = "healingram_user_first_name";
const LAST_NAME_KEY = "healingram_user_last_name";
const ADDRESS_KEY = "healingram_user_address";
const ACCOUNT_STATUS_KEY = "healingram_account_status";

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

/** Display cache only. Never use this as a security decision — call GET /api/users/me. */
export function getUserRole(): string {
  try {
    return (localStorage.getItem(ROLE_KEY) || "customer").toLowerCase();
  } catch {
    return "customer";
  }
}

export function homePathForRole(role?: string | null): string {
  const normalized = (role ?? "").trim().toLowerCase();
  if (normalized === "partner") return "/vendor";
  if (normalized === "admin") return "/admin";
  return "/dashboard";
}

export function isLoggedIn(): boolean {
  if (!getAccessToken()) return false;
  try {
    if (localStorage.getItem(AUTH_KEY) !== "1") return false;
    return (localStorage.getItem(ACCOUNT_STATUS_KEY) || "registered") !== "guest";
  } catch {
    return Boolean(getAccessToken());
  }
}

/** Guest OTP session can read requests without opening Profile / Wishlist / My Trips. */
export function hasRequestSession(): boolean {
  return Boolean(getAccessToken());
}

export function isGuestAccountStatus(status?: string | null): boolean {
  return (status ?? "registered") === "guest";
}

export function getUserName(): string {
  try {
    return localStorage.getItem(NAME_KEY) || "Guest";
  } catch {
    return "Guest";
  }
}

export function getCustomerProfile(): CustomerProfile {
  try {
    return {
      name: localStorage.getItem(NAME_KEY) || "",
      email: localStorage.getItem(EMAIL_KEY) || "",
      phone: localStorage.getItem(PHONE_KEY) || "",
      countryCode: localStorage.getItem(COUNTRY_KEY) || "+91",
      firstName: localStorage.getItem(FIRST_NAME_KEY) || "",
      lastName: localStorage.getItem(LAST_NAME_KEY) || "",
      address: localStorage.getItem(ADDRESS_KEY) || "",
      accountStatus: localStorage.getItem(ACCOUNT_STATUS_KEY) || "registered",
    };
  } catch {
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
}

export function saveCustomerProfile(profile: Partial<CustomerProfile>): void {
  try {
    if (profile.name != null) localStorage.setItem(NAME_KEY, profile.name);
    if (profile.email != null) localStorage.setItem(EMAIL_KEY, profile.email);
    if (profile.phone != null) localStorage.setItem(PHONE_KEY, profile.phone);
    if (profile.countryCode != null) localStorage.setItem(COUNTRY_KEY, profile.countryCode);
    if (profile.firstName != null) localStorage.setItem(FIRST_NAME_KEY, profile.firstName);
    if (profile.lastName != null) localStorage.setItem(LAST_NAME_KEY, profile.lastName);
    if (profile.address != null) localStorage.setItem(ADDRESS_KEY, profile.address);
    if (profile.accountStatus != null) localStorage.setItem(ACCOUNT_STATUS_KEY, profile.accountStatus);
    window.dispatchEvent(new Event("healingram-auth"));
  } catch {
    /* ignore */
  }
}

export function applyAuthUser(user: {
  email: string;
  fullName: string | null;
  role: string;
  firstName?: string | null;
  lastName?: string | null;
  phone?: string | null;
  address?: string | null;
  accountStatus?: string | null;
}): void {
  const firstName = user.firstName?.trim() ?? "";
  const lastName = user.lastName?.trim() ?? "";
  const name = user.fullName?.trim() || `${firstName} ${lastName}`.trim() || user.email;
  const accountStatus = user.accountStatus ?? "registered";
  saveCustomerProfile({
    name,
    email: user.email,
    role: user.role,
    phone: nationalPhone(user.phone),
    firstName,
    lastName,
    address: user.address ?? "",
    countryCode: "+91",
    accountStatus,
  });
  if (accountStatus === "guest") {
    return;
  }

  logIn(name, {
    email: user.email,
    role: user.role,
    phone: nationalPhone(user.phone),
    firstName,
    lastName,
    address: user.address ?? "",
    countryCode: "+91",
    accountStatus,
  });
}

export function isRegisteredAccount(): boolean {
  return isLoggedIn();
}

export function logIn(name = "Guest", extras?: Partial<CustomerProfile>): void {
  try {
    localStorage.setItem(AUTH_KEY, "1");
    localStorage.setItem(NAME_KEY, name);
    if (extras?.email) localStorage.setItem(EMAIL_KEY, extras.email);
    if (extras?.phone != null) localStorage.setItem(PHONE_KEY, extras.phone);
    if (extras?.countryCode) localStorage.setItem(COUNTRY_KEY, extras.countryCode);
    else if (!localStorage.getItem(COUNTRY_KEY)) {
      localStorage.setItem(COUNTRY_KEY, "+91");
    }
    if (extras?.firstName != null) localStorage.setItem(FIRST_NAME_KEY, extras.firstName);
    if (extras?.lastName != null) localStorage.setItem(LAST_NAME_KEY, extras.lastName);
    if (extras?.address != null) localStorage.setItem(ADDRESS_KEY, extras.address);
    if (extras?.accountStatus) localStorage.setItem(ACCOUNT_STATUS_KEY, extras.accountStatus);
    if (extras?.role) localStorage.setItem(ROLE_KEY, extras.role.toLowerCase());
    else if (!localStorage.getItem(ROLE_KEY)) {
      localStorage.setItem(ROLE_KEY, "customer");
    }
    window.dispatchEvent(new Event("healingram-auth"));
  } catch {
    /* ignore */
  }
}

export function logOut(): void {
  try {
    localStorage.removeItem(AUTH_KEY);
    localStorage.removeItem(NAME_KEY);
    localStorage.removeItem(ROLE_KEY);
    localStorage.removeItem(ACCOUNT_STATUS_KEY);
    sessionStorage.removeItem("healingram_access_token");
    sessionStorage.removeItem("healingram_refresh_token");
    window.dispatchEvent(new Event("healingram-auth"));
  } catch {
    /* ignore */
  }
}

export function getCustomerId(): string | null {
  if (!isLoggedIn()) return null;
  try {
    const email = localStorage.getItem(EMAIL_KEY) || getUserName();
    return `cust_${email.toLowerCase().replace(/[^a-z0-9]/g, "_")}`;
  } catch {
    return "cust_guest";
  }
}
