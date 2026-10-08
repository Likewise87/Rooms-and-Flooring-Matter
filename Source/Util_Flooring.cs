using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
    public static class Util_Flooring
    {
        public enum FloorPolicy
        {
            Bedroom,  // Bedroom + Hospitality guest rooms
            Barracks,
            Workshop,
            Research, // Research benches + Drug lab (shared)
            Kitchen,
            Seating,  // Dining/Rec seating comfort
            Dining,   // DiningRoom eating speed
            Prison,   // PrisonCell / PrisonBarracks recruit resistance
            Deathrest, // Biotech DeathrestChamber
            Dormitory // Dormitories mod soft-compat (appended to preserve override ordinals)
        }

        private static HashSet<TerrainDef>[]? goodByPolicy;

        public static void RebuildGoodFloorCache()
        {
            var policies = (FloorPolicy[])Enum.GetValues(typeof(FloorPolicy));
            goodByPolicy = new HashSet<TerrainDef>[policies.Length];
            for (int i = 0; i < policies.Length; i++)
            {
                var set = new HashSet<TerrainDef>();
                var policy = policies[i];
                foreach (var terrain in DefDatabase<TerrainDef>.AllDefsListForReading)
                {
                    if (IsGoodFloorForPolicy(terrain, policy))
                        set.Add(terrain);
                }
                goodByPolicy[i] = set;
            }
        }

        public static bool IsGoodFloor(TerrainDef terrain, FloorPolicy policy)
        {
            if (terrain == null) return false;
            if (goodByPolicy == null)
                return IsGoodFloorForPolicy(terrain, policy);
            return goodByPolicy[(int)policy].Contains(terrain);
        }

        /// <summary>Built-in rule before any user override.</summary>
        public static bool IsDefaultGoodFloor(TerrainDef terrain, FloorPolicy policy) =>
            IsGoodFloorUncached(terrain, policy);

        public static HashSet<string> GetDefaultDefNameSnapshot(FloorPolicy policy)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var terrain in GetFloorCandidates())
            {
                if (IsGoodFloorUncached(terrain, policy))
                    set.Add(terrain.defName);
            }
            return set;
        }

        public static IEnumerable<TerrainDef> GetFloorCandidates()
        {
            foreach (var terrain in DefDatabase<TerrainDef>.AllDefsListForReading)
            {
                if (IsFloorCandidate(terrain))
                    yield return terrain;
            }
        }

        public static bool IsFloorCandidate(TerrainDef terrain)
        {
            if (terrain == null) return false;
            if (terrain.layerable) return true;
            if (terrain.costList != null && terrain.costList.Count > 0) return true;
            var dn = terrain.defName ?? string.Empty;
            if (dn.IndexOf("smooth", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }

        public static bool SetsEqual(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var x in a)
                if (!b.Contains(x)) return false;
            return true;
        }

        private static bool IsGoodFloorForPolicy(TerrainDef terrain, FloorPolicy policy)
        {
            var settings = ModEntry.Settings;
            if (settings != null && settings.IsFloorPolicyCustomized(policy))
                return settings.ContainsFloorOverrideDef(terrain, policy);
            return IsGoodFloorUncached(terrain, policy);
        }

        private static bool IsGoodFloorUncached(TerrainDef terrain, FloorPolicy policy)
        {
            if (terrain == null) return false;

            var dn = terrain.defName ?? string.Empty;
            if (dn.IndexOf("smooth", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (terrain.costList == null)
                return false;

            if (policy == FloorPolicy.Workshop || policy == FloorPolicy.Deathrest)
                return HasAnyStuffCategory(terrain, StuffCategoryDefOf.Stony, StuffCategoryDefOf.Metallic)
                       || ContainsSteel(terrain);

            return true;
        }

        private static bool HasAnyStuffCategory(TerrainDef def, StuffCategoryDef catA, StuffCategoryDef catB)
        {
            if (def.costList == null) return false;
            foreach (var cost in def.costList)
            {
                var sc = cost.thingDef?.stuffProps?.categories;
                if (sc == null) continue;
                if (sc.Contains(catA) || sc.Contains(catB)) return true;
            }
            return false;
        }

        private static bool ContainsSteel(TerrainDef def)
        {
            if (def.costList == null) return false;
            foreach (var cost in def.costList)
            {
                if (cost.thingDef == ThingDefOf.Steel) return true;
                var sc = cost.thingDef?.stuffProps?.categories;
                if (sc != null && sc.Contains(StuffCategoryDefOf.Metallic)) return true;
            }
            return false;
        }
    }
}
