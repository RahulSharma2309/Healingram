import { useEffect, useId, useMemo, useState } from "react";
import { Play, X } from "lucide-react";
import {
  filterTestimonials,
  getAvailableTestimonialFilters,
  getRetreatTestimonials,
  hasRetreatTestimonials,
  TESTIMONIAL_FILTER_LABELS,
  TRAVELLER_TYPE_LABELS,
  type RetreatTestimonial,
  type TestimonialFilterTag,
} from "../../data/retreatTestimonials";

type Props = {
  retreatId: string;
};

export function RetreatListingComponent6({ retreatId }: Props) {
  const all = useMemo(() => getRetreatTestimonials(retreatId), [retreatId]);
  const filters = useMemo(() => getAvailableTestimonialFilters(all), [all]);
  const [activeFilter, setActiveFilter] = useState<TestimonialFilterTag | null>(null);
  const [detailId, setDetailId] = useState<string | null>(null);

  const visible = useMemo(
    () => filterTestimonials(all, activeFilter),
    [all, activeFilter],
  );
  const detail = all.find((t) => t.testimonialId === detailId) ?? null;

  useEffect(() => {
    if (!hasRetreatTestimonials(retreatId) && import.meta.env.DEV) {
      console.warn(
        `[Healingram] Component 6: no verified/consented testimonials for retreat "${retreatId}". Section omitted — do not invent guest stories.`,
      );
    }
  }, [retreatId]);

  useEffect(() => {
    if (activeFilter && !filters.includes(activeFilter)) {
      setActiveFilter(null);
    }
  }, [activeFilter, filters]);

  if (all.length === 0) {
    if (import.meta.env.DEV) {
      return (
        <section
          id="guest-stories"
          className="relative z-0 mt-14 pt-12 border-t border-sand-200"
        >
          <p className="text-sm text-sage-500">No verified guest stories added yet.</p>
        </section>
      );
    }
    return null;
  }

  return (
    <section
      id="guest-stories"
      className="relative z-0 mt-14 pt-12 border-t border-sand-200"
      aria-labelledby="listing-c6-heading"
    >
      <h2
        id="listing-c6-heading"
        className="font-display text-2xl md:text-3xl font-semibold text-sage-800 text-balance"
      >
        Stories from people who stayed here
      </h2>
      <p className="mt-2 text-sm md:text-base text-sage-600 max-w-2xl">
        Real experiences from guests who chose this retreat for their own wellness journey.
      </p>

      {filters.length > 0 && (
        <div className="mt-6 flex flex-wrap gap-2" role="group" aria-label="Filter stories">
          <button
            type="button"
            onClick={() => setActiveFilter(null)}
            className={`rounded-xl border px-3.5 py-2 text-sm transition ${
              activeFilter == null
                ? "border-teal-600/50 bg-teal-50 text-sage-800 ring-1 ring-teal-600/20"
                : "border-sand-200 bg-white text-sage-700 hover:bg-sand-50"
            }`}
          >
            All stories
          </button>
          {filters.map((tag) => {
            const on = activeFilter === tag;
            return (
              <button
                key={tag}
                type="button"
                onClick={() => setActiveFilter(on ? null : tag)}
                className={`rounded-xl border px-3.5 py-2 text-sm transition ${
                  on
                    ? "border-teal-600/50 bg-teal-50 text-sage-800 ring-1 ring-teal-600/20"
                    : "border-sand-200 bg-white text-sage-700 hover:bg-sand-50"
                }`}
              >
                {TESTIMONIAL_FILTER_LABELS[tag]}
              </button>
            );
          })}
        </div>
      )}

      {visible.length === 0 ? (
        <p className="mt-8 text-sm text-sage-500">No stories match this filter.</p>
      ) : (
        <ul className="mt-8 grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {visible.map((story) => (
            <li key={story.testimonialId}>
              <TestimonialCard
                story={story}
                onReadFull={() => setDetailId(story.testimonialId)}
              />
            </li>
          ))}
        </ul>
      )}

      {detail && (
        <StoryDrawer story={detail} onClose={() => setDetailId(null)} />
      )}
    </section>
  );
}

