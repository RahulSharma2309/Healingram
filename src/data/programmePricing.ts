/**
 * Centralised programme pricing for Healingram listings.
 * Do not invent partner rates — only VERIFIED rows may show amounts.
 * MVP demo seeds are flagged; replace with partner rate cards when verified.
 */

import type { LaunchProgrammeTheme } from "./launchSupply";
import { LAUNCH_RETREATS, LAUNCH_PROGRAMME_LABELS } from "./launchSupply";

export type PriceStatus = "VERIFIED" | "ESTIMATED" | "ON_REQUEST";

export type OccupancyType = "single" | "double" | "per_person" | "package";

export type SettlementMode = "MARKETPLACE_SPLIT" | "PARTNER_DIRECT";

export type DurationUnit = "nights";

/** fixed = start date only; flexible = start + end (must be explicit) */
export type DurationMode = "fixed" | "flexible";

export type ProgrammePricing = {
  retreatId: string;
  programmeId: string;
  programmeName: string;
  /** Supported stay lengths in nights (ignored for flexible range selection beyond minimumStay) */
  supportedDurations: number[];
  /** Default fixed — never assume flexible */
  durationMode: DurationMode;
  durationUnit: DurationUnit;
  roomType: string;
  occupancyType: OccupancyType;
  singleOccupancyPrice: number | null;
  doubleOccupancyPrice: number | null;
  perPersonPrice: number | null;
  totalPackagePrice: number | null;
  taxesIncluded: boolean;
  /** Known tax amount in INR, or null if unknown */
  taxAmount: number | null;
  /** e.g. "GST 5% on request" — never invent a rate */
  taxRule: string | null;
  validFrom: string | null;
  validTo: string | null;
  priceStatus: PriceStatus;
  inclusions: string[];
  exclusions: string[];
  minimumStay: number;
  /** Partner settlement preference — configurable, not hard-coded in UI */
  settlementMode: SettlementMode;
  /** True only for provisional workflow-seed rates — replace with partner cards */
  mvpDemoSeed?: boolean;
};

const PROGRAMME_DURATION_DEFAULTS: Partial<Record<LaunchProgrammeTheme | "general", number[]>> = {
  weekend: [2, 3],
  rejuvenation: [7],
  panchakarma: [14, 21],
  detox: [7, 14],
  long_stay: [21, 28],
  ayurveda: [7, 14],
  yoga: [3, 7],
  meditation: [3, 7],
  stress_burnout: [5, 7],
  weight_metabolic: [7, 14],
  lifestyle_holistic: [3, 7],
  nature_wellness: [3, 7],
  general: [7],
};

const PROGRAMME_NAMES: Record<string, string> = {
  ...LAUNCH_PROGRAMME_LABELS,
  weekend: "Weekend wellness",
  rejuvenation: "Rejuvenation programme",
  panchakarma: "Panchakarma programme",
  detox: "Detox programme",
  long_stay: "Longer restorative stay",
  ayurveda: "Ayurveda programme",
  yoga: "Yoga-focused stay",
  meditation: "Meditation-focused stay",
  stress_burnout: "Stress recovery stay",
  weight_metabolic: "Metabolic wellness programme",
  lifestyle_holistic: "Lifestyle wellness stay",
  nature_wellness: "Nature wellness stay",
  general: "Wellness programme",
};

/**
 * MVP workflow seed — VERIFIED amounts for end-to-end testing only.
 * Not published partner rate cards. Replace when real rates are confirmed.
 */
const MVP_DEMO_VERIFIED: ProgrammePricing[] = [
  {
    retreatId: "ayurvedagram",
    programmeId: "rejuvenation",
    programmeName: "Rejuvenation programme",
    supportedDurations: [7],
    durationMode: "fixed",
    durationUnit: "nights",
    roomType: "Standard double occupancy room",
    occupancyType: "double",
    singleOccupancyPrice: 95000,
    doubleOccupancyPrice: 140000,
    perPersonPrice: null,
    totalPackagePrice: null,
    taxesIncluded: false,
    taxAmount: null,
    taxRule: "Taxes not yet confirmed",
    validFrom: "2026-01-01",
    validTo: "2026-12-31",
    priceStatus: "VERIFIED",
    inclusions: [],
    exclusions: [],
    minimumStay: 7,
    settlementMode: "MARKETPLACE_SPLIT",
    mvpDemoSeed: true,
  },
  {
    retreatId: "ayurvedagram",
    programmeId: "panchakarma",
    programmeName: "Panchakarma programme",
    supportedDurations: [14, 21],
    durationMode: "fixed",
    durationUnit: "nights",
    roomType: "Standard double occupancy room",
    occupancyType: "double",
    singleOccupancyPrice: 185000,
    doubleOccupancyPrice: 280000,
    perPersonPrice: null,
    totalPackagePrice: null,
    taxesIncluded: false,
    taxAmount: null,
    taxRule: "Taxes not yet confirmed",
    validFrom: "2026-01-01",
    validTo: "2026-12-31",
    priceStatus: "VERIFIED",
    inclusions: [],
    exclusions: [],
    minimumStay: 14,
    settlementMode: "MARKETPLACE_SPLIT",
    mvpDemoSeed: true,
  },
  {
    retreatId: "shathayu",
    programmeId: "rejuvenation",
    programmeName: "Rejuvenation programme",
    supportedDurations: [7],
    durationMode: "fixed",
    durationUnit: "nights",
    roomType: "Standard room",
    occupancyType: "per_person",
    singleOccupancyPrice: null,
    doubleOccupancyPrice: null,
    perPersonPrice: 72000,
    totalPackagePrice: null,
    taxesIncluded: true,
    taxAmount: null,
    taxRule: "Taxes included",
    validFrom: "2026-01-01",
    validTo: "2026-12-31",
    priceStatus: "VERIFIED",
    inclusions: [],
    exclusions: [],
    minimumStay: 7,
    settlementMode: "MARKETPLACE_SPLIT",
    mvpDemoSeed: true,
  },
  {
    retreatId: "soukya",
    programmeId: "long_stay",
    programmeName: "Longer restorative stay",
    supportedDurations: [21, 28],
    durationMode: "fixed",
    durationUnit: "nights",
    roomType: "To be confirmed with retreat",
    occupancyType: "package",
    singleOccupancyPrice: null,
    doubleOccupancyPrice: null,
    perPersonPrice: null,
    totalPackagePrice: null,
    taxesIncluded: false,
    taxAmount: null,
    taxRule: "Taxes not yet confirmed",
    validFrom: null,
    validTo: null,
    priceStatus: "ON_REQUEST",
    inclusions: [],
    exclusions: [],
    minimumStay: 21,
    settlementMode: "MARKETPLACE_SPLIT",
  },
];

