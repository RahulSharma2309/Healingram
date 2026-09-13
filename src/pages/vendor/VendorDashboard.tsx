import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { fetchPartnerQueue } from "../../lib/api/availability";
import {
  isAgingRequest,
  listAvailabilityRequests,
  markPartnerViewed,
  mergeServerAvailabilityList,
  partnerConfirmAvailability,
  partnerMarkUnavailable,
  partnerSuggestAlternative,
  requestAgeLabel,
  type AvailabilityRequest,
} from "../../lib/availabilityRequests";
import { formatDisplayDate } from "../../lib/pricing";
import { formatInr } from "../../data/programmePricing";
import { addNights } from "../../lib/pricing";

export function VendorDashboard() {
  const [requests, setRequests] = useState<AvailabilityRequest[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [action, setAction] = useState<"confirm" | "alternative" | null>(null);
  const [queueError, setQueueError] = useState<string | null>(null);

  const refresh = async () => {
    setRequests(listAvailabilityRequests());
    try {
      mergeServerAvailabilityList(await fetchPartnerQueue());
      setRequests(listAvailabilityRequests());
      setQueueError(null);
    } catch {
      setQueueError("Could not load the partner queue from the server. Sign in as the partner on this browser.");
    }
  };

  useEffect(() => {
    refresh();
    const onChange = () => refresh();
    window.addEventListener("healingram-requests", onChange);
    return () => window.removeEventListener("healingram-requests", onChange);
  }, []);

  const pending = useMemo(
    () => requests.filter((r) => r.status === "REQUESTED" || r.status === "ALTERNATIVE_PROPOSED"),
    [requests],
  );

  const active = requests.find((r) => r.requestId === activeId) ?? null;

  return (
    <div className="space-y-8">
      <div>
        <h1 className="font-display text-2xl font-bold text-sage-800">Partner dashboard</h1>
        <p className="text-sm text-sage-600 mt-1">Availability requests for launch retreats</p>
      </div>

      <section id="availability-requests" className="bg-white rounded-xl border border-sand-200 p-6">
        <h2 className="font-semibold text-sage-800 mb-4">Pending Availability Requests</h2>
        {queueError ? <p className="text-sm text-red-700 mb-3">{queueError}</p> : null}
        {pending.length === 0 ? (
          <p className="text-sm text-sage-500">No pending requests.</p>
        ) : (
          <div className="space-y-3">
            {pending.map((r) => (
              <div
                key={r.requestId}
                className={`rounded-xl border p-4 ${
                  isAgingRequest(r) ? "border-amber-300 bg-amber-50/50" : "border-sand-200"
                }`}
              >
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div className="text-sm space-y-1">
                    <p className="font-mono text-xs text-teal-700">{r.requestId}</p>
                    <p className="font-medium text-sage-800">{r.customerName}</p>
                    <p>
                      {r.programmeName} · {formatDisplayDate(r.checkIn)} →{" "}
                      {formatDisplayDate(r.checkOut)}
                    </p>
                    <p>
                      {r.guests} guest{r.guests === 1 ? "" : "s"} · {r.occupancy} · {r.roomType}
                    </p>
                    <p>
                      Requested price: {r.displayedPrice}
                      {isAgingRequest(r) && (
                        <span className="ml-2 text-amber-700 text-xs font-medium">Aging</span>
                      )}
                      <span className="ml-2 text-sage-500">· {requestAgeLabel(r.requestedAt)}</span>
                    </p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <button
                      type="button"
                      className="px-3 py-1.5 rounded-lg bg-teal-600 text-white text-xs font-semibold"
                      onClick={() => {
                        markPartnerViewed(r.requestId);
                        setActiveId(r.requestId);
                        setAction("confirm");
                      }}
                    >
                      Confirm Availability
                    </button>
                    <button
                      type="button"
                      className="px-3 py-1.5 rounded-lg border border-sand-200 text-xs font-semibold"
                      onClick={() => {
                        markPartnerViewed(r.requestId);
                        setActiveId(r.requestId);
                        setAction("alternative");
                      }}
                    >
                      Suggest Alternative
                    </button>
                    <button
                      type="button"
                      className="px-3 py-1.5 rounded-lg border border-sand-200 text-xs font-semibold text-red-700"
                      onClick={() => {
                        void partnerMarkUnavailable(r.requestId)
                          .then(() => {
                            setQueueError(null);
                            refresh();
                            setActiveId(null);
                          })
                          .catch(() => {
                            setQueueError("Could not mark unavailable on the server. Stay signed in as the partner.");
                          });
                      }}
                    >
                      Unavailable
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      {active && action === "confirm" && (
        <ConfirmForm
          request={active}
          onDone={() => {
            setActiveId(null);
            setAction(null);
            refresh();
          }}
          onCancel={() => {
            setActiveId(null);
            setAction(null);
          }}
        />
      )}

      {active && action === "alternative" && (
        <AlternativeForm
          request={active}
          onDone={() => {
            setActiveId(null);
            setAction(null);
            refresh();
          }}
          onCancel={() => {
            setActiveId(null);
            setAction(null);
          }}
        />
      )}

      <section className="bg-white rounded-xl border border-sand-200 p-6">
        <h2 className="font-semibold mb-3">Recent requests</h2>
        <ul className="text-sm space-y-2">
          {requests.slice(0, 8).map((r) => (
            <li key={r.requestId} className="flex justify-between gap-3 border-b border-sand-100 py-2">
              <span>
                {r.requestId} · {r.retreatName} · {r.status}
              </span>
              <Link to={`/requests/${r.requestId}`} className="text-teal-600 text-xs">
                View
              </Link>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}

function ConfirmForm({
  request,
  onDone,
  onCancel,
}: {
  request: AvailabilityRequest;
  onDone: () => void;
  onCancel: () => void;
}) {
  const [finalAmount, setFinalAmount] = useState(
    String(request.finalPayableAmount ?? request.priceSnapshot.totalAmount ?? ""),
  );
  const [taxesNote, setTaxesNote] = useState(
    request.priceSnapshot.taxDisplay === "included"
      ? "Taxes included"
      : "Taxes not yet confirmed",
  );
  const [inclusionsNote, setInclusionsNote] = useState("Programme inclusions as discussed");
  const [roomType, setRoomType] = useState(request.roomType);
  const [occupancy, setOccupancy] = useState(request.occupancy);

  return (
    <section className="bg-white rounded-xl border border-teal-200 p-6 space-y-3">
      <h3 className="font-semibold text-sage-800">Confirm Availability — {request.requestId}</h3>
      <p className="text-sm text-sage-600">
        {request.programmeName} · {formatDisplayDate(request.checkIn)} →{" "}
        {formatDisplayDate(request.checkOut)}
      </p>
      <label className="block text-sm">
        Room / occupancy
        <input
          className="mt-1 w-full border rounded-lg px-3 py-2"
          value={roomType}
          onChange={(e) => setRoomType(e.target.value)}
        />
      </label>
      <label className="block text-sm">
        Occupancy label
        <input
          className="mt-1 w-full border rounded-lg px-3 py-2"
          value={occupancy}
          onChange={(e) => setOccupancy(e.target.value)}
        />
      </label>
      <label className="block text-sm">
        Final payable amount (INR)
        <input
          type="number"
          className="mt-1 w-full border rounded-lg px-3 py-2"
          value={finalAmount}
          onChange={(e) => setFinalAmount(e.target.value)}
          required
        />
      </label>
      <label className="block text-sm">
        Taxes
        <input
          className="mt-1 w-full border rounded-lg px-3 py-2"
          value={taxesNote}
          onChange={(e) => setTaxesNote(e.target.value)}
        />
      </label>
      <label className="block text-sm">
        Inclusions note
        <input
          className="mt-1 w-full border rounded-lg px-3 py-2"
          value={inclusionsNote}
          onChange={(e) => setInclusionsNote(e.target.value)}
        />
      </label>
      <div className="flex gap-2 pt-2">
        <button
          type="button"
          className="px-4 py-2 bg-teal-600 text-white rounded-lg text-sm font-semibold"
          onClick={() => {
            const amount = Number(finalAmount);
            if (!amount || amount <= 0) return;
            void partnerConfirmAvailability(request.requestId, {
              programmeId: request.programmeId,
              programmeName: request.programmeName,
              checkIn: request.checkIn,
              checkOut: request.checkOut,
              durationNights: request.durationNights,
              guests: request.guests,
              occupancy,
              roomType,
              finalAmount: amount,
              taxesNote,
              inclusionsNote,
            })
              .then((updated) => {
                if (updated) onDone();
              })
              .catch(() => {
                window.alert("Confirm failed on the server. Sign in as partner or admin and try again.");
              });
          }}
        >
          Confirm & send payment-ready
        </button>
        <button type="button" className="px-4 py-2 border rounded-lg text-sm" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </section>
  );
}

function AlternativeForm({
  request,
  onDone,
  onCancel,
}: {
  request: AvailabilityRequest;
  onDone: () => void;
  onCancel: () => void;
}) {
  const [checkIn, setCheckIn] = useState(request.checkIn);
  const [nights, setNights] = useState(String(request.durationNights));
  const [roomType, setRoomType] = useState(request.roomType);
  const [finalAmount, setFinalAmount] = useState(
    String(request.finalPayableAmount ?? request.priceSnapshot.totalAmount ?? ""),
  );

  const checkOut = checkIn && nights ? addNights(checkIn, Number(nights)) : "";

  return (
    <section className="bg-white rounded-xl border border-amber-200 p-6 space-y-3">
      <h3 className="font-semibold text-sage-800">Suggest Alternative — {request.requestId}</h3>
      <label className="block text-sm">
        Proposed check-in
        <input
          type="date"
          className="mt-1 w-full border rounded-lg px-3 py-2"
          value={checkIn}
          onChange={(e) => setCheckIn(e.target.value)}
        />
      </label>
      <label className="block text-sm">
        Duration (nights)
        <input
          type="number"
          className="mt-1 w-full border rounded-lg px-3 py-2"
          value={nights}
          onChange={(e) => setNights(e.target.value)}
        />
      </label>
      <p className="text-sm text-sage-600">Checkout: {checkOut ? formatDisplayDate(checkOut) : "—"}</p>
      <label className="block text-sm">
        Room / occupancy
        <input
          className="mt-1 w-full border rounded-lg px-3 py-2"
          value={roomType}
          onChange={(e) => setRoomType(e.target.value)}
        />
      </label>
      <label className="block text-sm">
        Updated price (INR)
        <input
          type="number"
          className="mt-1 w-full border rounded-lg px-3 py-2"
          value={finalAmount}
          onChange={(e) => setFinalAmount(e.target.value)}
        />
      </label>
      <div className="flex gap-2 pt-2">
        <button
          type="button"
          className="px-4 py-2 bg-amber-700 text-white rounded-lg text-sm font-semibold"
          onClick={() => {
            const amount = Number(finalAmount);
            const durationNights = Number(nights);
            if (!checkIn || !checkOut || !durationNights) return;
            void partnerSuggestAlternative(request.requestId, {
              programmeId: request.programmeId,
              programmeName: request.programmeName,
              checkIn,
              checkOut,
              durationNights,
              guests: request.guests,
              occupancy: request.occupancy,
              roomType,
              finalAmount: amount > 0 ? amount : null,
              taxesNote: "Taxes not yet confirmed",
              inclusionsNote: "",
            })
              .then((updated) => {
                if (updated) onDone();
              })
              .catch(() => {
                window.alert("Could not send the alternative. Sign in as partner or admin and try again.");
              });
          }}
        >
          Send alternative
        </button>
        <button type="button" className="px-4 py-2 border rounded-lg text-sm" onClick={onCancel}>
          Cancel
        </button>
      </div>
      {finalAmount && Number(finalAmount) > 0 && (
        <p className="text-xs text-sage-500">Proposed total {formatInr(Number(finalAmount))}</p>
      )}
    </section>
  );
}
