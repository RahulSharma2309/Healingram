/**
 * Maps catalog listing DTO → RetreatListingView.
 * Geography comes from the DTO (stateLabel + locality), never LAUNCH_DESTINATIONS.
 */

import {
  getLaunchRetreatById,
  getTrustSignals,
  type RetreatListingView,
  type RetreatMediaItem,
  type RetreatProgrammeOption,
} from "../../data/launchListing";
import {
  formatLaunchPrice,
  LAUNCH_PROGRAMME_LABELS,
  type LaunchProgrammeTheme,
  type LaunchRetreat,
} from "../../data/launchSupply";
import type {
  ListingInclusion,
  ListingPriceStatus,
  ListingProgramme,
  RetreatListing,
} from "./catalog";

export function isLocalLaunchSlug(slug: string | undefined): boolean {
  return getLaunchRetreatById(slug) != null;
}

/** True when the API omitted the array (seed is thin). Null/undefined only — not []. */
export function isOmittedSection<T>(value: T[] | null | undefined): boolean {
  return value == null;
}

export function hasSectionItems<T>(value: T[] | null | undefined): boolean {
  return Array.isArray(value) && value.length > 0;
}

/**
 * Local experts/rooms/testimonials only when the slug is in launch data
 * and the API omitted that array. API-only slugs never use local extras.
 */
export function shouldUseLocalOptionalSection(
  slug: string | undefined,
  apiArray: unknown[] | null | undefined,
): boolean {
  return isLocalLaunchSlug(slug) && isOmittedSection(apiArray);
}

export function mapListingPriceStatus(raw: string | null | undefined): ListingPriceStatus {
  const normalized = (raw ?? "").trim().toUpperCase();
  if (normalized === "VERIFIED" || normalized === "ESTIMATED" || normalized === "ON_REQUEST") {
    return normalized;
  }
  return "ON_REQUEST";
}

function durationLabelFromNights(nights: number[]): string {
  if (nights.length === 0) return "Duration on request";
  if (nights.length === 1) return `${nights[0]} nights`;
  return `${nights[0]}–${nights[nights.length - 1]} nights`;
}

function sortedNights(nights: number[] | null | undefined): number[] {
  return [...(nights ?? [])].filter((n) => Number.isFinite(n)).sort((a, b) => a - b);
}

function knownThemes(slugs: string[] | undefined): LaunchProgrammeTheme[] {
  return (slugs ?? []).filter((slug): slug is LaunchProgrammeTheme => slug in LAUNCH_PROGRAMME_LABELS);
}

function listingTags(dto: RetreatListing): string[] {
  const labels = (dto.programmeThemes ?? []).map((slug) => {
    if (slug in LAUNCH_PROGRAMME_LABELS) {
      return LAUNCH_PROGRAMME_LABELS[slug as LaunchProgrammeTheme];
    }
    return slug
      .split(/[_-]+/)
      .filter(Boolean)
      .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
      .join(" ");
  }).filter(Boolean);
  return [...new Set(labels)].slice(0, 5);
}

const SUPPORTING_MEDIA: { src: string; category: RetreatMediaItem["category"]; label: string }[] = [
  {
    src: "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?w=800&q=80",
    category: "accommodation",
    label: "accommodation",
  },
  {
    src: "https://images.unsplash.com/photo-1544161515-4ab6ce6db874?w=800&q=80",
    category: "treatment",
    label: "treatment setting",
  },
  {
    src: "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=800&q=80",
    category: "yoga",
    label: "yoga / practice",
  },
  {
    src: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=800&q=80",
    category: "food",
    label: "food / dining",
  },
];

