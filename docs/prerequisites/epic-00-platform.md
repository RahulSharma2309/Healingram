# Prerequisites — EPIC-00 Platform

| Need | V1 approach | Paid? | Later replace |
| --- | --- | --- | --- |
| .NET SDK 10 | Local install (already on this machine: 10.0.301) | No | — |
| Node.js 20+ | Local install for the React app | No | — |
| Docker Desktop | Run Postgres, Seq, Jaeger, Mailpit, and the app stack | No (Docker Desktop licence if a large company — check) | — |
| PostgreSQL | Docker service `postgres:16-alpine`. Localhost install optional (same port 5432). | No | Managed Postgres on the cloud |
| Log viewer | Seq in Docker (`datalust/seq`) | Free for single-user dev | Cloud logs |
| Traces | Jaeger all-in-one in Docker | No | Cloud APM |
| Email catcher | Mailpit in Docker | No | Real ESP |
| GitHub Actions | Uses the repo’s free minutes | No until private-minute overage | Same pipeline in the cloud host |

No external vendor accounts are required to finish this epic.
