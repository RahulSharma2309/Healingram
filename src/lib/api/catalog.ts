import { apiFetch } from "./client";

export type CatalogNeed = {
  slug: string;
  label: string;
  description?: string | null;
  imageUrl?: string | null;
  iconKey?: string | null;
  sortOrder?: number;
  kind?: string;
};

export type DiscoveryCard = {
  slug: string;
  surface: string;
  label: string;
  description?: string | null;
  imageUrl?: string | null;
  iconKey?: string | null;
  href?: string | null;
  sortOrder: number;
};

export type CatalogTheme = { slug: string; label: string; sortOrder: number };

export type PriceQuote = {
  quoteId: string;
  retreatSlug: string;
  programmeSlug: string;
  durationNights: number;
  occupancy: string;
  guests: number;
  currency: string;
  baseAmount?: number | null;
  taxAmount?: number | null;
  totalAmount?: number | null;
  priceStatus: string;
  pricingVersion: string;
  nights: number;
};

export type ContentPage = {
  slug: string;
  title: string;
  body: string;
  kind: string;
  sortOrder: number;
};
export type CatalogCity = { slug: string; label: string; count: number };
export type CatalogState = {
  slug: string;
  label: string;
  cities: CatalogCity[];
  description?: string | null;
  imageUrl?: string | null;
  sortOrder?: number;
};
export type RetreatCard = {
  slug: string;
  name: string;
  locality: string;
  stateSlug: string;
  stateLabel: string;
  imageUrl?: string | null;
  typicalDuration?: string | null;
  priceFromInr?: number | null;
  priceStatus?: string;
  programmeThemes?: string[];
};

export async function fetchNeeds(): Promise<CatalogNeed[]> {
  const data = await apiFetch<{ items: CatalogNeed[] }>("/api/catalog/needs");
  return data.items ?? [];
}

export async function fetchPlaces(): Promise<CatalogState[]> {
  const data = await apiFetch<{ states: CatalogState[] }>("/api/catalog/places");
  return data.states ?? [];
}

export async function fetchRetreats(query: {
  need?: string;
  state?: string;
  locality?: string;
  duration?: string;
}): Promise<RetreatCard[]> {
  const params = new URLSearchParams();
  if (query.need) params.set("need", query.need);
  if (query.state) params.set("state", query.state);
  if (query.locality) params.set("locality", query.locality);
  if (query.duration) params.set("duration", query.duration);
  const qs = params.toString();
  const data = await apiFetch<{ items: RetreatCard[] }>(`/api/catalog/retreats${qs ? `?${qs}` : ""}`);
  return data.items ?? [];
}

export type ListingPriceStatus = "VERIFIED" | "ESTIMATED" | "ON_REQUEST";

export type ListingInclusion = {
  kind: string;
  label: string;
};

export type ListingProgramme = {
  slug: string;
  name: string;
  needSlug: string;
  themeSlug: string;
  supportedDurations: number[];
  priceStatus: string;
  priceFromInr?: number | null;
  inclusions?: ListingInclusion[];
  description?: string | null;
  bestFor?: string | null;
};

export type ListingRoom = {
  name: string;
  occupancyMax: number;
  description?: string | null;
};

export type ListingExpert = {
  name: string;
  role?: string | null;
  bio?: string | null;
  imageUrl?: string | null;
};

export type ListingTestimonial = {
  body: string;
  guestName?: string | null;
};

/** GET /api/catalog/retreats/{slug} — matches RetreatListingDto (camelCase). */
export type RetreatListing = {
  slug: string;
  name: string;
  locality: string;
  stateSlug: string;
  stateLabel: string;
  imageUrl?: string | null;
  typicalDuration?: string | null;
  positioning?: string | null;
  priceFromInr?: number | null;
  priceStatus: string;
  programmeThemes?: string[];
  programmes: ListingProgramme[];
  durations?: number[];
  rooms?: ListingRoom[];
  inclusions?: ListingInclusion[];
  experts?: ListingExpert[];
  testimonials?: ListingTestimonial[];
  media?: { url: string; alt?: string | null; category: string; sortOrder: number }[];
  sections?: { kind: string; payload: unknown }[];
};

export async function fetchRetreatListing(slug: string): Promise<RetreatListing> {
  return apiFetch<RetreatListing>(`/api/catalog/retreats/${encodeURIComponent(slug)}`);
}

export async function fetchDiscovery(): Promise<DiscoveryCard[]> {
  const data = await apiFetch<{ items: DiscoveryCard[] }>("/api/catalog/discovery");
  return data.items ?? [];
}

export async function fetchThemes(): Promise<CatalogTheme[]> {
  const data = await apiFetch<{ items: CatalogTheme[] }>("/api/catalog/themes");
  return data.items ?? [];
}

export async function quoteProgrammePrice(input: {
  retreatSlug: string;
  programmeSlug: string;
  durationNights: number;
  occupancy: string;
  guests: number;
}): Promise<PriceQuote> {
  return apiFetch<PriceQuote>("/api/catalog/pricing/quote", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function fetchContentPages(kind?: string): Promise<ContentPage[]> {
  const qs = kind ? `?kind=${encodeURIComponent(kind)}` : "";
  const data = await apiFetch<{ items: ContentPage[] }>(`/api/content/pages${qs}`);
  return data.items ?? [];
}

export async function fetchContentPage(slug: string): Promise<ContentPage> {
  return apiFetch<ContentPage>(`/api/content/pages/${encodeURIComponent(slug)}`);
}

export async function fetchPlatformSettings(): Promise<Record<string, string>> {
  const data = await apiFetch<{ items: Record<string, string> }>("/api/platform/settings");
  return data.items ?? {};
}
