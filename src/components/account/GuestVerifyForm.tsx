import { type FormEvent, useState } from "react";
import { FieldHint } from "./FieldHint";
import { IndiaPhoneField } from "./IndiaPhoneField";
import {
  authErrorMessage,
  LOCAL_GUEST_CODE,
  startGuestVerification,
  verifyGuestRequest,
  type TokenResponse,
} from "../../lib/api/auth";

const fieldClass = "w-full mt-1 border border-sand-200 rounded-lg px-3 py-2";

export function GuestVerifyForm({
  onResolved,
}: {
  onResolved: (session: TokenResponse | null) => void;
}) {
  const [channel, setChannel] = useState<"email" | "phone">("email");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [code, setCode] = useState("");
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const onSend = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await startGuestVerification({
        channel,
        email: channel === "email" ? email.trim() : undefined,
        phone: channel === "phone" ? phone : undefined,
      });
      setSent(true);
    } catch (err) {
      setError(authErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  const onVerify = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const session = await verifyGuestRequest({
        email: channel === "email" ? email.trim() : undefined,
        phone: channel === "phone" ? phone : undefined,
        code: code.trim(),
      });
      onResolved(session);
    } catch (err) {
      setError(authErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  if (!sent) {
    return (
      <form className="mt-8 bg-white rounded-2xl border border-sand-200 p-6 space-y-4" onSubmit={onSend} noValidate>
        <div className="flex gap-2">
          <button
            type="button"
            className={`flex-1 py-2 text-sm rounded-xl border ${
              channel === "email" ? "border-teal-600 text-teal-700 bg-teal-50" : "border-sand-200 text-sage-700"
            }`}
            onClick={() => setChannel("email")}
          >
            Email
          </button>
          <button
            type="button"
            className={`flex-1 py-2 text-sm rounded-xl border ${
              channel === "phone" ? "border-teal-600 text-teal-700 bg-teal-50" : "border-sand-200 text-sage-700"
            }`}
            onClick={() => setChannel("phone")}
          >
            Mobile
          </button>
        </div>
        {channel === "email" ? (
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
          </label>
        ) : (
          <IndiaPhoneField value={phone} onChange={setPhone} />
        )}
        {error ? <p className="text-sm text-red-700">{error}</p> : null}
        <button
          type="submit"
          disabled={busy}
          className="block w-full py-3 bg-teal-600 text-white text-sm font-medium rounded-xl hover:bg-teal-500 disabled:opacity-60"
        >
          {busy ? "Sending…" : "Send verification code"}
        </button>
      </form>
    );
  }

  return (
    <form className="mt-8 bg-white rounded-2xl border border-sand-200 p-6 space-y-4" onSubmit={onVerify} noValidate>
      <p className="text-sm text-sage-700">
        Enter the verification code sent to your {channel === "email" ? "email" : "mobile"}.
      </p>
      <label className="block">
        <span className="text-xs text-gray-500">Verification code</span>
        <input
          required
          inputMode="numeric"
          value={code}
          onChange={(e) => setCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
          className={fieldClass}
          autoComplete="one-time-code"
        />
        <FieldHint message={error ?? undefined} />
      </label>
      <p className="text-xs text-gray-500">Local UAT: enter {LOCAL_GUEST_CODE} (stand-in for SMS/email OTP).</p>
      <button
        type="submit"
        disabled={busy}
        className="block w-full py-3 bg-teal-600 text-white text-sm font-medium rounded-xl hover:bg-teal-500 disabled:opacity-60"
      >
        {busy ? "Checking…" : "View request"}
      </button>
      <button
        type="button"
        className="block w-full text-sm text-sage-600"
        onClick={() => {
          setSent(false);
          setCode("");
          setError(null);
        }}
      >
        Use a different email or mobile
      </button>
    </form>
  );
}
