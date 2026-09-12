# Prerequisites — EPIC-02 Identity

| Need | V1 approach | Paid? | Later |
| --- | --- | --- | --- |
| Password hashing | ASP.NET Identity / PBKDF2 or Argon2 in-process | No | — |
| Email verify / reset | Mailpit in Docker | No | ESP |
| SMS OTP | **Not in V1** | — | SMS provider |
| OAuth social login | **Not in V1** | — | IdP |
| Secrets | `appsettings.Development.json` + user-secrets / `.env` (never commit) | No | Cloud secret store |
