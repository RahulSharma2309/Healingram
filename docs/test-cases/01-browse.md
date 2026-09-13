# Cases — browse

| ID | Steps | Expected |
| --- | --- | --- |
| TC-01-01 | Open `/` | Loads. Need chips from API or a clear empty state. No crash. |
| TC-01-02 | Open `/retreats` | Published stays listed. No invented OTA cards. |
| TC-01-03 | Open `/retreats/shathayu` | Name, place, programmes. Price only if verified. |
| TC-01-04 | `GET /api/catalog/places` | States/cities from published inventory only. |
| TC-01-05 | Open `/retreats/not-a-real-slug` | Missing / 404. No invented listing. |
| TC-01-06 | Browser console on listing | No missing-export errors. |
