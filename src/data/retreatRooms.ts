/**
 * Listing Component 7 — retreat rooms / accommodation.
 *
 * Descriptions below are paraphrased from publicly listed stay pages for the
 * partner (ayurvedagram.com / partner materials). Images are temporary
 * placeholders until partner media is supplied — not claimed as verified
 * room photography.
 *
 * Do not invent amenities, sizes, or price modifiers. Only include verified fields.
 * Healingram sells programmes with stay — never present independent nightly room rates.
 */

import { formatInr } from "./programmePricing";
import { LAUNCH_PROGRAMME_LABELS } from "./launchSupply";

export type RoomOccupancyType = "single" | "double" | "triple" | "family";

export type RoomPricingStatus = "included" | "modifier" | "on_request";

export type RetreatRoom = {
  roomId: string;
  retreatId: string;
  roomName: string;
  slug: string;
  shortDescription: string;
  longDescription: string;
  images: { src: string; alt: string; temporary?: boolean }[];
  maxGuests: number;
  occupancyTypes: RoomOccupancyType[];
  bedConfiguration: string | null;
  roomSize: string | null;
  bathroomType: string | null;
  airConditioning: boolean | null;
  view: string | null;
  amenities: string[];
  compatibleProgrammeIds: string[];
  /** Verified INR package delta vs base programme room — never a nightly rate */
  pricingModifier: number | null;
  pricingStatus: RoomPricingStatus;
  goodToKnow: string[];
  verified: boolean;
};

/** Temporary accommodation imagery — replace with partner room photos */
const TEMP_ROOM = {
  a: "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?w=1200&q=80",
  b: "https://images.unsplash.com/photo-1616594039964-ae9021a400a0?w=1200&q=80",
  c: "https://images.unsplash.com/photo-1590490360182-c33d57733427?w=1200&q=80",
  d: "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=1200&q=80",
} as const;

/**
 * Seeded rooms — only include when name + description are supported by a
 * public partner source or confirmed partner feed.
 */
const ROOMS: RetreatRoom[] = [
  {
    roomId: "ayurvedagram-heritage",
    retreatId: "ayurvedagram",
    roomName: "Heritage room",
    slug: "heritage-room",
    shortDescription:
      "A heritage cottage room with traditional character, shaped for rest within the campus’s restored Kerala homes.",
    longDescription:
      "Heritage rooms sit within small cottages relocated and restored from Kerala. Interiors emphasise timber character, private garden or sit-out space where the cottage allows, and a calmer setting for programme rest between therapies and practices.",
    images: [
      {
        src: TEMP_ROOM.a,
        alt: "Temporary placeholder — heritage room",
        temporary: true,
      },
      {
        src: TEMP_ROOM.b,
        alt: "Temporary placeholder — heritage room interior",
        temporary: true,
      },
    ],
    maxGuests: 2,
    occupancyTypes: ["single", "double"],
    bedConfiguration: "Double bed",
    roomSize: null,
    bathroomType: "Private bathroom",
    airConditioning: null,
    view: "Garden / cottage setting",
    amenities: ["Private bathroom", "Reading / writing space", "Garden or sit-out where available"],
    compatibleProgrammeIds: [
      "rejuvenation",
      "panchakarma",
      "ayurveda",
      "detox",
      "yoga",
      "long_stay",
    ],
    pricingModifier: null,
    pricingStatus: "included",
    goodToKnow: [
      "Rooms are heritage cottages — interiors are intentionally simple",
      "Televisions are not provided in rooms",
      "Accommodation is spread across the campus among heritage cottages",
    ],
    verified: true,
  },
  {
    roomId: "ayurvedagram-poets-cottage",
    retreatId: "ayurvedagram",
    roomName: "Poet’s cottage",
    slug: "poets-cottage",
    shortDescription:
      "An independent teak-and-rosewood cottage overlooking a private courtyard, once home to a renowned 19th-century poet.",
    longDescription:
      "Nallamuttam Tharavadu — known as the Poet’s cottage — is an independent cottage made of teak and rosewood. It overlooks a small private courtyard, with a spacious bedroom under a traditional vaulting roof. Suited to guests seeking a quieter, more private stay within their programme.",
    images: [
      {
        src: TEMP_ROOM.c,
        alt: "Temporary placeholder — Poet’s cottage",
        temporary: true,
      },
      {
        src: TEMP_ROOM.a,
        alt: "Temporary placeholder — cottage courtyard setting",
        temporary: true,
      },
    ],
    maxGuests: 2,
    occupancyTypes: ["single", "double"],
    bedConfiguration: "Spacious bedroom",
    roomSize: null,
    bathroomType: "Private bathroom",
    airConditioning: null,
    view: "Private courtyard",
    amenities: ["Independent cottage", "Private courtyard", "Traditional vaulting roof"],
    compatibleProgrammeIds: [
      "rejuvenation",
      "panchakarma",
      "ayurveda",
      "detox",
      "yoga",
      "long_stay",
    ],
    pricingModifier: null,
    pricingStatus: "on_request",
    goodToKnow: [
      "Independent cottage setting — quieter and more private than shared heritage wings",
      "Televisions are not provided in rooms",
    ],
    verified: true,
  },
  {
    roomId: "ayurvedagram-harmony-suite",
    retreatId: "ayurvedagram",
    roomName: "Harmony suite",
    slug: "harmony-suite",
    shortDescription:
      "A 75+ year teak-and-rosewood cottage in its own garden — two rooms and two bathrooms, including one open to the sky.",
    longDescription:
      "Muhamma Mana — the Harmony suite — is a 75+ year-old cottage of teak and rosewood in its own garden with a walled compound. It includes two rooms and two bathrooms (one enclosed, one open to the sky). Often chosen by couples or guests who want more space within a longer programme.",
    images: [
      {
        src: TEMP_ROOM.d,
        alt: "Temporary placeholder — Harmony suite",
        temporary: true,
      },
      {
        src: TEMP_ROOM.b,
        alt: "Temporary placeholder — suite garden setting",
        temporary: true,
      },
    ],
    maxGuests: 4,
    occupancyTypes: ["single", "double", "family"],
    bedConfiguration: "Two rooms",
    roomSize: null,
    bathroomType: "Two bathrooms (one enclosed, one open to the sky)",
    airConditioning: null,
    view: "Private garden, walled compound",
    amenities: [
      "Independent cottage",
      "Private garden",
      "Two bathrooms",
      "Open-to-sky bathroom option",
    ],
    compatibleProgrammeIds: [
      "rejuvenation",
      "panchakarma",
      "ayurveda",
      "detox",
      "yoga",
      "long_stay",
    ],
    pricingModifier: null,
    pricingStatus: "on_request",
    goodToKnow: [
      "Larger cottage footprint — useful for couples or guests wanting more space",
      "Televisions are not provided in rooms",
      "Accommodation is spread across the campus among heritage cottages",
    ],
    verified: true,
  },
  {
    roomId: "ayurvedagram-travancore",
    retreatId: "ayurvedagram",
    roomName: "Travancore room",
    slug: "travancore-room",
    shortDescription:
      "A reflective chamber with a carved wooden oonjal (swing), once the private room of a Travancore household head.",
    longDescription:
      "The Travancore room was once the private chamber of the Karanavar, head of an aristocratic Travancore household. A carved wooden oonjal (swing) anchors the room, offering space for reflection and quiet rest between programme sessions.",
    images: [
      {
        src: TEMP_ROOM.b,
        alt: "Temporary placeholder — Travancore room",
        temporary: true,
      },
    ],
    maxGuests: 2,
    occupancyTypes: ["single", "double"],
    bedConfiguration: null,
    roomSize: null,
    bathroomType: "Private bathroom",
    airConditioning: null,
    view: null,
    amenities: ["Carved wooden oonjal (swing)", "Private bathroom"],
    compatibleProgrammeIds: [
      "rejuvenation",
      "panchakarma",
      "ayurveda",
      "detox",
      "yoga",
      "long_stay",
    ],
    pricingModifier: null,
    pricingStatus: "on_request",
    goodToKnow: [
      "Rooms are heritage cottages — interiors are intentionally simple",
      "Televisions are not provided in rooms",
    ],
    verified: true,
  },
];

