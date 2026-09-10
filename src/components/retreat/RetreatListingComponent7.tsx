import { useEffect, useId, useMemo, useState } from "react";
import { Check, Images, X } from "lucide-react";
import {
  getRetreatRooms,
  hasRetreatRooms,
  isRoomCompatibleWithProgramme,
  occupancyLabel,
  occupancyOptionsForGuests,
  programmeLabel,
  roomFitsGuestCount,
  roomPriceImpactLabel,
  sortRoomsForPlan,
  type RetreatRoom,
  type RoomOccupancyType,
} from "../../data/retreatRooms";
import { useListingPlan } from "../../lib/listingPlanContext";

type Props = {
  retreatId: string;
};

export function RetreatListingComponent7({ retreatId }: Props) {
  const plan = useListingPlan();
  const rooms = useMemo(() => getRetreatRooms(retreatId), [retreatId]);
  const {
    programmeId,
    roomId,
    guests,
    guestMode,
    groupSeats,
    selectRoom,
    clearRoom,
    focusPlanPanel,
  } = plan;

  const guestCount =
    guestMode === "group"
      ? Math.floor(Number(groupSeats)) || 0
      : Math.floor(Number(guests)) || 1;

  const sorted = useMemo(
    () => sortRoomsForPlan(rooms, programmeId || null),
    [rooms, programmeId],
  );

  const [detailId, setDetailId] = useState<string | null>(null);
  const [galleryOpen, setGalleryOpen] = useState(false);
  const detail = rooms.find((r) => r.roomId === detailId) ?? null;

  useEffect(() => {
    if (!hasRetreatRooms(retreatId) && import.meta.env.DEV) {
      console.warn(
        `[Healingram] Component 7: no verified rooms for retreat "${retreatId}". Section omitted — do not invent accommodation.`,
      );
    }
  }, [retreatId]);

  // Clear invalid room when programme or guests change
  useEffect(() => {
    if (!roomId) return;
    const room = rooms.find((r) => r.roomId === roomId);
    if (!room) {
      clearRoom();
      return;
    }
    if (!isRoomCompatibleWithProgramme(room, programmeId || null)) {
      clearRoom();
      return;
    }
    if (!roomFitsGuestCount(room, guestCount)) {
      clearRoom();
    }
  }, [roomId, programmeId, guestCount, rooms, clearRoom]);

  if (rooms.length === 0) {
    if (import.meta.env.DEV) {
      return (
        <section
          id="rooms-accommodation"
          className="relative z-0 mt-14 pt-12 border-t border-sand-200"
        >
          <p className="text-sm text-sage-500">Accommodation details being added.</p>
        </section>
      );
    }
    return null;
  }

  const chooseRoom = (room: RetreatRoom, occupancy?: RoomOccupancyType) => {
    if (!isRoomCompatibleWithProgramme(room, programmeId || null)) return;
    if (!roomFitsGuestCount(room, guestCount)) return;

    const options = occupancyOptionsForGuests(room, guestCount);
    const preferred =
      occupancy && options.includes(occupancy)
        ? occupancy
        : guestCount === 1 && options.includes("single")
          ? "single"
          : options.includes("double")
            ? "double"
            : options[0] ?? null;

    selectRoom({
      roomId: room.roomId,
      roomName: room.roomName,
      occupancyPreference: preferred === "single" || preferred === "double" ? preferred : "auto",
    });
    focusPlanPanel();
  };

  return (
    <section
      id="rooms-accommodation"
      className="relative z-0 mt-14 pt-12 border-t border-sand-200"
      aria-labelledby="listing-c7-heading"
    >
      <h2
        id="listing-c7-heading"
        className="font-display text-2xl md:text-3xl font-semibold text-sage-800 text-balance"
      >
        Rooms &amp; accommodation
      </h2>
      <p className="mt-2 text-sm md:text-base text-sage-600 max-w-2xl">
        See where you’ll stay and choose the accommodation that fits your programme.
      </p>

      <div className="mt-8 grid gap-6 sm:grid-cols-2 xl:grid-cols-3">
        {sorted.map((room) => {
          const compatible = isRoomCompatibleWithProgramme(room, programmeId || null);
          const fitsGuests = roomFitsGuestCount(room, guestCount);
          const selectable = compatible && fitsGuests;
          const selected = roomId === room.roomId;
          const primary = room.images[0];

          return (
            <article
              key={room.roomId}
              className={`flex flex-col overflow-hidden rounded-2xl border bg-white transition ${
                selected
                  ? "border-teal-600/50 ring-1 ring-teal-600/20"
                  : "border-sand-200"
              } ${!selectable ? "opacity-75" : ""}`}
            >
              <div className="relative aspect-[4/3] bg-sand-100 overflow-hidden">
                {primary && (
                  <img
                    src={primary.src}
                    alt={
                      primary.temporary
                        ? `${room.roomName} — temporary placeholder`
                        : primary.alt
                    }
                    className="h-full w-full object-cover"
                  />
                )}
                {selected && (
                  <span className="absolute top-3 left-3 inline-flex items-center gap-1 rounded-lg bg-white/95 px-2.5 py-1 text-xs font-semibold text-teal-800 shadow-sm">
                    <Check className="w-3.5 h-3.5" />
                    Selected
                  </span>
                )}
              </div>

              <div className="flex flex-1 flex-col p-5">
                <h3 className="font-display text-lg font-semibold text-sage-800 text-balance">
                  {room.roomName}
                </h3>
                <p className="mt-1 text-sm font-medium text-sage-700">
                  Up to {room.maxGuests} guest{room.maxGuests === 1 ? "" : "s"}
                </p>
                <p className="mt-2 text-sm text-sage-600 leading-relaxed">
                  {room.shortDescription}
                </p>

                {room.amenities.length > 0 && (
                  <ul className="mt-4 space-y-1.5">
                    {room.amenities.slice(0, 4).map((a) => (
                      <li key={a} className="text-sm text-sage-700 flex gap-2">
                        <span className="text-teal-700 mt-0.5" aria-hidden>
                          ·
                        </span>
                        <span>{a}</span>
                      </li>
                    ))}
                  </ul>
                )}

                <div className="mt-4 space-y-2">
                  {programmeId && !compatible ? (
                    <p className="text-sm text-sage-600">
                      Not available with your selected programme
                    </p>
                  ) : room.compatibleProgrammeIds.length > 0 ? (
                    <div>
                      <p className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                        Available with
                      </p>
                      <p className="mt-1 text-sm text-sage-700">
                        {room.compatibleProgrammeIds
                          .slice(0, 4)
                          .map((id) => programmeLabel(id))
                          .join(" · ")}
                        {room.compatibleProgrammeIds.length > 4 ? " · …" : ""}
                      </p>
                    </div>
                  ) : (
                    <p className="text-sm text-sage-600">
                      Available with selected wellness programmes
                    </p>
                  )}

                  {!fitsGuests && guestCount > 0 && (
                    <p className="text-sm text-sage-600">
                      Fits up to {room.maxGuests} guests — adjust guest count to select
                    </p>
                  )}

                  <p className="text-sm text-sage-700">{roomPriceImpactLabel(room)}</p>
                </div>

                <div className="mt-5 flex flex-col gap-2 mt-auto pt-2">
                  <button
                    type="button"
                    onClick={() => {
                      setDetailId(room.roomId);
                      setGalleryOpen(false);
                    }}
                    className="w-full rounded-xl border border-sand-200 py-2.5 text-sm font-semibold text-sage-800 hover:bg-sand-50 transition"
                  >
                    View room
                  </button>
                  <button
                    type="button"
                    disabled={!selectable}
                    onClick={() => chooseRoom(room)}
                    className="w-full rounded-xl bg-teal-600 py-2.5 text-sm font-semibold text-white hover:bg-teal-500 transition disabled:opacity-45 disabled:cursor-not-allowed"
                  >
                    {selected ? "Selected for your stay" : "Choose this room"}
                  </button>
                </div>
              </div>
            </article>
          );
        })}
      </div>

      {detail && (
        <RoomDetailDrawer
          room={detail}
          programmeId={programmeId || null}
          guestCount={guestCount}
          selected={roomId === detail.roomId}
          galleryOpen={galleryOpen}
          onOpenGallery={() => setGalleryOpen(true)}
          onCloseGallery={() => setGalleryOpen(false)}
          onClose={() => {
            setDetailId(null);
            setGalleryOpen(false);
          }}
          onChoose={(occ) => {
            chooseRoom(detail, occ);
            setDetailId(null);
            setGalleryOpen(false);
          }}
        />
      )}
    </section>
  );
}

