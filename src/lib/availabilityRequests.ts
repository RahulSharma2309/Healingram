import type { PriceStatus, SettlementMode } from "./pricing";
import {
  type ServerAvailability,
  acceptAlternativeOnServer,
  fetchAdminQueue,
  fetchMyAvailabilityRequests,
  fetchPartnerQueue,
  getAvailabilityByPublicId,
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
  durationUnit: "nights";
  checkIn: string;
  checkOut: string;
  guests: number;
  occupancy: string;
  roomType: string;
  displayedPrice: string;
  priceStatus: PriceStatus;
  priceSnapshot: PriceSnapshot;
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
  bookingId: string | null;
  paymentMode: SettlementMode | null;
};

const EVENT = "healingram-requests";

let memory: AvailabilityRequest[] = [];

function emit(): void {
  window.dispatchEvent(new Event(EVENT));
}

function remember(request: AvailabilityRequest): AvailabilityRequest {
  const idx = memory.findIndex((item) => item.requestId === request.requestId);
  if (idx >= 0) memory[idx] = request;
  else memory.unshift(request);
  emit();
  return request;
}

export function listAvailabilityRequests(): AvailabilityRequest[] {
  return [...memory];
}

export function getAvailabilityRequest(requestId: string): AvailabilityRequest | undefined {
  return memory.find((item) => item.requestId === requestId);
}

export async function refreshMyRequests(): Promise<AvailabilityRequest[]> {
  const items = await fetchMyAvailabilityRequests();
  memory = items.map(mergeServerAvailability);
  emit();
  return listAvailabilityRequests();
}

export async function refreshPartnerRequests(): Promise<AvailabilityRequest[]> {
  const items = await fetchPartnerQueue();
  memory = items.map(mergeServerAvailability);
  emit();
  return listAvailabilityRequests();
}

export async function refreshAdminRequests(): Promise<AvailabilityRequest[]> {
  const items = await fetchAdminQueue();
  memory = items.map(mergeServerAvailability);
  emit();
  return listAvailabilityRequests();
}

export async function loadAvailabilityRequest(requestId: string): Promise<AvailabilityRequest> {
  const server = await getAvailabilityByPublicId(requestId);
  return remember(mergeServerAvailability(server));
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
  quoteId?: string;
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
    quoteId: input.quoteId,
  });
  return remember(mergeServerAvailability(server));
}

export async function partnerConfirmAvailability(
  requestId: string,
  confirmation: Omit<PartnerConfirmation, "confirmedAt">,
): Promise<AvailabilityRequest | null> {
  const server = await partnerConfirmOnServer(requestId, confirmation.finalAmount);
  return remember(mergeServerAvailability(server));
}

export async function partnerSuggestAlternative(
  requestId: string,
  alternative: Omit<AlternativeProposal, "proposedAt">,
): Promise<AvailabilityRequest | null> {
  const server = await partnerAlternativeOnServer(requestId, {
    ...alternative,
    proposedAt: new Date().toISOString(),
  });
  return remember(mergeServerAvailability(server));
}

export async function partnerMarkUnavailable(requestId: string, reason?: string): Promise<AvailabilityRequest | null> {
  const server = await partnerUnavailableOnServer(requestId, reason ?? "Dates not available");
  return remember(mergeServerAvailability(server));
}

export async function customerAcceptAlternative(requestId: string): Promise<AvailabilityRequest | null> {
  const server = await acceptAlternativeOnServer(requestId);
  return remember(mergeServerAvailability(server));
}

export function markPartnerViewed(_requestId: string): void {
  /* Partner view is recorded by the server when the queue is read. */
}

export function mapServerAvailabilityStatus(status: string): AvailabilityRequestStatus {
  switch (status.toUpperCase()) {
    case "REQUESTED":
      return "REQUESTED";
    case "ALTERNATIVE_OFFERED":
      return "ALTERNATIVE_PROPOSED";
    case "UNAVAILABLE":
      return "REJECTED";
    case "CONFIRMED":
      return "PAYMENT_PENDING";
    case "PAID":
      return "PAID";
    default:
      return "REQUESTED";
  }
}

