/**
 * Listing Component 2 — why choose / suitability fit copy.
 * Only include verified editorial statements. Never fabricate to fill cards.
 */

export type RetreatHighlightIconKey =
  | "practitioner"
  | "programme"
  | "setting"
  | "yoga"
  | "nature"
  | "coastal"
  | "ayurveda"
  | "duration"
  | "calm";

export type RetreatHighlight = {
  title: string;
  description: string;
  iconKey: RetreatHighlightIconKey;
  verified: boolean;
};

export type SuitabilityStatement = {
  id: string;
  text: string;
  verified: boolean;
  /** Loose tags for Find My Match preference highlighting — not medical */
  matchTags?: string[];
};

export type RetreatListingFit = {
  highlights: RetreatHighlight[];
  suitableFor: SuitabilityStatement[];
  notSuitableFor: SuitabilityStatement[];
};

const FIT: Record<string, RetreatListingFit> = {
  shathayu: {
    highlights: [
      {
        title: "Ayurveda with yoga rhythm",
        description:
          "Often chosen for programmes that pair Ayurveda care with daily yoga rather than a resort-style break.",
        iconKey: "yoga",
        verified: true,
      },
      {
        title: "Near Bengaluru access",
        description:
          "A Devanahalli setting that suits people who want a structured stay without a long journey from the city.",
        iconKey: "setting",
        verified: true,
      },
      {
        title: "Stress-recovery oriented stays",
        description:
          "Designed for guests looking to step away from routine and focus on rest and rejuvenation.",
        iconKey: "calm",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You want Ayurveda and yoga in one structured programme",
        verified: true,
        matchTags: ["ayurveda", "yoga", "structured"],
      },
      {
        id: "s2",
        text: "You’re looking for a few days to about two weeks away from routine",
        verified: true,
        matchTags: ["week", "few-days", "rest"],
      },
      {
        id: "s3",
        text: "You prefer a retreat near Bengaluru rather than a distant coast stay",
        verified: true,
        matchTags: ["bengaluru", "karnataka"],
      },
      {
        id: "s4",
        text: "You’re exploring rejuvenation or stress-recovery focused stays",
        verified: true,
        matchTags: ["rejuvenation", "stress", "rest"],
      },
      {
        id: "s5",
        text: "You value a calm wellness environment over sightseeing",
        verified: true,
        matchTags: ["calm", "quiet"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You’re mainly looking for nightlife or city entertainment",
        verified: true,
      },
      {
        id: "n2",
        text: "You only want a short casual spa visit without a programme",
        verified: true,
      },
      {
        id: "n3",
        text: "You prefer a completely unstructured beach holiday",
        verified: true,
      },
    ],
  },
  ayurvedagram: {
    highlights: [
      {
        title: "Ayurveda-led restorative stays",
        description:
          "Often chosen by guests who want a structured Ayurveda programme rather than a hotel wellness add-on.",
        iconKey: "ayurveda",
        verified: true,
      },
      {
        title: "Defined programme lengths",
        description:
          "Built around multi-day stays with consultations, therapies and restorative time woven into the schedule.",
        iconKey: "programme",
        verified: true,
      },
      {
        title: "Whitefield heritage setting",
        description:
          "A Bengaluru-side retreat environment for people who want immersion without travelling far.",
        iconKey: "setting",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You want a structured Ayurveda-led wellness programme",
        verified: true,
        matchTags: ["ayurveda", "structured", "doctor"],
      },
      {
        id: "s2",
        text: "You’re ready to commit to several nights rather than a day visit",
        verified: true,
        matchTags: ["week", "deeper", "long"],
      },
      {
        id: "s3",
        text: "You’re considering rejuvenation, detox or longer restorative stays",
        verified: true,
        matchTags: ["rejuvenation", "detox", "reset"],
      },
      {
        id: "s4",
        text: "You prefer guided therapies and programme pacing over freeform travel",
        verified: true,
        matchTags: ["structured", "programme"],
      },
      {
        id: "s5",
        text: "You value a calm, wellness-focused setting near Bengaluru",
        verified: true,
        matchTags: ["bengaluru", "calm"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You’re looking for nightlife, shopping trips or sightseeing days",
        verified: true,
      },
      {
        id: "n2",
        text: "You only want a casual spa afternoon",
        verified: true,
      },
      {
        id: "n3",
        text: "You cannot commit to the programme’s minimum stay",
        verified: true,
      },
      {
        id: "n4",
        text: "You prefer a completely unstructured holiday",
        verified: true,
      },
    ],
  },
  tattvam: {
    highlights: [
      {
        title: "Nature-facing short escapes",
        description:
          "Often chosen for quieter stays in the hills near Bengaluru, including weekend-length breaks.",
        iconKey: "nature",
        verified: true,
      },
      {
        title: "Yoga and lifestyle wellness",
        description:
          "Suits guests exploring yoga-led or holistic lifestyle time rather than a long clinical programme.",
        iconKey: "yoga",
        verified: true,
      },
      {
        title: "Flexible shorter stays",
        description:
          "Designed for people who want restorative time without necessarily committing to a multi-week stay.",
        iconKey: "duration",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You want a nature-oriented break near Bengaluru",
        verified: true,
        matchTags: ["nature", "bengaluru", "quiet"],
      },
      {
        id: "s2",
        text: "You’re considering a weekend or short restorative stay",
        verified: true,
        matchTags: ["weekend", "few-days"],
      },
      {
        id: "s3",
        text: "You’re drawn to yoga or lifestyle wellness rather than a long Panchakarma",
        verified: true,
        matchTags: ["yoga", "lifestyle"],
      },
      {
        id: "s4",
        text: "You value a quieter setting over a busy resort atmosphere",
        verified: true,
        matchTags: ["quiet", "calm"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You’re seeking a long, intensive multi-week Ayurveda programme",
        verified: true,
      },
      {
        id: "n2",
        text: "You mainly want beach access or coastal scenery",
        verified: true,
      },
      {
        id: "n3",
        text: "You’re looking for nightlife or packed sightseeing itineraries",
        verified: true,
      },
    ],
  },
  shreyas: {
    highlights: [
      {
        title: "Yoga and meditation focus",
        description:
          "Often chosen by guests who want calm practice-led days rather than a treatment-heavy clinical stay.",
        iconKey: "yoga",
        verified: true,
      },
      {
        title: "Lifestyle reset pace",
        description:
          "Suits people looking for mindful routines and quieter schedules over a few days.",
        iconKey: "calm",
        verified: true,
      },
      {
        title: "Near-city retreat setting",
        description:
          "A Nelamangala base for guests who want immersion with easier access from Bengaluru.",
        iconKey: "setting",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You want yoga and meditation at the centre of your stay",
        verified: true,
        matchTags: ["yoga", "meditation", "calm"],
      },
      {
        id: "s2",
        text: "You’re looking for a short to mid-length restorative break",
        verified: true,
        matchTags: ["few-days", "week"],
      },
      {
        id: "s3",
        text: "You prefer a mindful, quiet environment",
        verified: true,
        matchTags: ["quiet", "calm"],
      },
      {
        id: "s4",
        text: "You’re exploring lifestyle wellness rather than a beach holiday",
        verified: true,
        matchTags: ["lifestyle"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You’re mainly seeking beach leisure or nightlife",
        verified: true,
      },
      {
        id: "n2",
        text: "You want only a one-off spa treatment",
        verified: true,
      },
      {
        id: "n3",
        text: "You prefer a highly medicalised long-stay programme",
        verified: true,
      },
    ],
  },
  soukya: {
    highlights: [
      {
        title: "Longer restorative programmes",
        description:
          "Often chosen by guests prepared for extended stays with a deeper wellness focus.",
        iconKey: "duration",
        verified: true,
      },
      {
        title: "Holistic Ayurveda framing",
        description:
          "Suited to people looking for Ayurveda, yoga and meditation woven into a longer programme.",
        iconKey: "ayurveda",
        verified: true,
      },
      {
        title: "Immersive campus feel",
        description:
          "Designed for guests who want to step fully away from everyday schedules.",
        iconKey: "setting",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You can commit to a longer structured wellness stay",
        verified: true,
        matchTags: ["deeper", "long", "week"],
      },
      {
        id: "s2",
        text: "You’re interested in Ayurveda-led rejuvenation or detox programmes",
        verified: true,
        matchTags: ["ayurveda", "rejuvenation", "detox"],
      },
      {
        id: "s3",
        text: "You prefer guided wellness days over unstructured leisure",
        verified: true,
        matchTags: ["structured"],
      },
      {
        id: "s4",
        text: "You want immersion near Bengaluru rather than a coastal holiday",
        verified: true,
        matchTags: ["bengaluru", "karnataka"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You only have a weekend and want a light leisure break",
        verified: true,
      },
      {
        id: "n2",
        text: "You’re looking for nightlife or sightseeing-led travel",
        verified: true,
      },
      {
        id: "n3",
        text: "You cannot commit to the programme’s longer minimum stay",
        verified: true,
      },
    ],
  },
  mekosha: {
    highlights: [
      {
        title: "Coastal Ayurveda spa suites",
        description:
          "Often chosen for Kerala stays that combine Ayurveda programmes with a quieter coastal setting.",
        iconKey: "coastal",
        verified: true,
      },
      {
        title: "Panchakarma and detox focus",
        description:
          "Suited to guests exploring deeper Ayurveda programmes rather than a short leisure trip.",
        iconKey: "ayurveda",
        verified: true,
      },
      {
        title: "Structured multi-day stays",
        description:
          "Built for people ready to follow a defined programme over several nights.",
        iconKey: "programme",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You want Ayurveda or Panchakarma in a coastal Kerala setting",
        verified: true,
        matchTags: ["ayurveda", "panchakarma", "kerala"],
      },
      {
        id: "s2",
        text: "You’re considering a week or longer restorative programme",
        verified: true,
        matchTags: ["week", "deeper"],
      },
      {
        id: "s3",
        text: "You prefer guided therapies over a casual beach holiday",
        verified: true,
        matchTags: ["structured"],
      },
      {
        id: "s4",
        text: "You’re looking for rejuvenation or detox-oriented time",
        verified: true,
        matchTags: ["rejuvenation", "detox", "reset"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You’re mainly looking for nightlife or party tourism",
        verified: true,
      },
      {
        id: "n2",
        text: "You only want a day spa booking",
        verified: true,
      },
      {
        id: "n3",
        text: "You prefer a completely unstructured beach vacation",
        verified: true,
      },
    ],
  },
  "amal-tamara": {
    highlights: [
      {
        title: "Quiet backwater setting",
        description:
          "Often chosen for calmer Kerala stays with nature and restorative pacing.",
        iconKey: "nature",
        verified: true,
      },
      {
        title: "Ayurveda with yoga space",
        description:
          "Suits guests exploring Ayurveda and yoga without needing a large resort atmosphere.",
        iconKey: "yoga",
        verified: true,
      },
      {
        title: "Shorter restorative stays",
        description:
          "Designed for people looking at a few days of quieter wellness time.",
        iconKey: "duration",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You want a quieter backwater or nature-oriented Kerala stay",
        verified: true,
        matchTags: ["kerala", "nature", "quiet"],
      },
      {
        id: "s2",
        text: "You’re considering a shorter restorative break",
        verified: true,
        matchTags: ["few-days", "weekend"],
      },
      {
        id: "s3",
        text: "You’re interested in Ayurveda and yoga in a calm setting",
        verified: true,
        matchTags: ["ayurveda", "yoga"],
      },
      {
        id: "s4",
        text: "You value stillness over a busy resort calendar",
        verified: true,
        matchTags: ["calm", "quiet"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You’re seeking nightlife or crowded tourist circuits",
        verified: true,
      },
      {
        id: "n2",
        text: "You want an intensive multi-week Panchakarma only",
        verified: true,
      },
      {
        id: "n3",
        text: "You prefer a large, highly social resort experience",
        verified: true,
      },
    ],
  },
  "kalari-rasayana": {
    highlights: [
      {
        title: "Deeper Ayurveda programmes",
        description:
          "Often chosen by guests considering longer Panchakarma-oriented restorative stays.",
        iconKey: "ayurveda",
        verified: true,
      },
      {
        title: "Extended stay orientation",
        description:
          "Suited to people who can commit to multi-week programme lengths.",
        iconKey: "duration",
        verified: true,
      },
      {
        title: "Structured therapeutic pacing",
        description:
          "Built around defined programme days rather than open leisure itineraries.",
        iconKey: "programme",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You want a deeper Ayurveda or Panchakarma-oriented stay",
        verified: true,
        matchTags: ["ayurveda", "panchakarma", "deeper"],
      },
      {
        id: "s2",
        text: "You can commit to a longer programme duration",
        verified: true,
        matchTags: ["deeper", "long"],
      },
      {
        id: "s3",
        text: "You prefer structured days over an unstructured holiday",
        verified: true,
        matchTags: ["structured"],
      },
      {
        id: "s4",
        text: "You’re looking for restorative immersion in Kerala",
        verified: true,
        matchTags: ["kerala", "rejuvenation"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You only have a short weekend available",
        verified: true,
      },
      {
        id: "n2",
        text: "You’re mainly looking for sightseeing and nightlife",
        verified: true,
      },
      {
        id: "n3",
        text: "You want a casual spa visit without a programme",
        verified: true,
      },
    ],
  },
  "prakriti-shakti": {
    highlights: [
      {
        title: "Ayurveda with metabolic focus",
        description:
          "Often considered by guests exploring Ayurveda programmes that include metabolic wellness themes.",
        iconKey: "ayurveda",
        verified: true,
      },
      {
        title: "Yoga alongside therapies",
        description:
          "Suits people who want movement and Ayurveda care in the same stay.",
        iconKey: "yoga",
        verified: true,
      },
      {
        title: "Structured multi-day programmes",
        description:
          "Designed for guests ready to follow a defined wellness schedule.",
        iconKey: "programme",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You’re interested in Ayurveda, Panchakarma or detox programmes",
        verified: true,
        matchTags: ["ayurveda", "panchakarma", "detox"],
      },
      {
        id: "s2",
        text: "You’re exploring metabolic or weight-oriented wellness themes",
        verified: true,
        matchTags: ["weight", "metabolic", "reset"],
      },
      {
        id: "s3",
        text: "You want yoga included in a structured stay",
        verified: true,
        matchTags: ["yoga", "structured"],
      },
      {
        id: "s4",
        text: "You’re looking at a week or longer in Kerala",
        verified: true,
        matchTags: ["week", "kerala"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You’re seeking nightlife or entertainment-led travel",
        verified: true,
      },
      {
        id: "n2",
        text: "You only want an unstructured beach break",
        verified: true,
      },
      {
        id: "n3",
        text: "You cannot commit to the programme’s minimum stay",
        verified: true,
      },
    ],
  },
  somatheeram: {
    highlights: [
      {
        title: "Coastal Ayurveda village",
        description:
          "Often chosen for Kerala programmes in a dedicated Ayurveda village setting by the coast.",
        iconKey: "coastal",
        verified: true,
      },
      {
        title: "Programme-led days",
        description:
          "Built around structured Ayurveda stays with yoga and rejuvenation options.",
        iconKey: "programme",
        verified: true,
      },
      {
        title: "Immersive wellness atmosphere",
        description:
          "Suits guests who want a wellness campus feel rather than a city hotel.",
        iconKey: "setting",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You want Ayurveda or Panchakarma in a coastal Kerala setting",
        verified: true,
        matchTags: ["ayurveda", "panchakarma", "kerala"],
      },
      {
        id: "s2",
        text: "You’re considering a structured multi-day programme",
        verified: true,
        matchTags: ["structured", "week"],
      },
      {
        id: "s3",
        text: "You’re interested in rejuvenation with yoga alongside therapies",
        verified: true,
        matchTags: ["rejuvenation", "yoga"],
      },
      {
        id: "s4",
        text: "You value a wellness-focused village environment",
        verified: true,
        matchTags: ["calm"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You’re mainly looking for nightlife or party tourism",
        verified: true,
      },
      {
        id: "n2",
        text: "You want only a casual spa visit",
        verified: true,
      },
      {
        id: "n3",
        text: "You prefer a completely unstructured holiday",
        verified: true,
      },
    ],
  },
  nattika: {
    highlights: [
      {
        title: "Beachside Ayurveda setting",
        description:
          "Often chosen for Kerala programmes with a beachside wellness atmosphere.",
        iconKey: "coastal",
        verified: true,
      },
      {
        title: "Panchakarma and yoga options",
        description:
          "Suits guests exploring Ayurveda programmes with yoga and detox themes.",
        iconKey: "ayurveda",
        verified: true,
      },
      {
        title: "Multi-day restorative stays",
        description:
          "Designed for people ready to follow a programme over several nights.",
        iconKey: "programme",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You want Ayurveda by the coast in Kerala",
        verified: true,
        matchTags: ["ayurveda", "kerala"],
      },
      {
        id: "s2",
        text: "You’re considering Panchakarma, yoga or detox programmes",
        verified: true,
        matchTags: ["panchakarma", "yoga", "detox"],
      },
      {
        id: "s3",
        text: "You prefer guided wellness over a leisure-only beach trip",
        verified: true,
        matchTags: ["structured"],
      },
      {
        id: "s4",
        text: "You’re looking at several days of restorative time",
        verified: true,
        matchTags: ["week", "few-days"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You’re seeking nightlife-led beach tourism",
        verified: true,
      },
      {
        id: "n2",
        text: "You only want a short spa appointment",
        verified: true,
      },
      {
        id: "n3",
        text: "You cannot commit to the programme’s minimum stay",
        verified: true,
      },
    ],
  },
  carnoustie: {
    highlights: [
      {
        title: "Ayurveda resort setting",
        description:
          "Often chosen for rejuvenation and yoga stays within a larger wellness resort environment.",
        iconKey: "setting",
        verified: true,
      },
      {
        title: "Flexible programme lengths",
        description:
          "Suits guests exploring restorative stays with more flexible duration options.",
        iconKey: "duration",
        verified: true,
      },
      {
        title: "Kerala coastal access",
        description:
          "Designed for people who want wellness programmes with a coastal Kerala backdrop.",
        iconKey: "coastal",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You want Ayurveda or yoga in a Kerala resort setting",
        verified: true,
        matchTags: ["ayurveda", "yoga", "kerala"],
      },
      {
        id: "s2",
        text: "You’re looking for rejuvenation with some schedule flexibility",
        verified: true,
        matchTags: ["rejuvenation", "flexible"],
      },
      {
        id: "s3",
        text: "You prefer a resort-style wellness environment",
        verified: true,
        matchTags: ["lifestyle"],
      },
      {
        id: "s4",
        text: "You’re considering short to mid-length restorative stays",
        verified: true,
        matchTags: ["few-days", "week"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You’re seeking nightlife as the main purpose of travel",
        verified: true,
      },
      {
        id: "n2",
        text: "You want a minimal, ashram-style setting only",
        verified: true,
      },
      {
        id: "n3",
        text: "You only want a day spa booking",
        verified: true,
      },
    ],
  },
  kairali: {
    highlights: [
      {
        title: "Ayurvedic healing village",
        description:
          "Often chosen for longer structured Ayurveda programmes in a village-style campus.",
        iconKey: "ayurveda",
        verified: true,
      },
      {
        title: "Panchakarma-oriented stays",
        description:
          "Suits guests exploring deeper programmes rather than short leisure breaks.",
        iconKey: "programme",
        verified: true,
      },
      {
        title: "Yoga within the programme",
        description:
          "Designed for people who want movement and Ayurveda care together.",
        iconKey: "yoga",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You want a structured Ayurveda or Panchakarma stay",
        verified: true,
        matchTags: ["ayurveda", "panchakarma", "structured"],
      },
      {
        id: "s2",
        text: "You’re prepared for a longer programme duration",
        verified: true,
        matchTags: ["deeper", "long", "week"],
      },
      {
        id: "s3",
        text: "You prefer a wellness village atmosphere",
        verified: true,
        matchTags: ["calm"],
      },
      {
        id: "s4",
        text: "You’re looking for restorative immersion in Kerala",
        verified: true,
        matchTags: ["kerala", "rejuvenation"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You only have a weekend for a light leisure trip",
        verified: true,
      },
      {
        id: "n2",
        text: "You’re mainly seeking nightlife or sightseeing",
        verified: true,
      },
      {
        id: "n3",
        text: "You cannot commit to the programme’s minimum stay",
        verified: true,
      },
    ],
  },
  "niraamaya-surya": {
    highlights: [
      {
        title: "Clifftop Kerala setting",
        description:
          "Often chosen for shorter restorative escapes with a distinctive coastal vantage.",
        iconKey: "coastal",
        verified: true,
      },
      {
        title: "Yoga and meditation space",
        description:
          "Suits guests looking for calmer practice-led days alongside Ayurveda options.",
        iconKey: "yoga",
        verified: true,
      },
      {
        title: "Shorter restorative stays",
        description:
          "Designed for people seeking a few restorative days rather than a long clinical programme.",
        iconKey: "duration",
        verified: true,
      },
    ],
    suitableFor: [
      {
        id: "s1",
        text: "You want a shorter Kerala restorative escape",
        verified: true,
        matchTags: ["few-days", "weekend", "kerala"],
      },
      {
        id: "s2",
        text: "You’re interested in yoga, meditation or light Ayurveda",
        verified: true,
        matchTags: ["yoga", "meditation", "ayurveda"],
      },
      {
        id: "s3",
        text: "You value a scenic, calm coastal setting",
        verified: true,
        matchTags: ["calm", "quiet"],
      },
      {
        id: "s4",
        text: "You’re looking for rest rather than sightseeing intensity",
        verified: true,
        matchTags: ["rest", "calm"],
      },
    ],
    notSuitableFor: [
      {
        id: "n1",
        text: "You’re seeking a long intensive Panchakarma only",
        verified: true,
      },
      {
        id: "n2",
        text: "You’re mainly looking for nightlife",
        verified: true,
      },
      {
        id: "n3",
        text: "You want a budget backpacker-style trip",
        verified: true,
      },
    ],
  },
};

export function getRetreatListingFit(retreatId: string): RetreatListingFit {
  const fit = FIT[retreatId];
  if (!fit) {
    return { highlights: [], suitableFor: [], notSuitableFor: [] };
  }
  return {
    highlights: fit.highlights.filter((h) => h.verified).slice(0, 3),
    suitableFor: fit.suitableFor.filter((s) => s.verified),
    notSuitableFor: fit.notSuitableFor.filter((s) => s.verified),
  };
}

/** Light FMM alignment — keyword overlap only, no medical interpretation */
export function statementMatchesPreferences(
  statement: SuitabilityStatement,
  preferenceTokens: string[],
): boolean {
  if (!statement.matchTags?.length || preferenceTokens.length === 0) return false;
  const tokens = preferenceTokens.map((t) => t.toLowerCase());
  return statement.matchTags.some((tag) =>
    tokens.some((t) => t.includes(tag) || tag.includes(t)),
  );
}
