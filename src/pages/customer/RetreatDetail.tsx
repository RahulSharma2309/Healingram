import { useEffect, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import {
  RetreatListingComponent1,
  type FindMyMatchListingState,
} from "../../components/retreat/RetreatListingComponent1";
import { RetreatListingComponent2 } from "../../components/retreat/RetreatListingComponent2";
import { type RetreatListingView } from "../../lib/listingTypes";
import { ExpertReferralBridge } from "../../components/ExpertReferralBridge";
import { ListingPlanProvider } from "../../lib/listingPlanContext";
import { fetchRetreatListing, type RetreatListing } from "../../lib/api/catalog";
import { ApiError } from "../../lib/api/client";
import {
  excludedLabels,
  hasSectionItems,
  includedLabels,
  mapRetreatListingToView,
} from "../../lib/api/listing";

type PageStatus = "loading" | "ready" | "unpublished" | "missing" | "error";

type PageState = {
  status: PageStatus;
  listing: RetreatListingView | null;
  dto: RetreatListing | null;
  source: "api" | null;
};

const INITIAL: PageState = { status: "loading", listing: null, dto: null, source: null };

/**
 * Retreat listing page.
 * Renders GET /api/catalog/retreats/{slug}. Empty API sections stay empty.
 */
export function RetreatDetail() {
  const { id } = useParams();
  const location = useLocation();
  const findMyMatch = (location.state as { findMyMatch?: FindMyMatchListingState } | null)
    ?.findMyMatch;
  const [page, setPage] = useState<PageState>(INITIAL);

  useEffect(() => {
    if (!id) {
      setPage({ status: "missing", listing: null, dto: null, source: null });
      return;
    }

    let cancelled = false;
    setPage(INITIAL);

    fetchRetreatListing(id)
      .then((dto) => {
        if (cancelled) return;
        setPage({
          status: "ready",
          listing: mapRetreatListingToView(dto),
          dto,
          source: "api",
        });
      })
      .catch((error) => {
        if (cancelled) return;
        if (error instanceof ApiError && error.status === 404) {
          setPage({ status: "unpublished", listing: null, dto: null, source: null });
          return;
        }
        setPage({ status: "error", listing: null, dto: null, source: null });
      });

    return () => {
      cancelled = true;
    };
  }, [id]);

  if (page.status === "loading") {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center">
        <p className="text-sage-600">Loading this retreat…</p>
      </div>
    );
  }

  if (page.status === "error") {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center">
        <h1 className="font-display text-2xl font-bold text-sage-800 mb-3">
          Could not load this retreat
        </h1>
        <p className="text-sage-600 mb-6">
          The listing comes from the catalog API. Start the gateway and API, then try again.
        </p>
        <Link to="/retreats" className="text-sm font-semibold text-teal-600 hover:text-teal-700">
          Explore retreats
        </Link>
      </div>
    );
  }

  if (page.status === "unpublished" || page.status === "missing" || !page.listing) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center">
        <h1 className="font-display text-2xl font-bold text-sage-800 mb-3">
          Retreat not found
        </h1>
        <p className="text-sage-600 mb-6">
          This property isn’t in Healingram’s current launch inventory.
        </p>
        <Link to="/retreats" className="text-sm font-semibold text-teal-600 hover:text-teal-700">
          Explore retreats
        </Link>
      </div>
    );
  }

  const listing = page.listing;
  const dto = page.dto;
  const retreatId = listing.retreat.id;
  const apiExperts = hasSectionItems(dto?.experts) ? dto!.experts! : [];
  const apiTestimonials = hasSectionItems(dto?.testimonials) ? dto!.testimonials! : [];
  const apiRooms = hasSectionItems(dto?.rooms) ? dto!.rooms! : [];
  const apiIncluded = includedLabels(dto?.inclusions);
  const apiExcluded = excludedLabels(dto?.inclusions);

  return (
    <ListingPlanProvider>
      <ExpertReferralBridge
        retreatId={retreatId}
        retreatName={listing.retreat.name}
      />
      <div className="max-w-7xl mx-auto px-4 py-8 pb-28 lg:pb-12">
        <RetreatListingComponent1 listing={listing} />

        {findMyMatch && (
          <RetreatListingComponent2 retreatId={retreatId} findMyMatch={findMyMatch} />
        )}

        {apiExperts.length > 0 && <ApiExpertsSection experts={apiExperts} />}
        {apiTestimonials.length > 0 && <ApiTestimonialsSection stories={apiTestimonials} />}
        {apiRooms.length > 0 && <ApiRoomsSection rooms={apiRooms} />}
        {(apiIncluded.length > 0 || apiExcluded.length > 0) && (
          <ApiInclusionsSection included={apiIncluded} excluded={apiExcluded} />
        )}
      </div>
    </ListingPlanProvider>
  );
}

