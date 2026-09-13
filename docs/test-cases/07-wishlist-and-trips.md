# Cases — wishlist and trips

| ID | Steps | Expected |
| --- | --- | --- |
| TC-07-01 | Heart while logged in | Survives refresh. |
| TC-07-02 | Un-heart | Gone. |
| TC-07-03 | Header My Trips | Opens `/dashboard?tab=trips`. |
| TC-07-07 | Header Wishlist | Opens `/dashboard?tab=wishlist`. |
| TC-07-08 | Header profile avatar | Opens `/dashboard?tab=profile`. |
| TC-07-04 | GET `/api/trips` | Only this user. Groups make sense. |
| TC-07-05 | Other user’s token | Cannot see the first guest’s trips. |
| TC-07-06 | Logged-in request → confirm → pay | Row in upcoming. |
