import { Link } from "react-router-dom";
import { CheckCircle } from "lucide-react";

export function PaymentSuccess() {
  return (
    <div className="max-w-lg mx-auto px-4 py-20 text-center">
      <CheckCircle className="w-16 h-16 text-teal-600 mx-auto mb-4" />
      <h1 className="font-display text-2xl font-bold text-sage-800">Payment successful</h1>
      <p className="text-gray-600 mt-2">Your booking has been confirmed. A receipt was sent to your email.</p>
      <Link
        to="/booking-confirmation"
        className="inline-block mt-8 px-6 py-3 bg-teal-600 text-white rounded-xl font-medium hover:bg-teal-500"
      >
        View booking confirmation
      </Link>
    </div>
  );
}
