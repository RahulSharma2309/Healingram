# Healingram V1 vision

Healingram is a **programme-led retreat marketplace**. A guest does not shop rooms by the night. They choose a programme, a duration, who they travel with, and then **ask the retreat if that stay is available**. Money moves only after the retreat (or Healingram admin) confirms the exact stay.

## Who it is for

- **Guest** — find a fit, request availability, pay when ready, see requests and trips.
- **Retreat partner** — confirm, propose an alternative, or mark unavailable.
- **Healingram admin** — operate the queue, correct records with an audit trail, never fake a payment.
- **Healingram expert** — receive consented concierge leads (call or WhatsApp handoff).

## V1 geography and supply

**Inventory-driven (PO override of the written spec).** The site shows whatever is **published** in the catalog, in any Indian state or city. Filters group by state; selecting a state expands the cities that actually have published retreats (with counts). A state or city with zero published inventory does not appear.

The original 14 names (Karnataka / Bengaluru area and Kerala) are **seed data**, not a permanent ceiling. Unpublished or incomplete rows stay hidden.

Authority: `docs/engineer/THE-PLAN.md`.

## Commerce rule

`Check Availability` → request → partner/admin confirm or alternative → payment-ready → **server webhook** → paid → confirmed booking in My Trips.

The browser return URL must never mark a booking paid.

## What V1 will not do

Why Healingram homepage section, featured/ranked lists, listing components 9–13 as long pages, live instant inventory, automated marketplace settlement, WhatsApp Business API automation, public medical recommendations. New states/cities appear when published inventory exists — that is not a later rewrite.

Those are reserved so the next iteration can add them without rewriting the core.
