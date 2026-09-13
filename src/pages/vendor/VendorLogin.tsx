import { type FormEvent, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import {
  authErrorMessage,
  loginWithPassword,
  persistAuthenticatedSession,
  userHasActivePartnerMembership,
  userHasRole,
} from "../../lib/api/auth";
import { applyAuthUser } from "../../lib/auth";
import { isDemoMode, staffLoginDestination } from "../../lib/runtimeConfig";

export function VendorLogin() {
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? "/vendor";
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const session = await loginWithPassword(email.trim(), password, false, "vendor");
      const vendorOk = userHasRole(session.user, "admin")
        || (userHasRole(session.user, "partner") && userHasActivePartnerMembership(session.user));
      if (!vendorOk) {
        setError(
          userHasRole(session.user, "partner")
            ? "This account is not linked to an active partner membership."
            : "This account is not a retreat partner.",
        );
        return;
      }
      await persistAuthenticatedSession(session);
      applyAuthUser(session.user);
      navigate(staffLoginDestination("vendor", from), { replace: true });
    } catch (err) {
      setError(authErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="min-h-screen bg-sand-50 flex items-center justify-center px-4">
      <form className="w-full max-w-md bg-white rounded-2xl border border-sand-200 p-6 space-y-4" onSubmit={onSubmit}>
        <h1 className="font-display text-2xl font-bold text-sage-800 text-center">Vendor login</h1>
        <label className="block">
          <span className="text-xs text-gray-500">Email</span>
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full mt-1 border border-sand-200 rounded-lg px-3 py-2"
          />
        </label>
        <label className="block">
          <span className="text-xs text-gray-500">Password</span>
          <input
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full mt-1 border border-sand-200 rounded-lg px-3 py-2"
          />
        </label>
        {error ? <p className="text-sm text-red-700">{error}</p> : null}
        <button
          type="submit"
          disabled={busy}
          className="block w-full py-3 bg-teal-600 text-white font-medium rounded-xl disabled:opacity-60"
        >
          {busy ? "Signing in…" : "Log in"}
        </button>
        {isDemoMode() ? (
          <p className="text-xs text-gray-500">Local demo partner: partner@local.test — Local123!</p>
        ) : null}
      </form>
    </div>
  );
}
