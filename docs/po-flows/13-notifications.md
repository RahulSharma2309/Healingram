# Notifications

## Purpose

Tell the right user that a request or payment changed, without putting PII in logs.

## Flow

Module writes `notifications.outbox` in the same transaction as the business write when `IUnitOfWork` is present. A worker delivers (local: Mailpit). User-visible copies can land in `notifications.inbox` (`GET /api/notifications`).

## Current local behaviour

Mailpit UI `:8025`. No SendGrid/SES.

## Files

`Healingram.BuildingBlocks/Notifications/*`, Identity notification endpoints.
