import { Link, useLocation, useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { hasRequestSession, isLoggedIn } from "../../lib/auth";
import {
  fetchRequest,
  type AvailabilityRequest,
} from "../../lib/availabilityRequests";
import { formatDisplayDate } from "../../lib/pricing";

export function AvailabilityRequestReceived() {
  const { requestId } = useParams();
  const location = useLocation();
  const fromCreate = (location.state as { request?: AvailabilityRequest } | null)?.request;
  const [request, setRequest] = useState<AvailabilityRequest | undefined>(
    fromCreate && fromCreate.requestId === requestId ? fromCreate : undefined,
  );
  const [phase, setPhase] = useState<"ready" | "loading" | "error">(
    fromCreate && fromCreate.requestId === requestId ? "ready" : "loading",
  );

  useEffect(() => {
    if (!requestId) {
      setPhase("error");
      return;
    }
    if (fromCreate && fromCreate.requestId === requestId) {
      setRequest(fromCreate);
      setPhase("ready");
    }
    if (!hasRequestSession() && !fromCreate) {
      setPhase("error");
      return;
    }
    if (!hasRequestSession()) {
      return;
    }
    void fetchRequest(requestId)
      .then((item) => {
        setRequest(item);
        setPhase("ready");
      })
      .catch(() => {
        if (!fromCreate) setPhase("error");
      });
  }, [requestId, fromCreate]);

  if (phase === "loading") {
    return (
      <div className="max-w-lg mx-auto px-4 py-16 text-center">
        <p className="text-sm text-sage-600">Loading your request…</p>
      </div>
    );
  }

  if (phase === "error" || !request) {
    return (
      <div className="max-w-lg mx-auto px-4 py-16 text-center">
        <h1 className="font-display text-2xl font-bold text-sage-800">Could not load this request</h1>
        <p className="mt-3 text-sm text-sage-600">
          Verify it’s you to view a guest request, or sign in if this belongs to your account.
        </p>
        {requestId ? (
          <Link
            to={`/requests/${requestId}/verify`}
            className="mt-6 inline-block text-teal-600 font-medium"
          >
            Verify it’s you
          </Link>
        ) : (
          <Link to="/search" className="mt-6 inline-block text-teal-600 font-medium">
            Explore More Retreats
          </Link>
        )}
      </div>
    );
  }

  return (
    <div className="max-w-lg mx-auto px-4 py-12">
      <h1 className="font-display text-2xl md:text-3xl font-bold text-sage-800 text-balance">
        Availability request received
      </h1>
      <p className="mt-2 font-mono text-sm text-teal-700">Request ID: {request.requestId}</p>

      <div className="mt-8 rounded-2xl border border-sand-200 bg-white p-5 space-y-2 text-sm text-sage-700">
        <p>
          <span className="text-sage-500">Retreat</span>
          <br />
          <span className="font-medium text-sage-800">{request.retreatName ?? request.retreatId}</span>
        </p>
        <p>
          <span className="text-sage-500">Programme</span>
          <br />
          <span className="font-medium text-sage-800">{request.programmeName ?? request.programmeId}</span>
        </p>
        <p>
          <span className="text-sage-500">Dates</span>
          <br />
          <span className="font-medium text-sage-800">
            {formatDisplayDate(request.checkIn)} → {formatDisplayDate(request.checkOut)}
          </span>
        </p>
        <p>
          <span className="text-sage-500">Guests</span>
          <br />
          <span className="font-medium text-sage-800">{request.guests ?? "—"}</span>
        </p>
      </div>

      <p className="mt-6 text-sm text-sage-600 leading-relaxed">
        We’ll notify you when the retreat responds. No payment is required yet. Current status: waiting for
        availability.
      </p>

      <div className="mt-8 flex flex-col sm:flex-row gap-3">
        <Link
          to={
            isLoggedIn() || hasRequestSession()
              ? `/requests/${request.requestId}`
              : `/requests/${request.requestId}/verify`
          }
          className="inline-flex justify-center rounded-xl bg-teal-600 px-5 py-3 text-sm font-semibold text-white hover:bg-teal-500"
        >
          View My Request
        </Link>
        <Link
          to="/search"
          className="inline-flex justify-center rounded-xl border border-sand-200 px-5 py-3 text-sm font-semibold text-sage-800 hover:bg-sand-50"
        >
          Explore More Retreats
        </Link>
      </div>
    </div>
  );
}