function TestimonialCard({
  story,
  onReadFull,
}: {
  story: RetreatTestimonial;
  onReadFull: () => void;
}) {
  const meta = `${story.stayDuration} ${story.programmeName} · ${TRAVELLER_TYPE_LABELS[story.travellerType]}`;

  return (
    <article className="flex h-full flex-col rounded-2xl border border-sand-200 bg-white p-5 md:p-6">
      <div className="flex items-start gap-3">
        {story.guestPhoto ? (
          <img
            src={story.guestPhoto}
            alt={
              story.guestPhotoTemporary
                ? `${story.guestName} — temporary portrait placeholder`
                : story.guestName
            }
            className="h-14 w-14 rounded-full object-cover bg-sand-100 shrink-0"
          />
        ) : (
          <div
            className="h-14 w-14 rounded-full bg-sand-100 shrink-0 flex items-center justify-center text-sage-600 font-display text-lg font-semibold"
            aria-hidden
          >
            {story.guestName.slice(0, 1)}
          </div>
        )}
        <div className="min-w-0">
          <h3 className="font-display text-xl font-semibold text-sage-800">
            {story.guestName}
          </h3>
          <p className="mt-0.5 text-sm text-sage-600 leading-snug">{meta}</p>
        </div>
      </div>

      <div className="mt-5">
        <p className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
          Why they came
        </p>
        <p className="mt-1.5 text-sm text-sage-700 leading-relaxed">{story.reasonForVisit}</p>
      </div>

      <div className="mt-5">
        <p className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
          Their experience
        </p>
        <blockquote className="mt-2 text-sm md:text-[15px] text-sage-800 leading-relaxed border-l-2 border-sand-200 pl-3">
          {story.quote}
        </blockquote>
      </div>

      {story.outcomeReflection && (
        <p className="mt-4 text-sm text-sage-600 leading-relaxed">{story.outcomeReflection}</p>
      )}

      <div className="mt-5">
        <p className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
          Programme
        </p>
        <p className="mt-1 text-sm font-medium text-sage-800">
          {story.programmeName} · {story.stayDuration}
        </p>
      </div>

      <div className="mt-auto pt-5 space-y-2">
        {story.longerStory && (
          <button
            type="button"
            onClick={onReadFull}
            className="w-full rounded-xl border border-sand-200 py-2.5 text-sm font-semibold text-sage-800 hover:bg-sand-50 transition"
          >
            Read full story
          </button>
        )}
        {story.videoUrl && (
          <a
            href={story.videoUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex w-full items-center justify-center gap-2 rounded-xl py-2 text-sm font-medium text-teal-700 hover:text-teal-800"
          >
            <Play className="w-3.5 h-3.5" />
            Watch {story.guestName}’s story
          </a>
        )}
      </div>
    </article>
  );
}

function StoryDrawer({
  story,
  onClose,
}: {
  story: RetreatTestimonial;
  onClose: () => void;
}) {
  const titleId = useId();

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
        aria-label="Close guest story"
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
            <h3 id={titleId} className="font-display text-xl font-semibold text-sage-800">
              {story.guestName}’s story
            </h3>
            <p className="mt-1 text-sm text-sage-600">
              {story.stayDuration} {story.programmeName} ·{" "}
              {TRAVELLER_TYPE_LABELS[story.travellerType]}
            </p>
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
          {story.guestPhoto && (
            <img
              src={story.guestPhoto}
              alt={
                story.guestPhotoTemporary
                  ? `${story.guestName} — temporary portrait placeholder`
                  : story.guestName
              }
              className="h-24 w-24 rounded-full object-cover bg-sand-100"
            />
          )}

          <section>
            <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
              Why they came
            </h4>
            <p className="mt-2 text-sm text-sage-700 leading-relaxed">{story.reasonForVisit}</p>
          </section>

          <section>
            <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
              Programme
            </h4>
            <p className="mt-2 text-sm font-medium text-sage-800">
              {story.programmeName} · {story.stayDuration}
            </p>
          </section>

          <section>
            <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
              Their experience
            </h4>
            <p className="mt-2 text-sm text-sage-800 leading-relaxed">
              {story.longerStory ?? story.quote}
            </p>
          </section>

          {story.outcomeReflection && (
            <section>
              <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                Reflection
              </h4>
              <p className="mt-2 text-sm text-sage-700 leading-relaxed">
                {story.outcomeReflection}
              </p>
            </section>
          )}

          {story.videoUrl && (
            <section>
              <h4 className="font-display text-lg font-semibold text-sage-800 mb-3">
                Watch {story.guestName}’s story
              </h4>
              <a
                href={story.videoUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="relative block overflow-hidden rounded-2xl bg-sage-800 aspect-video group"
              >
                {story.guestPhoto && (
                  <img
                    src={story.guestPhoto}
                    alt=""
                    className="h-full w-full object-cover opacity-70 group-hover:opacity-60 transition"
                  />
                )}
                <span className="absolute inset-0 flex items-center justify-center">
                  <span className="inline-flex items-center gap-2 rounded-full bg-white px-4 py-2 text-sm font-semibold text-sage-800 shadow">
                    <Play className="w-4 h-4" />
                    Play
                  </span>
                </span>
              </a>
            </section>
          )}
        </div>
      </div>
    </div>
  );
}
