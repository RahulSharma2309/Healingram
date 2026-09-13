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
| GET | `/api/catalog/retreats` | no | query: `need`, `state`, `locality`, `duration` (comma-separated), `page`, `pageSize`. Response `{ items, page, pageSize, total }` |
| GET | `/api/content/homepage` | no | `{ sections }` from `content.sections` |
| GET | `/api/content/navigation` | no | `?menu=` → `{ menu, items }` from `content.navigation_*` |
| GET | `/api/catalog/retreats/{slug}` | no | 404 if not published |
| GET | `/api/catalog/discovery` | no | Homepage need/destination cards |
| GET | `/api/catalog/themes` | no | Programme theme labels |
| POST | `/api/catalog/pricing/quote` | no | Server price snapshot |
| GET | `/api/content/pages` | no | Published About / FAQ / blog |
| GET | `/api/platform/settings` | no | WhatsApp and other platform config |

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
| `content.sections` | Homepage and other surfaces |
| `content.navigation_menus` / `content.navigation_items` | Footer / account / header menus |

## Rules in code

- No Karnataka/Kerala allow-list. Places are grouped from published retreats.
- Empty optional listing arrays stay empty. The website does not invent experts, rooms, or media.
- Seed lives in `Healingram.Modules.Catalog` (`LaunchCatalogData` plus `LocalDemoCatalogData` for extra local-UAT states). Applied at API startup; existing slugs are left unchanged (`ON CONFLICT DO NOTHING`).
- The website does not silently substitute `LAUNCH_RETREATS` when the gateway is down. Home, `/retreats`, `/search`, and listings show an error, not an empty catalog.