/** Explicit flexible-duration programmes only — never inferred */
const FLEXIBLE_DURATION_KEYS = new Set(["tattvam:weekend"]);

function onRequestRow(
  retreatId: string,
  programmeId: string,
  durations: number[],
): ProgrammePricing {
  const flexible = FLEXIBLE_DURATION_KEYS.has(`${retreatId}:${programmeId}`);
  return {
    retreatId,
    programmeId,
    programmeName: PROGRAMME_NAMES[programmeId] ?? "Wellness programme",
    supportedDurations: durations,
    durationMode: flexible ? "flexible" : "fixed",
    durationUnit: "nights",
    roomType: "To be confirmed with retreat",
    occupancyType: "package",
    singleOccupancyPrice: null,
    doubleOccupancyPrice: null,
    perPersonPrice: null,
    totalPackagePrice: null,
    taxesIncluded: false,
    taxAmount: null,
    taxRule: "Taxes not yet confirmed",
    validFrom: null,
    validTo: null,
    priceStatus: "ON_REQUEST",
    inclusions: [],
    exclusions: [],
    minimumStay: flexible ? Math.min(...durations, 2) : durations[0] ?? 7,
    settlementMode: "MARKETPLACE_SPLIT",
  };
}

/** All programme pricing rows for launch retreats */
export function buildProgrammePricingCatalog(): ProgrammePricing[] {
  const byKey = new Map<string, ProgrammePricing>();

  for (const seed of MVP_DEMO_VERIFIED) {
    byKey.set(`${seed.retreatId}:${seed.programmeId}`, seed);
  }

  for (const retreat of LAUNCH_RETREATS) {
    const themes =
      retreat.programmes.length > 0
        ? retreat.programmes.slice(0, 4)
        : (["general"] as const);

    for (const theme of themes) {
      const key = `${retreat.id}:${theme}`;
      if (byKey.has(key)) continue;
      const durations =
        PROGRAMME_DURATION_DEFAULTS[theme] ?? PROGRAMME_DURATION_DEFAULTS.general ?? [7];
      byKey.set(key, onRequestRow(retreat.id, theme, durations));
    }
  }

  // Ensure explicitly flexible programmes exist even if outside the first-4 slice
  for (const key of FLEXIBLE_DURATION_KEYS) {
    const [retreatId, programmeId] = key.split(":");
    if (!byKey.has(key)) {
      const durations =
        PROGRAMME_DURATION_DEFAULTS[programmeId as LaunchProgrammeTheme] ?? [2, 3];
      byKey.set(key, onRequestRow(retreatId, programmeId, durations));
    } else {
      byKey.get(key)!.durationMode = "flexible";
    }
  }

  return Array.from(byKey.values());
}

let _catalog: ProgrammePricing[] | null = null;

export function getProgrammePricingCatalog(): ProgrammePricing[] {
  if (!_catalog) _catalog = buildProgrammePricingCatalog();
  return _catalog;
}

/** Allow admin/demo to mutate a VERIFIED amount without losing request snapshots */
export function updateProgrammeVerifiedPrice(
  retreatId: string,
  programmeId: string,
  patch: Partial<
    Pick<
      ProgrammePricing,
      | "singleOccupancyPrice"
      | "doubleOccupancyPrice"
      | "perPersonPrice"
      | "totalPackagePrice"
      | "priceStatus"
    >
  >,
): void {
  const catalog = getProgrammePricingCatalog();
  const row = catalog.find((p) => p.retreatId === retreatId && p.programmeId === programmeId);
  if (!row) return;
  Object.assign(row, patch);
}

export function getProgrammesForRetreat(retreatId: string): ProgrammePricing[] {
  return getProgrammePricingCatalog().filter((p) => p.retreatId === retreatId);
}

export function getProgrammePricing(
  retreatId: string,
  programmeId: string,
): ProgrammePricing | undefined {
  return getProgrammePricingCatalog().find(
    (p) => p.retreatId === retreatId && p.programmeId === programmeId,
  );
}

export function formatInr(amount: number): string {
  return `₹${amount.toLocaleString("en-IN")}`;
}

export function getSettlementModeForRetreat(retreatId: string): SettlementMode {
  const row = getProgrammesForRetreat(retreatId)[0];
  return row?.settlementMode ?? "MARKETPLACE_SPLIT";
}
