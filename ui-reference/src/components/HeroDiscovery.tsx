import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
  type KeyboardEvent as ReactKeyboardEvent,
} from "react";
import { Link, useNavigate } from "react-router-dom";
import { ChevronDown } from "lucide-react";
import {
  HERO_DISCOVERY_OPTIONS,
  type HeroDiscoveryOption,
} from "../data/launchSupply";

/** Placeholder results route — filter state via query until results page is rebuilt */
const RESULTS_PATH = "/search";
const FIND_MY_MATCH_PATH = "/questionnaire";

export function HeroDiscovery() {
  const navigate = useNavigate();
  const listboxId = useId();
  const labelId = useId();
  const rootRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<HeroDiscoveryOption | null>(null);
  const [highlight, setHighlight] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const close = useCallback(() => {
    setOpen(false);
    setHighlight(0);
  }, []);

  useEffect(() => {
    if (!open) return;
    const onPointer = (e: MouseEvent) => {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) {
        close();
      }
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        e.preventDefault();
        close();
        buttonRef.current?.focus();
      }
    };
    document.addEventListener("mousedown", onPointer);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onPointer);
      document.removeEventListener("keydown", onKey);
    };
  }, [open, close]);

  const pick = (option: HeroDiscoveryOption) => {
    setSelected(option);
    setError(null);
    close();
    buttonRef.current?.focus();
  };

  const onTriggerKeyDown = (e: ReactKeyboardEvent<HTMLButtonElement>) => {
    if (e.key === "ArrowDown" || e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      setOpen(true);
      setHighlight(
        selected
          ? Math.max(
              0,
              HERO_DISCOVERY_OPTIONS.findIndex((o) => o.id === selected.id),
            )
          : 0,
      );
    }
  };

  const onListKeyDown = (e: ReactKeyboardEvent<HTMLUListElement>) => {
    const last = HERO_DISCOVERY_OPTIONS.length - 1;
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setHighlight((i) => (i >= last ? 0 : i + 1));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setHighlight((i) => (i <= 0 ? last : i - 1));
    } else if (e.key === "Home") {
      e.preventDefault();
      setHighlight(0);
    } else if (e.key === "End") {
      e.preventDefault();
      setHighlight(last);
    } else if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      pick(HERO_DISCOVERY_OPTIONS[highlight]);
    } else if (e.key === "Escape") {
      e.preventDefault();
      close();
      buttonRef.current?.focus();
    } else if (e.key === "Tab") {
      close();
    }
  };

  const findRetreats = () => {
    if (!selected) {
      setError("Choose what you’re looking for first.");
      setOpen(true);
      buttonRef.current?.focus();
      return;
    }
    setError(null);
    if (selected.programme === null) {
      navigate(FIND_MY_MATCH_PATH);
      return;
    }
    const params = new URLSearchParams();
    params.set("need", selected.id);
    params.set("programme", selected.programme);
    navigate(`${RESULTS_PATH}?${params.toString()}`);
  };

  return (
    <div className="w-full max-w-xl mx-auto text-left">
      <div
        ref={rootRef}
        className="flex flex-col sm:flex-row sm:items-stretch gap-3"
      >
        <div className="relative flex-1 min-w-0">
          <label
            id={labelId}
            className="block text-[11px] font-semibold uppercase tracking-wide text-teal-700/90 mb-1.5 px-0.5"
          >
            What are you looking for?
          </label>
          <button
            ref={buttonRef}
            type="button"
            aria-haspopup="listbox"
            aria-expanded={open}
            aria-controls={listboxId}
            aria-labelledby={labelId}
            aria-invalid={error ? true : undefined}
            onClick={() => {
              setOpen((v) => !v);
              setError(null);
            }}
            onKeyDown={onTriggerKeyDown}
            className={`w-full flex items-center justify-between gap-3 rounded-xl bg-white px-4 py-3.5 text-left shadow-sm ring-1 transition
              ${
                error
                  ? "ring-amber-500/70"
                  : open
                    ? "ring-teal-500"
                    : "ring-sand-200 hover:ring-sand-300"
              }
              focus:outline-none focus-visible:ring-2 focus-visible:ring-teal-500 focus-visible:ring-offset-2 focus-visible:ring-offset-transparent`}
          >
            <span
              className={`text-sm truncate ${
                selected ? "text-sage-800 font-medium" : "text-gray-400"
              }`}
            >
              {selected ? selected.label : "Choose what you need"}
            </span>
            <ChevronDown
              className={`w-4 h-4 shrink-0 text-sage-500 transition-transform ${
                open ? "rotate-180" : ""
              }`}
              aria-hidden
            />
          </button>

          {open && (
            <ul
              id={listboxId}
              role="listbox"
              aria-labelledby={labelId}
              tabIndex={-1}
              onKeyDown={onListKeyDown}
              ref={(el) => {
                el?.focus();
              }}
              className="absolute z-20 mt-2 w-full max-h-64 overflow-y-auto rounded-xl border border-sand-200 bg-white py-1.5 shadow-lg focus:outline-none"
            >
              {HERO_DISCOVERY_OPTIONS.map((option, index) => {
                const isSelected = selected?.id === option.id;
                const isActive = highlight === index;
                return (
                  <li
                    key={option.id}
                    role="option"
                    aria-selected={isSelected}
                    id={`${listboxId}-${option.id}`}
                    onMouseEnter={() => setHighlight(index)}
                    onClick={() => pick(option)}
                    className={`cursor-pointer px-4 py-2.5 text-sm transition-colors ${
                      isActive ? "bg-sand-100 text-sage-800" : "text-sage-700"
                    } ${isSelected ? "font-semibold text-teal-700" : "font-medium"} hover:bg-sand-100`}
                  >
                    {option.label}
                  </li>
                );
              })}
            </ul>
          )}
        </div>

        <div className="sm:pt-[22px] shrink-0">
          <button
            type="button"
            onClick={findRetreats}
            className="w-full sm:w-auto sm:min-w-[9.5rem] inline-flex items-center justify-center rounded-xl bg-teal-600 px-6 py-3.5 text-sm font-semibold text-white shadow-sm hover:bg-teal-500 focus:outline-none focus-visible:ring-2 focus-visible:ring-white/80 focus-visible:ring-offset-2 focus-visible:ring-offset-teal-800 transition"
          >
            Find Retreats
          </button>
        </div>
      </div>

      {error && (
        <p
          role="alert"
          className="mt-2.5 text-sm text-amber-100/95 px-0.5"
        >
          {error}
        </p>
      )}

      <p className="mt-5 text-center sm:text-left text-sm text-white/85">
        Not sure what you need?{" "}
        <Link
          to={FIND_MY_MATCH_PATH}
          className="font-semibold text-teal-100 underline decoration-teal-200/50 underline-offset-4 hover:text-white hover:decoration-white transition"
        >
          Find My Match →
        </Link>
      </p>
    </div>
  );
}
