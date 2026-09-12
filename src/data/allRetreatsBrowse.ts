/**
 * All Retreats browse — filters, counts, and sorting from launch inventory only.
 * One source of truth: launchSupply + programmePricing. No legacy mock data.
 */

import {
  EXPLORE_BY_NEED_CARDS,
  HERO_DISCOVERY_OPTIONS,
  LAUNCH_DESTINATIONS,
  LAUNCH_RETREATS,
  filterLaunchRetreats,
  getNeedLabel,
  getProgrammesForNeedId,
  type LaunchProgrammeTheme,
  type LaunchRetreat,
} from "./launchSupply";
import { getFromAmount } from "../lib/pricing";
import { defaultNightsForThemes, getProgrammesForRetreat } from "./programmePricing";

export type DurationBandId = "weekend" | "4-5" | "6-8" | "10-14" | "21";

export type DurationBand = {
  id: DurationBandId;
  label: string;
  minNights: number;
  maxNights: number;
};

export const DURATION_BANDS: DurationBand[] = [
  { id: "weekend", label: "Weekend / 2–3 nights", minNights: 2, maxNights: 3 },
  { id: "4-5", label: "4–5 nights", minNights: 4, maxNights: 5 },
  { id: "6-8", label: "About a week / 6–8 nights", minNights: 6, maxNights: 8 },
  { id: "10-14", label: "10–14 nights", minNights: 10, maxNights: 14 },
  { id: "21", label: "21+ nights", minNights: 21, maxNights: 999 },
];

export type AllRetreatsSortId =
  | "recommended"
  | "price_asc"
  | "price_desc"
  | "duration_asc";

export type NeedFilterOption = {
  id: string;
  label: string;
  programmes: LaunchProgrammeTheme[];
};

export type LocationFilterOption = {
  /** Unique key — locality string or `region:karnataka` */
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
  /** Locality names and/or region keys `region:karnataka` */
  locations: string[];
  durations: DurationBandId[];
  sort: AllRetreatsSortId;
};

/** Core need filters from Explore-by-need cards (SOP). */
export function getCoreNeedFilterOptions(): NeedFilterOption[] {
  return EXPLORE_BY_NEED_CARDS.map((c) => ({
    id: c.id,
    label: c.label,
    programmes: [...c.programmes],
  }));
}

/** Weight & Metabolic only if at least one launch retreat offers it. */
export function getOptionalNeedFilterOptions(): NeedFilterOption[] {
  const hasWeight = LAUNCH_RETREATS.some((r) => r.programmes.includes("weight_metabolic"));
  if (!hasWeight) return [];
  return [
    {
      id: "weight-metabolic",
      label: "Weight & Metabolic Wellness",
      programmes: ["weight_metabolic"],
    },
  ];
}

export function getAllNeedFilterOptions(): NeedFilterOption[] {
  return [...getCoreNeedFilterOptions(), ...getOptionalNeedFilterOptions()];
}

export function programmesForNeeds(needIds: string[]): LaunchProgrammeTheme[] {
  const set = new Set<LaunchProgrammeTheme>();
  for (const id of needIds) {
    for (const p of getProgrammesForNeedId(id)) set.add(p);
  }
  return [...set];
}

export function getRetreatVerifiedFromPrice(retreatId: string): number | null {
  const amounts = getProgrammesForRetreat(retreatId)
    .filter((p) => p.priceStatus === "VERIFIED")
    .map((p) => getFromAmount(p))
    .filter((n): n is number => n != null && n > 0);
  if (amounts.length === 0) return null;
  return Math.min(...amounts);
}

