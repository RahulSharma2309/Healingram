/**
 * Expert / concierge lead store — local MVP “backend”.
 * Captures Talk to an Expert requests for WhatsApp / phone follow-up.
 */

import { postExpertLead } from "./api/leads";

export type ExpertLeadStatus =
  | "NEW"
  | "CONTACTED"
  | "QUALIFIED"
  | "FOLLOW_UP"
  | "CONVERTED"
  | "CLOSED";

export type ExpertLeadSource =
  | "header"
  | "listing"
  | "find_my_match"
  | "all_retreats"
  | "homepage"
  | "availability_modal"
  | "request_page"
  | "contact"
  | "other";

export type ExpertHelpType =
  | "choosing_retreat"
  | "comparing_programmes"
  | "dates_availability"
  | "pricing_inclusions"
  | "something_else";

export type ExpertWellnessNeed =
  | "stress_burnout"
  | "ayurveda_panchakarma"
  | "yoga_meditation"
  | "rejuvenation_reset"
  | "not_sure";

export type ExpertTravelWindow =
  | "within_2_weeks"
  | "this_month"
  | "next_1_3_months"
  | "later"
  | "not_sure";

export type ExpertLeadDates = {
  checkIn?: string;
  checkOut?: string;
};

export type ExpertLead = {
  leadId: string;
  fullName: string;
  phoneCountryCode: string;
  phoneNumber: string;
  normalizedPhone: string;
  whatsappConsent: boolean;
  email: string;
  helpTypes: ExpertHelpType[];
  wellnessNeeds: ExpertWellnessNeed[];
  travelWindow: ExpertTravelWindow | "";
  message: string;
  retreatId?: string;
  retreatName?: string;
  programmeId?: string;
  programmeName?: string;
  duration?: number | null;
  dates?: ExpertLeadDates;
  source: ExpertLeadSource;
  createdAt: string;
  updatedAt: string;
  status: ExpertLeadStatus;
  notes: string[];
};

export type ExpertLeadInput = {
  fullName: string;
  phoneCountryCode: string;
  phoneNumber: string;
  whatsappConsent: boolean;
  email: string;
  helpTypes: ExpertHelpType[];
  wellnessNeeds: ExpertWellnessNeed[];
  travelWindow: ExpertTravelWindow | "";
  message?: string;
  retreatId?: string;
  retreatName?: string;
  programmeId?: string;
  programmeName?: string;
  duration?: number | null;
  dates?: ExpertLeadDates;
  source?: ExpertLeadSource;
};

/** Referral context passed via router state or session bridge */
export type ExpertReferralContext = {
  source?: ExpertLeadSource;
  retreatId?: string;
  retreatName?: string;
  programmeId?: string;
  programmeName?: string;
  duration?: number | null;
  checkIn?: string;
  checkOut?: string;
};

export const EXPERT_LEAD_STATUSES: ExpertLeadStatus[] = [
  "NEW",
  "CONTACTED",
  "QUALIFIED",
  "FOLLOW_UP",
  "CONVERTED",
  "CLOSED",
];

export const PHONE_COUNTRY_CODES = [
  { code: "+91", label: "India (+91)", digits: 10 },
  { code: "+1", label: "US/CA (+1)", digits: 10 },
  { code: "+44", label: "UK (+44)", digitsMin: 10, digitsMax: 11 },
  { code: "+971", label: "UAE (+971)", digits: 9 },
  { code: "+65", label: "Singapore (+65)", digits: 8 },
] as const;

const CONTEXT_KEY = "healingram_expert_context_v1";
const EVENT = "healingram-expert-leads";

let memory: ExpertLead[] = [];

function readLeads(): ExpertLead[] {
  return memory;
}

function writeLeads(leads: ExpertLead[]): void {
  memory = leads;
  window.dispatchEvent(new Event(EVENT));
}

export function digitsOnly(value: string): string {
  return value.replace(/\D/g, "");
}

export function normalizePhone(countryCode: string, phoneNumber: string): string {
  const cc = digitsOnly(countryCode);
  const local = digitsOnly(phoneNumber);
  return `${cc}${local}`;
}

export function validatePhoneNumber(
  countryCode: string,
  phoneNumber: string,
): string | null {
  const local = digitsOnly(phoneNumber);
  if (!local) return "Enter a mobile number.";

  const rule = PHONE_COUNTRY_CODES.find((c) => c.code === countryCode);
  if (!rule) {
    if (local.length < 8 || local.length > 15) {
      return "Enter a valid mobile number.";
    }
    return null;
  }

  if ("digits" in rule && rule.digits != null) {
    if (local.length !== rule.digits) {
      return `Enter a ${rule.digits}-digit mobile number for ${countryCode}.`;
    }
  } else {
    const min = "digitsMin" in rule ? rule.digitsMin : 8;
    const max = "digitsMax" in rule ? rule.digitsMax : 15;
    if (local.length < min || local.length > max) {
      return `Enter a ${min}–${max} digit mobile number for ${countryCode}.`;
    }
  }

  if (countryCode === "+91" && !/^[6-9]\d{9}$/.test(local)) {
    return "Enter a valid Indian mobile number.";
  }

  return null;
}

export function helpTypeLabel(id: ExpertHelpType): string {
  return id;
}

export function wellnessNeedLabel(id: ExpertWellnessNeed): string {
  return id;
}

export function travelWindowLabel(id: ExpertTravelWindow | ""): string {
  return id;
}

