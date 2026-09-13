import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { fetchAdminQueue } from "../../lib/api/availability";
import {
  adminAddInternalNote,
  adminConfirmFinalPrice,
  adminSetPaymentPending,
  adminUpdateStatus,
  isAgingRequest,
  listAvailabilityRequests,
  listNotifications,
  mergeServerAvailabilityList,
  requestAgeLabel,
  resendNotification,
  type AvailabilityRequest,
  type AvailabilityRequestStatus,
  type NotificationRecord,
} from "../../lib/availabilityRequests";
import {
  addExpertLeadNote,
  EXPERT_LEAD_STATUSES,
  helpTypeLabel,
  listExpertLeads,
  openWhatsAppCallLink,
  openWhatsAppForLead,
  subscribeExpertLeads,
  telLink,
  travelWindowLabel,
  updateExpertLeadStatus,
  type ExpertLead,
  type ExpertLeadStatus,
} from "../../lib/expertLeads";
import { formatDisplayDate } from "../../lib/pricing";
import { formatInr, updateProgrammeVerifiedPrice } from "../../data/programmePricing";
import { simulateVerifiedPaymentWebhook } from "../../lib/payment";

const STATUSES: AvailabilityRequestStatus[] = [
  "REQUESTED",
  "AVAILABLE",
  "ALTERNATIVE_PROPOSED",
  "PAYMENT_PENDING",
  "PAID",
  "CONFIRMED",
  "COMPLETED",
  "REJECTED",
  "CANCELLED",
  "REFUND_PENDING",
  "REFUNDED",
];

