import type { LaunchRetreat } from "./catalogTypes";
import { titleFromSlug } from "./catalogTypes";

export type DurationBandId = "weekend" | "4-5" | "6-8" | "10-14" | "21";

export type DurationBand = {
  id: DurationBandId;
  label: string;
  minNights: number;
  maxNights: number;
};

/** Browse duration chips are UI filter bands, not catalogue configuration. */
export const DURATION_BANDS: DurationBand[] = [
  { id: "weekend", label: "Weekend / 2–3 nights", minNights: 2, maxNights: 3 },
  { id: "4-5", label: "4–5 nights", minNights: 4, maxNights: 5 },
  { id: "6-8", label: "About a week / 6–8 nights", minNights: 6, maxNights: 8 },
  { id: "10-14", label: "10–14 nights", minNights: 10, maxNights: 14 },
  { id: "21", label: "21+ nights", minNights: 21, maxNights: 999 },
];

export type AllRetreatsSortId = "recommended" | "price_asc" | "price_desc" | "duration_asc";

export type LocationFilterOption = {
  id: string;
  label: string;
  region: string;
  locality: string | null;
  count: number;
};

export type FilterOptionWithCount<T extends string = string> = {
  id: T;
  label: string;
  count: number;
};

export type AllRetreatsBrowseState = {
  needs: string[];
  locations: string[];
  durations: DurationBandId[];
  sort: AllRetreatsSortId;
};

export type NeedThemeMap = Record<string, string[]>;

export type NeedFilterInput = {
  id: string;
  label: string;
};

function normalizeToken(value: string): string {
  return value.toLowerCase().replace(/-/g, "_");
}

export function retreatMatchesNeed(
  retreat: LaunchRetreat,
  needId: string,
  themeMap: NeedThemeMap = {},
): boolean {
  const mapped = themeMap[needId] ?? themeMap[normalizeToken(needId)];
  if (mapped?.length) {
    const wanted = new Set(mapped.map(normalizeToken));
    return retreat.programmes.some((theme) => wanted.has(normalizeToken(theme)));
  }
  const need = normalizeToken(needId);
  if (need === "yoga_meditation") {
    return retreat.programmes.some((theme) => {
      const token = normalizeToken(theme);
      return token === "yoga" || token === "meditation" || token === "yoga_meditation";
    });
  }
  return retreat.programmes.some((theme) => {
    const token = normalizeToken(theme);
    return token === need || token.includes(need) || need.includes(token);
  });
}

export function retreatMatchesLocationKeys(retreat: LaunchRetreat, locationKeys: string[]): boolean {
  if (locationKeys.length === 0) return true;
  return locationKeys.some((key) => {
    if (key.startsWith("region:")) {
      return retreat.region === key.slice("region:".length);
    }
    const localitySlug = retreat.locality
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-|-$/g, "");
    return retreat.locality === key || localitySlug === key;
  });
}

function nightsFromRetreat(retreat: LaunchRetreat): number[] {
  const text = retreat.typicalDuration ?? "";
  const nums = [...text.matchAll(/\d+/g)].map((match) => Number(match[0])).filter((n) => n > 0);
  return [...new Set(nums)].sort((a, b) => a - b);
}

export function retreatMatchesDurationBand(retreat: LaunchRetreat, band: DurationBand): boolean {
  const nights = nightsFromRetreat(retreat);
  if (nights.length === 0) return false;
  return nights.some((n) => n >= band.minNights && n <= band.maxNights);
}

export type BrowseFilterInput = {
  needs?: string[];
  locations?: string[];
  durations?: DurationBandId[];
};

export function filterBrowseRetreats(
  input: BrowseFilterInput,
  inventory: LaunchRetreat[],
  themeMap: NeedThemeMap = {},
): LaunchRetreat[] {
  const needs = input.needs ?? [];
  const locations = input.locations ?? [];
  const durations = input.durations ?? [];

  return inventory.filter((retreat) => {
    if (needs.length > 0 && !needs.some((need) => retreatMatchesNeed(retreat, need, themeMap))) {
      return false;
    }
    if (!retreatMatchesLocationKeys(retreat, locations)) return false;
    if (durations.length > 0) {
      const bands = DURATION_BANDS.filter((band) => durations.includes(band.id));
      if (!bands.some((band) => retreatMatchesDurationBand(retreat, band))) return false;
    }
    return true;
  });
}

