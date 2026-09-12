# V1 backlog

Status: `todo` | `in_progress` | `dev_done` | `qa_passed` | `uat_passed` | `done`

This table is the **ID index**. Story truth (acceptance, Done notes, next file) lives in `docs/engineer/epics/`. Current pointer: [docs/engineer/CURRENT.md](../engineer/CURRENT.md).

Work one story at a time. Current story to **read**: **STORY-00-02-02** (do not implement until you say go). **STORY-00-02-01** is `dev_done` — UAT locally.

Branch for a story: `story/STORY-XX-YY-ZZ-short-slug` from `feature/v1-iteration-1`.

---

## EPIC-00 — Platform foundations

Make the iteration runnable: repo, gateway, modular monolith, Postgres, Docker, logs, traces, CI. After this epic a developer can deliver product stories without inventing infrastructure.

Prerequisites: [epic-00-platform.md](../prerequisites/epic-00-platform.md)

### FEAT-00-01 Repo, docs, and CI

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-00-01-01 | Iteration branch, agents, docs, architecture | `in_progress` | `docs/` exists; PO/Dev/QA skills exist; feature branch is `feature/v1-iteration-1`; spec is the authority. |
| STORY-00-01-02 | GitHub Actions CI | `done` | PRs into `feature/v1-iteration-1` build frontend and backend and run unit tests. Red tests fail the workflow. PR #1 merged. |

### FEAT-00-02 Modular monolith host and gateway

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-00-02-01 | API host + module interface | `dev_done` | `Healingram.Api` boots, registers modules, `GET /api/health` and `GET /api/meta` return JSON. Unit tests cover health payload shape. |
| STORY-00-02-02 | Gateway forwards to API | `todo` | Gateway exposes `/api/health`, `/api/meta`, `/api/docs`. Correlation ID is created and forwarded. |

### FEAT-00-03 Postgres (Docker and localhost)

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-00-03-01 | Compose Postgres + local connection | `in_progress` | `docker compose` starts Postgres. README documents a localhost install path. API can connect both ways. |
| STORY-00-03-02 | Per-module schemas and migrate-on-boot | `in_progress` | Schemas `identity`, `catalog`, `matching`, `availability`, `booking`, `payment`, `leads`, `partners` exist after boot. |

### FEAT-00-04 Observability stand-ins

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-00-04-01 | Serilog → Seq, OTLP → Jaeger, Mailpit | `in_progress` | One request to `/api/health` appears in Seq and Jaeger. Mailpit UI is up. No PII fields in the log template. |

### FEAT-00-05 Runnable packaging

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-00-05-01 | Optimized Docker images + one-command up | `todo` | `infra/docker-compose.yml` runs web, gateway, api, postgres. Images are multi-stage. `scripts/dev-up.ps1` starts the stack. |

### FEAT-00-06 UI reference split

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-00-06-01 | Snapshot the demo into `ui-reference/` | `todo` | Demo pages copied to `ui-reference/`. Live `npm run dev` still works. No new product behaviour in the snapshot. |

---

## EPIC-01 — Central inventory and publication

One catalog dataset feeds header, homepage, results, match, and listing. Unpublished or incomplete retreats never appear publicly. **States and cities are derived from published inventory** (any Indian state), not a Karnataka/Kerala allow-list.

Flow: [inventory.md](../flows/inventory.md) · Prerequisites: [epic-01-inventory.md](../prerequisites/epic-01-inventory.md)

### FEAT-01-01 Taxonomy and geography

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-01-01-01 | Needs and places from inventory | `todo` | Needs and `GET /api/catalog/places` (state → cities + counts) from **published** catalog rows. A published retreat in any Indian state makes that state appear. Unpublished rows do not. No Karnataka/Kerala special case. Full file: `docs/engineer/epics/EPIC-01-inventory/FEAT-01-01-taxonomy-and-places/STORY-01-01-01.md`. |
| STORY-01-01-02 | Destination tree persistence (written after 01-01-01) | `todo` | Place rows persisted; still inventory-driven. Story file created only after 01-01-01 Done. |

