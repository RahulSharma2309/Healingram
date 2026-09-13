# Leadership review — Healingram is not deployable yet

**Date:** 13 September 2026  
**Audience:** Founder / board / whoever is about to point a public domain at this repo  
**Combined verdict: NO-GO for any public hostname.**  
**Allowed use today:** founder laptop and private walkthroughs only.

Three agents read the full doc pack (`docs/po-flows/`, `docs/technical-flows/`, `docs/engineering/`, `docs/test-cases/`) and inspected the code. They then compared notes. This file is the shared record.

Easy version of the blockers (intended for the laptop or not): [blockers-in-plain-english.md](blockers-in-plain-english.md).

| Agent | Question they answered | Verdict |
| --- | --- | --- |
| **CEO** | Can we take a stranger’s money, data, or partner brand tomorrow? | **NO-GO.** This is a laptop marketplace prototype. Calling it “enterprise” is a category error. |
| **CPO** | Does the product a human sees match the promised journeys and trust rules? | **NO-SHIP.** The server is starting to behave like a marketplace. The product shell still behaves like a pitch deck. |
| **CTO** | Is the runtime safe to put on the internet? | **NO-SHIP.** Several security.md controls are real on the API. Several are not. The founder’s example (customer opens admin/vendor) is confirmed, and it sits next to worse holes. |

The founder’s example is real: **any visitor, including a logged-in customer, can open `/admin` and `/vendor`.** The site advertises those doors. That is P0. It is not the only P0.

---

## How the three agents talked to each other

They did not write three isolated memos. Each finding below was checked by at least one other seat.

| From | To | What they insisted the other own |
| --- | --- | --- |
| **CEO → CTO** | Prove whether a stranger can mark a booking **paid** with the published webhook secret. | **Yes.** `POST /api/payment/webhooks/fake` is unauthenticated. The secret default is in git and in the browser bundle (`src/lib/api/payment.ts`). |
| **CEO → CTO** | Will demo users and “one partner owns every retreat” seed in production? | **Yes, unless someone turns the flags off.** `Identity:SeedOnStartup` and partner seed default to the same “apply schema on startup” switch, which is **true**. |
| **CEO → CPO** | Which of the 14 retreat names have signed listing + pricing authority? | **None in this repo.** Seed catalog + Unsplash + demo testimonials marked `verified`. |
| **CPO → CTO** | Is the admin/vendor leak “only UI”? | **No.** UI is unguarded. Partner confirm APIs are JWT-gated (customer gets 403). Admin “Simulate webhook” still hits a **real** server endpoint with a **public** secret. Same-browser `localStorage` also shows request/lead PII on those screens. |
| **CPO → CEO** | Can we do a quiet partner-only beta? | **Not with `Local123!` printed on `/login`, OTP `560142` printed on verify, and DemoBanner saying bookings are fake.** That is not a beta. That is a leak. |
| **CTO → CPO** | Guest OTP is worse than “printed for UAT.” | **Agreed.** `VerifyGuestAsync` accepts `560142`, looks up **any** email/phone (not guest-only), and issues that user’s real JWT **role**. `admin@local.test` + `560142` is privilege escalation. |
| **CTO → CEO** | Docs already say “replace before customers.” | **CEO: the runtime does not enforce that.** Empty JWT key / webhook secret fall back to the committed defaults. Production boot does not fail closed. |
| **All three** | Is this an enterprise app? | **No.** Cost doc is ~$40–80/month on one VM. No SSO, no SLA proof, no restore drill, no GST invoice, no payouts, no on-call. Architecture (modular monolith + gateway) is a reasonable **start**. The **business** is not ready. |

---

## What is actually solid (so the no-ship is not hand-waving)

These are real. Keep them. They are necessary and not sufficient.

- Register **forces** `customer`. You cannot become admin by posting a role on signup.
- Partner/admin **write APIs** use server policies. A customer JWT should 403 on confirm / admin queue. There are unit tests for that.
- Customer A cannot `GET` customer B’s availability request by user id (`/api/availability/mine` is scoped).
- Public catalog GETs hide unpublished rows.
- Passwords are hashed. Refresh tokens are stored hashed.
- `PaymentSuccess` polls status and does not write `paid` from the query string.
- Availability create rejects unpublished slugs.
- Payment webhook events are unique (replay is idempotent).
- Product rules that the UI mostly keeps: Find My Match is not in the header; Explore Retreats is not a dropdown; check-availability-first on listings; leftover `/checkout/:id` refuses to take money; guest can request without an account; force-signup before pay.
- Create-retreat CMS is correctly **absent** as an editor (V1 rule). Leftover `#retreats` nav still **lies** about it.

