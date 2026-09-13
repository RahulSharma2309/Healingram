const NAME = /^[\p{L}][\p{L} .'-]{0,59}$/u;
const EMAIL = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}$/i;

export const INDIA_COUNTRY_CODE = "+91";

export const PASSWORD_RULES = [
  "At least 8 characters",
  "At least one letter",
  "At least one number",
  "At least one special character, such as ! @ # $ %",
] as const;

export type AccountFields = {
  firstName: string;
  lastName: string;
  phone: string;
  email: string;
  address?: string;
  password?: string;
  confirmPassword?: string;
};

export type AccountHints = {
  firstName?: string;
  lastName?: string;
  phone?: string;
  email?: string;
  password?: string;
  confirmPassword?: string;
  address?: string;
};

const COPY: Record<string, string> = {
  "firstName is required": "First name is required.",
  "lastName is required": "Last name is required.",
  "phone must be exactly 10 digits": "Enter exactly 10 digits.",
  "phone must start with 6, 7, 8, or 9": "Indian mobiles start with 6, 7, 8, or 9.",
  "email is required": "Email is required.",
  "email is not valid": "Enter a valid email, like name@example.com.",
  "password must be at least 8 characters and include a letter, a number, and a special character":
    "Password must be at least 8 characters and include a letter, a number, and a special character.",
  "passwords do not match": "Passwords do not match.",
  "address must be 200 characters or fewer": "Address must be 200 characters or fewer.",
};

export function validateAccount(fields: AccountFields, requirePassword = false): string[] {
  const details: string[] = [];

  if (!isName(fields.firstName)) {
    details.push("firstName is required");
  }

  if (!isName(fields.lastName)) {
    details.push("lastName is required");
  }

  details.push(...phoneErrors(fields.phone));

  const email = fields.email?.trim() ?? "";
  if (!email) {
    details.push("email is required");
  } else if (!isEmail(email)) {
    details.push("email is not valid");
  }

  const address = fields.address?.trim() ?? "";
  if (address.length > 200) {
    details.push("address must be 200 characters or fewer");
  }

  if (requirePassword) {
    if (!isStrongPassword(fields.password)) {
      details.push(
        "password must be at least 8 characters and include a letter, a number, and a special character",
      );
    }
    if (fields.password !== fields.confirmPassword) {
      details.push("passwords do not match");
    }
  }

  return details;
}

export function accountHints(details: string[]): AccountHints {
  const hints: AccountHints = {};
  for (const item of details) {
    const message = COPY[item] ?? item;
    if (item.startsWith("firstName")) hints.firstName = message;
    else if (item.startsWith("lastName")) hints.lastName = message;
    else if (item.startsWith("phone")) hints.phone = message;
    else if (item.startsWith("email")) hints.email = message;
    else if (item === "passwords do not match") hints.confirmPassword = message;
    else if (item.startsWith("password")) hints.password = message;
    else if (item.startsWith("address")) hints.address = message;
  }
  return hints;
}

export function normalizePhone(raw: string | null | undefined): string | null {
  const national = nationalDigits(raw);
  if (national.length === 10 && national[0] >= "6" && national[0] <= "9") {
    return `${INDIA_COUNTRY_CODE}${national}`;
  }
  return null;
}

export function nationalPhone(raw: string | null | undefined): string {
  const normalized = normalizePhone(raw);
  if (normalized) return normalized.slice(INDIA_COUNTRY_CODE.length);
  return nationalDigits(raw).slice(0, 10);
}

export function digitsOnlyPhone(raw: string): string {
  return raw.replace(/\D/g, "").slice(0, 10);
}

function phoneErrors(raw: string | null | undefined): string[] {
  const national = nationalDigits(raw);
  if (national.length !== 10) {
    return ["phone must be exactly 10 digits"];
  }
  if (national[0] < "6" || national[0] > "9") {
    return ["phone must start with 6, 7, 8, or 9"];
  }
  return [];
}

function nationalDigits(raw: string | null | undefined): string {
  let digits = (raw ?? "").replace(/\D/g, "");
  if (digits.length === 12 && digits.startsWith("91")) {
    digits = digits.slice(2);
  }
  return digits;
}

function isName(raw: string | null | undefined): boolean {
  const trimmed = raw?.trim() ?? "";
  return trimmed.length >= 1 && trimmed.length <= 60 && NAME.test(trimmed);
}

function isEmail(raw: string | null | undefined): boolean {
  const email = raw?.trim() ?? "";
  return email.length >= 6 && email.length <= 254 && !email.includes("..") && EMAIL.test(email);
}

function isStrongPassword(password: string | null | undefined): boolean {
  if (!password || password.length < 8) return false;
  return /[A-Za-z]/.test(password) && /\d/.test(password) && /[^A-Za-z0-9]/.test(password);
}
