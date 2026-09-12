/**
 * Availability request store — local MVP “backend”.
 * Status model is shared across customer, partner, admin, and payment.
 */

import type { PriceStatus, SettlementMode } from "../data/programmePricing";
import {
  type ServerAvailability,
  acceptAlternativeOnServer,
  partnerAlternativeOnServer,
  partnerConfirmOnServer,
  partnerUnavailableOnServer,
  postAvailabilityRequest,
} from "./api/availability";
import type { PriceSnapshot } from "./pricing";

export type AvailabilityRequestStatus =
  | "REQUESTED"
  | "AVAILABLE"
  | "ALTERNATIVE_PROPOSED"
  | "PAYMENT_PENDING"
  | "PAID"
  | "CONFIRMED"
  | "COMPLETED"
  | "REJECTED"
  | "CANCELLED"
  | "REFUND_PENDING"
  | "REFUNDED";

export type AvailabilityRequestSource = "listing" | "find_my_match" | "admin" | "expert";

export type AlternativeProposal = {
  programmeId: string;
  programmeName: string;
  checkIn: string;
  checkOut: string;
  durationNights: number;
  guests: number;
  occupancy: string;
  roomType: string;
  finalAmount: number | null;
  taxesNote: string;
  inclusionsNote: string;
  proposedAt: string;
};

export type PartnerConfirmation = {
  programmeId: string;
  programmeName: string;
  checkIn: string;
  checkOut: string;
  durationNights: number;
  guests: number;
  occupancy: string;
  roomType: string;
  finalAmount: number;
  taxesNote: string;
  inclusionsNote: string;
  confirmedAt: string;
};

export type AuditEntry = {
  at: string;
  actor: "customer" | "partner" | "admin" | "system";
  action: string;
  detail?: string;
};

export type AvailabilityRequest = {
  requestId: string;
  customerId: string | null;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  countryCode: string;
  retreatId: string;
  retreatName: string;
  programmeId: string;
  programmeName: string;
  durationNights: number;
  /** Auditable unit — currently always nights */
  durationUnit: "nights";
  checkIn: string;
  checkOut: string;
  guests: number;
  occupancy: string;
  roomType: string;
  displayedPrice: string;
  priceStatus: PriceStatus;
  priceSnapshot: PriceSnapshot;
  /** Final amount after partner confirm / accepted alternative */
  finalPayableAmount: number | null;
  settlementMode: SettlementMode;
  source: AvailabilityRequestSource;
  customerNotes: string;
  status: AvailabilityRequestStatus;
  alternative: AlternativeProposal | null;
  partnerConfirmation: PartnerConfirmation | null;
  internalNotes: string[];
  auditTrail: AuditEntry[];
  requestedAt: string;
  partnerViewedAt: string | null;
  partnerRespondedAt: string | null;
  createdAt: string;
  updatedAt: string;
  /** Booking ID after confirmed payment — never invent */
  bookingId: string | null;
  paymentMode: SettlementMode | null;
};

export type NotificationChannel = "in_app" | "email_ready" | "whatsapp_later";

export type NotificationRecord = {
  id: string;
  audience: "customer" | "partner" | "admin";
  type: string;
  requestId: string;
  channel: NotificationChannel;
  title: string;
  body: string;
  createdAt: string;
  read: boolean;
};

const REQUESTS_KEY = "healingram_availability_requests_v1";
const NOTIFS_KEY = "healingram_notifications_v1";
const SEQ_KEY = "healingram_request_seq_v1";

function readJson<T>(key: string, fallback: T): T {
  try {
    const raw = localStorage.getItem(key);
    if (!raw) return fallback;
    return JSON.parse(raw) as T;
  } catch {
    return fallback;
  }
}

function writeJson(key: string, value: unknown): void {
  try {
    localStorage.setItem(key, JSON.stringify(value));
    window.dispatchEvent(new Event("healingram-requests"));
  } catch {
    /* ignore */
  }
}

