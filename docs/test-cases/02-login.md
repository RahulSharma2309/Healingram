# Cases — login

| ID | Steps | Expected |
| --- | --- | --- |
| TC-02-01 | Login `guest@local.test` / `Local123!` | Lands `/dashboard`. Token stored. |
| TC-02-02 | `GET /api/users/me` | Same email, role `customer`. |
| TC-02-03 | Login `partner@local.test` on `/vendor` | Lands vendor queue. Server required an active membership. |
| TC-02-03b | Customer password on `/vendor` | Denied. No vendor session. |
| TC-02-03c | Partner role, membership revoked/suspended | Vendor login denied. |
| TC-02-04 | Login `admin@local.test` | Lands `/admin`. |
| TC-02-04b | Admin on vendor portal | Allowed (support policy). |
| TC-02-05 | Wrong password | 401. Clear error. No token. |
| TC-02-06 | Signup with first, last, phone, email, matching passwords | Session + dashboard. Row has first_name, last_name, phone_e164. |
| TC-02-07 | Signup same email again | 409 / already registered. |
| TC-02-08 | `GET /api/users/me` no token | 401. |
| TC-02-09 | Signup with mismatched confirm password | Client shows the error under Confirm password. No request. |
| TC-02-10 | Signup with 9 digits or a number starting 1 | Client: exactly 10 digits / must start 6–9. Server 400 if the request is forced. |
| TC-02-13 | Signup click with several empty/invalid fields | All field errors show at once. No API call. |
| TC-02-14 | Signup password `Local123` (no special) | Client and server reject. Info icon lists the four rules. |
| TC-02-15 | Signup phone `9876543210` | Stored `phone_e164` = `+919876543210`. UI prefix stays `+91`. |
| TC-02-11 | Dashboard Profile → Edit, change last name and address | PATCH `/api/users/me` 200. Fields persist after refresh. |
| TC-02-12 | Profile edit to another account’s email | 409. |
