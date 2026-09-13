# Testing

## How to run

```powershell
dotnet test backend/Healingram.slnx -c Release
npm test
npm run build
```

CI (`.github/workflows/ci.yml`): frontend `npm test` + `npm run build`; backend `dotnet test`.

There is **no** Playwright/Cypress suite. Human UAT: `docs/test-cases/` and `scripts/uat-flows.ps1`.

## Backend (xUnit)

| Project | What it proves |
| --- | --- |
| Identity | Login portals, **vendor membership**, OTP consume/replay, guest scope, admin permissions |
| Catalog | Publication gate, **SQL-equivalent in-memory search**, **pagination**, empty needs/themes, quotes |
| Matching | Rank only published cards; session returns **retreat DTO** |
| Availability | Status machine, guest access, partner isolation |
| Booking | Transitions |
| Payment | Intent ownership, webhook idempotency, factory safety |
| Leads / Gateway / Api / BuildingBlocks | Capture, proxy, production safety, schema ledger + advisory lock |

Integration tests use in-memory fakes or the local model. They do not call Twilio/Razorpay.

## Frontend (Vitest)

`src/lib/__tests__/`: browse, listing emptiness, session IDs, **no localStorage**, API error messages, **single-page catalogue fetch**, matching uses server cards.

## Critical business-flow matrix

| Flow | Automated | Manual UAT |
| --- | --- | --- |
| Browse + filter + page | Catalog + frontend pagination tests | `test-cases/01-browse.md` |
| Match | MatchingServiceTests | `03-find-my-match.md` |
| Guest request + OTP | Identity + Availability | `04-request-availability.md` |
| Vendor confirm | Availability partner tests | `05-partner-confirm.md` |
| Pay + webhook replay | PaymentWebhookTests | `06-payment.md` |
| Vendor login membership | VendorMembershipLoginTests | vendor portal |
| Admin permissions | AdminPermissionPolicyTests | `10-admin.md` |

## Expected output

`dotnet test` should print `Passed!` per project with Failed: 0. `npm test` should report all files passed.

## Environment

Unit tests do not need Postgres. `uat-flows.ps1` needs the laptop stack + demo users.
