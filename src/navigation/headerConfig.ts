/**
 * Header chrome. Menu content is filled from catalog APIs at render time.
 */

export type HeaderItemId =
  | 1
  | 2
  | 3
  | 4
  | 5
  | 6
  | 7
  | 8
  | 9
  | 10
  | 11;

export type HeaderItemStatus = "awaiting_spec" | "implemented";

export type NavLinkItem = {
  id: string;
  label: string;
  to: string;
  emphasis?: boolean;
  group?: string;
};

export type HeaderItemConfig = {
  id: HeaderItemId;
  key: string;
  label: string;
  status: HeaderItemStatus;
  hasDropdown?: boolean;
  to?: string;
  getMenuItems?: () => NavLinkItem[];
};

export function buildRetreatTypesMenu(
  themes: { slug: string; label: string }[],
): NavLinkItem[] {
  return themes.map((theme) => ({
    id: `type-${theme.slug}`,
    label: theme.label,
    to: `/retreats?need=${encodeURIComponent(theme.slug.replace(/_/g, "-"))}`,
  }));
}

export function buildDestinationsMenuFromPlaces(
  states: { slug: string; label: string; cities: { slug: string; label: string }[] }[],
): NavLinkItem[] {
  const items: NavLinkItem[] = [];
  for (const state of states) {
    items.push({
      id: `dest-state-${state.slug}`,
      label: `All ${state.label}`,
      to: `/retreats?state=${encodeURIComponent(state.slug)}`,
      group: state.label,
    });
    for (const city of state.cities) {
      items.push({
        id: `dest-${state.slug}-${city.slug}`,
        label: city.label,
        to: `/retreats?state=${encodeURIComponent(state.slug)}&location=${encodeURIComponent(city.label)}`,
        group: state.label,
      });
    }
  }
  return items;
}

export const HEADER_ITEMS: HeaderItemConfig[] = [
  { id: 1, key: "logo", label: "Healingram", status: "awaiting_spec" },
  {
    id: 2,
    key: "explore",
    label: "Explore Retreats",
    status: "implemented",
    hasDropdown: false,
    to: "/retreats",
  },
  {
    id: 3,
    key: "retreat-types",
    label: "Retreat Types",
    status: "implemented",
    hasDropdown: true,
  },
  {
    id: 4,
    key: "destinations",
    label: "Destinations",
    status: "implemented",
    hasDropdown: true,
  },
  { id: 5, key: "find-my-match", label: "Find My Match", status: "awaiting_spec" },
  { id: 6, key: "wishlist", label: "Wishlist", status: "awaiting_spec" },
  { id: 7, key: "login", label: "Log in", status: "awaiting_spec" },
  { id: 8, key: "talk-to-expert", label: "Talk to an Expert", status: "awaiting_spec" },
  { id: 9, key: "for-partners", label: "For retreat partners →", status: "awaiting_spec" },
  { id: 10, key: "dev-panels", label: "Vendor / Admin panels", status: "awaiting_spec" },
  { id: 11, key: "account", label: "My Trips + account menu", status: "awaiting_spec" },
];

export function getHeaderItem(id: HeaderItemId): HeaderItemConfig {
  const item = HEADER_ITEMS.find((i) => i.id === id);
  if (!item) throw new Error(`Unknown header item ${id}`);
  return item;
}