function nextRequestId(): string {
  const year = new Date().getFullYear();
  let seq = 126;
  try {
    seq = Number(localStorage.getItem(SEQ_KEY) || "126");
    seq += 1;
    localStorage.setItem(SEQ_KEY, String(seq));
  } catch {
    seq = Date.now() % 100000;
  }
  return `HR-${year}-${String(seq).padStart(5, "0")}`;
}

function nextBookingId(): string {
  const year = new Date().getFullYear();
  const n = Math.floor(Math.random() * 90000) + 10000;
  return `BK-${year}-${n}`;
}

function audit(
  actor: AuditEntry["actor"],
  action: string,
  detail?: string,
): AuditEntry {
  return { at: new Date().toISOString(), actor, action, detail };
}

export function listAvailabilityRequests(): AvailabilityRequest[] {
  return readJson<AvailabilityRequest[]>(REQUESTS_KEY, []).sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
  );
}

export function getAvailabilityRequest(requestId: string): AvailabilityRequest | undefined {
  return listAvailabilityRequests().find((r) => r.requestId === requestId);
}

export function saveAvailabilityRequests(all: AvailabilityRequest[]): void {
  writeJson(REQUESTS_KEY, all);
}

function upsert(request: AvailabilityRequest): AvailabilityRequest {
  const all = listAvailabilityRequests();
  const idx = all.findIndex((r) => r.requestId === request.requestId);
  if (idx >= 0) all[idx] = request;
  else all.unshift(request);
  saveAvailabilityRequests(all);
  return request;
}

export async function createAvailabilityRequest(input: {
  customerId: string | null;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  countryCode: string;
  retreatId: string;
  retreatName: string;
  programmeId: string;
  programmeName: string;
  durationNights: number;
  durationUnit: "nights";
  checkIn: string;
  checkOut: string;
  guests: number;
  occupancy: string;
  roomType: string;
  displayedPrice: string;
  priceStatus: PriceStatus;
  priceSnapshot: PriceSnapshot;
  settlementMode: SettlementMode;
  source: AvailabilityRequestSource;
  customerNotes: string;
}): Promise<AvailabilityRequest> {
  const server = await postAvailabilityRequest({
    retreatSlug: input.retreatId,
    programmeSlug: input.programmeId,
    durationNights: input.durationNights,
    occupancy: input.occupancy,
    guests: input.guests,
    checkIn: input.checkIn,
    customerName: input.customerName,
    email: input.customerEmail,
    phone: input.customerPhone,
  });
  const request = buildLocalRequest(input, server.publicId);
  persistNewRequest(request);
  return request;
}

function buildLocalRequest(
  input: Parameters<typeof createAvailabilityRequest>[0],
  requestId: string,
): AvailabilityRequest {
  const now = new Date().toISOString();
  const request: AvailabilityRequest = {
    ...input,
    durationUnit: input.durationUnit ?? "nights",
    requestId,
    finalPayableAmount: input.priceSnapshot.totalAmount,
    status: "REQUESTED",
    alternative: null,
    partnerConfirmation: null,
    internalNotes: [],
    auditTrail: [audit("customer", "REQUESTED", "Customer submitted availability request")],
    requestedAt: now,
    partnerViewedAt: null,
    partnerRespondedAt: null,
    createdAt: now,
    updatedAt: now,
    bookingId: null,
    paymentMode: null,
  };

  return request;
}

function persistNewRequest(request: AvailabilityRequest): void {
  const requestId = request.requestId;
  upsert(request);
  pushNotification({
    audience: "customer",
    type: "request_received",
    requestId,
    title: "Availability request received",
    body: `We received ${requestId}. No payment is required yet.`,
  });
  pushNotification({
    audience: "partner",
    type: "new_availability_request",
    requestId,
    title: "New availability request",
    body: `${request.customerName} requested ${request.programmeName} at ${request.retreatName}.`,
  });
  pushNotification({
    audience: "admin",
    type: "new_availability_request",
    requestId,
    title: "New availability request",
    body: `${requestId} · ${request.retreatName}`,
  });
}

