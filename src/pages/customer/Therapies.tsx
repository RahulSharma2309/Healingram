import { Link } from "react-router-dom";
import { therapyCategories } from "../../data/mockData";

export function Therapies() {
  return (
    <div className="max-w-7xl mx-auto px-4 py-10">
      <h1 className="font-display text-3xl font-bold text-sage-800 mb-2">Therapy categories</h1>
      <p className="text-gray-600 mb-10">Browse retreats by healing approach</p>
      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
        {therapyCategories.map((c) => (
          <Link
            key={c.name}
            to={`/retreats?therapy=${encodeURIComponent(c.name)}`}
            className="group relative rounded-2xl overflow-hidden aspect-[4/3] border border-sand-200"
          >
            <img
              src={c.image}
              alt={c.name}
              className="absolute inset-0 w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-sage-800/90 via-sage-800/40 to-transparent" />
            <div className="absolute inset-x-0 bottom-0 p-6 text-white">
              <h2 className="font-display text-xl font-semibold">{c.name}</h2>
              <p className="text-sm text-white/85 mt-1">{c.tagline}</p>
              <p className="text-xs text-teal-200 mt-2">{c.count} retreats available</p>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
