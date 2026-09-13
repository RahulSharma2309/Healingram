import { type FormEvent, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { authErrorMessage, loginWithPassword, userHasRole } from "../../lib/api/auth";
import { applyAuthUser } from "../../lib/auth";
import { isDemoMode, resolvePortal } from "../../lib/runtimeConfig";

export function AdminLogin() {
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? "/admin";
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const session = await loginWithPassword(email.trim(), password);
      applyAuthUser(session.user);
      if (!userHasRole(session.user, "admin")) {
        setError("This account is not an admin.");
        return;
      }
      const dest = resolvePortal() === "admin" ? "/" : from.startsWith("/admin") ? from : "/admin";
      navigate(dest, { replace: true });
    } catch (err) {
      setError(authErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center px-4">
      <form className="w-full max-w-md bg-white rounded-2xl border p-6 space-y-4" onSubmit={onSubmit}>
        <h1 className="font-display text-2xl font-bold text-center">Admin login</h1>
        <label className="block">
          <span className="text-xs text-gray-500">Email</span>
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full mt-1 border rounded-lg px-3 py-2"
          />
        </label>
        <label className="block">
          <span className="text-xs text-gray-500">Password</span>
          <input
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full mt-1 border rounded-lg px-3 py-2"
          />
        </label>
        {error ? <p className="text-sm text-red-700">{error}</p> : null}
        <button type="submit" disabled={busy} className="block w-full py-3 bg-gray-900 text-white rounded-xl disabled:opacity-60">
          {busy ? "Signing in…" : "Log in"}
        </button>
        {isDemoMode() ? (
          <p className="text-xs text-gray-500">Local demo admin: admin@local.test — Local123!</p>
        ) : null}
      </form>
    </div>
  );
}
