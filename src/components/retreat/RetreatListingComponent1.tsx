import { useEffect, useId, useMemo, useRef, useState, type MutableRefObject } from "react";
import { Link, useLocation } from "react-router-dom";
import { Check, Images, MapPin, MessageCircle } from "lucide-react";
import {
  TRUST_SIGNAL_LABELS,
  type RetreatListingView,
  type RetreatProgrammeOption,
} from "../../data/launchListing";
import { getProgrammePricing } from "../../data/programmePricing";
import {
  addNights,
  buildProgressivePriceView,
  calculateProgrammeTotal,
  formatDisplayDate,
  nightsBetween,
} from "../../lib/pricing";
import {
  AvailabilityRequestModal,
  type AvailabilityDraft,
} from "./AvailabilityRequestModal";
import { useListingPlanOptional } from "../../lib/listingPlanContext";

export type FindMyMatchListingState = {
  youToldUs?: string[];
  matches?: string[];
};

type Props = {
  listing: RetreatListingView;
};

function todayIsoDate(): string {
  const d = new Date();
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

export function RetreatListingComponent1({ listing }: Props) {
  const { retreat, locationLine, positioning, tags, media, trustSignals, programmeOptions } =
    listing;
  const location = useLocation();
  const matchState = (location.state as { findMyMatch?: FindMyMatchListingState } | null)
    ?.findMyMatch;
  const plan = useListingPlanOptional();

  const [localProgrammeId, setLocalProgrammeId] = useState("");
  const [localDurationNights, setLocalDurationNights] = useState<number | null>(null);
  const [localCheckIn, setLocalCheckIn] = useState("");
  const [localFlexibleCheckOut, setLocalFlexibleCheckOut] = useState("");
  const [localGuests, setLocalGuests] = useState("2");
  const [localGuestMode, setLocalGuestMode] = useState<"preset" | "group">("preset");
  const [localGroupSeats, setLocalGroupSeats] = useState("");

  const programmeId = plan?.programmeId ?? localProgrammeId;
  const durationNights = plan?.durationNights ?? localDurationNights;
  const setProgrammeId = plan?.setProgrammeId ?? setLocalProgrammeId;
  const setDurationNights = plan?.setDurationNights ?? setLocalDurationNights;
  const checkIn = plan?.checkIn ?? localCheckIn;
  const setCheckIn = plan?.setCheckIn ?? setLocalCheckIn;
  const flexibleCheckOut = plan?.flexibleCheckOut ?? localFlexibleCheckOut;
  const setFlexibleCheckOut = plan?.setFlexibleCheckOut ?? setLocalFlexibleCheckOut;
  const guests = plan?.guests ?? localGuests;
  const setGuests = plan?.setGuests ?? setLocalGuests;
  const guestMode = plan?.guestMode ?? localGuestMode;
  const setGuestMode = plan?.setGuestMode ?? setLocalGuestMode;
  const groupSeats = plan?.groupSeats ?? localGroupSeats;
  const setGroupSeats = plan?.setGroupSeats ?? setLocalGroupSeats;
  const roomName = plan?.roomName ?? "";
  const occupancyPreference = plan?.occupancyPreference ?? "auto";

  const [dateError, setDateError] = useState<string | null>(null);
  const [validationHint, setValidationHint] = useState<string | null>(null);
  const [galleryNote, setGalleryNote] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [draft, setDraft] = useState<AvailabilityDraft | null>(null);

  const formId = useId();
  const minCheckIn = todayIsoDate();
  const primary = media[0];
  const tiles = media.slice(1, 5);

  const selectedProgramme: RetreatProgrammeOption | undefined = useMemo(
    () => programmeOptions.find((p) => p.id === programmeId),
    [programmeOptions, programmeId],
  );

  const pricingRow = useMemo(() => {
    if (!programmeId) return null;
    return getProgrammePricing(retreat.id, programmeId) ?? null;
  }, [retreat.id, programmeId]);

  const isFlexible = pricingRow?.durationMode === "flexible";
  const durationUnit = pricingRow?.durationUnit ?? selectedProgramme?.durationUnit ?? "nights";
  const guestCount =
    guestMode === "group"
      ? Math.floor(Number(groupSeats))
      : Math.floor(Number(guests)) || 1;
  const guestsValid =
    guestMode === "group"
      ? Number.isFinite(guestCount) && guestCount >= 7
      : guestCount >= 1 && guestCount <= 6;

  // Fixed programmes: checkout is always derived — never customer-entered
  const checkOut = useMemo(() => {
    if (isFlexible) return flexibleCheckOut;
    if (checkIn && durationNights) return addNights(checkIn, durationNights);
    return "";
  }, [isFlexible, flexibleCheckOut, checkIn, durationNights]);

  const nights = useMemo(() => {
    if (!isFlexible) {
      return checkIn && durationNights ? durationNights : null;
    }
    return nightsBetween(checkIn, flexibleCheckOut);
  }, [isFlexible, checkIn, durationNights, flexibleCheckOut]);

  // Pricing needs programme + duration + start date (+ guests) — not a manual checkout
  const datesComplete = Boolean(
    checkIn &&
      !dateError &&
      (isFlexible
        ? nights != null && nights > 0
        : durationNights != null && durationNights > 0),
  );

  const priceView = buildProgressivePriceView({
    programmeSelected: Boolean(programmeId && pricingRow),
    row: pricingRow,
    guests: guestCount,
    nights: isFlexible ? nights : durationNights,
    datesComplete,
    occupancyPreference,
  });

  const canSubmit =
    Boolean(programmeId) &&
    datesComplete &&
    guestsValid &&
    Boolean(checkOut) &&
    (isFlexible
      ? nights != null && nights >= (pricingRow?.minimumStay ?? 1)
      : durationNights != null &&
        (pricingRow?.supportedDurations.includes(durationNights) ?? false));

  // Keep duration in sync if catalogue options change; programme change sets duration synchronously
  useEffect(() => {
    if (!selectedProgramme || !pricingRow) return;
    if (pricingRow.durationMode === "flexible") return;
    const durations = selectedProgramme.supportedDurations;
    if (durations.length === 1 && durationNights !== durations[0]) {
      setDurationNights(durations[0]);
    }
  }, [selectedProgramme, pricingRow, durationNights]);
  useEffect(() => {
    if (!checkIn) {
      setDateError(null);
      return;
    }
    if (checkIn < minCheckIn) {
      setDateError("Start date cannot be in the past.");
      return;
    }
    if (isFlexible) {
      if (flexibleCheckOut && flexibleCheckOut <= checkIn) {
        setDateError("End date must be after the start date.");
        return;
      }
      const n = nightsBetween(checkIn, flexibleCheckOut);
      if (flexibleCheckOut && n != null && pricingRow && n < pricingRow.minimumStay) {
        setDateError(`Minimum stay is ${pricingRow.minimumStay} nights.`);
        return;
      }
      setDateError(null);
      return;
    }
    setDateError(null);
  }, [checkIn, flexibleCheckOut, isFlexible, minCheckIn, pricingRow]);

  const displayedPriceLabel =
    priceView.kind === "total"
      ? priceView.label
      : priceView.kind === "from"
        ? priceView.label
        : priceView.message;

  const openAvailability = (overrides?: { programmeId?: string; durationNights?: number | null }) => {
    const effectiveProgrammeId = overrides?.programmeId ?? programmeId;
    const effectiveDuration =
      overrides?.durationNights !== undefined ? overrides.durationNights : durationNights;

    const selected =
      programmeOptions.find((p) => p.id === effectiveProgrammeId) ?? selectedProgramme;
    const row =
      effectiveProgrammeId
        ? getProgrammePricing(retreat.id, effectiveProgrammeId) ?? null
        : pricingRow;

    if (!effectiveProgrammeId || !row || !selected) {
      setValidationHint("Select a programme, start date and guests to continue.");
      return;
    }

    const flexible = row.durationMode === "flexible";
    if (!flexible && effectiveDuration == null) {
      setValidationHint("Select a programme duration to continue.");
      return;
    }

    const derivedCheckOut =
      !flexible && checkIn && effectiveDuration
        ? addNights(checkIn, effectiveDuration)
        : flexible
          ? flexibleCheckOut
          : checkOut;

    const nightsForDraft = flexible
      ? nightsBetween(checkIn, flexibleCheckOut) ?? row.minimumStay
      : effectiveDuration!;

    const datesOk = Boolean(
      checkIn &&
        !dateError &&
        (flexible
          ? nightsBetween(checkIn, flexibleCheckOut) != null
          : effectiveDuration != null) &&
        derivedCheckOut,
    );
    const complete = datesOk && guestsValid;

    const calc = calculateProgrammeTotal(row, guestCount, occupancyPreference);
    const priceViewNow = buildProgressivePriceView({
      programmeSelected: true,
      row,
      guests: guestCount,
      nights: flexible ? nightsBetween(checkIn, flexibleCheckOut) : effectiveDuration,
      datesComplete: datesOk,
      occupancyPreference,
    });
    const priceLabel =
      priceViewNow.kind === "total" || priceViewNow.kind === "from"
        ? priceViewNow.label
        : priceViewNow.message;

    setValidationHint(null);
    setDraft({
      retreatId: retreat.id,
      retreatName: retreat.name,
      programmeId: selected.id,
      programmeName: selected.label,
      checkIn,
      checkOut: derivedCheckOut || "",
      guests: guestCount,
      durationNights: nightsForDraft,
      durationUnit: row.durationUnit ?? "nights",
      occupancy: calc?.occupancy ?? "pending",
      roomType: roomName || calc?.roomType || row.roomType,
      displayedPrice: priceLabel,
      source: matchState ? "find_my_match" : "listing",
      needsBookingDetails: !complete,
      isFlexible: flexible,
      minCheckIn,
      minimumStay: row.minimumStay,
    });
    setModalOpen(true);
  };

  const openAvailabilityRef = useRef(openAvailability);
  openAvailabilityRef.current = openAvailability;

  useEffect(() => {
    if (!plan) return;
    const handler = (overrides?: { programmeId?: string; durationNights?: number | null }) => {
      openAvailabilityRef.current(overrides);
    };
    plan.registerStartAvailabilityFlow(handler);
    return () => plan.registerStartAvailabilityFlow(null);
  }, [plan]);

  const onProgrammeChange = (id: string) => {
    setProgrammeId(id);
    setValidationHint(null);
    setFlexibleCheckOut("");
    const opt = programmeOptions.find((p) => p.id === id);
    const row = id ? getProgrammePricing(retreat.id, id) : null;
    if (!opt || !row || row.durationMode === "flexible") {
      setDurationNights(null);
      return;
    }
    if (opt.supportedDurations.length === 1) {
      setDurationNights(opt.supportedDurations[0]);
    } else {
      setDurationNights(null);
    }
  };

  const onDurationChange = (n: number) => {
    setDurationNights(n);
    setValidationHint(null);
    // checkOut is derived from checkIn + duration — no manual end date
  };

  const onCheckInChange = (v: string) => {
    setCheckIn(v);
    setValidationHint(null);
  };

  return (
    <>
      {/* Identity */}
      <header className="mb-6 md:mb-8">
        <h1 className="font-display text-3xl md:text-4xl font-bold text-sage-800 tracking-tight text-balance">
          {retreat.name}
        </h1>
        <p className="mt-2 flex items-center gap-1.5 text-sage-600">
          <MapPin className="w-4 h-4 shrink-0" />
          {locationLine}
        </p>
        <p className="mt-4 text-base md:text-lg text-sage-700 leading-relaxed max-w-3xl text-balance">
          {positioning}
        </p>
        {tags.length > 0 && (
          <div className="mt-4 flex flex-wrap gap-2">
            {tags.map((tag) => (
              <span
                key={tag}
                className="px-2.5 py-1 rounded-md bg-sand-100 text-sage-700 text-xs font-medium"
              >
                {tag}
              </span>
            ))}
          </div>
        )}

        <ul className="mt-5 flex flex-wrap gap-x-4 gap-y-2 text-sm text-sage-600">
          {trustSignals.map((signal) => (
            <li key={signal} className="inline-flex items-center gap-1.5">
              <Check className="w-3.5 h-3.5 text-teal-600" />
              {TRUST_SIGNAL_LABELS[signal]}
            </li>
          ))}
        </ul>

        {matchState && (matchState.youToldUs?.length || matchState.matches?.length) ? (
          <div className="mt-6 rounded-2xl border border-sand-200 bg-sand-50/80 px-5 py-4 max-w-3xl">
            <h2 className="font-display text-lg font-semibold text-sage-800 mb-3">
              Why this matched you
            </h2>
            {matchState.youToldUs && matchState.youToldUs.length > 0 && (
              <p className="text-sm text-sage-600 mb-3">
                <span className="font-medium text-sage-800">You told us: </span>
                {matchState.youToldUs.join(" · ")}
              </p>
            )}
            {matchState.matches && matchState.matches.length > 0 && (
              <div>
                <p className="text-sm font-medium text-sage-800 mb-2">This retreat matches:</p>
                <ul className="space-y-1.5">
                  {matchState.matches.map((m) => (
                    <li key={m} className="text-sm text-sage-700 flex gap-2">
                      <Check className="w-3.5 h-3.5 text-teal-600 shrink-0 mt-0.5" />
                      {m}
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        ) : null}
      </header>

      <div className="grid lg:grid-cols-3 gap-8 items-start">
        <div className="lg:col-span-2 space-y-6">
          <div>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-2 sm:gap-3">
              <div className="sm:col-span-2 sm:row-span-2 relative rounded-2xl overflow-hidden aspect-[4/3] sm:aspect-auto sm:min-h-[320px] lg:min-h-[380px] bg-sand-100">
                {primary && (
                  <img
                    src={primary.src}
                    alt={primary.alt}
                    className="absolute inset-0 w-full h-full object-cover"
                  />
                )}
              </div>
              {tiles.map((tile) => (
                <div
                  key={tile.id}
                  className="relative rounded-xl overflow-hidden aspect-[4/3] bg-sand-100"
                >
                  <img
                    src={tile.src}
                    alt={tile.alt}
                    className="absolute inset-0 w-full h-full object-cover"
                  />
                  <span className="absolute bottom-2 left-2 px-2 py-0.5 rounded bg-black/45 text-white text-[10px] font-medium capitalize">
                    {tile.category}
                  </span>
                </div>
              ))}
            </div>
            <button
              type="button"
              className="mt-3 inline-flex items-center gap-2 text-sm font-semibold text-teal-600 hover:text-teal-700"
              onClick={() =>
                setGalleryNote(
                  "Full photo & video gallery will open here once partner media is connected.",
                )
              }
            >
              <Images className="w-4 h-4" />
              View all photos & videos
            </button>
            {media.some((m) => m.temporary) && (
              <p className="mt-2 text-xs text-sage-500">
                Some images are temporary placeholders until partner media is supplied.
              </p>
            )}
            {galleryNote && (
              <p role="status" className="mt-2 text-sm text-sage-600">
                {galleryNote}
              </p>
            )}
          </div>

          <div className="rounded-2xl border border-sand-200 bg-white px-5 py-5">
            <h2 className="font-display text-lg font-semibold text-sage-800 mb-3">Programmes</h2>
            <p className="text-sage-700 font-medium">{listing.durationSummary}</p>
            <p className="mt-2 text-sage-600 text-sm">
              {listing.priceLabel ? (
                <span className="text-lg font-semibold text-sage-800">{listing.priceLabel}</span>
              ) : (
                listing.pricePlaceholder
              )}
            </p>
            <p className="mt-3 text-xs text-sage-500">
              Healingram presents wellness programmes with stay — not nightly hotel room rates.
            </p>
          </div>

          <div className="hidden lg:flex flex-wrap gap-3">
            <button
              type="button"
              onClick={openAvailability}
              disabled={!canSubmit}
              className="inline-flex items-center justify-center rounded-xl bg-teal-600 px-6 py-3 text-sm font-semibold text-white hover:bg-teal-500 transition disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Check Availability
            </button>
            <Link
              to="/contact"
              state={{
                source: "listing" as const,
                retreatId: retreat.id,
                retreatName: retreat.name,
                programmeId: programmeId || undefined,
                programmeName: selectedProgramme?.label,
                duration: durationNights,
                checkIn: checkIn || undefined,
                checkOut: checkOut || undefined,
              }}
              className="inline-flex items-center justify-center gap-2 rounded-xl border border-sand-200 px-5 py-3 text-sm font-semibold text-sage-800 hover:bg-sand-50 transition"
            >
              <MessageCircle className="w-4 h-4" />
              Talk to an Expert
            </Link>
          </div>
          {validationHint && (
            <p role="status" className="hidden lg:block text-sm text-sage-600">
              {validationHint}
            </p>
          )}
        </div>

        <aside className="hidden lg:block">
          <div
            ref={(node) => {
              if (plan?.planPanelRef) {
                (plan.planPanelRef as MutableRefObject<HTMLElement | null>).current = node;
              }
            }}
            id="plan-your-stay"
            className="sticky top-24 rounded-2xl border border-sand-200 bg-white p-6 shadow-sm outline-none focus-visible:ring-2 focus-visible:ring-teal-500/40"
          >
            <h2 className="font-display text-xl font-semibold text-sage-800 mb-5 text-balance">
              Plan your stay
            </h2>

            <AvailabilityFields
              formId={formId}
              programmeOptions={programmeOptions}
              programmeId={programmeId}
              onProgrammeChange={onProgrammeChange}
              durationNights={durationNights}
              onDurationChange={onDurationChange}
              checkIn={checkIn}
              checkOut={checkOut}
              onCheckIn={onCheckInChange}
              onCheckOut={(v) => {
                setFlexibleCheckOut(v);
                setValidationHint(null);
              }}
              isFlexible={isFlexible}
              minCheckIn={minCheckIn}
              guests={guests}
              guestMode={guestMode}
              groupSeats={groupSeats}
              onGuests={(v) => {
                if (v === "group") {
                  setGuestMode("group");
                  setValidationHint(null);
                  return;
                }
                setGuestMode("preset");
                setGuests(v);
                setValidationHint(null);
              }}
              onGroupSeats={(v) => {
                setGroupSeats(v.replace(/\D/g, ""));
                setValidationHint(null);
              }}
              dateError={dateError}
            />

            <PriceBlock view={priceView} />

            {roomName && selectedProgramme && (
              <p className="mt-3 text-sm text-sage-700 leading-relaxed">
                <span className="font-medium text-sage-800">
                  {durationNights
                    ? `${durationNights}-night ${selectedProgramme.label}`
                    : selectedProgramme.label}
                </span>
                {" + "}
                {roomName}
              </p>
            )}

            <button
              type="button"
              onClick={openAvailability}
              disabled={!canSubmit}
              className="mt-5 w-full rounded-xl bg-teal-600 py-3.5 text-sm font-semibold text-white hover:bg-teal-500 transition disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Check Availability
            </button>
            <p className="mt-3 text-xs text-center text-sage-500 leading-relaxed">
              No payment yet. We’ll confirm availability with the retreat first.
            </p>
            <Link
              to="/contact"
              state={{
                source: "listing" as const,
                retreatId: retreat.id,
                retreatName: retreat.name,
                programmeId: programmeId || undefined,
                programmeName: selectedProgramme?.label,
                duration: durationNights,
                checkIn: checkIn || undefined,
                checkOut: checkOut || undefined,
              }}
              className="mt-4 block text-center text-sm font-medium text-teal-600 hover:text-teal-700"
            >
              Need help choosing? Talk to an Expert
            </Link>
            {validationHint && (
              <p role="status" className="mt-4 text-sm text-sage-600 text-center">
                {validationHint}
              </p>
            )}
          </div>
        </aside>
      </div>

      {/* Mobile inline fields + bottom CTA */}
      <div
        id="plan-your-stay-mobile"
        className="lg:hidden mt-8 rounded-2xl border border-sand-200 bg-white p-5 outline-none focus-visible:ring-2 focus-visible:ring-teal-500/40"
      >
        <h2 className="font-display text-lg font-semibold text-sage-800 mb-4">Plan your stay</h2>
        <AvailabilityFields
          formId={`${formId}-m`}
          programmeOptions={programmeOptions}
          programmeId={programmeId}
          onProgrammeChange={onProgrammeChange}
          durationNights={durationNights}
          onDurationChange={onDurationChange}
          checkIn={checkIn}
          checkOut={checkOut}
          onCheckIn={onCheckInChange}
          onCheckOut={(v) => {
            setFlexibleCheckOut(v);
            setValidationHint(null);
          }}
          isFlexible={isFlexible}
          minCheckIn={minCheckIn}
          guests={guests}
          guestMode={guestMode}
          groupSeats={groupSeats}
          onGuests={(v) => {
            if (v === "group") {
              setGuestMode("group");
              setValidationHint(null);
              return;
            }
            setGuestMode("preset");
            setGuests(v);
            setValidationHint(null);
          }}
          onGroupSeats={(v) => {
            setGroupSeats(v.replace(/\D/g, ""));
            setValidationHint(null);
          }}
          dateError={dateError}
        />
        <div className="mt-4">
          <PriceBlock view={priceView} />
        </div>
        {roomName && selectedProgramme && (
          <p className="mt-3 text-sm text-sage-700 leading-relaxed">
            <span className="font-medium text-sage-800">
              {durationNights
                ? `${durationNights}-night ${selectedProgramme.label}`
                : selectedProgramme.label}
            </span>
            {" + "}
            {roomName}
          </p>
        )}
      </div>

      <div className="lg:hidden fixed bottom-0 inset-x-0 z-30 border-t border-sand-200 bg-white/95 backdrop-blur-md px-4 py-3 pb-[max(0.75rem,env(safe-area-inset-bottom))]">
        <div className="max-w-7xl mx-auto flex items-center gap-3">
          <div className="min-w-0 flex-1">
            <p className="text-xs text-sage-500 truncate">
              {selectedProgramme?.label ?? "Choose programme"}
              {roomName ? ` · ${roomName}` : ""}
            </p>
            <p className="text-sm font-semibold text-sage-800 truncate">{displayedPriceLabel}</p>
          </div>
          <button
            type="button"
            onClick={openAvailability}
            disabled={!canSubmit}
            className="shrink-0 rounded-xl bg-teal-600 px-5 py-3 text-sm font-semibold text-white hover:bg-teal-500 disabled:opacity-50"
          >
            Check Availability
          </button>
        </div>
      </div>

      {draft && (
        <AvailabilityRequestModal
          open={modalOpen}
          onClose={() => setModalOpen(false)}
          draft={draft}
          onBookingDetailsChange={({ checkIn: nextIn, checkOut: nextOut, guests: nextGuests }) => {
            setCheckIn(nextIn);
            if (isFlexible) setFlexibleCheckOut(nextOut);
            if (nextGuests >= 7) {
              setGuestMode("group");
              setGroupSeats(String(nextGuests));
            } else {
              setGuestMode("preset");
              setGuests(String(nextGuests));
            }
          }}
        />
      )}
    </>
  );
}

function PriceBlock({
  view,
}: {
  view: ReturnType<typeof buildProgressivePriceView>;
}) {
  if (view.kind === "total") {
    return (
      <div className="mt-5">
        <p className="text-2xl font-semibold text-sage-800">{view.label}</p>
        <p className="text-sm text-sage-600 mt-1">{view.detail}</p>
        <p className="text-xs text-sage-500 mt-1">{view.taxNote}</p>
      </div>
    );
  }
  if (view.kind === "from") {
    return (
      <div className="mt-5">
        <p className="text-xl font-semibold text-sage-800">{view.label}</p>
        <p className="text-xs text-sage-500 mt-1">
          {view.taxDisplay === "included"
            ? "Taxes included"
            : view.taxDisplay === "additional"
              ? "Taxes additional"
              : "Taxes not yet confirmed"}
        </p>
      </div>
    );
  }
  return (
    <p className="mt-5 text-sm text-sage-700">{view.message}</p>
  );
}

function AvailabilityFields({
  formId,
  programmeOptions,
  programmeId,
  onProgrammeChange,
  durationNights,
  onDurationChange,
  checkIn,
  checkOut,
  onCheckIn,
  onCheckOut,
  isFlexible,
  minCheckIn,
  guests,
  guestMode,
  groupSeats,
  onGuests,
  onGroupSeats,
  dateError,
}: {
  formId: string;
  programmeOptions: RetreatProgrammeOption[];
  programmeId: string;
  onProgrammeChange: (id: string) => void;
  durationNights: number | null;
  onDurationChange: (n: number) => void;
  checkIn: string;
  checkOut: string;
  onCheckIn: (v: string) => void;
  onCheckOut: (v: string) => void;
  isFlexible: boolean;
  minCheckIn: string;
  guests: string;
  guestMode: "preset" | "group";
  groupSeats: string;
  onGuests: (v: string) => void;
  onGroupSeats: (v: string) => void;
  dateError: string | null;
}) {
  const selected = programmeOptions.find((p) => p.id === programmeId);
  const multiDuration =
    !isFlexible && (selected?.supportedDurations.length ?? 0) > 1;
  const durationReady = isFlexible || durationNights != null;
  const canPickStart = Boolean(programmeId) && durationReady;

  return (
    <div className="space-y-4">
      <label className="block">
        <span className="text-[11px] font-semibold uppercase tracking-wide text-teal-700">
          Programme
        </span>
        <select
          id={`${formId}-programme`}
          value={programmeId}
          onChange={(e) => onProgrammeChange(e.target.value)}
          className="mt-1.5 w-full rounded-xl border border-sand-200 bg-white px-3 py-2.5 text-sm text-sage-800 outline-none focus:ring-2 focus:ring-teal-500/30"
        >
          <option value="">Choose programme</option>
          {programmeOptions.map((p) => (
            <option key={p.id} value={p.id}>
              {p.label} · {p.durationLabel}
            </option>
          ))}
        </select>
      </label>

      {multiDuration && (
        <label className="block">
          <span className="text-[11px] font-semibold uppercase tracking-wide text-teal-700">
            Duration
          </span>
          <select
            value={durationNights ?? ""}
            onChange={(e) => onDurationChange(Number(e.target.value))}
            className="mt-1.5 w-full rounded-xl border border-sand-200 bg-white px-3 py-2.5 text-sm text-sage-800 outline-none focus:ring-2 focus:ring-teal-500/30"
          >
            <option value="">Choose duration</option>
            {selected?.supportedDurations.map((n) => (
              <option key={n} value={n}>
                {n} nights
              </option>
            ))}
          </select>
        </label>
      )}

      <div>
        {isFlexible ? (
          <>
            <span className="text-[11px] font-semibold uppercase tracking-wide text-teal-700">
              Preferred dates
            </span>
            <div className="mt-1.5 grid grid-cols-2 gap-2">
              <label className="block">
                <span className="sr-only">Start date</span>
                <input
                  type="date"
                  value={checkIn}
                  min={minCheckIn}
                  disabled={!programmeId}
                  onChange={(e) => onCheckIn(e.target.value)}
                  className="w-full rounded-xl border border-sand-200 px-3 py-2.5 text-sm text-sage-800 outline-none focus:ring-2 focus:ring-teal-500/30 disabled:bg-sand-50"
                />
              </label>
              <label className="block">
                <span className="sr-only">End date</span>
                <input
                  type="date"
                  value={checkOut}
                  min={checkIn || minCheckIn}
                  disabled={!checkIn}
                  onChange={(e) => onCheckOut(e.target.value)}
                  className="w-full rounded-xl border border-sand-200 px-3 py-2.5 text-sm text-sage-800 outline-none focus:ring-2 focus:ring-teal-500/30 disabled:bg-sand-50"
                />
              </label>
            </div>
            <p className="mt-1.5 text-xs text-sage-500">Flexible-duration programme</p>
          </>
        ) : (
          <>
            <label className="block">
              <span className="text-[11px] font-semibold uppercase tracking-wide text-teal-700">
                Select start date
              </span>
              <input
                type="date"
                value={checkIn}
                min={minCheckIn}
                disabled={!canPickStart}
                onChange={(e) => onCheckIn(e.target.value)}
                className="mt-1.5 w-full rounded-xl border border-sand-200 px-3 py-2.5 text-sm text-sage-800 outline-none focus:ring-2 focus:ring-teal-500/30 disabled:bg-sand-50"
              />
            </label>
            {!canPickStart && programmeId && multiDuration && (
              <p className="mt-1.5 text-xs text-sage-500">Choose a duration first</p>
            )}
            {checkIn && durationNights && checkOut && (
              <div className="mt-3">
                <p className="text-sm font-medium text-sage-800">
                  {formatDisplayDate(checkIn)} → {formatDisplayDate(checkOut)}
                </p>
                <p className="mt-0.5 text-xs text-sage-500">
                  {durationNights}-night programme
                </p>
              </div>
            )}
          </>
        )}
        {dateError && (
          <p role="alert" className="mt-1.5 text-xs text-red-600">
            {dateError}
          </p>
        )}
      </div>

      <div>
        <label className="block">
          <span className="text-[11px] font-semibold uppercase tracking-wide text-teal-700">
            Guests
          </span>
          <select
            value={guestMode === "group" ? "group" : guests}
            onChange={(e) => onGuests(e.target.value)}
            className="mt-1.5 w-full rounded-xl border border-sand-200 bg-white px-3 py-2.5 text-sm text-sage-800 outline-none focus:ring-2 focus:ring-teal-500/30"
          >
            {[1, 2, 3, 4, 5, 6].map((n) => (
              <option key={n} value={String(n)}>
                {n} {n === 1 ? "guest" : "guests"}
              </option>
            ))}
            <option value="group">Book for a group</option>
          </select>
        </label>
        {guestMode === "group" && (
          <label className="block mt-3">
            <span className="text-[11px] font-semibold uppercase tracking-wide text-teal-700">
              Seats to reserve
            </span>
            <input
              type="number"
              inputMode="numeric"
              min={7}
              step={1}
              placeholder="e.g. 12"
              value={groupSeats}
              onChange={(e) => onGroupSeats(e.target.value)}
              className="mt-1.5 w-full rounded-xl border border-sand-200 px-3 py-2.5 text-sm text-sage-800 outline-none focus:ring-2 focus:ring-teal-500/30"
            />
            <p className="mt-1.5 text-xs text-sage-500">
              For groups of 7 or more. Enter how many seats you’d like to reserve.
            </p>
            {groupSeats !== "" && Number(groupSeats) < 7 && (
              <p role="alert" className="mt-1 text-xs text-red-600">
                Enter at least 7 seats for a group booking.
              </p>
            )}
          </label>
        )}
      </div>
    </div>
  );
}
