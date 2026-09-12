import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { Shield } from "lucide-react";
import { fetchRetreatListing } from "../../lib/api/catalog";

export function Checkout() {
  const { id } = useParams();
  const [name, setName] = useState<string | null>(null);
  const [place, setPlace] = useState<string | null>(null);
  const [status, setStatus] = useState<"loading" | "ready" | "missing">("loading");

  useEffect(() => {
    if (!id) {
      setStatus("missing");
      return;
    }
    let cancelled = false;
    fetchRetreatListing(id)
      .then((listing) => {
        if (cancelled) return;
        setName(listing.name);
        setPlace(`${listing.locality}, ${listing.stateLabel}`);
        setStatus("ready");
      })
      .catch(() => {
        if (!cancelled) setStatus("missing");
      });
    return () => {
      cancelled = true;
    };
  }, [id]);

  return (
    <div className="max-w-4xl mx-auto px-4 py-10">
      <h1 className="font-display text-2xl font-bold text-sage-800 mb-8">Checkout</h1>
      <div className="bg-white rounded-xl border border-sand-200 p-6 max-w-xl">
        <p className="text-sm text-sage-600 mb-4">
          This page does not take payment and does not show dummy retreat prices. Request
          availability on a listing first. Only a verified webhook can mark a booking paid.
        </p>
        {status === "loading" && <p className="text-sm text-sage-500">Loading retreat…</p>}
        {status === "ready" && (
          <p className="font-medium text-sage-800">
            {name}
            {place ? <span className="block text-sm font-normal text-sage-600">{place}</span> : null}
          </p>
        )}
        {status === "missing" && (
          <p className="text-sm text-sage-600">That retreat is not in the published catalog.</p>
        )}
        <p className="flex items-center gap-1 text-xs text-gray-400 mt-4">
          <Shield className="w-3 h-3" /> Secure payment is on the request payment page, not here.
        </p>
        <Link
          to={id ? `/retreats/${id}` : "/retreats"}
          className="mt-6 inline-flex justify-center py-3 px-5 bg-teal-600 text-white font-medium rounded-xl hover:bg-teal-500"
        >
          Back to retreat
        </Link>
      </div>
    </div>
  );
}
