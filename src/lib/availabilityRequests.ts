import type { PriceStatus, SettlementMode } from "./pricing";
import {
  type ServerAvailability,
  acceptAlternativeOnServer,
  cancelAvailabilityOnServer,
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
  countryCode: string | null;
  retreatId: string;
  retreatName: string | null;
  programmeId: string;
  programmeName: string | null;
  durationNights: number | null;
  durationUnit: "nights";
  checkIn: string | null;
  checkOut: string | null;
  guests: number | null;
  occupancy: string | null;
  roomType: string | null;
  displayedPrice: string | null;
  priceStatus: PriceStatus | null;
  priceSnapshot: PriceSnapshot | null;
  finalPayableAmount: number | null;
  settlementMode: SettlementMode | null;
  source: AvailabilityRequestSource | null;
  customerNotes: string | null;
  status: AvailabilityRequestStatus;
  alternative: AlternativeProposal | null;
  partnerConfirmation: PartnerConfirmation | null;
  internalNotes: string[];
  auditTrail: AuditEntry[];
  requestedAt: string | null;
  partnerViewedAt: string | null;
  partnerRespondedAt: string | null;
  createdAt: string | null;
  updatedAt: string | null;
  bookingId: string | null;
  paymentMode: SettlementMode | null;
};

function asString(value: unknown): string | null {
  return typeof value === "string" && value.trim().length > 0 ? value : null;
}

function asNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function snapshotFromServer(item: ServerAvailability): PriceSnapshot | null {
  const snap = item.snapshot;
  if (!snap) {
    return null;
  }

  const status = asString(snap.priceStatus)?.toUpperCase();
  const capturedAt = asString(snap.capturedAt) ?? item.requestedAt;
  if (!capturedAt) {
    return null;
  }

  return {
    priceStatus: status === "VERIFIED" || status === "ESTIMATED" ? status : "ON_REQUEST",
    baseAmount: asNumber(snap.baseAmount),
    taxAmount: asNumber(snap.taxAmount),
    taxDisplay: "not_confirmed",
    totalAmount: asNumber(snap.totalAmount),
    occupancy: asString(snap.occupancy) ?? "",
    roomType: asString(snap.roomType) ?? "",
    durationNights: asNumber(snap.durationNights) ?? 0,
    guests: asNumber(snap.guests) ?? 0,
    currency: "INR",
    label: asString(snap.label) ?? "",
    capturedAt,
  };
}

function sourceFromServer(value: string | null | undefined): AvailabilityRequestSource | null {
  return value === "listing" || value === "find_my_match" || value === "admin" || value === "expert"
    ? value
    : null;
}

function settlementFromServer(value: string | null | undefined): SettlementMode | null {
  return value === "MARKETPLACE_SPLIT" || value === "PARTNER_DIRECT"
    ? value
    : null;
}

/** Display aliases of server statuses. Not invented business state. */
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
    case "CANCELLED":
      return "CANCELLED";
    case "REFUND_PENDING":
      return "REFUND_PENDING";
    case "REFUNDED":
      return "REFUNDED";
    case "COMPLETED":
      return "COMPLETED";
    default:
      return status.toUpperCase() as AvailabilityRequestStatus;
  }
}

export function toAvailabilityRequest(item: ServerAvailability): AvailabilityRequest {
  const snapshot = snapshotFromServer(item);
  const requestedAt = item.requestedAt ?? null;
  return {
    requestId: item.publicId,
    customerId: item.customerUserId ?? null,
    customerName: item.customerName ?? "",
    customerEmail: item.email ?? "",
    customerPhone: item.phone ?? "",
    countryCode: item.countryCode ?? asString(item.snapshot?.countryCode),
    retreatId: item.retreatSlug ?? "",
    retreatName: item.retreatName ?? asString(item.snapshot?.retreatName),
    programmeId: item.programmeSlug ?? "",
    programmeName: item.programmeName ?? asString(item.snapshot?.programmeName),
    durationNights: item.durationNights ?? snapshot?.durationNights ?? asNumber(item.snapshot?.durationNights),
    durationUnit: "nights",
    checkIn: item.checkIn ?? asString(item.snapshot?.checkIn),
    checkOut: item.checkOut ?? asString(item.snapshot?.checkOut),
    guests: item.guests ?? snapshot?.guests ?? asNumber(item.snapshot?.guests),
    occupancy: item.occupancy ?? snapshot?.occupancy ?? asString(item.snapshot?.occupancy),
    roomType: snapshot?.roomType ?? asString(item.snapshot?.roomType),
    displayedPrice: snapshot?.label ?? null,
    priceStatus: snapshot?.priceStatus ?? null,
    priceSnapshot: snapshot,
    finalPayableAmount: item.finalAmountInr ?? snapshot?.totalAmount ?? null,
    settlementMode: settlementFromServer(item.settlementMode ?? asString(item.snapshot?.settlementMode)),
    source: sourceFromServer(item.source ?? asString(item.snapshot?.source)),
    customerNotes: item.customerNotes ?? asString(item.snapshot?.customerNotes),
    status: mapServerAvailabilityStatus(item.status),
    alternative: (item.alternative as AlternativeProposal | null) ?? null,
    partnerConfirmation: null,
    internalNotes: (item.internalNotes ?? []).map((note) => note.body),
    auditTrail: (item.history ?? []).map((h) => ({
      at: h.occurredAt,
      actor:
        h.actorRole === "partner" || h.actorRole === "admin" || h.actorRole === "customer"
          ? h.actorRole
          : "system",
      action: h.toStatus,
      detail: h.reason ?? undefined,
    })),
    requestedAt,
    partnerViewedAt: item.partnerViewedAt ?? null,
    partnerRespondedAt: item.partnerRespondedAt ?? null,
    createdAt: requestedAt,
    updatedAt: item.partnerRespondedAt ?? requestedAt,
    bookingId: item.bookingNumber ?? null,
    paymentMode: settlementFromServer(item.settlementMode),
  };
}

