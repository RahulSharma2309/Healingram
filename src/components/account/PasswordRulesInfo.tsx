import { useEffect, useId, useRef, useState } from "react";
import { Info } from "lucide-react";
import { PASSWORD_RULES } from "../../lib/accountValidation";

export function PasswordRulesInfo() {
  const [open, setOpen] = useState(false);
  const panelId = useId();
  const rootRef = useRef<HTMLSpanElement>(null);

  useEffect(() => {
    if (!open) return;
    const onPointer = (event: MouseEvent) => {
      if (rootRef.current && !rootRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", onPointer);
    return () => document.removeEventListener("mousedown", onPointer);
  }, [open]);

  return (
    <span className="relative inline-flex" ref={rootRef}>
      <button
        type="button"
        className="inline-flex items-center justify-center text-sage-600 hover:text-teal-700"
        aria-label="Password rules"
        aria-expanded={open}
        aria-controls={panelId}
        onClick={() => setOpen((current) => !current)}
      >
        <Info className="size-3.5" />
      </button>
      {open ? (
        <span
          id={panelId}
          role="tooltip"
          className="absolute left-0 top-6 z-20 w-64 rounded-xl border border-sand-200 bg-white p-3 text-xs text-sage-700 shadow-lg"
        >
          <span className="block font-medium text-sage-800 mb-1.5">Password must have:</span>
          <ul className="list-disc pl-4 space-y-1">
            {PASSWORD_RULES.map((rule) => (
              <li key={rule}>{rule}</li>
            ))}
          </ul>
        </span>
      ) : null}
    </span>
  );
}
