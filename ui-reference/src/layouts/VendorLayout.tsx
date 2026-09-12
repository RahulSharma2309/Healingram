import { Link, NavLink, Outlet } from "react-router-dom";
import { BarChart3, Calendar, Home, IndianRupee, LayoutDashboard, Users } from "lucide-react";

const nav = [
  { to: "/vendor", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/vendor#availability-requests", label: "Availability", icon: Calendar },
  { to: "/vendor#retreats", label: "Retreats", icon: Home },
  { to: "/vendor#bookings", label: "Bookings", icon: Users },
  { to: "/vendor#earnings", label: "Earnings", icon: IndianRupee },
  { to: "/vendor#analytics", label: "Analytics", icon: BarChart3 },
];

export function VendorLayout() {
  return (
    <div className="min-h-screen flex bg-sand-50">
      <aside className="w-56 bg-sage-800 text-white shrink-0 hidden md:flex flex-col">
        <div className="p-4 border-b border-sage-700">
          <Link to="/" className="font-display font-bold text-lg">Healingram.com</Link>
          <p className="text-xs text-sage-300 mt-1">Vendor panel</p>
        </div>
        <nav className="p-3 flex-1 space-y-1">
          {nav.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                `flex items-center gap-2 px-3 py-2 rounded-lg text-sm ${isActive ? "bg-sage-700" : "hover:bg-sage-700/50"}`
              }
            >
              <Icon className="w-4 h-4" /> {label}
            </NavLink>
          ))}
        </nav>
        <Link to="/" className="p-4 text-xs text-sage-300 hover:text-white border-t border-sage-700">
          ← Back to website
        </Link>
      </aside>
      <div className="flex-1 flex flex-col min-w-0">
        <header className="bg-white border-b border-sand-200 px-4 py-3 flex justify-between items-center">
          <p className="font-medium text-sage-800">Ganga Wellness Ashram</p>
          <span className="text-xs px-2 py-1 bg-teal-100 text-teal-700 rounded">Vendor MVP</span>
        </header>
        <main className="flex-1 p-4 md:p-6 overflow-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
