import { apiFetch } from "./client";
import { fetchRetreats, type RetreatCard } from "./catalog";
import { retreatCardToLaunch } from "./usePublishedRetreats";
import { getRetreatDisplayTags } from "../../data/launchSupply";
import type { FindMyMatchAnswers, RankedMatch } from "../findMyMatch";

export type MatchSessionResponse = {
  id: string;
  matches: { slug: string; reasons: string[] }[];
};

export async function createMatchSession(answers: FindMyMatchAnswers): Promise<MatchSessionResponse> {
  if (!answers.duration) {
    throw new Error("Duration is required");
  }
  return apiFetch<MatchSessionResponse>("/api/matching/sessions", {
    method: "POST",
    body: JSON.stringify({
      answers: {
        q1: answers.needs,
        q2: answers.experiences,
        q3: answers.duration,
        q4: answers.destinations,
      },
    }),
  });
}

export async function matchesFromSession(
  session: MatchSessionResponse,
): Promise<RankedMatch[]> {
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
        score: 100 - index,
        exact: true,
        reasons: item.reasons,
        tags: getRetreatDisplayTags(retreat),
      },
    ];
  });
}
