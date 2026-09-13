import { describe, expect, it } from "vitest";
import { getCustomerId } from "../auth";
import { setSessionUser } from "../session";

describe("customer identity", () => {
  it("never invents a customer id", () => {
    setSessionUser(null);
    expect(getCustomerId()).toBeNull();
  });

  it("uses the server user id when a session exists", () => {
    setSessionUser({
      id: "user-1",
      email: "guest@local.test",
      fullName: "Local Guest",
      role: "customer",
    });
    expect(getCustomerId()).toBe("user-1");
    setSessionUser(null);
  });
});
