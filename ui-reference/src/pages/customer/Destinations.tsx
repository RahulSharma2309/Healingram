import { Link } from "react-router-dom";
import { destinations } from "../../data/mockData";

export function Destinations() {
  return (
    <div className="max-w-7xl mx-auto px-4 py-10">
      <h1 className="font-display text-3xl font-bold text-sage-800 mb-2">Destinations</h1>
      <p className="text-gray-600 mb-10">Popular wellness hubs across India</p>
      <div className="grid md:grid-cols-2 gap-6">
        {destinations.map((d) => (
          <Link
            key={d.name}
            to={`/search?location=${d.name}`}
            className="flex gap-4 bg-white rounded-2xl overflow-hidden border border-sand-200 hover:shadow-md"
          >
            <img src={d.image} alt={d.name} className="w-40 h-32 object-cover" />
            <div className="py-4 pr-4">
              <h2 className="font-display text-xl font-semibold text-sage-800">{d.name}</h2>
              <p className="text-gray-500 text-sm">{d.retreats} retreats</p>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
