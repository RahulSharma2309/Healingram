# CI and branching

## Branches

```text
main
 └── feature/v1-iteration-1          ← this iteration
      └── story/STORY-00-01-01-…     ← one story, one PR
```

`main` is not updated until the user asks to land the finished iteration.

## CI (`/.github/workflows/ci.yml`)

Runs on every pull request and push into `feature/v1-iteration-1` (and on `main` later).

1. Frontend job: `npm install`, `npm run build` (install, not `npm ci` — Windows lockfiles omit Linux-only optional native packages)
2. Backend job: `dotnet restore`, `dotnet build`, `dotnet test` (unit tests, plus integration tests when present)
3. A failing test or build fails the job. There is no `continue-on-error`.

Merge only when the workflow is green **and** QA + UAT have passed.

## CD

Out of scope for this iteration. Compose files and image Dockerfiles are the deploy contract we will hand to the host later.
