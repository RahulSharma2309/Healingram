# Technical — create retreat

**There is no write path.**

Catalog module maps only `GET` routes. Seed: `CatalogStartupHostedService` + `LaunchCatalogData` → `INSERT` into `catalog.*` at startup (idempotent).

Partner “Retreats” hash in the UI does not call an API.

To add a stay later you either: extend seed, or (next iteration) add an admin/partner write API + UI with the same publication rules (`active` + identity complete + ≥1 programme).