That is a **local UAT monolith**. It is not a company you can put on Healingram.com.

---

## P0 — blockers (do not take public traffic)

### P0-1. Any customer or anonymous visitor can open admin and vendor

**Who found it:** Founder first. CPO confirmed as the product-shell failure. CTO confirmed there is no `ProtectedRoute` / `RequireAuth` / role guard anywhere in `src/`.

**Evidence**

- `src/App.tsx` — `/vendor` and `/admin` are bare layouts. No login. No role. No redirect.
- `src/layouts/VendorLayout.tsx` / `AdminLayout.tsx` — no session check, no logout.
- `src/components/DemoBanner.tsx` on the customer site: “Proposal MVP — UX prototype only. No real bookings or payments” plus links **Vendor panel** and **Admin panel**.
- `src/components/CustomerHeader.tsx` trust strip: “For retreat partners →” → `/vendor` for everyone.
- `src/components/CustomerFooter.tsx`: “Partner with Healingram” → `/vendor`.
- `src/lib/auth.ts` — `getUserRole()` is used for **post-login landing only** (`homePathForRole`). It is never used to lock a shell. Role is also stored in `localStorage` (`healingram_user_role`), which is not authorization.

**What happens**

1. Guest or customer clicks the banner or types `/admin` / `/vendor`.
2. They see ops chrome (“Admin control panel”, “Vendor panel”, hardcoded “Ganga Wellness Ashram”).
3. Partner/admin **write APIs** should 403 without the right JWT — so they cannot confirm another retreat’s stay from a customer token.
4. They **can** still see same-browser request/lead data from `localStorage`, learn the ops model, and (see P0-2) fire the fake payment webhook from the admin button.

**Comparable apps:** Booking.com, Airbnb, MakeMyTrip, BookRetreats never put “Partner extranet” and “Platform admin” on the guest homepage. Guest / host / ops are separate products. URL-hacking a host dashboard as a guest is a launch-stopping incident.

**Fix direction:** Route guards that redirect unless the **server** JWT role matches. Remove DemoBanner and public `/vendor` / `/admin` links from the consumer site. Partner login should be a separate entry (or at least not advertised next to “no real payments”). Do not treat `localStorage` role as auth.

---

### P0-2. Anyone can mark a booking paid (fake webhook + secret in the browser)

**Who found it:** CTO. CEO treated it as the money-event that kills a marketplace in week one. CPO: admin UI also lets a human pick `PAID` in the browser, which breaks the PO rule “admin cannot mark paid.”

**Evidence**

- `backend/src/Modules/Healingram.Modules.Payment/PaymentEndpoints.cs` — `POST /api/payment/webhooks/fake` has **no** auth policy and **no** environment gate.
- Secret default committed: `backend/src/Healingram.Api/appsettings.json` → `Payment:FakeWebhookSecret = "local-dev-webhook-secret"`. Empty env falls back to the same string (`PaymentSettings`).
- Browser **calls** the webhook: `src/lib/api/payment.ts` sends `X-Webhook-Secret` from `VITE_FAKE_WEBHOOK_SECRET` **or** `"local-dev-webhook-secret"`.
- Admin button: `src/pages/admin/AdminDashboard.tsx` → simulate verified payment webhook.
- Webhook body is `{ intentId, providerEventId }`. It does **not** verify amount, currency, or booking owner — despite `docs/engineering/security.md` saying you must.
- There is **no Razorpay/Stripe implementation** in `backend/` or `src/`. FAQ still claims it (`src/pages/customer/StaticPage.tsx`).
- Product docs are honest that V1 will not do real Razorpay (`docs/po-flows/00-what-healingram-is.md`). That means **this cannot be a consumer launch** until a later payment programme exists.

**Business meaning:** Paid is the money event. “Webhook-only paid” is true in spirit and false in operations. If this URL is public with defaults, anyone who can start an intent can mark the stay paid without a rupee moving. You would owe the retreat a stay you never collected.

**Fix direction:** Delete or Production-forbid `/webhooks/fake`. Remove `postFakePaymentWebhook` from the SPA. Implement one provider webhook with signature + amount/currency/booking match. Intents only for the booking owner.

