import { Link, useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { Shield } from "lucide-react";
import { getAvailabilityByPublicId } from "../../lib/api/availability";
import {
  applyPaymentWebhook,
  getAvailabilityRequest,
  mergeServerAvailability,
  type AvailabilityRequest,
} from "../../lib/availabilityRequests";
import { formatDisplayDate } from "../../lib/pricing";
import { formatInr } from "../../data/programmePricing";
import { rememberedIntentId, refreshIntentStatus, startPlaceholderCheckout } from "../../lib/payment";

export function PaymentReady() {
  const { requestId } = useParams();
  const [request, setRequest] = useState<AvailabilityRequest | undefined>();
  const [lookup, setLookup] = useState<"loading" | "ready" | "missing">("loading");
  const [message, setMessage] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [intentId, setIntentId] = useState<string | null>(null);

  useEffect(() => {
    if (!requestId) {
      setLookup("missing");
      return;
    }

    const local = getAvailabilityRequest(requestId);
    if (local) {
      setRequest(local);
      setLookup("ready");
    } else {
      setLookup("loading");
    }

    getAvailabilityByPublicId(requestId)
      .then((dto) => {
        setRequest(mergeServerAvailability(dto));
        setLookup("ready");
      })
      .catch(() => {
        setLookup(getAvailabilityRequest(requestId) ? "ready" : "missing");
      });

    const remembered = rememberedIntentId(requestId);
    if (remembered) setIntentId(remembered);
  }, [requestId]);

  useEffect(() => {
    if (!intentId) return;
    let cancelled = false;
    const tick = async () => {
      try {
        const intent = await refreshIntentStatus(intentId);
        if (cancelled || !requestId) return;
        if (intent.status === "paid") {
          const current = getAvailabilityRequest(requestId);
          if (current && current.status === "PAYMENT_PENDING" && current.finalPayableAmount != null) {
            applyPaymentWebhook(requestId, {
              providerPaymentId: intent.id,
              amount: current.finalPayableAmount,
              verified: true,
            });
          }
          const latest = getAvailabilityRequest(requestId);
          if (latest) setRequest(latest);
        }
      } catch {
        /* GET is read-only; ignore if API is down */
      }
    };
    tick();
    const timer = window.setInterval(tick, 3000);
    return () => {
      cancelled = true;
      window.clearInterval(timer);
    };
  }, [intentId, requestId]);

  if (lookup === "loading") {
    return (
      <div className="max-w-lg mx-auto px-4 py-16 text-center">
        <p className="text-sm text-sage-600">Checking whether this request is payment-ready…</p>
      </div>
    );
  }

  if (lookup === "missing" || !request) {
    return (
      <div className="max-w-lg mx-auto px-4 py-16 text-center">
        <h1 className="font-display text-2xl font-bold text-sage-800">Payment not available</h1>
        <Link to="/dashboard" className="mt-6 inline-block text-teal-600">
          My dashboard
        </Link>
      </div>
    );
  }

  if (request.status === "PAID" || request.status === "CONFIRMED") {
    return (
      <div className="max-w-lg mx-auto px-4 py-16">
        <h1 className="font-display text-2xl font-bold text-sage-800">Payment confirmed</h1>
        <p className="mt-3 text-sm text-sage-600">
          A verified webhook marked this booking paid. This page did not set that status.
        </p>
        <Link
          to={`/requests/${request.requestId}`}
          className="mt-6 inline-block text-teal-600 font-medium"
        >
          View request
        </Link>
      </div>
    );
  }

  if (request.status !== "PAYMENT_PENDING") {
    return (
      <div className="max-w-lg mx-auto px-4 py-16">
        <h1 className="font-display text-2xl font-bold text-sage-800">Not ready for payment</h1>
        <p className="mt-3 text-sm text-sage-600">
          Current status: {request.status}. Payment opens only when the retreat confirms
          availability.
        </p>
        <Link
          to={`/requests/${request.requestId}`}
          className="mt-6 inline-block text-teal-600 font-medium"
        >
          View request
        </Link>
      </div>
    );
  }

  const total = request.finalPayableAmount;
  const base = request.priceSnapshot.baseAmount;
  const tax = request.priceSnapshot.taxAmount;

  return (
    <div className="max-w-xl mx-auto px-4 py-10">
      <h1 className="font-display text-2xl font-bold text-sage-800 mb-6">Booking summary</h1>

      <div className="rounded-2xl border border-sand-200 bg-white p-6 space-y-2 text-sm text-sage-700">
        <p className="font-semibold text-sage-800 text-base">{request.retreatName}</p>
        <p>{request.programmeName}</p>
        <p>
          {formatDisplayDate(request.checkIn)} → {formatDisplayDate(request.checkOut)}
        </p>
        <p>
          {request.guests} guest{request.guests === 1 ? "" : "s"}
        </p>
        <p>{request.roomType}</p>

        <hr className="my-4 border-sand-200" />

        <div className="flex justify-between">
          <span>Programme price</span>
          <span>{base != null ? formatInr(base) : "On request"}</span>
        </div>
        <div className="flex justify-between">
          <span>Taxes</span>
          <span>
            {request.priceSnapshot.taxDisplay === "included"
              ? "Included"
              : tax != null
                ? formatInr(tax)
                : "Not yet confirmed"}
          </span>
        </div>
        <div className="flex justify-between font-semibold text-sage-800 text-lg pt-3">
          <span>Total</span>
          <span>{total != null ? formatInr(total) : "—"}</span>
        </div>
        <p className="text-xs text-sage-500 pt-2">
          Settlement mode: {request.settlementMode.replace("_", " ")}
        </p>
      </div>

      <button
        type="button"
        disabled={total == null || submitting}
        onClick={async () => {
          setSubmitting(true);
          const result = await startPlaceholderCheckout(request.requestId);
          setMessage(result.message);
          if (result.intentId) setIntentId(result.intentId);
          setSubmitting(false);
        }}
        className="mt-6 w-full rounded-xl bg-teal-600 py-3.5 text-sm font-semibold text-white hover:bg-teal-500 disabled:opacity-50"
      >
        {total != null ? `Pay ${formatInr(total)} securely` : "Payment amount not confirmed"}
      </button>
      <p className="mt-3 flex items-center justify-center gap-1.5 text-xs text-sage-500">
        <Shield className="w-3.5 h-3.5" />
        No payment is marked complete until a verified provider webhook confirms it.
      </p>

      {message && (
        <p role="status" className="mt-4 text-sm text-sage-600 rounded-xl border border-sand-200 bg-sand-50 p-4">
          {message}
        </p>
      )}

      {intentId && (
        <Link
          to={`/payment-success?intent=${encodeURIComponent(intentId)}`}
          className="mt-4 inline-block text-sm text-sage-600"
        >
          Return from provider (status is read-only)
        </Link>
      )}

      <Link
        to={`/requests/${request.requestId}`}
        className="mt-6 inline-block text-sm text-teal-600 font-medium"
      >
        ← Back to request
      </Link>
    </div>
  );
}
