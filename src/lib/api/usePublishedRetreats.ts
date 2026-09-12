import { useEffect, useMemo, useState } from "react";
import type { LaunchProgrammeTheme, LaunchRetreat } from "../../data/launchSupply";
import {
  fetchNeeds,
  fetchPlaces,
  fetchRetreats,
  type CatalogNeed,
  type CatalogState,
  type RetreatCard,
} from "./catalog";

const FALLBACK_IMAGE =
  "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=800&q=80";

export type CatalogLoadSource = "loading" | "api" | "error";

export function retreatCardToLaunch(card: RetreatCard): LaunchRetreat {
  return {
    id: card.slug,
    name: card.name,
    region: card.stateSlug,
    stateLabel: card.stateLabel,
    locality: card.locality,
    programmes: (card.programmeThemes ?? []) as LaunchProgrammeTheme[],
    image: card.imageUrl || FALLBACK_IMAGE,
    typicalDuration: card.typicalDuration ?? undefined,
    priceFrom: card.priceFromInr ?? null,
    mvpDemoVerified: card.priceStatus === "VERIFIED",
  };
}

export function usePublishedRetreats() {
  const [retreats, setRetreats] = useState<LaunchRetreat[]>([]);
  const [places, setPlaces] = useState<CatalogState[]>([]);
  const [needs, setNeeds] = useState<CatalogNeed[]>([]);
  const [source, setSource] = useState<CatalogLoadSource>("loading");

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [items, apiPlaces, apiNeeds] = await Promise.all([
          fetchRetreats({}),
          fetchPlaces(),
          fetchNeeds(),
        ]);
        if (cancelled) return;
        setRetreats(items.map(retreatCardToLaunch));
        setPlaces(apiPlaces);
        setNeeds(apiNeeds);
        setSource("api");
      } catch {
        if (!cancelled) {
          setRetreats([]);
          setPlaces([]);
          setNeeds([]);
          setSource("error");
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const byId = useMemo(() => new Map(retreats.map((r) => [r.id, r])), [retreats]);

  return { retreats, places, needs, source, byId };
}
