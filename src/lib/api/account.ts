import { apiFetch } from "./client";

export type WishlistItem = { slug: string };

export type TripCard = {
  publicId: string;
  status: string;
  retreatSlug?: string;
  programmeSlug?: string;
  requestedAt?: string;
  finalAmountInr?: number | null;
};

export type TripGroups = {
  paymentPending: TripCard[];
  upcoming: TripCard[];
  completed: TripCard[];
  cancelled: TripCard[];
};

export async function fetchWishlist(): Promise<WishlistItem[]> {
  const data = await apiFetch<{ items: WishlistItem[] }>("/api/wishlist");
  return data.items ?? [];
}

export async function addWishlistSlug(slug: string): Promise<void> {
  await apiFetch("/api/wishlist", {
    method: "POST",
    body: JSON.stringify({ slug }),
  });
}

export async function removeWishlistSlug(slug: string): Promise<void> {
  await apiFetch(`/api/wishlist/${encodeURIComponent(slug)}`, { method: "DELETE" });
}

export async function fetchTrips(): Promise<TripGroups> {
  return apiFetch<TripGroups>("/api/trips");
}
