import { Link } from "react-router-dom";

export function DemoBanner() {
  return (
    <div className="bg-sage-800 text-white text-center py-2 px-4 text-sm">
      <span className="opacity-90">Proposal MVP — UX prototype only. No real bookings or payments.</span>
      <span className="mx-3 opacity-40">|</span>
      <Link to="/vendor" className="underline hover:text-sage-200">Vendor panel</Link>
      <span className="mx-2 opacity-40">·</span>
      <Link to="/admin" className="underline hover:text-sage-200">Admin panel</Link>
    </div>
  );
}

