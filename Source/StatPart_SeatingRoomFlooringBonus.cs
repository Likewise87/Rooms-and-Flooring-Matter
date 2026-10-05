using System;
using RimWorld;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
    /// <summary>
    /// Adds additive Comfort bonus (+%pt) to seating (chairs/sofas) when in DiningRoom or RecRoom,
    /// room is fully roofed, and meets size/coverage thresholds (any human-made floor).
    /// Always shows an explanation line (via parent stat’s report).
    /// </summary>
    public class StatPart_SeatingRoomFlooringBonus : StatPart
    {
        private static readonly RoomRoleDef DiningRole = DefDatabase<RoomRoleDef>.GetNamedSilentFail("DiningRoom");
        private static readonly RoomRoleDef RecRole = DefDatabase<RoomRoleDef>.GetNamedSilentFail("RecRoom");

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (parentStat == null || parentStat.defName != "Comfort") return;
            if (!req.HasThing) return;

            var thing = req.Thing;
            if (!IsSittableFurniture(thing)) return;

            var room = thing.GetRoom();
            var map = thing.Map;
            if (room == null || map == null) return;

            if (!IsDiningOrRec(room)) return;

            var s = ModEntry.Settings;
            int size = room.CellCount;
            if (size < s.minSizeSeating) return;

            var scan = RoomFloorCache.Get(room, map, Util_Flooring.FloorPolicy.Seating);
            if (!scan.FullyRoofed) return;

            if (scan.Coverage.Percent >= s.coverageThresholdSeating)
                val += s.flooringBonusSeating; // additive (+%pt)
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (parentStat == null || parentStat.defName != "Comfort") return null;
            if (!req.HasThing) return null;

            var thing = req.Thing;
            if (!IsSittableFurniture(thing)) return null;

            var room = thing.GetRoom();
            var map = thing.Map;
            if (room == null || map == null) return "Comfort bonus: 0%pt (no valid room)";

            if (!IsDiningOrRec(room))
                return "Comfort bonus: 0%pt (room is neither Dining nor Recreation)";

            var s = ModEntry.Settings;
            int size = room.CellCount;
            if (size < s.minSizeSeating)
                return $"Comfort bonus: 0%pt (too small: {size}/{s.minSizeSeating})";

            var scan = RoomFloorCache.Get(room, map, Util_Flooring.FloorPolicy.Seating);
            if (!scan.FullyRoofed)
                return "Comfort bonus: 0%pt (room not fully roofed)";

            bool coverOk = scan.Coverage.Percent >= s.coverageThresholdSeating;
            if (coverOk)
            {
                return $"Comfort bonus: +{(int)(s.flooringBonusSeating * 100f)}%pt (size {size} ≥ {s.minSizeSeating}, coverage {Math.Round(scan.Coverage.Percent)}% ≥ {s.coverageThresholdSeating:F0}%)";
            }

            return $"Comfort bonus: 0%pt (insufficient coverage: {Math.Round(scan.Coverage.Percent)}%/{s.coverageThresholdSeating:F0}%)";
        }

        private static bool IsSittableFurniture(Thing t)
        {
            if (t is Building_Bed) return false;
            var props = t.def?.building;
            return props != null && props.isSittable; // chairs, stools, armchairs, sofas, benches
        }

        private static bool IsDiningOrRec(Room room)
        {
            var role = room.Role;
            if (role == null) return false;

            if (DiningRole != null && role == DiningRole) return true;
            if (RecRole != null && role == RecRole) return true;

            var name = role.defName ?? string.Empty;
            return name.Equals("DiningRoom", StringComparison.OrdinalIgnoreCase)
                || name.Equals("RecRoom", StringComparison.OrdinalIgnoreCase);
        }
    }
}
