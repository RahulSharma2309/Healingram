import type { LaunchRetreat } from "./catalogTypes";
import { titleFromSlug } from "./catalogTypes";

export type FindMyMatchAnswers = {
  q1?: string[];
  q2?: string[];
  q3?: string | null;
  q4?: string[];
  needs: string[];
  experiences: string[];
  duration: string | null;
  destinations: string[];
};

export type RankedMatch = {
  retreat: LaunchRetreat;
  score: number;
  exact: boolean;
  reasons: string[];
  tags: string[];
};

export function regionLabel(region: string): string {
  return titleFromSlug(region);
}