/** @deprecated Use toAvailabilityRequest. Kept for existing imports. */
export const mergeServerAvailability = toAvailabilityRequest;

export async function fetchMyRequests(): Promise<AvailabilityRequest[]> {
  const items = await fetchMyAvailabilityRequests();
  return items.map(toAvailabilityRequest);
}

export async function fetchRequest(publicId: string): Promise<AvailabilityRequest> {
  return toAvailabilityRequest(await getAvailabilityByPublicId(publicId));
}

export async function refreshMyRequests(): Promise<AvailabilityRequest[]> {
  return fetchMyRequests();
}

export async function refreshPartnerRequests(): Promise<AvailabilityRequest[]> {
  const items = await fetchPartnerQueue();
  return items.map(toAvailabilityRequest);
}

export async function refreshAdminRequests(): Promise<AvailabilityRequest[]> {
  const items = await fetchAdminQueue();
  return items.map(toAvailabilityRequest);
}

export async function loadAvailabilityRequest(requestId: string): Promise<AvailabilityRequest> {
  return fetchRequest(requestId);
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
    checkOut: input.checkOut,
    customerName: input.customerName,
    email: input.customerEmail,
    phone: input.customerPhone,
    quoteId: input.quoteId,
    source: input.source,
    countryCode: input.countryCode,
    customerNotes: input.customerNotes,
  });
  return toAvailabilityRequest(server);
}

export async function partnerConfirmAvailability(
  requestId: string,
  confirmation: Omit<PartnerConfirmation, "confirmedAt">,
): Promise<AvailabilityRequest> {
  return toAvailabilityRequest(await partnerConfirmOnServer(requestId, confirmation.finalAmount));
}

export async function partnerSuggestAlternative(
  requestId: string,
  alternative: Omit<AlternativeProposal, "proposedAt">,
): Promise<AvailabilityRequest> {
  return toAvailabilityRequest(
    await partnerAlternativeOnServer(requestId, {
      ...alternative,
      proposedAt: new Date().toISOString(),
    }),
  );
}

export async function partnerMarkUnavailable(requestId: string, reason?: string): Promise<AvailabilityRequest> {
  return toAvailabilityRequest(await partnerUnavailableOnServer(requestId, reason ?? "Dates not available"));
}

export async function customerAcceptAlternative(requestId: string): Promise<AvailabilityRequest> {
  return toAvailabilityRequest(await acceptAlternativeOnServer(requestId));
}

export async function customerCancelRequest(requestId: string): Promise<AvailabilityRequest> {
  return toAvailabilityRequest(await cancelAvailabilityOnServer(requestId));
}

export function isAgingRequest(request: AvailabilityRequest): boolean {
  if (!request.requestedAt) return false;
  const started = Date.parse(request.requestedAt);
  if (Number.isNaN(started)) return false;
  return Date.now() - started > 24 * 60 * 60 * 1000 && request.status === "REQUESTED";
}

export function requestAgeLabel(request: AvailabilityRequest): string {
  if (!request.requestedAt) return "";
  const started = Date.parse(request.requestedAt);
  if (Number.isNaN(started)) return "";
  const hours = Math.max(0, Math.round((Date.now() - started) / (60 * 60 * 1000)));
  if (hours < 24) return `${hours}h`;
  return `${Math.round(hours / 24)}d`;
}

export async function adminAddInternalNote(requestId: string, note: string): Promise<AvailabilityRequest> {
  const { apiFetch } = await import("./api/client");
  await apiFetch(`/api/admin/availability/${encodeURIComponent(requestId)}/note`, {
    method: "POST",
    body: JSON.stringify({ note }),
  });
  return fetchRequest(requestId);
}