---

### P0-3. Guest “OTP” is a public constant and can mint that user’s JWT — including admin

**Who found it:** CTO (escalation). CPO (printed on the page). CEO (account takeover + staff impersonation).

**Evidence**

- Server: `backend/src/Modules/Healingram.Modules.Identity/Auth/GuestVerification.cs` — `DevCode = "560142"`.
- `AuthService.VerifyGuestAsync` accepts that code, then `FindGuestContactAsync` → `FindByEmailAsync` / `FindByPhoneAsync` with **no guest-only check**, then `IssueTokensAsync` (access + refresh, real `role` claim). It only requires `user.Status == "active"`.
- `StartGuestVerificationAsync` logs and returns `{ sent: true }`. **No SMS/email is sent.**
- Frontend ships the same code: `src/lib/api/auth.ts` `LOCAL_GUEST_CODE = "560142"`. `GuestVerifyForm.tsx` prints “Local UAT: enter 560142”.
- Docs teach the attack: `docs/technical-flows/02-login.md`, `docs/technical-flows/04-request-availability.md`.

**Attack in one sentence:** `POST /api/auth/guest/verify` `{ "email": "admin@local.test", "code": "560142" }` → admin JWT. Same for any registered email or phone that exists.

**Comparable apps:** Booking.com / MMT send a real SMS or email. They do not print the code. They do not let “verify my request” become “become the admin.”

**Fix direction:** Real OTP (or magic link), hashed, short TTL, rate-limited, **guest-only**. Never issue tokens for `registered` / `partner` / `admin` on this path. Fail closed if the OTP provider is not configured. Strip the code from UI and docs in any non-local build.

---

### P0-4. Production boot still seeds demo gods, demo catalog, and one partner who owns every retreat

**Who found it:** CTO + CEO.

**Evidence**

- `IdentitySeedHostedService` — default on (follows `Schema:ApplyOnStartup`, default **true**). Password constant `Local123!`. Seeds `guest@` / `partner@` / `admin@local.test`.
- Login page prints the trio + password (`src/pages/customer/Login.tsx`). Same in `docs/README.md`.
- Catalog seed runs unless env is `Testing` — **not** skipped in Production. Includes local-demo published rows.
- `PartnerSeedHostedService` + `PostgresPartnerStore.SeedLocalPartnerAsync` maps **all** `catalog.retreats` with `status = 'active'` to `partner@local.test`.
- Compose publishes Postgres as `healingram` / `healingram` on `5432`.
- JWT key in git: `healingram-local-dev-jwt-key-change-me-32`. Empty `Jwt:Key` falls back to that string. Anyone who cloned the repo can forge `role=admin` against a misconfigured API.

**Business meaning:** Lift-and-shift of current compose/appsettings = public admin, a public partner that owns the whole catalog, and fake retreats sold as inventory.

**Fix direction:** Seed only when `Development` **and** an explicit flag. Production: refuse to boot if JWT / webhook / DB secrets are missing or equal to known defaults. Rotate everything. Never ship the login hint.

---

### P0-5. The site tells guests it is a prototype — and shows invented “facts” the product forbids

**Who found it:** CPO (trust). CEO (legal / advertising / other people’s brands).

PO hard rules (`docs/po-flows/00-what-healingram-is.md`): no invented price, testimonial, credential, or ranking as fact; no “medically recommended”; published inventory is the ceiling of **real** partners.

