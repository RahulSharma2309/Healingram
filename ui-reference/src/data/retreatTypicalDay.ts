/**
 * Listing Component 4 — typical day rhythm.
 * Only broad, verified editorial periods — never invent clock times or therapies.
 */

export type TypicalDayIconKey =
  | "sunrise"
  | "yoga"
  | "meal"
  | "consultation"
  | "treatment"
  | "rest"
  | "meditation"
  | "evening";

export type TypicalDayItem = {
  time: string;
  title: string;
  description?: string;
  iconKey: TypicalDayIconKey;
  programmeId?: string;
  verified: boolean;
};

export type TypicalDaySchedule = {
  retreatId: string;
  /** When set, this schedule applies only to that programme */
  programmeId: string | null;
  label: string;
  items: TypicalDayItem[];
  /** Optional supporting image key from listing media categories */
  imageCategory?: "property" | "yoga" | "treatment" | "surroundings" | "food";
};

/**
 * Verified indicative rhythms only.
 * Times are broad periods — partner clock schedules are not claimed.
 */
const SCHEDULES: TypicalDaySchedule[] = [
  // ——— Ayurvedagram ———
  {
    retreatId: "ayurvedagram",
    programmeId: null,
    label: "A typical day at this retreat",
    imageCategory: "property",
    items: [
      {
        time: "Morning",
        title: "Quiet start",
        description: "Gentle settling into the day, with space for rest or light movement.",
        iconKey: "sunrise",
        verified: true,
      },
      {
        time: "Midday",
        title: "Programme time & meals",
        description: "Structured programme blocks and meals as arranged for your stay.",
        iconKey: "meal",
        verified: true,
      },
      {
        time: "Afternoon",
        title: "Rest or programme activity",
        description: "Downtime, personal pace, or scheduled programme sessions.",
        iconKey: "rest",
        verified: true,
      },
      {
        time: "Evening",
        title: "Wind-down",
        description: "Quieter hours to close the day without a packed itinerary.",
        iconKey: "evening",
        verified: true,
      },
    ],
  },
  {
    retreatId: "ayurvedagram",
    programmeId: "rejuvenation",
    label: "Typical day for: Rejuvenation programme",
    imageCategory: "yoga",
    items: [
      {
        time: "Morning",
        title: "Restorative start",
        description: "A calmer morning oriented around rest rather than a packed activity list.",
        iconKey: "sunrise",
        programmeId: "rejuvenation",
        verified: true,
      },
      {
        time: "Midday",
        title: "Consultation / programme session",
        description: "Programme pacing may include consultation and restorative routines.",
        iconKey: "consultation",
        programmeId: "rejuvenation",
        verified: true,
      },
      {
        time: "Afternoon",
        title: "Rest & personal time",
        description: "Space to recover between programme elements.",
        iconKey: "rest",
        programmeId: "rejuvenation",
        verified: true,
      },
      {
        time: "Evening",
        title: "Quiet wind-down",
        description: "Low-stimulus close to the day.",
        iconKey: "evening",
        programmeId: "rejuvenation",
        verified: true,
      },
    ],
  },
  {
    retreatId: "ayurvedagram",
    programmeId: "panchakarma",
    label: "Typical day for: Panchakarma programme",
    imageCategory: "treatment",
    items: [
      {
        time: "Morning",
        title: "Structured programme start",
        description: "Days are paced around a longer Ayurveda-led programme rhythm.",
        iconKey: "sunrise",
        programmeId: "panchakarma",
        verified: true,
      },
      {
        time: "Midday",
        title: "Programme sessions & meals",
        description: "Scheduled programme blocks; exact treatments depend on assessment.",
        iconKey: "treatment",
        programmeId: "panchakarma",
        verified: true,
      },
      {
        time: "Afternoon",
        title: "Rest between sessions",
        description: "Recovery time is part of the programme pacing.",
        iconKey: "rest",
        programmeId: "panchakarma",
        verified: true,
      },
      {
        time: "Evening",
        title: "Early wind-down",
        description: "Quieter evenings to support a restorative multi-week stay.",
        iconKey: "evening",
        programmeId: "panchakarma",
        verified: true,
      },
    ],
  },
  {
    retreatId: "ayurvedagram",
    programmeId: "yoga",
    label: "Typical day for: Yoga-focused stay",
    imageCategory: "yoga",
    items: [
      {
        time: "Morning",
        title: "Practice-led start",
        description: "Yoga-oriented mornings in a calm retreat setting.",
        iconKey: "yoga",
        programmeId: "yoga",
        verified: true,
      },
      {
        time: "Midday",
        title: "Meals & quieter hours",
        description: "Time to eat and rest between practice blocks.",
        iconKey: "meal",
        programmeId: "yoga",
        verified: true,
      },
      {
        time: "Afternoon",
        title: "Personal time",
        description: "Space away from a packed activity schedule.",
        iconKey: "rest",
        programmeId: "yoga",
        verified: true,
      },
      {
        time: "Evening",
        title: "Optional practice / wind-down",
        description: "A gentler close — exact sessions confirmed with the retreat.",
        iconKey: "meditation",
        programmeId: "yoga",
        verified: true,
      },
    ],
  },

  // ——— Shathayu ———
  {
    retreatId: "shathayu",
    programmeId: null,
    label: "A typical day at this retreat",
    imageCategory: "yoga",
    items: [
      {
        time: "Morning",
        title: "Yoga-oriented start",
        description: "Days often begin with practice-led or quiet settling time.",
        iconKey: "yoga",
        verified: true,
      },
      {
        time: "Midday",
        title: "Programme time & meals",
        description: "Ayurveda programme blocks and meals as arranged for your stay.",
        iconKey: "meal",
        verified: true,
      },
      {
        time: "Afternoon",
        title: "Rest or programme activity",
        description: "Downtime balanced with any scheduled sessions.",
        iconKey: "rest",
        verified: true,
      },
      {
        time: "Evening",
        title: "Wind-down",
        description: "Quieter hours away from city pace.",
        iconKey: "evening",
        verified: true,
      },
    ],
  },
  {
    retreatId: "shathayu",
    programmeId: "rejuvenation",
    label: "Typical day for: Rejuvenation programme",
    imageCategory: "surroundings",
    items: [
      {
        time: "Morning",
        title: "Gentle restorative start",
        description: "Oriented toward rest and recovery rather than a full activity calendar.",
        iconKey: "sunrise",
        programmeId: "rejuvenation",
        verified: true,
      },
      {
        time: "Midday",
        title: "Programme session & meals",
        description: "Programme pacing with meals; details confirmed after availability.",
        iconKey: "consultation",
        programmeId: "rejuvenation",
        verified: true,
      },
      {
        time: "Afternoon",
        title: "Personal rest time",
        description: "Space to move at your own pace.",
        iconKey: "rest",
        programmeId: "rejuvenation",
        verified: true,
      },
      {
        time: "Evening",
        title: "Quiet close",
        description: "Low-stimulus wind-down.",
        iconKey: "evening",
        programmeId: "rejuvenation",
        verified: true,
      },
    ],
  },
  {
    retreatId: "shathayu",
    programmeId: "yoga",
    label: "Typical day for: Yoga-focused stay",
    imageCategory: "yoga",
    items: [
      {
        time: "Morning",
        title: "Practice-led morning",
        description: "Yoga as a central part of the day’s rhythm.",
        iconKey: "yoga",
        programmeId: "yoga",
        verified: true,
      },
      {
        time: "Midday",
        title: "Meals & recovery",
        description: "Time to eat and rest between practice.",
        iconKey: "meal",
        programmeId: "yoga",
        verified: true,
      },
      {
        time: "Afternoon",
        title: "Open / personal time",
        description: "Unstructured hours for rest or quiet activity.",
        iconKey: "rest",
        programmeId: "yoga",
        verified: true,
      },
      {
        time: "Evening",
        title: "Wind-down practice or quiet time",
        description: "A calmer end to the day — exact sessions confirmed with the retreat.",
        iconKey: "meditation",
        programmeId: "yoga",
        verified: true,
      },
    ],
  },

  // ——— Soukya ———
  {
    retreatId: "soukya",
    programmeId: null,
    label: "A typical day at this retreat",
    imageCategory: "property",
    items: [
      {
        time: "Morning",
        title: "Settled start",
        description: "A quieter morning suited to longer restorative stays.",
        iconKey: "sunrise",
        verified: true,
      },
      {
        time: "Midday",
        title: "Programme blocks & meals",
        description: "Structured wellness time balanced with meals.",
        iconKey: "meal",
        verified: true,
      },
      {
        time: "Afternoon",
        title: "Rest between sessions",
        description: "Personal time is part of a longer-stay rhythm.",
        iconKey: "rest",
        verified: true,
      },
      {
        time: "Evening",
        title: "Wind-down",
        description: "Quieter evenings without a resort-style programme.",
        iconKey: "evening",
        verified: true,
      },
    ],
  },

  // ——— Shreyas ———
  {
    retreatId: "shreyas",
    programmeId: null,
    label: "A typical day at this retreat",
    imageCategory: "yoga",
    items: [
      {
        time: "Morning",
        title: "Yoga / meditation start",
        description: "Practice-led mornings suited to mindful routines.",
        iconKey: "yoga",
        verified: true,
      },
      {
        time: "Midday",
        title: "Meals & quieter hours",
        description: "Space to eat and stay unhurried.",
        iconKey: "meal",
        verified: true,
      },
      {
        time: "Afternoon",
        title: "Personal / lifestyle pace",
        description: "Time for rest or gentle activity.",
        iconKey: "rest",
        verified: true,
      },
      {
        time: "Evening",
        title: "Meditation or wind-down",
        description: "A calmer close to the day.",
        iconKey: "meditation",
        verified: true,
      },
    ],
  },

  // ——— Mekosha ———
  {
    retreatId: "mekosha",
    programmeId: null,
    label: "A typical day at this retreat",
    imageCategory: "surroundings",
    items: [
      {
        time: "Morning",
        title: "Coastal quiet start",
        description: "A slower morning before programme time.",
        iconKey: "sunrise",
        verified: true,
      },
      {
        time: "Midday",
        title: "Programme sessions & meals",
        description: "Ayurveda-led blocks as arranged for your programme.",
        iconKey: "treatment",
        verified: true,
      },
      {
        time: "Afternoon",
        title: "Rest by the coast",
        description: "Downtime between sessions.",
        iconKey: "rest",
        verified: true,
      },
      {
        time: "Evening",
        title: "Wind-down",
        description: "Quieter evening hours.",
        iconKey: "evening",
        verified: true,
      },
    ],
  },
];

function verifiedItems(items: TypicalDayItem[]): TypicalDayItem[] {
  return items.filter((i) => i.verified);
}

/** Resolve schedule: programme-specific first, else retreat-level. */
export function getTypicalDaySchedule(
  retreatId: string,
  programmeId?: string | null,
): TypicalDaySchedule | null {
  const forRetreat = SCHEDULES.filter((s) => s.retreatId === retreatId);
  if (forRetreat.length === 0) return null;

  if (programmeId) {
    const programmeSchedule = forRetreat.find((s) => s.programmeId === programmeId);
    if (programmeSchedule) {
      const items = verifiedItems(programmeSchedule.items);
      if (items.length === 0) return null;
      return { ...programmeSchedule, items };
    }
  }

  const retreatSchedule = forRetreat.find((s) => s.programmeId === null);
  if (!retreatSchedule) return null;
  const items = verifiedItems(retreatSchedule.items);
  if (items.length === 0) return null;
  return { ...retreatSchedule, items };
}

export function hasTypicalDayData(retreatId: string): boolean {
  return SCHEDULES.some(
    (s) => s.retreatId === retreatId && verifiedItems(s.items).length > 0,
  );
}
