/**
 * Component 1 — Global Header / Navigation
 *
 * Visual design preserved. Numbered items 1–11 are wired through
 * `navigation/headerConfig.ts`. Dropdown *content* for items 2–4 is empty
 * until each tab is specified (launch-supply only — no invented categories).
 */

import { Link, NavLink, useLocation, useNavigate } from "react-router-dom";
import { ChevronDown, Heart, Menu, User, X } from "lucide-react";
import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
  type KeyboardEvent as ReactKeyboardEvent,
  type ReactNode,
} from "react";
import { getUserName, isLoggedIn, logOut } from "../lib/auth";
import { buildDestinationsMenuFromPlaces, getHeaderItem, type NavLinkItem } from "../navigation/headerConfig";
import { usePublishedRetreats } from "../lib/api/usePublishedRetreats";
import healingramMark from "../assets/healingram-mark.png";

function WhatsAppIcon({ className = "w-4 h-4" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.435 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z" />
    </svg>
  );
}

function isNavItemActive(to: string, pathname: string, search: string): boolean {
  const [path, query = ""] = to.split("?");
  if (pathname !== path) return false;
  if (!query) return search === "" || search === "?";
  const want = new URLSearchParams(query);
  const have = new URLSearchParams(search);
  for (const [k, v] of want.entries()) {
    if (have.get(k) !== v) return false;
  }
  return true;
}

function groupNavItems(items: NavLinkItem[]): { group?: string; items: NavLinkItem[] }[] {
  const sections: { group?: string; items: NavLinkItem[] }[] = [];
  for (const item of items) {
    const last = sections[sections.length - 1];
    if (last && last.group === item.group) {
      last.items.push(item);
    } else {
      sections.push({ group: item.group, items: [item] });
    }
  }
  return sections;
}

function useAuthState() {
  const [loggedIn, setLoggedIn] = useState(false);
  const [userName, setUserName] = useState("Guest");

  const refresh = useCallback(() => {
    setLoggedIn(isLoggedIn());
    setUserName(getUserName());
  }, []);

  useEffect(() => {
    refresh();
    window.addEventListener("healingram-auth", refresh);
    window.addEventListener("storage", refresh);
    return () => {
      window.removeEventListener("healingram-auth", refresh);
      window.removeEventListener("storage", refresh);
    };
  }, [refresh]);

  return { loggedIn, userName };
}

