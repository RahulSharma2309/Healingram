# Technical — find my match

## Path

```text
/questionnaire
  → POST /api/matching/sessions
  → Matching module
  → matching.match_sessions
  → catalog read port (published slugs only)
```

## APIs

| Method | Path | Auth | Body |
| --- | --- | --- | --- |
| GET | `/api/matching/options` | no | Active questions/options from `matching.questions` |
| POST | `/api/matching/sessions` | no | `{ answers: { q1[], q2[], q3, q4[] } }` |

Result: `{ id, matches: [{ slug, reasons[], retreat }] }`. `retreat` is the published catalogue card (name, place, price status, themes, image). The frontend must not download `/api/catalog/retreats` to decorate matches.

## Tables

| Table | Role |
| --- | --- |
| `matching.questions` / `matching.question_options` | Configurable questionnaire |
| `matching.match_sessions` | Stores the session id and answers |
| `catalog.retreats` / `catalog.programmes` | **Read via port**, not a SQL join from matching |

Matching never writes catalog. Unpublished slugs cannot appear in `matches`.
