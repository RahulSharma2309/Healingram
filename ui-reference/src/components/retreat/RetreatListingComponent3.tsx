import { useEffect, useId, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Check, MessageCircle, X } from "lucide-react";
import {
  formatDurationLabel,
  formatProgrammeFromPrice,
  getListingProgrammes,
  occupancyPriceLines,
  recommendProgrammeId,
  type ProgrammeListingRecord,
} from "../../data/programmeCatalog";
import { LAUNCH_RETREATS } from "../../data/launchSupply";
import { useListingPlan } from "../../lib/listingPlanContext";
import type { FindMyMatchListingState } from "./RetreatListingComponent1";

type Props = {
  retreatId: string;
  findMyMatch?: FindMyMatchListingState | null;
};

function preferenceTokens(match: FindMyMatchListingState | null | undefined): string[] {
  if (!match) return [];
  return [...(match.youToldUs ?? []), ...(match.matches ?? [])]
    .join(" ")
    .toLowerCase()
    .split(/[^a-z0-9+]+/)
    .filter(Boolean);
}

export function RetreatListingComponent3({ retreatId, findMyMatch }: Props) {
  const programmes = useMemo(() => getListingProgrammes(retreatId), [retreatId]);
  const { programmeId, durationNights, selectProgramme, setDurationNights, startAvailabilityFlow } =
    useListingPlan();
  const [detailId, setDetailId] = useState<string | null>(null);
  const [detailDuration, setDetailDuration] = useState<number | null>(null);

  const tokens = preferenceTokens(findMyMatch);
  const recommendedId = recommendProgrammeId(programmes, tokens);
  const detail = programmes.find((p) => p.programmeId === detailId) ?? null;

  useEffect(() => {
    if (!detail) return;
    const initial =
      durationNights && detail.supportedDurations.includes(durationNights)
        ? durationNights
        : detail.supportedDurations.length === 1
          ? detail.supportedDurations[0]
          : detailDuration && detail.supportedDurations.includes(detailDuration)
            ? detailDuration
            : detail.supportedDurations[0] ?? null;
    setDetailDuration(initial);
  }, [detailId]); // eslint-disable-line react-hooks/exhaustive-deps -- reset when opening a programme

  useEffect(() => {
    if (programmes.length === 0 && import.meta.env.DEV) {
      console.warn(
        `[Healingram] Component 3: no programme records for retreat "${retreatId}". Section omitted — do not invent programmes.`,
      );
    }
  }, [programmes.length, retreatId]);

  if (programmes.length === 0) return null;

  const openDetail = (id: string) => {
    const p = programmes.find((x) => x.programmeId === id);
    setDetailId(id);
    if (!p) return;
    const nights =
      programmeId === id && durationNights && p.supportedDurations.includes(durationNights)
        ? durationNights
        : p.supportedDurations.length === 1
          ? p.supportedDurations[0]
          : p.supportedDurations[0] ?? null;
    setDetailDuration(nights);
  };

  const onCheckAvailabilityFromDetail = () => {
    if (!detail || detailDuration == null) return;
    const pid = detail.programmeId;
    const nights = detailDuration;
    selectProgramme(pid, nights);
    setDetailId(null);
    // Same canonical availability workflow as Plan your stay — move forward, do not scroll back
    requestAnimationFrame(() =>
      startAvailabilityFlow({ programmeId: pid, durationNights: nights }),
    );
  };

  return (
    <section
      id="choose-your-programme"
      className="relative z-0 mt-14 block pt-12 border-t border-sand-200 opacity-100"
      aria-labelledby="listing-c3-heading"
    >
      <h2
        id="listing-c3-heading"
        className="font-display text-2xl md:text-3xl font-semibold text-sage-800 text-balance"
      >
        Choose your programme
      </h2>
      <p className="mt-2 text-sm md:text-base text-sage-600 max-w-2xl">
        Compare the retreat’s programmes, durations and inclusions before checking availability.
      </p>

      {programmes.length >= 2 && (
        <p className="mt-4 text-xs text-sage-500">
          Compare by duration, best-for focus and starting price — then open a programme for full
          detail.
        </p>
      )}

      <ul
        className={`mt-8 grid gap-5 ${
          programmes.length === 1 ? "max-w-xl" : "sm:grid-cols-2 xl:grid-cols-3"
        }`}
      >
        {programmes.map((p) => {
          const selected = programmeId === p.programmeId;
          const recommended = recommendedId === p.programmeId;
          const priceLabel = formatProgrammeFromPrice(p);
          const inclusionsPreview = p.inclusions.slice(0, 5);

          return (
            <li
              key={p.programmeId}
              className={`flex flex-col rounded-2xl border bg-white p-5 md:p-6 ${
                selected ? "border-teal-600/50 ring-1 ring-teal-600/20" : "border-sand-200"
              }`}
            >
              {recommended && (
                <p className="mb-3 text-[11px] font-semibold uppercase tracking-wide text-teal-700">
                  Recommended for your preferences
                </p>
              )}
              <h3 className="font-display text-xl font-semibold text-sage-800 text-balance">
                {p.programmeName}
              </h3>
              <p className="mt-1.5 text-sm font-medium text-sage-700">
                {formatDurationLabel(p.supportedDurations)}
              </p>
              <p className="mt-3 text-sm text-sage-600 leading-relaxed">{p.shortDescription}</p>

              {p.bestFor.length > 0 && (
                <div className="mt-4">
                  <p className="text-[11px] font-semibold uppercase tracking-wide text-sage-500 mb-2">
                    Best for
                  </p>
                  <div className="flex flex-wrap gap-1.5">
                    {p.bestFor.map((tag) => (
                      <span
                        key={tag}
                        className="px-2 py-0.5 rounded-md bg-sand-100 text-sage-700 text-xs font-medium"
                      >
                        {tag}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              {inclusionsPreview.length > 0 && (
                <div className="mt-4">
                  <p className="text-[11px] font-semibold uppercase tracking-wide text-sage-500 mb-2">
                    Includes
                  </p>
                  <ul className="space-y-1">
                    {inclusionsPreview.map((item) => (
                      <li key={item} className="flex gap-2 text-sm text-sage-700">
                        <Check className="w-3.5 h-3.5 text-teal-600 shrink-0 mt-0.5" />
                        {item}
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              <div className="mt-auto pt-5">
                <p className="text-lg font-semibold text-sage-800">{priceLabel}</p>
                {p.priceStatus === "VERIFIED" && (
                  <p className="mt-1 text-xs text-sage-500">
                    Final price depends on duration, occupancy and guests.
                  </p>
                )}
                <button
                  type="button"
                  onClick={() => openDetail(p.programmeId)}
                  className="mt-4 w-full rounded-xl border border-sand-200 py-2.5 text-sm font-semibold text-sage-800 hover:bg-sand-50 transition"
                >
                  View programme
                </button>
              </div>
            </li>
          );
        })}
      </ul>

      {detail && (
        <ProgrammeDetailDrawer
          retreatId={retreatId}
          programme={detail}
          duration={detailDuration}
          onDurationChange={(n) => {
            setDetailDuration(n);
            if (programmeId === detail.programmeId) {
              setDurationNights(n);
            }
          }}
          onClose={() => setDetailId(null)}
          onCheckAvailability={onCheckAvailabilityFromDetail}
        />
      )}
    </section>
  );
}

function ProgrammeDetailDrawer({
  retreatId,
  programme,
  duration,
  onDurationChange,
  onClose,
  onCheckAvailability,
}: {
  retreatId: string;
  programme: ProgrammeListingRecord;
  duration: number | null;
  onDurationChange: (n: number) => void;
  onClose: () => void;
  onCheckAvailability: () => void;
}) {
  const titleId = useId();
  const priceLabel = formatProgrammeFromPrice(programme);
  const lines = occupancyPriceLines(programme, duration);
  const multi = programme.supportedDurations.length > 1;
  const retreatName =
    LAUNCH_RETREATS.find((r) => r.id === retreatId)?.name ?? retreatId;

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);

  return (
    <div className="fixed inset-0 z-50 flex justify-end">
      <button
        type="button"
        className="absolute inset-0 bg-black/40"
        aria-label="Close programme details"
        onClick={onClose}
      />
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="relative h-full w-full max-w-lg overflow-y-auto bg-white shadow-xl border-l border-sand-200"
      >
        <div className="sticky top-0 z-10 flex items-start justify-between gap-3 border-b border-sand-100 bg-white px-5 py-4">
          <div>
            <h3 id={titleId} className="font-display text-xl font-semibold text-sage-800 text-balance">
              {programme.programmeName}
            </h3>
            <p className="mt-1 text-sm text-sage-600">
              {formatDurationLabel(programme.supportedDurations)}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 rounded-lg text-sage-500 hover:bg-sand-50"
            aria-label="Close"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="px-5 py-6 space-y-8">
          <section>
            <h4 className="font-display text-lg font-semibold text-sage-800">About this programme</h4>
            <p className="mt-2 text-sm text-sage-600 leading-relaxed">{programme.longDescription}</p>
          </section>

          {programme.bestFor.length > 0 && (
            <section>
              <h4 className="font-display text-lg font-semibold text-sage-800">Who it may suit</h4>
              <ul className="mt-3 flex flex-wrap gap-2">
                {programme.bestFor.map((tag) => (
                  <li
                    key={tag}
                    className="px-2.5 py-1 rounded-md bg-sand-100 text-sage-700 text-sm font-medium"
                  >
                    {tag}
                  </li>
                ))}
              </ul>
            </section>
          )}

          <section>
            <h4 className="font-display text-lg font-semibold text-sage-800">Duration</h4>
            {multi ? (
              <>
                <p className="mt-2 text-xs font-semibold uppercase tracking-wide text-teal-700">
                  Choose duration
                </p>
                <div className="mt-2 flex flex-wrap gap-2">
                  {programme.supportedDurations.map((n) => (
                    <button
                      key={n}
                      type="button"
                      onClick={() => onDurationChange(n)}
                      className={`rounded-xl px-3 py-2 text-sm font-medium border transition ${
                        duration === n
                          ? "border-teal-600 bg-teal-50 text-teal-800"
                          : "border-sand-200 text-sage-700 hover:bg-sand-50"
                      }`}
                    >
                      {n} nights
                    </button>
                  ))}
                </div>
              </>
            ) : (
              <p className="mt-2 text-sm text-sage-700">
                {programme.supportedDurations[0]} nights
              </p>
            )}
            <p className="mt-2 text-xs text-sage-500">
              Minimum stay: {programme.minimumStay} nights
            </p>
          </section>

          <section>
            <h4 className="font-display text-lg font-semibold text-sage-800">Pricing</h4>
            <p className="mt-2 text-xl font-semibold text-sage-800">{priceLabel}</p>
            {programme.priceStatus === "VERIFIED" && (
              <p className="mt-1 text-xs text-sage-500">
                Final price depends on duration, occupancy and number of guests.
              </p>
            )}
            {lines.length > 0 && (
              <ul className="mt-3 space-y-1.5">
                {lines.map((line) => (
                  <li key={line.label} className="text-sm text-sage-700">
                    {line.label}
                  </li>
                ))}
              </ul>
            )}
          </section>

          {programme.inclusions.length > 0 && (
            <section>
              <h4 className="font-display text-lg font-semibold text-sage-800">What’s included</h4>
              <ul className="mt-3 space-y-2">
                {programme.inclusions.map((item) => (
                  <li key={item} className="flex gap-2 text-sm text-sage-700">
                    <Check className="w-4 h-4 text-teal-600 shrink-0 mt-0.5" />
                    {item}
                  </li>
                ))}
              </ul>
            </section>
          )}

          {programme.exclusions.length > 0 && (
            <section>
              <h4 className="font-display text-lg font-semibold text-sage-800">What’s not included</h4>
              <ul className="mt-3 space-y-2">
                {programme.exclusions.map((item) => (
                  <li key={item} className="text-sm text-sage-700">
                    · {item}
                  </li>
                ))}
              </ul>
            </section>
          )}

          {programme.treatmentsIncluded.length > 0 && (
            <section>
              <h4 className="font-display text-lg font-semibold text-sage-800">Treatments</h4>
              <ul className="mt-3 space-y-1">
                {programme.treatmentsIncluded.map((t) => (
                  <li key={t} className="text-sm text-sage-700">
                    · {t}
                  </li>
                ))}
              </ul>
            </section>
          )}

          {programme.activitiesIncluded.length > 0 && (
            <section>
              <h4 className="font-display text-lg font-semibold text-sage-800">Activities</h4>
              <ul className="mt-3 space-y-1">
                {programme.activitiesIncluded.map((t) => (
                  <li key={t} className="text-sm text-sage-700">
                    · {t}
                  </li>
                ))}
              </ul>
            </section>
          )}

          {programme.mealPlan && (
            <section>
              <h4 className="font-display text-lg font-semibold text-sage-800">Meals</h4>
              <p className="mt-2 text-sm text-sage-600">{programme.mealPlan}</p>
            </section>
          )}

          {programme.importantNotes.length > 0 && (
            <section>
              <h4 className="font-display text-lg font-semibold text-sage-800">Important to know</h4>
              <ul className="mt-3 space-y-2">
                {programme.importantNotes.map((note) => (
                  <li key={note} className="text-sm text-sage-600 leading-relaxed">
                    · {note}
                  </li>
                ))}
              </ul>
            </section>
          )}

          <div className="flex flex-col gap-3 pt-2 pb-6">
            <button
              type="button"
              disabled={duration == null}
              onClick={onCheckAvailability}
              className="w-full rounded-xl bg-teal-600 py-3.5 text-sm font-semibold text-white hover:bg-teal-500 disabled:opacity-50"
            >
              Check Availability
            </button>
            <Link
              to="/contact"
              state={{
                source: "listing" as const,
                retreatId,
                retreatName,
                programmeId: programme.programmeId,
                programmeName: programme.programmeName,
                duration,
              }}
              className="inline-flex items-center justify-center gap-2 rounded-xl border border-sand-200 py-3 text-sm font-semibold text-sage-800 hover:bg-sand-50"
            >
              <MessageCircle className="w-4 h-4" />
              Talk to an Expert
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
