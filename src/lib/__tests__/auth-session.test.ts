import { describe, expect, it } from "vitest";
import { userCanUseVendorPortal } from "../auth/AuthProvider";
import { getCustomerId, isLoggedIn, logOut } from "../auth";
import { getSessionUser, setSessionUser } from "../session";

describe("auth bootstrap helpers", () => {
  it("clears in-memory session on logout without inventing a customer id", () => {
    setSessionUser({
      id: "user-9",
      email: "guest@local.test",
      fullName: "Local Guest",
      role: "customer",
      accountStatus: "registered",
    });
    expect(getCustomerId()).toBe("user-9");
    logOut();
    expect(getSessionUser()).toBeNull();
    expect(getCustomerId()).toBeNull();
    expect(isLoggedIn()).toBe(false);
  });

  it("does not open the vendor portal from a customer-only profile", () => {
    expect(
      userCanUseVendorPortal({
        id: "user-1",
        email: "guest@local.test",
        fullName: "Guest",
        role: "customer",
        roles: ["customer"],
        partnerMemberships: [],
      }),
    ).toBe(false);
  });
});
