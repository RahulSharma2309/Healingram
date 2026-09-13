# Technical — partner confirm

## Path

```text
/vendor (partner JWT)
  → GET /api/partner/availability
  → POST /api/availability/requests/{publicId}/confirm
  → Availability + Booking port
  → INSERT booking.bookings (awaiting_payment)
  → notifications.outbox
```

Policy: `PartnerWrite` = authenticated + partner/admin role + **active partner membership** (admins skip membership). Customer token → `403`.

Each confirm/alternative/unavailable call also checks **retreat-level** authorization (`IPartnerAuthorization` / `partners.partner_retreats`). Role alone is not enough.

See [security-and-authorization.md](../engineering/security-and-authorization.md).

## APIs

| Method | Path | Auth |
| --- | --- | --- |
| GET | `/api/partner/availability` | PartnerWrite |
| POST | `.../confirm` | PartnerWrite · `{ finalAmountInr }` |
| POST | `.../alternative` | PartnerWrite · `{ proposal }` |
| POST | `.../unavailable` | PartnerWrite · `{ reason }` |
| POST | `.../accept-alternative` | guest (own request) |

## Tables

| Table | Role |
| --- | --- |
| `availability.requests` | Status → `CONFIRMED` / `ALTERNATIVE_OFFERED` / `UNAVAILABLE`; `final_amount_inr` |
| `availability.status_history` | Audit |
| `availability.alternatives` | Proposed stay JSON |
| `booking.bookings` | Created on confirm as `awaiting_payment` |
| `booking.events` | Booking audit |
| `partners.partners` | Partner org |
| `partners.partner_users` | User ↔ partner |
| `partners.partner_retreats` | Partner ↔ catalog slug |
| `notifications.outbox` | Mail jobs (ids only, no PII in payload) |
