import { getRetreatDisplayTags } from "../catalogTypes";
import type { FindMyMatchAnswers, RankedMatch } from "../findMyMatch";
import { apiFetch } from "./client";
import { fetchRetreats, type RetreatCard } from "./catalog";
import { retreatCardToLaunch } from "./usePublishedRetreats";

export type MatchQuestionOption = {
  key: string;
  label: string;
  description?: string | null;
  iconKey?: string | null;
  sortOrder: number;
  themeSlugs?: string[];
};

export type MatchQuestion = {
  key: string;
  label: string;
  selectionMode: "multi" | "single";
  sortOrder: number;
  options: MatchQuestionOption[];
};

export type MatchSessionResponse = {
  id: string;
  matches: { slug: string; reasons: string[] }[];
};

export async function fetchMatchOptions(): Promise<MatchQuestion[]> {
  const data = await apiFetch<{ questions: MatchQuestion[] }>("/api/matching/options");
  return data.questions ?? [];
}

export async function createMatchSession(answers: FindMyMatchAnswers): Promise<MatchSessionResponse> {
  const duration = answers.q3 ?? answers.duration;
  if (!duration) {
    throw new Error("Duration is required");
  }
  return apiFetch<MatchSessionResponse>("/api/matching/sessions", {
    method: "POST",
    body: JSON.stringify({
      answers: {
        q1: answers.q1 ?? answers.needs,
        q2: answers.q2 ?? answers.experiences,
        q3: duration,
        q4: answers.q4 ?? answers.destinations,
      },
    }),
  });
}

export async function matchesFromSession(session: MatchSessionResponse): Promise<RankedMatch[]> {
  let cards: RetreatCard[] = [];
  try {
    cards = await fetchRetreats({});
  } catch {
    cards = [];
  }
  const bySlug = new Map(cards.map((c) => [c.slug, retreatCardToLaunch(c)]));

  return session.matches.flatMap((item, index) => {
    const retreat = bySlug.get(item.slug);
    if (!retreat) return [];
    return [
      {
        retreat,
        score: index,
        exact: true,
        reasons: item.reasons,
        tags: getRetreatDisplayTags(retreat),
      },
    ];
  });
}
