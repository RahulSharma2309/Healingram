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
| GET | `/api/leads/options` | public · help types, needs, travel windows |
| GET | `/api/leads/{id}` | AdminWrite |
| GET | `/api/admin/leads` | Admin · read-only list |

Body: `fullName`, `phone`, `email`, `helpType`, `need`, `travelWindow`, `whatsappConsent`, `source`.

10-digit Indian mobiles are stored as `+91XXXXXXXXXX`.

## Tables

| Table | Role |
| --- | --- |
| `leads.expert_leads` | The lead; phone as E.164-ish |
| `leads.status_history` | `NEW` |
| `leads.options` | Configurable form choices |
| `platform.settings` | WhatsApp number and other config |

Admin can list leads. Status mutation is not implemented yet — do not fake it in the UI. Duplicate POST = duplicate rows.