/** Shortest supported nights across programmes (optionally constrained by need programmes). */
export function getRetreatDurationNights(
  retreat: LaunchRetreat,
  programmeConstraint?: LaunchProgrammeTheme[] | null,
): number[] {
  const rows = getProgrammesForRetreat(retreat.id);
  const nights = new Set<number>();
  for (const row of rows) {
    const theme = row.programmeId as LaunchProgrammeTheme;
    if (programmeConstraint?.length && !programmeConstraint.includes(theme)) continue;
    for (const n of row.supportedDurations) nights.add(n);
  }
  if (nights.size === 0) {
    const themes = programmeConstraint?.length
      ? retreat.programmes.filter((p) => programmeConstraint.includes(p))
      : retreat.programmes;
    for (const n of defaultNightsForThemes(themes)) nights.add(n);
  }
  return [...nights].sort((a, b) => a - b);
}

export function retreatMatchesDurationBand(
  retreat: LaunchRetreat,
  band: DurationBand,
  programmeConstraint?: LaunchProgrammeTheme[] | null,
): boolean {
  const nights = getRetreatDurationNights(retreat, programmeConstraint);
  return nights.some((n) => n >= band.minNights && n <= band.maxNights);
}

export function retreatMatchesLocationKeys(
  retreat: LaunchRetreat,
  locationKeys: string[],
): boolean {
  if (locationKeys.length === 0) return true;
  return locationKeys.some((key) => {
    if (key.startsWith("region:")) {
      return retreat.region === key.slice("region:".length);
    }
    const localitySlug = retreat.locality.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
    return retreat.locality === key || localitySlug === key;
  });
}

export type BrowseFilterInput = {
  needs?: string[];
  locations?: string[];
  durations?: DurationBandId[];
};

export function filterBrowseRetreats(
  input: BrowseFilterInput = {},
  inventory: LaunchRetreat[] = LAUNCH_RETREATS,
): LaunchRetreat[] {
  const needs = input.needs ?? [];
  const programmes = programmesForNeeds(needs);
  const locations = input.locations ?? [];
  const durations = input.durations ?? [];

  let list = filterLaunchRetreats({
    programmes: programmes.length > 0 ? programmes : null,
    inventory,
  });

  list = list.filter((r) => retreatMatchesLocationKeys(r, locations));

  if (durations.length > 0) {
    const bands = DURATION_BANDS.filter((b) => durations.includes(b.id));
    list = list.filter((r) =>
      bands.some((band) => retreatMatchesDurationBand(r, band, programmes.length ? programmes : null)),
    );
  }

  return list;
}

/** Need options with counts given location/duration filters (each need counted independently). */
export function getNeedOptionsWithCounts(
  state: Pick<AllRetreatsBrowseState, "needs" | "locations" | "durations">,
  inventory: LaunchRetreat[] = LAUNCH_RETREATS,
): FilterOptionWithCount[] {
  const options = getAllNeedFilterOptions();
  return options
    .map((opt) => {
      const count = filterBrowseRetreats(
        {
          needs: [opt.id],
          locations: state.locations,
          durations: state.durations,
        },
        inventory,
      ).length;
      return { id: opt.id, label: opt.label, count };
    })
    .filter((o) => o.count > 0 || state.needs.includes(o.id));
}

