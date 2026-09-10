import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Calendar, Heart, User } from "lucide-react";
import {
  listAvailabilityRequests,
  listCustomerTrips,
  type AvailabilityRequest,
} from "../../lib/availabilityRequests";
import { formatDisplayDate } from "../../lib/pricing";
import { formatInr } from "../../data/programmePricing";
import { getCustomerProfile, isLoggedIn } from "../../lib/auth";

type Tab = "trips" | "requests" | "wishlist" | "profile";

export function UserDashboard() {
  const [tab, setTab] = useState<Tab>("requests");
  const [requests, setRequests] = useState<AvailabilityRequest[]>([]);
  const [trips, setTrips] = useState<AvailabilityRequest[]>([]);

  useEffect(() => {
    const refresh = () => {
      const all = listAvailabilityRequests();
      const profile = getCustomerProfile();
      const mine = isLoggedIn()
        ? all.filter(
            (r) =>
              (profile.email &&
                r.customerEmail.toLowerCase() === profile.email.toLowerCase()) ||
              r.customerName === profile.name,
          )
        : all;
      setRequests(mine);
      setTrips(
        listCustomerTrips().filter((r) =>
          mine.some((m) => m.requestId === r.requestId),
        ),
      );
    };
    refresh();
    window.addEventListener("healingram-requests", refresh);
    return () => window.removeEventListener("healingram-requests", refresh);
  }, []);

  const tabs: { id: Tab; label: string; icon: typeof Calendar }[] = [
    { id: "requests", label: "Requests", icon: Calendar },
    { id: "trips", label: "My Trips", icon: Calendar },
    { id: "wishlist", label: "Wishlist", icon: Heart },
    { id: "profile", label: "Profile", icon: User },
  ];

  return (
    <div className="max-w-5xl mx-auto px-4 py-10">
      <h1 className="font-display text-2xl font-bold text-sage-800 mb-6">My dashboard</h1>
      <div className="flex gap-2 mb-8 border-b border-sand-200 overflow-x-auto">
        {tabs.map(({ id, label, icon: Icon }) => (
          <button
            key={id}
            type="button"
            onClick={() => setTab(id)}
            className={`flex items-center gap-2 px-4 py-3 text-sm font-medium border-b-2 -mb-px whitespace-nowrap ${
              tab === id ? "border-teal-600 text-teal-600" : "border-transparent text-gray-500"
            }`}
          >
            <Icon className="w-4 h-4" /> {label}
          </button>
        ))}
      </div>

      {tab === "requests" && (
        <div className="bg-white rounded-xl border border-sand-200 p-6 space-y-3">
          <h2 className="font-semibold mb-2">Availability requests</h2>
          {requests.length === 0 && (
            <p className="text-sm text-sage-500">No requests yet. Check availability on a retreat listing.</p>
          )}
          {requests.map((r) => (
            <div
              key={r.requestId}
              className="border border-sand-200 rounded-lg p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3"
            >
              <div>
                <p className="font-mono text-xs text-teal-700">{r.requestId}</p>
                <p className="font-medium">{r.retreatName}</p>
                <p className="text-sm text-gray-500">
                  {r.programmeName} · {formatDisplayDate(r.checkIn)} →{" "}
                  {formatDisplayDate(r.checkOut)}
                </p>
                <p className="text-sm text-teal-700 mt-1">{r.status}</p>
              </div>
              <Link
                to={`/requests/${r.requestId}`}
                className="text-sm font-semibold text-teal-600"
              >
                View request
              </Link>
            </div>
          ))}
        </div>
      )}

      {tab === "trips" && (
        <div className="bg-white rounded-xl border border-sand-200 p-6 space-y-3">
          <h2 className="font-semibold mb-2">My Trips</h2>
          {trips.length === 0 && (
            <p className="text-sm text-sage-500">
              Confirmed and payment-ready stays appear here after the retreat responds.
            </p>
          )}
          {trips.map((r) => (
            <div
              key={r.requestId}
              className="border border-sand-200 rounded-lg p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3"
            >
              <div>
                <p className="font-medium">{r.retreatName}</p>
                <p className="text-sm text-gray-500">
                  {formatDisplayDate(r.checkIn)} → {formatDisplayDate(r.checkOut)}
                </p>
                <p className="text-sm text-teal-700 mt-1">
                  {r.status}
                  {r.bookingId ? ` · ${r.bookingId}` : ""}
                </p>
              </div>
              <div className="text-right">
                <p className="font-semibold">
                  {r.finalPayableAmount != null
                    ? formatInr(r.finalPayableAmount)
                    : r.displayedPrice}
                </p>
                <Link to={`/requests/${r.requestId}`} className="text-sm text-teal-600">
                  View details
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}

      {tab === "wishlist" && (
        <p className="text-sm text-sage-500">Wishlist — coming in a later pass.</p>
      )}

      {tab === "profile" && (
        <div className="bg-white rounded-xl border border-sand-200 p-6 text-sm text-sage-700">
          <p>Name: {getCustomerProfile().name || "—"}</p>
          <p className="mt-1">Email: {getCustomerProfile().email || "—"}</p>
          <p className="mt-1">
            Phone: {getCustomerProfile().countryCode} {getCustomerProfile().phone || "—"}
          </p>
        </div>
      )}
    </div>
  );
}
