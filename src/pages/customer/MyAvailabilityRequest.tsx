import { Link, useNavigate, useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { ApiError } from "../../lib/api/client";
import { getAvailabilityByPublicId } from "../../lib/api/availability";
import { isRegisteredAccount } from "../../lib/auth";
import {
  customerAcceptAlternative,
  getAvailabilityRequest,
  mergeServerAvailability,
  type AvailabilityRequest,
} from "../../lib/availabilityRequests";
import { formatDisplayDate } from "../../lib/pricing";
import { formatInr } from "../../lib/money";

export function MyAvailabilityRequest() {
  const { requestId } = useParams();
  const navigate = useNavigate();
  const [request, setRequest] = useState<AvailabilityRequest | undefined>();
  const [lookup, setLookup] = useState<"loading" | "ready" | "missing">("loading");

  const refresh = () => {
    if (!requestId) return;
    const local = getAvailabilityRequest(requestId);
    if (local) setRequest(local);
  };

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
      .catch((error) => {
        if (error instanceof ApiError && (error.status === 401 || error.status === 403)) {
          navigate(`/requests/${requestId}/verify`, { replace: true });
          return;
        }
        setLookup(getAvailabilityRequest(requestId) ? "ready" : "missing");
      });

    const onChange = () => refresh();
    window.addEventListener("healingram-requests", onChange);
    window.addEventListener("storage", onChange);
    return () => {
      window.removeEventListener("healingram-requests", onChange);
      window.removeEventListener("storage", onChange);
    };
  }, [requestId]);

  if (lookup === "loading") {
    return (
      <div className="max-w-lg mx-auto px-4 py-16 text-center">
        <p className="text-sm text-sage-600">Looking up your availability request…</p>
      </div>
    );
  }

  if (lookup === "missing" || !request) {
    return (
      <div className="max-w-lg mx-auto px-4 py-16 text-center">
        <h1 className="font-display text-2xl font-bold text-sage-800">Request not found</h1>
        <Link to="/my-request" className="mt-6 inline-block text-teal-600 font-medium">
          My Request
        </Link>
      </div>
    );
  }

  const amount =
    request.finalPayableAmount != null
      ? formatInr(request.finalPayableAmount)
      : request.displayedPrice;

  return (
    <div className="max-w-2xl mx-auto px-4 py-10">
      <p className="text-xs font-mono text-teal-700 mb-2">{request.requestId}</p>
      <p className="text-xs uppercase tracking-wide text-sage-500 mb-4">Status: {request.status}</p>
      {!isRegisteredAccount() ? (
        <div className="mb-6 rounded-2xl border border-sand-200 bg-sand-50 p-4">
          <p className="text-sm font-medium text-sage-800">Want to access this request anytime?</p>
          <p className="mt-1 text-sm text-sage-600">
            Create your Healingram account in one step. Payment still waits until the retreat confirms.
          </p>
          <Link
            to={`/signup?next=${encodeURIComponent(`/requests/${request.requestId}`)}`}
            className="mt-3 inline-flex text-sm font-semibold text-teal-700 hover:text-teal-600"
          >
            Create your Healingram account
          </Link>
        </div>
      ) : null}

      {request.status === "REQUESTED" && (
        <>
          <h1 className="font-display text-2xl font-bold text-sage-800">We’re checking availability</h1>
          <p className="mt-3 text-sm text-sage-600">
            We’re confirming your programme and dates with the retreat. No payment is required yet.
          </p>
        </>
      )}

      {request.status === "PAYMENT_PENDING" && (
        <>
          <h1 className="font-display text-2xl font-bold text-sage-800">Your retreat is available</h1>
          <Summary request={request} amount={amount} />
          <Link
            to={
              isRegisteredAccount()
                ? `/requests/${request.requestId}/payment`
                : `/signup?next=${encodeURIComponent(`/requests/${request.requestId}/payment`)}`
            }
            className="mt-6 inline-flex rounded-xl bg-teal-600 px-5 py-3 text-sm font-semibold text-white hover:bg-teal-500"
          >
            {isRegisteredAccount() ? "Continue to payment" : "Yes, I want to book"}
          </Link>
        </>
      )}

      {request.status === "ALTERNATIVE_PROPOSED" && request.alternative && (
        <>
          <h1 className="font-display text-2xl font-bold text-sage-800">
            Your exact request isn’t available
          </h1>
          <p className="mt-3 text-sm text-sage-600">The retreat has suggested this option:</p>
          <div className="mt-4 rounded-2xl border border-sand-200 bg-sand-50/80 p-5 text-sm space-y-1">
            <p className="font-medium text-sage-800">{request.alternative.programmeName}</p>
            <p>
              {formatDisplayDate(request.alternative.checkIn)} →{" "}
              {formatDisplayDate(request.alternative.checkOut)}
            </p>
            <p>
              {request.alternative.guests} guests · {request.alternative.roomType}
            </p>
            <p className="font-semibold text-sage-800 pt-2">
              {request.alternative.finalAmount != null
                ? formatInr(request.alternative.finalAmount)
                : "Price on request"}
            </p>
          </div>
          <div className="mt-6 flex flex-col sm:flex-row gap-3">
            <button
              type="button"
              className="rounded-xl bg-teal-600 px-5 py-3 text-sm font-semibold text-white"
              onClick={() => {
                customerAcceptAlternative(request.requestId);
                refresh();
              }}
            >
              Accept this option
            </button>
            <Link
              to="/contact"
              className="rounded-xl border border-sand-200 px-5 py-3 text-sm font-semibold text-center"
            >
              Talk to an Expert
            </Link>
          </div>
        </>
      )}

      {request.status === "REJECTED" && (
        <>
          <h1 className="font-display text-2xl font-bold text-sage-800">
            Those dates aren’t available
          </h1>
          <div className="mt-6 flex flex-col sm:flex-row gap-3">
            <Link
              to={`/retreats/${request.retreatId}`}
              className="rounded-xl bg-teal-600 px-5 py-3 text-sm font-semibold text-white text-center"
            >
              Try different dates
            </Link>
            <Link
              to="/search"
              className="rounded-xl border border-sand-200 px-5 py-3 text-sm font-semibold text-center"
            >
              See similar retreats
            </Link>
            <Link
              to="/contact"
              className="rounded-xl border border-sand-200 px-5 py-3 text-sm font-semibold text-center"
            >
              Talk to an Expert
            </Link>
          </div>
        </>
      )}

      {request.status === "CONFIRMED" && (
        <>
          <h1 className="font-display text-2xl font-bold text-sage-800">Your retreat is confirmed</h1>
          <p className="mt-2 font-mono text-sm text-teal-700">Booking ID: {request.bookingId}</p>
          <Summary request={request} amount={amount} />
          <p className="mt-4 text-sm text-sage-600">
            Cancellation policy: details being verified with the partner.
          </p>
          <p className="mt-2 text-sm text-sage-600">
            What happens next: you’ll receive programme arrival guidance from Healingram and the
            retreat.
          </p>
          <Link
            to="/dashboard"
            className="mt-6 inline-flex rounded-xl bg-sage-800 px-5 py-3 text-sm font-semibold text-white"
          >
            My Trips
          </Link>
        </>
      )}

      {(request.status === "CANCELLED" || request.status === "COMPLETED") && (
        <>
          <h1 className="font-display text-2xl font-bold text-sage-800">Request {request.status}</h1>
          <Summary request={request} amount={amount} />
        </>
      )}

      {!["PAYMENT_PENDING", "ALTERNATIVE_PROPOSED", "REJECTED", "CONFIRMED"].includes(
        request.status,
      ) &&
        request.status === "REQUESTED" && (
          <div className="mt-6">
            <Summary request={request} amount={amount} />
          </div>
        )}

      <button
        type="button"
        className="mt-10 text-sm text-sage-500 hover:text-sage-700"
        onClick={() => navigate(-1)}
      >
        ← Back
      </button>
    </div>
  );
}

function Summary({
  request,
  amount,
}: {
  request: AvailabilityRequest;
  amount: string;
}) {
  return (
    <div className="mt-6 rounded-2xl border border-sand-200 bg-white p-5 text-sm text-sage-700 space-y-2">
      <p className="font-semibold text-sage-800">{request.retreatName}</p>
      <p>{request.programmeName}</p>
      <p>
        {formatDisplayDate(request.checkIn)} → {formatDisplayDate(request.checkOut)}
      </p>
      <p>
        {request.guests} guest{request.guests === 1 ? "" : "s"} · {request.roomType}
      </p>
      <p className="pt-2 text-lg font-semibold text-sage-800">{amount} total</p>
      <p className="text-xs text-sage-500">
        Price snapshot at request: {request.priceSnapshot.label} (captured{" "}
        {new Date(request.priceSnapshot.capturedAt).toLocaleString("en-IN")})
      </p>
    </div>
  );
}