export function getNeedOptionsWithCounts(
  state: Pick<AllRetreatsBrowseState, "needs" | "locations" | "durations">,
  inventory: LaunchRetreat[],
  needs: NeedFilterInput[],
  themeMap: NeedThemeMap = {},
): FilterOptionWithCount[] {
  return needs
    .map((opt) => ({
      id: opt.id,
      label: opt.label,
      count: filterBrowseRetreats(
        { needs: [opt.id], locations: state.locations, durations: state.durations },
        inventory,
        themeMap,
      ).length,
    }))
    .filter((opt) => opt.count > 0 || state.needs.includes(opt.id));
}

export function getLocationOptionsWithCounts(
  state: Pick<AllRetreatsBrowseState, "needs" | "locations" | "durations">,
  inventory: LaunchRetreat[],
  themeMap: NeedThemeMap = {},
): { region: string; regionLabel: string; options: LocationFilterOption[] }[] {
  const base = filterBrowseRetreats(
    { needs: state.needs, locations: [], durations: state.durations },
    inventory,
    themeMap,
  );
  const regionOrder = [...new Set(base.map((retreat) => retreat.region))];
  const groups: { region: string; regionLabel: string; options: LocationFilterOption[] }[] = [];

  for (const region of regionOrder) {
    const inRegion = base.filter((retreat) => retreat.region === region);
    if (inRegion.length === 0) continue;
    const localityCounts = new Map<string, number>();
    for (const retreat of inRegion) {
      localityCounts.set(retreat.locality, (localityCounts.get(retreat.locality) ?? 0) + 1);
    }

    const options: LocationFilterOption[] = [];
    const regionCount = filterBrowseRetreats(
      { needs: state.needs, locations: [`region:${region}`], durations: state.durations },
      inventory,
      themeMap,
    ).length;
    if (regionCount > 0 || state.locations.includes(`region:${region}`)) {
      options.push({
        id: `region:${region}`,
        label: inRegion[0]?.stateLabel ?? titleFromSlug(region),
        region,
        locality: null,
        count: regionCount,
      });
    }

    for (const [locality] of localityCounts) {
      const withLoc = filterBrowseRetreats(
        { needs: state.needs, locations: [locality], durations: state.durations },
        inventory,
        themeMap,
      ).length;
      if (withLoc <= 0 && !state.locations.includes(locality)) continue;
      options.push({
        id: locality,
        label: locality,
        region,
        locality,
        count: withLoc,
      });
    }

    if (options.length > 0) {
      groups.push({
        region,
        regionLabel: inRegion[0]?.stateLabel ?? titleFromSlug(region),
        options: options.filter((opt) => opt.count > 0 || state.locations.includes(opt.id)),
      });
    }
  }

  return groups;
}

export function getDurationOptionsWithCounts(
  state: Pick<AllRetreatsBrowseState, "needs" | "locations" | "durations">,
  inventory: LaunchRetreat[],
  themeMap: NeedThemeMap = {},
): FilterOptionWithCount<DurationBandId>[] {
  return DURATION_BANDS.map((band) => ({
    id: band.id,
    label: band.label,
    count: filterBrowseRetreats(
      { needs: state.needs, locations: state.locations, durations: [band.id] },
      inventory,
      themeMap,
    ).length,
  })).filter((opt) => opt.count > 0 || state.durations.includes(opt.id));
}

export function sortBrowseRetreats(list: LaunchRetreat[], sort: AllRetreatsSortId): LaunchRetreat[] {
  const copy = [...list];
  if (sort === "recommended") return copy;
  if (sort === "price_asc" || sort === "price_desc") {
    copy.sort((a, b) => {
      const pa = a.priceFrom ?? null;
      const pb = b.priceFrom ?? null;
      if (pa == null && pb == null) return 0;
      if (pa == null) return 1;
      if (pb == null) return -1;
      return sort === "price_asc" ? pa - pb : pb - pa;
    });
    return copy;
  }
  copy.sort((a, b) => {
    const na = nightsFromRetreat(a)[0] ?? 999;
    const nb = nightsFromRetreat(b)[0] ?? 999;
    return na - nb;
  });
  return copy;
}

