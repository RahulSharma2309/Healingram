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

If the guest created the request **without** a token, `customer_user_id` is null and the row **never appears** on My Trips. Login first, then request.

Frontend: `src/lib/api/account.ts`, dashboard tab `?tab=trips`.
