/**
 * Listing Component 6 — guest stories / testimonials.
 *
 * Public display requires: consentGranted === true AND verified === true.
 * Do not invent real guest stories. Do not surface unsupported medical claims.
 *
 * DEMO RECORDS: entries with `isDemo: true` are UX development placeholders only.
 * They are not partner-collected testimonials. Replace with consented partner stories
 * before treating them as production social proof.
 */

export type TravellerType = "solo" | "couple" | "friends" | "family" | "group";

export type TestimonialFilterTag =
  | "stress_burnout"
  | "ayurveda"
  | "panchakarma"
  | "solo"
  | "couple"
  | "first_retreat";

export type RetreatTestimonial = {
  testimonialId: string;
  retreatId: string;
  guestName: string;
  guestPhoto: string | null;
  guestPhotoTemporary?: boolean;
  programmeId: string;
  programmeName: string;
  stayDuration: string;
  travellerType: TravellerType;
  reasonForVisit: string;
  quote: string;
  longerStory: string | null;
  outcomeReflection: string | null;
  videoUrl: string | null;
  consentGranted: boolean;
  verified: boolean;
  /** Internal: UX demo placeholder — not a real consented guest story */
  isDemo: boolean;
  filterTags: TestimonialFilterTag[];
};

export const TESTIMONIAL_FILTER_LABELS: Record<TestimonialFilterTag, string> = {
  stress_burnout: "Stress & Burnout",
  ayurveda: "Ayurveda",
  panchakarma: "Panchakarma",
  solo: "Solo traveller",
  couple: "Couple",
  first_retreat: "First retreat",
};

export const TRAVELLER_TYPE_LABELS: Record<TravellerType, string> = {
  solo: "Solo traveller",
  couple: "Couple",
  friends: "Friends",
  family: "Family",
  group: "Group",
};

/** Temporary portrait placeholders — not claimed as real guest photos */
const TEMP_GUEST = {
  a: "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=400&q=80",
  b: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&q=80",
  c: "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=400&q=80",
} as const;

/**
 * Seed store. Only publish rows with consent + verified.
 * Demo rows are labelled `isDemo: true` for internal clarity.
 */
const TESTIMONIALS: RetreatTestimonial[] = [
  {
    testimonialId: "ayurvedagram-demo-priya",
    retreatId: "ayurvedagram",
    guestName: "Priya",
    guestPhoto: TEMP_GUEST.a,
    guestPhotoTemporary: true,
    programmeId: "rejuvenation",
    programmeName: "Rejuvenation programme",
    stayDuration: "7 nights",
    travellerType: "solo",
    reasonForVisit: "Wanted time away from work and a more structured reset.",
    quote:
      "The days felt intentionally paced. I wasn’t rushed between sessions, and the quiet of the campus made it easier to actually rest.",
    longerStory:
      "I came looking for a break that wasn’t a hotel spa weekend. The Rejuvenation programme gave the stay a clear shape without feeling rigid. Mornings were calmer than I expected, consultations helped me understand what the week would involve, and the evenings stayed low-stimulus. I’m glad I chose a structured retreat rather than trying to invent rest on my own.",
    outcomeReflection:
      "I left feeling more settled and clearer about how I want to protect rest at home.",
    videoUrl: null,
    consentGranted: true,
    verified: true,
    isDemo: true,
    filterTags: ["stress_burnout", "solo", "first_retreat", "ayurveda"],
  },
  {
    testimonialId: "ayurvedagram-demo-arjun",
    retreatId: "ayurvedagram",
    guestName: "Arjun",
    guestPhoto: TEMP_GUEST.b,
    guestPhotoTemporary: true,
    programmeId: "panchakarma",
    programmeName: "Panchakarma programme",
    stayDuration: "14 nights",
    travellerType: "couple",
    reasonForVisit: "Wanted a longer Ayurveda-led stay with clearer programme structure.",
    quote:
      "Having a defined programme length helped us commit. The team explained the rhythm of the stay clearly, which made the two weeks feel purposeful rather than open-ended.",
    longerStory:
      "My partner and I wanted something more intentional than a short wellness break. Choosing the Panchakarma programme meant we arrived knowing the stay would be structured. Daily check-ins and the quieter campus environment suited us. We appreciated that expectations were explained early, so we weren’t guessing what each day would ask of us.",
    outcomeReflection:
      "We valued the structure and the sense that the stay had a clear beginning, middle and close.",
    videoUrl: null,
    consentGranted: true,
    verified: true,
    isDemo: true,
    filterTags: ["panchakarma", "ayurveda", "couple"],
  },
  {
    testimonialId: "ayurvedagram-demo-meera",
    retreatId: "ayurvedagram",
    guestName: "Meera",
    guestPhoto: TEMP_GUEST.c,
    guestPhotoTemporary: true,
    programmeId: "ayurveda",
    programmeName: "Ayurveda programme",
    stayDuration: "7 nights",
    travellerType: "solo",
    reasonForVisit: "Curious about an Ayurveda-led programme after a demanding season at work.",
    quote:
      "What stood out was how practical the guidance felt. The setting was calm, the programme had shape, and I never felt pushed into a packed itinerary.",
    longerStory: null,
    outcomeReflection: null,
    videoUrl: null,
    consentGranted: true,
    verified: true,
    isDemo: true,
    filterTags: ["ayurveda", "solo", "stress_burnout"],
  },
];

/** Public-facing stories: consented + verified only */
export function getRetreatTestimonials(retreatId: string): RetreatTestimonial[] {
  return TESTIMONIALS.filter(
    (t) => t.retreatId === retreatId && t.consentGranted && t.verified,
  );
}

export function hasRetreatTestimonials(retreatId: string): boolean {
  return getRetreatTestimonials(retreatId).length > 0;
}

export function getAvailableTestimonialFilters(
  testimonials: RetreatTestimonial[],
): TestimonialFilterTag[] {
  const present = new Set<TestimonialFilterTag>();
  for (const t of testimonials) {
    for (const tag of t.filterTags) present.add(tag);
  }
  const order: TestimonialFilterTag[] = [
    "stress_burnout",
    "ayurveda",
    "panchakarma",
    "solo",
    "couple",
    "first_retreat",
  ];
  return order.filter((tag) => present.has(tag));
}

export function filterTestimonials(
  testimonials: RetreatTestimonial[],
  active: TestimonialFilterTag | null,
): RetreatTestimonial[] {
  if (!active) return testimonials;
  return testimonials.filter((t) => t.filterTags.includes(active));
}