export function priceSortAvailable(list: LaunchRetreat[]): boolean {
  return list.some((retreat) => retreat.priceFrom != null);
}

export function parseDurationParam(raw: string | null): DurationBandId[] {
  if (!raw) return [];
  const parts = raw.split(",").map((s) => s.trim()).filter(Boolean);
  const ids: DurationBandId[] = [];
  for (const part of parts) {
    if (DURATION_BANDS.some((band) => band.id === part)) {
      ids.push(part as DurationBandId);
      continue;
    }
    const n = Number(part);
    if (!Number.isNaN(n) && n > 0) {
      const band = DURATION_BANDS.find((item) => n >= item.minNights && n <= item.maxNights);
      if (band && !ids.includes(band.id)) ids.push(band.id);
    }
  }
  return ids;
}

export function parseCommaList(raw: string | null): string[] {
  if (!raw) return [];
  return raw.split(",").map((s) => s.trim()).filter(Boolean);
}

export function parseBrowseStateFromParams(params: URLSearchParams): AllRetreatsBrowseState {
  const needs = parseCommaList(params.get("need"));
  const programme = params.get("programme");
  if (programme) needs.push(programme.replace(/_/g, "-"));
  const locations: string[] = [];
  for (const state of parseCommaList(params.get("state"))) {
    locations.push(state === "bengaluru" ? "region:karnataka" : `region:${state}`);
  }
  for (const loc of parseCommaList(params.get("location"))) {
    if (loc.toLowerCase() === "bengaluru") locations.push("region:karnataka");
    else locations.push(loc);
  }
  const region = params.get("region");
  if (region) {
    const key = `region:${region}`;
    if (!locations.includes(key)) locations.push(key);
  }
  const sortRaw = params.get("sort") as AllRetreatsSortId | null;
  const sort: AllRetreatsSortId =
    sortRaw === "price_asc" || sortRaw === "price_desc" || sortRaw === "duration_asc" || sortRaw === "recommended"
      ? sortRaw
      : "recommended";
  return {
    needs: [...new Set(needs)],
    locations: [...new Set(locations)],
    durations: [...new Set(parseDurationParam(params.get("duration")))],
    sort,
  };
}

export function browseStateToSearchParams(state: AllRetreatsBrowseState): URLSearchParams {
  const next = new URLSearchParams();
  if (state.needs.length) next.set("need", state.needs.join(","));
  const regions = state.locations.filter((item) => item.startsWith("region:")).map((item) => item.slice("region:".length));
  const localities = state.locations.filter((item) => !item.startsWith("region:"));
  if (regions.length) next.set("state", regions.join(","));
  if (localities.length) next.set("location", localities.join(","));
  if (state.durations.length) next.set("duration", state.durations.join(","));
  if (state.sort !== "recommended") next.set("sort", state.sort);
  return next;
}

export function activeFilterChips(
  state: AllRetreatsBrowseState,
  needLabels: Record<string, string> = {},
): { key: string; label: string; remove: Partial<AllRetreatsBrowseState> }[] {
  const chips: { key: string; label: string; remove: Partial<AllRetreatsBrowseState> }[] = [];
  for (const need of state.needs) {
    chips.push({
      key: `need:${need}`,
      label: needLabels[need] ?? titleFromSlug(need),
      remove: { needs: state.needs.filter((item) => item !== need) },
    });
  }
  for (const loc of state.locations) {
    let label = loc;
    if (loc.startsWith("region:")) label = titleFromSlug(loc.slice("region:".length));
    chips.push({
      key: `loc:${loc}`,
      label,
      remove: { locations: state.locations.filter((item) => item !== loc) },
    });
  }
  for (const duration of state.durations) {
    const band = DURATION_BANDS.find((item) => item.id === duration);
    chips.push({
      key: `dur:${duration}`,
      label: band?.label ?? duration,
      remove: { durations: state.durations.filter((item) => item !== duration) },
    });
  }
  return chips;
}

export function countActiveFilters(state: AllRetreatsBrowseState): number {
  return state.needs.length + state.locations.length + state.durations.length;
}

export function programmesForNeeds(needIds: string[], themeMap: NeedThemeMap = {}): string[] {
  return [...new Set(needIds.flatMap((id) => themeMap[id] ?? []))];
}
