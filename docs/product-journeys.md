# Product journeys (implementation maps)

Authoritative **what a person does**: `docs/po-flows/`.  
Authoritative **HTTP/table detail**: `docs/technical-flows/`.  
This file adds the engineer map required for every major flow.

Last reviewed against commit: `c2cbbcbc5e27a902ac40541628db2dc094cd408d`.

For every flow: purpose, user, preconditions, entry, UI, APIs, authn/authz, tables, fields, transitions, success, validation, errors, notifications, audit, idempotency, security, local provider, future provider, files.

---

## 1. Product overview

See [po-flows/00-what-healingram-is.md](po-flows/00-what-healingram-is.md). Postgres is the business source of truth. `src/` is presentation. Providers are local adapters until Razorpay / Twilio / external inventory exist.

---

## 2. Customer journeys

**Purpose.** Browse, match, request, pay after confirm, manage profile/trips.  
**User.** Anonymous visitor or registered customer.  
**Preconditions.** Published catalogue rows (or honest empty). API + gateway up.  
**Entry.** `/`  
**UI.** Home → list/search → listing → questionnaire → request → dashboard.  
**APIs.** Catalog GETs, matching POST, availability POST/GET, auth login/register, trips, wishlist.  
**Auth.** Browse public. Request optional. Pay / wishlist / trips require registered session (`auth_kind=registered`).  
**AuthZ.** Own rows only.  
**Tables.** `catalog.*`, `content.*`, `matching.*`, `availability.requests`, `booking.bookings`, `identity.*`.  
**Success.** Published cards render from API pages of 24.  
**Errors.** Catalog 5xx → “unavailable”, not “no retreats”. Empty 200 → empty state.  
**Idempotency.** Availability create key; payment intent key.  
**Local.** Seed catalogue + demo users.  
**Future.** Real listings, Razorpay, Twilio.  
**Files.** `src/pages/customer/*`, Catalog/Matching/Availability/Identity modules.

---

## 3. Guest journey

**Purpose.** Ask availability without an account; later prove the same contact.  
**User.** Anonymous.  
**Preconditions.** Published retreat/programme.  
**Entry.** Listing → Check availability.  
**UI.** Modal → received page (`HR-…`) → My Request → OTP.  
**APIs.** `POST /api/availability/requests`; `POST /api/auth/guest/verify-start`; `POST /api/auth/guest/verify`; `GET /api/availability/mine` or `/api/guest/requests/{id}`.  
**Auth.** Create is optional-auth. Verify issues `guest_request` JWT with `request_id`.  
**AuthZ.** Token reads only that public id. Cannot patch profile, pay, wishlist, or staff APIs.  
**Tables.** `availability.requests`, `identity.users` (guest), `identity.otp_challenges`, `identity.refresh_tokens`, `inventory.holds`.  
**Fields.** `public_id`, `customer_user_id`, `idempotency_key`, OTP `destination`/`purpose`/`public_id`.  
**Transitions.** Request `REQUESTED`. OTP challenge created → consumed.  
**Success.** Guest sees that request.  
**Validation.** Registered email/phone on **create** → sign-in required (not a silent claim). Verify without matching request → `{ matched: false }`.  
**Errors.** 400 validation; 429 OTP; 5xx error UI.  
**Notifications.** Outbox on create.  
**Audit.** Login-like verify is logged without OTP value.  
**Idempotency.** Create key; OTP consume once.  
**Security.** publicId bound; privileged OTP purposes rejected on this path; email equality is not identity without OTP + match.  
**Local.** `LocalOtpProvider` code `560142` returned only when `DemoMode=true`. UI shows it only if `VITE_DEMO_MODE=true`.  
**Future.** Twilio/MSG91 behind `IOtpProvider`.  
**Files.** `AuthService`, `OtpService`, `AvailabilityService`, `GuestVerifyForm.tsx`, `MyRequest.tsx`.

---

## 4. Vendor journey

**Purpose.** Partners act on *their* requests.  
**User.** Partner with active membership (or admin).  
**Entry.** `/vendor` or vendor host.  
**UI.** Login → queue → confirm / alternative / unavailable.  
**APIs.** `POST /api/auth/login` `portal=vendor`; `GET /api/partner/availability`; confirm/alternative/unavailable.  
**Auth.** Password + portal.  
**AuthZ.** Server: partner/admin role **and** active membership before session (admins exempt). Every write: `PartnerWrite` + slug isolation. Frontend guard is UX only.  
**Tables.** `identity.users`, `partners.partners`, `partners.partner_users`, `partners.partner_retreats`, `availability.requests`, `booking.bookings`.  
**Transitions.** Confirm → booking `awaiting_payment`. Unavailable/cancel → release hold.  
**Success.** Only own slugs.  
**Errors.** Customer/admin-without-policy/partner-without-membership → 401 at login. Wrong slug → 403. Stale status → 409.  
**Security.** Never trust a client `partnerId`.  
**Local.** `partner@local.test` + seed membership.  
**Files.** `AuthService.DenyVendorMembershipAsync`, `PartnerMembershipHandler`, `VendorLogin.tsx`, `VendorPortalGuard`.

