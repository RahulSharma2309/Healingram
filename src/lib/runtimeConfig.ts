export type PortalName = "customer" | "vendor" | "admin";

export function parseDemoModeFlag(value: string | undefined | null): boolean {
  return String(value ?? "").trim().toLowerCase() === "true";
}

export function isDemoMode(): boolean {
  return parseDemoModeFlag(import.meta.env.VITE_DEMO_MODE);
}

export function customerAppUrl(): string {
  return trimUrl(import.meta.env.VITE_CUSTOMER_APP_URL) || window.location.origin;
}

export function vendorAppUrl(): string {
  return trimUrl(import.meta.env.VITE_VENDOR_APP_URL) || `${window.location.origin}/vendor`;
}

export function adminAppUrl(): string {
  return trimUrl(import.meta.env.VITE_ADMIN_APP_URL) || `${window.location.origin}/admin`;
}

export function configuredPortal(hostname = window.location.hostname): PortalName | null {
  const env = import.meta.env.VITE_PORTAL as string | undefined;
  if (env === "vendor" || env === "admin" || env === "customer") return env;
  const host = hostname.toLowerCase();
  if (host.startsWith("vendor.") || host.includes("vendor.healingram")) return "vendor";
  if (host.startsWith("admin.") || host.includes("admin.healingram")) return "admin";
  return null;
}

export function resolvePortal(hostname = window.location.hostname, pathname = window.location.pathname): PortalName {
  return configuredPortal(hostname)
    ?? (pathname === "/vendor" || pathname.startsWith("/vendor/")
      ? "vendor"
      : pathname === "/admin" || pathname.startsWith("/admin/")
        ? "admin"
        : "customer");
}

export function staffLoginDestination(portal: "vendor" | "admin", from: string): string {
  if (configuredPortal() === portal) {
    if (from === `/${portal}/login` || from === "/login") return "/";
    return from.startsWith("/") ? from : "/";
  }
  const prefix = `/${portal}`;
  if (from.startsWith(prefix) && from !== `${prefix}/login`) return from;
  return prefix;
}

export function vendorPortalHref(): string {
  const url = vendorAppUrl();
  if (sameOrigin(url)) return url.endsWith("/vendor") ? url : `${url.replace(/\/$/, "")}/vendor`;
  return url;
}

export function adminPortalHref(): string {
  const url = adminAppUrl();
  if (sameOrigin(url)) return url.endsWith("/admin") ? url : `${url.replace(/\/$/, "")}/admin`;
  return url;
}

export function shouldRedirectStaffPath(portal: PortalName, pathname: string): string | null {
  if (portal === "customer" && (pathname === "/vendor" || pathname.startsWith("/vendor/"))) {
    const target = vendorAppUrl();
    if (!sameOrigin(target)) return target;
  }
  if (portal === "customer" && (pathname === "/admin" || pathname.startsWith("/admin/"))) {
    const target = adminAppUrl();
    if (!sameOrigin(target)) return target;
  }
  return null;
}

function trimUrl(value: unknown): string {
  return typeof value === "string" ? value.trim().replace(/\/$/, "") : "";
}

function sameOrigin(url: string): boolean {
  try {
    return new URL(url, window.location.origin).origin === window.location.origin;
  } catch {
    return true;
  }
}