export async function createExpertLead(input: ExpertLeadInput): Promise<ExpertLead> {
  const now = new Date().toISOString();
  const phoneNumber = digitsOnly(input.phoneNumber);
  const lead: ExpertLead = {
    leadId: "",
    fullName: input.fullName.trim(),
    phoneCountryCode: input.phoneCountryCode,
    phoneNumber,
    normalizedPhone: normalizePhone(input.phoneCountryCode, phoneNumber),
    whatsappConsent: Boolean(input.whatsappConsent),
    email: input.email.trim(),
    helpTypes: [...input.helpTypes],
    wellnessNeeds: [...input.wellnessNeeds],
    travelWindow: input.travelWindow,
    message: (input.message ?? "").trim(),
    retreatId: input.retreatId,
    retreatName: input.retreatName,
    programmeId: input.programmeId,
    programmeName: input.programmeName,
    duration: input.duration ?? null,
    dates: input.dates,
    source: input.source ?? "contact",
    createdAt: now,
    updatedAt: now,
    status: "NEW",
    notes: [],
  };

  try {
    const server = await postExpertLead({
      fullName: lead.fullName,
      phone: lead.normalizedPhone,
      email: lead.email,
      helpType: lead.helpTypes[0] ?? "something_else",
      need: lead.wellnessNeeds[0] ?? "not_sure",
      travelWindow: lead.travelWindow || "not_sure",
      whatsappConsent: lead.whatsappConsent,
      source: lead.source,
    });
    if (!server.id) throw new Error("Lead was not stored.");
    lead.leadId = server.id;
  } catch (error) {
    throw error instanceof Error ? error : new Error("Could not store that enquiry.");
  }

  const leads = readLeads();
  leads.unshift(lead);
  writeLeads(leads);
  return lead;
}

export function listExpertLeads(): ExpertLead[] {
  return readLeads().sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
  );
}

export function getExpertLead(leadId: string): ExpertLead | null {
  return readLeads().find((l) => l.leadId === leadId) ?? null;
}

export function updateExpertLeadStatus(
  leadId: string,
  status: ExpertLeadStatus,
): ExpertLead | null {
  const leads = readLeads();
  const idx = leads.findIndex((l) => l.leadId === leadId);
  if (idx < 0) return null;
  leads[idx] = { ...leads[idx], status, updatedAt: new Date().toISOString() };
  writeLeads(leads);
  return leads[idx];
}

export function addExpertLeadNote(leadId: string, note: string): ExpertLead | null {
  const trimmed = note.trim();
  if (!trimmed) return getExpertLead(leadId);
  const leads = readLeads();
  const idx = leads.findIndex((l) => l.leadId === leadId);
  if (idx < 0) return null;
  const stamp = new Date().toLocaleString("en-IN");
  leads[idx] = {
    ...leads[idx],
    notes: [`${stamp}: ${trimmed}`, ...leads[idx].notes],
    updatedAt: new Date().toISOString(),
  };
  writeLeads(leads);
  return leads[idx];
}

export function saveExpertReferralContext(ctx: ExpertReferralContext): void {
  try {
    sessionStorage.setItem(CONTEXT_KEY, JSON.stringify(ctx));
  } catch {
    /* ignore */
  }
}

export function readExpertReferralContext(): ExpertReferralContext | null {
  try {
    const raw = sessionStorage.getItem(CONTEXT_KEY);
    if (!raw) return null;
    return JSON.parse(raw) as ExpertReferralContext;
  } catch {
    return null;
  }
}

export function clearExpertReferralContext(): void {
  try {
    sessionStorage.removeItem(CONTEXT_KEY);
  } catch {
    /* ignore */
  }
}

export function buildWhatsAppMessage(lead: ExpertLead): string {
  const lines = ["Hi Healingram, I’d like help choosing a retreat."];

  if (lead.fullName) lines.push(`Name: ${lead.fullName}`);

  if (lead.wellnessNeeds.length > 0) {
    lines.push(
      `Looking for: ${lead.wellnessNeeds.map(wellnessNeedLabel).join(", ")}`,
    );
  }

  if (lead.helpTypes.length > 0) {
    lines.push(`Help with: ${lead.helpTypes.map(helpTypeLabel).join(", ")}`);
  }

  if (lead.travelWindow) {
    lines.push(`Travel window: ${travelWindowLabel(lead.travelWindow)}`);
  }

  if (lead.retreatName) {
    lines.push(`Retreat: ${lead.retreatName}`);
  }

  if (lead.programmeName) {
    const dur =
      lead.duration != null ? ` · ${lead.duration} nights` : "";
    lines.push(`Programme: ${lead.programmeName}${dur}`);
  }

  if (lead.dates?.checkIn) {
    const end = lead.dates.checkOut ? ` → ${lead.dates.checkOut}` : "";
    lines.push(`Dates: ${lead.dates.checkIn}${end}`);
  }

  if (lead.message) {
    lines.push(`Note: ${lead.message}`);
  }

  return lines.join("\n");
}

export async function openWhatsAppForLead(lead: ExpertLead): Promise<void> {
  const { fetchPlatformSettings } = await import("./api/catalog");
  const settings = await fetchPlatformSettings();
  const number = (settings["whatsapp.number"] ?? "").replace(/\D/g, "");
  if (!number) return;
  const text = encodeURIComponent(buildWhatsAppMessage(lead));
  window.open(`https://wa.me/${number}?text=${text}`, "_blank", "noopener,noreferrer");
}

export function openWhatsAppCallLink(normalizedPhone: string): string {
  return `https://wa.me/${digitsOnly(normalizedPhone)}`;
}

export function telLink(countryCode: string, phoneNumber: string): string {
  return `tel:${normalizePhone(countryCode, phoneNumber)}`;
}

export function subscribeExpertLeads(onChange: () => void): () => void {
  window.addEventListener(EVENT, onChange);
  return () => window.removeEventListener(EVENT, onChange);
}
