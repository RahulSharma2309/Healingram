import { describe, expect, it } from "vitest";
import type { MatchQuestion } from "../api/matching";
import {
  applyAnswer,
  currentSelection,
  emptyMatchAnswers,
  optionKeysFromQuestions,
} from "../questionnaire";

const questions: MatchQuestion[] = [
  {
    key: "q1",
    label: "What do you need?",
    selectionMode: "multi",
    sortOrder: 1,
    options: [
      { key: "calm-mind", label: "Calm my mind", sortOrder: 1 },
      { key: "sleep-better", label: "Sleep better", sortOrder: 2 },
    ],
  },
  {
    key: "q3",
    label: "How long?",
    selectionMode: "single",
    sortOrder: 3,
    options: [{ key: "weekend", label: "A weekend", sortOrder: 1 }],
  },
];

describe("dynamic questionnaire", () => {
  it("renders whatever option keys the API returned, including new ones", () => {
    expect(optionKeysFromQuestions(questions)).toEqual(["calm-mind", "sleep-better", "weekend"]);
  });

  it("stores selected option ids without inventing defaults", () => {
    const next = applyAnswer(emptyMatchAnswers(), "q1", "sleep-better", true);
    expect(next.q1).toEqual(["sleep-better"]);
    expect(next.needs).toEqual(["sleep-better"]);
    expect(currentSelection(next, questions[0])).toEqual(["sleep-better"]);
  });

  it("keeps single-select duration as one server option id", () => {
    const next = applyAnswer(emptyMatchAnswers(), "q3", "weekend", false);
    expect(next.q3).toBe("weekend");
    expect(currentSelection(next, questions[1])).toEqual(["weekend"]);
  });
});
