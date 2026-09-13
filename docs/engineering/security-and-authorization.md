# Security and authorization

This is the authoritative V1 security model. Technical-flow pages should describe the journey and link here instead of restating auth rules.

## 1. Authentication

Password login, register, refresh, and logout live in Identity. Access tokens are JWTs (`sub`, `role`, `auth_kind`, `account_status`, optional `purpose` / `request_id`). They do **not** carry email or name. Profile comes from `GET /api/users/me`.

`POST /api/auth/login` may send `portal` (`customer` / `vendor` / `admin`). Vendor login requires a partner or admin role. Admin login requires an admin role. The server rejects the wrong portal even if the password is correct.

Frontend: access + refresh tokens in `sessionStorage`. `apiFetch` retries once after `POST /api/auth/refresh` on 401. Concurrent 401s share a single in-flight refresh.

Refresh tokens store `auth_kind`, `purpose`, and `request_id`. A guest-request refresh re-issues the same scoped token. It does **not** become a registered customer session.

## 2. Customer session

`auth_kind = registered`. Role includes `customer`. Can list `GET /api/availability/mine` and `GET /api/trips` for that user.

## 3. Guest request session

Anonymous request create either:

- creates a **guest** identity, or
- if the email/phone already belongs to a **registered** account: `400` “You already have a Healingram account. Please sign in to continue.”

A `guest_request` token **cannot** create another availability request.

OTP purpose `REQUEST_ACCESS` is request-scoped. Start and verify both require `publicId`. If the challenge has `publicId`, verify must send the same id. Consume is atomic. Destination-aware OTP limits: resend window plus `Otp:MaxStartsPerHour` per destination.

Guest verify issues `auth_kind = guest_request` (not a normal customer session). That token may read **only** the scoped request (`GET /api/availability/requests/{publicId}` or `GET /api/guest/requests/{publicId}`). `GET /api/availability/mine` returns only that request. An unscoped guest token cannot read owned requests by user id. Guest tokens cannot change `/api/users/me`, create payments, or call partner/admin APIs.

## 4. Vendor session

Partner APIs use policy `PartnerWrite`: authenticated + partner/admin role + **active partner membership** (admins bypass membership). Retreat writes also go through `IPartnerAccess` (slug-level). Partner queues are filtered in SQL by those slugs. The browser cannot supply a `partnerId` to widen the query.

`GET /api/users/me` returns `partnerMemberships[]` (`partnerId`, `partnerName`, `role`, `status`). The Vendor portal guard requires partner role **and** an active membership (or admin).

`membership_role` (`owner` / `manager` / `finance` / `operations` / `member`) is stored and returned. **Implemented now:** any *active* membership plus retreat-slug access may confirm / alternative / unavailable. **Not implemented yet:** role-specific partner policies (`partner.finance`, etc.). Constants live in `PartnerMembershipRoles`.

## 5. Admin session

Admin access is **authenticated + admin role + required permission**. Permission rows alone are not enough.

| Endpoint | Permission / policy |
| --- | --- |
| `GET /api/admin/availability` | admin role + `requests.read` |
| `POST /api/admin/availability/{id}/note` | admin role + `requests.manage` |
| `POST /api/admin/payments/simulate` | admin role + `payments.simulate` |
| Lead admin GET | `AdminWrite` (role admin) until those routes are split |

If an admin has **no** rows in `identity.admin_permissions`, the role still grants all known permissions (bootstrap). Once any permission row exists, only those rows apply.

## 6. Roles

`customer` · `partner` · `admin`, stored on `identity.users.role` and `identity.user_roles`.

## 7. Permissions

`AdminPermissions` (`requests.read`, `requests.manage`, `payments.read`, `payments.simulate`, `refunds.manage`, …). Enforced via `IAdminAuthorization` plus an admin-role assertion on the policy.

## 8. Partner memberships

`partners.partner_users` (`user_id`, `partner_id`, `membership_role`). Status `active` when the partner org is `approved`.

## 9. Resource authorization

