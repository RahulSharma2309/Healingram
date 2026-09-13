import { describe, expect, it } from "vitest";
import type { RetreatListing } from "../api/catalog";
import { hasSectionItems, includedLabels, mapRetreatListingToView } from "../api/listing";

const listing: RetreatListing = {
  slug: "empty-stay",
  name: "Empty Stay",
  locality: "Kollam",
  stateSlug: "kerala",
  stateLabel: "Kerala",
  imageUrl: "",
  positioning: "",
  priceStatus: "ON_REQUEST",
  programmeThemes: [],
  programmes: [],
  experts: [],
  testimonials: [],
  rooms: [],
  inclusions: [],
  media: [],
};

describe("dynamic catalog listing", () => {
  it("keeps empty API sections empty", () => {
    expect(hasSectionItems(listing.experts)).toBe(false);
    expect(hasSectionItems(listing.rooms)).toBe(false);
    expect(includedLabels(listing.inclusions)).toEqual([]);
    const view = mapRetreatListingToView(listing);
    expect(view.programmeOptions).toEqual([]);
    expect(view.media).toEqual([]);
    expect(view.retreat.name).toBe("Empty Stay");
  });
});
