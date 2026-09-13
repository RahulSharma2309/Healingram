import { Link } from "react-router-dom";
import { Heart, MapPin, Star } from "lucide-react";
import type { Retreat } from "../data/mockData";
import { formatPrice } from "../data/mockData";

export function RetreatCard({
  retreat,
  compact = false,
  badge,
}: {
  retreat: Retreat;
  compact?: boolean;
  badge?: string;
}) {
  const label = badge || (retreat.spotsLeft <= 3 ? "Almost full" : retreat.rating >= 4.8 ? "Guest favourite" : undefined);

  if (compact) {
    return (
      <article className="bg-white rounded-xl overflow-hidden shadow-sm border border-sand-200 hover:shadow-md transition-shadow group">
        <div className="relative aspect-[16/10] overflow-hidden">
          <img
            src={retreat.image}
            alt={retreat.name}
            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
          />
          <button
            type="button"
            className="absolute top-2 right-2 p-1.5 bg-white/90 rounded-full hover:bg-white"
            aria-label="Add to wishlist"
          >
            <Heart className="w-3.5 h-3.5 text-sage-600" />
          </button>
          {label && (
            <span className="absolute top-2 left-2 px-1.5 py-0.5 bg-teal-600 text-white text-[10px] font-medium rounded">
              {label}
            </span>
          )}
        </div>
        <div className="p-2.5">
          <div className="flex items-center gap-1 text-[11px] text-sage-600 mb-0.5">
            <MapPin className="w-3 h-3 shrink-0" />
            <span className="truncate">
              {retreat.location}, {retreat.state}
            </span>
          </div>
          <h3 className="font-display font-semibold text-sm text-sage-800 mb-1.5 line-clamp-2 leading-snug">
            <Link to={`/retreats/${retreat.id}`} className="hover:text-teal-600">
              {retreat.name}
            </Link>
          </h3>
          <div className="flex items-center justify-between gap-2">
            <p className="font-semibold text-sm text-sage-800 truncate">
              From {formatPrice(retreat.price)}
            </p>
            <div className="flex items-center gap-0.5 text-xs shrink-0">
              <Star className="w-3.5 h-3.5 fill-amber-500 text-amber-500" />
              <span className="font-medium">{retreat.rating}</span>
            </div>
          </div>
        </div>
      </article>
    );
  }

  return (
    <article className="bg-white rounded-2xl overflow-hidden border border-sand-200 hover:shadow-lg transition-shadow group">
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
        {label && (
          <span className="absolute top-3 left-3 px-2.5 py-1 bg-teal-600 text-white text-xs font-semibold rounded-md shadow-sm">
            {label}
          </span>
        )}
      </div>
      <div className="p-5">
        <div className="flex items-center justify-between gap-2 mb-2">
          <div className="flex items-center gap-1 text-sm text-sage-600 min-w-0">
            <MapPin className="w-3.5 h-3.5 shrink-0" />
            <span className="truncate">
              {retreat.location}, {retreat.state}
            </span>
          </div>
          <div className="flex items-center gap-1 text-sm shrink-0">
            <Star className="w-4 h-4 fill-amber-500 text-amber-500" />
            <span className="font-semibold text-sage-800">{retreat.rating}</span>
            <span className="text-gray-400">({retreat.reviews})</span>
          </div>
        </div>
        <h3 className="font-display font-semibold text-lg text-sage-800 mb-2 line-clamp-2 leading-snug">
          <Link to={`/retreats/${retreat.id}`} className="hover:text-teal-600">
            {retreat.name}
          </Link>
        </h3>
        <p className="text-sm text-gray-500 mb-4">{retreat.duration} · {retreat.vendor}</p>
        <div className="flex items-end justify-between gap-3 border-t border-sand-100 pt-4">
          <div>
            <p className="text-[11px] uppercase tracking-wide text-gray-400 font-medium">From</p>
            <p className="font-semibold text-lg text-sage-800">
              {formatPrice(retreat.price)}{" "}
              <span className="text-xs font-normal text-gray-500">/ person</span>
            </p>
          </div>
          <Link
            to={`/retreats/${retreat.id}`}
            className="text-sm font-semibold text-teal-600 hover:text-teal-700"
          >
            View details →
          </Link>
        </div>
      </div>
    </article>
  );
}
