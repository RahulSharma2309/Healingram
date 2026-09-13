# Leadership review — foundation vs public launch

**Date:** 13 September 2026  
**Branch reviewed:** `feature/v1-final-foundation-release-readiness`  
**Audience:** Founder / board before pointing a public domain at this repo  

**Combined verdict: NO-GO for a public hostname that takes real money or stranger PII.**  
**Allowed use today:** founder laptop, private walkthroughs, and a **staging** environment that still uses local OTP/payment adapters.

This review describes **the current code**, not an older prototype. Earlier reviews that said “anyone can open `/admin` without login” or “guest OTP issues an admin JWT” are **obsolete**.

---

## Verdicts

| Seat | Question | Verdict |
| --- | --- | --- |
| **CEO** | Can we take a stranger’s money, data, or partner brand tomorrow? | **NO-GO.** Providers are still local. Seed catalogue is not signed listings. |
| **CPO** | Does the product a human sees match the promised journeys? | **YES for local UAT** of browse / match / request / partner confirm / fake pay. **NO-SHIP** as a public marketplace brand. |
| **CTO** | Is the runtime safe to put on the internet? | **Foundation is coherent.** Production boot **fail-closes** (no default JWT/webhook secrets, no demo seed, no local providers unless explicitly allowed — and real providers are not implemented, so Production cannot start yet). That is correct. It is not the same as “ready to take UPI.” |

---

## What is actually solid (keep this)

- PostgreSQL is the business source of truth. `src/` does not keep an authoritative catalogue, booking, or vendor queue in `localStorage`.
- Tokens live in `sessionStorage` (auth transport). That is a known future hardening (HttpOnly BFF), not a business database.
- Catalogue search filters and paginates in SQL. The website requests `page` + `pageSize=24`.
- Matching returns complete retreat cards. The questionnaire does not download the catalogue to decorate results.
- Empty needs/themes/retreats are empty — the API does not invent product data when tables are empty.
- Vendor login requires an **active partner membership** on the server before a vendor session is issued (admins may enter vendor for support). `PartnerWrite` re-checks membership on every request.
- Vendor A cannot read vendor B slugs (`IPartnerAccess`).
- Admin APIs are role + permission. There is no hardcoded admin email.
- Guest create cannot silently claim a registered email/phone.
- Guest OTP is bound to destination + purpose + `publicId`, consumed once, and issues `auth_kind=guest_request` (customer role only). It cannot become admin/partner.
- A registered contact who uses guest OTP still gets a **scoped** token, not a password session.
- Portal shells (`/vendor`, `/admin`) require login + role/membership. Demo banner staff links appear only when `VITE_DEMO_MODE=true`.
- Paid is webhook-only. Replay of the same `provider_event_id` converges. Production refuses `AllowLocalSimulate`.
- OTP / payment / inventory are ports. Unknown names fail startup. No silent fallback to local.
- Schema files `001`–`014` apply once under `pg_advisory_lock` + `schema_migrations`.
- Rate limits are partitioned: login, register, OTP send/verify, payment create/simulate, plus `sensitive`.
- Logs must not contain OTP, passwords, or tokens (code paths use user ids and correlation ids).

---

## What still blocks a public launch (honest P0)

### P0-1. No real OTP, payment, or inventory provider

`Otp:Provider=twilio` (or anything other than `local`) **refuses to start**. Same for Razorpay and external inventory. Production also refuses `local` unless an allow flag is set. **You cannot legally/safely take live UPI or send real SMS from this build.**

### P0-2. Demo and seed material

Development seeds `Local123!` users and a demo catalogue. `VITE_DEMO_MODE` defaults **off** if unset (production-safe). Local Vite sets it on via `.env.development`. A public build must keep it `false` and must not seed demo users.

### P0-3. Local payment simulation exists in Development

`POST /api/payment/webhooks/fake` and admin simulate are real doors when `Payment:AllowLocalSimulate=true` (Development default). The webhook secret default is in git. Production boot fails if simulate is on or the secret is the default. **Do not expose Development settings on the internet.**

### P0-4. Catalogue and copy are still seed

Retreat names, prices, testimonials, and homepage sections come from seed SQL/C#. They are not partner-signed commercial listings. Shipping them as “the marketplace” would be a trust failure.

### P0-5. No automated browser E2E

xUnit + Vitest cover the foundation. There is no Playwright suite. Human UAT on localhost is still required before calling a story done.

### P0-6. Token transport

Access/refresh tokens in `sessionStorage` are XSS-reachable. Acceptable for local UAT. For production, plan a same-site BFF / HttpOnly cookie. Do not rip this out in a hurry.

---

## Closed since the previous (stale) review

| Old finding | Current fact |
| --- | --- |
| `/admin` and `/vendor` had no guards | `AdminPortalGuard` / `VendorPortalGuard` + server portal login |
| Business data in `localStorage` | Forbidden under `src/`; test `no-local-storage` |
| Guest OTP `560142` issued the user’s real role (admin takeover) | `guest_request` + `request_id` only; privileged purposes rejected |
| Production accepted default JWT/webhook secrets | `HealingramRuntime.EnsureSafeToStart` fails closed |
| Frontend downloaded the full catalogue | Server page of 24; matching cards from API |
| Needs invented when the table was empty | Empty list |
| Vendor JWT from partner role alone | Active membership required at login |

---

## Cost / “enterprise” language

The architecture (modular monolith + gateway + ports) is a reasonable **company start**. It is not SSO, SLA, GST invoicing, partner payouts, or an on-call org. Cost for a first URL is still on the order of tens of USD per month plus usage (OTP SMS, Razorpay MDR). See [production-infrastructure-and-cost.md](production-infrastructure-and-cost.md).

---

## What to tell the board

1. The **foundation** (auth, ownership, catalogue SQL, providers, docs) is now internally consistent.  
2. **Launch** still requires real OTP, Razorpay, real listings, secrets, HTTPS, backups, and `DemoMode=false`.  
3. Until those adapters exist, `ASPNETCORE_ENVIRONMENT=Production` **will not start** — by design.
