export function formatInr(amount: number | null | undefined): string {
  if (amount == null || Number.isNaN(amount)) return "";
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    maximumFractionDigits: 0,
  }).format(amount);
}

export function formatLaunchPrice(priceFrom: number | null | undefined): string | null {
  if (priceFrom == null || Number.isNaN(priceFrom)) return null;
  return `From ${formatInr(priceFrom)}`;
}

export function nightsBetween(checkIn: string, checkOut: string): number | null {
  if (!checkIn || !checkOut) return null;
  const a = new Date(`${checkIn}T12:00:00`);
  const b = new Date(`${checkOut}T12:00:00`);
  if (Number.isNaN(a.getTime()) || Number.isNaN(b.getTime())) return null;
  const diff = Math.round((b.getTime() - a.getTime()) / (1000 * 60 * 60 * 24));
  return diff > 0 ? diff : null;
}

export function addNights(checkIn: string, nights: number): string {
  const d = new Date(`${checkIn}T12:00:00`);
  d.setDate(d.getDate() + nights);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

export function formatDisplayDate(iso: string): string {
  if (!iso) return "";
  const d = new Date(`${iso}T12:00:00`);
  return d.toLocaleDateString("en-IN", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
}