| What the guest sees | Where | Why it is a blocker |
| --- | --- | --- |
| “Proposal MVP — no real bookings or payments” | `DemoBanner.tsx` | You cannot collect INR while the header says money is fake |
| “Verified wellness retreats” / “Transparent programme pricing” | `CustomerHeader.tsx` | Unverified platform claim |
| “Healingram Verified” when `priceStatus === "VERIFIED"` | launch cards | Price flag for UAT ≠ property verification |
| “Real experiences from guests…” with Priya / Arjun / Meera | `retreatTestimonials.ts` + `RetreatListingComponent6.tsx` | Rows are `isDemo: true` **and** `consentGranted`/`verified` true. `getRetreatTestimonials` does **not** filter `isDemo` |
| FAQ: “pay online, and receive **instant confirmation**” | `StaticPage.tsx` | Opposite of Check Availability |
| FAQ: “**Razorpay/Stripe integration in production**” | same | Provider is fake |
| FAQ: “free cancellation up to 7 days” | same | Invented policy |
| About: “verified retreat partners” | `StaticPage.tsx` | No partner contract store |
| Hard-coded Rishikesh / Demo User / ₹19,000 voucher | `/booking-confirmation` | OTA fiction |
| “48 / 62 / 34 retreats”, Therapy & Counselling, Goa/Rishikesh | `/therapies` ← `mockData.ts` | Invented inventory + excluded category |
| Blog: “CMS managed in admin” + dummy posts | `/blog` | Dead CMS |
| 14 real retreat **brands** (Shathayu, Ayurvedagram, Somatheeram, Soukya, …) with Unsplash rooms and paraphrased physician bios | `LaunchCatalogData` / `launchSupply.ts` / `retreatExperts.ts` | Trademark + advertising risk if shown as live Healingram inventory |
| Programme amounts flagged `VERIFIED` / `MVP_DEMO_VERIFIED` | `programmePricing.ts` | Comment says testing only — UI still treats them as verified |

**Comparable apps:** BookRetreats and Airbnb do not invent review counts. MMT does not publish a Rishikesh voucher that is not a booking. Cure.fit never ships a sticky “UX prototype only” bar to India traffic.

**Fix direction:** DemoBanner off in any non-local build. Strip demo testimonials (`isDemo` must hide). Kill FAQ lies. Catalog = only retreats you have **written authority** to represent. No “verified” price without a partner rate card. `/therapies`, `/blog`, `/booking-confirmation` mock paths off or clearly internal.

---

## P1 — would block a careful private beta with strangers

### P1-1. Partner writes are not retreat-scoped (IDOR)

**CTO.** `ListPartnerPendingAsync` uses `IPartnerAccess`. `ConfirmAsync` / `OfferAlternativeAsync` / `MarkUnavailableAsync` only check `actor.IsPartnerWrite`. `CanRead` returns true for **any** partner or admin (`AvailabilityService.cs`). `GET /api/availability/requests/{publicId}` returns email and phone. Public IDs are sequential (`HR-{year}-{sequence}`). Anonymous GET: existing id → 401, missing → 404 (existence oracle).

The second real partner can confirm, reprice, or reject the first partner’s stays, and read guest PII. Combined with P0-3, a stolen/guessed partner email is enough.

**Fix:** Reuse `IPartnerAccess` on every partner write and GET. Return 404 for both missing and unauthorized.

---

### P1-2. Payment intent APIs are not owner-scoped

**CTO + CEO.** `CreateIntentAsync` looks up booking by `publicId` only — no user id. Any authenticated **non-guest** can `POST /api/payment/intents`. `GET /api/payment/intents/{id}` has **no** `RequireAuthorization`.

With sequential request IDs, any registered account can create intents for other people’s bookings, then (P0-2) webhook them paid.

**Fix:** Bind intent to `sub`. Same user (or staff policy) on GET.

---

### P1-3. Availability create can attach to an existing account by email/phone without proving it

**CTO.** `GuestIdentityAdapter.EnsureCustomerAsync` finds by email or phone, else creates a guest. A stranger can file a request as `victim@email`; it lands on the victim’s `/mine`. With P0-3 they can then “verify” as that email.

**Fix:** If email/phone matches a registered user, require login. Guest create only creates a **new** guest, or OTP **before** attach.

---

### P1-4. No rate limits, no lockout, broken session story

**CTO.** Gateway `Program.cs` is CORS + YARP + correlation. **No** rate limiter, **no** JWT check at the edge. Docs (`security.md`, `scale-and-availability.md`) claim login/lead limits. `LoginAsync` has no lockout. Access token is 15 minutes; `apiFetch` does **not** call `/api/auth/refresh` on 401. Tokens live in `sessionStorage`; `isLoggedIn()` is `localStorage` — new tab can show “logged in” with no access token.

**Fix:** Gateway limits on `/api/auth/*` and `POST /api/leads`. Lockout. Silent refresh. One session source of truth.

---

### P1-5. Staff UI can locally set PAID; customer pages believe it

**CPO + CTO.** PO `10-admin.md`: admin must not mark paid by clicking a status. `adminUpdateStatus` writes any status including `PAID` into `localStorage`. Admin dropdown includes `PAID` / `REFUNDED` / `COMPLETED`. `PaymentReady.tsx` treats local `PAID` as paid UX.

