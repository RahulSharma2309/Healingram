import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { GuestVerifyForm } from "../../components/account/GuestVerifyForm";
import { fetchMyAvailabilityRequests } from "../../lib/api/availability";
import { applyAuthUser, hasRequestSession, isGuestAccountStatus, isLoggedIn } from "../../lib/auth";
import {
  mergeServerAvailability,
  type AvailabilityRequest,
} from "../../lib/availabilityRequests";
import { formatDisplayDate } from "../../lib/pricing";

export function MyRequest() {
  const { requestId } = useParams();
  const navigate = useNavigate();
  const [phase, setPhase] = useState<"verify" | "loading" | "list" | "empty">(
    isLoggedIn() || hasRequestSession() ? "loading" : "verify",
  );
  const [requests, setRequests] = useState<AvailabilityRequest[]>([]);

  const loadMine = async () => {
    setPhase("loading");
    try {
      const items = await fetchMyAvailabilityRequests();
      const mapped = items.map(mergeServerAvailability);
      setRequests(mapped);
      if (requestId) {
        const match = mapped.find((item) => item.requestId === requestId);
        if (match) {
          navigate(`/requests/${match.requestId}`, { replace: true });
          return;
        }
      }
      setPhase(mapped.length > 0 ? "list" : "empty");
    } catch {
      setPhase("verify");
    }
  };

  useEffect(() => {
    if (isLoggedIn()) {
      navigate("/dashboard?tab=requests", { replace: true });
      return;
    }
    if (hasRequestSession()) {
      void loadMine();
    }
  }, [requestId, navigate]);

  if (phase === "verify") {
    return (
      <div className="max-w-md mx-auto px-4 py-16">
        <h1 className="font-display text-2xl font-bold text-sage-800 text-center">Let’s verify it’s you</h1>
        <p className="mt-2 text-sm text-sage-600 text-center">
          Enter the email or mobile number you used for your request. We’ll send a verification code.
        </p>
        <GuestVerifyForm
          publicId={requestId}
          onResolved={(session) => {
            if (!session) {
              setRequests([]);
              setPhase("empty");
              return;
            }
            applyAuthUser(session.user);
            if (!isGuestAccountStatus(session.user.accountStatus)) {
              navigate("/dashboard?tab=requests", { replace: true });
              return;
            }
            void loadMine();
          }}
        />
      </div>
    );
  }

  if (phase === "loading") {
    return (
      <div className="max-w-md mx-auto px-4 py-16 text-center">
        <p className="text-sm text-sage-600">Looking up your requests…</p>
      </div>
    );
  }

  if (phase === "empty") {
    return (
      <div className="max-w-md mx-auto px-4 py-16 text-center">
        <h1 className="font-display text-2xl font-bold text-sage-800">No request for now</h1>
        <p className="mt-3 text-sm text-sage-600">Do you want to explore retreats?</p>
        <Link
          to="/"
          className="mt-8 inline-flex rounded-xl bg-teal-600 px-5 py-3 text-sm font-semibold text-white hover:bg-teal-500"
        >
          Explore retreats
        </Link>
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto px-4 py-10">
      <h1 className="font-display text-2xl font-bold text-sage-800">My Request</h1>
      <p className="mt-2 text-sm text-sage-600">Requests linked to the email or mobile you just verified.</p>
      <div className="mt-8 space-y-3">
        {requests.map((request) => (
          <div
            key={request.requestId}
            className="rounded-2xl border border-sand-200 bg-white p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-3"
          >
            <div>
              <p className="font-mono text-xs text-teal-700">{request.requestId}</p>
              <p className="font-medium text-sage-800">{request.retreatName}</p>
              <p className="text-sm text-sage-500">
                {request.programmeName}
                {request.checkIn
                  ? ` · ${formatDisplayDate(request.checkIn)} → ${formatDisplayDate(request.checkOut)}`
                  : ""}
              </p>
              <p className="text-sm text-teal-700 mt-1">{request.status}</p>
            </div>
            <div className="flex flex-col items-start sm:items-end gap-2">
              <Link to={`/requests/${request.requestId}`} className="text-sm font-semibold text-teal-600">
                View request
              </Link>
              {request.status === "PAYMENT_PENDING" ? (
                <Link
                  to={`/signup?next=${encodeURIComponent(`/requests/${request.requestId}/payment`)}`}
                  className="text-sm font-semibold text-teal-700"
                >
                  Yes, I want to book
                </Link>
              ) : null}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
