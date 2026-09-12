/**
 * Listing Component 8 — programme inclusions / exclusions.
 *
 * Structured, verified items only. Do not invent treatments, meals, transfers,
 * medicines, tests, activities, or extra charges.
 *
 * Aligns with seed lists used by the programme catalog for the same programmes.
 */

export type InclusionCategory =
  | "stay"
  | "food"
  | "consultation"
  | "treatment"
  | "activity"
  | "transfer"
  | "facility"
  | "medical"
  | "other";

export type InclusionCondition =
  | "guaranteed"
  | "subject_to_consultation"
  | "varies_by_programme"
  | "varies_by_duration"
  | "varies_by_room";

export type ProgrammeInclusionItem = {
  id: string;
  title: string;
  description?: string;
  category: InclusionCategory;
  verified: boolean;
  condition?: InclusionCondition;
};

export type ProgrammeExclusionItem = {
  id: string;
  title: string;
  description?: string;
  category: InclusionCategory;
  verified: boolean;
  /** Extra-cost wording when verified — never invent amounts */
  additionalCostNote?: string;
};

export type ProgrammeInclusionSet = {
  retreatId: string;
  programmeId: string;
  inclusions: ProgrammeInclusionItem[];
  exclusions: ProgrammeExclusionItem[];
};

const INCLUSION_SETS: ProgrammeInclusionSet[] = [
  {
    retreatId: "ayurvedagram",
    programmeId: "rejuvenation",
    inclusions: [
      {
        id: "ayg-rej-accommodation",
        title: "Accommodation",
        category: "stay",
        verified: true,
      },
      {
        id: "ayg-rej-meals",
        title: "Programme meals",
        category: "food",
        verified: true,
      },
      {
        id: "ayg-rej-consultations",
        title: "Consultations",
        description: "Programme consultation schedule as confirmed with the retreat",
        category: "consultation",
        verified: true,
      },
      {
        id: "ayg-rej-therapies",
        title: "Included therapies",
        description: "Therapy plan confirmed after assessment — not a fixed menu of treatments",
        category: "treatment",
        verified: true,
        condition: "subject_to_consultation",
      },
      {
        id: "ayg-rej-yoga",
        title: "Yoga / meditation",
        category: "activity",
        verified: true,
        condition: "varies_by_programme",
      },
    ],
    exclusions: [
      {
        id: "ayg-rej-flights",
        title: "Flights",
        category: "transfer",
        verified: true,
      },
      {
        id: "ayg-rej-transfers",
        title: "Airport transfers",
        category: "transfer",
        verified: true,
        additionalCostNote: "Not included · Confirm transfer options with the retreat",
      },
      {
        id: "ayg-rej-personal",
        title: "Personal expenses",
        category: "other",
        verified: true,
      },
      {
        id: "ayg-rej-addons",
        title: "Optional add-on therapies",
        category: "treatment",
        verified: true,
        additionalCostNote: "Charged separately if requested",
      },
    ],
  },
  {
    retreatId: "ayurvedagram",
    programmeId: "panchakarma",
    inclusions: [
      {
        id: "ayg-pk-accommodation",
        title: "Accommodation",
        category: "stay",
        verified: true,
      },
      {
        id: "ayg-pk-meals",
        title: "Programme meals",
        category: "food",
        verified: true,
      },
      {
        id: "ayg-pk-consultations",
        title: "Consultations",
        description: "Programme consultation schedule as confirmed with the retreat",
        category: "consultation",
        verified: true,
      },
      {
        id: "ayg-pk-therapies",
        title: "Included therapies",
        description:
          "Treatment eligibility and plan depend on practitioner assessment at the retreat",
        category: "treatment",
        verified: true,
        condition: "subject_to_consultation",
      },
    ],
    exclusions: [
      {
        id: "ayg-pk-flights",
        title: "Flights",
        category: "transfer",
        verified: true,
      },
      {
        id: "ayg-pk-transfers",
        title: "Airport transfers",
        category: "transfer",
        verified: true,
        additionalCostNote: "Not included · Confirm transfer options with the retreat",
      },
      {
        id: "ayg-pk-personal",
        title: "Personal expenses",
        category: "other",
        verified: true,
      },
      {
        id: "ayg-pk-addons",
        title: "Optional add-on therapies",
        category: "treatment",
        verified: true,
        additionalCostNote: "Charged separately if requested",
      },
    ],
  },
  {
    retreatId: "shathayu",
    programmeId: "rejuvenation",
    inclusions: [
      {
        id: "sha-rej-accommodation",
        title: "Accommodation",
        category: "stay",
        verified: true,
      },
      {
        id: "sha-rej-meals",
        title: "Programme meals",
        category: "food",
        verified: true,
      },
      {
        id: "sha-rej-consultations",
        title: "Consultations",
        category: "consultation",
        verified: true,
      },
      {
        id: "sha-rej-yoga",
        title: "Yoga",
        category: "activity",
        verified: true,
        condition: "varies_by_programme",
      },
    ],
    exclusions: [
      {
        id: "sha-rej-flights",
        title: "Flights and transfers",
        category: "transfer",
        verified: true,
      },
      {
        id: "sha-rej-personal",
        title: "Personal expenses",
        category: "other",
        verified: true,
      },
    ],
  },
];

export type InclusionGroupId = "stay" | "wellness" | "other";

export const INCLUSION_GROUP_LABELS: Record<InclusionGroupId, string> = {
  stay: "Stay",
  wellness: "Wellness programme",
  other: "Other",
};

function groupForCategory(category: InclusionCategory): InclusionGroupId {
  if (category === "stay" || category === "food") return "stay";
  if (
    category === "consultation" ||
    category === "treatment" ||
    category === "activity"
  ) {
    return "wellness";
  }
  return "other";
}

export function conditionLabel(condition: InclusionCondition | undefined): string | null {
  switch (condition) {
    case "subject_to_consultation":
      return "Subject to consultation";
    case "varies_by_programme":
      return "Varies by programme";
    case "varies_by_duration":
      return "Varies by duration";
    case "varies_by_room":
      return "Varies by room category";
    default:
      return null;
  }
}

export function getProgrammeInclusionSet(
  retreatId: string,
  programmeId: string,
): ProgrammeInclusionSet | null {
  const set = INCLUSION_SETS.find(
    (s) => s.retreatId === retreatId && s.programmeId === programmeId,
  );
  if (!set) return null;
  return {
    ...set,
    inclusions: set.inclusions.filter((i) => i.verified),
    exclusions: set.exclusions.filter((e) => e.verified),
  };
}

export function hasVerifiedInclusionSet(retreatId: string, programmeId: string): boolean {
  const set = getProgrammeInclusionSet(retreatId, programmeId);
  if (!set) return false;
  return set.inclusions.length > 0 || set.exclusions.length > 0;
}

/** Group inclusions when there are enough items to warrant subtle sections */
export function groupInclusions(
  items: ProgrammeInclusionItem[],
): { group: InclusionGroupId; items: ProgrammeInclusionItem[] }[] {
  if (items.length <= 4) {
    return [{ group: "wellness", items }];
  }
  const buckets: Record<InclusionGroupId, ProgrammeInclusionItem[]> = {
    stay: [],
    wellness: [],
    other: [],
  };
  for (const item of items) {
    buckets[groupForCategory(item.category)].push(item);
  }
  return (["stay", "wellness", "other"] as InclusionGroupId[])
    .filter((g) => buckets[g].length > 0)
    .map((group) => ({ group, items: buckets[group] }));
}