export function AdminDashboard() {
  const [requests, setRequests] = useState<AvailabilityRequest[]>([]);
  const [notifs, setNotifs] = useState<NotificationRecord[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [note, setNote] = useState("");
  const [priceEdit, setPriceEdit] = useState("");
  const [seedPrice, setSeedPrice] = useState("");
  const [msg, setMsg] = useState<string | null>(null);
  const [leads, setLeads] = useState<ExpertLead[]>([]);
  const [selectedLeadId, setSelectedLeadId] = useState<string | null>(null);
  const [leadNote, setLeadNote] = useState("");

  const refresh = async () => {
    setRequests(listAvailabilityRequests());
    setNotifs(listNotifications("admin").slice(0, 12));
    setLeads(listExpertLeads());
    try {
      mergeServerAvailabilityList(await fetchAdminQueue());
      setRequests(listAvailabilityRequests());
    } catch {
      /* admin token missing or API down — keep local store */
    }
  };

  useEffect(() => {
    refresh();
    const onChange = () => refresh();
    window.addEventListener("healingram-requests", onChange);
    const unsubLeads = subscribeExpertLeads(onChange);
    return () => {
      window.removeEventListener("healingram-requests", onChange);
      unsubLeads();
    };
  }, []);

  const selected = requests.find((r) => r.requestId === selectedId) ?? null;
  const selectedLead = leads.find((l) => l.leadId === selectedLeadId) ?? null;

  return (
    <div className="space-y-8">
      <h1 className="font-display text-2xl font-bold text-gray-900">Admin dashboard</h1>

      <section id="expert-leads" className="bg-white rounded-xl border p-6">
        <h2 className="font-semibold mb-4">Expert lead queue</h2>
        <div className="overflow-x-auto">
          <table className="w-full text-sm min-w-[1100px]">
            <thead>
              <tr className="text-left text-gray-500 border-b">
                <th className="pb-2 pr-3">Lead ID</th>
                <th className="pb-2 pr-3">Name</th>
                <th className="pb-2 pr-3">Phone</th>
                <th className="pb-2 pr-3">WhatsApp consent</th>
                <th className="pb-2 pr-3">Need</th>
                <th className="pb-2 pr-3">Retreat</th>
                <th className="pb-2 pr-3">Travel window</th>
                <th className="pb-2 pr-3">Source</th>
                <th className="pb-2 pr-3">Status</th>
                <th className="pb-2">Created</th>
              </tr>
            </thead>
            <tbody>
              {leads.length === 0 && (
                <tr>
                  <td colSpan={10} className="py-6 text-gray-400">
                    No expert leads yet.
                  </td>
                </tr>
              )}
              {leads.map((l) => (
                <tr
                  key={l.leadId}
                  className={`border-b border-gray-100 cursor-pointer hover:bg-gray-50 ${
                    selectedLeadId === l.leadId ? "bg-teal-50" : ""
                  }`}
                  onClick={() => {
                    setSelectedLeadId(l.leadId);
                    setLeadNote("");
                  }}
                >
                  <td className="py-3 pr-3 font-mono text-xs">{l.leadId}</td>
                  <td className="py-3 pr-3">{l.fullName}</td>
                  <td className="py-3 pr-3 whitespace-nowrap">
                    {l.phoneCountryCode} {l.phoneNumber}
                  </td>
                  <td className="py-3 pr-3">{l.whatsappConsent ? "Yes" : "No"}</td>
                  <td className="py-3 pr-3 max-w-[160px] truncate">
                    {l.helpTypes.map(helpTypeLabel).join(", ") || "—"}
                  </td>
                  <td className="py-3 pr-3">{l.retreatName ?? "—"}</td>
                  <td className="py-3 pr-3">
                    {travelWindowLabel(l.travelWindow) || "—"}
                  </td>
                  <td className="py-3 pr-3">{l.source}</td>
                  <td className="py-3 pr-3">{l.status}</td>
                  <td className="py-3 whitespace-nowrap">
                    {new Date(l.createdAt).toLocaleString("en-IN")}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {selectedLead && (
          <div className="mt-6 border-t pt-5 space-y-4">
            <div className="flex flex-wrap justify-between gap-3">
              <h3 className="font-semibold">Lead {selectedLead.leadId}</h3>
              <p className="text-sm text-gray-500">{selectedLead.email}</p>
            </div>
            <p className="text-sm text-gray-600">
              {selectedLead.programmeName
                ? `${selectedLead.programmeName}${
                    selectedLead.duration != null
                      ? ` · ${selectedLead.duration} nights`
                      : ""
                  }`
                : "No programme selected"}
              {selectedLead.message ? ` · Note: ${selectedLead.message}` : ""}
            </p>

            <div className="flex flex-wrap gap-2">
              {selectedLead.whatsappConsent && (
                <button
                  type="button"
                  className="px-3 py-2 bg-teal-700 text-white rounded text-sm"
                  onClick={() => openWhatsAppForLead(selectedLead)}
                >
                  Open WhatsApp
                </button>
              )}
              {!selectedLead.whatsappConsent && (
                <a
                  href={openWhatsAppCallLink(selectedLead.normalizedPhone)}
                  target="_blank"
                  rel="noreferrer"
                  className="px-3 py-2 bg-gray-800 text-white rounded text-sm"
                >
                  Open WhatsApp
                </a>
              )}
              <a
                href={telLink(selectedLead.phoneCountryCode, selectedLead.phoneNumber)}
                className="px-3 py-2 border rounded text-sm"
              >
                Call
              </a>
              <a
                href={`mailto:${selectedLead.email}`}
                className="px-3 py-2 border rounded text-sm"
              >
                Email
              </a>
              <label className="inline-flex items-center gap-2 text-sm">
                Status
                <select
                  className="border rounded px-2 py-1"
                  value={selectedLead.status}
                  onChange={(e) => {
                    updateExpertLeadStatus(
                      selectedLead.leadId,
                      e.target.value as ExpertLeadStatus,
                    );
                    refresh();
                  }}
                >
                  {EXPERT_LEAD_STATUSES.map((s) => (
                    <option key={s} value={s}>
                      {s}
                    </option>
                  ))}
                </select>
              </label>
            </div>

            <div className="flex flex-wrap gap-2 items-end">
              <label className="text-sm flex-1 min-w-[200px]">
                Add note
                <input
                  className="mt-1 w-full border rounded px-3 py-2"
                  value={leadNote}
                  onChange={(e) => setLeadNote(e.target.value)}
                  placeholder="Internal note"
                />
              </label>
              <button
                type="button"
                className="px-3 py-2 bg-gray-900 text-white rounded text-sm"
                onClick={() => {
                  if (!leadNote.trim()) return;
                  addExpertLeadNote(selectedLead.leadId, leadNote);
                  setLeadNote("");
                  refresh();
                }}
              >
                Add note
              </button>
            </div>
            <ul className="text-xs text-gray-500 space-y-1">
              {selectedLead.notes.map((n, i) => (
                <li key={`${n}-${i}`}>• {n}</li>
              ))}
            </ul>
          </div>
        )}
      </section>

      <section id="availability" className="bg-white rounded-xl border p-6">
        <h2 className="font-semibold mb-4">Availability request queue</h2>
        <div className="overflow-x-auto">
          <table className="w-full text-sm min-w-[900px]">
            <thead>
              <tr className="text-left text-gray-500 border-b">
                <th className="pb-2 pr-3">Request ID</th>
                <th className="pb-2 pr-3">Customer</th>
                <th className="pb-2 pr-3">Retreat</th>
                <th className="pb-2 pr-3">Programme</th>
                <th className="pb-2 pr-3">Check-in</th>
                <th className="pb-2 pr-3">Guests</th>
                <th className="pb-2 pr-3">Amount</th>
                <th className="pb-2 pr-3">Status</th>
                <th className="pb-2">Age</th>
              </tr>
            </thead>
            <tbody>
              {requests.length === 0 && (
                <tr>
                  <td colSpan={9} className="py-6 text-gray-400">
                    No availability requests yet.
                  </td>
                </tr>
              )}
              {requests.map((r) => (
                <tr
                  key={r.requestId}
                  className={`border-b border-gray-100 cursor-pointer hover:bg-gray-50 ${
                    isAgingRequest(r) ? "bg-amber-50" : ""
                  } ${selectedId === r.requestId ? "bg-teal-50" : ""}`}
                  onClick={() => {
                    setSelectedId(r.requestId);
                    setPriceEdit(
                      String(r.finalPayableAmount ?? r.priceSnapshot.totalAmount ?? ""),
                    );
                    setMsg(null);
                  }}
                >
                  <td className="py-3 pr-3 font-mono text-xs">{r.requestId}</td>
                  <td className="py-3 pr-3">{r.customerName}</td>
                  <td className="py-3 pr-3">{r.retreatName}</td>
                  <td className="py-3 pr-3">{r.programmeName}</td>
                  <td className="py-3 pr-3">{formatDisplayDate(r.checkIn)}</td>
                  <td className="py-3 pr-3">{r.guests}</td>
                  <td className="py-3 pr-3">
                    {r.finalPayableAmount != null
                      ? formatInr(r.finalPayableAmount)
                      : r.displayedPrice}
                  </td>
                  <td className="py-3 pr-3">{r.status}</td>
                  <td className="py-3">{requestAgeLabel(r.requestedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      {selected && (
        <section className="bg-white rounded-xl border p-6 space-y-4">
          <div className="flex flex-wrap justify-between gap-3">
            <h2 className="font-semibold">Request {selected.requestId}</h2>
            <Link to={`/requests/${selected.requestId}`} className="text-sm text-teal-600">
              Customer view
            </Link>
          </div>
          <p className="text-sm text-gray-600">
            Snapshot at request: {selected.priceSnapshot.label} · status{" "}
            {selected.priceSnapshot.priceStatus} · captured{" "}
            {new Date(selected.priceSnapshot.capturedAt).toLocaleString("en-IN")}
          </p>

          <div className="flex flex-wrap gap-2 items-end">
            <label className="text-sm">
              Update status
              <select
                className="ml-2 border rounded px-2 py-1"
                value={selected.status}
                onChange={(e) => {
                  adminUpdateStatus(
                    selected.requestId,
                    e.target.value as AvailabilityRequestStatus,
                    "Admin status change",
                  );
                  refresh();
                }}
              >
                {STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </label>
            <label className="text-sm">
              Confirm final price
              <input
                type="number"
                className="ml-2 border rounded px-2 py-1 w-28"
                value={priceEdit}
                onChange={(e) => setPriceEdit(e.target.value)}
              />
            </label>
            <button
              type="button"
              className="px-3 py-1.5 bg-gray-900 text-white rounded text-sm"
              onClick={() => {
                const n = Number(priceEdit);
                if (!n) return;
                adminConfirmFinalPrice(selected.requestId, n);
                refresh();
              }}
            >
              Save price
            </button>
            <button
              type="button"
              className="px-3 py-1.5 bg-teal-600 text-white rounded text-sm"
              onClick={() => {
                const n = Number(priceEdit);
                if (!n) return;
                adminSetPaymentPending(selected.requestId, n, "Admin created payment-ready state");
                refresh();
              }}
            >
              Send payment-ready
            </button>
          </div>

          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              className="px-3 py-1.5 border rounded text-sm"
              onClick={() => {
                resendNotification(
                  selected.requestId,
                  "partner",
                  "new_availability_request",
                  "Reminder: availability request",
                  `${selected.requestId} still needs a response.`,
                );
                setMsg("Partner notification queued (email-ready).");
                refresh();
              }}
            >
              Resend partner notification
            </button>
            <button
              type="button"
              className="px-3 py-1.5 border rounded text-sm"
              onClick={() => {
                resendNotification(
                  selected.requestId,
                  "customer",
                  "request_received",
                  "Update on your availability request",
                  `Status for ${selected.requestId}: ${selected.status}`,
                );
                setMsg("Customer notification queued (email-ready).");
                refresh();
              }}
            >
              Resend customer notification
            </button>
            <button
              type="button"
              className="px-3 py-1.5 border border-amber-300 text-amber-900 rounded text-sm"
              onClick={async () => {
                const result = await simulateVerifiedPaymentWebhook(selected.requestId);
                setMsg(result.message);
                refresh();
              }}
            >
              Simulate verified payment webhook
            </button>
          </div>

          <div>
            <label className="text-sm block">
              Internal notes
              <div className="mt-1 flex gap-2">
                <input
                  className="flex-1 border rounded px-3 py-2"
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                  placeholder="Add note…"
                />
                <button
                  type="button"
                  className="px-3 py-2 bg-gray-900 text-white rounded text-sm"
                  onClick={() => {
                    if (!note.trim()) return;
                    adminAddInternalNote(selected.requestId, note.trim());
                    setNote("");
                    refresh();
                  }}
                >
                  Add
                </button>
              </div>
            </label>
            <ul className="mt-2 text-xs text-gray-500 space-y-1">
              {selected.internalNotes.map((n, i) => (
                <li key={`${n}-${i}`}>• {n}</li>
              ))}
            </ul>
          </div>

          <div>
            <h3 className="text-sm font-semibold mb-2">Audit trail</h3>
            <ul className="text-xs text-gray-600 space-y-1 max-h-40 overflow-y-auto">
              {selected.auditTrail.map((a, i) => (
                <li key={`${a.at}-${i}`}>
                  {new Date(a.at).toLocaleString("en-IN")} · {a.actor} · {a.action}
                  {a.detail ? ` — ${a.detail}` : ""}
                </li>
              ))}
            </ul>
          </div>

          {msg && <p className="text-sm text-teal-700">{msg}</p>}
        </section>
      )}

      <section className="bg-white rounded-xl border p-6">
        <h2 className="font-semibold mb-2">Rate-card mutation test (snapshot safety)</h2>
        <p className="text-sm text-gray-500 mb-3">
          Change Ayurvedagram rejuvenation single occupancy after a request — the request snapshot
          must stay unchanged.
        </p>
        <div className="flex gap-2 items-center">
          <input
            type="number"
            className="border rounded px-3 py-2 w-40"
            placeholder="New single price"
            value={seedPrice}
            onChange={(e) => setSeedPrice(e.target.value)}
          />
          <button
            type="button"
            className="px-3 py-2 bg-gray-900 text-white rounded text-sm"
            onClick={() => {
              const n = Number(seedPrice);
              if (!n) return;
              updateProgrammeVerifiedPrice("ayurvedagram", "rejuvenation", {
                singleOccupancyPrice: n,
              });
              setMsg(`Catalogue single occupancy updated to ${formatInr(n)}. Open an existing request to verify snapshot unchanged.`);
            }}
          >
            Update catalogue price
          </button>
        </div>
      </section>

      <section className="bg-white rounded-xl border p-6">
        <h2 className="font-semibold mb-3">Admin notifications (in-app / email-ready)</h2>
        <ul className="text-sm space-y-2">
          {notifs.length === 0 && <li className="text-gray-400">No notifications yet.</li>}
          {notifs.map((n) => (
            <li key={n.id} className="border-b border-gray-100 py-2">
              <p className="font-medium">{n.title}</p>
              <p className="text-xs text-gray-500">
                {n.type} · {n.channel} · {n.requestId}
              </p>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
