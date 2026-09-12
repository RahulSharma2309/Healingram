# Technical — contact / leads

## Path

```text
/contact → POST /api/leads → leads.expert_leads
Admin    → GET  /api/leads/{id}  (AdminWrite)
```

## APIs

| Method | Path | Auth |
| --- | --- | --- |
| POST | `/api/leads` | public |
| GET | `/api/leads/{id}` | AdminWrite |

Body: `fullName`, `phone`, `email`, `helpType`, `need`, `travelWindow`, `whatsappConsent`, `source`.

10-digit Indian mobiles are stored as `+91XXXXXXXXXX`.

## Tables

| Table | Role |
| --- | --- |
| `leads.expert_leads` | The lead; phone as E.164-ish |
| `leads.status_history` | `NEW` |

No admin list endpoint in V1. Duplicate POST = duplicate rows.
