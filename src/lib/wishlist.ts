import { getAccessToken } from "./api/client";
import { addWishlistSlug, fetchWishlist, removeWishlistSlug } from "./api/account";

const KEY = "healingram_wishlist_v1";
const EVENT = "healingram-wishlist";

function readLocal(): string[] {
  try {
    const raw = JSON.parse(localStorage.getItem(KEY) || "[]");
    return Array.isArray(raw) ? raw.filter((s): s is string => typeof s === "string") : [];
  } catch {
    return [];
  }
}

function writeLocal(slugs: string[]): void {
  try {
    localStorage.setItem(KEY, JSON.stringify([...new Set(slugs)]));
    window.dispatchEvent(new Event(EVENT));
  } catch {
    /* ignore */
  }
}

export function listWishlistSlugs(): string[] {
  return readLocal();
}

export function isWishlisted(slug: string): boolean {
  return readLocal().includes(slug);
}

export function subscribeWishlist(onChange: () => void): () => void {
  window.addEventListener(EVENT, onChange);
  window.addEventListener("storage", onChange);
  return () => {
    window.removeEventListener(EVENT, onChange);
    window.removeEventListener("storage", onChange);
  };
}

export async function hydrateWishlistFromServer(): Promise<void> {
  if (!getAccessToken()) return;
  try {
    const items = await fetchWishlist();
    writeLocal(items.map((i) => i.slug));
  } catch {
    /* keep local until login/API is up */
  }
}

/** Push guest-local hearts, then replace local with the signed-in server list. */
export async function mergeWishlistOnLogin(): Promise<void> {
  if (!getAccessToken()) return;
  const local = readLocal();
  try {
    await Promise.all(local.map((slug) => addWishlistSlug(slug).catch(() => undefined)));
    await hydrateWishlistFromServer();
  } catch {
    /* keep local if the API is down */
  }
}

export async function toggleWishlist(slug: string): Promise<boolean> {
  const next = isWishlisted(slug)
    ? readLocal().filter((s) => s !== slug)
    : [...readLocal(), slug];
  writeLocal(next);
  const saved = next.includes(slug);

  if (getAccessToken()) {
    try {
      if (saved) await addWishlistSlug(slug);
      else await removeWishlistSlug(slug);
    } catch {
      /* local heart still works offline */
    }
  }

  return saved;
}
