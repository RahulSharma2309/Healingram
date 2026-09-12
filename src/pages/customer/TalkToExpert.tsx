import { useEffect, useState, type FormEvent } from "react";
import { Link, useLocation } from "react-router-dom";
import { Check, MessageCircle, Phone, Shield } from "lucide-react";
import { getCustomerProfile } from "../../lib/auth";
import {
  clearExpertReferralContext,
  createExpertLead,
  HELP_TYPE_OPTIONS,
  openWhatsAppForLead,
  PHONE_COUNTRY_CODES,
  readExpertReferralContext,
  TRAVEL_WINDOW_OPTIONS,
  validatePhoneNumber,
  WELLNESS_NEED_OPTIONS,
  type ExpertHelpType,
  type ExpertLead,
  type ExpertLeadSource,
  type ExpertReferralContext,
  type ExpertTravelWindow,
  type ExpertWellnessNeed,
} from "../../lib/expertLeads";

const HERO_IMAGE =
  "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=1400&q=80";

function resolveContext(
  state: ExpertReferralContext | null,
  stored: ExpertReferralContext | null,
): ExpertReferralContext {
  return {
    source: state?.source ?? stored?.source ?? "header",
    retreatId: state?.retreatId ?? stored?.retreatId,
    retreatName: state?.retreatName ?? stored?.retreatName,
    programmeId: state?.programmeId ?? stored?.programmeId,
    programmeName: state?.programmeName ?? stored?.programmeName,
    duration: state?.duration ?? stored?.duration ?? null,
    checkIn: state?.checkIn ?? stored?.checkIn,
    checkOut: state?.checkOut ?? stored?.checkOut,
  };
}

function toggleInList<T>(list: T[], id: T, exclusive?: T): T[] {
  if (list.includes(id)) return list.filter((x) => x !== id);
  if (exclusive != null && id === exclusive) return [id];
  if (exclusive != null && list.includes(exclusive)) {
    return [...list.filter((x) => x !== exclusive), id];
  }
  return [...list, id];
}

