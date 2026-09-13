import { useEffect, useState } from "react";
import {
  EXPLORE_BY_NEED_CARDS,
  HOME_DESTINATION_JOURNEYS,
  type ExploreByNeedCard,
} from "../../data/launchSupply";
import { fetchNeeds, fetchPlaces, type CatalogNeed, type CatalogState } from "./catalog";

export type DiscoverySource = "api" | "fallback";

const CARD_BY_SLUG = new Map(EXPLORE_BY_NEED_CARDS.map((c) => [c.id, c]));

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
      image: known?.image ?? "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=1400&q=80",
      imageTemporary: true as const,
    };
  });
}

export function useCatalogDiscovery() {
  const [needs, setNeeds] = useState(EXPLORE_BY_NEED_CARDS);
  const [destinations, setDestinations] = useState(
    HOME_DESTINATION_JOURNEYS.map((d) => ({ ...d, region: d.region as string })),
  );
  const [source, setSource] = useState<DiscoverySource>("fallback");

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [apiNeeds, apiPlaces] = await Promise.all([fetchNeeds(), fetchPlaces()]);
        if (cancelled) return;
        if (apiNeeds.length) {
          setNeeds(apiNeeds.map(needToCard));
          setSource("api");
        }
        if (apiPlaces.length) {
          setDestinations(statesToJourneys(apiPlaces));
          setSource("api");
        }
      } catch {
        if (!cancelled) setSource("fallback");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return { needs, destinations, source };
}
