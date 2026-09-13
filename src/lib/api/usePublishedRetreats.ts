import { useEffect, useMemo, useState } from "react";
import type { LaunchProgrammeTheme, LaunchRetreat } from "../catalogTypes";
import type { NeedThemeMap } from "../browse";
import {
  CATALOG_PAGE_SIZE,
  fetchNeeds,
  fetchPlaces,
  fetchRetreatsPage,
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

export function usePublishedRetreats(
  query: {
    need?: string;
    state?: string;
    locality?: string;
    duration?: string;
    theme?: string;
    sort?: string;
    page?: number;
    pageSize?: number;
  } = {},
) {
  const [retreats, setRetreats] = useState<LaunchRetreat[]>([]);
  const [places, setPlaces] = useState<CatalogState[]>([]);
  const [needs, setNeeds] = useState<CatalogNeed[]>([]);
  const [needThemeMap, setNeedThemeMap] = useState<NeedThemeMap>({});
  const [source, setSource] = useState<CatalogLoadSource>("loading");
  const [page, setPageState] = useState(query.page && query.page > 0 ? query.page : 1);
  const [pageSize] = useState(query.pageSize && query.pageSize > 0 ? query.pageSize : CATALOG_PAGE_SIZE);
  const [total, setTotal] = useState(0);
  const need = query.need ?? "";
  const state = query.state ?? "";
  const locality = query.locality ?? "";
  const duration = query.duration ?? "";
  const theme = query.theme ?? "";
  const sort = query.sort ?? "";
  const requestedPage = query.page && query.page > 0 ? query.page : 1;

  useEffect(() => {
    setPageState(requestedPage);
  }, [requestedPage, need, state, locality, duration, theme, sort]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [result, apiPlaces, apiNeeds, questions] = await Promise.all([
          fetchRetreatsPage({
            need: need || undefined,
            state: state || undefined,
            locality: locality || undefined,
            duration: duration || undefined,
            theme: theme || undefined,
            sort: sort || undefined,
            page: requestedPage,
            pageSize,
          }),
          fetchPlaces(),
          fetchNeeds(),
          fetchMatchOptions(),
        ]);
        if (cancelled) return;
        setRetreats((result.items ?? []).map(retreatCardToLaunch));
        setTotal(result.total ?? 0);
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
          setTotal(0);
          setSource("error");
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [need, state, locality, duration, theme, sort, requestedPage, pageSize]);

  const byId = useMemo(() => new Map(retreats.map((r) => [r.id, r])), [retreats]);
  const pageCount = Math.max(1, Math.ceil(total / pageSize));

  return {
    retreats,
    places,
    needs,
    needThemeMap,
    source,
    byId,
    page,
    pageSize,
    total,
    pageCount,
  };
}