export function markPartnerViewed(requestId: string): void {
  const r = getAvailabilityRequest(requestId);
  if (!r || r.partnerViewedAt) return;
  r.partnerViewedAt = new Date().toISOString();
  r.updatedAt = r.partnerViewedAt;
  r.auditTrail.push(audit("partner", "VIEWED"));
  upsert(r);
}

export async function partnerConfirmAvailability(
  requestId: string,
  confirmation: Omit<PartnerConfirmation, "confirmedAt">,
): Promise<AvailabilityRequest | null> {
  const r = getAvailabilityRequest(requestId);
  if (!r) return null;
  await partnerConfirmOnServer(requestId, confirmation.finalAmount);
  const now = new Date().toISOString();
  r.partnerConfirmation = { ...confirmation, confirmedAt: now };
  r.finalPayableAmount = confirmation.finalAmount;
  r.status = "PAYMENT_PENDING";
  r.partnerRespondedAt = now;
  r.updatedAt = now;
  r.auditTrail.push(
    audit("partner", "AVAILABLE→PAYMENT_PENDING", `Final ${confirmation.finalAmount}`),
  );
  upsert(r);

  pushNotification({
    audience: "customer",
    type: "availability_confirmed",
    requestId,
    title: "Your retreat is available",
    body: "Continue to payment when you are ready. No charge until you pay.",
  });
  pushNotification({
    audience: "customer",
    type: "payment_pending",
    requestId,
    title: "Payment pending",
    body: `${requestId} is ready for payment.`,
  });

  return r;
}

export async function partnerSuggestAlternative(
  requestId: string,
  alternative: Omit<AlternativeProposal, "proposedAt">,
): Promise<AvailabilityRequest | null> {
  const r = getAvailabilityRequest(requestId);
  if (!r) return null;
  const proposed = { ...alternative, proposedAt: new Date().toISOString() };
  await partnerAlternativeOnServer(requestId, proposed);
  const now = proposed.proposedAt;
  r.alternative = proposed;
  r.status = "ALTERNATIVE_PROPOSED";
  r.partnerRespondedAt = now;
  r.updatedAt = now;
  r.auditTrail.push(audit("partner", "ALTERNATIVE_PROPOSED"));
  upsert(r);

  pushNotification({
    audience: "customer",
    type: "alternative_proposed",
    requestId,
    title: "Alternative dates proposed",
    body: "The retreat suggested a different option for your request.",
  });

  return r;
}

export async function partnerMarkUnavailable(requestId: string, reason?: string): Promise<AvailabilityRequest | null> {
  const r = getAvailabilityRequest(requestId);
  if (!r) return null;
  await partnerUnavailableOnServer(requestId, reason ?? "Dates not available");
  const now = new Date().toISOString();
  r.status = "REJECTED";
  r.partnerRespondedAt = now;
  r.updatedAt = now;
  r.auditTrail.push(audit("partner", "REJECTED", reason));
  upsert(r);

  pushNotification({
    audience: "customer",
    type: "unavailable",
    requestId,
    title: "Those dates aren’t available",
    body: "Try different dates or see similar retreats.",
  });

  return r;
}

