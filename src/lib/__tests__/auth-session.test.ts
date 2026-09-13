import { describe, expect, it } from "vitest";
import { userCanUseVendorPortal } from "../auth/AuthProvider";
import { getCustomerId, isFullCustomerSession, isLoggedIn, logOut } from "../auth";
import { parseDemoModeFlag } from "../runtimeConfig";
import { authErrorMessage } from "../api/auth";
import { ApiError } from "../api/client";
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

  it("does not treat a guest_request session as a full customer login", () => {
    expect(
      isFullCustomerSession({
        id: "user-2",
        email: "rahul@local.test",
        fullName: "Rahul",
        role: "customer",
        accountStatus: "registered",
        authKind: "guest_request",
      }),
    ).toBe(false);
    expect(
      isFullCustomerSession({
        id: "user-3",
        email: "rahul@local.test",
        fullName: "Rahul",
        role: "customer",
        accountStatus: "registered",
        authKind: "registered",
      }),
    ).toBe(true);
  });

  it("defaults demo mode off unless explicitly enabled", () => {
    expect(parseDemoModeFlag(undefined)).toBe(false);
    expect(parseDemoModeFlag("false")).toBe(false);
    expect(parseDemoModeFlag("true")).toBe(true);
  });

  it("surfaces vendor membership denial instead of a generic credential error", () => {
    expect(
      authErrorMessage(new ApiError(401, "This account is not linked to an approved partner.", null)),
    ).toBe("This account is not linked to an approved partner.");
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
