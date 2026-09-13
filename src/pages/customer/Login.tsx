import { type FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { authErrorMessage, loginWithPassword, userHasRole } from "../../lib/api/auth";
import { applyAuthUser, homePathForRole } from "../../lib/auth";
import { adminPortalHref, isDemoMode, vendorPortalHref } from "../../lib/runtimeConfig";

export function Login() {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const session = await loginWithPassword(email.trim(), password, true, "customer");
      applyAuthUser(session.user);
      if (userHasRole(session.user, "admin")) {
        window.location.assign(adminPortalHref());
        return;
      }
      if (userHasRole(session.user, "partner")) {
        window.location.assign(vendorPortalHref());
        return;
      }
      navigate(homePathForRole(session.user.role));
    } catch (err) {
      setError(authErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="max-w-md mx-auto px-4 py-16">
      <h1 className="font-display text-2xl font-bold text-sage-800 text-center mb-8">Welcome back</h1>
      <form className="bg-white rounded-2xl border border-sand-200 p-6 space-y-4" onSubmit={onSubmit}>
        <label className="block">
          <span className="text-xs text-gray-500">Email</span>
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full mt-1 border border-sand-200 rounded-lg px-3 py-2"
            autoComplete="email"
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
            autoComplete="current-password"
          />
        </label>
        {error ? <p className="text-sm text-red-700">{error}</p> : null}
        <button
          type="submit"
          disabled={busy}
          className="block w-full py-3 bg-teal-600 text-white text-center font-medium rounded-xl hover:bg-teal-500 disabled:opacity-60"
        >
          {busy ? "Signing in…" : "Log in"}
        </button>
        {isDemoMode() ? (
          <p className="text-xs text-gray-500">
            Local demo: guest@local.test / partner@local.test / admin@local.test — password Local123!
          </p>
        ) : null}
      </form>
      <p className="text-center text-sm text-gray-500 mt-4">
        New here? <Link to="/signup" className="text-teal-600">Create account</Link>
      </p>
    </div>
  );
}
