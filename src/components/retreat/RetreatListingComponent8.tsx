import { useEffect, useMemo } from "react";
import { Check, Minus } from "lucide-react";
import { getListingProgrammes } from "../../data/programmeCatalog";
import {
  conditionLabel,
  getProgrammeInclusionSet,
  groupInclusions,
  INCLUSION_GROUP_LABELS,
  type ProgrammeExclusionItem,
  type ProgrammeInclusionItem,
} from "../../data/programmeInclusions";
import { useListingPlan } from "../../lib/listingPlanContext";

type Props = {
  retreatId: string;
};

export function RetreatListingComponent8({ retreatId }: Props) {
  const programmes = useMemo(() => getListingProgrammes(retreatId), [retreatId]);
  const { programmeId, durationNights } = useListingPlan();

  const soleProgrammeId =
    programmes.length === 1 ? programmes[0]?.programmeId ?? null : null;

  const effectiveProgrammeId = programmeId || soleProgrammeId;
  const selected = programmes.find((p) => p.programmeId === effectiveProgrammeId) ?? null;
  const inclusionSet = effectiveProgrammeId
    ? getProgrammeInclusionSet(retreatId, effectiveProgrammeId)
    : null;

  useEffect(() => {
    if (programmes.length === 0 && import.meta.env.DEV) {
      console.warn(
        `[Healingram] Component 8: no programmes for retreat "${retreatId}". Section still shown with empty guidance.`,
      );
    }
  }, [programmes.length, retreatId]);

  const focusProgrammeSection = () => {
    document
      .getElementById("choose-your-programme")
      ?.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  const durationLabel =
    selected && durationNights && selected.supportedDurations.includes(durationNights)
      ? `${durationNights}-night `
      : "";

  return (
    <section
      id="whats-included"
      className="relative z-0 mt-14 pt-12 border-t border-sand-200"
      aria-labelledby="listing-c8-heading"
    >
      <h2
        id="listing-c8-heading"
        className="font-display text-2xl md:text-3xl font-semibold text-sage-800 text-balance"
      >
        What’s included in your programme
      </h2>
      <p className="mt-2 text-sm md:text-base text-sage-600 max-w-2xl">
        A clear breakdown of what is covered — and what may cost extra.
      </p>

      {!effectiveProgrammeId ? (
        <div className="mt-8 max-w-xl">
          <p className="text-sm md:text-base text-sage-700 leading-relaxed">
            Programme inclusions vary by programme. Choose a programme above to see the exact
            details.
          </p>
          <button
            type="button"
            onClick={focusProgrammeSection}
            className="mt-5 rounded-xl bg-teal-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-teal-500 transition"
          >
            Choose a programme
          </button>
        </div>
      ) : (
        <>
          <p className="mt-6 text-sm font-medium text-sage-800">
            Showing for{" "}
            <span className="text-teal-800">
              {durationLabel}
              {selected?.programmeName ?? "selected programme"}
            </span>
          </p>

          {!inclusionSet ||
          (inclusionSet.inclusions.length === 0 && inclusionSet.exclusions.length === 0) ? (
            <p className="mt-6 text-sm text-sage-600 max-w-xl leading-relaxed">
              To be confirmed with the retreat. Exact inclusions for this programme will appear
              here once verified with the partner.
            </p>
          ) : (
            <div className="mt-8 grid gap-10 md:grid-cols-2 md:gap-12">
              <InclusionColumn items={inclusionSet.inclusions} />
              <ExclusionColumn items={inclusionSet.exclusions} />
            </div>
          )}
        </>
      )}

      <p className="mt-10 max-w-2xl text-xs md:text-sm text-sage-500 leading-relaxed">
        Inclusions can vary by programme, duration and individual assessment. The final booking
        summary will show exactly what is included before payment.
      </p>
    </section>
  );
}

function InclusionColumn({ items }: { items: ProgrammeInclusionItem[] }) {
  const groups = groupInclusions(items);
  const showGroupLabels = items.length > 4 && groups.length > 1;

  return (
    <div>
      <h3 className="font-display text-lg font-semibold text-sage-800">Included</h3>
      {items.length === 0 ? (
        <p className="mt-4 text-sm text-sage-600">To be confirmed with the retreat</p>
      ) : (
        <div className="mt-5 space-y-6">
          {groups.map(({ group, items: groupItems }) => (
            <div key={group}>
              {showGroupLabels && (
                <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500 mb-3">
                  {INCLUSION_GROUP_LABELS[group]}
                </h4>
              )}
              <ul className="space-y-4">
                {groupItems.map((item) => {
                  const condition = conditionLabel(item.condition);
                  return (
                    <li key={item.id} className="flex gap-3">
                      <span
                        className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-sand-100 text-teal-700"
                        aria-hidden
                      >
                        <Check className="h-3 w-3" strokeWidth={2.5} />
                      </span>
                      <div className="min-w-0">
                        <p className="text-sm font-medium text-sage-800">{item.title}</p>
                        {item.description && (
                          <p className="mt-0.5 text-sm text-sage-600 leading-relaxed">
                            {item.description}
                          </p>
                        )}
                        {condition && (
                          <p className="mt-1 text-xs font-medium text-sage-500">{condition}</p>
                        )}
                      </div>
                    </li>
                  );
                })}
              </ul>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function ExclusionColumn({ items }: { items: ProgrammeExclusionItem[] }) {
  return (
    <div>
      <h3 className="font-display text-lg font-semibold text-sage-800">
        Not included / Additional charges
      </h3>
      {items.length === 0 ? (
        <p className="mt-4 text-sm text-sage-600">To be confirmed with the retreat</p>
      ) : (
        <ul className="mt-5 space-y-4">
          {items.map((item) => (
            <li key={item.id} className="flex gap-3">
              <span
                className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-sand-100 text-sage-500"
                aria-hidden
              >
                <Minus className="h-3 w-3" strokeWidth={2.5} />
              </span>
              <div className="min-w-0">
                <p className="text-sm font-medium text-sage-800">{item.title}</p>
                {item.description && (
                  <p className="mt-0.5 text-sm text-sage-600 leading-relaxed">
                    {item.description}
                  </p>
                )}
                {item.additionalCostNote && (
                  <p className="mt-1 text-xs font-medium text-sage-500">
                    {item.additionalCostNote}
                  </p>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
