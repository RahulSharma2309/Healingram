# Healingram

Programme-led retreat marketplace. The real app is `src/` (React) talking to a .NET modular monolith through a gateway. **Documentation:** [`docs/README.md`](docs/README.md).

## Run on this laptop

```powershell
.\scripts\dev-deps.ps1
dotnet run --project backend/src/Healingram.Api
dotnet run --project backend/src/Healingram.Gateway
npm install
npm run dev
```

| What | URL |
| --- | --- |
| Website | http://localhost:5173 |
| Gateway | http://localhost:5000 |
| API | http://localhost:5080 |
| Mailpit | http://localhost:8025 |

`VITE_API_BASE_URL=http://localhost:5000`.  
Demo login: `guest@local.test` / `partner@local.test` / `admin@local.test` — password `Local123!`.

If something else owns port 5000 (old Docker `infra-gateway-1`), stop it.

## Tests

```powershell
dotnet test backend/Healingram.slnx
.\scripts\uat-flows.ps1
```

Product cases to click: `docs/test-cases/`.

## Spec

`Healingram_Developer_Functional_Specification_V1.docx` plus the human overrides in `docs/po-flows/00-what-healingram-is.md`.
