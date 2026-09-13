# Technical — payment

## Path

```text
/requests/{publicId}/payment
  → POST /api/payment/intents
  → Payment looks up booking by publicId
  → INSERT payment.intents (status ready)
  → later POST /api/payment/webhooks/fake + X-Webhook-Secret
  → payment.intents = paid
  → booking marked paid
  → GET /api/trips puts the stay in upcoming
```

The return URL and `GET /api/payment/intents/{id}` are **read-only**.

## APIs

| Method | Path | Auth | Result |
| --- | --- | --- | --- |
| POST | `/api/payment/intents` | none required locally | `{ id, status: "ready", checkoutUrl }` only if booking is `awaiting_payment` and has amount. Before confirm: **404** (no booking) or 400 |
| GET | `/api/payment/intents/{id}` | no | Current status |
| POST | `/api/payment/webhooks/fake` | header `X-Webhook-Secret` | Local secret `local-dev-webhook-secret`. Wrong/missing → 401. Same `providerEventId` → idempotent paid |

## Tables

| Table | Role |
| --- | --- |
| `booking.bookings` | Must exist and be `awaiting_payment` before an intent |
| `payment.intents` | Amount copied from booking; status `ready` → `paid` |
| `payment.webhook_events` | Provider event id; unique so we do not double-apply |
| `notifications.outbox` | Paid mail job |

## Production swap

Replace fake webhook with Razorpay/Stripe webhook signature check. Same tables. Never trust the browser.
