import { apiFetch } from "./client";

export type ServerLead = {
  id: string;
  status?: string;
};

export type LeadOption = {
  kind: string;
  key: string;
  label: string;
  sortOrder: number;
};

export async function fetchLeadOptions(): Promise<LeadOption[]> {
  const data = await apiFetch<{ items: LeadOption[] }>("/api/leads/options");
  return data.items ?? [];
}

export type AdminLead = {
  id: string;
  fullName: string;
  phone: string;
  email: string;
  whatsappConsent: boolean;
  source?: string | null;
  status: string;
  context?: string;
  createdAt: string;
};

export async function fetchAdminLeads(): Promise<AdminLead[]> {
  const data = await apiFetch<{ items: AdminLead[] }>("/api/admin/leads");
  return data.items ?? [];
}

export async function fetchAdminOverview(): Promise<Record<string, number>> {
  return apiFetch<Record<string, number>>("/api/admin/overview");
}

export async function postExpertLead(input: {
  fullName: string;
  phone: string;
  email: string;
  helpType: string;
  need: string;
  travelWindow: string;
  whatsappConsent: boolean;
  source: string;
}): Promise<ServerLead> {
  return apiFetch<ServerLead>("/api/leads", {
    method: "POST",
    body: JSON.stringify(input),
  });
}
