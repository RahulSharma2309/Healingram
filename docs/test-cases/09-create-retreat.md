# Cases — create retreat

**N/A for V1 UI.** There is no create form and no `POST /api/catalog`.

| ID | Steps | Expected |
| --- | --- | --- |
| TC-09-01 | Partner looks for Create | No working editor. |
| TC-09-02 | GET `/api/catalog/retreats` | Seeded published stays exist. |
| TC-09-03 | Unpublished row | Hidden on `/retreats`. |

Fail only if the UI claims a retreat was created but catalog GET does not return it.
