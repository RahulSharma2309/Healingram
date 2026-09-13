import { getAccessToken } from "./api/client";
import { addWishlistSlug, fetchWishlist, removeWishlistSlug } from "./api/account";

const EVENT = "healingram-wishlist";

let memory: string[] = [];

function emit(): void {
  window.dispatchEvent(new Event(EVENT));
}

export function listWishlistSlugs(): string[] {
  return [...memory];
}

export function isWishlisted(slug: string): boolean {
  return memory.includes(slug);
}

export function subscribeWishlist(onChange: () => void): () => void {
  window.addEventListener(EVENT, onChange);
  return () => window.removeEventListener(EVENT, onChange);
}

export async function hydrateWishlistFromServer(): Promise<void> {
  if (!getAccessToken()) {
    memory = [];
    emit();
    return;
  }
  const items = await fetchWishlist();
  memory = items.map((i) => i.slug);
  emit();
}

export async function toggleWishlist(slug: string): Promise<boolean> {
  if (!getAccessToken()) {
    throw new Error("Sign in to save a wishlist.");
  }
  const next = isWishlisted(slug)
    ? memory.filter((item) => item !== slug)
    : [...memory, slug];
  const previous = memory;
  memory = next;
  emit();
  const saved = next.includes(slug);
  try {
    if (saved) await addWishlistSlug(slug);
    else await removeWishlistSlug(slug);
  } catch {
    memory = previous;
    emit();
    throw new Error("Could not update wishlist.");
  }
  return saved;
}
