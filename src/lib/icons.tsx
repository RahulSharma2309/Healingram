import type { ReactNode } from "react";
import {
  Battery,
  Brain,
  CalendarDays,
  Compass,
  Flower2,
  Hourglass,
  Leaf,
  ListChecks,
  MapPin,
  Moon,
  Palmtree,
  Sparkles,
  Stethoscope,
} from "lucide-react";

const ICONS: Record<string, ReactNode> = {
  brain: <Brain className="w-5 h-5" />,
  battery: <Battery className="w-5 h-5" />,
  leaf: <Leaf className="w-5 h-5" />,
  sparkles: <Sparkles className="w-5 h-5" />,
  palmtree: <Palmtree className="w-5 h-5" />,
  stethoscope: <Stethoscope className="w-5 h-5" />,
  flower: <Flower2 className="w-5 h-5" />,
  moon: <Moon className="w-5 h-5" />,
  list: <ListChecks className="w-5 h-5" />,
  compass: <Compass className="w-5 h-5" />,
  calendar: <CalendarDays className="w-5 h-5" />,
  hourglass: <Hourglass className="w-5 h-5" />,
  map: <MapPin className="w-5 h-5" />,
};

export function iconForKey(key?: string | null): ReactNode {
  if (!key) return <Sparkles className="w-5 h-5" />;
  return ICONS[key] ?? <Sparkles className="w-5 h-5" />;
}
