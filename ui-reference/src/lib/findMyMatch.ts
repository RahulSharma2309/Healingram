/**
 * Find My Match — scoring against launch inventory only.
 * Option → programme / region mappings live here so the UI stays declarative.
 */

import {
  LAUNCH_DESTINATIONS,
  LAUNCH_RETREATS,
  getRetreatDisplayTags,
  type LaunchProgrammeTheme,
  type LaunchRetreat,
  type LaunchRegion,
} from "../data/launchSupply";

export type MatchNeedId =
  | "calm-mind"
  | "rest-recharge"
  | "reset-body"
  | "go-deeper"
  | "real-break";

export type MatchExperienceId =
  | "doctor-ayurveda"
  | "yoga-meditation"
  | "quiet-escape"
  | "structured"
  | "open-rec";

export type MatchDurationId =
  | "weekend"
  | "few-days"
  | "week"
  | "deeper"
  | "flexible";

export type MatchDestinationId =
  | "bengaluru-nearby"
  | "karnataka"
  | "kerala"
  | "peaceful"
  | "anywhere";

export type FindMyMatchAnswers = {
  needs: MatchNeedId[];
  experiences: MatchExperienceId[];
  duration: MatchDurationId | null;
  destinations: MatchDestinationId[];
};

const NEED_PROGRAMMES: Record<MatchNeedId, LaunchProgrammeTheme[]> = {
  "calm-mind": ["stress_burnout", "meditation", "yoga"],
  "rest-recharge": ["rejuvenation", "stress_burnout", "weekend"],
  "reset-body": ["detox", "weight_metabolic", "panchakarma", "ayurveda"],
  "go-deeper": ["ayurveda", "panchakarma", "yoga", "meditation"],
  "real-break": ["lifestyle_holistic", "nature_wellness", "weekend", "rejuvenation"],
};

const EXPERIENCE_PROGRAMMES: Record<MatchExperienceId, LaunchProgrammeTheme[]> = {
  "doctor-ayurveda": ["ayurveda", "panchakarma"],
  "yoga-meditation": ["yoga", "meditation"],
  "quiet-escape": ["nature_wellness", "rejuvenation", "lifestyle_holistic"],
  structured: ["panchakarma", "long_stay", "detox", "ayurveda"],
  "open-rec": [],
};

const DURATION_PROGRAMMES: Record<MatchDurationId, LaunchProgrammeTheme[]> = {
  weekend: ["weekend"],
  "few-days": ["weekend", "rejuvenation", "yoga", "meditation"],
  week: ["rejuvenation", "ayurveda", "yoga", "detox"],
  deeper: ["long_stay", "panchakarma", "detox", "ayurveda"],
  flexible: [],
};

const BENGALURU_NEARBY = new Set<string>(
  LAUNCH_DESTINATIONS.karnataka.localities.filter((l) => l !== "Bengaluru"),
);

function programmesFromNeeds(needs: MatchNeedId[]): Set<LaunchProgrammeTheme> {
  const set = new Set<LaunchProgrammeTheme>();
  for (const n of needs) {
    for (const p of NEED_PROGRAMMES[n]) set.add(p);
  }
  return set;
}

function programmesFromExperiences(exps: MatchExperienceId[]): Set<LaunchProgrammeTheme> {
  const set = new Set<LaunchProgrammeTheme>();
  let openOnly = exps.length > 0;
  for (const e of exps) {
    const list = EXPERIENCE_PROGRAMMES[e];
    if (list.length > 0) {
      openOnly = false;
      for (const p of list) set.add(p);
    }
  }
  return openOnly ? new Set() : set;
}

function destinationAllows(retreat: LaunchRetreat, destinations: MatchDestinationId[]): boolean {
  if (!destinations.length || destinations.includes("anywhere")) return true;
  if (destinations.includes("peaceful")) {
    // Soft preference — scored separately; still allow all for hard filter
  }
  const hard = destinations.filter((d) => d !== "peaceful" && d !== "anywhere");
  if (!hard.length) return true;

  return hard.some((d) => {
    if (d === "kerala") return retreat.region === "kerala";
    if (d === "karnataka") return retreat.region === "karnataka";
    if (d === "bengaluru-nearby") {
      return retreat.region === "karnataka" && BENGALURU_NEARBY.has(retreat.locality);
    }
    return false;
  });
}

function durationHint(retreat: LaunchRetreat, duration: MatchDurationId | null): boolean {
  if (!duration || duration === "flexible") return true;
  const wanted = DURATION_PROGRAMMES[duration];
  if (!wanted.length) return true;
  if (wanted.some((p) => retreat.programmes.includes(p))) return true;
  // Soft: long_stay properties also suit deeper resets even without exact tag hit above
  if (duration === "deeper" && retreat.programmes.includes("long_stay")) return true;
  if (duration === "weekend" && retreat.typicalDuration?.toLowerCase().includes("weekend")) return true;
  return false;
}

export type MatchReason = string;

export type RankedMatch = {
  retreat: LaunchRetreat;
  score: number;
  exact: boolean;
  reasons: MatchReason[];
  tags: string[];
};