### FEAT-01-02 Retreat records and publication

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-01-02-01 | Retreat + programme + room + price model | `todo` | A programme cannot belong to another retreat. Price has `VERIFIED` / `ESTIMATED` / `ON_REQUEST` and validity window. Unit tests for those rules. |
| STORY-01-02-02 | Publication gate | `todo` | Public list = `ACTIVE` + required identity + ≥1 valid programme. **Not** geo-locked. Unit tests cover each failing reason. |
| STORY-01-02-03 | Seed launch retreats | `todo` | Seed is idempotent. Today’s agreed names are data, not a permanent ceiling. Unverified prices stay `ON_REQUEST`. Unpublished stay hidden. |

### FEAT-01-03 Supporting listing records

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-01-03-01 | Experts, testimonials, inclusions | `todo` | Missing optional records hide the public section. Testimonials require consent + verified flags. |

---

## EPIC-02 — Identity and session

Guests, partners, and admin can sign in. Guest requests can later merge to an account.

Flow: [account.md](../flows/account.md) · Prerequisites: [epic-02-identity.md](../prerequisites/epic-02-identity.md)

### FEAT-02-01 Auth

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-02-01-01 | Register / login / logout / refresh | `todo` | Password hashed. Duplicate email rejected. Unit tests for hash-verify and duplicate. Gateway routes `/api/auth/*`. |
| STORY-02-01-02 | Roles customer / partner / admin | `todo` | Role is server-assigned. A customer token cannot call partner/admin writes. |

### FEAT-02-02 Frontend session

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-02-02-01 | Login / signup pages use the gateway | `todo` | Success loads `/users/me`. Logout returns header to logged-out. Failed session lookup shows logged-out chrome. |

---

## EPIC-03 — Discovery shell

Header, homepage hero, need cards, destinations, expert CTA, footer. Spec sections 3–5.

Flow: [discovery.md](../flows/discovery.md) · Prerequisites: [epic-03-discovery.md](../prerequisites/epic-03-discovery.md)

### FEAT-03-01 Header and footer

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-03-01-01 | Header contract | `todo` | Logo → `/`. Explore Retreats → `/retreats` with **no** dropdown. Types and Destinations from catalog. No Find My Match in header. Wishlist, Login, Talk to an Expert on the right. |
| STORY-03-01-02 | Footer contract | `todo` | Spec links only. No admin/vendor public links. No Featured leftovers. |

### FEAT-03-02 Homepage

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-03-02-01 | Hero + Choose what you need | `todo` | Find Retreats requires a selection → `/retreats?need=`. Not sure → `/find-my-match`. No hotel-style date search in the hero. |
| STORY-03-02-02 | Six need cards + two destination cards + expert CTA | `todo` | Cards use catalog. Why Healingram is an empty insertion point only. No Featured / ratings. Expert CTA records `homepage_destination_cta`. |

---

## EPIC-04 — Results, facets, cards

`/retreats` with contextual locations. No zero-count location. Spec sections 7–8.

Flow: [results.md](../flows/results.md) · Prerequisites: [epic-04-results.md](../prerequisites/epic-04-results.md)

### FEAT-04-01 Query and facets

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-04-01-01 | Public retreat search | `todo` | Filters: need, location, duration, verified price band, programme style, verified flags. OR within group, AND across groups. Location = published inventory places. Unit tests for combination; no hard-coded state fence. |
| STORY-04-01-02 | Contextual location facets | `todo` | A location is listed only if ≥1 retreat matches the non-location filters. Changing need clears an invalid location with the spec message. |

### FEAT-04-02 Results UI

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-04-02-01 | Cards and empty/error | `todo` | Card contract from spec. No stars/fake ratings. Empty state has Clear Location, Clear filters, View matching, Talk to an Expert. URL holds non-personal state. |