Partner confirm/alternative/unavailable checks retreat membership, not only role. Availability status updates are conditional (`WHERE status = expected`). A stale partner action returns **409**. Confirm is idempotent if the request is already `CONFIRMED` (reuses the existing booking).

Confirm coordinates availability + booking + outbox in one database transaction when `IUnitOfWork` is present (API process). Booking creation is idempotent per `request_id`.

## 10. Payment ownership

`payment.intents.customer_user_id` is written and read. `GET` / `POST` intents require the owner (or admin). Guests and `guest_request` tokens cannot create intents.

Create sequence: insert intent `creating` → call `IPaymentProvider.CreatePayment` with a stable idempotency key → update `ready`. A provider failure marks `failed`. The same idempotency key retries that intent. At most one `creating`/`ready` intent exists per booking (partial unique index). Failed intents leave room for a later attempt with a new key.

Webhook: verify (provider-specific) → validate → insert event `received` → process payment + booking → `processed`. Replay of the same provider event **re-runs** booking reconcile. A processing failure marks the event `failed` so the provider retry can finish. Outbox enqueue is not swallowed.

Local simulate: `POST /api/admin/payments/simulate` (admin role + `payments.simulate` + `Payment:AllowLocalSimulate`). Production refuses `AllowLocalSimulate=true` even if local providers are explicitly allowed. Use Development/UAT for simulation. The browser must not send a webhook secret. `/api/payment/webhooks/local` remains for UAT scripts and is disabled when simulate is off.

**Implemented now:** `LocalPaymentProvider`. **Future:** `RazorpayPaymentProvider` / `StripePaymentProvider` behind the same `IPaymentProvider` (startup already refuses those names).

## 11. OTP lifecycle

Persist challenge → dispatch → store provider reference. Verify is request-scoped and atomically consumed.

**Implemented now:** `LocalOtpProvider`. **Future:** `TwilioOtpProvider` / `Msg91OtpProvider` (startup refuses those names).

## 12. Provider abstraction

`Otp:Provider` and `Payment:Provider` select the implementation at startup.

- `local` (payment also `fake`) → local adapter
- `razorpay` / `stripe` / `twilio` / `msg91` → **fail startup** (declared, not in this build)
- unknown → **fail startup** (no silent Local fallback)

Business code talks to `IOtpProvider` / `IPaymentProvider` only.

## 13. Portal separation

Configured hosts: `App:CustomerUrl` / `VendorUrl` / `AdminUrl` and `VITE_*_APP_URL` / `VITE_PORTAL`.

Local fallback is pathname `/vendor` and `/admin` on the same origin. Login destination uses **host / `VITE_PORTAL`**, not “current path looks like vendor”. After vendor/admin login, the session is persisted only if the account is eligible for that portal. Server login `portal` is the authoritative check; frontend guards are UX only.

## 14. Production rules

- CORS = configured app origins **only** (no localhost).
- Production frontend must set `VITE_API_BASE_URL` at build time (no localhost fallback).
- Production refuses default JWT/webhook secrets, demo seeds, `DemoMode`, unimplemented providers, and local payment simulation.
- Rate limiting is configuration-driven and partitioned by client IP (`auth-login`, `auth-register`, `otp-send`, `otp-verify`, `payment-create`, `payment-simulate`, plus `sensitive`). Gateway still has no limiter.
- Schema scripts apply once. `public.schema_migrations` records `id`, `version`, `applied_at`.
- Gateway = routing / CORS / correlation. API = authentication / authorization.

## 15. Frontend state

The API and PostgreSQL are authoritative for payment, booking, availability, and authorization. `sessionStorage` holds tokens and payment idempotency/intent ids. `localStorage` may cache display/UX only. PaymentReady waits for the server request; it does not treat a local cache as payment-ready.

## Still later

Real Razorpay/Twilio adapters. HttpOnly cookie BFF. Owner/manager/finance/operations partner policies. Gateway rate limits. Full refund execution. Portal-specific JWT (`portal=vendor`) beyond login `portal`.
