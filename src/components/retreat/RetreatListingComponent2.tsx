import { Check } from "lucide-react";
import type { FindMyMatchListingState } from "./RetreatListingComponent1";

type Props = {
  retreatId: string;
  findMyMatch?: FindMyMatchListingState | null;
};

/** Match reasons come from the matching API session, not local listing copy. */
export function RetreatListingComponent2({ findMyMatch }: Props) {
  const reasons = findMyMatch?.matches ?? [];
  const told = findMyMatch?.youToldUs ?? [];
  if (reasons.length === 0 && told.length === 0) return null;

  return (
    <section className="mt-14 pt-12 border-t border-sand-200" aria-labelledby="listing-match-heading">
      <h2
        id="listing-match-heading"
        className="font-display text-2xl md:text-3xl font-semibold text-sage-800 text-balance"
      >
        Why this matched you
      </h2>
      {told.length > 0 && (
        <p className="mt-3 text-sm text-sage-600">
          <span className="font-medium text-sage-800">You told us: </span>
          {told.join(" · ")}
        </p>
      )}
      {reasons.length > 0 && (
        <ul className="mt-6 space-y-2">
          {reasons.map((reason) => (
            <li key={reason} className="flex gap-2 text-sm text-sage-700">
              <Check className="w-4 h-4 text-teal-600 shrink-0 mt-0.5" />
              {reason}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
