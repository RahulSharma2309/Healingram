/**
 * Healingram launch supply — single source of truth for MVP navigation
 * and downstream UX. Do not invent retreats, destinations, or programmes
 * outside this file.
 *
 * Later-phase properties (Atmantan, Dharana at Shillim, Ananda in the Himalayas,
 * Six Senses Vana, etc.) must stay out of public launch navigation unless
 * explicitly added here.
 */

export type LaunchRegion = "karnataka" | "kerala";

/**
 * Allowed programme groupings for launch — only use when a launch-target
 * retreat actually offers them. Downstream filters should derive from
 * retreat.programmes, not from a generic wellness taxonomy.
 */
export type LaunchProgrammeTheme =
  | "ayurveda"
  | "panchakarma"
  | "yoga"
  | "meditation"
  | "stress_burnout"
  | "rejuvenation"
  | "detox"
  | "weight_metabolic"
  | "lifestyle_holistic"
  | "nature_wellness"
  | "weekend"
  | "long_stay"; // 7 / 14 / 21 / 28-day style programmes

export type LaunchRetreat = {
  id: string;
  name: string;
  region: string;
  /** Display name for the state when it is not in the old KA/KL map */
  stateLabel?: string;
  /** City / locality used for destination nav */
  locality: string;
  /** Programme themes this property is known to offer (launch-relevant only) */
  programmes: LaunchProgrammeTheme[];
  /** Editorial image for cards — replace with partner assets when available */
  image: string;
  /** Typical stay length when known; omit rather than invent */
  typicalDuration?: string;
  /**
   * Starting price in INR when verified. `null` / omitted = do not invent —
   * UI shows “Price on programme selection”.
   */
  priceFrom?: number | null;
  /** MVP demo badge only — remove when real verification pipeline exists */
  mvpDemoVerified?: boolean;
};

export const LAUNCH_PROGRAMME_LABELS: Record<LaunchProgrammeTheme, string> = {
  ayurveda: "Ayurveda",
  panchakarma: "Panchakarma",
  yoga: "Yoga",
  meditation: "Meditation",
  stress_burnout: "Stress & Burnout",
  rejuvenation: "Rejuvenation",
  detox: "Detox / cleansing",
  weight_metabolic: "Weight & Metabolic Wellness",
  lifestyle_holistic: "Lifestyle / holistic wellness",
  nature_wellness: "Nature-based wellness",
  weekend: "Weekend Wellness",
  long_stay: "Longer 7 / 14 / 21 / 28-day programmes",
};

/** Destination tree limited to current launch supply localities */
export const LAUNCH_DESTINATIONS = {
  karnataka: {
    regionLabel: "Karnataka",
    localities: ["Bengaluru", "Devanahalli", "Whitefield", "Nelamangala", "Doddaballapur"] as const,
  },
  kerala: {
    regionLabel: "Kerala",
    localities: [
      "Kovalam / Thiruvananthapuram",
      "Kollam",
      "Alappuzha",
      "Thrissur",
      "Palakkad",
    ] as const,
  },
} as const;

/**
 * Homepage destination journeys — Karnataka + Kerala only.
 * Resolves via `/retreats?state=` into All Retreats filters (launch inventory).
 * Images are temporary destination mood photography until partner assets arrive.
 */
export const HOME_DESTINATION_JOURNEYS = [
  {
    region: "karnataka" as const,
    label: "Karnataka",
    description:
      "Wellness retreats in and around Bengaluru, from short restorative stays to structured Ayurveda programmes.",
    cta: "Explore Karnataka",
    to: "/retreats?state=karnataka",
    /** Temporary — Hampi / Karnataka landscape mood, not a specific partner property */
    image:
      "https://images.unsplash.com/photo-1477587458883-47145ed94245?w=1400&q=80",
    imageTemporary: true,
  },
  {
    region: "kerala" as const,
    label: "Kerala",
    description:
      "Ayurveda and wellness retreats across one of India’s most established healing destinations.",
    cta: "Explore Kerala",
    to: "/retreats?state=kerala",
    /** Temporary — Kerala backwaters mood, not a specific partner property */
    image:
      "https://images.unsplash.com/photo-1602216056096-3b40cc0c9944?w=1400&q=80",
    imageTemporary: true,
  },
] as const;

