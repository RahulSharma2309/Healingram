import {
  Check,
  CircleMinus,
  Flower2,
  Leaf,
  Mountain,
  Sparkles,
  Sun,
  Trees,
  Waves,
  type LucideIcon,
} from "lucide-react";
import type { FindMyMatchListingState } from "./RetreatListingComponent1";
import {
  getRetreatListingFit,
  statementMatchesPreferences,
  type RetreatHighlightIconKey,
  type SuitabilityStatement,
} from "../../data/retreatListingFit";

type Props = {
  retreatId: string;
  findMyMatch?: FindMyMatchListingState | null;
};

const ICONS: Record<RetreatHighlightIconKey, LucideIcon> = {
  practitioner: Sparkles,
  programme: Flower2,
  setting: Mountain,
  yoga: Sun,
  nature: Trees,
  coastal: Waves,
  ayurveda: Leaf,
  duration: Flower2,
  calm: Leaf,
};

function preferenceTokensFromMatch(
  match: FindMyMatchListingState | null | undefined,
): string[] {
  if (!match) return [];
  return [...(match.youToldUs ?? []), ...(match.matches ?? [])]
    .join(" ")
    .toLowerCase()
    .split(/[^a-z0-9+]+/)
    .filter(Boolean);
}

export function RetreatListingComponent2({ retreatId, findMyMatch }: Props) {
  const fit = getRetreatListingFit(retreatId);
  const hasHighlights = fit.highlights.length > 0;
  const hasSuitable = fit.suitableFor.length > 0;
  const hasNotSuitable = fit.notSuitableFor.length > 0;

  if (!hasHighlights && !hasSuitable && !hasNotSuitable) {
    return null;
  }

  const tokens = preferenceTokensFromMatch(findMyMatch);
  const showPersonalization = tokens.length > 0 && hasSuitable;

  return (
    <section className="mt-14 pt-12 border-t border-sand-200" aria-labelledby="listing-c2-heading">
      {hasHighlights && (
        <div className="mb-12 md:mb-14">
          <h2
            id="listing-c2-heading"
            className="font-display text-2xl md:text-3xl font-semibold text-sage-800 text-balance"
          >
            Why people choose this retreat
          </h2>
          <p className="mt-2 text-sm md:text-base text-sage-600 max-w-2xl">
            A quick look at what makes this experience different and who it may suit best.
          </p>

          <ul className="mt-8 grid gap-4 md:grid-cols-3">
            {fit.highlights.map((h) => {
              const Icon = ICONS[h.iconKey] ?? Sparkles;
              return (
                <li
                  key={h.title}
                  className="rounded-2xl border border-sand-200/90 bg-sand-50/40 px-5 py-5"
                >
                  <div className="flex h-9 w-9 items-center justify-center rounded-full bg-white border border-sand-200 text-teal-700">
                    <Icon className="w-4 h-4" aria-hidden />
                  </div>
                  <h3 className="mt-4 font-display text-lg font-semibold text-sage-800 text-balance">
                    {h.title}
                  </h3>
                  <p className="mt-2 text-sm text-sage-600 leading-relaxed">{h.description}</p>
                </li>
              );
            })}
          </ul>
        </div>
      )}

      {(hasSuitable || hasNotSuitable) && (
        <div>
          <h2 className="font-display text-2xl md:text-3xl font-semibold text-sage-800 text-balance">
            Is this retreat right for you?
          </h2>

          {showPersonalization && (
            <p className="mt-3 text-sm font-medium text-teal-800">Based on your preferences</p>
          )}

          <div className="mt-6 grid gap-5 lg:grid-cols-2">
            {hasSuitable && (
              <SuitPanel
                title="This may suit you if…"
                tone="suit"
                items={fit.suitableFor}
                tokens={tokens}
                highlightMatches={showPersonalization}
              />
            )}
            {hasNotSuitable && (
              <SuitPanel
                title="It may not be the right fit if…"
                tone="not"
                items={fit.notSuitableFor}
                tokens={[]}
                highlightMatches={false}
              />
            )}
          </div>
        </div>
      )}
    </section>
  );
}

function SuitPanel({
  title,
  tone,
  items,
  tokens,
  highlightMatches,
}: {
  title: string;
  tone: "suit" | "not";
  items: SuitabilityStatement[];
  tokens: string[];
  highlightMatches: boolean;
}) {
  return (
    <div
      className={`rounded-2xl border px-5 py-6 md:px-6 ${
        tone === "suit"
          ? "border-sand-200 bg-white"
          : "border-sand-200 bg-sage-50/50"
      }`}
    >
      <h3 className="font-display text-lg font-semibold text-sage-800 mb-4">{title}</h3>
      <ul className="space-y-3">
        {items.map((item) => {
          const aligned =
            highlightMatches && statementMatchesPreferences(item, tokens);
          return (
            <li
              key={item.id}
              className={`flex gap-2.5 text-sm leading-snug ${
                aligned ? "text-sage-800 font-medium" : "text-sage-700"
              }`}
            >
              {tone === "suit" ? (
                <Check
                  className={`w-4 h-4 shrink-0 mt-0.5 ${
                    aligned ? "text-teal-700" : "text-teal-600/80"
                  }`}
                  aria-hidden
                />
              ) : (
                <CircleMinus
                  className="w-4 h-4 shrink-0 mt-0.5 text-sage-500"
                  aria-hidden
                />
              )}
              <span>
                {item.text}
                {aligned && (
                  <span className="sr-only"> (aligns with your preferences)</span>
                )}
              </span>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
