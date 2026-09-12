# Cases — request availability

| ID | Steps | Expected |
| --- | --- | --- |
| TC-04-01 | Listing → check availability → valid form | `201` public id `HR-…`, status `REQUESTED`. |
| TC-04-02 | GET that id | Same snapshot. Still `REQUESTED`. |
| TC-04-03 | Same idempotency key | Replay or 409. No second stay. |
| TC-04-04 | Missing email | Error. **No silent local booking.** |
| TC-04-05 | Unpublished slug | 400. |
| TC-04-06 | Logged-in guest | Request can show on My Trips later. |
| TC-04-07 | Pay before confirm | Intent refused (400/404). |
