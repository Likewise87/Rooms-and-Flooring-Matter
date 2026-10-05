using RimWorld;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
    /// <summary>Soft compatibility with Hospitality (Continued) guest rooms — no Hospitality.dll reference.</summary>
    public static class HospitalityCompat
    {
        public const string HospitalityPackageId = "Orion.Hospitality";
        public const string GuestRoomRoleDefName = "GuestRoom";

        private static RoomRoleDef? guestRoomDef;

        public static RoomRoleDef? GuestRoomDef
        {
            get
            {
                if (!ModsConfig.IsActive(HospitalityPackageId))
                    return null;
                return guestRoomDef ??= DefDatabase<RoomRoleDef>.GetNamedSilentFail(GuestRoomRoleDefName);
            }
        }

        public static bool IsGuestRoom(Room? room) =>
            room?.Role != null && GuestRoomDef != null && room.Role == GuestRoomDef;

        public static bool IsBedroomLike(Room? room) =>
            room?.Role == RoomRoleDefOf.Bedroom || IsGuestRoom(room);
    }
}
