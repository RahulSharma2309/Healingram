const AUTH_KEY = "healingram_logged_in";
const NAME_KEY = "healingram_user_name";
const EMAIL_KEY = "healingram_user_email";
const PHONE_KEY = "healingram_user_phone";
const COUNTRY_KEY = "healingram_user_country";

export type CustomerProfile = {
  name: string;
  email: string;
  phone: string;
  countryCode: string;
};

export function isLoggedIn(): boolean {
  try {
    return localStorage.getItem(AUTH_KEY) === "1";
  } catch {
    return false;
  }
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
    };
  } catch {
    return { name: "", email: "", phone: "", countryCode: "+91" };
  }
}

export function saveCustomerProfile(profile: Partial<CustomerProfile>): void {
  try {
    if (profile.name != null) localStorage.setItem(NAME_KEY, profile.name);
    if (profile.email != null) localStorage.setItem(EMAIL_KEY, profile.email);
    if (profile.phone != null) localStorage.setItem(PHONE_KEY, profile.phone);
    if (profile.countryCode != null) localStorage.setItem(COUNTRY_KEY, profile.countryCode);
    window.dispatchEvent(new Event("healingram-auth"));
  } catch {
    /* ignore */
  }
}

export function logIn(name = "Priya", extras?: Partial<CustomerProfile>): void {
  try {
    localStorage.setItem(AUTH_KEY, "1");
    localStorage.setItem(NAME_KEY, name);
    if (extras?.email) localStorage.setItem(EMAIL_KEY, extras.email);
    else if (!localStorage.getItem(EMAIL_KEY)) {
      localStorage.setItem(EMAIL_KEY, "priya@example.com");
    }
    if (extras?.phone) localStorage.setItem(PHONE_KEY, extras.phone);
    else if (!localStorage.getItem(PHONE_KEY)) {
      localStorage.setItem(PHONE_KEY, "9876543210");
    }
    if (extras?.countryCode) localStorage.setItem(COUNTRY_KEY, extras.countryCode);
    else if (!localStorage.getItem(COUNTRY_KEY)) {
      localStorage.setItem(COUNTRY_KEY, "+91");
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
