import { apiFetch } from "./client";

export type PartnerMe = {
  userId: string;
  memberships: { partnerId: string; partnerName: string; role: string; status: string }[];
  retreatSlugs: string[];
};

export async function fetchPartnerMe(): Promise<PartnerMe> {
  return apiFetch<PartnerMe>("/api/partner/me");
}

export async function fetchPartnerRetreats(): Promise<string[]> {
  const data = await apiFetch<{ items: { slug: string }[] }>("/api/partner/retreats");
  return (data.items ?? []).map((item) => item.slug);
}
