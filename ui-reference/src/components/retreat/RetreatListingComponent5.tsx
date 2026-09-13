import { useEffect, useId, useMemo, useState } from "react";
import { BadgeCheck, Play, X } from "lucide-react";
import {
  getListingProgramme,
  getListingProgrammes,
} from "../../data/programmeCatalog";
import {
  getRetreatExperts,
  groupRetreatExperts,
  hasRetreatExperts,
  type RetreatExpert,
} from "../../data/retreatExperts";
import { useListingPlan } from "../../lib/listingPlanContext";

type Props = {
  retreatId: string;
};

export function RetreatListingComponent5({ retreatId }: Props) {
  const experts = useMemo(() => getRetreatExperts(retreatId), [retreatId]);
  const groups = useMemo(() => groupRetreatExperts(experts), [experts]);
  const showGroupLabels = groups.length > 1;
  const [detailId, setDetailId] = useState<string | null>(null);
  const detail = experts.find((e) => e.expertId === detailId) ?? null;
  const { selectProgramme } = useListingPlan();

  useEffect(() => {
    if (!hasRetreatExperts(retreatId) && import.meta.env.DEV) {
      console.warn(
        `[Healingram] Component 5: no verified expert profiles for retreat "${retreatId}". Section omitted — do not invent practitioners.`,
      );
    }
  }, [retreatId]);

  if (experts.length === 0) return null;

  const onProgrammeClick = (programmeId: string) => {
    const record = getListingProgramme(retreatId, programmeId);
    if (!record) return;
    const nights =
      record.supportedDurations.length === 1 ? record.supportedDurations[0] : null;
    selectProgramme(programmeId, nights);
    setDetailId(null);
    requestAnimationFrame(() => {
      document
        .getElementById("choose-your-programme")
        ?.scrollIntoView({ behavior: "smooth", block: "start" });
    });
  };

  return (
    <section
      id="meet-the-experts"
      className="relative z-0 mt-14 pt-12 border-t border-sand-200"
      aria-labelledby="listing-c5-heading"
    >
      <h2
        id="listing-c5-heading"
        className="font-display text-2xl md:text-3xl font-semibold text-sage-800 text-balance"
      >
        Meet the people guiding your stay
      </h2>
      <p className="mt-2 text-sm md:text-base text-sage-600 max-w-2xl">
        Get to know the doctors and practitioners behind the retreat experience.
      </p>

      <div className="mt-10 space-y-12">
        {groups.map((group) => (
          <div key={group.group}>
            {showGroupLabels && (
              <h3 className="font-display text-lg font-semibold text-sage-700 mb-5">
                {group.label}
              </h3>
            )}
            <ul className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
              {group.experts.map((expert) => (
                <li key={expert.expertId}>
                  <ExpertCard
                    expert={expert}
                    retreatId={retreatId}
                    onViewProfile={() => setDetailId(expert.expertId)}
                    onProgrammeClick={onProgrammeClick}
                  />
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>

      {detail && (
        <ExpertProfileDrawer
          expert={detail}
          retreatId={retreatId}
          onClose={() => setDetailId(null)}
          onProgrammeClick={onProgrammeClick}
        />
      )}
    </section>
  );
}

function ExpertCard({
  expert,
  retreatId,
  onViewProfile,
  onProgrammeClick,
}: {
  expert: RetreatExpert;
  retreatId: string;
  onViewProfile: () => void;
  onProgrammeClick: (programmeId: string) => void;
}) {
  const programmeLabels = programmeLabelsFor(expert, retreatId);

  return (
    <article className="flex h-full flex-col rounded-2xl border border-sand-200 bg-white overflow-hidden">
      <div className="relative aspect-[4/5] max-h-72 bg-sand-100 overflow-hidden">
        <img
          src={expert.image}
          alt={
            expert.imageTemporary
              ? `${expert.name} — temporary portrait placeholder`
              : expert.name
          }
          className="h-full w-full object-cover"
        />
        {expert.verified && (
          <span className="absolute top-3 left-3 inline-flex items-center gap-1 rounded-md bg-white/95 px-2 py-1 text-[11px] font-semibold text-sage-700 border border-sand-200">
            <BadgeCheck className="w-3.5 h-3.5 text-teal-600" />
            Verified profile
          </span>
        )}
      </div>

      <div className="flex flex-1 flex-col p-5 md:p-6">
        <h3 className="font-display text-xl font-semibold text-sage-800 text-balance">
          {expert.name}
        </h3>
        <p className="mt-1 text-sm font-medium text-sage-700">{expert.role}</p>
        {expert.qualifications.length > 0 && (
          <p className="mt-1.5 text-sm text-sage-500">
            {expert.qualifications.join(" · ")}
          </p>
        )}
        {expert.yearsExperience != null && (
          <p className="mt-1 text-xs text-sage-500">
            {expert.yearsExperience}+ years of experience
          </p>
        )}

        {expert.specialties.length > 0 && (
          <div className="mt-4">
            <p className="text-[11px] font-semibold uppercase tracking-wide text-sage-500 mb-2">
              Focus
            </p>
            <p className="text-sm text-sage-700 leading-relaxed">
              {expert.specialties.join(" · ")}
            </p>
          </div>
        )}

        {expert.languages.length > 0 && (
          <div className="mt-3">
            <p className="text-[11px] font-semibold uppercase tracking-wide text-sage-500 mb-1">
              Languages
            </p>
            <p className="text-sm text-sage-700">{expert.languages.join(" · ")}</p>
          </div>
        )}

        <p className="mt-4 text-sm text-sage-600 leading-relaxed line-clamp-3">
          {expert.shortBio}
        </p>

        {programmeLabels.length > 0 && (
          <div className="mt-4">
            <p className="text-[11px] font-semibold uppercase tracking-wide text-sage-500 mb-2">
              Guides programmes
            </p>
            <div className="flex flex-wrap gap-1.5">
              {programmeLabels.map(({ id, name }) => (
                <button
                  key={id}
                  type="button"
                  onClick={() => onProgrammeClick(id)}
                  className="px-2.5 py-1 rounded-md bg-sand-100 text-sage-700 text-xs font-medium hover:bg-sand-200 transition"
                >
                  {name}
                </button>
              ))}
            </div>
          </div>
        )}

        <div className="mt-auto pt-5 space-y-2">
          <button
            type="button"
            onClick={onViewProfile}
            className="w-full rounded-xl border border-sand-200 py-2.5 text-sm font-semibold text-sage-800 hover:bg-sand-50 transition"
          >
            View profile
          </button>
          {expert.videoUrl && (
            <a
              href={expert.videoUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex w-full items-center justify-center gap-2 rounded-xl py-2 text-sm font-medium text-teal-700 hover:text-teal-800"
            >
              <Play className="w-3.5 h-3.5" />
              Watch introduction
            </a>
          )}
        </div>
      </div>
    </article>
  );
}

function ExpertProfileDrawer({
  expert,
  retreatId,
  onClose,
  onProgrammeClick,
}: {
  expert: RetreatExpert;
  retreatId: string;
  onClose: () => void;
  onProgrammeClick: (programmeId: string) => void;
}) {
  const titleId = useId();
  const programmeLabels = programmeLabelsFor(expert, retreatId);

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
        aria-label="Close expert profile"
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
            <h3
              id={titleId}
              className="font-display text-xl font-semibold text-sage-800 text-balance"
            >
              {expert.name}
            </h3>
            <p className="mt-1 text-sm text-sage-600">{expert.role}</p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-2 text-sage-500 hover:bg-sand-100"
            aria-label="Close"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="px-5 py-6 space-y-6">
          <div className="relative overflow-hidden rounded-2xl bg-sand-100 aspect-[4/5] max-h-80">
            <img
              src={expert.image}
              alt={
                expert.imageTemporary
                  ? `${expert.name} — temporary portrait placeholder`
                  : expert.name
              }
              className="h-full w-full object-cover"
            />
          </div>

          {expert.verified && (
            <p className="inline-flex items-center gap-1.5 text-xs font-semibold text-sage-700">
              <BadgeCheck className="w-4 h-4 text-teal-600" />
              Verified profile
            </p>
          )}

          {expert.qualifications.length > 0 && (
            <section>
              <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                Qualification
              </h4>
              <p className="mt-1.5 text-sm text-sage-800">
                {expert.qualifications.join(" · ")}
              </p>
            </section>
          )}

          {expert.yearsExperience != null && (
            <section>
              <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                Experience
              </h4>
              <p className="mt-1.5 text-sm text-sage-800">
                {expert.yearsExperience}+ years
              </p>
            </section>
          )}

          {expert.specialties.length > 0 && (
            <section>
              <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                Specialties
              </h4>
              <ul className="mt-2 flex flex-wrap gap-1.5">
                {expert.specialties.map((s) => (
                  <li
                    key={s}
                    className="px-2.5 py-1 rounded-md bg-sand-100 text-sage-700 text-xs font-medium"
                  >
                    {s}
                  </li>
                ))}
              </ul>
            </section>
          )}

          {expert.languages.length > 0 && (
            <section>
              <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                Languages
              </h4>
              <p className="mt-1.5 text-sm text-sage-800">
                {expert.languages.join(" · ")}
              </p>
            </section>
          )}

          <section>
            <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
              About
            </h4>
            <p className="mt-2 text-sm text-sage-700 leading-relaxed">{expert.longBio}</p>
          </section>

          {programmeLabels.length > 0 && (
            <section>
              <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500 mb-2">
                Guides programmes
              </h4>
              <p className="text-xs text-sage-500 mb-2">
                Programme association for guidance — not a guarantee of personal treatment for
                every guest.
              </p>
              <div className="flex flex-wrap gap-1.5">
                {programmeLabels.map(({ id, name }) => (
                  <button
                    key={id}
                    type="button"
                    onClick={() => onProgrammeClick(id)}
                    className="px-2.5 py-1.5 rounded-md border border-sand-200 text-sage-800 text-xs font-semibold hover:bg-sand-50"
                  >
                    {name}
                  </button>
                ))}
              </div>
            </section>
          )}

          {expert.videoUrl && (
            <section>
              <h4 className="font-display text-lg font-semibold text-sage-800 mb-3">
                Meet {expert.name}
              </h4>
              <a
                href={expert.videoUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="relative block overflow-hidden rounded-2xl bg-sage-800 aspect-video group"
              >
                <img
                  src={expert.image}
                  alt=""
                  className="h-full w-full object-cover opacity-70 group-hover:opacity-60 transition"
                />
                <span className="absolute inset-0 flex items-center justify-center">
                  <span className="inline-flex items-center gap-2 rounded-full bg-white px-4 py-2 text-sm font-semibold text-sage-800 shadow">
                    <Play className="w-4 h-4" />
                    Watch introduction
                  </span>
                </span>
              </a>
            </section>
          )}

          {expert.imageTemporary && (
            <p className="text-xs text-sage-500">
              Portrait is a temporary placeholder until partner media is supplied.
            </p>
          )}
        </div>
      </div>
    </div>
  );
}

function programmeLabelsFor(
  expert: RetreatExpert,
  retreatId: string,
): { id: string; name: string }[] {
  const available = new Set(
    getListingProgrammes(retreatId).map((p) => p.programmeId),
  );
  return expert.programmeIds
    .filter((id) => available.has(id))
    .map((id) => {
      const p = getListingProgramme(retreatId, id);
      return p ? { id, name: p.programmeName } : null;
    })
    .filter((x): x is { id: string; name: string } => x != null);
}
