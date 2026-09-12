# Prerequisites — EPIC-12 Hardening

| Need | V1 approach | Later |
| --- | --- | --- |
| Email | Mailpit | ESP |
| Uptime / APM | Seq + Jaeger | Cloud |
| Error tracking | Logs first | Sentry or similar |
| Load test | Optional k6 in Docker | — |
| Production secrets | Not this epic | Cloud secret store + HTTPS certs |

After this epic the remaining work is **deployment**, not product.
