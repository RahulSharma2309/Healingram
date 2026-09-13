# How to run tests

## Laptop stack

```powershell
.\scripts\dev-deps.ps1
dotnet run --project backend/src/Healingram.Api
dotnet run --project backend/src/Healingram.Gateway
npm run dev
```

| Process | URL |
| --- | --- |
| Website | http://localhost:5173 |
| Gateway | http://localhost:5000 |
| API | http://localhost:5080 |
| Mailpit | http://localhost:8025 |

If Docker `infra-gateway-1` is on port 5000, **stop it**. That is the old stack.

Website `.env`: `VITE_API_BASE_URL=http://localhost:5000`.

## Postgres (inspect tables)

Docker Postgres from `infra/docker-compose.yml`:

```text
Host=localhost;Port=5432;Database=healingram;Username=healingram;Password=healingram
```

In DBeaver / pgAdmin / Azure Data Studio: host `localhost`, port `5432`, database `healingram`, user `healingram`, password `healingram`. Look at `catalog.retreats`, `catalog.destinations`, `catalog.programmes`.

## Demo users

Password `Local123!` — `guest@local.test` · `partner@local.test` · `admin@local.test`

## API pack (no browser)

```powershell
.\scripts\uat-flows.ps1
```

This hits the gateway: catalog, login, match, request, confirm, payment webhook, trips, wishlist, leads, admin.

## Click pack

Open the site and walk each file in this folder in order. Write Pass/Fail on a copy or in a note. Do not mark paid from the browser.
