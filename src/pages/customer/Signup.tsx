import { FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { authErrorMessage, registerAccount } from "../../lib/api/auth";
import { homePathForRole, logIn } from "../../lib/auth";

export function Signup() {
  const navigate = useNavigate();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const session = await registerAccount({
        email: email.trim(),
        password,
        fullName: fullName.trim(),
      });
      logIn(session.user.fullName || session.user.email, {
        email: session.user.email,
        role: session.user.role,
      });
      navigate(homePathForRole(session.user.role));
    } catch (err) {
      setError(authErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="max-w-md mx-auto px-4 py-16">
      <h1 className="font-display text-2xl font-bold text-sage-800 text-center mb-8">Create your account</h1>
      <form className="bg-white rounded-2xl border border-sand-200 p-6 space-y-4" onSubmit={onSubmit}>
        <label className="block">
          <span className="text-xs text-gray-500">Full name</span>
          <input
            required
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            className="w-full mt-1 border border-sand-200 rounded-lg px-3 py-2"
            autoComplete="name"
          />
        </label>
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
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full mt-1 border border-sand-200 rounded-lg px-3 py-2"
            autoComplete="new-password"
          />
        </label>
        {error ? <p className="text-sm text-red-700">{error}</p> : null}
        <button
          type="submit"
          disabled={busy}
          className="block w-full py-3 bg-teal-600 text-white text-center font-medium rounded-xl hover:bg-teal-500 disabled:opacity-60"
        >
          {busy ? "Creating…" : "Sign up"}
        </button>
      </form>
      <p className="text-center text-sm text-gray-500 mt-4">
        Already have an account? <Link to="/login" className="text-teal-600">Log in</Link>
      </p>
    </div>
  );
}
