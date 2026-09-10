import { Link, useParams } from "react-router-dom";
import { getAvailabilityRequest } from "../../lib/availabilityRequests";
import { formatDisplayDate } from "../../lib/pricing";

export function AvailabilityRequestReceived() {
  const { requestId } = useParams();
  const request = requestId ? getAvailabilityRequest(requestId) : undefined;

  if (!request) {
    return (
      <div className="max-w-lg mx-auto px-4 py-16 text-center">
        <h1 className="font-display text-2xl font-bold text-sage-800">Request not found</h1>
        <Link to="/search" className="mt-6 inline-block text-teal-600 font-medium">
          Explore More Retreats
        </Link>
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
          <span className="font-medium text-sage-800">{request.retreatName}</span>
        </p>
        <p>
          <span className="text-sage-500">Programme</span>
          <br />
          <span className="font-medium text-sage-800">{request.programmeName}</span>
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
          <span className="font-medium text-sage-800">{request.guests}</span>
        </p>
      </div>

      <p className="mt-6 text-sm text-sage-600 leading-relaxed">
        We’re confirming your programme and dates with the retreat. No payment is required yet.
      </p>

      <div className="mt-8 flex flex-col sm:flex-row gap-3">
        <Link
          to={`/requests/${request.requestId}`}
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
