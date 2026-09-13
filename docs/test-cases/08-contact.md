# Cases — contact

| ID | Steps | Expected |
| --- | --- | --- |
| TC-08-01 | Submit `/contact` | Lead id. Thank-you, not a fake callback promise. |
| TC-08-02 | Missing fields | Stay on the form. |
| TC-08-03 | Admin GET lead | 200. Guest GET → 403. |
| TC-08-04 | Phone/email | Not in the URL. |
