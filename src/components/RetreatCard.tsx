import { LaunchRetreatCard } from "./LaunchRetreatCard";
import type { LaunchRetreat } from "../lib/catalogTypes";

/** Legacy name — cards render published catalog retreats. */
export function RetreatCard({ retreat }: { retreat: LaunchRetreat }) {
  return <LaunchRetreatCard retreat={retreat} />;
}