Same-browser customer view can show paid when Postgres is not. Support and guests will disagree with the bank.

**Fix:** Remove local status machine for money states. UI reads server only.

---

### P1-6. Admin “inbox” and partner queue hydrate from the guest’s browser

**CPO + CEO.** `src/lib/expertLeads.ts` — phone, email, wellness, WhatsApp consent in `localStorage`. Leads API: public POST, admin GET by id, **no list**. `AdminDashboard` loads `listExpertLeads()` locally. Vendor/admin dashboards merge `listAvailabilityRequests()` from the same browser store.

Production concierge work happens on whoever’s laptop last submitted a form. A guest who requested, then opened `/admin` on that laptop, sees **their** PII in the “ops” table.

**Comparable:** Host dashboards never hydrate from the guest’s browser cache.

**Fix:** Admin list API + server-side notes/status. Do not persist lead/request PII in `localStorage` in prod.

---

### P1-7. Partner console is a costume; admin is a toy ops desk

**CPO.** Vendor chrome hardcodes “Ganga Wellness Ashram” / “Vendor MVP”. Nav: Retreats, Bookings, Earnings, Analytics — **no screens**. No logout. Confirm/alternative/unavailable exist; the rest is theatre.

Admin leftover hashes: Vendors, Retreats, Bookings, Commission, CMS/SEO, Reports, Users — **no screens**. “Rate-card mutation test” rewrites prices in the browser. Header still says “Proposal MVP.”

**Comparable:** Booking.com Extranet = property identity, inbox, rates, reservations, payouts. A real marketplace admin is queue + payments risk + audit, not a prototype table.

---

### P1-8. Legal, recovery, and mail are empty

**CPO + CEO.** `/terms` and `/privacy` are one marketing paragraph (`StaticPage.tsx`). `deploy.md` already says: publish a real privacy page before production leads. **No forgot-password** anywhere in `src/`. Outbox points at Mailpit (`localhost:1025`). Guests will not get request/payment mail on a real host.

India: DPDP, GST invoices, TCS/TDS, marketplace-vs-agent disclosure — not in the product. Prices still say taxes not confirmed.

---

### P1-9. HTTPS, CORS, health, and the website image are not production-shaped

**CTO.** `infra/docker/nginx.conf` listens on 80 only; no HSTS/CSP/frame options. API/gateway Dockerfiles bind HTTP. No `UseHttpsRedirection` / `UseHsts`. `src/lib/api/client.ts` production fallback is `http://localhost:5000` if `VITE_API_BASE_URL` is unset. `web.Dockerfile` does not pass that build-arg. Gateway CORS is hardcoded to `localhost:5173` only — a real domain either cannot call the API, or someone “fixes” it to `*` (forbidden in `security.md`). `GET /api/health` is a constant `{ status: "ok" }` with **no** Postgres ping. `deploy.md` describes Caddy/Cloudflare/managed Postgres/backup drill — **none of that exists in the repo**.

---

### P1-10. Talk to an Expert / WhatsApp is half-real

**CPO.** Thank-you copy is modest. WhatsApp opens `919900112233` (comment: MVP placeholder). Admin can open WhatsApp even when consent is No. Lead notes stay in the browser. POST `/api/leads` is open, no rate limit, duplicates allowed.

---

### P1-11. Guest vs account still has sharp edges

**CPO.** Hearts on cards still work while logged out; header Wishlist is hidden until password login — two products. `/requests/:id/received` reads **localStorage only**; another device sees “Request not found.” Gateway-down copy on Home/listing: “Start the gateway on port 5000” — engineer text on a consumer page. Signup `next` only checks `startsWith("/")` — `//evil.com` is an open redirect.

---

### P1-12. Geography fallback still hard-codes Karnataka + Kerala

**CPO.** If the places API is empty, Destinations menu falls back to `buildDestinationsMenuFromLaunchSupply()` — KA/KL only (`headerConfig.ts`). That is the exact override PO forbids when inventory exists elsewhere. When the API works, Home/Destinations/`/retreats` follow published places (compliant).

---

## P2 — not launch-blocking, but “enterprise” theatre or debt

