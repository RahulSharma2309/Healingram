import { describe, expect, it, vi } from "vitest";

vi.mock("../api/client", () => ({
  apiFetch: vi.fn(),
}));

import { apiFetch } from "../api/client";
import { CATALOG_PAGE_SIZE, fetchRetreatsPage } from "../api/catalog";
import { matchesFromSession } from "../api/matching";

describe("catalogue pagination", () => {
  it("requests a single bounded page", async () => {
    vi.mocked(apiFetch).mockResolvedValueOnce({
      items: [{ slug: "ayurvedagram", name: "Ayurvedagram", locality: "Whitefield", stateSlug: "karnataka", stateLabel: "Karnataka", priceStatus: "VERIFIED", programmeThemes: [] }],
      page: 2,
      pageSize: CATALOG_PAGE_SIZE,
      total: 40,
    });

    const page = await fetchRetreatsPage({ page: 2 });

    expect(page.items).toHaveLength(1);
    expect(page.page).toBe(2);
    expect(page.total).toBe(40);
    expect(String(vi.mocked(apiFetch).mock.calls[0][0])).toContain("page=2");
    expect(String(vi.mocked(apiFetch).mock.calls[0][0])).toContain(`pageSize=${CATALOG_PAGE_SIZE}`);
  });
});

describe("matching session", () => {
  it("uses server-owned retreat cards and does not invent matches when cards are missing", async () => {
    await expect(
      matchesFromSession({
        id: "session",
        matches: [{ slug: "missing", reasons: ["Need"] }],
      }),
    ).rejects.toThrow(/missing retreat data/i);

    const ranked = await matchesFromSession({
      id: "session",
      matches: [
        {
          slug: "ayurvedagram",
          reasons: ["Ayurveda"],
          retreat: {
            slug: "ayurvedagram",
            name: "Ayurvedagram",
            locality: "Whitefield",
            stateSlug: "karnataka",
            stateLabel: "Karnataka",
            priceStatus: "VERIFIED",
            programmeThemes: ["ayurveda"],
          },
        },
      ],
    });

    expect(ranked).toHaveLength(1);
    expect(ranked[0].retreat.name).toBe("Ayurvedagram");
  });
});
