/**
 * Programme listing catalog — editorial + pricing for Component 3.
 * Merges programmePricing with verified editorial fields only.
 * Never invent inclusions, therapies, or prices.
 */

import {
  formatInr,
  getProgrammePricing,
  getProgrammesForRetreat,
  type PriceStatus,
  type ProgrammePricing,
} from "./programmePricing";
import { getFromAmount } from "../lib/pricing";
import { LAUNCH_PROGRAMME_LABELS, type LaunchProgrammeTheme } from "./launchSupply";

export type ProgrammeListingRecord = {
  retreatId: string;
  programmeId: string;
  programmeName: string;
  slug: string;
  shortDescription: string;
  longDescription: string;
  supportedDurations: number[];
  bestFor: string[];
  programmeType: string;
  minimumStay: number;
  priceStatus: PriceStatus;
  fromPrice: number | null;
  singleOccupancyPrice: number | null;
  doubleOccupancyPrice: number | null;
  perPersonPrice: number | null;
  inclusions: string[];
  exclusions: string[];
  consultationIncluded: boolean | null;
  treatmentsIncluded: string[];
  activitiesIncluded: string[];
  mealPlan: string | null;
  roomOptions: string[];
  scheduleSummary: string | null;
  eligibilityNotes: string | null;
  importantNotes: string[];
  cancellationPolicyId: string | null;
  verified: boolean;
  pricing: ProgrammePricing;
};

type Editorial = {
  shortDescription: string;
  longDescription: string;
  bestFor: string[];
  programmeType: string;
  inclusions?: string[];
  exclusions?: string[];
  consultationIncluded?: boolean | null;
  treatmentsIncluded?: string[];
  activitiesIncluded?: string[];
  mealPlan?: string | null;
  roomOptions?: string[];
  scheduleSummary?: string | null;
  eligibilityNotes?: string | null;
  importantNotes?: string[];
  verified: boolean;
};

/** Theme-level editorial — used only when a retreat actually offers that programme */
const THEME_EDITORIAL: Partial<Record<string, Editorial>> = {
  rejuvenation: {
    shortDescription:
      "For people looking for rest, restorative routines and a structured wellness break.",
    longDescription:
      "A restorative programme paced around rest, consultations and included wellness activities. Exact schedule is confirmed with the retreat after availability is checked.",
    bestFor: ["Rejuvenation", "Stress & Burnout"],
    programmeType: "rejuvenation",
    verified: true,
    importantNotes: [
      "Programme details may be refined after consultation with the retreat",
      "Minimum stay applies as listed for this programme",
    ],
  },
  panchakarma: {
    shortDescription:
      "A longer, structured Ayurveda-led programme for guests prepared for a deeper restorative stay.",
    longDescription:
      "Oriented around multi-week Ayurveda programme pacing. Treatment eligibility and exact inclusions are confirmed with the retreat — not assumed from this listing.",
    bestFor: ["Panchakarma", "Ayurveda", "Longer stays"],
    programmeType: "panchakarma",
    verified: true,
    importantNotes: [
      "Treatment eligibility depends on practitioner assessment at the retreat",
      "Programme length must match one of the supported durations",
      "Some services may carry additional charges",
    ],
  },
  detox: {
    shortDescription:
      "A structured cleansing-oriented stay for guests seeking a defined programme rather than a casual spa visit.",
    longDescription:
      "Focused on a defined multi-day wellness programme. Specific therapies and meal approaches are confirmed with the partner.",
    bestFor: ["Detox", "Reset"],
    programmeType: "detox",
    verified: true,
    importantNotes: [
      "Programme may be modified after consultation",
      "Minimum stay applies as listed",
    ],
  },
  ayurveda: {
    shortDescription:
      "An Ayurveda-led wellness programme with structured days and restorative time.",
    longDescription:
      "Built around Ayurveda programme pacing. Consultations, therapies and daily rhythm are confirmed with the retreat when you check availability.",
    bestFor: ["Ayurveda", "Structured wellness"],
    programmeType: "ayurveda",
    verified: true,
  },
  yoga: {
    shortDescription:
      "A yoga-focused stay for guests who want practice-led days in a calm retreat setting.",
    longDescription:
      "Centred on yoga practice within a retreat stay. Exact class schedule and inclusions are confirmed with the retreat.",
    bestFor: ["Yoga", "Mindful routines"],
    programmeType: "yoga",
    verified: true,
  },
  meditation: {
    shortDescription:
      "A quieter meditation-oriented stay for guests seeking calm and restorative pacing.",
    longDescription:
      "Oriented around meditation and quieter routines. Programme specifics are confirmed with the retreat.",
    bestFor: ["Meditation", "Calm"],
    programmeType: "meditation",
    verified: true,
  },
  long_stay: {
    shortDescription:
      "A longer restorative programme for guests who can commit to an extended wellness stay.",
    longDescription:
      "Designed for multi-week restorative time. Exact structure and inclusions are confirmed with the retreat.",
    bestFor: ["Longer stays", "Deeper restoration"],
    programmeType: "long_stay",
    verified: true,
    importantNotes: [
      "Longer minimum stay applies",
      "Programme pacing is confirmed with the retreat",
    ],
  },
  weekend: {
    shortDescription:
      "A shorter wellness break for guests looking for a restorative weekend-length stay.",
    longDescription:
      "Suited to shorter restorative escapes. Inclusions and schedule are confirmed with the retreat.",
    bestFor: ["Weekend wellness", "Short breaks"],
    programmeType: "weekend",
    verified: true,
  },
  stress_burnout: {
    shortDescription:
      "A restorative stay oriented toward rest and recovery from everyday pace.",
    longDescription:
      "Focused on rest and structured wellness time. Not a medical treatment programme — details are confirmed with the retreat.",
    bestFor: ["Stress & Burnout", "Rest"],
    programmeType: "stress_burnout",
    verified: true,
  },
  weight_metabolic: {
    shortDescription:
      "A structured wellness programme exploring metabolic and lifestyle-oriented themes.",
    longDescription:
      "Programme framing is wellness-oriented. Specific assessments and inclusions are confirmed with the retreat.",
    bestFor: ["Metabolic wellness", "Lifestyle"],
    programmeType: "weight_metabolic",
    verified: true,
  },
  lifestyle_holistic: {
    shortDescription:
      "A holistic lifestyle wellness stay for guests seeking restorative routines.",
    longDescription:
      "Oriented around holistic lifestyle pacing. Exact components are confirmed with the retreat.",
    bestFor: ["Lifestyle", "Holistic wellness"],
    programmeType: "lifestyle_holistic",
    verified: true,
  },
  nature_wellness: {
    shortDescription:
      "A nature-oriented restorative stay for guests seeking quieter surroundings.",
    longDescription:
      "Framed around nature and restorative time. Programme components are confirmed with the retreat.",
    bestFor: ["Nature", "Quiet escape"],
    programmeType: "nature_wellness",
    verified: true,
  },
};

