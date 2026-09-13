import { digitsOnlyPhone, INDIA_COUNTRY_CODE } from "../../lib/accountValidation";
import { FieldHint } from "./FieldHint";

const inputClass =
  "w-full border border-sand-200 rounded-r-lg px-3 py-2 disabled:bg-sand-50 disabled:text-sage-700";

export function IndiaPhoneField({
  value,
  onChange,
  disabled = false,
  error,
}: {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  error?: string;
}) {
  return (
    <label className="block">
      <span className="text-xs text-gray-500">Phone number</span>
      <div className="mt-1 flex">
        <span
          className="inline-flex items-center gap-1.5 shrink-0 px-3 border border-sand-200 border-r-0 rounded-l-lg bg-sand-50 text-sm text-sage-800"
          aria-label="India country code"
        >
          <span aria-hidden="true">🇮🇳</span>
          {INDIA_COUNTRY_CODE}
        </span>
        <input
          required
          disabled={disabled}
          type="tel"
          inputMode="numeric"
          maxLength={10}
          value={value}
          onChange={(event) => onChange(digitsOnlyPhone(event.target.value))}
          className={inputClass}
          autoComplete="tel-national"
          placeholder="10-digit mobile"
        />
      </div>
      <FieldHint message={error} />
    </label>
  );
}
