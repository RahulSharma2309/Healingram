import { type FormEvent, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { FieldHint } from "../../components/account/FieldHint";
import { IndiaPhoneField } from "../../components/account/IndiaPhoneField";
import { PasswordRulesInfo } from "../../components/account/PasswordRulesInfo";
import { accountHints, type AccountHints, validateAccount } from "../../lib/accountValidation";
import { authErrorMessage, registerAccount } from "../../lib/api/auth";
import { applyAuthUser, getCustomerProfile, homePathForRole } from "../../lib/auth";

const fieldClass = "w-full mt-1 border border-sand-200 rounded-lg px-3 py-2";

export function Signup() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const stored = getCustomerProfile();
  const [firstName, setFirstName] = useState(stored.firstName);
  const [lastName, setLastName] = useState(stored.lastName);
  const [phone, setPhone] = useState(stored.phone);
  const [email, setEmail] = useState(stored.email);
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [address, setAddress] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [hints, setHints] = useState<AccountHints>({});
  const [busy, setBusy] = useState(false);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    const details = validateAccount(
      { firstName, lastName, phone, email, password, confirmPassword, address },
      true,
    );
    if (details.length > 0) {
      setHints(accountHints(details));
      setError(null);
      return;
    }

    setBusy(true);
    setHints({});
    setError(null);
    try {
      const session = await registerAccount({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        phone,
        email: email.trim(),
        password,
        confirmPassword,
        address: address.trim() || undefined,
      });
      applyAuthUser(session.user);
      const next = searchParams.get("next");
      navigate(next && next.startsWith("/") ? next : homePathForRole(session.user.role));
    } catch (err) {
      setError(authErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="max-w-md mx-auto px-4 py-16">
      <h1 className="font-display text-2xl font-bold text-sage-800 text-center mb-2">Create your Healingram account</h1>
      <p className="text-sm text-sage-600 text-center mb-8">
        Track requests, get updates, and continue to payment when a retreat confirms.
      </p>
      <form className="bg-white rounded-2xl border border-sand-200 p-6 space-y-4" onSubmit={onSubmit} noValidate>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <label className="block">
            <span className="text-xs text-gray-500">First name</span>
            <input
              required
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              className={fieldClass}
              autoComplete="given-name"
            />
            <FieldHint message={hints.firstName} />
          </label>
          <label className="block">
            <span className="text-xs text-gray-500">Last name</span>
            <input
              required
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              className={fieldClass}
              autoComplete="family-name"
            />
            <FieldHint message={hints.lastName} />
          </label>
        </div>
        <IndiaPhoneField value={phone} onChange={setPhone} error={hints.phone} />
        <label className="block">
          <span className="text-xs text-gray-500">Email</span>
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className={fieldClass}
            autoComplete="email"
            placeholder="name@example.com"
          />
          <FieldHint message={hints.email} />
        </label>
        <label className="block">
          <span className="text-xs text-gray-500 inline-flex items-center gap-1.5">
            Password
            <PasswordRulesInfo />
          </span>
          <input
            type="password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className={fieldClass}
            autoComplete="new-password"
          />
          <FieldHint message={hints.password} />
        </label>
        <label className="block">
          <span className="text-xs text-gray-500">Confirm password</span>
          <input
            type="password"
            required
            minLength={8}
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            className={fieldClass}
            autoComplete="new-password"
          />
          <FieldHint message={hints.confirmPassword} />
        </label>
        <label className="block">
          <span className="text-xs text-gray-500">Address (optional)</span>
          <textarea
            value={address}
            onChange={(e) => setAddress(e.target.value)}
            className={fieldClass}
            rows={3}
            maxLength={200}
            autoComplete="street-address"
          />
          <FieldHint message={hints.address} />
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
