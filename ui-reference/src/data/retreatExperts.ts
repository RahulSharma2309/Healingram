/**
 * Listing Component 5 — retreat practitioners / physicians.
 *
 * Only profiles sourced from partner-public materials or confirmed partner data.
 * Do not invent names, qualifications, experience, languages, or videos.
 *
 * Ayurvedagram physician bios below are paraphrased from publicly listed
 * profiles on ayurvedagram.com (retrieved for MVP listing UX). Photos are
 * temporary placeholders until partner media is supplied — not claimed as
 * verified portraits.
 */

export type ExpertRoleGroup = "doctors" | "wellness_practitioners" | "yoga_meditation";

export type RetreatExpert = {
  expertId: string;
  retreatId: string;
  name: string;
  role: string;
  roleGroup: ExpertRoleGroup;
  qualifications: string[];
  /** Only set when years of experience are verified / publicly stated */
  yearsExperience: number | null;
  specialties: string[];
  languages: string[];
  shortBio: string;
  longBio: string;
  /** Temporary or partner image URL */
  image: string;
  imageTemporary?: boolean;
  /** Only set when a real practitioner introduction video exists */
  videoUrl: string | null;
  verified: boolean;
  /** Programme IDs this expert is associated with — never imply personal treatment of every guest */
  programmeIds: string[];
};

export const EXPERT_ROLE_GROUP_LABELS: Record<ExpertRoleGroup, string> = {
  doctors: "Doctors",
  wellness_practitioners: "Wellness Practitioners",
  yoga_meditation: "Yoga & Meditation",
};

/** Temporary portrait placeholders — not verified partner photography */
const TEMP_PORTRAIT = {
  a: "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?w=600&q=80",
  b: "https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?w=600&q=80",
  c: "https://images.unsplash.com/photo-1594824476967-48c8b964273f?w=600&q=80",
  d: "https://images.unsplash.com/photo-1622253692010-333f2da6031d?w=600&q=80",
} as const;

/**
 * Seeded experts — only include when name + role + credentials are supported
 * by a public partner source or confirmed partner feed.
 */
const EXPERTS: RetreatExpert[] = [
  {
    expertId: "ayurvedagram-harsha-nair",
    retreatId: "ayurvedagram",
    name: "Dr. Harsha Nair",
    role: "Senior Ayurveda Physician & Psychologist",
    roleGroup: "doctors",
    qualifications: ["BAMS", "MSc Psychology"],
    yearsExperience: 12,
    specialties: [
      "Mental wellbeing",
      "Women’s health",
      "Infertility support",
      "Lifestyle wellness",
    ],
    languages: [],
    shortBio:
      "Senior consultant physician supporting guests with women’s health concerns, infertility support and mental wellbeing, integrating Ayurveda with psychology.",
    longBio:
      "Dr. Harsha Nair is a senior consultant physician at Ayurvedagram. Her practice focuses on women’s health, infertility support and mental wellbeing, using an integrated Ayurveda and psychology approach. Programme involvement and consultation plans are confirmed with the retreat as part of each guest’s stay.",
    image: TEMP_PORTRAIT.a,
    imageTemporary: true,
    videoUrl: null,
    verified: true,
    programmeIds: ["rejuvenation", "ayurveda"],
  },
  {
    expertId: "ayurvedagram-seethalakshmi",
    retreatId: "ayurvedagram",
    name: "Dr. Seethalakshmi Vaidyanathan",
    role: "Ayurveda Physician",
    roleGroup: "doctors",
    qualifications: ["BAMS", "MD", "Panchakarma"],
    yearsExperience: 6,
    specialties: [
      "Bone and muscle health",
      "Neurological conditions",
      "Sleep-related concerns",
      "Panchakarma",
    ],
    languages: [],
    shortBio:
      "Physician focused on personalised Panchakarma and clinical assessment for musculoskeletal, neurological and sleep-related concerns.",
    longBio:
      "Dr. Seethalakshmi Vaidyanathan is an Ayurveda physician with clinical experience in Panchakarma. Public partner materials describe her focus on diagnosing complex concerns and delivering personalised Panchakarma therapies. Exact involvement in a guest’s programme is confirmed by the retreat after assessment.",
    image: TEMP_PORTRAIT.c,
    imageTemporary: true,
    videoUrl: null,
    verified: true,
    programmeIds: ["panchakarma", "ayurveda"],
  },
  {
    expertId: "ayurvedagram-vishwanath",
    retreatId: "ayurvedagram",
    name: "Dr. Vishwanath S. Guddadar",
    role: "Ayurveda Physician & Panchakarma Specialist",
    roleGroup: "doctors",
    qualifications: ["BAMS"],
    yearsExperience: 24,
    specialties: [
      "Digestive disorders",
      "Diabetes & cholesterol",
      "Arthritis",
      "Cardiovascular concerns",
      "Panchakarma",
    ],
    languages: [],
    shortBio:
      "Ayurveda physician and Panchakarma specialist with extensive clinical experience supporting guests with chronic and lifestyle-related concerns.",
    longBio:
      "Dr. Vishwanath S. Guddadar is listed by Ayurvedagram as an Ayurveda physician, Panchakarma specialist and holistic wellness consultant with over two decades of clinical and academic experience. Partner materials note work with chronic and lifestyle-related conditions including digestive, metabolic, joint and cardiovascular concerns. Individual care plans are set by the retreat after consultation.",
    image: TEMP_PORTRAIT.b,
    imageTemporary: true,
    videoUrl: null,
    verified: true,
    programmeIds: ["panchakarma", "detox", "ayurveda"],
  },
  {
    expertId: "ayurvedagram-manmohan",
    retreatId: "ayurvedagram",
    name: "Dr. Manmohan R",
    role: "Chief Physician",
    roleGroup: "doctors",
    qualifications: ["BAMS"],
    yearsExperience: 18,
    specialties: ["Ayurveda", "Institutional care", "Preventive wellness"],
    languages: [],
    shortBio:
      "Chief Physician at Ayurvedagram, guiding clinical systems and Ayurveda-led care across the heritage wellness centre.",
    longBio:
      "Dr. Manmohan R serves as Chief Physician at Ayurveda Gram Heritage Wellness Centre. Public professional records describe long-standing work enabling preventive and restorative Ayurveda care, mentoring, and institutional healthcare systems at the Whitefield campus. Guest-specific treatment plans remain subject to on-site consultation.",
    image: TEMP_PORTRAIT.d,
    imageTemporary: true,
    videoUrl: null,
    verified: true,
    programmeIds: ["rejuvenation", "panchakarma", "ayurveda"],
  },
];

export function getRetreatExperts(retreatId: string): RetreatExpert[] {
  return EXPERTS.filter((e) => e.retreatId === retreatId && e.verified);
}

export function hasRetreatExperts(retreatId: string): boolean {
  return getRetreatExperts(retreatId).length > 0;
}

export function getExpertById(expertId: string): RetreatExpert | undefined {
  return EXPERTS.find((e) => e.expertId === expertId && e.verified);
}

/** Group experts; omit empty groups */
export function groupRetreatExperts(
  experts: RetreatExpert[],
): { group: ExpertRoleGroup; label: string; experts: RetreatExpert[] }[] {
  const order: ExpertRoleGroup[] = [
    "doctors",
    "wellness_practitioners",
    "yoga_meditation",
  ];
  return order
    .map((group) => ({
      group,
      label: EXPERT_ROLE_GROUP_LABELS[group],
      experts: experts.filter((e) => e.roleGroup === group),
    }))
    .filter((g) => g.experts.length > 0);
}