function RoomDetailDrawer({
  room,
  programmeId,
  guestCount,
  selected,
  galleryOpen,
  onOpenGallery,
  onCloseGallery,
  onClose,
  onChoose,
}: {
  room: RetreatRoom;
  programmeId: string | null;
  guestCount: number;
  selected: boolean;
  galleryOpen: boolean;
  onOpenGallery: () => void;
  onCloseGallery: () => void;
  onClose: () => void;
  onChoose: (occupancy?: RoomOccupancyType) => void;
}) {
  const titleId = useId();
  const compatible = isRoomCompatibleWithProgramme(room, programmeId);
  const fitsGuests = roomFitsGuestCount(room, guestCount);
  const selectable = compatible && fitsGuests;
  const occOptions = occupancyOptionsForGuests(room, guestCount);
  const [occ, setOcc] = useState<RoomOccupancyType | undefined>(() => occOptions[0]);
  const [photoIndex, setPhotoIndex] = useState(0);

  useEffect(() => {
    const next = occupancyOptionsForGuests(room, guestCount);
    setOcc(next[0]);
    setPhotoIndex(0);
  }, [room, guestCount]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        if (galleryOpen) onCloseGallery();
        else onClose();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose, onCloseGallery, galleryOpen]);

  const primary = room.images[photoIndex] ?? room.images[0];

  return (
    <div className="fixed inset-0 z-50 flex justify-end">
      <button
        type="button"
        className="absolute inset-0 bg-black/40"
        aria-label="Close room details"
        onClick={onClose}
      />
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="relative h-full w-full max-w-lg overflow-y-auto bg-white shadow-xl border-l border-sand-200"
      >
        <div className="sticky top-0 z-10 flex items-start justify-between gap-3 border-b border-sand-100 bg-white px-5 py-4">
          <div>
            <h3
              id={titleId}
              className="font-display text-xl font-semibold text-sage-800 text-balance"
            >
              {room.roomName}
            </h3>
            <p className="mt-1 text-sm text-sage-600">
              Up to {room.maxGuests} guest{room.maxGuests === 1 ? "" : "s"}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-2 text-sage-500 hover:bg-sand-100"
            aria-label="Close"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="px-5 py-6 space-y-6">
          {primary && (
            <div className="relative overflow-hidden rounded-2xl bg-sand-100 aspect-[4/3]">
              <img
                src={primary.src}
                alt={
                  primary.temporary
                    ? `${room.roomName} — temporary placeholder`
                    : primary.alt
                }
                className="h-full w-full object-cover"
              />
            </div>
          )}

          {room.images.length > 1 && (
            <button
              type="button"
              onClick={onOpenGallery}
              className="inline-flex items-center gap-2 text-sm font-semibold text-teal-700 hover:text-teal-800"
            >
              <Images className="w-4 h-4" />
              View all room photos
            </button>
          )}

          <p className="text-sm text-sage-700 leading-relaxed">{room.longDescription}</p>

          <dl className="grid gap-3 text-sm">
            {room.bedConfiguration && (
              <div>
                <dt className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                  Bed setup
                </dt>
                <dd className="mt-1 text-sage-800">{room.bedConfiguration}</dd>
              </div>
            )}
            {room.bathroomType && (
              <div>
                <dt className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                  Bathroom
                </dt>
                <dd className="mt-1 text-sage-800">{room.bathroomType}</dd>
              </div>
            )}
            {room.roomSize && (
              <div>
                <dt className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                  Room size
                </dt>
                <dd className="mt-1 text-sage-800">{room.roomSize}</dd>
              </div>
            )}
            {room.airConditioning != null && (
              <div>
                <dt className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                  Air conditioning
                </dt>
                <dd className="mt-1 text-sage-800">
                  {room.airConditioning ? "Available" : "Not provided"}
                </dd>
              </div>
            )}
            {room.view && (
              <div>
                <dt className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                  View / setting
                </dt>
                <dd className="mt-1 text-sage-800">{room.view}</dd>
              </div>
            )}
            <div>
              <dt className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                Occupancy
              </dt>
              <dd className="mt-1 text-sage-800">
                {room.occupancyTypes.map(occupancyLabel).join(" · ")} · max {room.maxGuests}{" "}
                guests
              </dd>
            </div>
          </dl>

          {room.amenities.length > 0 && (
            <section>
              <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                Amenities
              </h4>
              <ul className="mt-2 space-y-1.5">
                {room.amenities.map((a) => (
                  <li key={a} className="text-sm text-sage-700">
                    {a}
                  </li>
                ))}
              </ul>
            </section>
          )}

          {room.compatibleProgrammeIds.length > 0 && (
            <section>
              <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                Programme compatibility
              </h4>
              <ul className="mt-2 flex flex-wrap gap-1.5">
                {room.compatibleProgrammeIds.map((id) => (
                  <li
                    key={id}
                    className="px-2.5 py-1 rounded-md bg-sand-100 text-sage-700 text-xs font-medium"
                  >
                    {programmeLabel(id)}
                  </li>
                ))}
              </ul>
            </section>
          )}

          <p className="text-sm font-medium text-sage-800">{roomPriceImpactLabel(room)}</p>

          {room.goodToKnow.length > 0 && (
            <section className="rounded-xl bg-sand-50 px-4 py-4">
              <h4 className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                Good to know
              </h4>
              <ul className="mt-2 space-y-1.5">
                {room.goodToKnow.map((g) => (
                  <li key={g} className="text-sm text-sage-700 leading-relaxed">
                    {g}
                  </li>
                ))}
              </ul>
            </section>
          )}

          {occOptions.length > 1 && selectable && (
            <fieldset>
              <legend className="text-[11px] font-semibold uppercase tracking-wide text-sage-500">
                Occupancy for your stay
              </legend>
              <div className="mt-2 flex flex-wrap gap-2">
                {occOptions.map((o) => (
                  <button
                    key={o}
                    type="button"
                    onClick={() => setOcc(o)}
                    className={`rounded-xl border px-3 py-2 text-sm transition ${
                      occ === o
                        ? "border-teal-600/50 bg-teal-50 text-sage-800 ring-1 ring-teal-600/20"
                        : "border-sand-200 bg-white text-sage-700 hover:bg-sand-50"
                    }`}
                  >
                    {occupancyLabel(o)}
                  </button>
                ))}
              </div>
            </fieldset>
          )}

          {!compatible && programmeId && (
            <p className="text-sm text-sage-600">
              Not available with your selected programme
            </p>
          )}
          {!fitsGuests && (
            <p className="text-sm text-sage-600">
              Fits up to {room.maxGuests} guests — adjust guest count to select
            </p>
          )}

          <button
            type="button"
            disabled={!selectable}
            onClick={() => onChoose(occ)}
            className="w-full rounded-xl bg-teal-600 py-3 text-sm font-semibold text-white hover:bg-teal-500 transition disabled:opacity-45 disabled:cursor-not-allowed"
          >
            {selected ? "Selected for your stay" : "Choose this room"}
          </button>
        </div>
      </div>

      {galleryOpen && (
        <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/80 p-4">
          <button
            type="button"
            className="absolute inset-0"
            aria-label="Close gallery"
            onClick={onCloseGallery}
          />
          <div className="relative z-10 w-full max-w-3xl">
            <img
              src={(room.images[photoIndex] ?? room.images[0]).src}
              alt={
                (room.images[photoIndex] ?? room.images[0]).temporary
                  ? `${room.roomName} — temporary placeholder`
                  : (room.images[photoIndex] ?? room.images[0]).alt
              }
              className="w-full max-h-[80vh] object-contain rounded-lg"
            />
            <div className="mt-4 flex items-center justify-between gap-3">
              <button
                type="button"
                className="rounded-lg bg-white/95 px-3 py-2 text-sm font-medium text-sage-800 disabled:opacity-40"
                disabled={photoIndex <= 0}
                onClick={() => setPhotoIndex((i) => Math.max(0, i - 1))}
              >
                Previous
              </button>
              <p className="text-sm text-white/90">
                {photoIndex + 1} / {room.images.length}
              </p>
              <button
                type="button"
                className="rounded-lg bg-white/95 px-3 py-2 text-sm font-medium text-sage-800 disabled:opacity-40"
                disabled={photoIndex >= room.images.length - 1}
                onClick={() =>
                  setPhotoIndex((i) => Math.min(room.images.length - 1, i + 1))
                }
              >
                Next
              </button>
            </div>
            <button
              type="button"
              onClick={onCloseGallery}
              className="absolute -top-2 -right-2 rounded-full bg-white p-2 text-sage-700 shadow"
              aria-label="Close gallery"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
