import type { ReactNode } from "react";
import { Link, Navigate, useLocation } from "react-router-dom";
import { userHasActivePartnerMembership, userHasRole } from "../../lib/api/auth";
import { useAuth } from "../../lib/auth/AuthProvider";
import { adminPortalHref, vendorPortalHref } from "../../lib/runtimeConfig";

function Forbidden({ title, homeTo }: { title: string; homeTo: string }) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-sand-50 px-4">
      <div className="max-w-md text-center space-y-3">
        <h1 className="font-display text-2xl font-bold text-sage-800">{title}</h1>
        <p className="text-sm text-sage-600">You do not have access to this portal.</p>
        <Link to={homeTo} className="inline-block text-teal-700 underline">
          Go back
        </Link>
      </div>
    </div>
  );
}

export function VendorPortalGuard({ children }: { children: ReactNode }) {
  const location = useLocation();
  const { user, loading } = useAuth();
  if (loading) {
    return <div className="min-h-screen bg-sand-50" />;
  }
  if (!user) {
    return <Navigate to="/vendor/login" replace state={{ from: location.pathname }} />;
  }
  const vendorOk = userHasRole(user, "admin")
    || (userHasRole(user, "partner") && userHasActivePartnerMembership(user));
  if (!vendorOk) {
    return <Forbidden title="Vendor access required" homeTo="/" />;
  }
  return children;
}

export function AdminPortalGuard({ children }: { children: ReactNode }) {
  const location = useLocation();
  const { user, loading } = useAuth();
  if (loading) {
    return <div className="min-h-screen bg-gray-100" />;
  }
  if (!user) {
    return <Navigate to="/admin/login" replace state={{ from: location.pathname }} />;
  }
  if (!userHasRole(user, "admin")) {
    return <Forbidden title="Admin access required" homeTo="/" />;
  }
  return children;
}

export function CustomerPortalGuard({ children }: { children: ReactNode }) {
  return children;
}

export function StaffEntryLinks() {
  return (
    <p className="text-xs text-gray-500">
      <a className="underline" href={vendorPortalHref()}>
        Vendor portal
      </a>
      {" · "}
      <a className="underline" href={adminPortalHref()}>
        Admin portal
      </a>
    </p>
  );
}
