import { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { SlidersHorizontal, X } from "lucide-react";
import { LaunchRetreatCard } from "../../components/LaunchRetreatCard";
import {
  activeFilterChips,
  browseStateToSearchParams,
  countActiveFilters,
  filterBrowseRetreats,
  getDurationOptionsWithCounts,
  getLocationOptionsWithCounts,
  getNeedOptionsWithCounts,
  parseBrowseStateFromParams,
  priceSortAvailable,
  programmesForNeeds,
  sortBrowseRetreats,
  type AllRetreatsBrowseState,
  type AllRetreatsSortId,
  type DurationBandId,
} from "../../data/allRetreatsBrowse";

/**
 * `/retreats` — All Retreats discovery from launch inventory only.
 * Filters: need, location, duration (+ contextual counts). No legacy OTA filters.
 */
export function RetreatList() {
  const [params, setParams] = useSearchParams();
  const state = useMemo(() => parseBrowseStateFromParams(params), [params]);
  const [mobileOpen, setMobileOpen] = useState(false);

  const setState = (patch: Partial<AllRetreatsBrowseState>) => {
    const next: AllRetreatsBrowseState = { ...state, ...patch };
    setParams(browseStateToSearchParams(next), { replace: true });
  };

  const toggleNeed = (id: string) => {
    setState({
      needs: state.needs.includes(id)
        ? state.needs.filter((n) => n !== id)
        : [...state.needs, id],
    });
  };

  const toggleLocation = (id: string) => {
    setState({
      locations: state.locations.includes(id)
        ? state.locations.filter((l) => l !== id)
        : [...state.locations, id],
    });
  };

  const toggleDuration = (id: DurationBandId) => {
    setState({
      durations: state.durations.includes(id)
        ? state.durations.filter((d) => d !== id)
        : [...state.durations, id],
    });
  };

  const needOptions = useMemo(() => getNeedOptionsWithCounts(state), [state]);
  const locationGroups = useMemo(() => getLocationOptionsWithCounts(state), [state]);
  const durationOptions = useMemo(() => getDurationOptionsWithCounts(state), [state]);

  const programmes = useMemo(() => programmesForNeeds(state.needs), [state.needs]);

  const results = useMemo(() => {
    const filtered = filterBrowseRetreats({
      needs: state.needs,
      locations: state.locations,
      durations: state.durations,
    });
    return sortBrowseRetreats(filtered, state.sort, programmes.length ? programmes : null);
  }, [state, programmes]);

  const chips = useMemo(() => activeFilterChips(state), [state]);
  const activeCount = countActiveFilters(state);
  const canPriceSort = priceSortAvailable(results);

  const clearAll = () =>
    setState({ needs: [], locations: [], durations: [], sort: "recommended" });

  const clearLast = () => {
    if (chips.length === 0) return;
    const last = chips[chips.length - 1];
    setState(last.remove);
  };

  const sortOptions: { id: AllRetreatsSortId; label: string; disabled?: boolean }[] = [
    { id: "recommended", label: "Recommended" },
    { id: "price_asc", label: "Price: Low to High", disabled: !canPriceSort },
    { id: "price_desc", label: "Price: High to Low", disabled: !canPriceSort },
    { id: "duration_asc", label: "Shortest Duration" },
  ];

  const filterPanel = (
    <div className="space-y-7">
      <fieldset>
        <legend className="text-sm font-semibold text-sage-800 mb-3">What do you need?</legend>
        <ul className="space-y-2">
          {needOptions.map((opt) => (
            <li key={opt.id}>
              <label className="flex items-start gap-2.5 cursor-pointer text-sm text-sage-700">
                <input
                  type="checkbox"
                  checked={state.needs.includes(opt.id)}
                  onChange={() => toggleNeed(opt.id)}
                  className="mt-0.5 rounded border-sand-300 text-teal-600 focus:ring-teal-500/40"
                />
                <span className="flex-1 leading-snug">
                  {opt.label}{" "}
                  <span className="text-sage-500">({opt.count})</span>
                </span>
              </label>
            </li>
          ))}
        </ul>
      </fieldset>

      <fieldset>
        <legend className="text-sm font-semibold text-sage-800 mb-3">Location</legend>
        <div className="space-y-4">
          {locationGroups.map((group) => (
            <div key={group.region}>
              <p className="text-[11px] font-semibold uppercase tracking-wide text-sage-500 mb-2">
                {group.regionLabel}
              </p>
              <ul className="space-y-2">
                {group.options.map((opt) => (
                  <li key={opt.id}>
                    <label className="flex items-start gap-2.5 cursor-pointer text-sm text-sage-700">
                      <input
                        type="checkbox"
                        checked={state.locations.includes(opt.id)}
                        onChange={() => toggleLocation(opt.id)}
                        className="mt-0.5 rounded border-sand-300 text-teal-600 focus:ring-teal-500/40"
                      />
                      <span className="flex-1 leading-snug">
                        {opt.label}{" "}
                        <span className="text-sage-500">({opt.count})</span>
                      </span>
                    </label>
                  </li>
                ))}
              </ul>
            </div>
          ))}
          {locationGroups.length === 0 && (
            <p className="text-sm text-sage-500">No locations for this combination.</p>
          )}
        </div>
      </fieldset>

      <fieldset>
        <legend className="text-sm font-semibold text-sage-800 mb-3">Duration</legend>
        <ul className="space-y-2">
          {durationOptions.map((opt) => (
            <li key={opt.id}>
              <label className="flex items-start gap-2.5 cursor-pointer text-sm text-sage-700">
                <input
                  type="checkbox"
                  checked={state.durations.includes(opt.id)}
                  onChange={() => toggleDuration(opt.id)}
                  className="mt-0.5 rounded border-sand-300 text-teal-600 focus:ring-teal-500/40"
                />
                <span className="flex-1 leading-snug">
                  {opt.label}{" "}
                  <span className="text-sage-500">({opt.count})</span>
                </span>
              </label>
            </li>
          ))}
        </ul>
      </fieldset>
    </div>
  );

  return (
    <div className="max-w-7xl mx-auto px-4 py-10">
      <h1 className="font-display text-3xl font-bold text-sage-800 mb-2">All retreats</h1>
      <p className="text-sage-600 mb-6 max-w-2xl">
        Healingram’s launch partners in Karnataka and Kerala — programmes with stay, not hotel
        room rates.
      </p>

      <div className="flex flex-col lg:flex-row gap-8">
        <aside className="hidden lg:block lg:w-64 shrink-0">
          <div className="bg-white rounded-2xl border border-sand-200 p-5 sticky top-24">
            <p className="font-semibold flex items-center gap-2 mb-5 text-sage-800">
              <SlidersHorizontal className="w-4 h-4" /> Filters
            </p>
            {filterPanel}
          </div>
        </aside>

        <div className="flex-1 min-w-0">
          <div className="flex flex-wrap items-center gap-3 mb-4">
            <button
              type="button"
              className="lg:hidden inline-flex items-center gap-2 rounded-xl border border-sand-200 bg-white px-3.5 py-2 text-sm font-semibold text-sage-800"
              onClick={() => setMobileOpen(true)}
            >
              <SlidersHorizontal className="w-4 h-4" />
              Filters{activeCount > 0 ? ` (${activeCount})` : ""}
            </button>

            <label className="ml-auto flex items-center gap-2 text-sm text-sage-600">
              <span className="sr-only sm:not-sr-only">Sort</span>
              <select
                value={state.sort}
                onChange={(e) => setState({ sort: e.target.value as AllRetreatsSortId })}
                className="rounded-lg border border-sand-200 bg-white px-2.5 py-1.5 text-sm text-sage-800"
              >
                {sortOptions.map((o) => (
                  <option key={o.id} value={o.id} disabled={o.disabled}>
                    {o.label}
                  </option>
                ))}
              </select>
            </label>
          </div>

          {chips.length > 0 && (
            <div className="flex flex-wrap items-center gap-2 mb-5">
              {chips.map((chip) => (
                <button
                  key={chip.key}
                  type="button"
                  onClick={() => setState(chip.remove)}
                  className="inline-flex items-center gap-1.5 rounded-full bg-sand-100 text-sage-800 pl-3 pr-2 py-1 text-sm font-medium hover:bg-sand-200 transition"
                  aria-label={`Remove ${chip.label}`}
                >
                  {chip.label}
                  <X className="w-3.5 h-3.5 text-sage-500" />
                </button>
              ))}
              {chips.length > 1 && (
                <button
                  type="button"
                  onClick={clearAll}
                  className="text-sm font-semibold text-teal-600 hover:text-teal-700"
                >
                  Clear all
                </button>
              )}
            </div>
          )}

          <p className="text-sm text-sage-600 mb-5">
            {results.length} {results.length === 1 ? "retreat" : "retreats"}
          </p>

          {results.length === 0 ? (
            <div className="rounded-2xl border border-sand-200 bg-white px-6 py-12 text-center max-w-lg mx-auto">
              <h2 className="font-display text-xl font-bold text-sage-800 mb-2">
                No exact matches
              </h2>
              <p className="text-sm text-sage-600 mb-6">
                Try removing one filter or explore the closest matching retreats.
              </p>
              <div className="flex flex-col sm:flex-row items-center justify-center gap-3">
                {chips.length > 0 && (
                  <button
                    type="button"
                    onClick={clearLast}
                    className="w-full sm:w-auto px-5 py-2.5 rounded-xl border border-sand-200 text-sm font-semibold text-sage-800 hover:bg-sand-50"
                  >
                    Clear last filter
                  </button>
                )}
                <Link
                  to="/retreats"
                  className="w-full sm:w-auto px-5 py-2.5 rounded-xl bg-teal-600 text-white text-sm font-semibold hover:bg-teal-500 text-center"
                >
                  View all retreats
                </Link>
              </div>
            </div>
          ) : (
            <div className="grid sm:grid-cols-2 gap-5">
              {results.map((r) => (
                <LaunchRetreatCard key={r.id} retreat={r} />
              ))}
            </div>
          )}
        </div>
      </div>

      {mobileOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <button
            type="button"
            className="absolute inset-0 bg-black/40"
            aria-label="Close filters"
            onClick={() => setMobileOpen(false)}
          />
          <div className="absolute inset-x-0 bottom-0 max-h-[85vh] overflow-y-auto rounded-t-2xl bg-white p-5 pb-[max(1.25rem,env(safe-area-inset-bottom))] shadow-xl">
            <div className="flex items-center justify-between mb-4">
              <p className="font-semibold text-sage-800">
                Filters{activeCount > 0 ? ` (${activeCount})` : ""}
              </p>
              <button
                type="button"
                onClick={() => setMobileOpen(false)}
                className="p-2 rounded-lg hover:bg-sand-50"
                aria-label="Close"
              >
                <X className="w-5 h-5 text-sage-600" />
              </button>
            </div>
            {filterPanel}
            <button
              type="button"
              onClick={() => setMobileOpen(false)}
              className="mt-6 w-full rounded-xl bg-teal-600 text-white py-3 text-sm font-semibold hover:bg-teal-500"
            >
              Show {results.length} {results.length === 1 ? "retreat" : "retreats"}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