function buildReasons(
  retreat: LaunchRetreat,
  answers: FindMyMatchAnswers,
  overlapNeeds: LaunchProgrammeTheme[],
  overlapExp: LaunchProgrammeTheme[],
): MatchReason[] {
  const reasons: MatchReason[] = [];

  if (answers.duration && answers.duration !== "flexible") {
    if (
      durationHint(retreat, answers.duration) ||
      DURATION_PROGRAMMES[answers.duration].some((p) => retreat.programmes.includes(p))
    ) {
      reasons.push("Fits your available time");
    }
  }

  if (overlapExp.includes("ayurveda") || overlapNeeds.includes("ayurveda")) {
    if (retreat.programmes.includes("ayurveda")) reasons.push("Strong Ayurveda focus");
  }
  if (overlapExp.includes("panchakarma") || overlapNeeds.includes("panchakarma")) {
    if (retreat.programmes.includes("panchakarma")) reasons.push("Panchakarma programmes available");
  }
  if (overlapExp.includes("yoga") || overlapNeeds.includes("yoga")) {
    if (retreat.programmes.includes("yoga")) reasons.push("Yoga practice on offer");
  }
  if (overlapExp.includes("meditation") || overlapNeeds.includes("meditation")) {
    if (retreat.programmes.includes("meditation")) reasons.push("Meditation-friendly setting");
  }
  if (overlapNeeds.includes("stress_burnout") && retreat.programmes.includes("stress_burnout")) {
    reasons.push("Suited to stress recovery stays");
  }
  if (overlapNeeds.includes("rejuvenation") && retreat.programmes.includes("rejuvenation")) {
    reasons.push("Suitable for a restorative stay");
  }
  if (overlapNeeds.includes("detox") && retreat.programmes.includes("detox")) {
    reasons.push("Detox-oriented programmes");
  }
  if (overlapNeeds.includes("weight_metabolic") && retreat.programmes.includes("weight_metabolic")) {
    reasons.push("Metabolic wellness programmes");
  }

  if (answers.destinations.includes("bengaluru-nearby") && retreat.region === "karnataka") {
    reasons.push("Close to Bengaluru");
  }
  if (answers.destinations.includes("kerala") && retreat.region === "kerala") {
    reasons.push("Based in Kerala");
  }
  if (answers.destinations.includes("karnataka") && retreat.region === "karnataka") {
    reasons.push("Based in Karnataka");
  }
  if (
    answers.destinations.includes("peaceful") &&
    (retreat.programmes.includes("nature_wellness") || retreat.programmes.includes("rejuvenation"))
  ) {
    reasons.push("Quiet, restorative setting");
  }

  if (answers.experiences.includes("quiet-escape") && retreat.programmes.includes("nature_wellness")) {
    reasons.push("Nature-led atmosphere");
  }
  if (answers.experiences.includes("structured") && retreat.programmes.includes("long_stay")) {
    reasons.push("Structured longer programmes");
  }

  // Dedupe and cap
  return [...new Set(reasons)].slice(0, 3);
}

export function rankFindMyMatch(answers: FindMyMatchAnswers): {
  exact: RankedMatch[];
  closest: RankedMatch[];
} {
  const needProgs = programmesFromNeeds(answers.needs);
  const expProgs = programmesFromExperiences(answers.experiences);
  const duration = answers.duration;

  const scored: RankedMatch[] = [];

  for (const retreat of LAUNCH_RETREATS) {
    let score = 0;
    const overlapNeeds = [...needProgs].filter((p) => retreat.programmes.includes(p));
    const overlapExp = [...expProgs].filter((p) => retreat.programmes.includes(p));

    // Need overlap (primary)
    if (needProgs.size === 0) score += 2;
    else score += overlapNeeds.length * 4;

    // Experience overlap
    if (expProgs.size === 0 || answers.experiences.includes("open-rec")) score += 2;
    else score += overlapExp.length * 3;

    // Duration
    if (!duration || duration === "flexible") score += 2;
    else if (durationHint(retreat, duration)) score += 3;
    else score -= 1;

    // Destination hard preference
    const destOk = destinationAllows(retreat, answers.destinations);
    if (!destOk) {
      // Still consider as closest with penalty
      score -= 4;
    } else {
      score += 3;
      if (answers.destinations.includes("peaceful") && retreat.programmes.includes("nature_wellness")) {
        score += 1;
      }
    }

    if (score <= 0 && overlapNeeds.length === 0 && overlapExp.length === 0) continue;

    const exact =
      destOk &&
      (needProgs.size === 0 || overlapNeeds.length > 0) &&
      (expProgs.size === 0 || overlapExp.length > 0 || answers.experiences.includes("open-rec")) &&
      (!duration || duration === "flexible" || durationHint(retreat, duration));

    scored.push({
      retreat,
      score,
      exact,
      reasons: buildReasons(retreat, answers, overlapNeeds, overlapExp),
      tags: getRetreatDisplayTags(retreat, 3),
    });
  }

  scored.sort((a, b) => b.score - a.score);

  const exact = scored.filter((s) => s.exact && s.score > 0).slice(0, 6);
  if (exact.length > 0) {
    return { exact, closest: [] };
  }

  // Closest: top scored that still relate somehow
  const closest = scored.filter((s) => s.score > 0).slice(0, 4);
  return { exact: [], closest };
}

export function regionLabel(region: LaunchRegion): string {
  return LAUNCH_DESTINATIONS[region].regionLabel;
}
