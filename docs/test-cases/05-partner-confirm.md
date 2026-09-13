# Cases — partner confirm

| ID | Steps | Expected |
| --- | --- | --- |
| TC-05-01 | Partner → `/vendor` | Server queue, not only localStorage. |
| TC-05-02 | Confirm with INR amount | Status `CONFIRMED`. Guest is payment-ready. |
| TC-05-03 | Guest GET public id | Confirmed + `finalAmountInr`. |
| TC-05-04 | Customer JWT confirm | 403. |
| TC-05-05 | Alternative | `ALTERNATIVE_OFFERED`. Guest can accept. |
| TC-05-06 | Unavailable | Guest cannot pay. |
| TC-05-07 | Confirm while signed out | Error shown. Not “confirmed” only locally. |
