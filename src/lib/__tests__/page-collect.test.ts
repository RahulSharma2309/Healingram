import { describe, expect, it } from "vitest";
import { collectPages, type PageResult } from "../api/pages";

describe("collectPages", () => {
  it("follows total across pages", async () => {
    const items = await collectPages(async (page, pageSize) => {
      const all = ["a", "b", "c", "d", "e"];
      const start = (page - 1) * pageSize;
      return {
        items: all.slice(start, start + pageSize),
        page,
        pageSize,
        total: all.length,
      } satisfies PageResult<string>;
    }, 2);

    expect(items).toEqual(["a", "b", "c", "d", "e"]);
  });
});
