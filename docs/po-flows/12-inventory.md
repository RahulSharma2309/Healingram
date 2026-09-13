# Inventory

## Purpose

Hold capacity when a request is created, release it if the stay is cancelled/unavailable, confirm it when payment is paid.

## Users

None directly. The system does this behind availability and payment.

## Flow

Request create → `IInventoryProvider` hold → partner confirm (booking) → payment webhook → confirm hold. Cancel/unavailable → release.

## Current local behaviour

`LocalInventoryProvider` writes `inventory.holds` and checks the retreat is published. There is no external PMS.

## Future

`ExternalInventoryProvider` implements the same hold/confirm/release methods. Payment must not call the PMS directly.

## Files

`Healingram.Modules.Availability/Inventory/*`, `PaymentService.ReconcileBookingPaidAsync`.
