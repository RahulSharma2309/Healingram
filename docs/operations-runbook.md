# Operations runbook

For an operator who has logs and Postgres but has not read the whole repo.

## Health

- Gateway: `GET http://localhost:5000/health` (or the deployed equivalent)
- API module ready: `GET /api/catalog/ready`, `/api/identity/ready`, …
- Correlation: every request should carry/return `X-Correlation-Id`. Search Seq for that id.

## Logs

Seq (local `:5341`) or the production log sink. Look for `User {UserId}`, `match session`, `webhook`, booking ids. **Never** expect OTP codes, passwords, or tokens in logs.

## Common failures

| Symptom | Likely cause | What to do |
| --- | --- | --- |
| Site shows “Catalog unavailable” | API/gateway down or 5xx | Check API process, Postgres, correlation id |
| Empty retreat grid with 200 | No published rows | Check `catalog.retreats` status + programmes — this is a valid empty state |
| Vendor login “not linked to an approved partner” | No active membership | Check `partners.partners.status` and `partner_users.status` |
| 403 on partner confirm | Wrong slug / revoked membership | `PartnerWrite` re-checks membership every call |
| Payment stays awaiting | Webhook not received | Check `payment.webhook_events`; replay is safe |
| Webhook 404 | `AllowLocalSimulate=false` | Expected in Production |
| OTP always wrong | Wrong destination/publicId or consumed | New challenge; do not reuse |
| Migration failed | DDL error mid-file | Transaction rolled back; fix script; do not hand-edit ledger unless you know the file applied |
| Two instances racing migrate | Should block on advisory lock | If a lock is stuck, the locking connection died — reconnect clears session locks |

## Payment webhook

1. Confirm `payment.intents` row and booking `awaiting_payment`.
2. Inspect `payment.webhook_events.provider_event_id` and `processing_status`.
3. Replaying the same event must not double-pay. If status is `failed`, provider retry can finish reconcile.

## OTP

Challenges are destination + purpose + publicId bound. Production must not use `LocalOtpProvider` unless an explicit allow flag is set (do not).

## Rollback

App: redeploy previous image. Schema: restore Postgres from backup (there is no automatic down-migration). Ledger `schema_migrations` tells you which files ran.

## Backup restore

Restore the managed Postgres backup, start one API instance, confirm ledger, then start remaining instances.

## Incident investigation order

1. Correlation ID from the user/support ticket  
2. Seq/Jaeger for that id  
3. `audit.events` for login/payment  
4. Row in requests / bookings / intents / webhook_events  
5. Partner membership if vendor-related
