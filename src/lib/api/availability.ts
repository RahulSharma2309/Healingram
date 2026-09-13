import { apiFetch } from "./client";

export type ServerAvailability = {
  publicId: string;
  status: string;
  customerName?: string;
  email?: string;
  phone?: string;
  retreatSlug?: string;
  programmeSlug?: string;
  requestedAt?: string;
  finalAmountInr?: number | null;
};

export async function postAvailabilityRequest(input: {
  retreatSlug: string;
  programmeSlug: string;
  durationNights: number;
  occupancy: string;
  guests: number;
  checkIn: string;
  customerName: string;
  email: string;
  phone: string;
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
  const data = await apiFetch<{ items: ServerAvailability[] }>("/api/availability/mine");
  return data.items ?? [];
}

export async function partnerConfirmOnServer(publicId: string, finalAmountInr?: number): Promise<void> {
  await apiFetch(`/api/availability/requests/${encodeURIComponent(publicId)}/confirm`, {
    method: "POST",
    body: JSON.stringify({ finalAmountInr: finalAmountInr ?? null }),
  });
}

export async function partnerUnavailableOnServer(publicId: string, reason: string): Promise<void> {
  await apiFetch(`/api/availability/requests/${encodeURIComponent(publicId)}/unavailable`, {
    method: "POST",
    body: JSON.stringify({ reason }),
  });
}

export async function partnerAlternativeOnServer(publicId: string, proposal: unknown): Promise<void> {
  await apiFetch(`/api/availability/requests/${encodeURIComponent(publicId)}/alternative`, {
    method: "POST",
    body: JSON.stringify({ proposal }),
  });
}

export async function acceptAlternativeOnServer(publicId: string): Promise<void> {
  await apiFetch(`/api/availability/requests/${encodeURIComponent(publicId)}/accept-alternative`, {
    method: "POST",
  });
}

export async function fetchPartnerQueue(): Promise<ServerAvailability[]> {
  const data = await apiFetch<{ items: ServerAvailability[] }>("/api/partner/availability");
  return data.items ?? [];
}

export async function fetchAdminQueue(): Promise<ServerAvailability[]> {
  const data = await apiFetch<{ items: ServerAvailability[] }>("/api/admin/availability");
  return data.items ?? [];
}
