export type PageResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};

/** Operational queues only. Do not use this to download the public catalogue. */
export async function collectPages<T>(
  fetchPage: (page: number, pageSize: number) => Promise<PageResult<T>>,
  pageSize = 100,
): Promise<T[]> {
  const first = await fetchPage(1, pageSize);
  const items = [...(first.items ?? [])];
  const total = first.total ?? items.length;
  let page = 2;
  while (items.length < total && page <= 20) {
    const next = await fetchPage(page, pageSize);
    const batch = next.items ?? [];
    if (batch.length === 0) break;
    items.push(...batch);
    page += 1;
  }
  return items;
}
