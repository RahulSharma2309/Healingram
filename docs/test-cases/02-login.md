# Cases — login

| ID | Steps | Expected |
| --- | --- | --- |
| TC-02-01 | Login `guest@local.test` / `Local123!` | Lands `/dashboard`. Token stored. |
| TC-02-02 | `GET /api/users/me` | Same email, role `customer`. |
| TC-02-03 | Login `partner@local.test` | Lands `/vendor`. |
| TC-02-04 | Login `admin@local.test` | Lands `/admin`. |
| TC-02-05 | Wrong password | 401. Clear error. No token. |
| TC-02-06 | Signup new email | Session + dashboard. |
| TC-02-07 | Signup same email again | 409 / already registered. |
| TC-02-08 | `GET /api/users/me` no token | 401. |
