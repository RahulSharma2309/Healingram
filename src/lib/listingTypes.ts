import type { LaunchRetreat } from "./catalogTypes";

export type RetreatMediaCategory =
  | "property"
  | "accommodation"
  | "treatment"
  | "yoga"
  | "food"
  | "surroundings"
  | "guest"
  | "video";

export type RetreatMediaItem = {
  id: string;
  src: string;
  alt: string;
  category: RetreatMediaCategory;
  temporary?: boolean;
};

export type RetreatProgrammeOption = {
  id: string;
  label: string;
  durationLabel: string;
  supportedDurations: number[];
  priceStatus: "VERIFIED" | "ESTIMATED" | "ON_REQUEST";
  durationMode: "fixed" | "flexible";
  durationUnit: "nights";
};

export type RetreatTrustSignal = "healingram_listing" | "programme_reviewed" | "partner_verifying";

export const TRUST_SIGNAL_LABELS: Record<RetreatTrustSignal, string> = {
  healingram_listing: "Healingram listing",
  programme_reviewed: "Programme information reviewed",
  partner_verifying: "Partner information being verified",
};

export type RetreatListingView = {
  retreat: LaunchRetreat;
  regionLabel: string;
  locationLine: string;
  positioning: string;
  tags: string[];
  media: RetreatMediaItem[];
  trustSignals: RetreatTrustSignal[];
  programmeOptions: RetreatProgrammeOption[];
  durationSummary: string;
  priceLabel: string | null;
  pricePlaceholder: string;
};
