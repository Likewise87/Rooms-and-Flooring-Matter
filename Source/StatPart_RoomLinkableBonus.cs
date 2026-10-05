using System;
using System.Text;
using RimWorld;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
    /// <summary>
    /// Adds additive +%pt room/flooring bonuses to:
    /// - WorkTableWorkSpeedFactor (workshop benches)
    /// - ResearchSpeedFactor (research benches)  [shared with Drug lab settings]
    /// - DrugCookingSpeed (drug lab)              [shared with Research settings]
    /// - CookSpeed (stoves)
    /// - ButcheryFleshSpeed / ButcheryMechanoidSpeed (butcher tables)
    /// Requires the correct room role (Workshop / Laboratory / Kitchen) and full roofing.
    /// </summary>
    public class StatPart_RoomLinkableBonus : StatPart
    {
        private static readonly RoomRoleDef KitchenRole = DefDatabase<RoomRoleDef>.GetNamedSilentFail("Kitchen");
        private static readonly RoomRoleDef LaboratoryRole = DefDatabase<RoomRoleDef>.GetNamedSilentFail("Laboratory");
        private static readonly RoomRoleDef WorkshopRole = DefDatabase<RoomRoleDef>.GetNamedSilentFail("Workshop");

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing || req.Thing.Map == null) return;

            Thing thing = req.Thing;
            Room room = thing.GetRoom();
            if (room == null) return;

            var policyOpt = DetectPolicy(thing, parentStat?.defName);
            if (policyOpt == null) return;

            var policy = policyOpt.Value;

            // Must be the right room type
            if (!IsCorrectRoomForPolicy(room, policy)) return;

            (string _, int needSize, float needCover, float bonus) = GetPolicySettings(policy);
            int size = room.CellCount;
            if (size < needSize) return;

            var scan = RoomFloorCache.Get(room, thing.Map, policy);
            if (!scan.FullyRoofed) return;

            if (scan.Coverage.Percent >= needCover && bonus > 0f)
                val += bonus; // additive (+%pt)
        }

        public override string ExplanationPart(StatRequest req)
        {
            // Always return a string on all paths to avoid CS0161.
            if (!req.HasThing || req.Thing.Map == null)
                return string.Empty;

            Thing thing = req.Thing;
            Room room = thing.GetRoom();
            if (room == null)
                return $"{LabelFor(parentStat?.defName)}: 0%pt (no valid room)";

            var policyOpt = DetectPolicy(thing, parentStat?.defName);
            if (policyOpt == null)
                return string.Empty;

            var policy = policyOpt.Value;

            string roleLabel = room.Role?.LabelCap ?? "(none)";

            if (!IsCorrectRoomForPolicy(room, policy))
                return $"{LabelForStat(policy)}: 0%pt (wrong room type: {roleLabel})";

            (string label, int needSize, float needCover, float bonus) = GetPolicySettings(policy);
            int size = room.CellCount;

            if (size < needSize)
                return $"{label}: 0%pt (too small: {size}/{needSize})";

            var scan = RoomFloorCache.Get(room, thing.Map, policy);
            if (!scan.FullyRoofed)
                return $"{LabelForStat(policy)}: 0%pt (room not fully roofed)";

            bool coverOk = scan.Coverage.Percent >= needCover;

            var sb = new StringBuilder();
            if (coverOk)
            {
                sb.Append($"{label}: +{(int)(bonus * 100f)}%pt ");
                sb.Append($"(size {size} ≥ {needSize}, coverage {Math.Round(scan.Coverage.Percent)}% ≥ {needCover:F0}%)");
            }
            else
            {
                sb.Append($"{label}: 0%pt (insufficient coverage: {Math.Round(scan.Coverage.Percent)}%/{needCover:F0}%)");
            }

            return sb.ToString();
        }

        // --------------------------------------------------------------------

        private static Util_Flooring.FloorPolicy? DetectPolicy(Thing thing, string? statDefName)
        {
            string dn = thing.def.defName ?? string.Empty;

            switch (statDefName)
            {
                case "ResearchSpeedFactor":
                case "DrugCookingSpeed":
                    return Util_Flooring.FloorPolicy.Research; // shared settings block

                case "CookSpeed":
                case "ButcheryFleshSpeed":
                case "ButcheryMechanoidSpeed":
                    return Util_Flooring.FloorPolicy.Kitchen;

                case "WorkTableWorkSpeedFactor":
                    // Heuristic fallback by defName, if needed
                    if (dn.IndexOf("Stove", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        dn.IndexOf("Butcher", StringComparison.OrdinalIgnoreCase) >= 0)
                        return Util_Flooring.FloorPolicy.Kitchen;

                    if (dn.IndexOf("DrugLab", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (dn.IndexOf("Research", StringComparison.OrdinalIgnoreCase) >= 0 &&
                         dn.IndexOf("Bench", StringComparison.OrdinalIgnoreCase) >= 0))
                        return Util_Flooring.FloorPolicy.Research;

                    return Util_Flooring.FloorPolicy.Workshop;
            }

            return null;
        }

        private static bool IsCorrectRoomForPolicy(Room room, Util_Flooring.FloorPolicy policy)
        {
            var role = room.Role;
            if (role == null) return false;

            bool IsRole(RoomRoleDef? target, string fallbackName)
            {
                if (target != null && role == target) return true;
                var defName = role.defName ?? string.Empty;
                return defName.Equals(fallbackName, StringComparison.OrdinalIgnoreCase);
            }

            switch (policy)
            {
                case Util_Flooring.FloorPolicy.Kitchen:
                    return IsRole(KitchenRole, "Kitchen");
                case Util_Flooring.FloorPolicy.Research:
                    return IsRole(LaboratoryRole, "Laboratory");
                case Util_Flooring.FloorPolicy.Workshop:
                    return IsRole(WorkshopRole, "Workshop");
                default:
                    return false;
            }
        }

        private static (string, int, float, float) GetPolicySettings(Util_Flooring.FloorPolicy policy)
        {
            switch (policy)
            {
                case Util_Flooring.FloorPolicy.Workshop:
                    return ("Workshop flooring bonus", ModEntry.Settings.minSizeWorkshop, ModEntry.Settings.coverageThresholdWorkshop, ModEntry.Settings.flooringBonusWorkshop);
                case Util_Flooring.FloorPolicy.Research:
                    return ("Laboratory/Drug lab flooring bonus", ModEntry.Settings.minSizeResearch, ModEntry.Settings.coverageThresholdResearch, ModEntry.Settings.flooringBonusResearch);
                case Util_Flooring.FloorPolicy.Kitchen:
                    return ("Kitchen flooring bonus", ModEntry.Settings.minSizeKitchen, ModEntry.Settings.coverageThresholdKitchen, ModEntry.Settings.flooringBonusKitchen);
                default:
                    return ("Flooring bonus", 0, 0f, 0f);
            }
        }

        private static string LabelFor(string? statDefName)
        {
            switch (statDefName)
            {
                case "WorkTableWorkSpeedFactor": return "Workshop flooring bonus";
                case "ResearchSpeedFactor": return "Laboratory/Drug lab flooring bonus";
                case "DrugCookingSpeed": return "Laboratory/Drug lab flooring bonus";
                case "CookSpeed": return "Kitchen flooring bonus";
                case "ButcheryFleshSpeed": return "Kitchen flooring bonus";
                case "ButcheryMechanoidSpeed": return "Kitchen flooring bonus";
                default: return "Flooring bonus";
            }
        }

        private static string LabelForStat(Util_Flooring.FloorPolicy policy)
        {
            return GetPolicySettings(policy).Item1;
        }
    }
}
