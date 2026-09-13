import { Link, useParams } from "react-router-dom";
import { retreats, formatPrice } from "../../data/mockData";
import { CreditCard, Shield } from "lucide-react";

export function Checkout() {
  const { id } = useParams();
  const retreat = retreats.find((r) => r.id === id) ?? retreats[0];
  const total = retreat.price + 500;

  return (
    <div className="max-w-4xl mx-auto px-4 py-10">
      <h1 className="font-display text-2xl font-bold text-sage-800 mb-8">Checkout</h1>
      <div className="grid md:grid-cols-2 gap-8">
        <form
          className="space-y-4"
          onSubmit={(e) => {
            e.preventDefault();
            window.location.href = "/payment-success";
          }}
        >
          <div className="bg-white rounded-xl border border-sand-200 p-6">
            <h2 className="font-semibold mb-4">Guest details</h2>
            {["Full name", "Email", "Phone"].map((label) => (
              <label key={label} className="block mb-3">
                <span className="text-xs text-gray-500">{label}</span>
                <input className="w-full mt-1 border border-sand-200 rounded-lg px-3 py-2" required />
              </label>
            ))}
          </div>
          <div className="bg-white rounded-xl border border-sand-200 p-6">
            <h2 className="font-semibold mb-4 flex items-center gap-2">
              <CreditCard className="w-5 h-5" /> Payment (Razorpay demo)
            </h2>
            <p className="text-sm text-gray-500 mb-4">UPI, cards, net banking — integration in Phase 2</p>
            <button type="submit" className="w-full py-3 bg-teal-600 text-white font-medium rounded-xl hover:bg-teal-500">
              Pay {formatPrice(total)}
            </button>
            <p className="flex items-center justify-center gap-1 text-xs text-gray-400 mt-3">
              <Shield className="w-3 h-3" /> Secure payment
            </p>
          </div>
        </form>
        <div className="bg-sage-50 rounded-xl p-6 h-fit">
          <h2 className="font-semibold mb-4">Booking summary</h2>
          <p className="font-medium">{retreat.name}</p>
          <p className="text-sm text-gray-600">{retreat.location} · {retreat.duration}</p>
          <hr className="my-4 border-sand-200" />
          <div className="flex justify-between text-sm">
            <span>Retreat fee</span>
            <span>{formatPrice(retreat.price)}</span>
          </div>
          <div className="flex justify-between text-sm mt-2">
            <span>Platform fee</span>
            <span>{formatPrice(500)}</span>
          </div>
          <div className="flex justify-between font-bold mt-4">
            <span>Total</span>
            <span>{formatPrice(total)}</span>
          </div>
          <Link to={`/retreats/${retreat.id}`} className="text-sm text-teal-600 mt-4 inline-block">
            Change retreat
          </Link>
        </div>
      </div>
    </div>
  );
}