/** Shared dropdown shell — menu content is data-driven from launch supply. */
function NavDropdown({
  label,
  items,
  open,
  onOpen,
  onClose,
  onNavigate,
  mobile = false,
}: {
  label: string;
  items: NavLinkItem[];
  open: boolean;
  onOpen: () => void;
  onClose: () => void;
  onNavigate?: () => void;
  mobile?: boolean;
}) {
  const id = useId();
  const { pathname, search } = useLocation();
  const rootRef = useRef<HTMLDivElement>(null);
  const itemRefs = useRef<(HTMLAnchorElement | null)[]>([]);
  const closeTimer = useRef<number | null>(null);
  const hasMenu = items.length > 0;
  const sections = groupNavItems(items);
  const anyActive = items.some((item) => isNavItemActive(item.to, pathname, search));

  const clearCloseTimer = () => {
    if (closeTimer.current != null) {
      window.clearTimeout(closeTimer.current);
      closeTimer.current = null;
    }
  };

  const scheduleClose = () => {
    if (mobile) return;
    clearCloseTimer();
    closeTimer.current = window.setTimeout(() => onClose(), 160);
  };

  useEffect(() => () => clearCloseTimer(), []);

  useEffect(() => {
    if (!open || !hasMenu) return;
    const onDoc = (e: MouseEvent) => {
      if (!rootRef.current?.contains(e.target as Node)) onClose();
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        onClose();
        (rootRef.current?.querySelector("button") as HTMLButtonElement | null)?.focus();
      }
    };
    document.addEventListener("mousedown", onDoc);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDoc);
      document.removeEventListener("keydown", onKey);
    };
  }, [open, onClose, hasMenu]);

  const focusItem = (index: number) => itemRefs.current[index]?.focus();

  const onTriggerKeyDown = (e: ReactKeyboardEvent<HTMLButtonElement>) => {
    if (!hasMenu) return;
    if (e.key === "ArrowDown" || e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      onOpen();
      requestAnimationFrame(() => focusItem(0));
    } else if (e.key === "Escape") {
      onClose();
    }
  };

  const onMenuKeyDown = (e: ReactKeyboardEvent<HTMLDivElement>) => {
    const count = items.length;
    const current = itemRefs.current.findIndex((el) => el === document.activeElement);
    if (e.key === "ArrowDown") {
      e.preventDefault();
      focusItem(current < 0 ? 0 : (current + 1) % count);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      focusItem(current <= 0 ? count - 1 : current - 1);
    } else if (e.key === "Home") {
      e.preventDefault();
      focusItem(0);
    } else if (e.key === "End") {
      e.preventDefault();
      focusItem(count - 1);
    } else if (e.key === "Escape") {
      e.preventDefault();
      onClose();
      (rootRef.current?.querySelector("button") as HTMLButtonElement | null)?.focus();
    }
  };

  const renderSections = (compact: boolean) => {
    let flatIndex = 0;
    return sections.map((section) => (
      <div key={section.group ?? "default"} className={section.group ? "pt-1" : undefined}>
        {section.group && (
          <p
            className={`px-4 pt-2 pb-1 text-[10px] font-semibold uppercase tracking-wider text-sage-500 ${
              compact ? "px-2" : ""
            }`}
          >
            {section.group}
          </p>
        )}
        {section.items.map((item) => {
          const i = flatIndex++;
          const active = isNavItemActive(item.to, pathname, search);
          return (
            <Link
              key={item.id}
              to={item.to}
              role="menuitem"
              tabIndex={compact ? undefined : -1}
              aria-current={active ? "page" : undefined}
              ref={(el) => {
                itemRefs.current[i] = el;
              }}
              onClick={() => {
                onClose();
                onNavigate?.();
              }}
              className={
                compact
                  ? `py-2 text-sm rounded-lg px-2 hover:bg-sand-100 ${
                      active
                        ? "font-semibold text-teal-700 bg-teal-50"
                        : item.emphasis
                          ? "font-semibold text-teal-700"
                          : "text-gray-600"
                    }`
                  : `block px-4 py-2.5 text-sm hover:bg-sand-50 focus:bg-sand-50 focus:outline-none ${
                      active
                        ? "font-semibold text-teal-700 bg-teal-50"
                        : item.emphasis
                          ? "font-semibold text-teal-700 border-t border-sand-100 mt-1 pt-3"
                          : "text-sage-800"
                    }`
              }
            >
              {item.label}
            </Link>
          );
        })}
      </div>
    ));
  };

  if (!hasMenu) {
    if (mobile) {
      return (
        <div className="border-b border-sand-100 py-3 text-sm font-medium text-sage-800/70">
          {label}
        </div>
      );
    }
    return (
      <span className="inline-flex items-center gap-1 text-sm font-medium text-gray-500 cursor-default">
        {label}
        <ChevronDown className="w-3.5 h-3.5 opacity-40" />
      </span>
    );
  }

  if (mobile) {
    return (
      <div ref={rootRef} className="border-b border-sand-100 last:border-0">
        <button
          type="button"
          className={`w-full flex items-center justify-between py-3 text-sm font-medium ${
            anyActive || open ? "text-teal-700" : "text-sage-800"
          }`}
          aria-expanded={open}
          aria-controls={id}
          onClick={() => (open ? onClose() : onOpen())}
        >
          {label}
          <ChevronDown className={`w-4 h-4 text-sage-500 transition-transform ${open ? "rotate-180" : ""}`} />
        </button>
        {open && (
          <div id={id} role="menu" className="pb-3 pl-1 flex flex-col gap-0.5" onKeyDown={onMenuKeyDown}>
            {renderSections(true)}
          </div>
        )}
      </div>
    );
  }

  return (
    <div
      ref={rootRef}
      className="relative"
      onMouseEnter={() => {
        clearCloseTimer();
        onOpen();
      }}
      onMouseLeave={scheduleClose}
    >
      <button
        type="button"
        className={`inline-flex items-center gap-1 text-sm font-medium transition-colors ${
          open || anyActive ? "text-teal-600" : "text-gray-600 hover:text-sage-800"
        }`}
        aria-expanded={open}
        aria-haspopup="menu"
        aria-controls={id}
        onClick={() => (open ? onClose() : onOpen())}
        onKeyDown={onTriggerKeyDown}
      >
        {label}
        <ChevronDown className={`w-3.5 h-3.5 transition-transform ${open ? "rotate-180" : ""}`} />
      </button>
      {open && (
        <div
          id={id}
          role="menu"
          className="absolute left-0 top-full pt-2 z-50"
          onKeyDown={onMenuKeyDown}
          onMouseEnter={clearCloseTimer}
        >
          <div className="min-w-[260px] rounded-xl border border-sand-200 bg-white py-2 shadow-lg">
            {renderSections(false)}
          </div>
        </div>
      )}
    </div>
  );
}

