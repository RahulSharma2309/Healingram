/**
 * Progressive programme pricing display + totals.
 * Never fabricates amounts — only uses VERIFIED catalogue rows.
 */

import {
  formatInr,
  type OccupancyType,
  type PriceStatus,
  type ProgrammePricing,
} from "../data/programmePricing";

export type TaxDisplay = "included" | "additional" | "not_confirmed";

export type PriceSnapshot = {
  priceStatus: PriceStatus;
  baseAmount: number | null;
  taxAmount: number | null;
  taxDisplay: TaxDisplay;
  totalAmount: number | null;
  occupancy: OccupancyType | "pending";
  roomType: string;
  durationNights: number;
  guests: number;
  currency: "INR";
  label: string;
  capturedAt: string;
};

export type ProgressivePriceView =
  | { kind: "select_programme"; message: string }
  | { kind: "on_request"; message: string }
  | { kind: "from"; amount: number; label: string; taxDisplay: TaxDisplay }
  | {
      kind: "total";
      amount: number;
      label: string;
      detail: string;
      taxDisplay: TaxDisplay;
      taxNote: string;
    };

function taxDisplayFrom(row: ProgrammePricing): TaxDisplay {
  if (row.taxesIncluded) return "included";
  if (row.taxAmount != null) return "additional";
  return "not_confirmed";
}

function taxNote(display: TaxDisplay, rule: string | null): string {
  if (display === "included") return rule ?? "Taxes included";
  if (display === "additional") return rule ?? "Taxes additional";
  return rule ?? "Taxes not yet confirmed";
}

/** Lowest verified base for “From ₹…” before guests are locked */
export function getFromAmount(row: ProgrammePricing): number | null {
  if (row.priceStatus !== "VERIFIED") return null;
  const candidates = [
    row.singleOccupancyPrice,
    row.doubleOccupancyPrice,
    row.perPersonPrice,
    row.totalPackagePrice,
  ].filter((n): n is number => n != null && n > 0);
  if (candidates.length === 0) return null;
  return Math.min(...candidates);
}

/**
 * Calculate payable base for programme + guests + occupancy preference.
 * Returns null when data is insufficient (ON_REQUEST / missing amounts).
 */
export function calculateProgrammeTotal(
  row: ProgrammePricing,
  guests: number,
  occupancyPreference: "single" | "double" | "auto" = "auto",
): { base: number; occupancy: OccupancyType; roomType: string } | null {
  if (row.priceStatus !== "VERIFIED") return null;

  if (row.totalPackagePrice != null && row.occupancyType === "package") {
    return {
      base: row.totalPackagePrice,
      occupancy: "package",
      roomType: row.roomType,
    };
  }

  if (row.perPersonPrice != null) {
    return {
      base: row.perPersonPrice * guests,
      occupancy: "per_person",
      roomType: row.roomType,
    };
  }

  const prefer =
    occupancyPreference === "auto"
      ? guests === 1
        ? "single"
        : "double"
      : occupancyPreference;

  if (prefer === "single" && row.singleOccupancyPrice != null) {
    if (guests === 1) {
      return {
        base: row.singleOccupancyPrice,
        occupancy: "single",
        roomType: row.roomType,
      };
    }
    // Multiple guests on single-room rate → one room per guest when only single rate known
    return {
      base: row.singleOccupancyPrice * guests,
      occupancy: "single",
      roomType: row.roomType,
    };
  }

  if (prefer === "double" && row.doubleOccupancyPrice != null) {
    if (guests <= 2) {
      return {
        base: row.doubleOccupancyPrice,
        occupancy: "double",
        roomType: row.roomType,
      };
    }
    const rooms = Math.ceil(guests / 2);
    return {
      base: row.doubleOccupancyPrice * rooms,
      occupancy: "double",
      roomType: `${rooms} × ${row.roomType}`,
    };
  }

  // Fallbacks
  if (row.singleOccupancyPrice != null && guests === 1) {
    return {
      base: row.singleOccupancyPrice,
      occupancy: "single",
      roomType: row.roomType,
    };
  }
  if (row.doubleOccupancyPrice != null) {
    const rooms = Math.ceil(Math.max(guests, 1) / 2);
    return {
      base: row.doubleOccupancyPrice * rooms,
      occupancy: "double",
      roomType: rooms > 1 ? `${rooms} × ${row.roomType}` : row.roomType,
    };
  }

  return null;
}

export function buildProgressivePriceView(input: {
  programmeSelected: boolean;
  row: ProgrammePricing | null;
  guests: number;
  nights: number | null;
  datesComplete: boolean;
  occupancyPreference?: "single" | "double" | "auto";
}): ProgressivePriceView {
  const { programmeSelected, row, guests, nights, datesComplete } = input;

  if (!programmeSelected || !row) {
    return { kind: "select_programme", message: "Select a programme to see pricing" };
  }

  if (row.priceStatus === "ON_REQUEST" || row.priceStatus === "ESTIMATED") {
    return {
      kind: "on_request",
      message: row.priceStatus === "ON_REQUEST" ? "Price on request" : "Estimated price — confirm with retreat",
    };
  }

  const tax = taxDisplayFrom(row);

  if (!datesComplete || nights == null || nights <= 0) {
    const from = getFromAmount(row);
    if (from == null) {
      return { kind: "on_request", message: "Price available after programme selection" };
    }
    return {
      kind: "from",
      amount: from,
      label: `From ${formatInr(from)}`,
      taxDisplay: tax,
    };
  }

  const calc = calculateProgrammeTotal(row, guests, input.occupancyPreference ?? "auto");
  if (!calc) {
    return { kind: "on_request", message: "Price on request" };
  }

  const taxExtra = tax === "additional" && row.taxAmount != null ? row.taxAmount : 0;
  const total = calc.base + taxExtra;

  return {
    kind: "total",
    amount: total,
    label: formatInr(total),
    detail: `Total for ${guests} guest${guests === 1 ? "" : "s"} · ${nights} night${nights === 1 ? "" : "s"}`,
    taxDisplay: tax,
    taxNote: taxNote(tax, row.taxRule),
  };
}

export function buildPriceSnapshot(input: {
  row: ProgrammePricing;
  guests: number;
  nights: number;
  occupancyPreference?: "single" | "double" | "auto";
}): PriceSnapshot {
  const view = buildProgressivePriceView({
    programmeSelected: true,
    row: input.row,
    guests: input.guests,
    nights: input.nights,
    datesComplete: true,
    occupancyPreference: input.occupancyPreference,
  });

  const calc = calculateProgrammeTotal(
    input.row,
    input.guests,
    input.occupancyPreference ?? "auto",
  );

  const tax = taxDisplayFrom(input.row);
  let baseAmount: number | null = calc?.base ?? null;
  let totalAmount: number | null = null;
  let label = "Price on request";

  if (view.kind === "total") {
    totalAmount = view.amount;
    label = view.label;
  } else if (view.kind === "from") {
    baseAmount = view.amount;
    label = view.label;
  } else if (view.kind === "on_request" || view.kind === "select_programme") {
    label = view.message;
  }

  return {
    priceStatus: input.row.priceStatus,
    baseAmount,
    taxAmount: input.row.taxAmount,
    taxDisplay: tax,
    totalAmount,
    occupancy: calc?.occupancy ?? "pending",
    roomType: calc?.roomType ?? input.row.roomType,
    durationNights: input.nights,
    guests: input.guests,
    currency: "INR",
    label,
    capturedAt: new Date().toISOString(),
  };
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