export function getLocationOptionsWithCounts(
  state: Pick<AllRetreatsBrowseState, "needs" | "locations" | "durations">,
  inventory: LaunchRetreat[] = LAUNCH_RETREATS,
): { region: string; regionLabel: string; options: LocationFilterOption[] }[] {
  const base = filterBrowseRetreats(
    {
      needs: state.needs,
      locations: [],
      durations: state.durations,
    },
    inventory,
  );

  const regionOrder = [...new Set(["karnataka", "kerala", ...base.map((r) => r.region)])];
  const groups: { region: string; regionLabel: string; options: LocationFilterOption[] }[] = [];

  for (const region of regionOrder) {
    const inRegion = base.filter((r) => r.region === region);
    if (inRegion.length === 0) continue;

    const localityCounts = new Map<string, number>();
    for (const r of inRegion) {
      localityCounts.set(r.locality, (localityCounts.get(r.locality) ?? 0) + 1);
    }

    const options: LocationFilterOption[] = [];

    const regionCount = filterBrowseRetreats(
      {
        needs: state.needs,
        locations: [`region:${region}`],
        durations: state.durations,
      },
      inventory,
    ).length;
    if (regionCount > 0 || state.locations.includes(`region:${region}`)) {
      const known =
        region === "karnataka" || region === "kerala" ? LAUNCH_DESTINATIONS[region] : undefined;
      options.push({
        id: `region:${region}`,
        label:
          region === "karnataka"
            ? "Bengaluru & nearby"
            : (known?.regionLabel ?? inRegion[0]?.stateLabel ?? region),
        region,
        locality: null,
        count: regionCount,
      });
    }

    const destLocalities =
      region === "karnataka" || region === "kerala" ? LAUNCH_DESTINATIONS[region].localities : [];
    for (const locality of destLocalities) {
      const count = localityCounts.get(locality) ?? 0;
      if (count <= 0 && !state.locations.includes(locality)) continue;
      const withLoc = filterBrowseRetreats(
        {
          needs: state.needs,
          locations: [locality],
          durations: state.durations,
        },
        inventory,
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

    for (const [locality] of localityCounts) {
      if ((destLocalities as readonly string[]).includes(locality)) continue;
      if (options.some((o) => o.id === locality)) continue;
      const withLoc = filterBrowseRetreats(
        {
          needs: state.needs,
          locations: [locality],
          durations: state.durations,
        },
        inventory,
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
      const known =
        region === "karnataka" || region === "kerala" ? LAUNCH_DESTINATIONS[region] : undefined;
      groups.push({
        region,
        regionLabel: known?.regionLabel ?? inRegion[0]?.stateLabel ?? region,
        options: options.filter((o) => o.count > 0 || state.locations.includes(o.id)),
      });
    }
  }

  return groups;
}

export function getDurationOptionsWithCounts(
  state: Pick<AllRetreatsBrowseState, "needs" | "locations" | "durations">,
  inventory: LaunchRetreat[] = LAUNCH_RETREATS,
): FilterOptionWithCount<DurationBandId>[] {
  return DURATION_BANDS.map((band) => {
    const count = filterBrowseRetreats(
      {
        needs: state.needs,
        locations: state.locations,
        durations: [band.id],
      },
      inventory,
    ).length;
    return { id: band.id, label: band.label, count };
  }).filter((o) => o.count > 0 || state.durations.includes(o.id));
}

/** Budget filter — only when verified pricing covers a meaningful share of inventory. */
export function isBudgetFilterEnabled(): boolean {
  const withPrice = LAUNCH_RETREATS.filter((r) => getRetreatVerifiedFromPrice(r.id) != null);
  return withPrice.length >= Math.ceil(LAUNCH_RETREATS.length * 0.5);
}

export function sortBrowseRetreats(
  list: LaunchRetreat[],
  sort: AllRetreatsSortId,
  programmeConstraint?: LaunchProgrammeTheme[] | null,
): LaunchRetreat[] {
  const copy = [...list];
  if (sort === "recommended") return copy;

  if (sort === "price_asc" || sort === "price_desc") {
    copy.sort((a, b) => {
      const pa = getRetreatVerifiedFromPrice(a.id);
      const pb = getRetreatVerifiedFromPrice(b.id);
      if (pa == null && pb == null) return 0;
      if (pa == null) return 1;
      if (pb == null) return -1;
      return sort === "price_asc" ? pa - pb : pb - pa;
    });
    return copy;
  }

  if (sort === "duration_asc") {
    copy.sort((a, b) => {
      const na = getRetreatDurationNights(a, programmeConstraint);
      const nb = getRetreatDurationNights(b, programmeConstraint);
      const ma = na[0] ?? 999;
      const mb = nb[0] ?? 999;
      return ma - mb;
    });
  }
  return copy;
}

export function priceSortAvailable(list: LaunchRetreat[]): boolean {
  return list.some((r) => getRetreatVerifiedFromPrice(r.id) != null);
}

export function parseDurationParam(raw: string | null): DurationBandId[] {
  if (!raw) return [];
  const parts = raw.split(",").map((s) => s.trim()).filter(Boolean);
  const ids: DurationBandId[] = [];
  for (const part of parts) {
    if (DURATION_BANDS.some((b) => b.id === part)) {
      ids.push(part as DurationBandId);
      continue;
    }
    const n = Number(part);
    if (!Number.isNaN(n) && n > 0) {
      const band = DURATION_BANDS.find((b) => n >= b.minNights && n <= b.maxNights);
      if (band && !ids.includes(band.id)) ids.push(band.id);
    }
  }
  return ids;
}

export function parseCommaList(raw: string | null): string[] {
  if (!raw) return [];
  return raw
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);
}

export function parseBrowseStateFromParams(params: URLSearchParams): AllRetreatsBrowseState {
  const needs = parseCommaList(params.get("need"));
  const programme = params.get("programme");
  if (programme) {
    const fromHero = HERO_DISCOVERY_OPTIONS.find((o) => o.programme === programme);
    needs.push(fromHero?.id ?? programme.replace(/_/g, "-"));
  }
  const locations: string[] = [];
  const stateParam = parseCommaList(params.get("state"));
  for (const s of stateParam) {
    if (s === "bengaluru") locations.push("region:karnataka");
    else locations.push(`region:${s}`);
  }
  for (const loc of parseCommaList(params.get("location"))) {
    if (loc === "Bengaluru" || loc.toLowerCase() === "bengaluru") {
      locations.push("region:karnataka");
    } else {
      locations.push(loc);
    }
  }
  const region = params.get("region");
  if (region) {
    const key = `region:${region}`;
    if (!locations.includes(key)) locations.push(key);
  }

  const durations = parseDurationParam(params.get("duration"));
  const sortRaw = params.get("sort") as AllRetreatsSortId | null;
  const sort: AllRetreatsSortId =
    sortRaw === "price_asc" ||
    sortRaw === "price_desc" ||
    sortRaw === "duration_asc" ||
    sortRaw === "recommended"
      ? sortRaw
      : "recommended";

  return {
    needs: [...new Set(needs)],
    locations: [...new Set(locations)],
    durations: [...new Set(durations)],
    sort,
  };
}

export function browseStateToSearchParams(state: AllRetreatsBrowseState): URLSearchParams {
  const next = new URLSearchParams();
  if (state.needs.length) next.set("need", state.needs.join(","));

  const regions = state.locations
    .filter((l) => l.startsWith("region:"))
    .map((l) => l.slice("region:".length));
  const localities = state.locations.filter((l) => !l.startsWith("region:"));
  if (regions.length) next.set("state", regions.join(","));
  if (localities.length) next.set("location", localities.join(","));
  if (state.durations.length) next.set("duration", state.durations.join(","));
  if (state.sort !== "recommended") next.set("sort", state.sort);
  return next;
}

export function activeFilterChips(state: AllRetreatsBrowseState): {
  key: string;
  label: string;
  remove: Partial<AllRetreatsBrowseState>;
}[] {
  const chips: { key: string; label: string; remove: Partial<AllRetreatsBrowseState> }[] = [];
  for (const need of state.needs) {
    chips.push({
      key: `need:${need}`,
      label: getNeedLabel(need) ?? need,
      remove: { needs: state.needs.filter((n) => n !== need) },
    });
  }
  for (const loc of state.locations) {
    let label = loc;
    if (loc === "region:karnataka") label = "Bengaluru & nearby";
    else if (loc.startsWith("region:")) {
      label = loc
        .slice("region:".length)
        .split("-")
        .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
        .join(" ");
    }
    chips.push({
      key: `loc:${loc}`,
      label,
      remove: { locations: state.locations.filter((l) => l !== loc) },
    });
  }
  for (const d of state.durations) {
    const band = DURATION_BANDS.find((b) => b.id === d);
    chips.push({
      key: `dur:${d}`,
      label: band?.label ?? d,
      remove: { durations: state.durations.filter((x) => x !== d) },
    });
  }
  return chips;
}

export function countActiveFilters(state: AllRetreatsBrowseState): number {
  return state.needs.length + state.locations.length + state.durations.length;
}