---

## EPIC-05 — Find My Match

Four questions, launch-only explained matches. Spec section 6.

Flow: [find-my-match.md](../flows/find-my-match.md) · Prerequisites: [epic-05-matching.md](../prerequisites/epic-05-matching.md)

### FEAT-05-01 Assessment and matching

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-05-01-01 | Four-step API | `todo` | Q1/Q2/Q4 multi, Q3 single. Continue disabled until answered. Unit tests for selection rules. Persist non-sensitive slugs only. |
| STORY-05-01-02 | Match engine | `todo` | Matches only published catalog records. Explains Why it matches you. Closest matches may relax optional prefs; never invent supply. Geography = published inventory. |
| STORY-05-01-03 | Match UI | `todo` | `/find-my-match`, 1 of 4, Back keeps answers, See My Matches, Talk to an Expert on empty. |

---

## EPIC-06 — Retreat listing (components 1–8)

Shared `selectedStay`. Programme is the product. Spec sections 10–12.

Flow: [retreat-listing.md](../flows/retreat-listing.md) · Prerequisites: [epic-06-listing.md](../prerequisites/epic-06-listing.md)

### FEAT-06-01 Shared stay and price

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-06-01-01 | selectedStay + price tuple | `todo` | Price = programme + duration + occupancy/room + guests. Tax state explicit. `ON_REQUEST` cannot invent a total. Unit tests for tuple and validity window. |
| STORY-06-01-02 | Fixed-duration dates | `todo` | Checkout = check-in + nights (calendar days). 7 nights from 10 Oct → 17 Oct. Changing duration recalculates. Unit tests are DST-safe. |

### FEAT-06-02 Listing UI

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-06-02-01 | Components 1–4 | `todo` | Hero/plan stay, suitability, programme picker, typical day. Hide unsupported. Sticky CTA shares state. |
| STORY-06-02-02 | Components 5–8 | `todo` | Experts, consented stories, rooms (never sold alone), inclusions. Empty optional sections hide. Incompatible room clears with explanation. |

---

## EPIC-07 — Availability request (customer)

One Check Availability workflow. Idempotent request + immutable snapshot. Spec sections 13–14 (customer).

Flow: [availability-request.md](../flows/availability-request.md) · Prerequisites: [epic-07-availability.md](../prerequisites/epic-07-availability.md)

### FEAT-07-01 Request API

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-07-01-01 | Create request | `todo` | Validates selectedStay. Stores immutable price snapshot. Idempotency key ⇒ one row. Human-readable request id. Unit tests for idempotency and snapshot immutability. |
| STORY-07-01-02 | Status machine (customer-visible) | `todo` | Server validates transitions. Client cannot write arbitrary status. History records actor + from/to. |

### FEAT-07-02 Customer UI

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-07-02-01 | Shared Check Availability modal | `todo` | Every CTA uses the same function and endpoint. Duplicate tap = one request. No payment yet on review. |
| STORY-07-02-02 | Received + My Request | `todo` | Confirmation with request id. My Request shows snapshot, alternative compare, next valid action. Rejected has three recovery paths. |

---

## EPIC-08 — Partner and admin operations

Queues, alternatives, audit. Spec section 15.

Flow: [partner-admin.md](../flows/partner-admin.md) · Prerequisites: [epic-08-operations.md](../prerequisites/epic-08-operations.md)

### FEAT-08-01 Partner queue

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-08-01-01 | Partner actions | `todo` | Confirm (final commercial fields required), Suggest alternative, Unavailable. Timestamps: requested/viewed/responded. Aging highlight. No fake SLA copy. |
| STORY-08-01-02 | Partner queue UI | `todo` | `/partner/availability` shows pending rows and the three actions. |

### FEAT-08-02 Admin queue

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-08-02-01 | Admin transitions + audit | `todo` | Permitted transitions only. Price/snapshot/status edits require actor + audit. Admin cannot mark `PAID`. |
| STORY-08-02-02 | Admin queue UI | `todo` | `/admin/availability` columns from spec. Internal notes stay off the customer view. |

