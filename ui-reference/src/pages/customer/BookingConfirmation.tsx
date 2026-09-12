import { Link } from "react-router-dom";
import { Calendar, Download, MapPin } from "lucide-react";

export function BookingConfirmation() {
  return (
    <div className="max-w-2xl mx-auto px-4 py-10">
      <div className="bg-white rounded-2xl border border-sand-200 overflow-hidden">
        <div className="bg-teal-600 text-white px-6 py-4">
          <p className="text-sm opacity-90">Booking ID</p>
          <p className="font-mono text-lg font-bold">ST-2026-78421</p>
        </div>
        <div className="p-6">
          <h1 className="font-display text-xl font-bold text-sage-800">Himalayan Mindfulness Retreat</h1>
          <p className="flex items-center gap-2 text-sage-600 mt-2 text-sm">
            <MapPin className="w-4 h-4" /> Rishikesh, Uttarakhand
          </p>
          <p className="flex items-center gap-2 text-gray-600 mt-2 text-sm">
            <Calendar className="w-4 h-4" /> 20–22 Jun 2026 · 3 days / 2 nights
          </p>
          <hr className="my-6 border-sand-200" />
          <p className="text-sm text-gray-600">Guest: Demo User · demo@email.com</p>
          <p className="text-sm text-gray-600 mt-1">Package: Standard — shared room</p>
          <p className="font-bold text-sage-800 mt-4">Total paid: ₹19,000</p>
          <div className="flex gap-3 mt-6">
            <button type="button" className="flex items-center gap-2 px-4 py-2 border border-sand-200 rounded-lg text-sm hover:bg-sand-50">
              <Download className="w-4 h-4" /> Download voucher
            </button>
            <Link to="/dashboard" className="px-4 py-2 bg-sage-700 text-white rounded-lg text-sm hover:bg-sage-600">
              My bookings
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
