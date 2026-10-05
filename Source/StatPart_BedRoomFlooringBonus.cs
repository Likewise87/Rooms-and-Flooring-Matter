using System;
using System.Text;
using RimWorld;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
    public class StatPart_BedRoomFlooringBonus : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing) return;
            if (req.Thing is not Building_Bed bed) return;

            var map = bed.Map;
            var room = bed.GetRoom();
            if (map == null || room == null || room.Role == null) return;

            var s = ModEntry.Settings;
            int size = room.CellCount;
            float add = 0f;

            if (HospitalityCompat.IsBedroomLike(room))
            {
                if (size < s.minSizeBedroom) return;

                var scan = RoomFloorCache.Get(room, map, Util_Flooring.FloorPolicy.Bedroom);
                if (!scan.FullyRoofed) return;

                add += s.baseBonusBedroom;
                if (scan.Coverage.Percent >= s.coverageThresholdBedroom)
                    add += s.flooringBonusBedroom;
            }
            else if (room.Role == RoomRoleDefOf.Barracks)
            {
                if (size < s.minSizeBarracks) return;

                var scan = RoomFloorCache.Get(room, map, Util_Flooring.FloorPolicy.Barracks);
                if (!scan.FullyRoofed) return;

                add += s.baseBonusBarracks;
                if (scan.Coverage.Percent >= s.coverageThresholdBarracks)
                    add += s.flooringBonusBarracks;
            }
            else
            {
                return;
            }

            if (add > 0f)
                val += add;
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!req.HasThing) return null;
            if (req.Thing is not Building_Bed bed) return null;

            var map = bed.Map;
            var room = bed.GetRoom();
            if (map == null || room == null || room.Role == null)
                return "Bedroom/Barracks bonus: 0%pt (no valid room)";

            if (!HospitalityCompat.IsBedroomLike(room) && room.Role != RoomRoleDefOf.Barracks)
                return "Bedroom/Barracks bonus: 0%pt (room is neither Bedroom, Guest room, nor Barracks)";

            var s = ModEntry.Settings;
            int size = room.CellCount;
            var sb = new StringBuilder();

            if (HospitalityCompat.IsBedroomLike(room))
            {
                string roomLabel = HospitalityCompat.IsGuestRoom(room) ? "Guest room" : "Bedroom";
                bool sizeOk = size >= s.minSizeBedroom;
                if (!sizeOk)
                {
                    sb.AppendLine($"Base {roomLabel.ToLower()} bonus: 0%pt (too small: {size}/{s.minSizeBedroom})");
                    sb.AppendLine($"{roomLabel} flooring bonus: 0%pt (room too small)");
                    sb.AppendLine("Total: +0%pt");
                    return sb.ToString().TrimEnd();
                }

                var scan = RoomFloorCache.Get(room, map, Util_Flooring.FloorPolicy.Bedroom);
                if (!scan.FullyRoofed)
                    return "Bedroom/Barracks bonus: 0%pt (room not fully roofed)";

                bool covOk = scan.Coverage.Percent >= s.coverageThresholdBedroom;

                sb.AppendLine($"Base {roomLabel.ToLower()} bonus: +{(int)(s.baseBonusBedroom * 100f)}%pt");
                sb.AppendLine(covOk
                    ? $"{roomLabel} flooring bonus: +{(int)(s.flooringBonusBedroom * 100f)}%pt (coverage {Math.Round(scan.Coverage.Percent)}% ≥ {s.coverageThresholdBedroom:F0}%)"
                    : $"{roomLabel} flooring bonus: 0%pt (insufficient coverage: {Math.Round(scan.Coverage.Percent)}%/{s.coverageThresholdBedroom:F0}%)");

                float total = s.baseBonusBedroom;
                if (covOk) total += s.flooringBonusBedroom;
                sb.AppendLine($"Total: +{(int)(total * 100f)}%pt");
            }
            else
            {
                bool sizeOk = size >= s.minSizeBarracks;
                if (!sizeOk)
                {
                    sb.AppendLine($"Base barracks bonus: 0%pt (too small: {size}/{s.minSizeBarracks})");
                    sb.AppendLine("Barracks flooring bonus: 0%pt (room too small)");
                    sb.AppendLine("Total: +0%pt");
                    return sb.ToString().TrimEnd();
                }

                var scan = RoomFloorCache.Get(room, map, Util_Flooring.FloorPolicy.Barracks);
                if (!scan.FullyRoofed)
                    return "Bedroom/Barracks bonus: 0%pt (room not fully roofed)";

                bool covOk = scan.Coverage.Percent >= s.coverageThresholdBarracks;

                sb.AppendLine($"Base barracks bonus: +{(int)(s.baseBonusBarracks * 100f)}%pt");
                sb.AppendLine(covOk
                    ? $"Barracks flooring bonus: +{(int)(s.flooringBonusBarracks * 100f)}%pt (coverage {Math.Round(scan.Coverage.Percent)}% ≥ {s.coverageThresholdBarracks:F0}%)"
                    : $"Barracks flooring bonus: 0%pt (insufficient coverage: {Math.Round(scan.Coverage.Percent)}%/{s.coverageThresholdBarracks:F0}%)");

                float total = s.baseBonusBarracks;
                if (covOk) total += s.flooringBonusBarracks;
                sb.AppendLine($"Total: +{(int)(total * 100f)}%pt");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
