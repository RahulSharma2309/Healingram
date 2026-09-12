-- Payment lookup helpers (idempotent). Safe to re-run. No cross-schema foreign keys.

CREATE INDEX IF NOT EXISTS bookings_snapshot_request_public_id_idx
    ON booking.bookings ((snapshot->>'requestPublicId'));

CREATE INDEX IF NOT EXISTS payment_intents_booking_id_idx
    ON payment.intents (booking_id);
