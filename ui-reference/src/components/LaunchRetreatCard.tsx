import { Link } from "react-router-dom";
import { Heart, MapPin, BadgeCheck } from "lucide-react";
import {
  formatLaunchPrice,
  getRetreatDisplayTags,
  LAUNCH_DESTINATIONS,
  type LaunchRetreat,
} from "../data/launchSupply";
import { getRetreatVerifiedFromPrice } from "../data/allRetreatsBrowse";

export function LaunchRetreatCard({ retreat }: { retreat: LaunchRetreat }) {
  const regionLabel = LAUNCH_DESTINATIONS[retreat.region].regionLabel;
  const tags = getRetreatDisplayTags(retreat);
  const verifiedFrom = getRetreatVerifiedFromPrice(retreat.id);
  const priceLabel =
    formatLaunchPrice(verifiedFrom) ?? formatLaunchPrice(retreat.priceFrom ?? null);

  return (
    <article className="bg-white rounded-2xl overflow-hidden border border-sand-200 hover:shadow-md transition-shadow group flex flex-col">
      <div className="relative aspect-[4/3] overflow-hidden">
        <img
          src={retreat.image}
          alt={retreat.name}
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
        />
        <button
          type="button"
          className="absolute top-3 right-3 p-2 bg-white/95 rounded-full hover:bg-white shadow-sm"
          aria-label="Add to wishlist"
        >
          <Heart className="w-4 h-4 text-sage-600" />
        </button>
        {retreat.mvpDemoVerified && (
          <span className="absolute top-3 left-3 inline-flex items-center gap-1 px-2 py-1 bg-white/95 text-teal-700 text-[11px] font-semibold rounded-md shadow-sm">
            <BadgeCheck className="w-3.5 h-3.5" />
            Healingram Verified
          </span>
        )}
      </div>

      <div className="p-4 flex flex-col flex-1">
        <div className="flex items-center gap-1 text-sm text-sage-600 mb-1.5">
          <MapPin className="w-3.5 h-3.5 shrink-0" />
          <span className="truncate">
            {retreat.locality}, {regionLabel}
          </span>
        </div>

        <h3 className="font-display font-semibold text-lg text-sage-800 mb-2 leading-snug line-clamp-2">
          {retreat.name}
        </h3>

        {tags.length > 0 && (
          <div className="flex flex-wrap gap-1.5 mb-3">
            {tags.map((tag) => (
              <span
                key={tag}
                className="px-2 py-0.5 rounded-md bg-sand-100 text-sage-700 text-[11px] font-medium"
              >
                {tag}
              </span>
            ))}
          </div>
        )}

        {retreat.typicalDuration && (
          <p className="text-sm text-gray-500 mb-3">{retreat.typicalDuration}</p>
        )}

        <div className="mt-auto flex items-end justify-between gap-3 border-t border-sand-100 pt-3">
          <div className="min-w-0">
            {priceLabel ? (
              <p className="font-semibold text-sage-800">{priceLabel}</p>
            ) : (
              <p className="text-sm text-sage-600">Price on programme selection</p>
            )}
          </div>
          <Link
            to={`/retreats/${retreat.id}`}
            className="shrink-0 text-sm font-semibold text-teal-600 hover:text-teal-700"
          >
            View Retreat
          </Link>
        </div>
      </div>
    </article>
  );
}
