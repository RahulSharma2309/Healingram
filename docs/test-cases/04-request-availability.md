# Cases — request availability

| ID | Steps | Expected |
| --- | --- | --- |
| TC-04-01 | Listing → check availability → valid form | `201` public id `HR-…`, status `REQUESTED`. |
| TC-04-02 | GET that id without token | 401. |
| TC-04-02b | Verify email/phone + code `560142` then GET | Snapshot. Still `REQUESTED`. |
| TC-04-03 | Same idempotency key | Replay or 409. No second stay. |
| TC-04-04 | Missing email | Error. **No silent local booking.** |
| TC-04-05 | Unpublished slug | 400. |
| TC-04-06 | Logged-in guest | Request can show on My Trips later. |
| TC-04-07 | Pay before confirm | Intent refused (400/404). |
| TC-04-08 | Logged-out header **My Request** as a guest | Verify. `560142` lists that guest’s requests. No Profile / Wishlist / My Trips. Unknown contact after `560142` shows “No request for now” → home. |
| TC-04-08c | Logged-out **My Request** with a registered email | `560142` logs them in and opens dashboard Requests. |
| TC-04-08b | Open `/dashboard` while logged out | Redirect to login. No profile, no leftover request list. Header has no Wishlist. |
| TC-04-09 | Signup with that same email | Same user id. `account_status=registered`. My Trips shows the request. |
| TC-04-10 | Guest JWT creates payment intent | 403 until they create an account. |
