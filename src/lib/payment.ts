/**
 * Payment display helpers. The browser never marks a booking paid.
 * Server PaymentService + IPaymentProvider is the source of truth.
 * localStorage caches checkout UX only.
 */

import type { SettlementMode } from "../data/programmePricing";
import {
  getPaymentIntentById,
  postFakePaymentWebhook,
  postPaymentIntent,
  type ServerPaymentIntent,
} from "./api/payment";
import { applyPaymentWebhook, getAvailabilityRequest } from "./availabilityRequests";

export type PaymentIntent = {
  requestId: string;
  serverIntentId?: string;
  amount: number;
  currency: "INR";
  settlementMode: SettlementMode;
  status: "created" | "awaiting_provider" | "cancelled" | "ready";
  provider: "fake" | "placeholder";
  createdAt: string;
};

const INTENT_KEY = "healingram_payment_intents_v1";

function readIntents(): PaymentIntent[] {
  try {
    return JSON.parse(localStorage.getItem(INTENT_KEY) || "[]") as PaymentIntent[];
  } catch {
    return [];
  }
}

function writeIntents(intents: PaymentIntent[]): void {
  try {
    localStorage.setItem(INTENT_KEY, JSON.stringify(intents));
  } catch {
    /* ignore */
  }
}

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

function persistLocalIntent(intent: PaymentIntent): void {
  const all = readIntents().filter((i) => i.requestId !== intent.requestId);
  all.unshift(intent);
  writeIntents(all);
}

export async function createPaymentIntent(
  requestId: string,
): Promise<PaymentIntent | { error: string }> {
  const request = getAvailabilityRequest(requestId);
  if (!request) return { error: "Request not found" };
  if (request.status !== "PAYMENT_PENDING") {
    return { error: "Request is not payment-ready" };
  }
  if (request.finalPayableAmount == null || request.finalPayableAmount <= 0) {
    return { error: "Final payable amount not confirmed" };
  }

  try {
    const server = await postPaymentIntent(requestId, idempotencyKeyFor(requestId));
    rememberIntentId(requestId, server.id);
    const intent: PaymentIntent = {
      requestId,
      serverIntentId: server.id,
      amount: request.finalPayableAmount,
      currency: "INR",
      settlementMode: request.settlementMode,
      status: "ready",
      provider: "fake",
      createdAt: new Date().toISOString(),
    };
    persistLocalIntent(intent);
    return intent;
  } catch {
    return {
      error:
        "Could not create a payment intent on the server. Confirm availability as a partner first, then try again. No payment was taken.",
    };
  }
}

export function getPaymentIntent(requestId: string): PaymentIntent | undefined {
  return readIntents().find((i) => i.requestId === requestId);
}

/**
 * Opens payment-ready handoff only. Does not mark the request paid.
 */
export async function startPlaceholderCheckout(requestId: string): Promise<{
  ok: boolean;
  message: string;
  intentId?: string;
}> {
  const intent = await createPaymentIntent(requestId);
  if ("error" in intent) return { ok: false, message: intent.error };

  if (intent.settlementMode === "PARTNER_DIRECT") {
    return {
      ok: true,
      intentId: intent.serverIntentId,
      message:
        "Partner-direct settlement is configured. The retreat will complete payment separately. No payment was taken here, and this page did not mark the booking paid.",
    };
  }

  return {
    ok: true,
    intentId: intent.serverIntentId,
    message:
      "Payment intent is ready on the server. Status stays payment-pending until a verified webhook confirms it. This page does not mark the booking paid.",
  };
}

/**
 * Local admin stand-in for the fake provider. Calls the server webhook.
 * Does not mark paid unless the server returns status paid.
 */
export async function simulateVerifiedPaymentWebhook(requestId: string): Promise<{
  ok: boolean;
  message: string;
}> {
  const request = getAvailabilityRequest(requestId);
  if (!request) return { ok: false, message: "Request not found" };
  if (request.finalPayableAmount == null) {
    return { ok: false, message: "No final amount" };
  }

  let intentId = rememberedIntentId(requestId);
  try {
    if (!intentId) {
      const created = await postPaymentIntent(requestId, idempotencyKeyFor(requestId));
      intentId = created.id;
      rememberIntentId(requestId, created.id);
    }

    const paid = await postFakePaymentWebhook(
      intentId,
      `demo_wh_${crypto.randomUUID()}`,
      request.finalPayableAmount,
    );
    if (paid.status !== "paid" && paid.status !== "succeeded") {
      return { ok: false, message: "Server did not mark this intent paid." };
    }

    applyPaymentWebhook(requestId, {
      providerPaymentId: paid.id,
      amount: request.finalPayableAmount,
      verified: true,
    });

    return {
      ok: true,
      message: `Verified webhook applied. Intent ${paid.id} is paid.`,
    };
  } catch {
    return {
      ok: false,
      message:
        "Webhook was not accepted by the server. Sign in as admin/partner if needed, confirm availability on the API, then try again. The browser did not mark this paid.",
    };
  }
}

export async function refreshIntentStatus(intentId: string): Promise<ServerPaymentIntent> {
  return getPaymentIntentById(intentId);
}
