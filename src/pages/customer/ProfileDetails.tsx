import { type FormEvent, useEffect, useState } from "react";
import { FieldHint } from "../../components/account/FieldHint";
import { IndiaPhoneField } from "../../components/account/IndiaPhoneField";
import { accountHints, type AccountHints, nationalPhone, validateAccount } from "../../lib/accountValidation";
import { authErrorMessage, fetchCurrentUser, updateProfile } from "../../lib/api/auth";
import { applyAuthUser, getCustomerProfile } from "../../lib/auth";

const fieldClass =
  "w-full mt-1 border border-sand-200 rounded-lg px-3 py-2 disabled:bg-sand-50 disabled:text-sage-700";

export function ProfileDetails() {
  const stored = getCustomerProfile();
  const [firstName, setFirstName] = useState(stored.firstName);
  const [lastName, setLastName] = useState(stored.lastName);
  const [phone, setPhone] = useState(nationalPhone(stored.phone));
  const [email, setEmail] = useState(stored.email);
  const [address, setAddress] = useState(stored.address);
  const [editing, setEditing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hints, setHints] = useState<AccountHints>({});
  const [saved, setSaved] = useState(false);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    void fetchCurrentUser()
      .then((user) => {
        if (cancelled) return;
        applyAuthUser(user);
        setFirstName(user.firstName ?? "");
        setLastName(user.lastName ?? "");
        setPhone(nationalPhone(user.phone));
        setEmail(user.email);
        setAddress(user.address ?? "");
      })
      .catch(() => {
        /* keep localStorage values until the session answers */
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    const details = validateAccount({ firstName, lastName, phone, email, address });
    if (details.length > 0) {
      setHints(accountHints(details));
      setError(null);
      setSaved(false);
      return;
    }

    setBusy(true);
    setHints({});
    setError(null);
    setSaved(false);
    try {
      const user = await updateProfile({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        phone,
        email: email.trim(),
        address: address.trim() || undefined,
      });
      applyAuthUser(user);
      setFirstName(user.firstName ?? "");
      setLastName(user.lastName ?? "");
      setPhone(nationalPhone(user.phone));
      setEmail(user.email);
      setAddress(user.address ?? "");
      setEditing(false);
      setSaved(true);
    } catch (err) {
      setError(authErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  if (loading) {
    return <p className="text-sm text-sage-600">Loading your profile…</p>;
  }

  return (
    <form className="bg-white rounded-xl border border-sand-200 p-6 space-y-4" onSubmit={onSubmit} noValidate>
      <div className="flex items-center justify-between gap-3">
        <h2 className="font-display text-lg font-semibold text-sage-800">Profile details</h2>
        {!editing ? (
          <button
            type="button"
            className="text-sm font-medium text-teal-700 hover:text-teal-600"
            onClick={() => {
              setEditing(true);
              setSaved(false);
              setError(null);
            }}
          >
            Edit profile
          </button>
        ) : null}
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <label className="block">
          <span className="text-xs text-gray-500">First name</span>
          <input
            required
            disabled={!editing}
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
            disabled={!editing}
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
            className={fieldClass}
            autoComplete="family-name"
          />
          <FieldHint message={hints.lastName} />
        </label>
      </div>
      <IndiaPhoneField value={phone} onChange={setPhone} disabled={!editing} error={hints.phone} />
      <label className="block">
        <span className="text-xs text-gray-500">Email</span>
        <input
          required
          disabled={!editing}
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className={fieldClass}
          autoComplete="email"
          placeholder="name@example.com"
        />
        <FieldHint message={hints.email} />
      </label>
      <label className="block">
        <span className="text-xs text-gray-500">Address (optional)</span>
        <textarea
          disabled={!editing}
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
      {saved ? <p className="text-sm text-teal-700">Profile saved.</p> : null}
      {editing ? (
        <div className="flex gap-3">
          <button
            type="submit"
            disabled={busy}
            className="px-4 py-2 bg-teal-600 text-white text-sm font-medium rounded-xl hover:bg-teal-500 disabled:opacity-60"
          >
            {busy ? "Saving…" : "Save changes"}
          </button>
          <button
            type="button"
            disabled={busy}
            className="px-4 py-2 text-sm text-sage-700"
            onClick={() => {
              const latest = getCustomerProfile();
              setFirstName(latest.firstName);
              setLastName(latest.lastName);
              setPhone(nationalPhone(latest.phone));
              setEmail(latest.email);
              setAddress(latest.address);
              setEditing(false);
              setHints({});
              setError(null);
            }}
          >
            Cancel
          </button>
        </div>
      ) : null}
    </form>
  );
}
