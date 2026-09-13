import { describe, expect, it } from "vitest";
import {
  filterBrowseRetreats,
  parseBrowseStateFromParams,
  retreatMatchesNeed,
} from "../browse";
import type { LaunchRetreat } from "../catalogTypes";

const retreat: LaunchRetreat = {
  id: "ayurvedagram",
  name: "Ayurvedagram",
  region: "karnataka",
  stateLabel: "Karnataka",
  locality: "Bengaluru",
  programmes: ["ayurveda", "rejuvenation"],
  image: "",
  typicalDuration: "7 nights",
  priceFrom: 45000,
};

describe("browse filters", () => {
  it("matches questionnaire need slugs through theme maps", () => {
    expect(
      retreatMatchesNeed(retreat, "go-deeper", { "go-deeper": ["ayurveda", "panchakarma"] }),
    ).toBe(true);
    expect(retreatMatchesNeed(retreat, "calm-mind", { "calm-mind": ["stress_burnout"] })).toBe(false);
  });

  it("filters published inventory without inventing extra retreats", () => {
    const results = filterBrowseRetreats({ needs: ["go-deeper"] }, [retreat], {
      "go-deeper": ["ayurveda"],
    });
    expect(results).toHaveLength(1);
    expect(filterBrowseRetreats({ needs: ["missing"] }, [retreat], { missing: ["yoga"] })).toHaveLength(0);
  });

  it("reads need filters from the URL", () => {
    const state = parseBrowseStateFromParams(new URLSearchParams("need=calm-mind&state=karnataka"));
    expect(state.needs).toEqual(["calm-mind"]);
    expect(state.locations).toEqual(["region:karnataka"]);
  });
});