---

## EPIC-09 — Payment ready and webhook

No client-only success. Spec section 17.

Flow: [payment.md](../flows/payment.md) · Prerequisites: [epic-09-payment.md](../prerequisites/epic-09-payment.md)

### FEAT-09-01 Payment-ready and provider

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-09-01-01 | Freeze snapshot → payment-ready | `todo` | Only after exact confirm or accepted alternative. Settlement mode from data. Unit tests for freeze. |
| STORY-09-01-02 | Provider interface + fake provider | `todo` | Create intent. If no real gateway, placeholder “payment not yet available” — not a success screen. |
| STORY-09-01-03 | Webhook | `todo` | Signature, amount, currency, reference, idempotency. Return URL cannot set `PAID`. Unit + integration tests: replay is once. |

### FEAT-09-02 Payment UI

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-09-02-01 | Payment-ready page + processing return | `todo` | Summary fields from spec. Return shows Processing. Confirmed booking only after `PAID` → `CONFIRMED`. |

---

## EPIC-10 — Expert concierge

`/expert`. Save lead before WhatsApp. Spec section 16.

Flow: [expert-lead.md](../flows/expert-lead.md) · Prerequisites: [epic-10-expert.md](../prerequisites/epic-10-expert.md)

### FEAT-10-01 Lead capture

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-10-01-01 | Lead API | `todo` | Required name, mobile (normalized), email, help type, need, travel window. WhatsApp/phone consent never prechecked. Statuses audited. |
| STORY-10-01-02 | Expert UI + WhatsApp handoff | `todo` | Request a Call saves and confirms. Chat on WhatsApp saves first, then opens. Prefill has no internal IDs. Failed handoff keeps the lead. |

---

## EPIC-11 — Account, wishlist, My Trips

Spec section 18.

Flow: [account.md](../flows/account.md) · Prerequisites: [epic-11-account.md](../prerequisites/epic-11-account.md)

### FEAT-11-01 Wishlist and trips

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-11-01-01 | Wishlist | `todo` | Guest local save. Login to persist. Merge is explicit and duplicate-safe. Optimistic UI rolls back with toast. |
| STORY-11-01-02 | My Trips | `todo` | Groups: payment pending, upcoming, completed, cancelled/refund. Never show another customer’s data. |

---

## EPIC-12 — Launch hardening

Acceptance suite, a11y, notifications via Mailpit, failure testing. Spec sections 20–21.

Flow: [hardening.md](../flows/hardening.md) · Prerequisites: [epic-12-hardening.md](../prerequisites/epic-12-hardening.md)

### FEAT-12-01 Quality gates

| ID | Story | Status | Acceptance |
| --- | --- | --- | --- |
| STORY-12-01-01 | Notification outbox + Mailpit | `todo` | Transition emails are idempotent. Visible in Mailpit. No PII in logs. |
| STORY-12-01-02 | Spec acceptance suite | `todo` | Automated coverage of spec §21 items that are API-testable; written UAT script for the rest. |
| STORY-12-01-03 | Accessibility and mobile failure pass | `todo` | 44px targets, focus trap, Back restores, sticky CTA safe-area. No content covered by keyboard. |

---

## Later (not this iteration)

Why Healingram section, featured/rankings, listing components 9–13 as long pages, instant inventory, live gateway charging in production, WhatsApp Business API. Extra states/cities are data (published inventory), not a later epic.

---

## Suggested next implementation

UAT [STORY-00-02-01](../engineer/epics/EPIC-00-platform/FEAT-00-02-host-and-gateway/STORY-00-02-01.md) locally, then read [STORY-00-02-02](../engineer/epics/EPIC-00-platform/FEAT-00-02-host-and-gateway/STORY-00-02-02.md) and say **go**.
