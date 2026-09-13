import { formatInr } from "./money";

export type PriceStatus = "VERIFIED" | "ESTIMATED" | "ON_REQUEST";
export type OccupancyType = "single" | "double" | "per_person" | "package";
export type SettlementMode = "MARKETPLACE_SPLIT" | "PARTNER_DIRECT";
export type TaxDisplay = "included" | "additional" | "not_confirmed";

export type PriceSnapshot = {
  priceStatus: PriceStatus;
  baseAmount: number | null;
  taxAmount: number | null;
  taxDisplay: TaxDisplay;
  totalAmount: number | null;
  occupancy: OccupancyType | "pending" | string;
  roomType: string;
  durationNights: number;
  guests: number;
  currency: "INR";
  label: string;
  capturedAt: string;
};

export {
  addNights,
  formatDisplayDate,
  formatInr,
  nightsBetween,
} from "./money";

export type ProgressivePriceView =
  | { kind: "total"; label: string; detail: string; taxNote: string }
  | { kind: "from"; label: string; taxDisplay: TaxDisplay }
  | { kind: "hint"; message: string };

export function viewFromQuote(input: {
  programmeSelected: boolean;
  datesComplete: boolean;
  quote: { totalAmount?: number | null; priceStatus?: string } | null;
  quoting?: boolean;
  fromLabel: string | null;
}): ProgressivePriceView {
  if (!input.programmeSelected) {
    return { kind: "hint", message: "Select a programme to see pricing." };
  }
  if (!input.datesComplete) {
    if (input.fromLabel) {
      return { kind: "from", label: input.fromLabel, taxDisplay: "not_confirmed" };
    }
    return { kind: "hint", message: "Choose dates and guests to see the programme total." };
  }
  if (input.quoting || !input.quote) {
    return { kind: "hint", message: "Confirming the programme price…" };
  }
  if (input.quote.totalAmount != null && input.quote.priceStatus === "VERIFIED") {
    return {
      kind: "total",
      label: formatInr(input.quote.totalAmount),
      detail: "Programme total from Healingram pricing",
      taxNote: "Taxes not yet confirmed",
    };
  }
  return { kind: "hint", message: "Price available after the retreat confirms." };
}
