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
| POST | `/api/auth/register` | no | `{ firstName, lastName, phone, email, password, confirmPassword, address? }` → tokens + user. Role forced to `customer`. Address optional. |
| POST | `/api/auth/login` | no | `{ email, password }` → `{ accessToken, refreshToken, user }` |
| POST | `/api/auth/refresh` | no | `{ refreshToken }` |
| POST | `/api/auth/logout` | no | revokes refresh |
| GET | `/api/users/me` | Bearer | `{ id, email, fullName, role, firstName, lastName, phone, address }` |
| PATCH | `/api/users/me` | Bearer | `{ firstName, lastName, phone, email, address? }` → same user shape. No password change. |

The signup form validates every field at once on **Sign up** and does not call the API until the form is clean. The server repeats the same rules.

Register and profile reject: missing names; phone that is not exactly 10 digits starting 6–9; missing or invalid email (`name@example.com`); address over 200 characters; a new password that is not at least 8 characters with a letter, a number, and a special character; or confirm password mismatch. Duplicate email is 409.

The phone field is India-only (`+91` shown, not editable). The guest types 10 digits. `identity.users.phone_e164` stores country code + number (`+91` + 10 digits). The user payload also returns `phoneCountryCode: "+91"`. `fullName` is first + last.

## Tables

| Table | Role |
| --- | --- |
| `identity.users` | id, email, first_name, last_name, phone_e164, address, full_name, role (`customer` / `partner` / `admin`) |
| `identity.credentials` | password hash (not the raw password) |
| `identity.refresh_tokens` | rotatable refresh; logout revokes |

## Seed

On API start: `guest@local.test`, `partner@local.test`, `admin@local.test` / `Local123!`.

JWT settings: `appsettings.json` → `Jwt:Key`, `Issuer`, `Audience`. Production must use a long random key from a secret store, not the local string.