/**
 * Launch-target retreats. Programme tags are conservative starter associations
 * for architecture only — refine per property when rate cards are verified.
 */
export const LAUNCH_RETREATS: LaunchRetreat[] = [
  // Bengaluru / Karnataka
  {
    id: "shathayu",
    name: "Shathayu Ayurveda Yoga Retreat",
    region: "karnataka",
    locality: "Devanahalli",
    programmes: ["ayurveda", "yoga", "panchakarma", "rejuvenation", "stress_burnout"],
    image: "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=800&q=80",
    typicalDuration: "3–14 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
  {
    id: "ayurvedagram",
    name: "Ayurvedagram Heritage Wellness Centre",
    region: "karnataka",
    locality: "Whitefield",
    programmes: ["ayurveda", "panchakarma", "yoga", "detox", "rejuvenation", "long_stay"],
    image: "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=800&q=80",
    typicalDuration: "7–21 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
  {
    id: "tattvam",
    name: "Tattvam in the Hills",
    region: "karnataka",
    locality: "Doddaballapur",
    programmes: ["ayurveda", "yoga", "nature_wellness", "lifestyle_holistic", "weekend"],
    image: "https://images.unsplash.com/photo-1545389336-cf090694435e?w=800&q=80",
    typicalDuration: "Weekend–7 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
  {
    id: "shreyas",
    name: "Shreyas Retreat",
    region: "karnataka",
    locality: "Nelamangala",
    programmes: ["yoga", "meditation", "ayurveda", "stress_burnout", "lifestyle_holistic"],
    image: "https://images.unsplash.com/photo-1599901860904-17e6edd6938b?w=800&q=80",
    typicalDuration: "3–7 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
  {
    id: "soukya",
    name: "Soukya",
    region: "karnataka",
    locality: "Whitefield",
    programmes: ["ayurveda", "yoga", "meditation", "detox", "rejuvenation", "long_stay"],
    image: "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800&q=80",
    typicalDuration: "7–28 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
  // Kerala
  {
    id: "mekosha",
    name: "Mekosha Ayurveda Spasuites",
    region: "kerala",
    locality: "Kollam",
    programmes: ["ayurveda", "panchakarma", "detox", "rejuvenation", "long_stay"],
    image: "https://images.unsplash.com/photo-1600334129128-685c5582fd35?w=800&q=80",
    typicalDuration: "7–21 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
  {
    id: "amal-tamara",
    name: "Amal Tamara",
    region: "kerala",
    locality: "Alappuzha",
    programmes: ["ayurveda", "yoga", "meditation", "nature_wellness", "lifestyle_holistic"],
    image: "https://images.unsplash.com/photo-1518604666860-9edfe7b5b3a0?w=800&q=80",
    typicalDuration: "3–7 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
  {
    id: "kalari-rasayana",
    name: "Kalari Rasayana",
    region: "kerala",
    locality: "Kollam",
    programmes: ["ayurveda", "panchakarma", "detox", "rejuvenation", "long_stay"],
    image: "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=800&q=80",
    typicalDuration: "14–28 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
  {
    id: "prakriti-shakti",
    name: "Prakriti Shakti",
    region: "kerala",
    locality: "Thrissur",
    programmes: ["ayurveda", "panchakarma", "yoga", "detox", "weight_metabolic"],
    image: "https://images.unsplash.com/photo-1515377905703-c4788e51af15?w=800&q=80",
    typicalDuration: "7–21 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
  {
    id: "somatheeram",
    name: "Somatheeram Ayurveda Village",
    region: "kerala",
    locality: "Kovalam / Thiruvananthapuram",
    programmes: ["ayurveda", "panchakarma", "yoga", "rejuvenation", "long_stay"],
    image: "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=800&q=80",
    typicalDuration: "7–21 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
  {
    id: "nattika",
    name: "Nattika Beach Ayurveda Retreat",
    region: "kerala",
    locality: "Thrissur",
    programmes: ["ayurveda", "panchakarma", "yoga", "detox", "rejuvenation"],
    image: "https://images.unsplash.com/photo-1470252649378-9c29740c9fa8?w=800&q=80",
    typicalDuration: "7–14 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
  {
    id: "carnoustie",
    name: "Carnoustie Ayurveda & Wellness Resort",
    region: "kerala",
    locality: "Thrissur",
    programmes: ["ayurveda", "yoga", "lifestyle_holistic", "rejuvenation", "weekend"],
    image: "https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=800&q=80",
    typicalDuration: "Weekend–14 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
  {
    id: "kairali",
    name: "Kairali Ayurvedic Healing Village",
    region: "kerala",
    locality: "Palakkad",
    programmes: ["ayurveda", "panchakarma", "yoga", "detox", "long_stay"],
    image: "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800&q=80",
    typicalDuration: "7–28 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
  {
    id: "niraamaya-surya",
    name: "Niraamaya Retreats Surya Samudra",
    region: "kerala",
    locality: "Kovalam / Thiruvananthapuram",
    programmes: ["ayurveda", "yoga", "meditation", "nature_wellness", "lifestyle_holistic", "weekend"],
    image: "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?w=800&q=80",
    typicalDuration: "Weekend–7 days",
    priceFrom: null,
    mvpDemoVerified: true,
  },
];

/**
 * Homepage hero discovery options (Component 2) + results need chip.
 * Launch-relevant intents only — edit this list to change the dropdown.
 * `programme` maps to LaunchProgrammeTheme when filtering; `null` = Find My Match path.
 */
export type HeroDiscoveryOption = {
  id: string;
  label: string;
  /** Linked launch programme theme, or null for the “not sure” / match path */
  programme: LaunchProgrammeTheme | null;
};

export const HERO_DISCOVERY_OPTIONS: HeroDiscoveryOption[] = [
  { id: "stress-burnout", label: "Stress & Burnout", programme: "stress_burnout" },
  { id: "ayurveda", label: "Ayurveda", programme: "ayurveda" },
  { id: "panchakarma", label: "Panchakarma", programme: "panchakarma" },
  { id: "yoga", label: "Yoga", programme: "yoga" },
  { id: "meditation", label: "Meditation", programme: "meditation" },
  { id: "rejuvenation", label: "Rejuvenation", programme: "rejuvenation" },
  { id: "weekend-wellness", label: "Weekend Wellness", programme: "weekend" },
  { id: "weight-metabolic", label: "Weight & Metabolic Wellness", programme: "weight_metabolic" },
  { id: "not-sure", label: "Not sure what I need", programme: null },
];

/**
 * Component 3 — Explore by what you need.
 * Cards map to the same need ids / programmes as discovery + results filtering.
 * `yoga-meditation` matches retreats tagged yoga OR meditation.
 */
export type ExploreByNeedCard = {
  id: string;
  label: string;
  description: string;
  image: string;
  /** Launch programmes that satisfy this card (any match) */
  programmes: LaunchProgrammeTheme[];
};

export const EXPLORE_BY_NEED_CARDS: ExploreByNeedCard[] = [
  {
    id: "stress-burnout",
    label: "Stress & Burnout",
    description:
      "For slowing down, restoring energy and stepping away from constant overwhelm.",
    image: "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=900&q=80",
    programmes: ["stress_burnout"],
  },
  {
    id: "ayurveda",
    label: "Ayurveda",
    description:
      "Traditional wellness programmes built around personalised routines, food and therapies.",
    image: "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=900&q=80",
    programmes: ["ayurveda"],
  },
  {
    id: "panchakarma",
    label: "Panchakarma",
    description:
      "Longer, structured Ayurvedic programmes for people seeking a deeper reset.",
    image: "https://images.unsplash.com/photo-1600334129128-685c5582fd35?w=900&q=80",
    programmes: ["panchakarma"],
  },
  {
    id: "yoga-meditation",
    label: "Yoga & Meditation",
    description:
      "Retreats centred around movement, breath, stillness and mindful routines.",
    image: "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=900&q=80",
    programmes: ["yoga", "meditation"],
  },
  {
    id: "rejuvenation",
    label: "Rejuvenation",
    description: "For rest, recovery, better routines and feeling physically refreshed.",
    image: "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=900&q=80",
    programmes: ["rejuvenation"],
  },
  {
    id: "weekend-wellness",
    label: "Weekend Wellness",
    description:
      "Short restorative stays for people who need a reset without taking a long break.",
    image: "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?w=900&q=80",
    programmes: ["weekend"],
  },
];

export function getExploreByNeedCard(needId: string | null | undefined): ExploreByNeedCard | undefined {
  if (!needId) return undefined;
  return EXPLORE_BY_NEED_CARDS.find((c) => c.id === needId);
}

/**
 * Resolve URL `need=` to launch programmes for filtering.
 * Prefers explore cards, then hero discovery options.
 */
export function getProgrammesForNeedId(
  needId: string | null | undefined,
): LaunchProgrammeTheme[] {
  if (!needId) return [];
  const explore = getExploreByNeedCard(needId);
  if (explore) return [...explore.programmes];
  const hero = getHeroDiscoveryOption(needId);
  if (hero?.programme) return [hero.programme];
  return [];
}

export function getNeedLabel(needId: string | null | undefined): string | undefined {
  if (!needId) return undefined;
  return (
    getExploreByNeedCard(needId)?.label ??
    getHeroDiscoveryOption(needId)?.label
  );
}

/** Unique programme themes actually present on at least one launch retreat */
export function getLaunchProgrammeThemes(): LaunchProgrammeTheme[] {
  const set = new Set<LaunchProgrammeTheme>();
  for (const r of LAUNCH_RETREATS) {
    for (const p of r.programmes) set.add(p);
  }
  return [...set];
}

export function getLaunchRetreatsByRegion(region: LaunchRegion): LaunchRetreat[] {
  return LAUNCH_RETREATS.filter((r) => r.region === region);
}

export function getLaunchLocalities(region: LaunchRegion): readonly string[] {
  return LAUNCH_DESTINATIONS[region].localities;
}

export function getHeroDiscoveryOption(
  needId: string | null | undefined,
): HeroDiscoveryOption | undefined {
  if (!needId) return undefined;
  return HERO_DISCOVERY_OPTIONS.find((o) => o.id === needId);
}

export function getHeroDiscoveryByProgramme(
  programme: string | null | undefined,
): HeroDiscoveryOption | undefined {
  if (!programme) return undefined;
  return HERO_DISCOVERY_OPTIONS.find((o) => o.programme === programme);
}

/**
 * Location match for filters — exact locality from launch retreat records.
 */
export function retreatMatchesLocation(
  retreat: LaunchRetreat,
  location: string | null | undefined,
): boolean {
  if (!location || location === "all") return true;
  return retreat.locality === location;
}

export function retreatMatchesProgramme(
  retreat: LaunchRetreat,
  programme: LaunchProgrammeTheme | null | undefined,
): boolean {
  if (!programme) return true;
  return retreat.programmes.includes(programme);
}

/** Match if retreat has any of the listed programmes; empty list = no programme filter */
export function retreatMatchesAnyProgramme(
  retreat: LaunchRetreat,
  programmes: LaunchProgrammeTheme[] | null | undefined,
): boolean {
  if (!programmes || programmes.length === 0) return true;
  return programmes.some((p) => retreat.programmes.includes(p));
}

export type LaunchRetreatFilter = {
  programme?: LaunchProgrammeTheme | null;
  /** When set, retreat matches if it offers any of these programmes */
  programmes?: LaunchProgrammeTheme[] | null;
  location?: string | null;
  /** Reserved for future availability — stored but not inventing inventory cuts */
  checkIn?: string | null;
  checkOut?: string | null;
  inventory?: LaunchRetreat[];
};

export function filterLaunchRetreats(filter: LaunchRetreatFilter = {}): LaunchRetreat[] {
  const programmeFilter =
    filter.programmes && filter.programmes.length > 0
      ? filter.programmes
      : filter.programme
        ? [filter.programme]
        : null;

  const inventory = filter.inventory ?? LAUNCH_RETREATS;
  return inventory.filter(
    (r) =>
      retreatMatchesAnyProgramme(r, programmeFilter) &&
      retreatMatchesLocation(r, filter.location),
  );
}

export type LocationOptionWithCount = {
  locality: string;
  count: number;
  region: string;
};

export type LocationFilterGroup = {
  region: string;
  regionLabel: string;
  options: LocationOptionWithCount[];
};

/**
 * Location dropdown options derived from retreats matching the selected need.
 * Only localities with count > 0 are included; empty regions are omitted.
 */
export function getAvailableLocationGroups(
  programme?: LaunchProgrammeTheme | null,
  programmes?: LaunchProgrammeTheme[] | null,
  inventory?: LaunchRetreat[],
): LocationFilterGroup[] {
  const matches = filterLaunchRetreats({
    programme: programmes?.length ? null : programme,
    programmes: programmes?.length ? programmes : null,
    inventory,
  });
  const counts = new Map<string, { count: number; region: string; stateLabel?: string }>();

  for (const r of matches) {
    const prev = counts.get(r.locality);
    if (prev) prev.count += 1;
    else counts.set(r.locality, { count: 1, region: r.region, stateLabel: r.stateLabel });
  }

  const regionOrder = [
    ...new Set([
      "karnataka",
      "kerala",
      ...matches.map((r) => r.region),
    ]),
  ];
  const groups: LocationFilterGroup[] = [];

  for (const region of regionOrder) {
    const dest =
      region === "karnataka" || region === "kerala" ? LAUNCH_DESTINATIONS[region] : undefined;
    const options: LocationOptionWithCount[] = [];
    const seen = new Set<string>();

    for (const locality of dest?.localities ?? []) {
      const meta = counts.get(locality);
      if (!meta || meta.count <= 0) continue;
      options.push({ locality, count: meta.count, region });
      seen.add(locality);
    }

    for (const [locality, meta] of counts) {
      if (meta.region !== region || seen.has(locality) || meta.count <= 0) continue;
      options.push({ locality, count: meta.count, region });
    }

    if (options.length > 0) {
      const sample = matches.find((r) => r.region === region);
      groups.push({
        region,
        regionLabel:
          dest?.regionLabel ??
          sample?.stateLabel ??
          region
            .split("-")
            .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
            .join(" "),
        options,
      });
    }
  }

  return groups;
}

/** True when location is empty/all or appears in the need-derived option list */
export function isLocationValidForProgramme(
  programme: LaunchProgrammeTheme | null | undefined,
  location: string | null | undefined,
  programmes?: LaunchProgrammeTheme[] | null,
  inventory?: LaunchRetreat[],
): boolean {
  if (!location || location === "all") return true;
  const groups = getAvailableLocationGroups(programme, programmes, inventory);
  return groups.some((g) => g.options.some((o) => o.locality === location));
}

/** Tags shown on cards — prefer hero-facing labels when available */
export function getRetreatDisplayTags(retreat: LaunchRetreat, limit = 3): string[] {
  const labels = retreat.programmes.map((p) => {
    const hero = HERO_DISCOVERY_OPTIONS.find((o) => o.programme === p);
    return hero?.label ?? LAUNCH_PROGRAMME_LABELS[p];
  });
  return labels.slice(0, limit);
}

export function formatLaunchPrice(priceFrom: number | null | undefined): string | null {
  if (priceFrom == null || Number.isNaN(priceFrom)) return null;
  return `From ₹${priceFrom.toLocaleString("en-IN")}`;
}
