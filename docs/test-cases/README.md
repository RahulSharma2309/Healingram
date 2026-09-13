# Test cases

Tick these on the **laptop**. Same order as the product flows.

**How to start the stack and the API script:** [how-to-run.md](how-to-run.md)

| File | Feature |
| --- | --- |
| [01-browse.md](01-browse.md) | Catalog |
| [02-login.md](02-login.md) | Auth |
| [03-find-my-match.md](03-find-my-match.md) | Matching |
| [04-request-availability.md](04-request-availability.md) | Book = request |
| [05-partner-confirm.md](05-partner-confirm.md) | Partner |
| [06-payment.md](06-payment.md) | Pay |
| [07-wishlist-and-trips.md](07-wishlist-and-trips.md) | Account |
| [08-contact.md](08-contact.md) | Leads |
| [09-create-retreat.md](09-create-retreat.md) | Out of V1 |
| [10-admin.md](10-admin.md) | Admin |

Mark: `Pass` · `Fail` · `Blocked` · `N/A`  
Severity if fail: `blocker` · `major` · `minor`

Integrity fails (fake price, client-marked paid, unpublished stay shown) are **blockers**.

Last automated API run (12 Sep 2026): commerce loop passed on gateway `:5000` (`HR-2026-10002` → confirm → webhook paid). You still need to click the website.
