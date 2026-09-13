import { describe, expect, it } from "vitest";
import { cardFromDiscovery, cardFromNeed, statesToJourneys } from "../api/useCatalogDiscovery";

describe("dynamic discovery cards", () => {
  it("maps catalog discovery cards without inventing copy or images", () => {
    const card = cardFromDiscovery({
      slug: "sleep-better",
      surface: "need",
      label: "Sleep better",
      description: null,
      imageUrl: null,
      iconKey: "moon",
      href: "/retreats?need=sleep-better",
      sortOrder: 6,
    });
    expect(card.id).toBe("sleep-better");
    expect(card.label).toBe("Sleep better");
    expect(card.description).toBe("");
    expect(card.image).toBe("");
    expect(card.iconKey).toBe("moon");
  });

  it("maps need metadata from the catalog API", () => {
    const card = cardFromNeed({
      slug: "calm-mind",
      label: "Calm my mind",
      description: "Stress",
      imageUrl: "https://example.test/need.jpg",
      iconKey: "brain",
    });
    expect(card.id).toBe("calm-mind");
    expect(card.image).toBe("https://example.test/need.jpg");
  });

  it("maps destination cards from published places", () => {
    const [karnataka] = statesToJourneys([
      {
        slug: "karnataka",
        label: "Karnataka",
        cities: [],
        description: "From published inventory",
        imageUrl: "https://example.test/ka.jpg",
      },
    ]);
    expect(karnataka.region).toBe("karnataka");
    expect(karnataka.to).toBe("/retreats?state=karnataka");
    expect(karnataka.image).toBe("https://example.test/ka.jpg");
  });
});
