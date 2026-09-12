import { Link, useLocation, useParams } from "react-router-dom";
import {
  RetreatListingComponent1,
  type FindMyMatchListingState,
} from "../../components/retreat/RetreatListingComponent1";
import { RetreatListingComponent2 } from "../../components/retreat/RetreatListingComponent2";
import { RetreatListingComponent3 } from "../../components/retreat/RetreatListingComponent3";
import { RetreatListingComponent4 } from "../../components/retreat/RetreatListingComponent4";
import { RetreatListingComponent5 } from "../../components/retreat/RetreatListingComponent5";
import { RetreatListingComponent6 } from "../../components/retreat/RetreatListingComponent6";
import { RetreatListingComponent7 } from "../../components/retreat/RetreatListingComponent7";
import { RetreatListingComponent8 } from "../../components/retreat/RetreatListingComponent8";
import { getRetreatListingView } from "../../data/launchListing";
import { ExpertReferralBridge } from "../../components/ExpertReferralBridge";
import { ListingPlanProvider } from "../../lib/listingPlanContext";

/**
 * Retreat listing page.
 * Component 1 — identity, gallery, trust, availability
 * Component 2 — why choose / suitability
 * Component 3 — programmes
 * Component 4 — typical day
 * Component 5 — meet the experts
 * Component 6 — guest stories
 * Component 7 — rooms & accommodation
 * Component 8 — what’s included / not included
 */
export function RetreatDetail() {
  const { id } = useParams();
  const location = useLocation();
  const listing = getRetreatListingView(id);
  const findMyMatch = (location.state as { findMyMatch?: FindMyMatchListingState } | null)
    ?.findMyMatch;

  if (!listing) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center">
        <h1 className="font-display text-2xl font-bold text-sage-800 mb-3">
          Retreat not found
        </h1>
        <p className="text-sage-600 mb-6">
          This property isn’t in Healingram’s current launch inventory.
        </p>
        <Link to="/search" className="text-sm font-semibold text-teal-600 hover:text-teal-700">
          Explore retreats
        </Link>
      </div>
    );
  }

  const retreatId = listing.retreat.id;

  return (
    <ListingPlanProvider>
      <ExpertReferralBridge
        retreatId={retreatId}
        retreatName={listing.retreat.name}
      />
      <div className="max-w-7xl mx-auto px-4 py-8 pb-28 lg:pb-12">
        {/* 1 — Hero / media / availability */}
        <RetreatListingComponent1 listing={listing} />

        {/* 2 — Why choose / suitability */}
        <RetreatListingComponent2 retreatId={retreatId} findMyMatch={findMyMatch} />

        {/* 3 — Choose your programme (must render for every retreat with programme records) */}
        <RetreatListingComponent3 retreatId={retreatId} findMyMatch={findMyMatch} />

        {/* 4 — What a day here may look like */}
        <RetreatListingComponent4 retreatId={retreatId} />

        {/* 5 — Meet the experts */}
        <RetreatListingComponent5 retreatId={retreatId} />

        {/* 6 — Guest stories */}
        <RetreatListingComponent6 retreatId={retreatId} />

        {/* 7 — Rooms & accommodation */}
        <RetreatListingComponent7 retreatId={retreatId} />

        {/* 8 — What’s included / not included */}
        <RetreatListingComponent8 retreatId={retreatId} />

        {/* Below Component 8 — not redesigned yet; neutral stub only */}
        <section className="mt-12 pt-10 border-t border-sand-200 max-w-3xl">
          <h2 className="font-display text-xl font-semibold text-sage-800 mb-2">
            About this retreat
          </h2>
          <p className="text-sm text-sage-600 leading-relaxed">
            Programme details being verified. Fuller inclusions, schedules and stay information
            will appear here once confirmed with the partner.
          </p>
        </section>
      </div>
    </ListingPlanProvider>
  );
}