function AccountDropdown({
  userName,
  open,
  onOpen,
  onClose,
}: {
  userName: string;
  open: boolean;
  onOpen: () => void;
  onClose: () => void;
}) {
  // Item 11 shell — full account menu content awaits Tab 11 specification
  const id = useId();
  const rootRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();
  const closeTimer = useRef<number | null>(null);

  useEffect(() => {
    if (!open) return;
    const onDoc = (e: MouseEvent) => {
      if (!rootRef.current?.contains(e.target as Node)) onClose();
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    document.addEventListener("mousedown", onDoc);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDoc);
      document.removeEventListener("keydown", onKey);
    };
  }, [open, onClose]);

  return (
    <div
      ref={rootRef}
      className="relative"
      onMouseEnter={() => {
        if (closeTimer.current != null) window.clearTimeout(closeTimer.current);
        onOpen();
      }}
      onMouseLeave={() => {
        closeTimer.current = window.setTimeout(() => onClose(), 160);
      }}
    >
      <button
        type="button"
        className={`inline-flex items-center gap-2 text-sm font-medium ${
          open ? "text-teal-600" : "text-gray-600 hover:text-sage-800"
        }`}
        aria-expanded={open}
        aria-haspopup="menu"
        aria-controls={id}
        onClick={() => (open ? onClose() : onOpen())}
      >
        <span className="w-8 h-8 rounded-full bg-teal-100 text-teal-700 inline-flex items-center justify-center">
          <User className="w-4 h-4" />
        </span>
        <span className="max-w-[7rem] truncate">{userName}</span>
        <ChevronDown className={`w-3.5 h-3.5 transition-transform ${open ? "rotate-180" : ""}`} />
      </button>
      {open && (
        <div id={id} role="menu" className="absolute right-0 top-full pt-2 z-50">
          <div className="min-w-[220px] rounded-xl border border-sand-200 bg-white py-2 shadow-lg">
            <p className="px-4 py-2 text-xs text-gray-400">Account menu — awaiting Tab 11 spec</p>
            <button
              type="button"
              role="menuitem"
              className="w-full text-left px-4 py-2.5 text-sm text-sage-800 hover:bg-sand-50 border-t border-sand-100"
              onClick={() => {
                logOut();
                onClose();
                navigate("/");
              }}
            >
              Log out
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

type OpenMenu = "types" | "destinations" | "account" | null;

export function CustomerHeader() {
  const { loggedIn, userName } = useAuthState();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [openMenu, setOpenMenu] = useState<OpenMenu>(null);
  const [mobileSection, setMobileSection] = useState<OpenMenu>(null);
  const navigate = useNavigate();

  const item1 = getHeaderItem(1);
  const item2 = getHeaderItem(2);
  const item3 = getHeaderItem(3);
  const item4 = getHeaderItem(4);
  const item6 = getHeaderItem(6);
  const item7 = getHeaderItem(7);
  const item8 = getHeaderItem(8);
  const item9 = getHeaderItem(9);

  const { places } = usePublishedRetreats();
  const typeItems = item3.getMenuItems?.() ?? [];
  const destinationItems =
    places.length > 0 ? buildDestinationsMenuFromPlaces(places) : (item4.getMenuItems?.() ?? []);

  const closeAll = useCallback(() => {
    setOpenMenu(null);
    setMobileSection(null);
  }, []);

  useEffect(() => {
    if (!mobileOpen) setMobileSection(null);
  }, [mobileOpen]);

  useEffect(() => {
    const onResize = () => {
      if (window.innerWidth >= 1024) setMobileOpen(false);
    };
    window.addEventListener("resize", onResize);
    return () => window.removeEventListener("resize", onResize);
  }, []);

  const closeMobile = () => {
    setMobileOpen(false);
    closeAll();
  };

  let desktopActions: ReactNode;
  if (loggedIn) {
    desktopActions = (
      <>
        {/* Item 11 — shell until Tab 11 spec */}
        <Link
          to="/dashboard?tab=trips"
          className="text-sm font-medium text-gray-600 hover:text-sage-800"
        >
          My Trips
        </Link>
        <Link
          to="/dashboard"
          className="inline-flex items-center gap-1.5 text-sm font-medium text-gray-600 hover:text-sage-800"
          data-header-item={item6.id}
        >
          <Heart className="w-4 h-4" />
          {item6.label}
        </Link>
        <AccountDropdown
          userName={userName}
          open={openMenu === "account"}
          onOpen={() => setOpenMenu("account")}
          onClose={() => setOpenMenu((m) => (m === "account" ? null : m))}
        />
      </>
    );
  } else {
    desktopActions = (
      <>
        <Link
          to="/dashboard"
          className="inline-flex items-center gap-1.5 text-sm font-medium text-gray-600 hover:text-sage-800"
          data-header-item={item6.id}
        >
          <Heart className="w-4 h-4" />
          {item6.label}
        </Link>
        <Link
          to="/login"
          className="text-sm font-medium text-gray-600 hover:text-sage-800"
          data-header-item={item7.id}
        >
          {item7.label}
        </Link>
      </>
    );
  }

  return (
    <header className="sticky top-0 z-40 bg-white/95 backdrop-blur-md border-b border-sand-200">
      {/* Trust strip + Item 9 */}
      <div className="hidden md:block bg-sage-800 text-white text-[11px] tracking-wide">
        <div className="max-w-7xl mx-auto px-4 h-8 flex items-center justify-between gap-4">
          <p className="opacity-90 truncate">
            Verified wellness retreats
            <span className="mx-2 opacity-40">·</span>
            Transparent programme pricing
            <span className="mx-2 opacity-40">·</span>
            Expert help before you book
          </p>
          <Link
            to="/vendor"
            className="shrink-0 font-medium opacity-90 hover:opacity-100 hover:text-teal-200"
            data-header-item={item9.id}
          >
            {item9.label}
          </Link>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-4 h-16 flex items-center gap-3">
        <button
          type="button"
          className="lg:hidden p-2 -ml-2 rounded-lg hover:bg-sand-100"
          onClick={() => setMobileOpen((v) => !v)}
          aria-label={mobileOpen ? "Close menu" : "Open menu"}
          aria-expanded={mobileOpen}
        >
          {mobileOpen ? <X className="w-6 h-6 text-sage-800" /> : <Menu className="w-6 h-6 text-sage-800" />}
        </button>

        {/* ——— Tab / Item 1: [icon] Healingram wordmark ——— */}
        <NavLink
          to="/"
          end
          data-header-item={item1.id}
          data-header-key={item1.key}
          onClick={closeMobile}
          className={({ isActive }) =>
            `shrink-0 inline-flex items-center gap-2.5 sm:gap-4 -ml-3 sm:-ml-3.5 lg:mr-5 ${
              isActive ? "text-teal-700" : "text-sage-800 hover:text-sage-700"
            }`
          }
        >
          <img
            src={healingramMark}
            alt=""
            aria-hidden="true"
            className="h-11 w-auto sm:h-12 object-contain object-left select-none"
            draggable={false}
          />
          <span className="font-display text-xl font-bold tracking-tight">
            Healingram
          </span>
        </NavLink>

        <nav className="hidden lg:flex items-center gap-6 flex-1" aria-label="Primary">
          <NavLink
            to={item2.to ?? "/retreats"}
            end
            data-header-item={item2.id}
            data-header-key={item2.key}
            className={({ isActive }) =>
              `inline-flex items-center text-sm font-medium transition-colors ${
                isActive ? "text-teal-600" : "text-gray-600 hover:text-sage-800"
              }`
            }
          >
            {item2.label}
          </NavLink>
          <NavDropdown
            label={item3.label}
            items={typeItems}
            open={openMenu === "types"}
            onOpen={() => setOpenMenu("types")}
            onClose={() => setOpenMenu((m) => (m === "types" ? null : m))}
          />
          <NavDropdown
            label={item4.label}
            items={destinationItems}
            open={openMenu === "destinations"}
            onOpen={() => setOpenMenu("destinations")}
            onClose={() => setOpenMenu((m) => (m === "destinations" ? null : m))}
          />
        </nav>

        <div className="hidden lg:flex items-center gap-4 ml-auto">
          {desktopActions}
            <Link
            to="/contact"
            className="inline-flex items-center gap-1.5 text-sm font-semibold px-4 py-2 bg-teal-600 text-white rounded-lg hover:bg-teal-500"
            data-header-item={item8.id}
          >
            <WhatsAppIcon className="w-4 h-4" />
            {item8.label}
          </Link>
        </div>

        <div className="flex lg:hidden items-center gap-1 ml-auto">
          <Link
            to="/dashboard"
            className="p-2 rounded-lg hover:bg-sand-100"
            aria-label={item6.label}
            onClick={closeMobile}
            data-header-item={item6.id}
          >
            <Heart className="w-5 h-5 text-sage-700" />
          </Link>
          <Link
            to="/contact"
            className="inline-flex items-center gap-1.5 text-sm font-semibold px-3 py-1.5 bg-teal-600 text-white rounded-lg hover:bg-teal-500"
            onClick={closeMobile}
            aria-label={item8.label}
            data-header-item={item8.id}
          >
            <WhatsAppIcon className="w-4 h-4" />
            Talk
          </Link>
        </div>
      </div>

      {mobileOpen && (
        <nav className="lg:hidden border-t border-sand-200 bg-white px-4 py-2 max-h-[min(70vh,32rem)] overflow-y-auto">
          <NavLink
            to={item2.to ?? "/retreats"}
            end
            onClick={closeMobile}
            data-header-item={item2.id}
            data-header-key={item2.key}
            className={({ isActive }) =>
              `block border-b border-sand-100 py-3 text-sm font-medium ${
                isActive ? "text-teal-700" : "text-sage-800"
              }`
            }
          >
            {item2.label}
          </NavLink>
          <NavDropdown
            label={item3.label}
            items={typeItems}
            open={mobileSection === "types"}
            onOpen={() => setMobileSection("types")}
            onClose={() => setMobileSection((s) => (s === "types" ? null : s))}
            onNavigate={closeMobile}
            mobile
          />
          <NavDropdown
            label={item4.label}
            items={destinationItems}
            open={mobileSection === "destinations"}
            onOpen={() => setMobileSection("destinations")}
            onClose={() => setMobileSection((s) => (s === "destinations" ? null : s))}
            onNavigate={closeMobile}
            mobile
          />
          <Link
            to="/dashboard"
            onClick={closeMobile}
            className="block py-3 text-sm font-medium text-sage-800 border-b border-sand-100"
            data-header-item={item6.id}
          >
            {item6.label}
          </Link>
          {loggedIn ? (
            <button
              type="button"
              className="w-full text-left py-3 text-sm font-medium text-sage-800 border-b border-sand-100"
              onClick={() => {
                logOut();
                closeMobile();
                navigate("/");
              }}
            >
              Log out
            </button>
          ) : (
            <Link
              to="/login"
              onClick={closeMobile}
              className="block py-3 text-sm font-medium text-sage-800 border-b border-sand-100"
              data-header-item={item7.id}
            >
              {item7.label}
            </Link>
          )}
          <Link
            to="/contact"
            onClick={closeMobile}
            className="mt-3 mb-2 flex items-center justify-center gap-2 w-full py-3 bg-teal-600 text-white text-sm font-semibold rounded-xl"
            data-header-item={item8.id}
          >
            <WhatsAppIcon className="w-4 h-4" />
            {item8.label}
          </Link>
          <Link
            to="/vendor"
            onClick={closeMobile}
            className="block text-center text-xs text-gray-500 pb-3 hover:text-teal-700"
            data-header-item={item9.id}
          >
            {item9.label}
          </Link>
        </nav>
      )}
    </header>
  );
}