---

## 5. Admin journey

**Purpose.** See all requests, notes, audit. Never mark paid in Production.  
**User.** Admin role + permission.  
**Entry.** `/admin`.  
**APIs.** `/api/admin/availability`, notes, overview, audit, simulate (demo only).  
**AuthZ.** Role + `identity.admin_permissions` (empty set = bootstrap all). No hardcoded admin email.  
**Tables.** `availability.*`, `audit.events`, `leads.*`.  
**Local.** `admin@local.test`. Simulate requires `Payment:AllowLocalSimulate`.  
**Future.** Admin 2FA.  
**Files.** `AdminPermissionPolicy`, `AdminDashboard.tsx`, `AdminPortalGuard`.

---

## 6. Retreat discovery and matching

**Purpose.** Filter published programmes; rank questionnaire answers.  
**APIs.** `GET /api/catalog/retreats?page=&pageSize=24&need=&theme=…`; `GET /api/matching/options`; `POST /api/matching/sessions`.  
**Auth.** Public.  
**Tables.** `catalog.retreats/programmes/prices/needs/themes/destinations`, `matching.questions`, `matching.match_sessions`.  
**Success.** SQL `WHERE`/`JOIN`/`ORDER BY`/`LIMIT`. Matching returns `matches[].retreat` cards. Frontend does not download the catalogue to decorate.  
**Errors.** Options/session failure is an error, not an empty grid pretending success.  
**Files.** `NpgsqlCatalogStore`, `CatalogSearchSql`, `MatchingService`, `usePublishedRetreats.ts`, `Questionnaire.tsx`.

---

## 7. Availability request

**Purpose.** Server-owned “can we come?”  
**API.** `POST /api/availability/requests` (idempotency key required).  
**Auth.** Optional. Guest row or registered owner.  
**Tables.** `availability.requests`, `status_history`, `inventory.holds`.  
**Transitions.** `REQUESTED` → partner actions.  
**Security.** Unpublished slug rejected. Registered contact cannot be claimed as a new guest.  
**Files.** `AvailabilityService`, `AvailabilityRequestModal.tsx`.

---

## 8. Booking

**Purpose.** Stay record after confirm.  
**API.** Created by confirm, not by the browser.  
**Tables.** `booking.bookings` (unique `request_id`), `booking.events`.  
**Statuses (DB check).** `awaiting_payment` · `paid` · `completed` · `cancelled` · `refund_pending` · `refunded`. Invalid transitions rejected in C# + store.  
**Files.** `BookingCommands`, partner confirm endpoint.

---

## 9. Payment

**Purpose.** Money only after confirm; paid only from a verified webhook.  
**APIs.** `POST /api/payment/intents`; `GET /api/payment/intents/{id}`; webhooks.  
**Auth.** Registered owner (not guest_request).  
**Tables.** `payment.intents`, `payment.webhook_events`, `booking.bookings`, `inventory.holds`.  
**Idempotency.** Intent key; unique `provider_event_id`; replay re-reconciles to the same state.  
**Local.** `LocalPaymentProvider` + `/webhooks/local` when `AllowLocalSimulate=true`.  
**Future.** `RazorpayPaymentProvider` — application layer unchanged.  
**Security.** Browser never sets paid. Production refuses simulate + default secrets + local provider.  
**Files.** `PaymentService`, `PaymentReady.tsx`.

---

## 10. OTP and verification

**Purpose.** Prove destination + purpose + publicId.  
**APIs.** guest verify-start / verify.  
**Tables.** `identity.otp_challenges`.  
**Rules.** Expiry, attempt limits, atomic consume, replay reject, resend window. OTP values not logged. Production responses omit codes unless `DemoMode`.  
**Local.** `LocalOtpProvider`. Unsupported provider names fail startup (no silent fallback).  
**Future.** Twilio/MSG91 class only.  
**Files.** `OtpService`, `IOtpProvider`, `OtpProviderFactory`.

---

## 11. Cancellation and refund

See [po-flows/11-cancellation-and-refund.md](po-flows/11-cancellation-and-refund.md). Customer cancel; booking refund states are server transitions. No Razorpay refund API yet.

---

## 12. Inventory

See [po-flows/12-inventory.md](po-flows/12-inventory.md). Hold on request, confirm on paid, release on cancel/unavailable. Payment does not call a PMS.

---

## 13. Notifications

See [po-flows/13-notifications.md](po-flows/13-notifications.md). Outbox in the same UoW; inbox for the user. Local Mailpit.

---

## 14. Identity lifecycle

See [po-flows/14-identity-lifecycle.md](po-flows/14-identity-lifecycle.md) and [engineering/security-and-authorization.md](engineering/security-and-authorization.md).

Anonymous → request → OTP → `guest_request` → optional register (promote same user) → registered password session. Vendor/admin are separate portals and roles.
