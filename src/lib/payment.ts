import { ApiError, apiErrorMessage } from "./api/client";
import {
  getPaymentIntentById,
  postAdminSimulatePayment,
  postPaymentIntent,
  type ServerPaymentIntent,
} from "./api/payment";
import { loadAvailabilityRequest } from "./availabilityRequests";

export type PaymentIntent = {
  requestId: string;
  serverIntentId?: string;
  amount: number;
  currency: "INR";
  settlementMode: "MARKETPLACE_SPLIT" | "PARTNER_DIRECT";
  status: "created" | "awaiting_provider" | "cancelled" | "ready";
  provider: "fake" | "placeholder";
  createdAt: string;
};

function sessionGet(key: string): string | null {
  try {
    return sessionStorage.getItem(key);
  } catch {
    return null;
  }
}

function sessionSet(key: string, value: string): void {
  try {
    sessionStorage.setItem(key, value);
  } catch {
    /* ignore */
  }
}

export function idempotencyKeyFor(publicId: string): string {
  const key = `healingram_pay_idem_${publicId}`;
  const existing = sessionGet(key);
  if (existing) return existing;
  const next = crypto.randomUUID();
  sessionSet(key, next);
  return next;
}

export function rememberedIntentId(publicId: string): string | null {
  return sessionGet(`healingram_pay_intent_${publicId}`);
}

function rememberIntentId(publicId: string, intentId: string): void {
  sessionSet(`healingram_pay_intent_${publicId}`, intentId);
}

export function paymentFailureMessage(error: unknown): string {
  if (error instanceof ApiError && error.status === 409) {
    return "This request is no longer available for payment.";
  }
  if (error instanceof ApiError) {
    return apiErrorMessage(error);
  }
  return "Could not create a payment intent on the server.";
}

export async function createPaymentIntent(
  requestId: string,
): Promise<PaymentIntent | { error: string }> {
  try {
    const server = await postPaymentIntent(requestId, idempotencyKeyFor(requestId));
    rememberIntentId(requestId, server.id);
    return {
      requestId,
      serverIntentId: server.id,
      amount: 0,
      currency: "INR",
      settlementMode: "MARKETPLACE_SPLIT",
      status: "ready",
      provider: "fake",
      createdAt: new Date().toISOString(),
    };
  } catch (error) {
    return { error: paymentFailureMessage(error) };
  }
}

export async function startPlaceholderCheckout(requestId: string): Promise<{
  ok: boolean;
  message: string;
  intentId?: string;
}> {
  const intent = await createPaymentIntent(requestId);
  if ("error" in intent) return { ok: false, message: intent.error };
  return {
    ok: true,
    intentId: intent.serverIntentId,
    message:
      "Payment intent is ready on the server. Status stays payment-pending until a verified webhook confirms it.",
  };
}

export async function simulateVerifiedPaymentWebhook(requestId: string): Promise<{
  ok: boolean;
  message: string;
}> {
  try {
    const request = await loadAvailabilityRequest(requestId);
    let intentId = rememberedIntentId(requestId);
    if (!intentId) {
      const created = await postPaymentIntent(requestId, idempotencyKeyFor(requestId));
      intentId = created.id;
      rememberIntentId(requestId, created.id);
    }

    const paid = await postAdminSimulatePayment(
      intentId,
      `demo_wh_${crypto.randomUUID()}`,
      request.finalPayableAmount ?? 0,
    );
    if (paid.status !== "paid" && paid.status !== "succeeded") {
      return { ok: false, message: "Server did not mark this intent paid." };
    }
    return { ok: true, message: `Verified webhook applied. Intent ${paid.id} is paid.` };
  } catch (error) {
    return {
      ok: false,
      message: error instanceof ApiError ? apiErrorMessage(error) : "Webhook was not accepted by the server.",
    };
  }
}

export async function refreshIntentStatus(intentId: string): Promise<ServerPaymentIntent> {
  return getPaymentIntentById(intentId);
}
