import { useEffect, useId, useRef, useState } from "react";
import { Calendar, MapPin, X } from "lucide-react";
import type { LocationFilterGroup } from "../data/launchSupply";

type ResultsFilterBarProps = {
  location: string;
  locationGroups: LocationFilterGroup[];
  checkIn: string;
  checkOut: string;
  onLocationChange: (location: string) => void;
  onDatesChange: (checkIn: string, checkOut: string) => void;
};

export function ResultsFilterBar({
  location,
  locationGroups,
  checkIn,
  checkOut,
  onLocationChange,
  onDatesChange,
}: ResultsFilterBarProps) {
  const [datesOpen, setDatesOpen] = useState(false);
  const [draftIn, setDraftIn] = useState(checkIn);
  const [draftOut, setDraftOut] = useState(checkOut);
  const [dateError, setDateError] = useState<string | null>(null);
  const datesRef = useRef<HTMLDivElement>(null);
  const datesLabelId = useId();

  useEffect(() => {
    setDraftIn(checkIn);
    setDraftOut(checkOut);
  }, [checkIn, checkOut]);

  useEffect(() => {
    if (!datesOpen) return;
    const onPointer = (e: MouseEvent) => {
      if (datesRef.current && !datesRef.current.contains(e.target as Node)) {
        setDatesOpen(false);
      }
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setDatesOpen(false);
    };
    document.addEventListener("mousedown", onPointer);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onPointer);
      document.removeEventListener("keydown", onKey);
    };
  }, [datesOpen]);

  const datesSummary =
    checkIn && checkOut
      ? `${formatShort(checkIn)} – ${formatShort(checkOut)}`
      : checkIn
        ? `From ${formatShort(checkIn)}`
        : "When are you going?";

  const applyDates = () => {
    if (draftIn && draftOut && draftOut < draftIn) {
      setDateError("Check-out must be on or after check-in.");
      return;
    }
    setDateError(null);
    onDatesChange(draftIn, draftOut);
    setDatesOpen(false);
  };

  const clearDates = () => {
    setDraftIn("");
    setDraftOut("");
    setDateError(null);
    onDatesChange("", "");
    setDatesOpen(false);
  };

  return (
    <div className="bg-white rounded-2xl border border-sand-200 shadow-sm p-2 flex flex-col sm:flex-row sm:items-stretch gap-0">
      <label className="flex-1 flex flex-col gap-1 px-4 py-3 border-b sm:border-b-0 sm:border-r border-sand-200 min-w-0">
        <span className="text-[11px] font-semibold uppercase tracking-wide text-teal-700">
          Location
        </span>
        <span className="flex items-center gap-2">
          <MapPin className="w-4 h-4 text-sage-500 shrink-0" />
          <select
            value={location || "all"}
            onChange={(e) => onLocationChange(e.target.value === "all" ? "" : e.target.value)}
            className="w-full outline-none text-sm text-sage-800 bg-transparent"
          >
            <option value="all">All matching locations</option>
            {locationGroups.map((g) => (
              <optgroup key={g.region} label={g.regionLabel}>
                {g.options.map((o) => (
                  <option key={o.locality} value={o.locality}>
                    {o.locality} ({o.count})
                  </option>
                ))}
              </optgroup>
            ))}
          </select>
        </span>
      </label>

      <div className="relative flex-1 min-w-0" ref={datesRef}>
        <button
          type="button"
          aria-expanded={datesOpen}
          aria-controls={datesLabelId}
          onClick={() => setDatesOpen((v) => !v)}
          className="w-full flex flex-col gap-1 px-4 py-3 text-left hover:bg-sand-50/80 rounded-xl sm:rounded-none transition"
        >
          <span className="text-[11px] font-semibold uppercase tracking-wide text-teal-700">
            Dates
          </span>
          <span className="flex items-center gap-2">
            <Calendar className="w-4 h-4 text-sage-500 shrink-0" />
            <span
              className={`text-sm truncate ${
                checkIn || checkOut ? "text-sage-800 font-medium" : "text-gray-400"
              }`}
            >
              {datesSummary}
            </span>
          </span>
        </button>

        {datesOpen && (
          <div
            id={datesLabelId}
            className="absolute z-20 left-0 right-0 sm:left-auto sm:right-0 sm:min-w-[280px] mt-1 rounded-xl border border-sand-200 bg-white p-4 shadow-lg"
          >
            <div className="space-y-3">
              <label className="block">
                <span className="text-xs font-medium text-sage-600">Check-in</span>
                <input
                  type="date"
                  value={draftIn}
                  onChange={(e) => {
                    setDraftIn(e.target.value);
                    setDateError(null);
                    if (draftOut && e.target.value && draftOut < e.target.value) {
                      setDraftOut("");
                    }
                  }}
                  className="mt-1 w-full border border-sand-200 rounded-lg px-3 py-2 text-sm text-sage-800 outline-none focus:ring-2 focus:ring-teal-500/40"
                />
              </label>
              <label className="block">
                <span className="text-xs font-medium text-sage-600">Check-out</span>
                <input
                  type="date"
                  value={draftOut}
                  min={draftIn || undefined}
                  onChange={(e) => {
                    setDraftOut(e.target.value);
                    setDateError(null);
                  }}
                  className="mt-1 w-full border border-sand-200 rounded-lg px-3 py-2 text-sm text-sage-800 outline-none focus:ring-2 focus:ring-teal-500/40"
                />
              </label>
              {dateError && (
                <p role="alert" className="text-xs text-red-600">
                  {dateError}
                </p>
              )}
              <div className="flex items-center justify-between gap-2 pt-1">
                <button
                  type="button"
                  onClick={clearDates}
                  className="text-sm text-sage-600 hover:text-sage-800 inline-flex items-center gap-1"
                >
                  <X className="w-3.5 h-3.5" />
                  Clear dates
                </button>
                <button
                  type="button"
                  onClick={applyDates}
                  className="px-3.5 py-1.5 rounded-lg bg-teal-600 text-white text-sm font-semibold hover:bg-teal-500"
                >
                  Apply
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function formatShort(iso: string): string {
  const d = new Date(`${iso}T00:00:00`);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString("en-IN", { day: "numeric", month: "short" });
}