/**
 * Verified structural inclusions only for programmes with confirmed pricing seeds.
 * Do not copy these to unverified programmes.
 */
const SEED_INCLUSIONS: Record<string, string[]> = {
  "ayurvedagram:rejuvenation": [
    "Accommodation",
    "Programme meals",
    "Consultations",
    "Included therapies",
    "Yoga / meditation",
  ],
  "ayurvedagram:panchakarma": [
    "Accommodation",
    "Programme meals",
    "Consultations",
    "Included therapies",
  ],
  "shathayu:rejuvenation": [
    "Accommodation",
    "Programme meals",
    "Consultations",
    "Yoga",
  ],
};

const SEED_EXCLUSIONS: Record<string, string[]> = {
  "ayurvedagram:rejuvenation": ["Flights and transfers", "Personal expenses", "Optional add-on therapies"],
  "ayurvedagram:panchakarma": ["Flights and transfers", "Personal expenses", "Optional add-on therapies"],
  "shathayu:rejuvenation": ["Flights and transfers", "Personal expenses"],
};

function slugify(retreatId: string, programmeId: string): string {
  return `${retreatId}-${programmeId}`.replace(/_/g, "-");
}

function bestForFromId(programmeId: string, editorial?: Editorial): string[] {
  if (editorial?.bestFor?.length) return editorial.bestFor;
  const label = LAUNCH_PROGRAMME_LABELS[programmeId as LaunchProgrammeTheme];
  return label ? [label] : [];
}

