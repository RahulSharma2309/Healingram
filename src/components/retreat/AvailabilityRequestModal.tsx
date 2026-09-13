import { useEffect, useId, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Check, X } from "lucide-react";
import { IndiaPhoneField } from "../account/IndiaPhoneField";
import { nationalPhone } from "../../lib/accountValidation";
import { quoteProgrammePrice } from "../../lib/api/catalog";
import { apiErrorMessage } from "../../lib/api/client";
import {
  createAvailabilityRequest,
  type AvailabilityRequestSource,
} from "../../lib/availabilityRequests";
import { getCustomerProfile, isLoggedIn } from "../../lib/auth";
import { addNights, formatDisplayDate, nightsBetween } from "../../lib/pricing";

export type AvailabilityDraft = {
  retreatId: string;
  retreatName: string;
  programmeId: string;
  programmeName: string;
  checkIn: string;
  checkOut: string;
  guests: number;
  durationNights: number;
  durationUnit: "nights";
  occupancy: string;
  roomType: string;
  displayedPrice: string;
  quoteId?: string;
  source?: AvailabilityRequestSource;
  /** Collect start date / guests in this modal before requesting */
  needsBookingDetails?: boolean;
  isFlexible?: boolean;
  minCheckIn?: string;
  minimumStay?: number;
};

type Props = {
  open: boolean;
  onClose: () => void;
  draft: AvailabilityDraft;
  /** Sync completed booking fields back into Plan your stay */
  onBookingDetailsChange?: (patch: {
    checkIn: string;
    checkOut: string;
    guests: number;
  }) => void;
};

