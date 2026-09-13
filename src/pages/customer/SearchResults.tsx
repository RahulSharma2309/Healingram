import { useEffect, useMemo } from "react";
import { Link, useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { X } from "lucide-react";
import { LaunchRetreatCard } from "../../components/LaunchRetreatCard";
import { ResultsFilterBar } from "../../components/ResultsFilterBar";
import { usePublishedRetreats } from "../../lib/api/usePublishedRetreats";
import { filterBrowseRetreats } from "../../lib/browse";
import { titleFromSlug } from "../../lib/catalogTypes";

export function SearchResults() {
  const [params, setParams] = useSearchParams();
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const { retreats: inventory, needs, needThemeMap, source } = usePublishedRetreats();

  const needId = params.get("need");
  const locationParam = params.get("location") || "";
  const checkIn = params.get("checkIn") || "";
  const checkOut = params.get("checkOut") || "";

  const needLabel =
    needs.find((need) => need.slug === needId)?.label ??
    (needId ? titleFromSlug(needId) : undefined);

  const needMatches = useMemo(
    () =>
      filterBrowseRetreats(
        { needs: needId ? [needId] : [] },
        inventory,
        needThemeMap,
      ),
    [needId, inventory, needThemeMap],
  );

  const locationGroups = useMemo(() => {
    const regions = new Map<string, { region: string; regionLabel: string; localities: string[] }>();
    for (const retreat of needMatches) {
      const group = regions.get(retreat.region) ?? {
        region: retreat.region,
        regionLabel: retreat.stateLabel ?? retreat.region,
        localities: [],
      };
      if (!group.localities.includes(retreat.locality)) group.localities.push(retreat.locality);
      regions.set(retreat.region, group);
    }
    return [...regions.values()].map((group) => ({
      region: group.region,
      regionLabel: group.regionLabel,
      options: group.localities.map((locality) => ({
        locality,
        count: needMatches.filter((retreat) => retreat.locality === locality).length,
      })),
    }));
  }, [needMatches]);

  const locationValid =
    !locationParam ||
    needMatches.some(
      (retreat) =>
        retreat.locality === locationParam ||
        retreat.region === locationParam ||
        retreat.stateLabel === locationParam,
    );
  const location = locationValid ? locationParam : "";

  useEffect(() => {
    if (locationParam && !locationValid) {
      const next = new URLSearchParams(params);
      next.delete("location");
      setParams(next, { replace: true });
    }
  }, [locationParam, locationValid, params, setParams]);

  const results = useMemo(
    () =>
      filterBrowseRetreats(
        {
          needs: needId ? [needId] : [],
          locations: location ? [location] : [],
        },
        inventory,
        needThemeMap,
      ),
    [needId, location, inventory, needThemeMap],
  );

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

  const noInventoryForNeed = needMatches.length === 0 && Boolean(needId);

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
          {results.length} {results.length === 1 ? "retreat" : "retreats"}
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
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-5">
          {results.map((r) => (
            <LaunchRetreatCard key={r.id} retreat={r} />
          ))}
        </div>
      )}
    </div>
  );
}