| ID | Finding | Evidence |
| --- | --- | --- |
| P2-1 | OpenAPI and `/api/meta` always public | `Program.cs`; gateway proxies `/api/docs` |
| P2-2 | JWT carries `email` as a claim | `JwtTokenService` — PII in every bearer / log of Authorization |
| P2-3 | `AllowedHosts: *` on API and gateway | `appsettings.json` |
| P2-4 | Matching `POST /api/matching/sessions` is unauthenticated and persists answers | Matching module |
| P2-5 | Guest JWT can call wishlist / trips / `PATCH /api/users/me` | `RequireAuthorization` only; frontend hides dashboard for guests |
| P2-6 | CI is frontend build + `dotnet test` only | `.github/workflows/ci.yml` — no image build, no prod-config smoke |
| P2-7 | Schema installer re-runs every `*.sql` every boot | Works while scripts stay idempotent; not a versioned migrator. `stack.md` still lists `001`…`007`; `008`/`009` exist |
| P2-8 | `ui-reference/` old demo still in repo | `architecture.md` says delete when unused |
| P2-9 | Header items 5–11 still `awaiting_spec` while UI is live | `headerConfig.ts` |
| P2-10 | Dead `RetreatCard` with Guest favourite / stars / review counts | `mockData.ts` — not used on `/retreats` today; will get wired by accident |
| P2-11 | Questionnaire “Doctor-led Ayurveda” | Adjacent to medical framing; PO forbids “medically recommended” |
| P2-12 | Sort label “Recommended” | Implies ranking (`RetreatList.tsx`) |
| P2-13 | Leftover hotel checkout / payment-success routes | `App.tsx` |
| P2-14 | Partner/admin have no logout | Layouts |
| P2-15 | My Trips is a flat status dump | PO `07` wants Payment pending / Upcoming / Completed / Cancelled grouping |
| P2-16 | Admin 2FA is “later” | `security.md` |
| P2-17 | Cost/scale docs are honest droplet math, not enterprise SLA | `cost.md`, `scale-and-availability.md` — ~99.5% wish, RPO ~24h, restore drill not done |

---

## Journey scorecard (docs vs product)

| Journey | Docs | Leadership view |
| --- | --- | --- |
| Browse | `01-browse` | Mostly real if the API is up. Trust strip, DemoBanner, `/therapies`, `/booking-confirmation` break honesty. |
| Login / signup | `02-login` | API OK. Shells unguarded. Credentials printed. No password reset. Guest verify can mint staff JWTs. |
| Find My Match | `03` | Present, not in header. Catalog-backed. Medical-adjacent copy. |
| Request availability | `04` | Core path exists. Printed OTP. Received page is device-local. |
| Partner confirm | `05` | Buttons exist; APIs gated. UI open to everyone. Fake property name. Partner writes not retreat-scoped. |
| Payment | `06` | Intent + read-only success OK. No real PSP. Secret in JS. Admin can fake paid. |
| Wishlist / trips | `07` | Header gating OK. Trips not grouped. Guest hearts vs hidden header. |
| Contact | `08` | Form + thank-you OK. Placeholder WhatsApp. Admin lead UI is local. |
| Create retreat | `09` | Correctly absent as an editor. Leftover nav lies. |
| Admin | `10` | Notes + queue chrome. Can set PAID. Toy nav. Not an ops console. |

---

## What “enterprise” would require vs what this V1 is

Compared with how Booking.com, Airbnb, BookRetreats, MakeMyTrip Experiences, and Cure.fit actually launch a marketplace:

| Enterprise / marketplace expectation | Healingram today |
| --- | --- |
| Guest cannot open host or admin chrome | **Fails.** Banner + header + unguarded routes |
| Login does not publish the keys to the kingdom | **Fails.** `Local123!` and `560142` on screen |
| Real money (UPI/cards) + GST invoice | **Fails.** Fake webhook. No invoice module |
| Reviews real or absent | **Fails.** Demo stories marked verified |
| Cancellation, privacy, password reset | **Fails.** Stubs or missing |
| Partner: my property, my payouts, my calendar | **Fails.** One ashram name, one seed partner owns all slugs, no payouts |
| Ops: one queue that is the source of truth | **Fails.** localStorage + optional API + “Proposal MVP” |
| Role separation | API yes for many writes. **UI no.** Partner writes not slug-scoped. Guest verify issues real roles |
| HTTPS, secrets out of git, fail-closed boot | **Fails.** Defaults in git; process starts anyway |
| SLA, multi-AZ, SSO, SOC 2, WAF, second region | Explicitly **not V1**. Cost is a ~$40–80/month droplet |
| Named humans: who confirms Saturday, who refunds, who owns chargebacks | **None in the repo** |
| Inventory you have the right to sell | Seed of **other companies’ names** |

