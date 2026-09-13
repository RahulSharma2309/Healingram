import { readdirSync, readFileSync, statSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";

const SRC = join(process.cwd(), "src");
const FORBIDDEN = [/localStorage/, /window\.localStorage/];

function walk(dir: string): string[] {
  return readdirSync(dir).flatMap((name) => {
    const path = join(dir, name);
    if (statSync(path).isDirectory()) {
      return name === "__tests__" ? [] : walk(path);
    }
    return path.endsWith(".ts") || path.endsWith(".tsx") ? [path] : [];
  });
}

describe("src must not use localStorage", () => {
  it("has no localStorage calls under src/", () => {
    const hits: string[] = [];
    for (const file of walk(SRC)) {
      const text = readFileSync(file, "utf8");
      if (FORBIDDEN.some((pattern) => pattern.test(text))) {
        hits.push(file.replace(process.cwd(), "").replaceAll("\\", "/"));
      }
    }
    expect(hits).toEqual([]);
  });
});
