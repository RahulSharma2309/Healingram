import { useEffect, useMemo, useState } from "react";
import type { LaunchProgrammeTheme, LaunchRetreat } from "../catalogTypes";
import type { NeedThemeMap } from "../browse";
import {
  fetchNeeds,
  fetchPlaces,
  fetchRetreats,
  type CatalogNeed,
  type CatalogState,
  type RetreatCard,
} from "./catalog";
import { fetchMatchOptions } from "./matching";

export type CatalogLoadSource = "loading" | "api" | "error";

export function retreatCardToLaunch(card: RetreatCard): LaunchRetreat {
  return {
    id: card.slug,
    name: card.name,
    region: card.stateSlug,
    stateLabel: card.stateLabel,
    locality: card.locality,
    programmes: (card.programmeThemes ?? []) as LaunchProgrammeTheme[],
    image: card.imageUrl?.trim() || "",
    typicalDuration: card.typicalDuration ?? undefined,
    priceFrom: card.priceFromInr ?? null,
    mvpDemoVerified: card.priceStatus === "VERIFIED",
  };
}

export function usePublishedRetreats() {
  const [retreats, setRetreats] = useState<LaunchRetreat[]>([]);
  const [places, setPlaces] = useState<CatalogState[]>([]);
  const [needs, setNeeds] = useState<CatalogNeed[]>([]);
  const [needThemeMap, setNeedThemeMap] = useState<NeedThemeMap>({});
  const [source, setSource] = useState<CatalogLoadSource>("loading");

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [items, apiPlaces, apiNeeds, questions] = await Promise.all([
          fetchRetreats({}),
          fetchPlaces(),
          fetchNeeds(),
          fetchMatchOptions().catch(() => []),
        ]);
        if (cancelled) return;
        setRetreats(items.map(retreatCardToLaunch));
        setPlaces(apiPlaces);
        setNeeds(apiNeeds);
        const q1 = questions.find((q) => q.key === "q1");
        const map: NeedThemeMap = {};
        for (const option of q1?.options ?? []) {
          map[option.key] = option.themeSlugs ?? [];
        }
        setNeedThemeMap(map);
        setSource("api");
      } catch {
        if (!cancelled) {
          setRetreats([]);
          setPlaces([]);
          setNeeds([]);
          setNeedThemeMap({});
          setSource("error");
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const byId = useMemo(() => new Map(retreats.map((r) => [r.id, r])), [retreats]);

  return { retreats, places, needs, needThemeMap, source, byId };
}