export function customerAcceptAlternative(requestId: string): AvailabilityRequest | null {
  const r = getAvailabilityRequest(requestId);
  if (!r?.alternative) return null;
  const alt = r.alternative;
  const now = new Date().toISOString();

  r.programmeId = alt.programmeId;
  r.programmeName = alt.programmeName;
  r.checkIn = alt.checkIn;
  r.checkOut = alt.checkOut;
  r.durationNights = alt.durationNights;
  r.guests = alt.guests;
  r.occupancy = alt.occupancy;
  r.roomType = alt.roomType;
  r.finalPayableAmount = alt.finalAmount;
  r.priceSnapshot = {
    ...r.priceSnapshot,
    // Preserve original snapshot fields; append accepted alternative as working total
    totalAmount: alt.finalAmount,
    label: alt.finalAmount != null ? `₹${alt.finalAmount.toLocaleString("en-IN")}` : r.priceSnapshot.label,
    durationNights: alt.durationNights,
    guests: alt.guests,
    roomType: alt.roomType,
    occupancy: "pending",
    capturedAt: r.priceSnapshot.capturedAt, // original capture time preserved
  };
  r.displayedPrice =
    alt.finalAmount != null
      ? `₹${alt.finalAmount.toLocaleString("en-IN")}`
      : r.displayedPrice;
  r.status = "PAYMENT_PENDING";
  r.updatedAt = now;
  r.auditTrail.push(audit("customer", "ACCEPT_ALTERNATIVE→PAYMENT_PENDING"));
  upsert(r);

  pushNotification({
    audience: "partner",
    type: "customer_accepted_alternative",
    requestId,
    title: "Customer accepted alternative",
    body: `${r.customerName} accepted the proposed option.`,
  });
  pushNotification({
    audience: "customer",
    type: "payment_pending",
    requestId,
    title: "Payment pending",
    body: "Your accepted option is ready for payment.",
  });

  void acceptAlternativeOnServer(requestId).catch(() => {
    /* keep local status if API is down */
  });

  return r;
}

export function customerDeclineAlternative(requestId: string): AvailabilityRequest | null {
  const r = getAvailabilityRequest(requestId);
  if (!r) return null;
  r.status = "CANCELLED";
  r.updatedAt = new Date().toISOString();
  r.auditTrail.push(audit("customer", "DECLINE_ALTERNATIVE→CANCELLED"));
  upsert(r);
  return r;
}

export function customerRequestAnotherOption(requestId: string): AvailabilityRequest | null {
  const r = getAvailabilityRequest(requestId);
  if (!r) return null;
  r.status = "REQUESTED";
  r.alternative = null;
  r.updatedAt = new Date().toISOString();
  r.auditTrail.push(audit("customer", "REQUEST_ANOTHER_OPTION→REQUESTED"));
  upsert(r);
  pushNotification({
    audience: "partner",
    type: "new_availability_request",
    requestId,
    title: "Customer requested another option",
    body: `${r.requestId} needs a new proposal.`,
  });
  return r;
}

/** Admin / system: move to payment-ready without inventing payment success */
export function adminSetPaymentPending(
  requestId: string,
  finalAmount: number,
  note?: string,
): AvailabilityRequest | null {
  const r = getAvailabilityRequest(requestId);
  if (!r) return null;
  r.finalPayableAmount = finalAmount;
  r.status = "PAYMENT_PENDING";
  r.updatedAt = new Date().toISOString();
  if (note) r.internalNotes.push(note);
  r.auditTrail.push(audit("admin", "PAYMENT_PENDING", note));
  upsert(r);
  pushNotification({
    audience: "customer",
    type: "payment_pending",
    requestId,
    title: "Payment pending",
    body: `${requestId} is ready for payment.`,
  });
  return r;
}

export function adminUpdateStatus(
  requestId: string,
  status: AvailabilityRequestStatus,
  note?: string,
): AvailabilityRequest | null {
  const r = getAvailabilityRequest(requestId);
  if (!r) return null;
  const prev = r.status;
  r.status = status;
  r.updatedAt = new Date().toISOString();
  if (note) r.internalNotes.push(note);
  r.auditTrail.push(audit("admin", `STATUS ${prev}→${status}`, note));
  upsert(r);
  return r;
}

export function adminAddInternalNote(requestId: string, note: string): AvailabilityRequest | null {
  const r = getAvailabilityRequest(requestId);
  if (!r) return null;
  r.internalNotes.push(note);
  r.updatedAt = new Date().toISOString();
  r.auditTrail.push(audit("admin", "INTERNAL_NOTE", note));
  upsert(r);
  return r;
}

