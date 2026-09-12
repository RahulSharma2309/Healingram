# Technical — browse catalog

## Path

```text
React (/, /retreats, /retreats/:slug)
  → GET /api/catalog/...  (Vite proxies to :5000 in local dev)
  → Healingram.Gateway (YARP, CORS for localhost:5173)
  → Healingram.Api Catalog module
  → schema catalog
```

Frontend helpers: `src/lib/api/catalog.ts`, `src/lib/api/listing.ts`.

## APIs

| Method | Path | Auth | Writes? |
| --- | --- | --- | --- |
| GET | `/api/catalog/needs` | no | no |
| GET | `/api/catalog/places` | no | no |
| GET | `/api/catalog/retreats` | no | query: `need`, `state`, `locality`, `duration` |
| GET | `/api/catalog/retreats/{slug}` | no | 404 if not published |

## Tables

| Table | Role |
| --- | --- |
| `catalog.needs` | Need chips |
| `catalog.destinations` | State/city tree |
| `catalog.retreats` | Stay row; publication = active + identity complete |
| `catalog.programmes` | Programme on a retreat |
| `catalog.programme_prices` | Price rows; `priceFromInr` only shown when status is `VERIFIED` |
| `catalog.rooms` | Optional listing section |
| `catalog.inclusions` | Included / excluded labels |
| `catalog.experts` | Verified experts only on listing |
| `catalog.testimonials` | Consent **and** verified only |

## Rules in code

- No Karnataka/Kerala allow-list. Places are grouped from published retreats.
- Empty optional arrays are omitted on the listing DTO.
- Seed lives in `Healingram.Modules.Catalog` (`LaunchCatalogData` plus `LocalDemoCatalogData` for extra local-UAT states). Applied at API startup; existing slugs are left unchanged (`ON CONFLICT DO NOTHING`).
- The website does not silently substitute `LAUNCH_RETREATS` when the gateway is down. Home, `/retreats`, `/search`, and listings show empty/error instead.
