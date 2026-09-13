import { useEffect, useMemo } from "react";
import {
  Flower2,
  Moon,
  Salad,
  Sparkles,
  Sun,
  Trees,
  Waves,
  type LucideIcon,
} from "lucide-react";
import {
  getTypicalDaySchedule,
  hasTypicalDayData,
  type TypicalDayIconKey,
} from "../../data/retreatTypicalDay";
import { getListingProgramme } from "../../data/programmeCatalog";
import { getRetreatListingView } from "../../data/launchListing";
import { useListingPlan } from "../../lib/listingPlanContext";

type Props = {
  retreatId: string;
};

const ICONS: Record<TypicalDayIconKey, LucideIcon> = {
  sunrise: Sun,
  yoga: Flower2,
  meal: Salad,
  consultation: Sparkles,
  treatment: Waves,
  rest: Trees,
  meditation: Moon,
  evening: Moon,
};

export function RetreatListingComponent4({ retreatId }: Props) {
  const { programmeId } = useListingPlan();
  const selectedProgramme = programmeId
    ? getListingProgramme(retreatId, programmeId)
    : undefined;

  const schedule = useMemo(
    () => getTypicalDaySchedule(retreatId, programmeId || null),
    [retreatId, programmeId],
  );

  const listing = useMemo(() => getRetreatListingView(retreatId), [retreatId]);
  const supportingImage = useMemo(() => {
    if (!listing || !schedule?.imageCategory) return null;
    return (
      listing.media.find((m) => m.category === schedule.imageCategory) ??
      listing.media.find((m) => m.category === "property") ??
      null
    );
  }, [listing, schedule?.imageCategory]);

  useEffect(() => {
    if (!hasTypicalDayData(retreatId) && import.meta.env.DEV) {
      console.warn(
        `[Healingram] Component 4: no verified typicalDay data for retreat "${retreatId}". Section omitted — do not invent schedules.`,
      );
    }
  }, [retreatId]);

  if (!schedule || schedule.items.length === 0) return null;

  /** Programme-specific schedule when selected; otherwise retreat-level label */
  const displayLabel =
    programmeId && selectedProgramme && schedule.programmeId === programmeId
      ? `Typical day for: ${selectedProgramme.programmeName}`
      : "A typical day at this retreat";

  return (
    <section
      id="a-day-at-the-retreat"
      className="relative z-0 mt-14 pt-12 border-t border-sand-200"
      aria-labelledby="listing-c4-heading"
    >
      <div className="grid lg:grid-cols-5 gap-10 lg:gap-14 items-start">
        <div className="lg:col-span-3">
          <h2
            id="listing-c4-heading"
            className="font-display text-2xl md:text-3xl font-semibold text-sage-800 text-balance"
          >
            What a day here may look like
          </h2>
          <p className="mt-2 text-sm md:text-base text-sage-600 max-w-2xl">
            A glimpse of the rhythm, structure and downtime you can expect during your stay.
          </p>
          <p className="mt-4 text-xs font-semibold uppercase tracking-wide text-teal-700/90">
            {displayLabel}
          </p>

          <ol className="mt-8 relative">
            <div
              className="absolute left-[15px] top-2 bottom-2 w-px bg-sand-200"
              aria-hidden
            />
            {schedule.items.map((item, index) => {
              const Icon = ICONS[item.iconKey] ?? Sun;
              return (
                <li key={`${item.time}-${item.title}-${index}`} className="relative flex gap-4 pb-8 last:pb-0">
                  <div className="relative z-[1] flex h-8 w-8 shrink-0 items-center justify-center rounded-full border border-sand-200 bg-sand-50 text-sage-700">
                    <Icon className="w-3.5 h-3.5" aria-hidden />
                  </div>
                  <div className="min-w-0 pt-0.5">
                    <p className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                      {item.time}
                    </p>
                    <h3 className="mt-1 font-display text-lg font-semibold text-sage-800 text-balance">
                      {item.title}
                    </h3>
                    {item.description ? (
                      <p className="mt-1.5 text-sm text-sage-600 leading-relaxed max-w-xl">
                        {item.description}
                      </p>
                    ) : null}
                  </div>
                </li>
              );
            })}
          </ol>

          <p className="mt-8 text-sm text-sage-500 leading-relaxed max-w-xl border-t border-sand-200 pt-5">
            Your exact schedule may vary depending on your programme, consultations and individual
            assessment.
          </p>
        </div>

        {supportingImage ? (
          <div className="hidden lg:block lg:col-span-2">
            <div className="sticky top-28 overflow-hidden rounded-2xl aspect-[4/5] bg-sand-100">
              <img
                src={supportingImage.src}
                alt={supportingImage.alt}
                className="h-full w-full object-cover"
              />
            </div>
          </div>
        ) : null}
      </div>
    </section>
  );
}
