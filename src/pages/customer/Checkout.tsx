import { Navigate, useParams } from "react-router-dom";

/** Legacy route. Canonical payment is /requests/:requestId/payment. */
export function Checkout() {
  const { id } = useParams();
  if (id?.startsWith("HR-")) {
    return <Navigate to={`/requests/${id}/payment`} replace />;
  }
  return <Navigate to="/retreats" replace />;
}
