import { Link } from "react-router-dom";
import { useCatalogDiscovery } from "../../lib/api/useCatalogDiscovery";

export function Destinations() {
  const { destinations, source } = useCatalogDiscovery();

  if (source === "loading") {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center">
        <p className="text-sage-600">Loading destinations from the catalog…</p>
      </div>
    );
  }

  if (source === "error") {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center">
        <h1 className="font-display text-2xl font-bold text-sage-800 mb-3">Catalog unavailable</h1>
        <p className="text-sage-600">Start the gateway and API, then refresh.</p>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 py-10">
      <h1 className="font-display text-3xl font-bold text-sage-800 mb-2">Destinations</h1>
      <p className="text-gray-600 mb-10">States and cities from published inventory</p>
      <div className="grid md:grid-cols-2 gap-6">
        {destinations.map((d) => (
          <Link
            key={d.region}
            to={d.to}
            className="flex gap-4 bg-white rounded-2xl overflow-hidden border border-sand-200 hover:shadow-md"
          >
            <img src={d.image} alt={d.label} className="w-40 h-32 object-cover" />
            <div className="py-4 pr-4">
              <h2 className="font-display text-xl font-semibold text-sage-800">{d.label}</h2>
              <p className="text-gray-500 text-sm">{d.description}</p>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
