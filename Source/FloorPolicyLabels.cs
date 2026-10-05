using System;

namespace TSA.RoomsAndFlooringMatter
{
    public static class FloorPolicyLabels
    {
        public static string GetLabel(Util_Flooring.FloorPolicy policy) => policy switch
        {
            Util_Flooring.FloorPolicy.Bedroom => "Bedroom (applies to guest rooms too)",
            Util_Flooring.FloorPolicy.Barracks => "Barracks",
            Util_Flooring.FloorPolicy.Workshop => "Workshop",
            Util_Flooring.FloorPolicy.Research => "Research and drug lab",
            Util_Flooring.FloorPolicy.Kitchen => "Kitchen",
            Util_Flooring.FloorPolicy.Seating => "Dining and recreation (seating)",
            Util_Flooring.FloorPolicy.Dining => "Dining (eating speed)",
            Util_Flooring.FloorPolicy.Prison => "Prison (recruiting)",
            Util_Flooring.FloorPolicy.Deathrest => "Deathrest chamber",
            _ => policy.ToString()
        };

        public static string GetDefaultDescription(Util_Flooring.FloorPolicy policy) => policy switch
        {
            Util_Flooring.FloorPolicy.Workshop => "Stone, metal, or steel floors, plus smoothed natural floors",
            Util_Flooring.FloorPolicy.Deathrest => "Stone, metal, or steel floors, plus smoothed natural floors",
            _ => "Any built floor, plus smoothed natural floors"
        };

        public static string GetDefaultSummaryLabel(Util_Flooring.FloorPolicy policy) =>
            $"Default: {GetDefaultDescription(policy)}";
    }
}
