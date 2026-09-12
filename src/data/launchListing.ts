/**
 * Listing presentation data for Healingram retreat pages (Component 1+).
 * Complements launchSupply — never invent ratings, prices, or policies here.
 */

import {
  LAUNCH_DESTINATIONS,
  LAUNCH_RETREATS,
  getRetreatDisplayTags,
  type LaunchRetreat,
} from "./launchSupply";
import { formatInr, getProgrammesForRetreat } from "./programmePricing";
import { getFromAmount } from "../lib/pricing";

export type RetreatMediaCategory =
  | "property"
  | "accommodation"
  | "treatment"
  | "yoga"
  | "food"
  | "surroundings"
  | "guest"
  | "video";

export type RetreatMediaItem = {
  id: string;
  src: string;
  alt: string;
  category: RetreatMediaCategory;
  /** Temporary internal asset until partner media is supplied */
  temporary?: boolean;
};

export type RetreatProgrammeOption = {
  id: string;
  label: string;
  durationLabel: string;
  supportedDurations: number[];
  priceStatus: "VERIFIED" | "ESTIMATED" | "ON_REQUEST";
  durationMode: "fixed" | "flexible";
  durationUnit: "nights";
};

export type RetreatTrustSignal = "healingram_listing" | "programme_reviewed" | "partner_verifying";

export const TRUST_SIGNAL_LABELS: Record<RetreatTrustSignal, string> = {
  healingram_listing: "Healingram listing",
  programme_reviewed: "Programme information reviewed",
  partner_verifying: "Partner information being verified",
};

/** Concise positioning — editorial, non-medical */
const POSITIONING: Record<string, string> = {
  shathayu:
    "An Ayurveda and yoga retreat near Bengaluru for structured rest, rejuvenation and stress recovery.",
  ayurvedagram:
    "A structured Ayurveda-led wellness retreat for deeper rest, rejuvenation and longer restorative stays.",
  tattvam:
    "A nature-facing wellness escape near Bengaluru for yoga, Ayurveda and short restorative breaks.",
  shreyas:
    "A yoga and meditation-led retreat suited to calm, lifestyle reset and mindful routines.",
  soukya:
    "A longer-stay Ayurveda and holistic wellness centre for rejuvenation and restorative programmes.",
  mekosha:
    "An Ayurveda spa-suites retreat on Kerala’s coast for Panchakarma, detox and rejuvenation.",
  "amal-tamara":
    "A quiet backwater retreat for Ayurveda, yoga and restorative time in nature.",
  "kalari-rasayana":
    "A deeper Ayurveda and Panchakarma destination for longer, structured restorative programmes.",
  "prakriti-shakti":
    "An Ayurveda-led centre offering Panchakarma, yoga and metabolic wellness programmes.",
  somatheeram:
    "A coastal Ayurveda village known for structured programmes, yoga and rejuvenation stays.",
  nattika:
    "A beachside Ayurveda retreat for Panchakarma, yoga, detox and restorative programmes.",
  carnoustie:
    "An Ayurveda and wellness resort for rejuvenation, yoga and flexible-length restorative stays.",
  kairali:
    "An Ayurvedic healing village for Panchakarma, yoga and longer structured programmes.",
  "niraamaya-surya":
    "A clifftop Kerala retreat for Ayurveda, yoga, meditation and short restorative escapes.",
};

/** Temporary supporting media until partner assets arrive — clearly not verified property photography */
const TEMP_MEDIA = {
  accommodation: "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?w=800&q=80",
  treatment: "https://images.unsplash.com/photo-1544161515-4ab6ce6db874?w=800&q=80",
  yoga: "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=800&q=80",
  food: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=800&q=80",
  surroundings: "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800&q=80",
} as const;

