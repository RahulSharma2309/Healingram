import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";

type StartAvailabilityOverrides = {
  programmeId?: string;
  durationNights?: number | null;
};

type StartAvailabilityFlow = (overrides?: StartAvailabilityOverrides) => void;

export type OccupancyPreference = "single" | "double" | "auto";

type SelectRoomInput = {
  roomId: string;
  roomName: string;
  occupancyPreference?: OccupancyPreference;
};

type ListingPlanContextValue = {
  programmeId: string;
  durationNights: number | null;
  checkIn: string;
  flexibleCheckOut: string;
  guests: string;
  guestMode: "preset" | "group";
  groupSeats: string;
  /** Selected accommodation from Component 7 */
  roomId: string;
  roomName: string;
  occupancyPreference: OccupancyPreference;
  setProgrammeId: (id: string) => void;
  setDurationNights: (n: number | null) => void;
  setCheckIn: (v: string) => void;
  setFlexibleCheckOut: (v: string) => void;
  setGuests: (v: string) => void;
  setGuestMode: (v: "preset" | "group") => void;
  setGroupSeats: (v: string) => void;
  selectProgramme: (programmeId: string, durationNights?: number | null) => void;
  selectRoom: (input: SelectRoomInput) => void;
  clearRoom: () => void;
  setOccupancyPreference: (v: OccupancyPreference) => void;
  /** Canonical Check Availability action — opens the shared availability workflow */
  startAvailabilityFlow: StartAvailabilityFlow;
  /** Component 1 registers the real handler (has pricing + modal) */
  registerStartAvailabilityFlow: (fn: StartAvailabilityFlow | null) => void;
  focusPlanPanel: () => void;
  planPanelRef: React.RefObject<HTMLElement | null>;
};

const ListingPlanContext = createContext<ListingPlanContextValue | null>(null);

export function ListingPlanProvider({ children }: { children: ReactNode }) {
  const [programmeId, setProgrammeId] = useState("");
  const [durationNights, setDurationNights] = useState<number | null>(null);
  const [checkIn, setCheckIn] = useState("");
  const [flexibleCheckOut, setFlexibleCheckOut] = useState("");
  const [guests, setGuests] = useState("2");
  const [guestMode, setGuestMode] = useState<"preset" | "group">("preset");
  const [groupSeats, setGroupSeats] = useState("");
  const [roomId, setRoomId] = useState("");
  const [roomName, setRoomName] = useState("");
  const [occupancyPreference, setOccupancyPreference] =
    useState<OccupancyPreference>("auto");
  const planPanelRef = useRef<HTMLElement | null>(null);
  const startFlowRef = useRef<StartAvailabilityFlow | null>(null);

  const selectProgramme = useCallback((id: string, nights: number | null = null) => {
    setProgrammeId(id);
    setDurationNights(nights);
  }, []);

  const selectRoom = useCallback((input: SelectRoomInput) => {
    setRoomId(input.roomId);
    setRoomName(input.roomName);
    if (input.occupancyPreference) {
      setOccupancyPreference(input.occupancyPreference);
    }
  }, []);

  const clearRoom = useCallback(() => {
    setRoomId("");
    setRoomName("");
    setOccupancyPreference("auto");
  }, []);

  const registerStartAvailabilityFlow = useCallback((fn: StartAvailabilityFlow | null) => {
    startFlowRef.current = fn;
  }, []);

  const startAvailabilityFlow = useCallback((overrides?: StartAvailabilityOverrides) => {
    startFlowRef.current?.(overrides);
  }, []);

  const focusPlanPanel = useCallback(() => {
    const preferDesktop =
      typeof window !== "undefined" && window.matchMedia("(min-width: 1024px)").matches;
    const mobile = document.getElementById("plan-your-stay-mobile");
    const el = preferDesktop
      ? planPanelRef.current ?? mobile
      : mobile ?? planPanelRef.current;
    if (!el) return;
    el.scrollIntoView({ behavior: "smooth", block: "center" });
    el.setAttribute("tabindex", "-1");
    (el as HTMLElement).focus({ preventScroll: true });
  }, []);

  const value = useMemo(
    () => ({
      programmeId,
      durationNights,
      checkIn,
      flexibleCheckOut,
      guests,
      guestMode,
      groupSeats,
      roomId,
      roomName,
      occupancyPreference,
      setProgrammeId,
      setDurationNights,
      setCheckIn,
      setFlexibleCheckOut,
      setGuests,
      setGuestMode,
      setGroupSeats,
      selectProgramme,
      selectRoom,
      clearRoom,
      setOccupancyPreference,
      startAvailabilityFlow,
      registerStartAvailabilityFlow,
      focusPlanPanel,
      planPanelRef,
    }),
    [
      programmeId,
      durationNights,
      checkIn,
      flexibleCheckOut,
      guests,
      guestMode,
      groupSeats,
      roomId,
      roomName,
      occupancyPreference,
      selectProgramme,
      selectRoom,
      clearRoom,
      startAvailabilityFlow,
      registerStartAvailabilityFlow,
      focusPlanPanel,
    ],
  );

  return (
    <ListingPlanContext.Provider value={value}>{children}</ListingPlanContext.Provider>
  );
}

export function useListingPlan(): ListingPlanContextValue {
  const ctx = useContext(ListingPlanContext);
  if (!ctx) {
    throw new Error("useListingPlan must be used within ListingPlanProvider");
  }
  return ctx;
}

/** Optional — Component 1 can fall back to local state if provider missing */
export function useListingPlanOptional(): ListingPlanContextValue | null {
  return useContext(ListingPlanContext);
}