export function adminConfirmFinalPrice(
  requestId: string,
  amount: number,
): AvailabilityRequest | null {
  const r = getAvailabilityRequest(requestId);
  if (!r) return null;
  r.finalPayableAmount = amount;
  r.updatedAt = new Date().toISOString();
  r.auditTrail.push(audit("admin", "CONFIRM_FINAL_PRICE", String(amount)));
  upsert(r);
  return r;
}

/**
 * Simulate provider webhook — ONLY path that may set PAID → CONFIRMED.
 * Never call from a browser “success page” alone in production logic.
 */
export function applyPaymentWebhook(
  requestId: string,
  payload: { providerPaymentId: string; amount: number; verified: boolean },
): AvailabilityRequest | null {
  const r = getAvailabilityRequest(requestId);
  if (!r) return null;
  if (!payload.verified) {
    r.auditTrail.push(audit("system", "WEBHOOK_REJECTED", "Unverified payment event"));
    upsert(r);
    return r;
  }
  if (r.status !== "PAYMENT_PENDING" && r.status !== "PAID") {
    r.auditTrail.push(audit("system", "WEBHOOK_IGNORED", `Unexpected status ${r.status}`));
    upsert(r);
    return r;
  }

  const now = new Date().toISOString();
  r.status = "PAID";
  r.updatedAt = now;
  r.auditTrail.push(
    audit("system", "PAID", `Webhook ${payload.providerPaymentId} · ${payload.amount}`),
  );
  // Auto-confirm after verified paid for V1
  r.status = "CONFIRMED";
  r.bookingId = r.bookingId ?? nextBookingId();
  r.paymentMode = r.settlementMode;
  r.auditTrail.push(audit("system", "CONFIRMED", r.bookingId));
  upsert(r);

  pushNotification({
    audience: "customer",
    type: "payment_successful",
    requestId,
    title: "Payment successful",
    body: `Booking ${r.bookingId} is confirmed.`,
  });
  pushNotification({
    audience: "customer",
    type: "booking_confirmed",
    requestId,
    title: "Your retreat is confirmed",
    body: `${r.retreatName} · ${r.checkIn} → ${r.checkOut}`,
  });
  pushNotification({
    audience: "partner",
    type: "payment_confirmed",
    requestId,
    title: "Payment confirmed",
    body: `${r.requestId} is paid and confirmed.`,
  });

  return r;
}

export function requestAgeLabel(iso: string): string {
  const ms = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(ms / 60000);
  if (mins < 60) return `${Math.max(mins, 0)}m`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 48) return `${hrs}h`;
  return `${Math.floor(hrs / 24)}d`;
}

export function isAgingRequest(r: AvailabilityRequest): boolean {
  if (r.status !== "REQUESTED" && r.status !== "ALTERNATIVE_PROPOSED") return false;
  const hrs = (Date.now() - new Date(r.requestedAt).getTime()) / 3600000;
  return hrs >= 24;
}

export function listNotifications(audience?: NotificationRecord["audience"]): NotificationRecord[] {
  const all = readJson<NotificationRecord[]>(NOTIFS_KEY, []);
  return audience ? all.filter((n) => n.audience === audience) : all;
}