export function AvailabilityRequestModal({
  open,
  onClose,
  draft,
  onBookingDetailsChange,
}: Props) {
  const navigate = useNavigate();
  const formId = useId();
  const loggedIn = isLoggedIn();
  const profile = getCustomerProfile();

  const [name, setName] = useState(profile.name);
  const [email, setEmail] = useState(profile.email);
  const [phone, setPhone] = useState(nationalPhone(profile.phone));
  const [notes, setNotes] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const [localCheckIn, setLocalCheckIn] = useState(draft.checkIn);
  const [localFlexibleOut, setLocalFlexibleOut] = useState(
    draft.isFlexible ? draft.checkOut : "",
  );
  const [localGuests, setLocalGuests] = useState(String(draft.guests || 2));

  const minCheckIn = draft.minCheckIn ?? new Date().toISOString().slice(0, 10);
  const needsDetails = Boolean(draft.needsBookingDetails);

  const resolvedCheckOut = useMemo(() => {
    if (draft.isFlexible) return localFlexibleOut;
    if (localCheckIn && draft.durationNights) {
      return addNights(localCheckIn, draft.durationNights);
    }
    return draft.checkOut || "";
  }, [
    draft.isFlexible,
    draft.durationNights,
    draft.checkOut,
    localCheckIn,
    localFlexibleOut,
  ]);

  const guestCount = Math.floor(Number(localGuests)) || 1;

  const livePriceLabel = draft.displayedPrice;

  useEffect(() => {
    if (!open) return;
    const p = getCustomerProfile();
    setName(p.name);
    setEmail(p.email);
    setPhone(nationalPhone(p.phone));
    setNotes("");
    setError(null);
    setLocalCheckIn(draft.checkIn);
    setLocalFlexibleOut(draft.isFlexible ? draft.checkOut : "");
    setLocalGuests(String(draft.guests || 2));
  }, [open, draft.retreatId, draft.programmeId, draft.checkIn, draft.checkOut, draft.guests, draft.isFlexible]);

  if (!open) return null;

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!localCheckIn) {
      setError("Please choose a start date.");
      return;
    }
    if (localCheckIn < minCheckIn) {
      setError("Start date cannot be in the past.");
      return;
    }
    if (draft.isFlexible) {
      if (!localFlexibleOut) {
        setError("Please choose an end date.");
        return;
      }
      if (localFlexibleOut <= localCheckIn) {
        setError("End date must be after the start date.");
        return;
      }
      const n = nightsBetween(localCheckIn, localFlexibleOut);
      if (n != null && draft.minimumStay && n < draft.minimumStay) {
        setError(`Minimum stay is ${draft.minimumStay} nights.`);
        return;
      }
    }
    if (!guestCount || guestCount < 1) {
      setError("Please choose number of guests.");
      return;
    }
    if (!name.trim() || !email.trim() || phone.length !== 10) {
      setError("Please enter your name, email and 10-digit mobile number.");
      return;
    }
    const nights = draft.isFlexible
      ? nightsBetween(localCheckIn, localFlexibleOut)
      : draft.durationNights;
    if (!nights || nights < 1) {
      setError("Unable to determine stay length.");
      return;
    }
    if (!resolvedCheckOut) {
      setError("Unable to determine end date.");
      return;
    }

    setSubmitting(true);

    onBookingDetailsChange?.({
      checkIn: localCheckIn,
      checkOut: resolvedCheckOut,
      guests: guestCount,
    });

    try {
      const quote = await quoteProgrammePrice({
        retreatSlug: draft.retreatId,
        programmeSlug: draft.programmeId,
        durationNights: nights,
        occupancy: draft.occupancy || "package",
        guests: guestCount,
      });
      const request = await createAvailabilityRequest({
        customerId: null,
        customerName: name.trim(),
        customerEmail: email.trim(),
        customerPhone: phone,
        countryCode: "+91",
        retreatId: draft.retreatId,
        retreatName: draft.retreatName,
        programmeId: draft.programmeId,
        programmeName: draft.programmeName,
        durationNights: nights,
        durationUnit: draft.durationUnit,
        checkIn: localCheckIn,
        checkOut: resolvedCheckOut,
        guests: guestCount,
        occupancy: quote.occupancy,
        roomType: draft.roomType,
        displayedPrice: livePriceLabel,
        priceStatus:
          quote.priceStatus === "VERIFIED" || quote.priceStatus === "ESTIMATED"
            ? quote.priceStatus
            : "ON_REQUEST",
        priceSnapshot: {
          priceStatus:
            quote.priceStatus === "VERIFIED" || quote.priceStatus === "ESTIMATED"
              ? quote.priceStatus
              : "ON_REQUEST",
          baseAmount: quote.baseAmount ?? null,
          taxAmount: quote.taxAmount ?? null,
          taxDisplay: "not_confirmed",
          totalAmount: quote.totalAmount ?? null,
          occupancy: quote.occupancy,
          roomType: draft.roomType,
          durationNights: nights,
          guests: guestCount,
          currency: "INR",
          label: livePriceLabel,
          capturedAt: new Date().toISOString(),
        },
        settlementMode: "MARKETPLACE_SPLIT",
        source: draft.source ?? "listing",
        customerNotes: notes.trim(),
        quoteId: quote.quoteId,
      });
      onClose();
      navigate(`/requests/${request.requestId}/received`, { state: { request } });
    } catch (err) {
      setError(apiErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4">
      <button
        type="button"
        className="absolute inset-0 bg-black/40"
        aria-label="Close"
        onClick={onClose}
      />
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={`${formId}-title`}
        className="relative w-full sm:max-w-lg max-h-[92vh] overflow-y-auto rounded-t-2xl sm:rounded-2xl bg-white border border-sand-200 shadow-xl"
      >
        <div className="sticky top-0 flex items-center justify-between gap-3 px-5 py-4 border-b border-sand-100 bg-white">
          <h2 id={`${formId}-title`} className="font-display text-xl font-semibold text-sage-800">
            {needsDetails ? "Check availability" : "Request availability"}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 rounded-lg text-sage-500 hover:bg-sand-50"
            aria-label="Close dialog"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={onSubmit} className="p-5 space-y-5">
          <div className="rounded-xl border border-sand-200 bg-sand-50/80 px-4 py-3 text-sm text-sage-700 space-y-2">
            <p className="font-semibold text-sage-800">{draft.retreatName}</p>
            <p className="inline-flex items-center gap-1.5 font-medium text-sage-800">
              <Check className="w-3.5 h-3.5 text-teal-600" />
              {draft.programmeName}
              {!draft.isFlexible && draft.durationNights
                ? ` · ${draft.durationNights} nights`
                : ""}
            </p>
            {!needsDetails && draft.checkIn && draft.checkOut && (
              <>
                <p>
                  {formatDisplayDate(draft.checkIn)} → {formatDisplayDate(draft.checkOut)} ·{" "}
                  {draft.durationNights} nights
                </p>
                <p>
                  {draft.guests} guest{draft.guests === 1 ? "" : "s"}
                  {draft.roomType ? ` · ${draft.roomType}` : ""}
                </p>
                <p className="pt-1 font-medium text-sage-800">{draft.displayedPrice}</p>
              </>
            )}
            {needsDetails && localCheckIn && resolvedCheckOut && (
              <p className="text-sage-600">
                {formatDisplayDate(localCheckIn)} → {formatDisplayDate(resolvedCheckOut)}
                {!draft.isFlexible ? ` · ${draft.durationNights} nights` : ""}
              </p>
            )}
            {needsDetails && (
              <p className="pt-1 font-medium text-sage-800">{livePriceLabel}</p>
            )}
          </div>

          {needsDetails && (
            <div className="space-y-3">
              <label className="block">
                <span className="text-xs font-medium text-sage-600">Start date</span>
                <input
                  required
                  type="date"
                  min={minCheckIn}
                  value={localCheckIn}
                  onChange={(e) => {
                    setLocalCheckIn(e.target.value);
                    setError(null);
                  }}
                  className="mt-1 w-full rounded-xl border border-sand-200 px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-teal-500/30"
                />
              </label>
              {draft.isFlexible ? (
                <label className="block">
                  <span className="text-xs font-medium text-sage-600">End date</span>
                  <input
                    required
                    type="date"
                    min={localCheckIn || minCheckIn}
                    value={localFlexibleOut}
                    onChange={(e) => {
                      setLocalFlexibleOut(e.target.value);
                      setError(null);
                    }}
                    className="mt-1 w-full rounded-xl border border-sand-200 px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-teal-500/30"
                  />
                </label>
              ) : (
                <p className="text-xs text-sage-500">
                  End date is calculated automatically from the programme duration
                  {localCheckIn && resolvedCheckOut
                    ? ` → ${formatDisplayDate(resolvedCheckOut)}`
                    : "."}
                </p>
              )}
              <label className="block">
                <span className="text-xs font-medium text-sage-600">Guests</span>
                <select
                  value={localGuests}
                  onChange={(e) => {
                    setLocalGuests(e.target.value);
                    setError(null);
                  }}
                  className="mt-1 w-full rounded-xl border border-sand-200 px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-teal-500/30"
                >
                  {[1, 2, 3, 4, 5, 6].map((n) => (
                    <option key={n} value={n}>
                      {n} guest{n === 1 ? "" : "s"}
                    </option>
                  ))}
                </select>
              </label>
            </div>
          )}

          <p className="text-sm text-sage-600">
            No payment yet. We’ll check availability with the retreat.
          </p>
          {!loggedIn && (
            <p className="text-xs text-sage-500">
              No account needed. We’ll link this request to your email and mobile.
            </p>
          )}

          <div className="space-y-3">
            <label className="block">
              <span className="text-xs font-medium text-sage-600">Full name</span>
              <input
                required
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="mt-1 w-full rounded-xl border border-sand-200 px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-teal-500/30"
              />
            </label>
            <label className="block">
              <span className="text-xs font-medium text-sage-600">Email</span>
              <input
                required
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="mt-1 w-full rounded-xl border border-sand-200 px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-teal-500/30"
              />
            </label>
            <IndiaPhoneField value={phone} onChange={setPhone} />
            <label className="block">
              <span className="text-xs font-medium text-sage-600">
                Anything the retreat should know? (optional)
              </span>
              <textarea
                rows={3}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                className="mt-1 w-full rounded-xl border border-sand-200 px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-teal-500/30 resize-y"
              />
            </label>
          </div>

          {error && (
            <p role="alert" className="text-sm text-red-600">
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={submitting}
            className="w-full rounded-xl bg-teal-600 py-3.5 text-sm font-semibold text-white hover:bg-teal-500 disabled:opacity-60"
          >
            Request Availability
          </button>
          <p className="text-xs text-center text-sage-500">No payment is taken at this stage.</p>
          <p className="text-center text-sm">
            <Link to="/contact" className="font-medium text-teal-600 hover:text-teal-700">
              Need help? Talk to an Expert
            </Link>
          </p>
        </form>
      </div>
    </div>
  );
}
