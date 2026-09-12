# Cases — admin

| ID | Steps | Expected |
| --- | --- | --- |
| TC-10-01 | Admin → `/admin` | Pending list from server. |
| TC-10-02 | Internal note | Saved. Guest GET has no note. |
| TC-10-03 | Admin confirm | Allowed. Still cannot mark paid. |
| TC-10-04 | Partner token on admin queue | 403. |
| TC-10-05 | Simulate pay | Webhook + secret only. |
