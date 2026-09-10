import { useEffect } from "react";
import { getProgrammePricing } from "../data/programmePricing";
import { saveExpertReferralContext } from "../lib/expertLeads";
import { useListingPlanOptional } from "../lib/listingPlanContext";
import { addNights } from "../lib/pricing";

/** Syncs listing selections into session so Talk to an Expert can prefill context. */
export function ExpertReferralBridge({
  retreatId,
  retreatName,
}: {
  retreatId: string;
  retreatName: string;
}) {
  const plan = useListingPlanOptional();

  useEffect(() => {
    if (!plan) return;
    const row = plan.programmeId
      ? getProgrammePricing(retreatId, plan.programmeId)
      : null;
    const programmeName = row?.programmeName;
    let checkOut = plan.flexibleCheckOut || undefined;
    if (
      plan.checkIn &&
      plan.durationNights &&
      row &&
      row.durationMode !== "flexible"
    ) {
      checkOut = addNights(plan.checkIn, plan.durationNights);
    }

    saveExpertReferralContext({
      source: "listing",
      retreatId,
      retreatName,
      programmeId: plan.programmeId || undefined,
      programmeName,
      duration: plan.durationNights,
      checkIn: plan.checkIn || undefined,
      checkOut,
    });
  }, [
    plan,
    plan?.programmeId,
    plan?.durationNights,
    plan?.checkIn,
    plan?.flexibleCheckOut,
    retreatId,
    retreatName,
  ]);

  return null;
}
