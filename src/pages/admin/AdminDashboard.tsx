import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { fetchNotifications, type ServerNotification } from "../../lib/api/notifications";
import { fetchAdminLeads, fetchAdminOverview, type AdminLead } from "../../lib/api/leads";
import {
  adminAddInternalNote,
  isAgingRequest,
  refreshAdminRequests,
  requestAgeLabel,
  type AvailabilityRequest,
} from "../../lib/availabilityRequests";
import { formatDisplayDate } from "../../lib/pricing";
import { formatInr } from "../../lib/money";
import { simulateVerifiedPaymentWebhook } from "../../lib/payment";
import { isDemoMode } from "../../lib/runtimeConfig";

export function AdminDashboard() {
  const [overview, setOverview] = useState<Record<string, number> | null>(null);
  const [requests, setRequests] = useState<AvailabilityRequest[]>([]);
  const [notifs, setNotifs] = useState<ServerNotification[]>([]);
  const [leads, setLeads] = useState<AdminLead[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [note, setNote] = useState("");
  const [msg, setMsg] = useState<string | null>(null);
  const [queueError, setQueueError] = useState<string | null>(null);
  const [leadError, setLeadError] = useState<string | null>(null);
  const [notifError, setNotifError] = useState<string | null>(null);

  const refresh = async () => {
    try {
      setOverview(await fetchAdminOverview());
    } catch {
      setOverview(null);
    }
    try {
      setRequests(await refreshAdminRequests());
      setQueueError(null);
    } catch {
      setQueueError("Could not load availability requests.");
    }
    try {
      setNotifs(await fetchNotifications());
      setNotifError(null);
    } catch {
      setNotifError("Could not load notifications.");
    }
    try {
      setLeads(await fetchAdminLeads());
      setLeadError(null);
    } catch {
      setLeadError("Could not load expert leads.");
    }
  };

  useEffect(() => {
    void refresh();
  }, []);

  const selected = requests.find((r) => r.requestId === selectedId) ?? null;

  return (
    <div className="space-y-8">
      <h1 className="font-display text-2xl font-bold text-gray-900">Admin dashboard</h1>

      <section className="bg-white rounded-xl border p-6">
        <h2 className="font-semibold mb-4">Operational counts</h2>
        {overview ? (
          <dl className="grid sm:grid-cols-3 gap-3 text-sm">
            {Object.entries(overview).map(([key, value]) => (
              <div key={key} className="rounded-lg border border-gray-100 p-3">
                <dt className="text-gray-500">{key}</dt>
                <dd className="text-lg font-semibold">{value}</dd>
              </div>
            ))}
          </dl>
        ) : (
          <p className="text-sm text-gray-500">Could not load /api/admin/overview.</p>
        )}
      </section>

      <section id="expert-leads" className="bg-white rounded-xl border p-6">
        <h2 className="font-semibold mb-4">Expert lead queue</h2>
        {leadError ? <p className="text-sm text-red-700 mb-3">{leadError}</p> : null}
        <div className="overflow-x-auto">
          <table className="w-full text-sm min-w-[900px]">
            <thead>
              <tr className="text-left text-gray-500 border-b">
                <th className="pb-2 pr-3">Lead ID</th>
                <th className="pb-2 pr-3">Name</th>
                <th className="pb-2 pr-3">Phone</th>
                <th className="pb-2 pr-3">Email</th>
                <th className="pb-2 pr-3">Source</th>
                <th className="pb-2 pr-3">Status</th>
                <th className="pb-2">Created</th>
              </tr>
            </thead>
            <tbody>
              {!leadError && leads.length === 0 && (
                <tr>
                  <td colSpan={7} className="py-6 text-gray-400">
                    No expert leads from the server.
                  </td>
                </tr>
              )}
              {leads.map((lead) => (
                <tr key={lead.id} className="border-b border-gray-100">
                  <td className="py-3 pr-3 font-mono text-xs">{lead.id}</td>
                  <td className="py-3 pr-3">{lead.fullName}</td>
                  <td className="py-3 pr-3">{lead.phone}</td>
                  <td className="py-3 pr-3">{lead.email}</td>
                  <td className="py-3 pr-3">{lead.source ?? "—"}</td>
                  <td className="py-3 pr-3">{lead.status}</td>
                  <td className="py-3">{new Date(lead.createdAt).toLocaleString("en-IN")}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <p className="mt-3 text-xs text-gray-500">
          Lead status changes are not available in this screen yet. The queue is read from PostgreSQL.
        </p>
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
              {queueError && (
                <tr>
                  <td colSpan={9} className="py-6 text-red-700">
                    {queueError}
                  </td>
                </tr>
              )}
              {!queueError && requests.length === 0 && (
                <tr>
                  <td colSpan={9} className="py-6 text-gray-400">
                    No availability requests from the server.
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
                    {r.finalPayableAmount != null ? formatInr(r.finalPayableAmount) : r.displayedPrice}
                  </td>
                  <td className="py-3 pr-3">{r.status}</td>
                  <td className="py-3">{requestAgeLabel(r)}</td>
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
            Snapshot:{" "}
            {selected.priceSnapshot
              ? `${selected.priceSnapshot.label} · ${selected.priceSnapshot.priceStatus}`
              : "Price snapshot was not returned by the server."}
          </p>
          {isDemoMode() ? (
            <button
              type="button"
              className="px-3 py-1.5 border border-amber-300 text-amber-900 rounded text-sm"
              onClick={async () => {
                const result = await simulateVerifiedPaymentWebhook(selected.requestId);
                setMsg(result.message);
                void refresh();
              }}
            >
              Simulate verified payment webhook
            </button>
          ) : null}
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
                    void adminAddInternalNote(selected.requestId, note.trim()).then(() => {
                      setNote("");
                      void refresh();
                    });
                  }}
                >
                  Add
                </button>
              </div>
            </label>
          </div>
          <ul className="text-xs text-gray-600 space-y-1 max-h-40 overflow-y-auto">
            {selected.auditTrail.map((a, i) => (
              <li key={`${a.at}-${i}`}>
                {new Date(a.at).toLocaleString("en-IN")} · {a.actor} · {a.action}
              </li>
            ))}
          </ul>
          {msg && <p className="text-sm text-teal-700">{msg}</p>}
        </section>
      )}

      <section className="bg-white rounded-xl border p-6">
        <h2 className="font-semibold mb-3">Notifications</h2>
        {notifError ? <p className="text-sm text-red-700 mb-3">{notifError}</p> : null}
        <ul className="text-sm space-y-2">
          {!notifError && notifs.length === 0 && <li className="text-gray-400">No notifications from the server.</li>}
          {notifs.map((n) => (
            <li key={n.id} className="border-b border-gray-100 py-2">
              <p className="font-medium">{n.title}</p>
              <p className="text-xs text-gray-500">
                {n.kind} · {n.createdAt}
              </p>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
