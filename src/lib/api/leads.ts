import { apiFetch } from "./client";

export type ServerLead = {
  id: string;
  status?: string;
};

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
