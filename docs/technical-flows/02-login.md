# Technical — login and signup

## Path

```text
/login or /signup
  → POST /api/auth/login or /api/auth/register
  → Identity module
  → identity.users + identity.credentials
  → JWT access + refresh
```

Frontend: `src/lib/api/auth.ts`, `src/lib/auth.ts`.  
Access token: `sessionStorage` key `healingram_access_token`.  
Role after login: `homePathForRole` → `/dashboard` · `/vendor` · `/admin`.

## APIs

| Method | Path | Auth | Body / result |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | no | `{ email, password, fullName }` → tokens + user. Role forced to `customer` |
| POST | `/api/auth/login` | no | `{ email, password }` → `{ accessToken, refreshToken, user }` |
| POST | `/api/auth/refresh` | no | `{ refreshToken }` |
| POST | `/api/auth/logout` | no | revokes refresh |
| GET | `/api/users/me` | Bearer | current user or 401 |

## Tables

| Table | Role |
| --- | --- |
| `identity.users` | id, email, full name, role (`customer` / `partner` / `admin`) |
| `identity.credentials` | password hash (not the raw password) |
| `identity.refresh_tokens` | rotatable refresh; logout revokes |

## Seed

On API start: `guest@local.test`, `partner@local.test`, `admin@local.test` / `Local123!`.

JWT settings: `appsettings.json` → `Jwt:Key`, `Issuer`, `Audience`. Production must use a long random key from a secret store, not the local string.
