import { apiFetch } from "./client";

export type ServerPaymentIntent = {
  id: string;
  status: string;
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

export async function postFakePaymentWebhook(
  intentId: string,
  providerEventId: string,
): Promise<ServerPaymentIntent> {
  const secret =
    (import.meta.env.VITE_FAKE_WEBHOOK_SECRET as string | undefined) ?? "local-dev-webhook-secret";
  return apiFetch<ServerPaymentIntent>("/api/payment/webhooks/fake", {
    method: "POST",
    headers: { "X-Webhook-Secret": secret },
    body: JSON.stringify({ intentId, providerEventId }),
  });
}
