# Technical — admin

## APIs

| Method | Path | Auth |
| --- | --- | --- |
| GET | `/api/admin/availability` | AdminWrite |
| POST | `/api/admin/availability/{publicId}/note` | AdminWrite · `{ note }` |
| GET | `/api/leads/{id}` | AdminWrite |

Partner token on admin routes → `403`.  
Admin token **can** call partner confirm (same `PartnerWrite` policy).

## Tables

| Table | Role |
| --- | --- |
| `availability.requests` | All pending, not slug-scoped |
| `availability.admin_notes` | Internal; **not** on guest GET |
| `availability.status_history` | Who did what |

Admin cannot write `PAID` on availability. Paid is payment webhook → booking.
