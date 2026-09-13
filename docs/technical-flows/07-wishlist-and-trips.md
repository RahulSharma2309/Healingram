# Technical — wishlist and trips

## Wishlist

```text
Heart → POST /api/wishlist { slug }   (Bearer)
List  → GET  /api/wishlist
Remove→ DELETE /api/wishlist/{slug}
```

Table: `identity.wishlist` (user id + retreat slug).

## Trips

```text
GET /api/trips  (Bearer, own user id only)
```

Availability lists that user’s `availability.requests`, then asks the **booking port** which confirmed ids are `paid`.

| Group | Rule |
| --- | --- |
| `paymentPending` | Status `CONFIRMED` and booking not paid |
| `upcoming` | Confirmed and booking `paid` (card status shown as `paid` after API restart with that code) |
| `completed` | Always empty in V1 |
| `cancelled` | Availability `UNAVAILABLE` |

Guest submits still get a `customer_user_id` (guest identity). After they verify with the stand-in code or create an account on that email/phone, My Trips lists those rows.

Frontend: `src/lib/api/account.ts`, dashboard tab `?tab=trips`.
