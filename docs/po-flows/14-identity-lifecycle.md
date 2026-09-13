# User and identity lifecycle

Anonymous visitor → availability request → OTP (`publicId` bound) → `guest_request` token (one request) → optional register/promote → registered customer.

Registered email/phone cannot be silently claimed by the guest create path (server returns sign-in required). Guest tokens cannot pay, patch profile, or read another request. Vendor: login only after active membership (admins excepted). Admin: role + permission.

Authoritative rules: [../engineering/security-and-authorization.md](../engineering/security-and-authorization.md).
