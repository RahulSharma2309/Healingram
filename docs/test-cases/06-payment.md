# Cases — payment

| ID | Steps | Expected |
| --- | --- | --- |
| TC-06-01 | Intent while still `REQUESTED` | 400 or 404. No charge. |
| TC-06-02 | Intent after confirm | `201` `ready`. |
| TC-06-03 | Open return URL only | Still `ready`. Not paid. |
| TC-06-04 | Fake webhook + secret | Intent `paid` once. Trip → upcoming. |
| TC-06-05 | Same provider event again | Still paid once. |
| TC-06-06 | Webhook without secret | 401. |
| TC-06-07 | Admin simulate | Must call the **server** webhook. |