function buildDefaultMedia(retreat: LaunchRetreat): RetreatMediaItem[] {
  return [
    {
      id: `${retreat.id}-property`,
      src: retreat.image,
      alt: `${retreat.name} — property`,
      category: "property",
      temporary: true,
    },
    {
      id: `${retreat.id}-room`,
      src: TEMP_MEDIA.accommodation,
      alt: "Temporary placeholder — accommodation",
      category: "accommodation",
      temporary: true,
    },
    {
      id: `${retreat.id}-treatment`,
      src: TEMP_MEDIA.treatment,
      alt: "Temporary placeholder — treatment setting",
      category: "treatment",
      temporary: true,
    },
    {
      id: `${retreat.id}-yoga`,
      src: TEMP_MEDIA.yoga,
      alt: "Temporary placeholder — yoga / practice",
      category: "yoga",
      temporary: true,
    },
    {
      id: `${retreat.id}-food`,
      src: TEMP_MEDIA.food,
      alt: "Temporary placeholder — food / dining",
      category: "food",
      temporary: true,
    },
  ];
}

function durationLabelFromNights(nights: number[]): string {
  if (nights.length === 0) return "Duration on request";
  if (nights.length === 1) return `${nights[0]} nights`;
  return `${nights[0]}–${nights[nights.length - 1]} nights`;
}

export function getProgrammeOptions(retreat: LaunchRetreat): RetreatProgrammeOption[] {
  const priced = getProgrammesForRetreat(retreat.id);
  if (priced.length > 0) {
    return priced.map((p) => ({
      id: p.programmeId,
      label: p.programmeName,
      durationLabel: durationLabelFromNights(p.supportedDurations),
      supportedDurations: p.supportedDurations,
      priceStatus: p.priceStatus,
      durationMode: p.durationMode,
      durationUnit: p.durationUnit,
    }));
  }
  return [
    {
      id: "general",
      label: "Wellness programme",
      durationLabel: retreat.typicalDuration ?? "Duration on request",
      supportedDurations: [7],
      priceStatus: "ON_REQUEST",
      durationMode: "fixed",
      durationUnit: "nights",
    },
  ];
}

export function getTrustSignals(_retreat: LaunchRetreat): RetreatTrustSignal[] {
  // Only non-fabricated states until partner rate cards / verification pipeline exist
  return ["healingram_listing", "partner_verifying"];
}

export function getLaunchRetreatById(id: string | undefined): LaunchRetreat | undefined {
  if (!id) return undefined;
  return LAUNCH_RETREATS.find((r) => r.id === id);
}

export type RetreatListingView = {
  retreat: LaunchRetreat;
  regionLabel: string;
  locationLine: string;
  positioning: string;
  tags: string[];
  media: RetreatMediaItem[];
  trustSignals: RetreatTrustSignal[];
  programmeOptions: RetreatProgrammeOption[];
  durationSummary: string;
  priceLabel: string | null;
  pricePlaceholder: string;
};

export function getRetreatListingView(id: string | undefined): RetreatListingView | null {
  const retreat = getLaunchRetreatById(id);
  if (!retreat) return null;

  const dest =
    retreat.region === "karnataka" || retreat.region === "kerala"
      ? LAUNCH_DESTINATIONS[retreat.region]
      : undefined;
  const regionLabel = dest?.regionLabel ?? retreat.stateLabel ?? retreat.region;
  const nearBengaluru = ["Whitefield", "Devanahalli", "Nelamangala", "Doddaballapur"].includes(
    retreat.locality,
  );

  return {
    retreat,
    regionLabel,
    locationLine: nearBengaluru
      ? `${retreat.locality}, Bengaluru`
      : `${retreat.locality}, ${regionLabel}`,
    positioning:
      POSITIONING[retreat.id] ??
      `A curated wellness retreat in ${retreat.locality} offering programmes aligned with Healingram’s launch focus.`,
    tags: getRetreatDisplayTags(retreat, 5),
    media: buildDefaultMedia(retreat),
    trustSignals: getTrustSignals(retreat),
    programmeOptions: getProgrammeOptions(retreat),
    durationSummary: retreat.typicalDuration ?? "Programme details being verified",
    priceLabel: (() => {
      const verifiedFrom = getProgrammesForRetreat(retreat.id)
        .filter((p) => p.priceStatus === "VERIFIED")
        .map((p) => getFromAmount(p))
        .filter((n): n is number => n != null);
      if (verifiedFrom.length === 0) return null;
      return `From ${formatInr(Math.min(...verifiedFrom))}`;
    })(),
    pricePlaceholder: "Price available after programme selection",
  };
}
