# Cancellation and refund

## Purpose

Stop an availability request or a booking without inventing money movement in the browser.

## Users

Customer (own request). Partner cannot mark paid. Admin does not click “paid” in Production.

## Preconditions

A server request/booking exists.

## UI / API

Customer: request detail → cancel → `POST /api/availability/requests/{publicId}/cancel`.  
Booking transitions (`BookingCommands`): cancel from `awaiting_payment`; refund path from `paid` → `refund_pending` / `refunded`. Invalid transitions are rejected.

## Tables

`availability.requests`, `booking.bookings`, `inventory.holds` (release), `payment.intents` (no client-side PAID).

## Current local behaviour

No Razorpay refund API. Refund is a server status + outbox. Future Razorpay refunds stay inside `IPaymentProvider`.

## Files

`AvailabilityService`, `BookingCommands`, `src/lib/availabilityRequests.ts`.
