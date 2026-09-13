# Configuration

Never commit real secrets. Examples below are safe local values.

| Name | Purpose | Required | Local | Staging | Production | Secret? | Failure if missing / wrong |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Host environment | yes | Development | Staging | Production | no | Production gates only apply when `Production` |
| `ConnectionStrings__Postgres` | DB | API yes | localhost healingram/healingram | managed URL | managed URL | yes | API will not start |
| `Schema__ApplyOnStartup` | Run migrator | no | true | true | true (or migrate in release job) | no | empty schema |
| `Schema__SqlPath` | Hint file; installer loads the whole `db/` folder | no | `db/001_schemas.sql` | same | `/app/db/001_schemas.sql` | no | FileNotFound |
| `Jwt__Key` | Access-token HMAC | API yes | local default | unique | unique ≥32 chars | yes | Production refuses default |
| `Jwt__Issuer` / `Jwt__Audience` | JWT claims | yes | Healingram | env-specific | env-specific | no | token mismatch |
| `DemoMode` | Demo OTP display, simulate UI | no | true | false | **false** | no | Production refuses true |
| `Identity__SeedOnStartup` | Demo users | no | true | false | **false** | no | Production refuses true |
| `Partners__SeedOnStartup` | Local partner | no | true | false | **false** | no | Production refuses true |
| `Otp__Provider` | OTP adapter | yes | local | twilio/msg91 when built | real provider | no | unknown/unimplemented throws |
| `Otp__LocalCode` | Dev OTP | no | 560142 | unused | unused | yes-ish | only local provider |
| `Otp__AllowLocalInProduction` | Escape hatch | no | n/a | false | false | no | if true, local OTP in prod (do not) |
| `Payment__Provider` | Payment adapter | yes | local | razorpay when built | razorpay | no | unknown throws |
| `Payment__FakeWebhookSecret` | Local webhook auth | yes for local | `local-dev-webhook-secret` | unused | unused | yes | Production refuses default |
| `Payment__AllowLocalSimulate` | Admin/local webhook | no | true | false | **false** | no | Production refuses true |
| `Inventory__Provider` | Inventory adapter | yes | local | local or external | external when built | no | unknown throws |
| `App__CustomerUrl` / `VendorUrl` / `AdminUrl` | CORS + portal URLs | prod yes | localhost:5173 | real HTTPS | real HTTPS | no | Production CORS is these only |
| `App__AdditionalCorsOrigins` | Extra CORS | no | empty | empty | empty | no | |
| `RateLimiting__Policies__*__PermitLimit` | Per-policy cap | no | defaults | tune | tune | no | 429 |
| `Seq__Url` | Logs | no | http://localhost:5341 | vendor | vendor | no | console only |
| `OpenTelemetry__OtlpEndpoint` | Traces | no | http://localhost:4317 | vendor | vendor | no | no traces |
| `ReverseProxy__Clusters__monolith__Destinations__api__Address` | Gateway upstream | gateway | http://localhost:5080 | internal URL | internal URL | no | 502 |
| `VITE_API_BASE_URL` | Frontend API | frontend | empty (proxy) or http://localhost:5000 | gateway URL | gateway URL | no | calls fail |
| `VITE_DEMO_MODE` | Demo banner / printed OTP | frontend | `true` via `.env.development` | false | **false** (unset = false) | no | banner only when explicitly `true` |
| `VITE_CUSTOMER_APP_URL` / `VENDOR` / `ADMIN` | Portal hosts | optional | empty | real | real | no | same-origin /vendor /admin |

Local demo users (Development seed only): `guest@local.test`, `partner@local.test`, `admin@local.test` / `Local123!`.