export function pushNotification(input: {
  audience: NotificationRecord["audience"];
  type: string;
  requestId: string;
  title: string;
  body: string;
  channel?: NotificationChannel;
}): void {
  const all = listNotifications();
  all.unshift({
    id: `n-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
    ...input,
    channel: input.channel ?? "in_app",
    createdAt: new Date().toISOString(),
    read: false,
  });
  writeJson(NOTIFS_KEY, all.slice(0, 200));
}

export function resendNotification(
  requestId: string,
  audience: NotificationRecord["audience"],
  type: string,
  title: string,
  body: string,
): void {
  pushNotification({
    audience,
    type,
    requestId,
    title,
    body,
    channel: "email_ready",
  });
}

export function listCustomerTrips(): AvailabilityRequest[] {
  return listAvailabilityRequests().filter((r) =>
    ["CONFIRMED", "COMPLETED", "PAID", "PAYMENT_PENDING", "AVAILABLE"].includes(r.status),
  );
}

export function listCustomerRequestsByContact(email: string, phone?: string): AvailabilityRequest[] {
  const e = email.trim().toLowerCase();
  const p = phone?.replace(/\D/g, "") ?? "";
  return listAvailabilityRequests().filter((r) => {
    if (e && r.customerEmail.toLowerCase() === e) return true;
    if (p && r.customerPhone.replace(/\D/g, "").endsWith(p)) return true;
    return false;
  });
}

export function mapServerAvailabilityStatus(
  status: string,
  local?: AvailabilityRequestStatus,
): AvailabilityRequestStatus {
  switch (status.toUpperCase()) {
    case "REQUESTED":
      return "REQUESTED";
    case "ALTERNATIVE_OFFERED":
      return "ALTERNATIVE_PROPOSED";
    case "UNAVAILABLE":
      return "REJECTED";
    case "CONFIRMED":
      if (local === "PAID" || local === "CONFIRMED" || local === "COMPLETED") return local;
      return "PAYMENT_PENDING";
    case "PAID":
      return "PAID";
    default:
      return local ?? "REQUESTED";
  }
}

function placeholderSnapshot(): PriceSnapshot {
  return {
    priceStatus: "ON_REQUEST",
    baseAmount: null,
    taxAmount: null,
    taxDisplay: "not_confirmed",
    totalAmount: null,
    occupancy: "pending",
    roomType: "",
    durationNights: 7,
    guests: 2,
    currency: "INR",
    label: "On request",
    capturedAt: new Date().toISOString(),
  };
}

export function mergeServerAvailability(item: ServerAvailability): AvailabilityRequest {
  const existing = getAvailabilityRequest(item.publicId);
  const status = mapServerAvailabilityStatus(item.status, existing?.status);
  if (existing) {
    existing.status = status;
    if (item.finalAmountInr != null) existing.finalPayableAmount = item.finalAmountInr;
    if (item.customerName) existing.customerName = item.customerName;
    existing.updatedAt = new Date().toISOString();
    upsert(existing);
    return existing;
  }

  const now = item.requestedAt ?? new Date().toISOString();
  const request: AvailabilityRequest = {
    requestId: item.publicId,
    customerId: null,
    customerName: item.customerName ?? "Guest",
    customerEmail: item.email ?? "",
    customerPhone: item.phone ?? "",
    countryCode: "+91",
    retreatId: item.retreatSlug ?? "",
    retreatName: item.retreatSlug ?? "Retreat",
    programmeId: item.programmeSlug ?? "",
    programmeName: item.programmeSlug ?? "Programme",
    durationNights: 7,
    durationUnit: "nights",
    checkIn: "",
    checkOut: "",
    guests: 2,
    occupancy: "",
    roomType: "",
    displayedPrice: "On request",
    priceStatus: "ON_REQUEST",
    priceSnapshot: placeholderSnapshot(),
    finalPayableAmount: item.finalAmountInr ?? null,
    settlementMode: "MARKETPLACE_SPLIT",
    source: "listing",
    customerNotes: "",
    status,
    alternative: null,
    partnerConfirmation: null,
    internalNotes: [],
    auditTrail: [audit("system", item.status, "Synced from server")],
    requestedAt: now,
    partnerViewedAt: null,
    partnerRespondedAt: null,
    createdAt: now,
    updatedAt: now,
    bookingId: null,
    paymentMode: null,
  };
  upsert(request);
  return request;
}

export function mergeServerAvailabilityList(items: ServerAvailability[]): void {
  for (const item of items) mergeServerAvailability(item);
}
