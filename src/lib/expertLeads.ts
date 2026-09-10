/**
 * Expert / concierge lead store — local MVP “backend”.
 * Captures Talk to an Expert requests for WhatsApp / phone follow-up.
 */

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

export const HELP_TYPE_OPTIONS: { id: ExpertHelpType; label: string }[] = [
  { id: "choosing_retreat", label: "Choosing the right retreat" },
  { id: "comparing_programmes", label: "Comparing programmes" },
  { id: "dates_availability", label: "Dates & availability" },
  { id: "pricing_inclusions", label: "Pricing & inclusions" },
  { id: "something_else", label: "Something else" },
];

export const WELLNESS_NEED_OPTIONS: { id: ExpertWellnessNeed; label: string }[] = [
  { id: "stress_burnout", label: "Stress & Burnout" },
  { id: "ayurveda_panchakarma", label: "Ayurveda / Panchakarma" },
  { id: "yoga_meditation", label: "Yoga & Meditation" },
  { id: "rejuvenation_reset", label: "Rejuvenation / Reset" },
  { id: "not_sure", label: "I’m not sure" },
];

export const TRAVEL_WINDOW_OPTIONS: { id: ExpertTravelWindow; label: string }[] = [
  { id: "within_2_weeks", label: "Within 2 weeks" },
  { id: "this_month", label: "This month" },
  { id: "next_1_3_months", label: "Next 1–3 months" },
  { id: "later", label: "Later" },
  { id: "not_sure", label: "Not sure yet" },
];

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

/** Healingram concierge WhatsApp (MVP placeholder — replace with live number) */
export const HEALINGRAM_WHATSAPP_E164 = "919900112233";

const LEADS_KEY = "healingram_expert_leads_v1";
const SEQ_KEY = "healingram_expert_lead_seq_v1";
const CONTEXT_KEY = "healingram_expert_context_v1";
const EVENT = "healingram-expert-leads";

function readLeads(): ExpertLead[] {
  try {
    const raw = localStorage.getItem(LEADS_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as ExpertLead[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function writeLeads(leads: ExpertLead[]): void {
  localStorage.setItem(LEADS_KEY, JSON.stringify(leads));
  window.dispatchEvent(new Event(EVENT));
}

function nextLeadId(): string {
  let n = 1;
  try {
    n = Number(localStorage.getItem(SEQ_KEY) || "0") + 1;
    localStorage.setItem(SEQ_KEY, String(n));
  } catch {
    n = Date.now() % 100000;
  }
  return `EL-${String(n).padStart(5, "0")}`;
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
  return HELP_TYPE_OPTIONS.find((o) => o.id === id)?.label ?? id;
}

export function wellnessNeedLabel(id: ExpertWellnessNeed): string {
  return WELLNESS_NEED_OPTIONS.find((o) => o.id === id)?.label ?? id;
}

export function travelWindowLabel(id: ExpertTravelWindow | ""): string {
  if (!id) return "";
  return TRAVEL_WINDOW_OPTIONS.find((o) => o.id === id)?.label ?? id;
}

export function createExpertLead(input: ExpertLeadInput): ExpertLead {
  const now = new Date().toISOString();
  const phoneNumber = digitsOnly(input.phoneNumber);
  const lead: ExpertLead = {
    leadId: nextLeadId(),
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

export function openWhatsAppForLead(lead: ExpertLead): void {
  const text = encodeURIComponent(buildWhatsAppMessage(lead));
  const url = `https://wa.me/${HEALINGRAM_WHATSAPP_E164}?text=${text}`;
  window.open(url, "_blank", "noopener,noreferrer");
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
