# SereniTrip / Healingram — V1 iteration

Programme-led retreat marketplace. This iteration lives on **`feature/v1-iteration-1`**. Product, architecture, and story status: [`docs/README.md`](docs/README.md).

The React app in `src/` is the UX shell. The .NET modular monolith and gateway live in `backend/`.

## Run with Docker (app + Postgres)

```powershell
.\scripts\dev-up.ps1 -WithObservability
```

- Web: http://localhost:8080
- Gateway: http://localhost:5000/api/health
- API: http://localhost:5080/api/health
- Seq logs: http://localhost:5341
- Jaeger traces: http://localhost:16686
- Mailpit: http://localhost:8025
- Postgres: `localhost:5432` / user `healingram` / db `healingram`

## Run on localhost (deps in Docker)

```powershell
.\scripts\dev-deps.ps1
dotnet run --project backend/src/Healingram.Api
dotnet run --project backend/src/Healingram.Gateway
npm install
npm run dev
```

Frontend: http://localhost:5173 (or the next free port). Point it at the gateway with `VITE_API_BASE_URL=http://localhost:5000`.

## Tests

```powershell
dotnet test backend/Healingram.slnx
```

## Branching

- Iteration: `feature/v1-iteration-1`
- Story: `story/STORY-XX-YY-ZZ-slug`
- PR target: the iteration branch. CI must be green. QA + PO UAT before merge.
- `main` receives the finished iteration later.

## Spec

`Healingram_Developer_Functional_Specification_V1.docx` is authoritative for V1 behaviour.
