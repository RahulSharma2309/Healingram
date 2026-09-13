import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, ArrowRight, Check, MapPin } from "lucide-react";
import {
  createMatchSession,
  fetchMatchOptions,
  matchesFromSession,
  type MatchQuestion,
} from "../../lib/api/matching";
import { regionLabel, type RankedMatch } from "../../lib/findMyMatch";
import { iconForKey } from "../../lib/icons";
import { applyAnswer, currentSelection, emptyMatchAnswers } from "../../lib/questionnaire";

type Phase = "intro" | "questions" | "results";

export function Questionnaire() {
  const [phase, setPhase] = useState<Phase>("intro");
  const [step, setStep] = useState(0);
  const [answers, setAnswers] = useState(emptyMatchAnswers());
  const [enterAnim, setEnterAnim] = useState(true);
  const [questions, setQuestions] = useState<MatchQuestion[]>([]);
  const [optionsError, setOptionsError] = useState(false);
  const [ranked, setRanked] = useState<{ exact: RankedMatch[]; closest: RankedMatch[] } | null>(null);
  const [matchBusy, setMatchBusy] = useState(false);
  const [matchError, setMatchError] = useState(false);

  useEffect(() => {
    let cancelled = false;
    fetchMatchOptions()
      .then((items) => {
        if (!cancelled) setQuestions(items);
      })
      .catch(() => {
        if (!cancelled) setOptionsError(true);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    setEnterAnim(false);
    const t = requestAnimationFrame(() => setEnterAnim(true));
    return () => cancelAnimationFrame(t);
  }, [step, phase]);

  useEffect(() => {
    if (phase !== "results") return;
    let cancelled = false;
    setMatchBusy(true);
    setMatchError(false);
    (async () => {
      try {
        const session = await createMatchSession(answers);
        const list = await matchesFromSession(session);
        if (!cancelled) setRanked({ exact: list, closest: [] });
      } catch {
        if (!cancelled) {
          setRanked(null);
          setMatchError(true);
        }
      } finally {
        if (!cancelled) setMatchBusy(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [phase, answers]);

  const question = questions[step];
  const selectedIds = question ? currentSelection(answers, question) : [];
  const canContinue = selectedIds.length > 0;

  const toggleOption = (id: string) => {
    if (!question) return;
    const multi = question.selectionMode !== "single";
    setAnswers((current) => applyAnswer(current, question.key, id, multi));
  };

  const goNext = () => {
    if (!canContinue) return;
    if (step < questions.length - 1) setStep((s) => s + 1);
    else setPhase("results");
  };

  const goBack = () => {
    if (step === 0) setPhase("intro");
    else setStep((s) => s - 1);
  };

  const restart = () => {
    setAnswers(emptyMatchAnswers());
    setStep(0);
    setPhase("intro");
  };

  if (phase === "intro") {
    return (
      <div className="relative min-h-[calc(100vh-8rem)] overflow-hidden">
        <div
          className="absolute inset-0 opacity-40"
          style={{
            backgroundImage:
              "radial-gradient(ellipse at 20% 0%, rgba(184,160,120,0.25), transparent 50%), radial-gradient(ellipse at 80% 100%, rgba(92,85,76,0.12), transparent 45%)",
          }}
        />
        <div className="relative max-w-lg mx-auto px-4 py-16 md:py-24 text-center">
          <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-teal-600 mb-5">
            Find My Match
          </p>
          <h1 className="font-display text-3xl md:text-4xl font-bold text-sage-800 tracking-tight mb-4 text-balance">
            Find the retreat that fits you
          </h1>
          <p className="text-sage-600 text-base md:text-lg leading-relaxed mb-3 text-balance">
            A few quick choices. We’ll narrow down the retreats that may suit your current needs,
            time and preferences.
          </p>
          <p className="text-sm text-sage-500 mb-10">Takes less than a minute</p>
          {optionsError ? (
            <p className="text-sm text-red-600 mb-6">
              Could not load questionnaire options. Start the gateway and API, then refresh.
            </p>
          ) : null}
          <button
            type="button"
            disabled={questions.length === 0}
            onClick={() => {
              setPhase("questions");
              setStep(0);
            }}
            className="inline-flex items-center justify-center gap-2 rounded-xl bg-teal-600 px-8 py-3.5 text-sm font-semibold text-white shadow-sm hover:bg-teal-500 transition disabled:opacity-50"
          >
            Start My Match
            <ArrowRight className="w-4 h-4" />
          </button>
        </div>
      </div>
    );
  }

  if (phase === "results" && matchError) {
    return (
      <div className="max-w-lg mx-auto px-4 py-24 text-center">
        <h1 className="font-display text-2xl font-bold text-sage-800 mb-3">Could not load matches</h1>
        <p className="text-sage-600 mb-6">
          Find My Match uses the catalog API. Start the gateway and API, then try again.
        </p>
        <Link to="/retreats" className="text-sm font-semibold text-teal-600 hover:text-teal-700">
          Browse retreats
        </Link>
      </div>
    );
  }

  if (phase === "results" && matchBusy) {
    return (
      <div className="max-w-lg mx-auto px-4 py-24 text-center text-sage-600">
        Finding published retreats that fit…
      </div>
    );
  }

  if (phase === "results" && ranked) {
    const list = ranked.exact.length ? ranked.exact : ranked.closest;
    const closestOnly = !ranked.exact.length && ranked.closest.length > 0;

    return (
      <div className="relative min-h-[calc(100vh-8rem)]">
        <div
          className="absolute inset-0 pointer-events-none opacity-30"
          style={{
            backgroundImage: "radial-gradient(ellipse at 50% 0%, rgba(184,160,120,0.2), transparent 55%)",
          }}
        />
        <div className="relative max-w-5xl mx-auto px-4 py-10 md:py-14">
          <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-teal-600 mb-3">
            Your matches
          </p>
          <h1 className="font-display text-3xl md:text-4xl font-bold text-sage-800 mb-2">
            Your Healingram Matches
          </h1>
          <p className="text-sage-600 mb-8 max-w-2xl">
            Based on what you chose, these retreats look like the strongest fit.
          </p>
          {closestOnly && <p className="text-sm font-semibold text-sage-700 mb-4">Closest matches</p>}
          {list.length === 0 ? (
            <div className="rounded-2xl border border-sand-200 bg-white px-6 py-12 text-center max-w-md mx-auto">
              <p className="font-display text-xl font-bold text-sage-800 mb-2">
                We’re still curating for this mix.
              </p>
              <p className="text-sm text-sage-600 mb-6">
                Try adjusting your answers, or browse all launch retreats.
              </p>
              <div className="flex flex-col sm:flex-row gap-3 justify-center">
                <button
                  type="button"
                  onClick={restart}
                  className="px-5 py-2.5 rounded-xl border border-sand-200 text-sm font-semibold text-sage-800 hover:bg-sand-50"
                >
                  Retake
                </button>
                <Link
                  to="/retreats"
                  className="px-5 py-2.5 rounded-xl bg-teal-600 text-white text-sm font-semibold hover:bg-teal-500 text-center"
                >
                  Explore retreats
                </Link>
              </div>
            </div>
          ) : (
            <div className="grid md:grid-cols-2 gap-5">
              {list.map((m) => (
                <MatchCard key={m.retreat.id} match={m} />
              ))}
            </div>
          )}
          <div className="mt-10 flex flex-wrap gap-4 items-center">
            <button type="button" onClick={restart} className="text-sm font-medium text-teal-600 hover:text-teal-700">
              Start over
            </button>
            <Link to="/retreats" className="text-sm text-sage-500 hover:text-sage-700">
              Browse all retreats
            </Link>
          </div>
        </div>
      </div>
    );
  }

  if (!question) {
    return (
      <div className="max-w-lg mx-auto px-4 py-24 text-center text-sage-600">Loading questions…</div>
    );
  }

  return (
    <div className="relative min-h-[calc(100vh-8rem)] overflow-hidden">
      <div
        className="absolute inset-0 pointer-events-none opacity-35"
        style={{
          backgroundImage:
            "radial-gradient(ellipse at 10% 20%, rgba(184,160,120,0.18), transparent 45%), radial-gradient(ellipse at 90% 80%, rgba(58,53,47,0.06), transparent 40%)",
        }}
      />
      <div className="relative max-w-2xl mx-auto px-4 py-10 md:py-14">
        <ProgressDots total={questions.length} current={step} />
        <div
          key={question.key}
          className={`transition-all duration-300 ${
            enterAnim ? "opacity-100 translate-y-0" : "opacity-0 translate-y-2"
          }`}
        >
          <p className="text-sm text-sage-500 mb-2">
            {step + 1} of {questions.length}
          </p>
          <h1 className="font-display text-2xl md:text-3xl font-bold text-sage-800 mb-2 text-balance">
            {question.label}
          </h1>
          <p className="text-sage-600 mb-7">
            {question.selectionMode === "single" ? "Choose one." : "Choose all that feel relevant."}
          </p>
          <div className="grid sm:grid-cols-2 gap-3">
            {question.options.map((opt) => {
              const selected = selectedIds.includes(opt.key);
              return (
                <button
                  key={opt.key}
                  type="button"
                  onClick={() => toggleOption(opt.key)}
                  className={`relative text-left rounded-2xl border px-4 py-4 transition-all duration-200 ${
                    selected
                      ? "border-teal-600 bg-teal-50/80 shadow-sm ring-1 ring-teal-600/20"
                      : "border-sand-200 bg-white/90 hover:border-sage-300 hover:bg-sand-50/80"
                  }`}
                >
                  {selected && (
                    <span className="absolute top-3 right-3 w-5 h-5 rounded-full bg-teal-600 text-white inline-flex items-center justify-center">
                      <Check className="w-3 h-3" strokeWidth={3} />
                    </span>
                  )}
                  <span
                    className={`inline-flex items-center justify-center w-9 h-9 rounded-xl mb-3 ${
                      selected ? "bg-teal-600 text-white" : "bg-sand-100 text-sage-600"
                    }`}
                  >
                    {iconForKey(opt.iconKey)}
                  </span>
                  <span className="block font-semibold text-sage-800 pr-6">{opt.label}</span>
                  {opt.description && (
                    <span className="block text-sm text-sage-500 mt-1 leading-snug">{opt.description}</span>
                  )}
                </button>
              );
            })}
          </div>
        </div>
        <div className="mt-10 flex items-center justify-between gap-3">
          <button
            type="button"
            onClick={goBack}
            className="inline-flex items-center gap-1.5 text-sm font-medium text-sage-600 hover:text-sage-800"
          >
            <ArrowLeft className="w-4 h-4" />
            Back
          </button>
          <button
            type="button"
            disabled={!canContinue}
            onClick={goNext}
            className={`inline-flex items-center gap-2 rounded-xl px-6 py-3 text-sm font-semibold transition ${
              canContinue
                ? "bg-teal-600 text-white hover:bg-teal-500 shadow-sm"
                : "bg-sand-200 text-sage-400 cursor-not-allowed"
            }`}
          >
            {step === questions.length - 1 ? "See My Matches" : "Continue"}
            <ArrowRight className="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>
  );
}

function ProgressDots({ total, current }: { total: number; current: number }) {
  return (
    <div className="flex items-center gap-2 mb-8" aria-hidden>
      {Array.from({ length: total }, (_, i) => (
        <div
          key={i}
          className={`h-1 flex-1 rounded-full transition-colors ${
            i <= current ? "bg-teal-600" : "bg-sand-200"
          }`}
        />
      ))}
    </div>
  );
}

function MatchCard({ match }: { match: RankedMatch }) {
  const { retreat, reasons, tags } = match;
  return (
    <article className="rounded-2xl border border-sand-200 bg-white overflow-hidden flex flex-col shadow-sm hover:shadow-md transition-shadow">
      <div className="aspect-[16/10] overflow-hidden bg-sand-100">
        {retreat.image ? (
          <img src={retreat.image} alt={retreat.name} className="w-full h-full object-cover" />
        ) : null}
      </div>
      <div className="p-5 flex flex-col flex-1">
        <div className="flex items-center gap-1 text-sm text-sage-600 mb-1">
          <MapPin className="w-3.5 h-3.5 shrink-0" />
          <span>
            {retreat.locality}, {retreat.stateLabel ?? regionLabel(retreat.region)}
          </span>
        </div>
        <h2 className="font-display text-xl font-semibold text-sage-800 mb-2 leading-snug">{retreat.name}</h2>
        {tags.length > 0 && (
          <div className="flex flex-wrap gap-1.5 mb-4">
            {tags.map((t) => (
              <span key={t} className="px-2 py-0.5 rounded-md bg-sand-100 text-sage-700 text-[11px] font-medium">
                {t}
              </span>
            ))}
          </div>
        )}
        {reasons.length > 0 && (
          <div className="mb-4 rounded-xl bg-sand-50 px-3.5 py-3">
            <p className="text-xs font-semibold uppercase tracking-wide text-sage-500 mb-2">Why it matches you</p>
            <ul className="space-y-1.5">
              {reasons.map((r) => (
                <li key={r} className="text-sm text-sage-700 flex gap-2">
                  <Check className="w-3.5 h-3.5 text-teal-600 shrink-0 mt-0.5" />
                  <span>{r}</span>
                </li>
              ))}
            </ul>
          </div>
        )}
        <Link
          to={`/retreats/${retreat.id}`}
          state={{
            findMyMatch: {
              youToldUs: [],
              matches: reasons,
            },
          }}
          className="mt-auto inline-flex items-center justify-center rounded-xl bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-teal-500 transition"
        >
          View Retreat
        </Link>
      </div>
    </article>
  );
}
