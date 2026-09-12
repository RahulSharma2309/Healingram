import { Link, NavLink, Outlet } from "react-router-dom";
import {
  BarChart3,
  BookOpen,
  FileText,
  Headphones,
  LayoutDashboard,
  Percent,
  Store,
  Users,
} from "lucide-react";

const nav = [
  { to: "/admin", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/admin#expert-leads", label: "Expert leads", icon: Headphones },
  { to: "/admin#availability", label: "Availability", icon: FileText },
  { to: "/admin#vendors", label: "Vendors", icon: Store },
  { to: "/admin#retreats", label: "Retreats", icon: BookOpen },
  { to: "/admin#bookings", label: "Bookings", icon: FileText },
  { to: "/admin#commission", label: "Commission", icon: Percent },
  { to: "/admin#cms", label: "CMS / SEO", icon: FileText },
  { to: "/admin#reports", label: "Reports", icon: BarChart3 },
  { to: "/admin#users", label: "Users", icon: Users },
];

export function AdminLayout() {
  return (
    <div className="min-h-screen flex bg-gray-100">
      <aside className="w-56 bg-gray-900 text-white shrink-0 hidden md:flex flex-col">
        <div className="p-4 border-b border-gray-700">
          <p className="font-display font-bold">Healingram Admin</p>
          <p className="text-xs text-gray-400">Platform owner</p>
        </div>
        <nav className="p-3 flex-1 space-y-1">
          {nav.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                `flex items-center gap-2 px-3 py-2 rounded-lg text-sm ${isActive ? "bg-gray-700" : "hover:bg-gray-800"}`
              }
            >
              <Icon className="w-4 h-4" /> {label}
            </NavLink>
          ))}
        </nav>
        <Link to="/" className="p-4 text-xs text-gray-400 hover:text-white border-t border-gray-700">
          ← Customer website
        </Link>
      </aside>
      <div className="flex-1 flex flex-col min-w-0">
        <header className="bg-white border-b px-4 py-3 flex justify-between">
          <p className="font-medium">Admin control panel</p>
          <span className="text-xs px-2 py-1 bg-amber-100 text-amber-800 rounded">Proposal MVP</span>
        </header>
        <main className="flex-1 p-4 md:p-6 overflow-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
