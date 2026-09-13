import { describe, expect, it } from "vitest";
import { toAvailabilityRequest } from "../availabilityRequests";

describe("availability request mapping", () => {
  it("does not invent country, settlement, names, or checkout", () => {
    const request = toAvailabilityRequest({
      publicId: "HR-2026-10001",
      status: "REQUESTED",
      customerName: "Guest Local",
      email: "guest@local.test",
      retreatSlug: "published-retreat",
      programmeSlug: "panchakarma",
      requestedAt: "2026-09-13T10:00:00Z",
      snapshot: {
        checkIn: "2026-11-02",
        durationNights: 7,
        guests: 2,
        priceStatus: "ON_REQUEST",
        capturedAt: "2026-09-13T10:00:00Z",
        label: "On request",
      },
    });

    expect(request.countryCode).toBeNull();
    expect(request.settlementMode).toBeNull();
    expect(request.source).toBeNull();
    expect(request.checkOut).toBeNull();
    expect(request.retreatName).toBeNull();
    expect(request.programmeName).toBeNull();
    expect(request.bookingId).toBeNull();
  });

  it("renders server-provided stay labels and dates", () => {
    const request = toAvailabilityRequest({
      publicId: "HR-2026-10002",
      status: "CONFIRMED",
      retreatName: "Published Retreat",
      programmeName: "Panchakarma",
      checkIn: "2026-11-02",
      checkOut: "2026-11-09",
      bookingNumber: "BK-2026-10001",
      source: "listing",
    });

    expect(request.status).toBe("PAYMENT_PENDING");
    expect(request.retreatName).toBe("Published Retreat");
    expect(request.checkOut).toBe("2026-11-09");
    expect(request.bookingId).toBe("BK-2026-10001");
    expect(request.source).toBe("listing");
  });
});
