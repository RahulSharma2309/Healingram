import { Link, useSearchParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { CheckCircle } from "lucide-react";
import { refreshIntentStatus } from "../../lib/payment";

export function PaymentSuccess() {
  const [params] = useSearchParams();
  const intentId = params.get("intent");
  const [state, setState] = useState<"checking" | "paid" | "pending" | "unknown">("checking");

  useEffect(() => {
    if (!intentId) {
      setState("unknown");
      return;
    }

    let cancelled = false;
    const tick = async () => {
      try {
        const intent = await refreshIntentStatus(intentId);
        if (cancelled) return;
        setState(intent.status === "paid" ? "paid" : "pending");
      } catch {
        if (!cancelled) setState("unknown");
      }
    };

    tick();
    const timer = window.setInterval(tick, 2500);
    return () => {
      cancelled = true;
      window.clearInterval(timer);
    };
  }, [intentId]);

  if (state === "paid") {
    return (
      <div className="max-w-lg mx-auto px-4 py-20 text-center">
        <CheckCircle className="w-16 h-16 text-teal-600 mx-auto mb-4" />
        <h1 className="font-display text-2xl font-bold text-sage-800">Payment successful</h1>
        <p className="text-gray-600 mt-2">
          A verified server webhook confirmed this payment. Opening this page did not change the
          status.
        </p>
        <Link
          to="/dashboard"
          className="inline-block mt-8 px-6 py-3 bg-teal-600 text-white rounded-xl font-medium hover:bg-teal-500"
        >
          My dashboard
        </Link>
      </div>
    );
  }

  return (
    <div className="max-w-lg mx-auto px-4 py-20 text-center">
      <h1 className="font-display text-2xl font-bold text-sage-800">Payment not confirmed yet</h1>
      <p className="text-sage-600 mt-3 text-sm leading-relaxed">
        This return page is read-only. Healingram only marks a booking paid after a verified
        provider webhook. If you just paid, wait a moment or return to your request.
      </p>
      <Link to="/dashboard" className="inline-block mt-8 text-teal-600 font-medium">
        My dashboard
      </Link>
    </div>
  );
}