function snapshotFromServer(item: ServerAvailability): PriceSnapshot {
  const snap = item.snapshot ?? {};
  const status = String(snap.priceStatus ?? "ON_REQUEST").toUpperCase();
  return {
    priceStatus: status === "VERIFIED" || status === "ESTIMATED" ? status : "ON_REQUEST",
    baseAmount: typeof snap.baseAmount === "number" ? snap.baseAmount : null,
    taxAmount: typeof snap.taxAmount === "number" ? snap.taxAmount : null,
    taxDisplay: "not_confirmed",
    totalAmount: typeof snap.totalAmount === "number" ? snap.totalAmount : null,
    occupancy: typeof snap.occupancy === "string" ? snap.occupancy : "pending",
    roomType: typeof snap.roomType === "string" ? snap.roomType : "",
    durationNights: typeof snap.durationNights === "number" ? snap.durationNights : 0,
    guests: typeof snap.guests === "number" ? snap.guests : 0,
    currency: "INR",
    label: typeof snap.label === "string" ? snap.label : "On request",
    capturedAt: typeof snap.capturedAt === "string" ? snap.capturedAt : new Date().toISOString(),
  };
}

export function mergeServerAvailability(item: ServerAvailability): AvailabilityRequest {
  const snapshot = snapshotFromServer(item);
  const now = item.requestedAt ?? new Date().toISOString();
  return {
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
    durationNights: snapshot.durationNights || 0,
    durationUnit: "nights",
    checkIn: typeof item.snapshot?.checkIn === "string" ? item.snapshot.checkIn : "",
    checkOut: "",
    guests: snapshot.guests || 0,
    occupancy: snapshot.occupancy === "pending" ? "" : String(snapshot.occupancy),
    roomType: snapshot.roomType,
    displayedPrice: snapshot.label,
    priceStatus: snapshot.priceStatus,
    priceSnapshot: snapshot,
    finalPayableAmount: item.finalAmountInr ?? snapshot.totalAmount,
    settlementMode: "MARKETPLACE_SPLIT",
    source: "listing",
    customerNotes: "",
    status: mapServerAvailabilityStatus(item.status),
    alternative: (item.alternative as AlternativeProposal | null) ?? null,
    partnerConfirmation: null,
    internalNotes: [],
    auditTrail: (item.history ?? []).map((h) => ({
      at: h.occurredAt,
      actor: "system",
      action: h.toStatus,
      detail: h.reason ?? undefined,
    })),
    requestedAt: now,
    partnerViewedAt: item.partnerViewedAt ?? null,
    partnerRespondedAt: item.partnerRespondedAt ?? null,
    createdAt: now,
    updatedAt: now,
    bookingId: null,
    paymentMode: null,
  };
}

export function mergeServerAvailabilityList(items: ServerAvailability[]): void {
  memory = items.map(mergeServerAvailability);
  emit();
}

export function listCustomerTrips(): AvailabilityRequest[] {
  return listAvailabilityRequests().filter((item) =>
    ["PAYMENT_PENDING", "PAID", "CONFIRMED", "COMPLETED"].includes(item.status),
  );
}

export function customerRequestsFor(_customerId: string | null): AvailabilityRequest[] {
  return listAvailabilityRequests();
}

export function partnerQueue(): AvailabilityRequest[] {
  return listAvailabilityRequests();
}

export function isAgingRequest(request: AvailabilityRequest): boolean {
  const started = Date.parse(request.requestedAt);
  if (Number.isNaN(started)) return false;
  return Date.now() - started > 24 * 60 * 60 * 1000 && request.status === "REQUESTED";
}

export function requestAgeLabel(request: AvailabilityRequest): string {
  const started = Date.parse(request.requestedAt);
  if (Number.isNaN(started)) return "";
  const hours = Math.max(0, Math.round((Date.now() - started) / (60 * 60 * 1000)));
  if (hours < 24) return `${hours}h`;
  return `${Math.round(hours / 24)}d`;
}

export async function adminAddInternalNote(requestId: string, note: string): Promise<void> {
  const { apiFetch } = await import("./api/client");
  await apiFetch(`/api/admin/availability/${encodeURIComponent(requestId)}/note`, {
    method: "POST",
    body: JSON.stringify({ note }),
  });
  await loadAvailabilityRequest(requestId);
}