export function getRetreatRooms(retreatId: string): RetreatRoom[] {
  return ROOMS.filter((r) => r.retreatId === retreatId && r.verified);
}

export function hasRetreatRooms(retreatId: string): boolean {
  return getRetreatRooms(retreatId).length > 0;
}

export function getRoomById(roomId: string): RetreatRoom | undefined {
  return ROOMS.find((r) => r.roomId === roomId && r.verified);
}

export function isRoomCompatibleWithProgramme(
  room: RetreatRoom,
  programmeId: string | null | undefined,
): boolean {
  if (!programmeId) return true;
  if (room.compatibleProgrammeIds.length === 0) return true;
  return room.compatibleProgrammeIds.includes(programmeId);
}

export function roomFitsGuestCount(room: RetreatRoom, guests: number): boolean {
  if (!Number.isFinite(guests) || guests < 1) return true;
  return guests <= room.maxGuests;
}

/** Prefer rooms compatible with selected programme; incompatible last */
export function sortRoomsForPlan(
  rooms: RetreatRoom[],
  programmeId: string | null | undefined,
): RetreatRoom[] {
  if (!programmeId) return rooms;
  return [...rooms].sort((a, b) => {
    const aOk = isRoomCompatibleWithProgramme(a, programmeId) ? 0 : 1;
    const bOk = isRoomCompatibleWithProgramme(b, programmeId) ? 0 : 1;
    return aOk - bOk;
  });
}

export function programmeLabel(programmeId: string): string {
  return (
    LAUNCH_PROGRAMME_LABELS[programmeId as keyof typeof LAUNCH_PROGRAMME_LABELS] ??
    programmeId.replace(/_/g, " ")
  );
}

/**
 * Contextual price impact — never a nightly room rate.
 * Only surfaces a verified modifier amount when pricingStatus === "modifier".
 */
export function roomPriceImpactLabel(room: RetreatRoom): string {
  if (room.pricingStatus === "included") {
    return "Included in programme price";
  }
  if (room.pricingStatus === "modifier" && room.pricingModifier != null) {
    const sign = room.pricingModifier >= 0 ? "+" : "−";
    return `${sign} ${formatInr(Math.abs(room.pricingModifier))} for this programme`;
  }
  return "Price impact confirmed with programme selection";
}

export function occupancyLabel(type: RoomOccupancyType): string {
  switch (type) {
    case "single":
      return "Single occupancy";
    case "double":
      return "Double occupancy";
    case "triple":
      return "Triple occupancy";
    case "family":
      return "Family / multi-room";
  }
}

/** Occupancy options valid for current guest count */
export function occupancyOptionsForGuests(
  room: RetreatRoom,
  guests: number,
): RoomOccupancyType[] {
  if (!Number.isFinite(guests) || guests < 1) return room.occupancyTypes;
  return room.occupancyTypes.filter((t) => {
    if (t === "single") return guests === 1;
    if (t === "double") return guests >= 1 && guests <= 2;
    if (t === "triple") return guests >= 1 && guests <= 3;
    if (t === "family") return guests >= 1 && guests <= room.maxGuests;
    return false;
  });
}
