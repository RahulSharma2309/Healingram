import { isDemoMode, adminPortalHref, vendorPortalHref } from "../lib/runtimeConfig";

export function DemoBanner() {
  if (!isDemoMode()) return null;
  return (
    <div className="bg-sage-800 text-white text-center py-2 px-4 text-sm">
      <span className="opacity-90">Local demo — not a public marketplace.</span>
      <span className="mx-3 opacity-40">|</span>
      <a href={vendorPortalHref()} className="underline hover:text-sage-200">
        Vendor portal
      </a>
      <span className="mx-2 opacity-40">·</span>
      <a href={adminPortalHref()} className="underline hover:text-sage-200">
        Admin portal
      </a>
    </div>
  );
}
