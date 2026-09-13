import { Link } from "react-router-dom";
import type { Retreat } from "../data/mockData";

const tilts = ["-rotate-2", "rotate-1", "-rotate-1", "rotate-2", "rotate-0", "-rotate-3"];

export function Polaroid({
  retreat,
  index = 0,
  className = "",
  stacked = false,
}: {
  retreat: Retreat;
  index?: number;
  className?: string;
  stacked?: boolean;
}) {
  const tilt = tilts[index % tilts.length];

  return (
    <Link
      to={`/retreats/${retreat.id}`}
      className={`group block bg-[#fffcf7] p-3 pb-0 shadow-[0_8px_24px_rgba(61,42,26,0.18)] border border-sand-200/80 transition-transform duration-300 hover:scale-[1.03] hover:z-20 hover:rotate-0 ${
        stacked ? "" : tilt
      } ${className}`}
      style={{
        boxShadow:
          "0 1px 1px rgba(0,0,0,0.06), 0 10px 28px rgba(61,42,26,0.16), inset 0 0 0 1px rgba(255,255,255,0.6)",
      }}
    >
      <div className="relative aspect-square overflow-hidden bg-sand-100">
        <img
          src={retreat.image}
          alt={retreat.name}
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
        />
      </div>
      <div className="px-1 pt-3 pb-4 min-h-[4.25rem] flex flex-col justify-center">
        <p className="font-display text-center text-sm md:text-base font-semibold text-sage-800 leading-snug line-clamp-2">
          {retreat.name}
        </p>
        <p className="text-center text-[11px] text-sage-500 mt-1">
          {retreat.location}
        </p>
      </div>
    </Link>
  );
}

/** Overlapping polaroid stack — one photo sits on top */
export function PolaroidStack({ retreats }: { retreats: Retreat[] }) {
  const items = retreats.slice(0, 3);
  const positions = [
    "left-0 top-6 -rotate-6 z-10",
    "left-[18%] top-0 rotate-2 z-20",
    "left-[36%] top-8 rotate-6 z-30",
  ];

  return (
    <div className="relative mx-auto h-[280px] sm:h-[320px] w-full max-w-md">
      {items.map((retreat, i) => (
        <div
          key={retreat.id}
          className={`absolute w-[42%] sm:w-[48%] ${positions[i]} transition-transform duration-300 hover:scale-105 hover:z-40`}
        >
          <Polaroid retreat={retreat} index={i} stacked />
        </div>
      ))}
    </div>
  );
}
