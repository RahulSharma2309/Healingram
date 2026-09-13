import { Link } from "react-router-dom";
import { useEffect, useState } from "react";
import { ArrowRight } from "lucide-react";
import { HeroDiscovery } from "../../components/HeroDiscovery";
import { useCatalogDiscovery } from "../../lib/api/useCatalogDiscovery";
import { fetchHomepage, type HomepageSection } from "../../lib/api/catalog";

export function Home() {
  const { needs, destinations, source } = useCatalogDiscovery();
  const catalogReady = source === "api";
  const [sections, setSections] = useState<HomepageSection[]>([]);
  const [contentSource, setContentSource] = useState<"loading" | "api" | "error">("loading");

  useEffect(() => {
    fetchHomepage()
      .then((items) => {
        setSections(items);
        setContentSource("api");
      })
      .catch(() => setContentSource("error"));
  }, []);

  const hero = sections.find((s) => s.slug === "hero");
  const needsCopy = sections.find((s) => s.slug === "needs");
  const destCopy = sections.find((s) => s.slug === "destinations");
  const expert = sections.find((s) => s.slug === "expert");
  return (
    <>
      <section className="relative overflow-hidden flex items-center">
        <div
          className="absolute inset-0"
          style={{
            backgroundImage: contentSource === "api" && hero?.imageUrl ? `url(${hero.imageUrl})` : undefined,
            backgroundSize: "cover",
            backgroundPosition: "center",
          }}
        />
        <div className="absolute inset-0 bg-gradient-to-b from-sage-800/70 via-teal-800/55 to-sage-800/80" />
        <div className="relative w-full max-w-3xl mx-auto px-4 py-14 md:py-20 text-center text-white">
          {contentSource === "error" && (
            <p className="mb-4 text-sm text-white/90">Homepage copy could not be loaded from the catalog.</p>
          )}
          <h1 className="font-display text-3xl sm:text-4xl md:text-[2.75rem] font-bold tracking-tight leading-[1.15] mb-4 text-balance">
            {hero?.title ?? (contentSource === "loading" ? "Loading…" : "Find a retreat")}
          </h1>
          {hero?.body && (
            <p className="text-base md:text-lg font-medium leading-relaxed text-white/90 mb-8 max-w-2xl mx-auto text-balance">
              {hero.body}
            </p>
          )}
          <HeroDiscovery />
        </div>
      </section>

      {/* ——— Component 3: Explore by what you need ——— */}
      <section className="bg-sand-50 py-14 md:py-16 px-4">
        <div className="max-w-7xl mx-auto">
          <div className="max-w-2xl mx-auto text-center mb-10">
            <h2 className="font-display text-2xl md:text-3xl font-bold text-sage-800 mb-3 text-balance">
              {needsCopy?.title ?? "Explore by what you need"}
            </h2>
            {needsCopy?.body && (
              <p className="text-sage-600 text-base md:text-lg leading-relaxed text-balance">
                {needsCopy.body}
              </p>
            )}
          </div>

          {!catalogReady && (
            <p className="text-center text-sm text-sage-600 mb-8">
              {source === "loading"
                ? "Loading published programmes from the catalog…"
                : "Cannot reach the catalog. Start the gateway on port 5000 and refresh."}
            </p>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5 md:gap-6">
            {needs.map((card) => (
              <Link
                key={card.id}
                to={`/retreats?need=${encodeURIComponent(card.id)}`}
                className="group flex flex-col rounded-2xl overflow-hidden bg-white border border-sand-200 shadow-sm hover:shadow-md hover:-translate-y-0.5 focus:outline-none focus-visible:ring-2 focus-visible:ring-teal-600 focus-visible:ring-offset-2 transition-all duration-300"
              >
                <div className="relative aspect-[16/10] overflow-hidden">
                  <img
                    src={card.image}
                    alt=""
                    className="absolute inset-0 w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-sage-800/35 via-transparent to-transparent" />
                </div>
                <div className="flex flex-col flex-1 p-5">
                  <h3 className="font-display text-lg font-semibold text-sage-800 mb-2">
                    {card.label}
                  </h3>
                  <p className="text-sm text-sage-600 leading-relaxed mb-4 flex-1">
                    {card.description}
                  </p>
                  <span className="inline-flex items-center gap-1 text-sm font-semibold text-teal-600 group-hover:text-teal-700">
                    Explore
                    <ArrowRight className="w-4 h-4 transition-transform group-hover:translate-x-0.5" />
                  </span>
                </div>
              </Link>
            ))}
          </div>
        </div>
      </section>

      {/* Why Healingram — intentionally deferred; insert here in a later task */}

      {/* ——— Explore by destination ——— */}
      <section
        id="explore-by-destination"
        className="bg-white py-14 md:py-16 px-4"
        aria-labelledby="home-destinations-heading"
      >
        <div className="max-w-7xl mx-auto">
          <div className="max-w-2xl mb-8 md:mb-10">
            <h2
              id="home-destinations-heading"
              className="font-display text-2xl md:text-3xl font-bold text-sage-800 mb-3 text-balance"
            >
              {destCopy?.title ?? "Explore by destination"}
            </h2>
            {destCopy?.body && (
              <p className="text-sage-600 text-base md:text-lg leading-relaxed text-balance">
                {destCopy.body}
              </p>
            )}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-5 md:gap-6">
            {destinations.map((dest) => (
              <Link
                key={dest.region}
                to={dest.to}
                className="group relative overflow-hidden rounded-2xl aspect-[5/4] md:aspect-[4/3] focus:outline-none focus-visible:ring-2 focus-visible:ring-teal-600 focus-visible:ring-offset-2"
              >
                <img
                  src={dest.image}
                  alt={
                    dest.imageTemporary
                      ? `${dest.label} — temporary destination imagery`
                      : dest.label
                  }
                  className="absolute inset-0 h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
                />
                <div className="absolute inset-0 bg-gradient-to-t from-sage-900/85 via-sage-900/35 to-sage-900/10" />
                <div className="absolute inset-x-0 bottom-0 p-6 md:p-7 text-white">
                  <h3 className="font-display text-2xl md:text-3xl font-semibold text-balance">
                    {dest.label}
                  </h3>
                  <p className="mt-2 text-sm md:text-base text-white/90 leading-relaxed max-w-md text-balance">
                    {dest.description}
                  </p>
                  <span className="mt-4 inline-flex items-center gap-1.5 text-sm font-semibold text-white">
                    {dest.cta}
                    <ArrowRight className="w-4 h-4 transition-transform group-hover:translate-x-0.5" />
                  </span>
                </div>
              </Link>
            ))}
          </div>
        </div>
      </section>

      {/* ——— Compact Talk to an Expert CTA ——— */}
      <section
        className="relative overflow-hidden px-4 py-12 md:py-14"
        aria-labelledby="home-expert-cta-heading"
      >
        <div
          className="absolute inset-0"
          style={{
            backgroundImage: contentSource === "api" && expert?.imageUrl ? `url(${expert.imageUrl})` : undefined,
            backgroundSize: "cover",
            backgroundPosition: "center",
          }}
          aria-hidden
        />
        <div className="absolute inset-0 bg-sage-800/80" aria-hidden />
        <div className="relative max-w-3xl mx-auto text-center text-white">
          <h2
            id="home-expert-cta-heading"
            className="font-display text-2xl md:text-3xl font-semibold text-balance"
          >
            {expert?.title ?? "Talk to an expert"}
          </h2>
          {expert?.body && (
            <p className="mt-3 text-sm md:text-base text-white/90 leading-relaxed text-balance">
              {expert.body}
            </p>
          )}
          <Link
            to={expert?.ctaHref || "/contact"}
            className="mt-6 inline-flex items-center justify-center rounded-xl bg-teal-600 px-6 py-3 text-sm font-semibold text-white hover:bg-teal-500 transition"
          >
            {expert?.ctaLabel ?? "Talk to an Expert"}
          </Link>
        </div>
      </section>
    </>
  );
}
