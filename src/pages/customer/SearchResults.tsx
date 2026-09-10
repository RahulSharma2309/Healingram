import { useEffect, useMemo } from "react";
import { Link, useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { X } from "lucide-react";
import { LaunchRetreatCard } from "../../components/LaunchRetreatCard";
import { ResultsFilterBar } from "../../components/ResultsFilterBar";
import {
  filterLaunchRetreats,
  getAvailableLocationGroups,
  getHeroDiscoveryByProgramme,
  getNeedLabel,
  getProgrammesForNeedId,
  isLocationValidForProgramme,
  type LaunchProgrammeTheme,
} from "../../data/launchSupply";

export function SearchResults() {
  const [params, setParams] = useSearchParams();
  const navigate = useNavigate();
  const { pathname } = useLocation();

  const needId = params.get("need");
  const programmeParam = params.get("programme") as LaunchProgrammeTheme | null;
  const locationParam = params.get("location") || "";
  const checkIn = params.get("checkIn") || "";
  const checkOut = params.get("checkOut") || "";

  const programmes = useMemo((): LaunchProgrammeTheme[] => {
    const fromNeed = getProgrammesForNeedId(needId);
    if (fromNeed.length > 0) return fromNeed;
    if (programmeParam) return [programmeParam];
    return [];
  }, [needId, programmeParam]);

  const needLabel =
    getNeedLabel(needId) ??
    (programmeParam ? getHeroDiscoveryByProgramme(programmeParam)?.label : undefined);

  const needMatches = useMemo(
    () => filterLaunchRetreats({ programmes }),
    [programmes],
  );

  const locationGroups = useMemo(
    () => getAvailableLocationGroups(null, programmes),
    [programmes],
  );

  const locationValid = isLocationValidForProgramme(null, locationParam, programmes);
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
      filterLaunchRetreats({
        programmes,
        location: location || null,
        checkIn,
        checkOut,
      }),
    [programmes, location, checkIn, checkOut],
  );

  const updateParams = (patch: Record<string, string | null>) => {
    const next = new URLSearchParams(params);
    for (const [key, value] of Object.entries(patch)) {
      if (!value) next.delete(key);
      else next.set(key, value);
    }
    setParams(next, { replace: true });
  };

  const clearNeedChip = () => {
    const next = new URLSearchParams();
    if (location) next.set("location", location);
    if (checkIn) next.set("checkIn", checkIn);
    if (checkOut) next.set("checkOut", checkOut);
    const base = pathname.startsWith("/retreats") ? "/retreats" : "/search";
    navigate(next.toString() ? `${base}?${next}` : base);
  };

  const noInventoryForNeed = needMatches.length === 0 && programmes.length > 0;

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <h1 className="font-display text-2xl md:text-3xl font-bold text-sage-800 mb-1">
        {needLabel ? `${needLabel} retreats` : "Explore retreats"}
      </h1>
      <p className="text-sm text-gray-600 mb-5">
        Curated from Healingram’s launch partners in Karnataka and Kerala.
      </p>

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
          {checkIn || checkOut
            ? ` · dates saved for availability request`
            : ""}
        </p>
      )}

      {noInventoryForNeed ? (
        <div className="rounded-2xl border border-sand-200 bg-white px-6 py-12 text-center max-w-lg mx-auto mt-6">
          <h2 className="font-display text-xl font-bold text-sage-800 mb-2">
            We’re still curating retreats for this need.
          </h2>
          <div className="flex flex-col sm:flex-row items-center justify-center gap-3 mt-6">
            <Link
              to="/search"
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
