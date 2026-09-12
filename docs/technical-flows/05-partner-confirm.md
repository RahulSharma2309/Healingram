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

Policy: `PartnerWrite` (role `partner` or `admin`). Customer token → `403`.

Partner queue is filtered by `partners.partner_retreats` (which slugs this user may see).

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
