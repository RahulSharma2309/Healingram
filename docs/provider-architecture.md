# Provider architecture

Last reviewed against the current factories. **Do not integrate Twilio, Razorpay, or an external inventory system in this branch.** This document is the extension map.

## Rule

The business/application layer must not know which vendor is behind a port. Adding a real provider is: implement the interface, register it in the factory, set config. Domain services stay unchanged.

Provider selection **fails startup** rather than falling back to a local adapter so production cannot silently process real traffic on the development adapter.

## OTP

```text
AuthService / OtpService → IOtpProvider → LocalOtpProvider
                                         → Twilio / MSG91 (not in this build — factory throws)
```

- Interface: `Healingram.Contracts/Otp/IOtpProvider.cs`
- Factory: `Healingram.Modules.Identity/Auth/Otp/OtpProviderFactory.cs`
- Config: `Otp:Provider` (`local` | `twilio` | `msg91`)
- Production: `local` refused unless `Otp:AllowLocalInProduction=true`. Unimplemented names always throw.
- Challenge binding: destination, purpose, optional `publicId` / user, expiry, attempt limits, atomic consume, replay reject, resend window.
- OTP values are not logged. Demo responses may include `demoCode` only when `DemoMode=true`.

To add Twilio later: implement `TwilioOtpProvider : IOtpProvider`, add the factory case, keep `OtpService` unchanged.

## Payment

```text
PaymentService → IPaymentProvider → LocalPaymentProvider (local/fake)
                                  → Razorpay / Stripe (factory throws)
```

- Interface: `Healingram.Contracts/Payment/IPaymentProvider.cs`
- Factory: `Healingram.Modules.Payment/Infrastructure/PaymentProviderFactory.cs`
- Config: `Payment:Provider`, `Payment:FakeWebhookSecret`, `Payment:AllowLocalSimulate`
- Production: local/fake refused unless `Payment:AllowLocalInProduction=true`. `AllowLocalSimulate` cannot be true. Unimplemented names throw.
- Webhook path verifies, records `provider_event_id`, transitions intent, reconciles booking, confirms inventory. Replay converges.

To add Razorpay later: implement `RazorpayPaymentProvider`, webhook signature verify, factory case. Do not put Razorpay types in Booking or Availability.

## Inventory

```text
AvailabilityService / PaymentService → IInventoryProvider → LocalInventoryProvider
                                                         → external (factory throws)
```

- Interface: `Healingram.Contracts/Inventory/IInventoryProvider.cs`
- Factory: `Healingram.Modules.Availability/Inventory/InventoryProviderFactory.cs`
- Operations: hold, confirm, release (expiry/adjustment belong on the interface for the next provider).
- Payment must not know how an external PMS works.

## What application code MUST NOT change when a provider is added

- `AvailabilityService` confirm/cancel paths
- `BookingCommands` status machine
- `PaymentService` ownership and reconcile
- `OtpService` challenge rules
- Frontend checkout (still: create intent → wait for webhook → read trips)

## Production safety

`HealingramRuntime.EnsureSafeToStart` plus the factories. Tests: `ProductionSafetyTests`, `PaymentProviderFactoryTests`, `OtpScopeAndFactoryTests`.
