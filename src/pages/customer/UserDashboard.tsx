import { useEffect, useState } from "react";
import { Link, Navigate, useSearchParams } from "react-router-dom";
import { Calendar, Heart, User } from "lucide-react";
import { fetchTrips } from "../../lib/api/account";
import {
  refreshMyRequests,
  toAvailabilityRequest,
  type AvailabilityRequest,
} from "../../lib/availabilityRequests";
import { formatDisplayDate } from "../../lib/pricing";
import { formatInr } from "../../lib/money";
import { usePublishedRetreats } from "../../lib/api/usePublishedRetreats";
import { isLoggedIn } from "../../lib/auth";
import { ProfileDetails } from "./ProfileDetails";
import { hydrateWishlistFromServer, listWishlistSlugs, subscribeWishlist, toggleWishlist } from "../../lib/wishlist";

type Tab = "trips" | "requests" | "wishlist" | "profile";

function tabFromQuery(value: string | null): Tab {
  if (value === "trips" || value === "requests" || value === "wishlist" || value === "profile") {
    return value;
  }
  return "requests";
}

export function UserDashboard() {
  const { byId } = usePublishedRetreats();
  const [searchParams] = useSearchParams();
  const [tab, setTab] = useState<Tab>(() => tabFromQuery(searchParams.get("tab")));
  const [requests, setRequests] = useState<AvailabilityRequest[]>([]);

  useEffect(() => {
    setTab(tabFromQuery(searchParams.get("tab")));
  }, [searchParams]);
  const [trips, setTrips] = useState<AvailabilityRequest[]>([]);
  const [wishSlugs, setWishSlugs] = useState<string[]>([]);
  const [requestError, setRequestError] = useState<string | null>(null);
  const [tripError, setTripError] = useState<string | null>(null);
  const [wishlistError, setWishlistError] = useState<string | null>(null);

  useEffect(() => {
    if (!isLoggedIn()) {
      return;
    }
    void hydrateWishlistFromServer()
      .then(() => {
        setWishSlugs(listWishlistSlugs());
        setWishlistError(null);
      })
      .catch(() => setWishlistError("Could not load your wishlist."));
    void refreshMyRequests()
      .then((items) => {
        setRequests(items);
        setRequestError(null);
      })
      .catch(() => setRequestError("Could not load your requests."));
    void fetchTrips()
      .then((groups) => {
        const cards = [
          ...groups.paymentPending,
          ...groups.upcoming,
          ...groups.completed,
          ...groups.cancelled,
        ];
        setTrips(
          cards.map((card) =>
            toAvailabilityRequest({
              publicId: card.publicId,
              status: card.status,
              retreatSlug: card.retreatSlug,
              programmeSlug: card.programmeSlug,
              requestedAt: card.requestedAt,
              finalAmountInr: card.finalAmountInr,
            }),
          ),
        );
        setTripError(null);
      })
      .catch(() => setTripError("Could not load your trips."));
    const unsubWish = subscribeWishlist(() => setWishSlugs(listWishlistSlugs()));
    return () => {
      unsubWish();
    };
  }, []);

  if (!isLoggedIn()) {
    return <Navigate to="/login" replace />;
  }

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
          <Link
            key={id}
            to={`/dashboard?tab=${id}`}
            className={`flex items-center gap-2 px-4 py-3 text-sm font-medium border-b-2 -mb-px whitespace-nowrap ${
              tab === id ? "border-teal-600 text-teal-600" : "border-transparent text-gray-500"
            }`}
          >
            <Icon className="w-4 h-4" /> {label}
          </Link>
        ))}
      </div>

      {tab === "requests" && (
        <div className="bg-white rounded-xl border border-sand-200 p-6 space-y-3">
          <h2 className="font-semibold mb-2">Availability requests</h2>
          {requestError && <p className="text-sm text-red-700">{requestError}</p>}
          {!requestError && requests.length === 0 && (
            <p className="text-sm text-sage-500">No requests yet. Check availability on a retreat listing.</p>
          )}
          {requests.map((r) => (
            <div
              key={r.requestId}
              className="border border-sand-200 rounded-lg p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3"
            >
              <div>
                <p className="font-mono text-xs text-teal-700">{r.requestId}</p>
                <p className="font-medium">{r.retreatName ?? r.retreatId}</p>
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
          {tripError && <p className="text-sm text-red-700">{tripError}</p>}
          {!tripError && trips.length === 0 && (
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
                <p className="font-medium">{r.retreatName ?? r.retreatId}</p>
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
        <div className="bg-white rounded-xl border border-sand-200 p-6 space-y-3">
          <h2 className="font-semibold mb-2">Wishlist</h2>
          {wishlistError ? <p className="text-sm text-red-700">{wishlistError}</p> : null}
          {!wishlistError && wishSlugs.length === 0 && (
            <p className="text-sm text-sage-500">
              Save retreats from the browse page. Sign in to keep them after you change browsers.
            </p>
          )}
          {wishSlugs.map((slug) => {
            const retreat = byId.get(slug);
            return (
              <div
                key={slug}
                className="border border-sand-200 rounded-lg p-4 flex items-center justify-between gap-3"
              >
                <div>
                  <p className="font-medium">{retreat?.name ?? slug}</p>
                  {retreat && (
                    <p className="text-sm text-gray-500">
                      {retreat.locality}
                      {retreat.stateLabel ? `, ${retreat.stateLabel}` : ""}
                    </p>
                  )}
                </div>
                <div className="flex items-center gap-3">
                  <Link to={`/retreats/${slug}`} className="text-sm text-teal-600 font-semibold">
                    View
                  </Link>
                  <button
                    type="button"
                    className="text-sm text-sage-600"
                    onClick={() => void toggleWishlist(slug)}
                  >
                    Remove
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {tab === "profile" && <ProfileDetails />}
    </div>
  );
}
