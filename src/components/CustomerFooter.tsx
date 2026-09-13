import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { vendorPortalHref } from "../lib/runtimeConfig";
import { fetchNavigation, type NavigationItem } from "../lib/api/catalog";
import healingramMark from "../assets/healingram-mark.png";

function NavColumn({
  title,
  items,
  error,
}: {
  title: string;
  items: NavigationItem[];
  error: boolean;
}) {
  return (
    <div className="lg:col-span-2">
      <p className="font-semibold text-white mb-3">{title}</p>
      {error ? (
        <p className="text-sm text-sage-100/80">Links could not be loaded.</p>
      ) : (
        <ul className="space-y-2 text-sm">
          {items.map((item) =>
            item.href.startsWith("http") ? (
              <li key={`${item.menuKey}-${item.href}`}>
                <a href={item.href} className="hover:text-white transition-colors">
                  {item.label}
                </a>
              </li>
            ) : (
              <li key={`${item.menuKey}-${item.href}`}>
                <Link to={item.href} className="hover:text-white transition-colors">
                  {item.label}
                </Link>
              </li>
            ),
          )}
        </ul>
      )}
    </div>
  );
}

export function CustomerFooter() {
  const [explore, setExplore] = useState<NavigationItem[]>([]);
  const [legal, setLegal] = useState<NavigationItem[]>([]);
  const [navError, setNavError] = useState(false);

  useEffect(() => {
    Promise.all([fetchNavigation("customer.explore"), fetchNavigation("customer.legal")])
      .then(([exploreItems, legalItems]) => {
        setExplore(exploreItems);
        setLegal(legalItems);
        setNavError(false);
      })
      .catch(() => setNavError(true));
  }, []);

  return (
    <footer className="bg-sage-800 text-sage-100 mt-auto">
      <div className="max-w-7xl mx-auto px-4 py-12 md:py-14">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-12 gap-10 lg:gap-8">
          <div className="sm:col-span-2 lg:col-span-4">
            <Link
              to="/"
              className="inline-flex items-center gap-3 text-white hover:text-white/95"
            >
              <img
                src={healingramMark}
                alt=""
                aria-hidden="true"
                className="h-10 w-auto object-contain object-left select-none"
                draggable={false}
              />
              <span className="font-display text-xl font-bold tracking-tight">Healingram</span>
            </Link>
            <p className="mt-3 text-sm text-sage-100/90 leading-relaxed max-w-sm">
              Programme-led retreat marketplace.
            </p>
          </div>

          <NavColumn title="Explore" items={explore} error={navError} />

          <div className="lg:col-span-2">
            <p className="font-semibold text-white mb-3">For retreat partners</p>
            <ul className="space-y-2 text-sm">
              <li>
                <a href={vendorPortalHref()} className="hover:text-white transition-colors">
                  Partner with Healingram
                </a>
              </li>
            </ul>
          </div>

          <NavColumn title="Legal" items={legal} error={navError} />
        </div>
      </div>

      <div className="border-t border-sage-700 text-center py-4 text-xs text-sage-100/70">
        Healingram
      </div>
    </footer>
  );
}
