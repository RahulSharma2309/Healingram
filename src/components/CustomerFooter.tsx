import { Link } from "react-router-dom";
import { vendorPortalHref } from "../lib/runtimeConfig";
import healingramMark from "../assets/healingram-mark.png";

export function CustomerFooter() {
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
              Curated wellness retreats, clear programmes and help finding the right stay.
            </p>
          </div>

          <div className="lg:col-span-2">
            <p className="font-semibold text-white mb-3">Explore</p>
            <ul className="space-y-2 text-sm">
              <li>
                <Link to="/retreats" className="hover:text-white transition-colors">
                  Explore Retreats
                </Link>
              </li>
              <li>
                <Link to="/retreats" className="hover:text-white transition-colors">
                  Retreat Types
                </Link>
              </li>
              <li>
                <Link to="/#explore-by-destination" className="hover:text-white transition-colors">
                  Destinations
                </Link>
              </li>
              <li>
                <Link to="/questionnaire" className="hover:text-white transition-colors">
                  Find My Match
                </Link>
              </li>
            </ul>
          </div>

          <div className="lg:col-span-2">
            <p className="font-semibold text-white mb-3">Healingram</p>
            <ul className="space-y-2 text-sm">
              <li>
                <Link to="/about" className="hover:text-white transition-colors">
                  About
                </Link>
              </li>
              <li>
                <Link to="/contact" className="hover:text-white transition-colors">
                  Talk to an Expert
                </Link>
              </li>
              <li>
                <Link to="/contact" className="hover:text-white transition-colors">
                  Contact
                </Link>
              </li>
              <li>
                <Link to="/blog" className="hover:text-white transition-colors">
                  Blog
                </Link>
              </li>
            </ul>
          </div>

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

          <div className="lg:col-span-2">
            <p className="font-semibold text-white mb-3">Legal</p>
            <ul className="space-y-2 text-sm">
              <li>
                <Link to="/terms" className="hover:text-white transition-colors">
                  Terms
                </Link>
              </li>
              <li>
                <Link to="/privacy" className="hover:text-white transition-colors">
                  Privacy
                </Link>
              </li>
            </ul>
          </div>
        </div>
      </div>

      <div className="border-t border-sage-700 text-center py-4 text-xs text-sage-100/70">
        © 2026 Healingram
      </div>
    </footer>
  );
}
