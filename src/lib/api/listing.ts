import { programmeThemeLabel, type LaunchRetreat } from "../catalogTypes";
import { formatLaunchPrice } from "../money";
import type {
  RetreatListingView,
  RetreatMediaItem,
  RetreatProgrammeOption,
  RetreatTrustSignal,
} from "../listingTypes";
import type { ListingInclusion, ListingPriceStatus, ListingProgramme, RetreatListing } from "./catalog";

export function hasSectionItems<T>(value: T[] | null | undefined): boolean {
  return Array.isArray(value) && value.length > 0;
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

function listingTags(dto: RetreatListing): string[] {
  return [...new Set((dto.programmeThemes ?? []).map(programmeThemeLabel))].slice(0, 5);
}

function listingMedia(dto: RetreatListing): RetreatMediaItem[] {
  if (dto.media?.length) {
    return dto.media.map((item, index) => ({
      id: `${dto.slug}-${item.category}-${index}`,
      src: item.url,
      alt: item.alt || `${dto.name} — ${item.category}`,
      category: item.category as RetreatMediaItem["category"],
    }));
  }
  const src = dto.imageUrl?.trim();
  if (!src) return [];
  return [
    {
      id: `${dto.slug}-property`,
      src,
      alt: `${dto.name} — property`,
      category: "property",
    },
  ];
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

export function retreatFromListing(dto: RetreatListing): LaunchRetreat {
  const priceStatus = mapListingPriceStatus(dto.priceStatus);
  return {
    id: dto.slug,
    name: dto.name,
    region: dto.stateSlug,
    stateLabel: dto.stateLabel,
    locality: dto.locality,
    programmes: dto.programmeThemes ?? [],
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
  const trustSignals: RetreatTrustSignal[] = ["healingram_listing"];
  if (dto.programmes?.length) trustSignals.push("programme_reviewed");

  return {
    retreat,
    regionLabel,
    locationLine: locality && regionLabel ? `${locality}, ${regionLabel}` : locality || regionLabel,
    positioning: dto.positioning?.trim() || "",
    tags: listingTags(dto),
    media: listingMedia(dto),
    trustSignals,
    programmeOptions: mapListingProgrammes(dto.programmes),
    durationSummary: durationSummary(dto),
    priceLabel: formatLaunchPrice(amount),
    pricePlaceholder: "Price available after programme selection",
  };
}

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