**CEO:** V1 is a modular monolith for a founder. That is the right architecture for a beta. Calling it enterprise is how you get a chargeback, a partner legal letter, and a guest who found `/admin`.

---

## Combined must-fix list before any public URL

Order is the three agents’ shared priority. Items 1–7 are the floor.

1. **Kill guest-code takeover** — real OTP, guest-only, no tokens for registered/staff; remove `560142` from UI/docs/prod binaries.
2. **Kill fake paid** — disable `/webhooks/fake` in Production; remove secret from the SPA; no admin “simulate webhook” or local `PAID`.
3. **Real provider webhook** — Razorpay (or equivalent) signature + amount/currency/booking match; never trust the browser.
4. **Staff route + API consistency** — block `/admin` and `/vendor` without the right JWT; stop linking them on the public header/banner; partner writes and payment intents must check identity / `IPartnerAccess`.
5. **Disable all laptop seeds in Production** — no `Local123!` users, no demo catalog sold as live, no map-all-retreats; **fail boot** on default JWT/webhook/DB secrets.
6. **Trust copy and inventory rights** — DemoBanner off; no invented testimonials, FAQ instant-book, About “verified,” Rishikesh voucher, `/therapies` mock; only retreats you may legally represent.
7. **Production packaging** — HTTPS, security headers, `VITE_API_BASE_URL` required at build, CORS allowlist = site origin, Postgres not on `0.0.0.0:5432` with `healingram/healingram`.
8. **Rate-limit and lock** login, guest verify, and lead POST; refresh-token use and one session store.
9. **Readiness and backups** — health checks Postgres; managed DB + one restore drill; real mail.
10. **PII and legal** — no lead/request PII in `localStorage` for prod; real `/privacy` `/terms` refund policy; forgot-password; stop FAQ claims; named ops for confirm / refund / dispute.

Until 1–7 are done, a public deploy is not “early enterprise.” It is **demo credentials plus a payment forge** on the open internet.

---

## Who this can be shown to (CEO gate)

| Audience | Decision |
| --- | --- |
| Founder laptop / private walkthrough, banner on, no real PAN/Aadhaar/cards | **Yes** — that is what the docs built |
| Closed design/partner demo on a **password-gated** URL, seed brands **off or watermarked**, demo users **disabled**, no PII kept | **Maybe**, as a prototype — not as Healingram.com |
| Public beta, ads, SEO, “book with us” | **No** |
| “Enterprise launch” | **No.** Do not use that word until contracts, live payments, payouts, legal pages, support hours, and a restore drill exist |

---

## Open questions the agents left for the founder

These are not optional if you still want a date.

**For whoever wears CTO**

1. If we deployed current `appsettings` tomorrow, can a stranger mark a booking paid? (Code says yes.)
2. When does `560142` turn off, and what sends the real code?
3. Why is the webhook secret in `src/lib/api/payment.ts`?
4. Who is on-call? What restore RPO have we **proven**, not written?
5. Production secret map (JWT, webhook, SMTP, CORS, DB) — it is correctly not in git, so it also **does not exist yet**.

**For whoever wears CPO**

1. Which retreat names have **signed** listing + pricing + cancellation authority? If “none,” the site is a catalog of other people’s businesses.
2. Who confirms a request on a Saturday? SLA?
3. Who refunds, and from whose account, if the guest paid us and the retreat cancels?
4. Why do we show “verified” prices and guest stories the code labels as demo?
5. Private beta: named partners only, or public SEO? Those are different products.

**For whoever wears CEO**

1. Stop calling this enterprise. Call it **V1 local marketplace**.
2. Do not point a customer domain at this build.
3. Local UAT is not production UAT. You must click request → confirm → **real** pay → trip on the production stack before GO.

---

## Bottom line

The **engineering shape** (React + gateway + .NET modular monolith + Postgres + “browser never marks paid”) is a reasonable start. Several API controls are already adult.

The **product a human sees** still invites every guest into vendor and admin, prints the admin password, prints the OTP, ships a payment forge in JavaScript, and presents other people’s retreat names with invented stories and a banner that says none of it is real.

**Do not deploy this as an enterprise app.** Keep it on the laptop until the P0 list is closed. Then decide between a **named-partner private beta** and a public site — those are different companies.
