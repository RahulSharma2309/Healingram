import { apiFetch } from "./client";

export type CatalogNeed = { slug: string; label: string };
export type CatalogCity = { slug: string; label: string; count: number };
export type CatalogState = { slug: string; label: string; cities: CatalogCity[] };
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

export async function fetchRetreatListing(slug: string): Promise<unknown> {
  return apiFetch(`/api/catalog/retreats/${encodeURIComponent(slug)}`);
}
