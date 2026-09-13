import { apiFetch } from "./client";
import { collectPages, type PageResult } from "./pages";

export type ServerAvailability = {
  publicId: string;
  status: string;
  customerName?: string;
  email?: string;
  phone?: string;
  customerUserId?: string | null;
  retreatSlug?: string;
  programmeSlug?: string;
  retreatName?: string | null;
  programmeName?: string | null;
  checkIn?: string | null;
  checkOut?: string | null;
  durationNights?: number | null;
  occupancy?: string | null;
  guests?: number | null;
  source?: string | null;
  settlementMode?: string | null;
  countryCode?: string | null;
  customerNotes?: string | null;
  inventoryHoldId?: string | null;
  bookingNumber?: string | null;
  requestedAt?: string;
  partnerViewedAt?: string | null;
  partnerRespondedAt?: string | null;
  finalAmountInr?: number | null;
  snapshot?: Record<string, unknown>;
  alternative?: unknown;
  internalNotes?: { body: string; createdAt: string }[];
  history?: { fromStatus?: string; toStatus: string; actorRole?: string; occurredAt: string; reason?: string | null }[];
};

export async function postAvailabilityRequest(input: {
  retreatSlug: string;
  programmeSlug: string;
  durationNights: number;
  occupancy: string;
  guests: number;
  checkIn: string;
  checkOut?: string;
  customerName: string;
  email: string;
  phone: string;
  quoteId?: string;
  source?: string;
  countryCode?: string;
  customerNotes?: string;
}): Promise<ServerAvailability> {
  return apiFetch<ServerAvailability>("/api/availability/requests", {
    method: "POST",
    body: JSON.stringify({
      idempotencyKey: crypto.randomUUID(),
      ...input,
    }),
  });
}

export async function getAvailabilityByPublicId(publicId: string): Promise<ServerAvailability> {
  return apiFetch<ServerAvailability>(`/api/availability/requests/${encodeURIComponent(publicId)}`);
}

export async function fetchMyAvailabilityRequests(): Promise<ServerAvailability[]> {
  return collectPages((page, pageSize) =>
    apiFetch<PageResult<ServerAvailability>>(
      `/api/availability/mine?page=${page}&pageSize=${pageSize}`,
    ),
  );
}

export async function partnerConfirmOnServer(publicId: string, finalAmountInr?: number): Promise<ServerAvailability> {
  return apiFetch<ServerAvailability>(`/api/availability/requests/${encodeURIComponent(publicId)}/confirm`, {
    method: "POST",
    body: JSON.stringify({ finalAmountInr: finalAmountInr ?? null }),
  });
}

export async function partnerUnavailableOnServer(publicId: string, reason: string): Promise<ServerAvailability> {
  return apiFetch<ServerAvailability>(`/api/availability/requests/${encodeURIComponent(publicId)}/unavailable`, {
    method: "POST",
    body: JSON.stringify({ reason }),
  });
}

export async function partnerAlternativeOnServer(publicId: string, proposal: unknown): Promise<ServerAvailability> {
  return apiFetch<ServerAvailability>(`/api/availability/requests/${encodeURIComponent(publicId)}/alternative`, {
    method: "POST",
    body: JSON.stringify({ proposal }),
  });
}

export async function cancelAvailabilityOnServer(publicId: string): Promise<ServerAvailability> {
  return apiFetch<ServerAvailability>(
    `/api/availability/requests/${encodeURIComponent(publicId)}/cancel`,
    { method: "POST" },
  );
}

export async function acceptAlternativeOnServer(publicId: string): Promise<ServerAvailability> {
  return apiFetch<ServerAvailability>(
    `/api/availability/requests/${encodeURIComponent(publicId)}/accept-alternative`,
    { method: "POST" },
  );
}

export async function fetchPartnerQueue(): Promise<ServerAvailability[]> {
  return collectPages((page, pageSize) =>
    apiFetch<PageResult<ServerAvailability>>(
      `/api/partner/availability?page=${page}&pageSize=${pageSize}`,
    ),
  );
}

export async function fetchAdminQueue(): Promise<ServerAvailability[]> {
  return collectPages((page, pageSize) =>
    apiFetch<PageResult<ServerAvailability>>(
      `/api/admin/availability?page=${page}&pageSize=${pageSize}`,
    ),
  );
}
