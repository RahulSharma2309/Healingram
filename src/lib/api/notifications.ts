import { apiFetch } from "./client";

export type ServerNotification = {
  id: string;
  kind: string;
  title: string;
  body: string;
  entityType?: string | null;
  entityId?: string | null;
  createdAt: string;
  readAt?: string | null;
  read: boolean;
};

export async function fetchNotifications(): Promise<ServerNotification[]> {
  const data = await apiFetch<{ items: ServerNotification[] }>("/api/notifications");
  return data.items ?? [];
}

export async function markNotificationRead(id: string): Promise<void> {
  await apiFetch(`/api/notifications/${encodeURIComponent(id)}/read`, { method: "POST" });
}
