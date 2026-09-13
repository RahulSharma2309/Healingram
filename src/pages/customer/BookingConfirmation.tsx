import { Navigate } from "react-router-dom";

/** Legacy demo route. Real booking state lives on the request / trips APIs. */
export function BookingConfirmation() {
  return <Navigate to="/dashboard?tab=trips" replace />;
}