export function getListingProgrammes(retreatId: string): ProgrammeListingRecord[] {
  return getProgrammesForRetreat(retreatId).map((pricing) => {
    const editorial = THEME_EDITORIAL[pricing.programmeId];
    const key = `${retreatId}:${pricing.programmeId}`;
    const seedInclusions = SEED_INCLUSIONS[key] ?? [];
    const seedExclusions = SEED_EXCLUSIONS[key] ?? [];
    const verified = Boolean(editorial?.verified);

    return {
      retreatId,
      programmeId: pricing.programmeId,
      programmeName: pricing.programmeName,
      slug: slugify(retreatId, pricing.programmeId),
      shortDescription:
        editorial?.shortDescription ??
        "Programme details are being verified with the retreat.",
      longDescription:
        editorial?.longDescription ??
        "Fuller programme information will appear here once confirmed with the partner.",
      supportedDurations: pricing.supportedDurations,
      bestFor: bestForFromId(pricing.programmeId, editorial),
      programmeType: editorial?.programmeType ?? pricing.programmeId,
      minimumStay: pricing.minimumStay,
      priceStatus: pricing.priceStatus,
      fromPrice: getFromAmount(pricing),
      singleOccupancyPrice: pricing.singleOccupancyPrice,
      doubleOccupancyPrice: pricing.doubleOccupancyPrice,
      perPersonPrice: pricing.perPersonPrice,
      inclusions: seedInclusions.length
        ? seedInclusions
        : pricing.inclusions.filter(Boolean),
      exclusions: seedExclusions.length
        ? seedExclusions
        : pricing.exclusions.filter(Boolean),
      consultationIncluded: editorial?.consultationIncluded ?? null,
      treatmentsIncluded: editorial?.treatmentsIncluded ?? [],
      activitiesIncluded: editorial?.activitiesIncluded ?? [],
      mealPlan: editorial?.mealPlan ?? null,
      roomOptions: editorial?.roomOptions?.length
        ? editorial.roomOptions
        : pricing.roomType
          ? [pricing.roomType]
          : [],
      scheduleSummary: editorial?.scheduleSummary ?? null,
      eligibilityNotes: editorial?.eligibilityNotes ?? null,
      importantNotes: editorial?.importantNotes ?? [],
      cancellationPolicyId: null,
      verified,
      pricing,
    };
  });
}

export function getListingProgramme(
  retreatId: string,
  programmeId: string,
): ProgrammeListingRecord | undefined {
  return getListingProgrammes(retreatId).find((p) => p.programmeId === programmeId);
}

export function formatProgrammeFromPrice(record: ProgrammeListingRecord): string {
  if (record.priceStatus !== "VERIFIED" || record.fromPrice == null) {
    return "Price on request";
  }
  return `From ${formatInr(record.fromPrice)}`;
}

export function formatDurationLabel(nights: number[]): string {
  if (nights.length === 0) return "Duration on request";
  if (nights.length === 1) return `${nights[0]} nights`;
  return nights.map((n) => `${n}`).join(" / ") + " nights";
}

export function occupancyPriceLines(
  record: ProgrammeListingRecord,
  durationNights: number | null,
): { label: string; amount: number }[] {
  if (record.priceStatus !== "VERIFIED") return [];
  const nights = durationNights ?? record.supportedDurations[0] ?? null;
  if (nights == null) return [];

  const lines: { label: string; amount: number }[] = [];
  if (record.perPersonPrice != null) {
    lines.push({
      label: `${nights} nights — Per person — ${formatInr(record.perPersonPrice)}`,
      amount: record.perPersonPrice,
    });
    return lines;
  }
  if (record.singleOccupancyPrice != null) {
    lines.push({
      label: `${nights} nights — Single occupancy — ${formatInr(record.singleOccupancyPrice)}`,
      amount: record.singleOccupancyPrice,
    });
  }
  if (record.doubleOccupancyPrice != null) {
    lines.push({
      label: `${nights} nights — Double occupancy — ${formatInr(record.doubleOccupancyPrice)}`,
      amount: record.doubleOccupancyPrice,
    });
  }
  return lines;
}

/** Recommend a programme id from FMM preference tokens — non-medical */
export function recommendProgrammeId(
  programmes: ProgrammeListingRecord[],
  preferenceTokens: string[],
): string | null {
  if (!programmes.length || !preferenceTokens.length) return null;
  const tokens = preferenceTokens.map((t) => t.toLowerCase());
  let best: { id: string; score: number } | null = null;
  for (const p of programmes) {
    let score = 0;
    const hay = [p.programmeId, p.programmeName, ...p.bestFor, p.programmeType]
      .join(" ")
      .toLowerCase();
    for (const t of tokens) {
      if (t.length < 3) continue;
      if (hay.includes(t)) score += 2;
      if (p.bestFor.some((b) => b.toLowerCase().includes(t))) score += 2;
    }
    if (!best || score > best.score) best = { id: p.programmeId, score };
  }
  return best && best.score > 0 ? best.id : null;
}

export function getProgrammePricingRow(retreatId: string, programmeId: string) {
  return getProgrammePricing(retreatId, programmeId);
}