function ApiExpertsSection({
  experts,
}: {
  experts: NonNullable<RetreatListing["experts"]>;
}) {
  return (
    <section className="mt-14 pt-12 border-t border-sand-200" aria-labelledby="listing-api-experts">
      <h2
        id="listing-api-experts"
        className="font-display text-2xl md:text-3xl font-semibold text-sage-800 text-balance"
      >
        Meet the people guiding your stay
      </h2>
      <ul className="mt-8 grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
        {experts.map((expert, index) => (
          <li
            key={`${expert.name}-${index}`}
            className="rounded-2xl border border-sand-200 bg-white p-5"
          >
            <h3 className="font-display text-xl font-semibold text-sage-800">{expert.name}</h3>
            {expert.role && <p className="mt-1 text-sm font-medium text-sage-700">{expert.role}</p>}
          </li>
        ))}
      </ul>
    </section>
  );
}

function ApiTestimonialsSection({
  stories,
}: {
  stories: NonNullable<RetreatListing["testimonials"]>;
}) {
  return (
    <section className="mt-14 pt-12 border-t border-sand-200" aria-labelledby="listing-api-stories">
      <h2
        id="listing-api-stories"
        className="font-display text-2xl md:text-3xl font-semibold text-sage-800 text-balance"
      >
        Guest stories
      </h2>
      <ul className="mt-8 grid gap-5 md:grid-cols-2">
        {stories.map((story, index) => (
          <li
            key={`${story.guestName ?? "guest"}-${index}`}
            className="rounded-2xl border border-sand-200 bg-white p-5"
          >
            <p className="text-sm text-sage-700 leading-relaxed">{story.body}</p>
            {story.guestName && (
              <p className="mt-4 text-sm font-medium text-sage-800">{story.guestName}</p>
            )}
          </li>
        ))}
      </ul>
    </section>
  );
}

function ApiRoomsSection({ rooms }: { rooms: NonNullable<RetreatListing["rooms"]> }) {
  return (
    <section className="mt-14 pt-12 border-t border-sand-200" aria-labelledby="listing-api-rooms">
      <h2
        id="listing-api-rooms"
        className="font-display text-2xl md:text-3xl font-semibold text-sage-800 text-balance"
      >
        Rooms &amp; accommodation
      </h2>
      <ul className="mt-8 grid gap-5 sm:grid-cols-2">
        {rooms.map((room, index) => (
          <li
            key={`${room.name}-${index}`}
            className="rounded-2xl border border-sand-200 bg-white p-5"
          >
            <h3 className="font-display text-lg font-semibold text-sage-800">{room.name}</h3>
            <p className="mt-1 text-sm text-sage-600">Up to {room.occupancyMax} guests</p>
          </li>
        ))}
      </ul>
    </section>
  );
}

function ApiInclusionsSection({
  included,
  excluded,
}: {
  included: string[];
  excluded: string[];
}) {
  return (
    <section className="mt-14 pt-12 border-t border-sand-200" aria-labelledby="listing-api-included">
      <h2
        id="listing-api-included"
        className="font-display text-2xl md:text-3xl font-semibold text-sage-800 text-balance"
      >
        What’s included in your programme
      </h2>
      <div className="mt-8 grid gap-10 md:grid-cols-2">
        {included.length > 0 && (
          <div>
            <h3 className="font-display text-lg font-semibold text-sage-800">Included</h3>
            <ul className="mt-4 space-y-2">
              {included.map((label) => (
                <li key={label} className="text-sm text-sage-700">
                  {label}
                </li>
              ))}
            </ul>
          </div>
        )}
        {excluded.length > 0 && (
          <div>
            <h3 className="font-display text-lg font-semibold text-sage-800">Not included</h3>
            <ul className="mt-4 space-y-2">
              {excluded.map((label) => (
                <li key={label} className="text-sm text-sage-700">
                  {label}
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </section>
  );
}
