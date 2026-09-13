import { useEffect, useState } from "react";
import type { ExploreByNeedCard } from "../catalogTypes";
import { fetchDiscovery, fetchNeeds, fetchPlaces, type CatalogNeed, type CatalogState, type DiscoveryCard } from "./catalog";

export type DiscoverySource = "loading" | "api" | "error";

export function cardFromDiscovery(card: DiscoveryCard): ExploreByNeedCard {
  return {
    id: card.slug,
    label: card.label,
    description: card.description ?? "",
    image: card.imageUrl ?? "",
    iconKey: card.iconKey ?? undefined,
    href: card.href ?? undefined,
    programmes: [],
  };
}

export function cardFromNeed(need: CatalogNeed): ExploreByNeedCard {
  return {
    id: need.slug,
    label: need.label,
    description: need.description ?? "",
    image: need.imageUrl ?? "",
    iconKey: need.iconKey ?? undefined,
    programmes: [],
  };
}

export function statesToJourneys(states: CatalogState[]) {
  return states.map((state) => ({
    region: state.slug,
    label: state.label,
    description: state.description ?? `Published retreats in ${state.label}.`,
    cta: `Explore ${state.label}`,
    to: `/retreats?state=${encodeURIComponent(state.slug)}`,
    image: state.imageUrl ?? "",
    imageTemporary: false as const,
  }));
}

export function useCatalogDiscovery() {
  const [needs, setNeeds] = useState<ExploreByNeedCard[]>([]);
  const [destinations, setDestinations] = useState<ReturnType<typeof statesToJourneys>>([]);
  const [source, setSource] = useState<DiscoverySource>("loading");

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [discoveryResult, apiNeeds, apiPlaces] = await Promise.allSettled([
          fetchDiscovery(),
          fetchNeeds(),
          fetchPlaces(),
        ]);
        if (apiNeeds.status === "rejected") {
          throw apiNeeds.reason;
        }
        if (apiPlaces.status === "rejected") {
          throw apiPlaces.reason;
        }
        const discovery = discoveryResult.status === "fulfilled" ? discoveryResult.value : ([] as DiscoveryCard[]);
        const apiNeedsValue = apiNeeds.value;
        const apiPlacesValue = apiPlaces.value;
        if (cancelled) return;
        const needCards = discovery.filter((c) => c.surface === "need").map(cardFromDiscovery);
        setNeeds(needCards.length > 0 ? needCards : apiNeedsValue.map(cardFromNeed));
        const destCards = discovery.filter((c) => c.surface === "destination");
        setDestinations(
          destCards.length > 0
            ? destCards.map((card) => ({
                region: card.slug,
                label: card.label,
                description: card.description ?? "",
                cta: `Explore ${card.label}`,
                to: card.href || `/retreats?state=${encodeURIComponent(card.slug)}`,
                image: card.imageUrl ?? "",
                imageTemporary: false as const,
              }))
            : statesToJourneys(apiPlacesValue),
        );
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
