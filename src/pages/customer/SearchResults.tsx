import { useEffect, useMemo } from "react";
import { Link, useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { X } from "lucide-react";
import { LaunchRetreatCard } from "../../components/LaunchRetreatCard";
import { ResultsFilterBar } from "../../components/ResultsFilterBar";
import { usePublishedRetreats } from "../../lib/api/usePublishedRetreats";

export function SearchResults() {
  const [params, setParams] = useSearchParams();
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const needId = params.get("need");
  const locationParam = params.get("location") || "";
  const page = Math.max(1, Number(params.get("page") || "1") || 1);
  const { retreats: inventory, needs, places, source, total, pageCount } = usePublishedRetreats({
    need: needId ?? undefined,
    locality: locationParam || undefined,
    state: locationParam || undefined,
    page,
  });
  const checkIn = params.get("checkIn") || "";
  const checkOut = params.get("checkOut") || "";

  const needLabel = needs.find((need) => need.slug === needId)?.label;

  const locationGroups = useMemo(
    () =>
      places.map((group) => ({
        region: group.slug,
        regionLabel: group.label,
        options: group.cities.map((city) => ({
          locality: city.label,
          count: city.count,
        })),
      })),
    [places],
  );

  const locationValid =
    !locationParam ||
    places.some(
      (group) =>
        group.slug === locationParam ||
        group.label === locationParam ||
        group.cities.some((city) => city.slug === locationParam || city.label === locationParam),
    );
  const location = locationValid ? locationParam : "";

  useEffect(() => {
    if (locationParam && !locationValid) {
      const next = new URLSearchParams(params);
      next.delete("location");
      setParams(next, { replace: true });
    }
  }, [locationParam, locationValid, params, setParams]);

  const results = inventory;

  const updateParams = (patch: Record<string, string | null>) => {
    const next = new URLSearchParams(params);
    for (const [key, value] of Object.entries(patch)) {
      if (!value) next.delete(key);
      else next.set(key, value);
    }
    setParams(next);
  };

  const clearNeedChip = () => {
    const next = new URLSearchParams();
    if (location) next.set("location", location);
    if (checkIn) next.set("checkIn", checkIn);
    if (checkOut) next.set("checkOut", checkOut);
    const base = pathname.startsWith("/retreats") ? "/retreats" : "/search";
    navigate(next.toString() ? `${base}?${next}` : base);
  };

  if (source === "loading") {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center">
        <p className="text-sage-600">Loading published retreats from the catalog…</p>
      </div>
    );
  }

  if (source === "error") {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center">
        <h1 className="font-display text-2xl font-bold text-sage-800 mb-3">Catalog unavailable</h1>
        <p className="text-sage-600">
          Start Healingram.Gateway on port 5000 and Healingram.Api on port 5080, then refresh.
        </p>
      </div>
    );
  }

  const noInventoryForNeed = total === 0 && Boolean(needId);

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <h1 className="font-display text-2xl md:text-3xl font-bold text-sage-800 mb-1">
        {needLabel ? `${needLabel} retreats` : "Explore retreats"}
      </h1>
      <p className="text-sm text-gray-600 mb-5">Results from published catalog inventory.</p>

      {!noInventoryForNeed && (
        <ResultsFilterBar
          location={location}
          locationGroups={locationGroups}
          checkIn={checkIn}
          checkOut={checkOut}
          onLocationChange={(loc) => updateParams({ location: loc || null })}
          onDatesChange={(inDate, outDate) =>
            updateParams({
              checkIn: inDate || null,
              checkOut: outDate || null,
            })
          }
        />
      )}

      {needLabel && (
        <div className={`${noInventoryForNeed ? "mt-0" : "mt-4"} flex flex-wrap items-center gap-2`}>
          <button
            type="button"
            onClick={clearNeedChip}
            className="inline-flex items-center gap-1.5 rounded-full bg-sand-100 text-sage-800 pl-3 pr-2 py-1 text-sm font-medium hover:bg-sand-200 transition"
            aria-label={`Remove ${needLabel} filter`}
          >
            {needLabel}
            <X className="w-3.5 h-3.5 text-sage-500" />
          </button>
        </div>
      )}

      {!noInventoryForNeed && (
        <p className="text-sm text-gray-600 mt-4 mb-5">
          {total} {total === 1 ? "retreat" : "retreats"}
          {needLabel ? ` for ${needLabel}` : ""}
          {location ? ` in ${location}` : ""}
          {checkIn || checkOut ? ` · dates saved for availability request` : ""}
        </p>
      )}

      {noInventoryForNeed ? (
        <div className="rounded-2xl border border-sand-200 bg-white px-6 py-12 text-center max-w-lg mx-auto mt-6">
          <h2 className="font-display text-xl font-bold text-sage-800 mb-2">
            We’re still curating retreats for this need.
          </h2>
          <div className="flex flex-col sm:flex-row items-center justify-center gap-3 mt-6">
            <Link
              to="/retreats"
              className="w-full sm:w-auto px-5 py-2.5 rounded-xl bg-teal-600 text-white text-sm font-semibold hover:bg-teal-500 text-center"
            >
              Explore all retreats
            </Link>
            <Link
              to="/contact"
              state={{ source: "find_my_match" as const }}
              className="w-full sm:w-auto px-5 py-2.5 rounded-xl border border-sand-200 text-sm font-semibold text-sage-800 hover:bg-sand-50 text-center"
            >
              Talk to an Expert
            </Link>
          </div>
        </div>
      ) : (
        <>
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-5">
          {results.map((r) => (
            <LaunchRetreatCard key={r.id} retreat={r} />
          ))}
        </div>
        {pageCount > 1 && (
          <div className="mt-8 flex items-center justify-center gap-3">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => updateParams({ page: page > 2 ? String(page - 1) : null })}
              className="rounded-xl border border-sand-200 px-4 py-2 text-sm font-semibold disabled:opacity-40"
            >
              Previous
            </button>
            <button
              type="button"
              disabled={page >= pageCount}
              onClick={() => updateParams({ page: String(page + 1) })}
              className="rounded-xl bg-teal-600 text-white px-4 py-2 text-sm font-semibold disabled:opacity-40"
            >
              Next
            </button>
          </div>
        )}
        </>
      )}
    </div>
  );
}
