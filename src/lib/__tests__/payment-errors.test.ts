import { describe, expect, it } from "vitest";
import { ApiError } from "../api/client";
import { paymentFailureMessage } from "../payment";

describe("payment error states", () => {
  it("does not treat a cancelled request as a generic failure", () => {
    expect(paymentFailureMessage(new ApiError(409, "conflict", null))).toBe(
      "This request is no longer available for payment.",
    );
  });
});
