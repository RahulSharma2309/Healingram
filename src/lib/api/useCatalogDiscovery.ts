import { useEffect, useState } from "react";
import {
  EXPLORE_BY_NEED_CARDS,
  HOME_DESTINATION_JOURNEYS,
  type ExploreByNeedCard,
} from "../../data/launchSupply";
import { fetchNeeds, fetchPlaces, type CatalogNeed, type CatalogState } from "./catalog";

export type DiscoverySource = "loading" | "api" | "error";

const CARD_BY_SLUG = new Map(EXPLORE_BY_NEED_CARDS.map((c) => [c.id, c]));

const STATE_IMAGES: Record<string, string> = {
  karnataka: HOME_DESTINATION_JOURNEYS.find((j) => j.region === "karnataka")?.image ?? "",
  kerala: HOME_DESTINATION_JOURNEYS.find((j) => j.region === "kerala")?.image ?? "",
  goa: "https://images.unsplash.com/photo-1512343879784-a960bf40e7f2?w=1400&q=80",
  "himachal-pradesh":
    "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=1400&q=80",
  rajasthan: "https://images.unsplash.com/photo-1524492412937-b28074a5d7c7?w=1400&q=80",
  "tamil-nadu": "https://images.unsplash.com/photo-1564507592333-c60657eea523?w=1400&q=80",
  maharashtra: "https://images.unsplash.com/photo-1439066615861-d1af74d74000?w=1400&q=80",
};

function needToCard(need: CatalogNeed): ExploreByNeedCard {
  const existing = CARD_BY_SLUG.get(need.slug);
  if (existing) return existing;
  return {
    id: need.slug,
    label: need.label,
    description: "Retreats that match this need from published inventory.",
    image: EXPLORE_BY_NEED_CARDS[0]?.image ?? "",
    programmes: [],
  };
}

function statesToJourneys(states: CatalogState[]) {
  return states.map((state) => {
    const known = HOME_DESTINATION_JOURNEYS.find((j) => j.region === state.slug);
    return {
      region: state.slug,
      label: state.label,
      description:
        known?.description ??
        `Published retreats in ${state.label}${state.cities.length ? ` — ${state.cities.length} areas` : ""}.`,
      cta: `Explore ${state.label}`,
      to: `/retreats?state=${encodeURIComponent(state.slug)}`,
      image:
        known?.image ??
        STATE_IMAGES[state.slug] ??
        "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=1400&q=80",
      imageTemporary: true as const,
    };
  });
}

export function useCatalogDiscovery() {
  const [needs, setNeeds] = useState<ExploreByNeedCard[]>([]);
  const [destinations, setDestinations] = useState<ReturnType<typeof statesToJourneys>>([]);
  const [source, setSource] = useState<DiscoverySource>("loading");

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [apiNeeds, apiPlaces] = await Promise.all([fetchNeeds(), fetchPlaces()]);
        if (cancelled) return;
        setNeeds(apiNeeds.map(needToCard));
        setDestinations(statesToJourneys(apiPlaces));
        setSource("api");
      } catch {
        if (!cancelled) {
          setNeeds([]);
          setDestinations([]);
          setSource("error");
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return { needs, destinations, source };
}
