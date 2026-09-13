# Local development

A new engineer should reach a working laptop app with this path:

```text
git clone → dependencies → Postgres → migrations → seed → API → gateway → frontend
```

## Prerequisites

- Windows 10/11 (this repo’s scripts are PowerShell) or any OS with the same tools
- .NET SDK 10.x
- Node.js 22
- Docker Desktop (Postgres + Seq + Jaeger + Mailpit)
- Git

## Clone and install

```powershell
git clone https://github.com/RahulSharma2309/Healingram.git
cd Healingram
git checkout feature/v1-final-foundation-release-readiness
npm install
```

## Database and sidecars

```powershell
./scripts/dev-deps.ps1
```

Starts Postgres `:5432` (user/password/db `healingram`), Seq `:5341`, Jaeger `:16686`, Mailpit `:8025`.

## API + gateway + website

From three terminals, repo root:

```powershell
dotnet run --project backend/src/Healingram.Api
dotnet run --project backend/src/Healingram.Gateway
npm run dev
```

| What | URL |
| --- | --- |
| Website | http://localhost:5173 |
| Gateway | http://localhost:5000 |
| API | http://localhost:5080 |
| Mailpit | http://localhost:8025 |

Vite proxies `/api` to the gateway. First API start applies `backend/db/001`–`014` and (in Development) seeds catalogue + `guest@` / `partner@` / `admin@local.test` (`Local123!`).

## Tests

```powershell
dotnet test backend/Healingram.slnx
npm test
```

Optional API smoke: `./scripts/smoke-local.ps1`. Commerce loop: `./scripts/uat-flows.ps1`.

## Common errors

| Symptom | Cause | Fix |
| --- | --- | --- |
| Catalog unavailable | API/gateway down | Start both processes |
| 401 after login | Wrong portal or missing membership | Use `partner@local.test` on `/vendor` |
| Migration conflict | Two APIs starting | Installer uses advisory lock; retry once |
| Port 5000 in use | Old gateway | Stop the other process |
| Empty needs/themes | Seed did not run | Confirm Development + `Identity/Catalog` seed flags |

See also [test-cases/how-to-run.md](test-cases/how-to-run.md) and root `README.md`.
