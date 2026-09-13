import { apiFetch } from "./client";

export type ServerPaymentIntent = {
  id: string;
  bookingId?: string;
  status: string;
  amount?: number;
  currency?: string;
  provider?: string;
  providerRef?: string | null;
  checkoutUrl?: string | null;
};

export async function postPaymentIntent(
  publicId: string,
  idempotencyKey: string,
): Promise<ServerPaymentIntent> {
  return apiFetch<ServerPaymentIntent>("/api/payment/intents", {
    method: "POST",
    body: JSON.stringify({ publicId, idempotencyKey }),
  });
}

export async function getPaymentIntentById(id: string): Promise<ServerPaymentIntent> {
  return apiFetch<ServerPaymentIntent>(`/api/payment/intents/${encodeURIComponent(id)}`);
}

/** Development/demo only. Uses the admin server action so the browser never holds a webhook secret. */
export async function postAdminSimulatePayment(
  intentId: string,
  providerEventId: string,
  amountInr: number,
  currency = "INR",
): Promise<ServerPaymentIntent> {
  return apiFetch<ServerPaymentIntent>("/api/admin/payments/simulate", {
    method: "POST",
    body: JSON.stringify({ intentId, providerEventId, amountInr, currency }),
  });
}
