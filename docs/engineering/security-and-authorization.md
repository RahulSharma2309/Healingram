# Security and authorization

This is the authoritative V1 security model. Technical-flow pages should describe the journey and link here instead of restating auth rules.

## 1. Authentication

Password login, register, refresh, and logout live in Identity. Access tokens are JWTs (`sub`, `role`, `auth_kind`, `account_status`, optional `purpose` / `request_id`). They do **not** carry email or name. Profile comes from `GET /api/users/me`.

Frontend: access + refresh tokens in `sessionStorage`. `apiFetch` retries once after `POST /api/auth/refresh` on 401.

## 2. Customer session

`auth_kind = registered`. Role includes `customer`. Can list `GET /api/availability/mine` and `GET /api/trips` for that user.

## 3. Guest request session

Anonymous request create either:

- creates a **guest** identity, or
- if the email/phone already belongs to a **registered** account: `400` “You already have a Healingram account. Please sign in to continue.”

OTP purpose `REQUEST_ACCESS` is request-scoped. If the challenge has `publicId`, verify must send the same id. Consume is atomic.

Guest verify issues `auth_kind = guest_request` (not a normal customer session). That token may read the scoped request (`GET /api/availability/requests/{publicId}` or `GET /api/guest/requests/{publicId}`). `GET /api/availability/mine` returns only that request.

## 4. Vendor session

Partner APIs use policy `PartnerWrite`: authenticated + partner/admin role + **active partner membership** (admins bypass membership). Retreat writes also go through `IPartnerAuthorization` (slug-level).

`GET /api/users/me` returns `partnerMemberships[]` (`partnerId`, `partnerName`, `role`, `status`). The Vendor portal guard requires partner role **and** an active membership (or admin).

`membership_role` is stored and returned. Screen-level owner/manager/finance/operations splits are not all implemented yet.

## 5. Admin session

Admin portal entry is role `admin`. Individual APIs use permissions:

| Endpoint | Permission / policy |
| --- | --- |
| `GET /api/admin/availability` | `requests.read` |
| `POST /api/admin/availability/{id}/note` | `requests.manage` |
| Lead admin GET | `AdminWrite` (role admin) until those routes are split |

If an admin has **no** rows in `identity.admin_permissions`, the role still grants all known permissions (bootstrap). Once any permission row exists, only those rows apply.

## 6. Roles

`customer` · `partner` · `admin`, stored on `identity.users.role` and `identity.user_roles`.

## 7. Permissions

`AdminPermissions` (`requests.read`, `requests.manage`, `payments.read`, `refunds.manage`, …). Enforced via `IAdminAuthorization` on the admin availability policies above.

## 8. Partner memberships

`partners.partner_users` (`user_id`, `partner_id`, `membership_role`). Status `active` when the partner org is `approved`.

## 9. Resource authorization

Partner confirm/alternative/unavailable checks retreat membership, not only role.

## 10. Payment ownership

`payment.intents.customer_user_id` is written and read. `GET` / `POST` intents require the owner (or admin). Guests cannot create intents.

Webhook: verified event → intent `paid` → `Booking.MarkPaid` (`awaiting_payment → paid` only; `paid → paid` is harmless; `completed`/`cancelled` rejected). Replay of the same provider event **re-runs** booking reconcile.

Local simulate: `POST /api/admin/payments/simulate` (admin, `AllowLocalSimulate`). The browser must not send a webhook secret. `/api/payment/webhooks/local` remains for UAT scripts and is disabled when simulate is off.

## 11. OTP lifecycle

Persist challenge → dispatch → store provider reference. Verify is request-scoped and atomically consumed.

## 12. Provider abstraction

`Otp:Provider` and `Payment:Provider` select the implementation at startup.

- `local` (payment also `fake`) → local adapter
- `razorpay` / `stripe` / `twilio` / `msg91` → **fail startup** (declared, not in this build)
- unknown → **fail startup** (no silent Local fallback)

Business code talks to `IOtpProvider` / `IPaymentProvider` only.

## 13. Portal separation

Configured hosts: `App:CustomerUrl` / `VendorUrl` / `AdminUrl` and `VITE_*_APP_URL` / `VITE_PORTAL`.

Local fallback is pathname `/vendor` and `/admin` on the same origin. Login destination uses **host / `VITE_PORTAL`**, not “current path looks like vendor”. After vendor/admin login, the session is persisted only if the account is eligible for that portal.

## 14. Production rules

- CORS = configured app origins **only** (no localhost).
- Production frontend must set `VITE_API_BASE_URL` at build time (no localhost fallback).
- Production refuses default JWT/webhook secrets, demo seeds, `DemoMode`, and unimplemented providers.
- Rate limiting today is a single API `sensitive` fixed window (20/min). Not partitioned; not on the Gateway. Good enough for laptop UAT only.
- Gateway = routing / CORS / correlation. API = authentication / authorization.

## Still later (not this story)

Admin/vendor dashboards still mix some localStorage for prototype UI. Real Razorpay/Twilio adapters. Schema migration history table. Gateway rate limits. HttpOnly cookie BFF. Full audit of marketplace events. Portal-specific JWT (`portal=vendor`).
