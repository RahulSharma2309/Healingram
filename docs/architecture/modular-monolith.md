# Modular monolith

## Module list

| Module | Schema | Owns |
| --- | --- | --- |
| Identity | `identity` | Users, credentials, roles, sessions |
| Catalog | `catalog` | Retreats, taxonomy, programmes, rooms, prices, experts, testimonials, inclusions, publication |
| Matching | `matching` | Find My Match answers (non-sensitive slugs) and match explanations |
| Availability | `availability` | Requests, alternatives, status history, snapshots |
| Booking | `booking` | Payment-ready records, bookings, My Trips projections |
| Payment | `payment` | Payment intents, webhook events |
| Leads | `leads` | Expert concierge leads and consent |
| Partners | `partners` | Partner profile, queue view metadata, aging timestamps |

Admin UI uses the same modules with `admin` policies. There is no second admin database.

## Communication

```text
Module A  →  ISomethingPort (Healingram.Contracts)  →  Module B adapter
```

In V1 the adapter is in-process. Later the adapter becomes an HTTP client. Domain events stay in an outbox table so we can switch the dispatcher to a broker without changing publishers.

## Extractability checklist (do not break)

- No foreign keys across schemas.
- No shared “god” entity project.
- Migrations are per module.
- Seed data for launch inventory lives in Catalog only.

## Solution layout

```text
backend/
  Healingram.sln
  src/Healingram.BuildingBlocks/
  src/Healingram.Contracts/
  src/Healingram.Api/          # host: DI, middleware, module registration
  src/Healingram.Gateway/
  src/Modules/Healingram.Modules.*/
  tests/
```
