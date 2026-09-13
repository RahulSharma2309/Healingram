import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Calendar, MapPin, Search, Sparkles } from "lucide-react";
import { fetchNeeds, type CatalogNeed } from "../lib/api/catalog";

export function SearchBar({
  compact = false,
  stacked = false,
}: {
  compact?: boolean;
  stacked?: boolean;
}) {
  const navigate = useNavigate();
  const [location, setLocation] = useState("");
  const [category, setCategory] = useState("");
  const [dates, setDates] = useState("");
  const [needs, setNeeds] = useState<CatalogNeed[]>([]);
  const [needsError, setNeedsError] = useState(false);

  useEffect(() => {
    fetchNeeds()
      .then((items) => {
        setNeeds(items);
        setNeedsError(false);
      })
      .catch(() => setNeedsError(true));
  }, []);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    const params = new URLSearchParams();
    if (location) params.set("location", location);
    if (category) params.set("need", category);
    if (dates) params.set("dates", dates);
    navigate(`/search?${params.toString()}`);
  };

  const fields = (
    <>
      <label className="flex-1 flex flex-col gap-1 px-4 py-3 border-b md:border-b-0 md:border-r border-sand-200 min-w-0">
        <span className="text-[11px] font-semibold uppercase tracking-wide text-teal-700">Location</span>
        <span className="flex items-center gap-2">
          <MapPin className="w-4 h-4 text-sage-500 shrink-0" />
          <input
            type="text"
            placeholder="Where are you going?"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
            className="w-full outline-none text-sm text-sage-800 placeholder:text-gray-400 bg-transparent"
          />
        </span>
      </label>
      <label className="flex-1 flex flex-col gap-1 px-4 py-3 border-b md:border-b-0 md:border-r border-sand-200 min-w-0">
        <span className="text-[11px] font-semibold uppercase tracking-wide text-teal-700">Category</span>
        <span className="flex items-center gap-2">
          <Sparkles className="w-4 h-4 text-sage-500 shrink-0" />
          <select
            value={category}
            onChange={(e) => setCategory(e.target.value)}
            className="w-full outline-none text-sm text-sage-800 bg-transparent"
          >
            <option value="">{needsError ? "Categories unavailable" : "What are you seeking?"}</option>
            {needs.map((need) => (
              <option key={need.slug} value={need.slug}>
                {need.label}
              </option>
            ))}
          </select>
        </span>
      </label>
      <label className="flex-1 flex flex-col gap-1 px-4 py-3 border-b md:border-b-0 md:border-r border-sand-200 min-w-0">
        <span className="text-[11px] font-semibold uppercase tracking-wide text-teal-700">Dates</span>
        <span className="flex items-center gap-2">
          <Calendar className="w-4 h-4 text-sage-500 shrink-0" />
          <input
            type="text"
            placeholder="When are you going?"
            value={dates}
            onChange={(e) => setDates(e.target.value)}
            className="w-full outline-none text-sm text-sage-800 placeholder:text-gray-400 bg-transparent"
          />
        </span>
      </label>
    </>
  );

  if (stacked) {
    return (
      <form
        onSubmit={handleSearch}
        className="bg-white rounded-2xl shadow-xl border border-sand-200 p-2 flex flex-col gap-1 w-full"
      >
        {fields}
        <button
          type="submit"
          className="flex items-center justify-center gap-2 mx-2 mb-2 mt-1 px-6 py-3.5 bg-teal-600 hover:bg-teal-500 text-white font-semibold rounded-xl transition-colors"
        >
          <Search className="w-5 h-5" />
          Search retreats
        </button>
      </form>
    );
  }

  return (
    <form
      onSubmit={handleSearch}
      className={`bg-white rounded-2xl shadow-xl border border-sand-200 p-2 flex flex-col md:flex-row md:items-stretch gap-0 ${
        compact ? "max-w-4xl" : "max-w-4xl mx-auto"
      }`}
    >
      {fields}
      <div className="flex items-center p-2">
        <button
          type="submit"
          className="w-full md:w-auto flex items-center justify-center gap-2 px-8 py-3.5 bg-teal-600 hover:bg-teal-500 text-white font-semibold rounded-xl transition-colors whitespace-nowrap"
        >
          <Search className="w-5 h-5" />
          Search
        </button>
      </div>
    </form>
  );
}
