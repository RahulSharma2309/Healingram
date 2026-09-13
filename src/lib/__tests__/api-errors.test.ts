import { describe, expect, it } from "vitest";
import { ApiError, apiErrorMessage } from "../api/client";

describe("apiErrorMessage", () => {
  it("maps payment and ownership failures to honest copy", () => {
    expect(apiErrorMessage(new ApiError(409, "", null))).toContain("conflicted");
    expect(apiErrorMessage(new ApiError(401, "no", null))).toContain("sign in");
    expect(apiErrorMessage(new ApiError(403, "denied", null))).toBe("denied");
    expect(apiErrorMessage(new ApiError(404, "missing", null))).toContain("could not find");
    expect(apiErrorMessage(new ApiError(422, "invalid", null))).toBe("invalid");
    expect(apiErrorMessage(new ApiError(429, "slow", null))).toContain("Too many");
    expect(apiErrorMessage(new ApiError(500, "boom", null))).toContain("server");
  });
});
