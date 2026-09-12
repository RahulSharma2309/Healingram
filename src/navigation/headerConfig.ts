/**
 * Component 1 — Global Header architecture.
 *
 * Menu content for items 2–4 is data-driven from launch supply only.
 */

import {
  LAUNCH_DESTINATIONS,
  LAUNCH_PROGRAMME_LABELS,
  type LaunchProgrammeTheme,
} from "../data/launchSupply";

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
  /** Optional section heading inside a dropdown (e.g. Karnataka / Kerala) */
  group?: string;
};

export type HeaderItemConfig = {
  id: HeaderItemId;
  key: string;
  label: string;
  status: HeaderItemStatus;
  hasDropdown?: boolean;
  /** Direct link when the item is not a dropdown (e.g. Explore Retreats → /retreats) */
  to?: string;
  getMenuItems?: () => NavLinkItem[];
};

/**
 * @deprecated Explore Retreats is a direct link to /retreats — kept for reference only.
 */
export function buildExploreMenuFromLaunchSupply(): NavLinkItem[] {
  return [{ id: "all-retreats", label: "All Retreats", to: "/retreats" }];
}

/**
 * Retreat Types — only programme themes supported by launch-target supply.
 * Excludes Therapy & Counselling, spa-only, beauty, generic fitness, etc.
 */
export function buildRetreatTypesMenuFromLaunchSupply(): NavLinkItem[] {
  const types: { id: string; theme: LaunchProgrammeTheme; label: string }[] = [
    { id: "type-ayurveda", theme: "ayurveda", label: "Ayurveda" },
    { id: "type-panchakarma", theme: "panchakarma", label: "Panchakarma" },
    { id: "type-yoga", theme: "yoga", label: "Yoga" },
    { id: "type-meditation", theme: "meditation", label: "Meditation" },
    {
      id: "type-stress",
      theme: "stress_burnout",
      label: "Stress Management / Burnout Recovery",
    },
    { id: "type-rejuvenation", theme: "rejuvenation", label: "Rejuvenation" },
    { id: "type-detox", theme: "detox", label: "Detox / Cleansing" },
    {
      id: "type-weight",
      theme: "weight_metabolic",
      label: "Weight / Metabolic Wellness",
    },
    {
      id: "type-holistic",
      theme: "lifestyle_holistic",
      label: "Holistic / Lifestyle Wellness",
    },
    { id: "type-weekend", theme: "weekend", label: "Weekend Wellness" },
  ];

  return types.map((t) => {
    const need =
      t.theme === "weekend"
        ? "weekend-wellness"
        : t.theme.replace(/_/g, "-");
    return {
      id: t.id,
      label: t.label,
      to: `/retreats?need=${need}`,
    };
  });
}

/** Destinations — Karnataka + Kerala localities from launch supply only */
export function buildDestinationsMenuFromLaunchSupply(): NavLinkItem[] {
  const items: NavLinkItem[] = [];

  for (const locality of LAUNCH_DESTINATIONS.karnataka.localities) {
    items.push({
      id: `dest-ka-${locality}`,
      label: locality,
      to: `/retreats?state=karnataka&location=${encodeURIComponent(locality)}`,
      group: LAUNCH_DESTINATIONS.karnataka.regionLabel,
    });
  }

  for (const locality of LAUNCH_DESTINATIONS.kerala.localities) {
    items.push({
      id: `dest-kl-${locality}`,
      label: locality,
      to: `/retreats?state=kerala&location=${encodeURIComponent(locality)}`,
      group: LAUNCH_DESTINATIONS.kerala.regionLabel,
    });
  }

  return items;
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

export const MVP_EXCLUDED_REGIONS = [
  "Goa",
  "Rishikesh",
  "Himachal Pradesh",
  "Uttarakhand",
  "Maharashtra",
] as const;

export const MVP_EXCLUDED_CATEGORIES = [
  "Therapy & Counselling",
  "Online therapy",
  "Spa-only",
  "Fitness classes",
  "Beauty services",
] as const;

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
    getMenuItems: buildRetreatTypesMenuFromLaunchSupply,
  },
  {
    id: 4,
    key: "destinations",
    label: "Destinations",
    status: "implemented",
    hasDropdown: true,
    getMenuItems: buildDestinationsMenuFromLaunchSupply,
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

export function programmeThemeLabel(theme: LaunchProgrammeTheme): string {
  return LAUNCH_PROGRAMME_LABELS[theme];
}