export function TalkToExpert() {
  const location = useLocation();
  const navState = (location.state as ExpertReferralContext | null) ?? null;
  const [ctx] = useState(() =>
    resolveContext(navState, readExpertReferralContext()),
  );

  useEffect(() => {
    clearExpertReferralContext();
  }, []);

  const profile = getCustomerProfile();
  const [fullName, setFullName] = useState(profile.name);
  const [email, setEmail] = useState(profile.email);
  const [countryCode, setCountryCode] = useState(profile.countryCode || "+91");
  const [phone, setPhone] = useState(profile.phone);
  const [whatsappConsent, setWhatsappConsent] = useState(false);
  const [helpTypes, setHelpTypes] = useState<ExpertHelpType[]>([]);
  const [wellnessNeeds, setWellnessNeeds] = useState<ExpertWellnessNeed[]>([]);
  const [travelWindow, setTravelWindow] = useState<ExpertTravelWindow | "">("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [submittedLead, setSubmittedLead] = useState<ExpertLead | null>(null);

  const hasListingContext = Boolean(ctx.retreatName);

  const buildLeadPayload = () => ({
    fullName,
    phoneCountryCode: countryCode,
    phoneNumber: phone,
    whatsappConsent,
    email,
    helpTypes,
    wellnessNeeds,
    travelWindow,
    message,
    retreatId: ctx.retreatId,
    retreatName: ctx.retreatName,
    programmeId: ctx.programmeId,
    programmeName: ctx.programmeName,
    duration: ctx.duration,
    dates:
      ctx.checkIn || ctx.checkOut
        ? { checkIn: ctx.checkIn, checkOut: ctx.checkOut }
        : undefined,
    source: (ctx.source ?? "header") as ExpertLeadSource,
  });

  const validateForm = (requireWhatsAppConsent: boolean): string | null => {
    if (!fullName.trim()) return "Please enter your full name.";
    const phoneErr = validatePhoneNumber(countryCode, phone);
    if (phoneErr) return phoneErr;
    if (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      return "Please enter a valid email.";
    }
    if (helpTypes.length === 0) return "Select at least one thing you need help with.";
    if (wellnessNeeds.length === 0) return "Select what you’re looking for.";
    if (!travelWindow) return "Select when you’re thinking of travelling.";
    if (requireWhatsAppConsent && !whatsappConsent) {
      return "Please agree to WhatsApp or phone contact to continue on WhatsApp.";
    }
    return null;
  };

  const onRequestCall = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    const err = validateForm(false);
    if (err) {
      setError(err);
      return;
    }
    setSubmitting(true);
    try {
      const lead = await createExpertLead(buildLeadPayload());
      setSubmittedLead(lead);
    } finally {
      setSubmitting(false);
    }
  };

  const onChatWhatsApp = async () => {
    setError(null);
    const err = validateForm(true);
    if (err) {
      setError(err);
      return;
    }
    setSubmitting(true);
    try {
      const lead = await createExpertLead({ ...buildLeadPayload(), whatsappConsent: true });
      setSubmittedLead(lead);
      openWhatsAppForLead(lead);
    } finally {
      setSubmitting(false);
    }
  };

  if (submittedLead) {
    return (
      <div className="relative min-h-[70vh]">
        <div
          className="absolute inset-0 -z-10 bg-cover bg-center opacity-[0.18]"
          style={{ backgroundImage: `url(${HERO_IMAGE})` }}
          aria-hidden
        />
        <div className="absolute inset-0 -z-10 bg-gradient-to-b from-sand-50/95 via-sand-50/90 to-sand-50" />
        <div className="max-w-xl mx-auto px-4 py-16 md:py-24 text-center">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-sage-500 mb-4">
            Healingram Concierge
          </p>
          <h1 className="font-display text-3xl md:text-4xl font-semibold text-sage-800 text-balance">
            You’re in good hands
          </h1>
          <p className="mt-4 text-sage-600 leading-relaxed">
            We’ve received your request and will help you narrow down the best available
            options.
          </p>
          <p className="mt-2 text-sm text-sage-500">Reference {submittedLead.leadId}</p>

          <div className="mt-10 flex flex-col sm:flex-row gap-3 justify-center">
            {submittedLead.whatsappConsent && (
              <button
                type="button"
                onClick={() => openWhatsAppForLead(submittedLead)}
                className="inline-flex items-center justify-center gap-2 rounded-xl bg-teal-600 px-6 py-3.5 text-sm font-semibold text-white hover:bg-teal-500"
              >
                <MessageCircle className="w-4 h-4" />
                Continue on WhatsApp
              </button>
            )}
            <Link
              to="/retreats"
              className="inline-flex items-center justify-center rounded-xl border border-sand-200 bg-white/80 px-6 py-3.5 text-sm font-semibold text-sage-800 hover:bg-white"
            >
              Explore Retreats
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="relative">
      <div
        className="absolute inset-x-0 top-0 h-[42vh] -z-10 bg-cover bg-center"
        style={{ backgroundImage: `url(${HERO_IMAGE})` }}
        aria-hidden
      />
      <div className="absolute inset-x-0 top-0 h-[42vh] -z-10 bg-gradient-to-b from-sage-800/55 via-sage-800/35 to-sand-50" />

      <div className="max-w-6xl mx-auto px-4 pt-12 md:pt-16 pb-20">
        <div className="grid lg:grid-cols-[minmax(0,0.95fr)_minmax(0,1.05fr)] gap-10 lg:gap-14 items-start">
          <aside className="text-white lg:pt-6 lg:sticky lg:top-28">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-sand-100/90 mb-3">
              Private concierge
            </p>
            <h1 className="font-display text-3xl md:text-4xl lg:text-[2.75rem] font-semibold text-balance leading-tight">
              Talk to a Healingram Expert
            </h1>
            <p className="mt-4 text-sand-50/95 text-base md:text-lg leading-relaxed max-w-md">
              Tell us what you’re looking for and we’ll help you narrow down the right
              retreat, programme and stay.
            </p>
            <p className="mt-3 text-sm text-sand-100/85">No pressure to book.</p>

            <ul className="mt-8 space-y-3 text-sm text-sand-50/90 max-w-sm">
              {[
                "Unsure which retreat fits you",
                "Need help comparing programmes",
                "Dates, duration or pricing questions",
              ].map((item) => (
                <li key={item} className="flex gap-2.5">
                  <Check className="w-4 h-4 shrink-0 mt-0.5 text-amber-500" />
                  <span>{item}</span>
                </li>
              ))}
            </ul>

            <div className="mt-8 flex items-start gap-2.5 text-xs text-sand-100/80 max-w-sm">
              <Shield className="w-4 h-4 shrink-0 mt-0.5" />
              <p>
                Your details are used only to help with this request. WhatsApp contact
                requires your explicit consent.
              </p>
            </div>
          </aside>

          <div className="rounded-2xl border border-sand-200 bg-white/95 shadow-sm backdrop-blur-sm p-5 sm:p-8">
            {hasListingContext && (
              <div className="mb-6 rounded-xl bg-sand-50 border border-sand-200 px-4 py-3">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                  You’re asking about
                </p>
                <p className="mt-1 font-display text-lg font-semibold text-sage-800">
                  {ctx.retreatName}
                </p>
                {ctx.programmeName && (
                  <p className="mt-0.5 text-sm text-sage-600">
                    {ctx.programmeName}
                    {ctx.duration != null ? ` · ${ctx.duration} nights` : ""}
                  </p>
                )}
              </div>
            )}

            <form onSubmit={onRequestCall} className="space-y-6" noValidate>
              <label className="block">
                <span className="text-sm font-medium text-sage-700">Full name</span>
                <input
                  required
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  autoComplete="name"
                  className="mt-1.5 w-full rounded-xl border border-sand-200 px-3.5 py-3 text-sm outline-none focus:ring-2 focus:ring-teal-500/25 focus:border-teal-500"
                />
              </label>

              <div>
                <span className="text-sm font-medium text-sage-700">Mobile number</span>
                <div className="mt-1.5 grid grid-cols-[7.5rem_1fr] gap-2">
                  <select
                    value={countryCode}
                    onChange={(e) => setCountryCode(e.target.value)}
                    aria-label="Country code"
                    className="rounded-xl border border-sand-200 px-2 py-3 text-sm outline-none focus:ring-2 focus:ring-teal-500/25"
                  >
                    {PHONE_COUNTRY_CODES.map((c) => (
                      <option key={c.code} value={c.code}>
                        {c.code}
                      </option>
                    ))}
                  </select>
                  <input
                    required
                    inputMode="tel"
                    autoComplete="tel-national"
                    placeholder="Mobile number"
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                    className="w-full rounded-xl border border-sand-200 px-3.5 py-3 text-sm outline-none focus:ring-2 focus:ring-teal-500/25 focus:border-teal-500"
                  />
                </div>
                <p className="mt-2 text-xs text-sage-500">
                  We may contact you on WhatsApp about your retreat request.
                </p>
                <label className="mt-3 flex items-start gap-3 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={whatsappConsent}
                    onChange={(e) => setWhatsappConsent(e.target.checked)}
                    className="mt-1 rounded border-sand-300 text-teal-600 focus:ring-teal-500"
                  />
                  <span className="text-sm text-sage-700 leading-snug">
                    I agree to be contacted by Healingram on WhatsApp or phone regarding
                    this request.
                  </span>
                </label>
              </div>

              <label className="block">
                <span className="text-sm font-medium text-sage-700">Email</span>
                <input
                  required
                  type="email"
                  autoComplete="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="mt-1.5 w-full rounded-xl border border-sand-200 px-3.5 py-3 text-sm outline-none focus:ring-2 focus:ring-teal-500/25 focus:border-teal-500"
                />
              </label>

              <fieldset>
                <legend className="text-sm font-medium text-sage-700">
                  What do you need help with?
                </legend>
                <div className="mt-2.5 grid sm:grid-cols-2 gap-2">
                  {HELP_TYPE_OPTIONS.map((opt) => {
                    const on = helpTypes.includes(opt.id);
                    return (
                      <button
                        key={opt.id}
                        type="button"
                        onClick={() => setHelpTypes((prev) => toggleInList(prev, opt.id))}
                        className={`rounded-xl border px-3.5 py-3 text-left text-sm transition ${
                          on
                            ? "border-teal-600/50 bg-teal-50 text-sage-800 ring-1 ring-teal-600/20"
                            : "border-sand-200 bg-white text-sage-700 hover:bg-sand-50"
                        }`}
                      >
                        {opt.label}
                      </button>
                    );
                  })}
                </div>
              </fieldset>

              <fieldset>
                <legend className="text-sm font-medium text-sage-700">
                  What are you looking for?
                </legend>
                <div className="mt-2.5 flex flex-wrap gap-2">
                  {WELLNESS_NEED_OPTIONS.map((opt) => {
                    const on = wellnessNeeds.includes(opt.id);
                    return (
                      <button
                        key={opt.id}
                        type="button"
                        onClick={() =>
                          setWellnessNeeds((prev) =>
                            toggleInList(prev, opt.id, "not_sure"),
                          )
                        }
                        className={`rounded-xl border px-3.5 py-2.5 text-sm transition ${
                          on
                            ? "border-teal-600/50 bg-teal-50 text-sage-800 ring-1 ring-teal-600/20"
                            : "border-sand-200 bg-white text-sage-700 hover:bg-sand-50"
                        }`}
                      >
                        {opt.label}
                      </button>
                    );
                  })}
                </div>
              </fieldset>

              <fieldset>
                <legend className="text-sm font-medium text-sage-700">
                  When are you thinking of travelling?
                </legend>
                <div className="mt-2.5 flex flex-wrap gap-2">
                  {TRAVEL_WINDOW_OPTIONS.map((opt) => {
                    const on = travelWindow === opt.id;
                    return (
                      <button
                        key={opt.id}
                        type="button"
                        onClick={() => setTravelWindow(opt.id)}
                        className={`rounded-xl border px-3.5 py-2.5 text-sm transition ${
                          on
                            ? "border-teal-600/50 bg-teal-50 text-sage-800 ring-1 ring-teal-600/20"
                            : "border-sand-200 bg-white text-sage-700 hover:bg-sand-50"
                        }`}
                      >
                        {opt.label}
                      </button>
                    );
                  })}
                </div>
              </fieldset>

              <label className="block">
                <span className="text-sm font-medium text-sage-700">
                  Anything else we should know?
                </span>
                <textarea
                  rows={3}
                  value={message}
                  onChange={(e) => setMessage(e.target.value)}
                  className="mt-1.5 w-full rounded-xl border border-sand-200 px-3.5 py-3 text-sm outline-none focus:ring-2 focus:ring-teal-500/25 resize-y"
                />
              </label>

              {error && (
                <p role="alert" className="text-sm text-red-600">
                  {error}
                </p>
              )}

              <div className="space-y-3 pt-1">
                <button
                  type="submit"
                  disabled={submitting}
                  className="w-full inline-flex items-center justify-center gap-2 rounded-xl bg-teal-600 py-3.5 text-sm font-semibold text-white hover:bg-teal-500 disabled:opacity-60"
                >
                  <Phone className="w-4 h-4" />
                  Request a Call
                </button>
                <button
                  type="button"
                  disabled={submitting}
                  onClick={onChatWhatsApp}
                  className="w-full inline-flex items-center justify-center gap-2 rounded-xl border border-sand-200 py-3.5 text-sm font-semibold text-sage-800 hover:bg-sand-50 disabled:opacity-60"
                >
                  <MessageCircle className="w-4 h-4" />
                  Chat on WhatsApp
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}