function listingMedia(dto: RetreatListing): RetreatMediaItem[] {
  const src = dto.imageUrl?.trim();
  const items: RetreatMediaItem[] = [];
  if (src) {
    items.push({
      id: `${dto.slug}-property`,
      src,
      alt: `${dto.name} — property`,
      category: "property",
    });
  }
  for (const extra of SUPPORTING_MEDIA) {
    items.push({
      id: `${dto.slug}-${extra.category}`,
      src: extra.src,
      alt: `Temporary placeholder — ${extra.label}`,
      category: extra.category,
      temporary: true,
    });
  }
  return items;
}

export function mapListingProgrammes(programmes: ListingProgramme[] | undefined): RetreatProgrammeOption[] {
  return (programmes ?? []).map((programme) => {
    const nights = sortedNights(programme.supportedDurations);
    return {
      id: programme.slug,
      label: programme.name,
      durationLabel: durationLabelFromNights(nights),
      supportedDurations: nights,
      priceStatus: mapListingPriceStatus(programme.priceStatus),
      durationMode: "fixed" as const,
      durationUnit: "nights" as const,
    };
  });
}

function durationSummary(dto: RetreatListing): string {
  if (dto.typicalDuration?.trim()) return dto.typicalDuration.trim();
  const fromDto = sortedNights(dto.durations);
  if (fromDto.length > 0) return durationLabelFromNights(fromDto);
  const fromProgrammes = sortedNights((dto.programmes ?? []).flatMap((p) => p.supportedDurations ?? []));
  const unique = [...new Set(fromProgrammes)];
  if (unique.length > 0) return durationLabelFromNights(unique);
  return "Duration on request";
}

function listingPositioning(dto: RetreatListing): string {
  const fromApi = dto.positioning?.trim();
  if (fromApi) return fromApi;
  return `A wellness retreat in ${dto.locality}, ${dto.stateLabel}.`;
}

export function retreatFromListing(dto: RetreatListing): LaunchRetreat {
  const priceStatus = mapListingPriceStatus(dto.priceStatus);
  return {
    id: dto.slug,
    name: dto.name,
    region: dto.stateSlug,
    stateLabel: dto.stateLabel,
    locality: dto.locality,
    programmes: knownThemes(dto.programmeThemes),
    image: dto.imageUrl?.trim() || "",
    typicalDuration: dto.typicalDuration?.trim() || undefined,
    priceFrom: priceStatus === "VERIFIED" ? dto.priceFromInr ?? null : null,
  };
}

export function mapRetreatListingToView(dto: RetreatListing): RetreatListingView {
  const retreat = retreatFromListing(dto);
  const priceStatus = mapListingPriceStatus(dto.priceStatus);
  const amount = priceStatus === "VERIFIED" ? dto.priceFromInr ?? null : null;
  const locality = dto.locality.trim();
  const regionLabel = dto.stateLabel.trim();

  return {
    retreat,
    regionLabel,
    locationLine: locality && regionLabel ? `${locality}, ${regionLabel}` : locality || regionLabel,
    positioning: listingPositioning(dto),
    tags: listingTags(dto),
    media: listingMedia(dto),
    trustSignals: getTrustSignals(retreat),
    programmeOptions: mapListingProgrammes(dto.programmes),
    durationSummary: durationSummary(dto),
    priceLabel: formatLaunchPrice(amount),
    pricePlaceholder: "Price available after programme selection",
  };
}

/** @deprecated use mapRetreatListingToView */
export const listingToView = mapRetreatListingToView;

export function includedLabels(inclusions: ListingInclusion[] | null | undefined): string[] {
  if (!inclusions?.length) return [];
  const excluded = new Set(excludedLabels(inclusions));
  return inclusions.map((item) => item.label).filter((label) => Boolean(label) && !excluded.has(label));
}

export function excludedLabels(inclusions: ListingInclusion[] | null | undefined): string[] {
  if (!inclusions?.length) return [];
  return inclusions
    .filter((item) => {
      const kind = (item.kind ?? "").toLowerCase();
      return kind === "excluded" || kind === "exclude" || kind === "not_included";
    })
    .map((item) => item.label)
    .filter(Boolean);
}
