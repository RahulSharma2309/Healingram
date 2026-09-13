import type { MatchQuestion } from "./api/matching";
import type { FindMyMatchAnswers } from "./findMyMatch";

export function emptyMatchAnswers(): FindMyMatchAnswers {
  return {
    q1: [],
    q2: [],
    q3: null,
    q4: [],
    needs: [],
    experiences: [],
    duration: null,
    destinations: [],
  };
}

export function applyAnswer(
  answers: FindMyMatchAnswers,
  key: string,
  id: string,
  multi: boolean,
): FindMyMatchAnswers {
  const current =
    key === "q3"
      ? answers.q3
        ? [answers.q3]
        : []
      : ((answers as Record<string, string[] | string | null>)[key] as string[] | undefined) ?? [];
  const next = multi
    ? current.includes(id)
      ? current.filter((item) => item !== id)
      : [...current, id]
    : [id];
  const patch: FindMyMatchAnswers = { ...answers };
  if (key === "q1") {
    patch.q1 = next;
    patch.needs = next;
  } else if (key === "q2") {
    patch.q2 = next;
    patch.experiences = next;
  } else if (key === "q3") {
    patch.q3 = next[0] ?? null;
    patch.duration = next[0] ?? null;
  } else if (key === "q4") {
    patch.q4 = next;
    patch.destinations = next;
  }
  return patch;
}

export function currentSelection(answers: FindMyMatchAnswers, question: MatchQuestion): string[] {
  if (question.key === "q1") return answers.q1 ?? answers.needs;
  if (question.key === "q2") return answers.q2 ?? answers.experiences;
  if (question.key === "q3") return answers.q3 || answers.duration ? [answers.q3 ?? answers.duration!] : [];
  if (question.key === "q4") return answers.q4 ?? answers.destinations;
  return [];
}

export function optionKeysFromQuestions(questions: MatchQuestion[]): string[] {
  return questions.flatMap((question) => question.options.map((option) => option.key));
}
