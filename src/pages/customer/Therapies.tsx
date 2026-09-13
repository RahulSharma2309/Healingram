import { Link } from "react-router-dom";
import { useCatalogDiscovery } from "../../lib/api/useCatalogDiscovery";

export function Therapies() {
  const { needs, source } = useCatalogDiscovery();

  if (source === "loading") {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center text-sage-600">
        Loading published programmes…
      </div>
    );
  }

  if (source === "error") {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center text-sage-600">
        Could not load catalog programmes.
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 py-10">
      <h1 className="font-display text-3xl font-bold text-sage-800 mb-2">Therapy categories</h1>
      <p className="text-gray-600 mb-10">Browse retreats by published need</p>
      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
        {needs.map((card) => (
          <Link
            key={card.id}
            to={card.href || `/retreats?need=${encodeURIComponent(card.id)}`}
            className="group relative rounded-2xl overflow-hidden aspect-[4/3] border border-sand-200 bg-sand-100"
          >
            {card.image ? (
              <img
                src={card.image}
                alt={card.label}
                className="absolute inset-0 w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
              />
            ) : null}
            <div className="absolute inset-0 bg-gradient-to-t from-sage-800/90 via-sage-800/40 to-transparent" />
            <div className="absolute inset-x-0 bottom-0 p-6 text-white">
              <h2 className="font-display text-xl font-semibold">{card.label}</h2>
              <p className="text-sm text-white/85 mt-1">{card.description}</p>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
