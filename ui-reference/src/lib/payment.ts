/**
 * Payment service interface — supports MARKETPLACE_SPLIT and PARTNER_DIRECT.
 * Does not mark bookings paid from browser success pages.
 */

import type { SettlementMode } from "../data/programmePricing";
import { applyPaymentWebhook, getAvailabilityRequest } from "./availabilityRequests";

export type PaymentIntent = {
  requestId: string;
  amount: number;
  currency: "INR";
  settlementMode: SettlementMode;
  status: "created" | "awaiting_provider" | "cancelled";
  provider: "placeholder";
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

export function createPaymentIntent(requestId: string): PaymentIntent | { error: string } {
  const request = getAvailabilityRequest(requestId);
  if (!request) return { error: "Request not found" };
  if (request.status !== "PAYMENT_PENDING") {
    return { error: "Request is not payment-ready" };
  }
  if (request.finalPayableAmount == null || request.finalPayableAmount <= 0) {
    return { error: "Final payable amount not confirmed" };
  }

  const intent: PaymentIntent = {
    requestId,
    amount: request.finalPayableAmount,
    currency: "INR",
    settlementMode: request.settlementMode,
    status: "awaiting_provider",
    provider: "placeholder",
    createdAt: new Date().toISOString(),
  };

  const all = readIntents().filter((i) => i.requestId !== requestId);
  all.unshift(intent);
  writeIntents(all);
  return intent;
}

export function getPaymentIntent(requestId: string): PaymentIntent | undefined {
  return readIntents().find((i) => i.requestId === requestId);
}

/**
 * Placeholder “Pay securely” — opens provider handoff message only.
 * Does NOT mark the request paid.
 */
export function startPlaceholderCheckout(requestId: string): {
  ok: boolean;
  message: string;
} {
  const intent = createPaymentIntent(requestId);
  if ("error" in intent) return { ok: false, message: intent.error };

  if (intent.settlementMode === "PARTNER_DIRECT") {
    return {
      ok: true,
      message:
        "Partner-direct settlement is configured. Payment will be completed with the retreat once the gateway is connected. No payment was taken.",
    };
  }

  return {
    ok: true,
    message:
      "Marketplace split payment gateway is not connected yet. No payment was taken. Status remains PAYMENT_PENDING until a verified webhook confirms payment.",
  };
}

/**
 * Admin-only simulator for a verified webhook (demo / staging).
 * Production must receive this from the payment provider server-side.
 */
export function simulateVerifiedPaymentWebhook(requestId: string): {
  ok: boolean;
  message: string;
} {
  const request = getAvailabilityRequest(requestId);
  if (!request) return { ok: false, message: "Request not found" };
  if (request.finalPayableAmount == null) {
    return { ok: false, message: "No final amount" };
  }

  const updated = applyPaymentWebhook(requestId, {
    providerPaymentId: `demo_wh_${Date.now()}`,
    amount: request.finalPayableAmount,
    verified: true,
  });

  if (!updated || updated.status !== "CONFIRMED") {
    return { ok: false, message: "Webhook did not confirm payment" };
  }

  return {
    ok: true,
    message: `Verified webhook applied. Booking ${updated.bookingId} confirmed.`,
  };
}
