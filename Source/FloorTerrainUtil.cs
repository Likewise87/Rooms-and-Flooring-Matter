using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
    public enum FloorMaterialKind
    {
        Wood,
        Stone,
        Metal,
        Fabrics,
        Smoothed,
        Other
    }

    public static class FloorTerrainUtil
    {
        private static HashSet<TerrainDef>? playerBuildableTerrains;

        /// <summary>Player can place or produce this terrain (architect build designators or smooth order).</summary>
        public static bool IsPlayerBuildable(TerrainDef terrain)
        {
            if (terrain == null) return false;
            EnsurePlayerBuildableCache();
            return playerBuildableTerrains!.Contains(terrain);
        }

        public static bool IsBuildableFloor(TerrainDef terrain) => IsPlayerBuildable(terrain);

        public static float? GetMarketValue(TerrainDef terrain)
        {
            if (terrain == null) return null;
            if (!terrain.StatBaseDefined(StatDefOf.MarketValue) && StatDefOf.MarketValue.showIfUndefined == false)
                return null;
            return terrain.GetStatValueAbstract(StatDefOf.MarketValue);
        }

        public static string FormatMarketValue(TerrainDef terrain)
        {
            var value = GetMarketValue(terrain);
            if (!value.HasValue) return "—";
            return value.Value.ToStringMoney(null);
        }

        public static string GetMaterialLabel(FloorMaterialKind kind) => kind switch
        {
            FloorMaterialKind.Wood => "Wood",
            FloorMaterialKind.Stone => "Stone",
            FloorMaterialKind.Metal => "Metal",
            FloorMaterialKind.Fabrics => "Fabrics",
            FloorMaterialKind.Smoothed => "Smoothed",
            _ => "Other"
        };

        public static FloorMaterialKind GetMaterialKind(TerrainDef terrain)
        {
            if (terrain == null) return FloorMaterialKind.Other;
            var dn = terrain.defName ?? string.Empty;
            if (dn.IndexOf("smooth", StringComparison.OrdinalIgnoreCase) >= 0)
                return FloorMaterialKind.Smoothed;
            if (HasStuffCategory(terrain, StuffCategoryDefOf.Woody))
                return FloorMaterialKind.Wood;
            if (HasStuffCategory(terrain, StuffCategoryDefOf.Stony))
                return FloorMaterialKind.Stone;
            if (HasStuffCategory(terrain, StuffCategoryDefOf.Metallic) || ContainsSteel(terrain))
                return FloorMaterialKind.Metal;
            if (HasStuffCategory(terrain, StuffCategoryDefOf.Fabric))
                return FloorMaterialKind.Fabrics;
            return FloorMaterialKind.Other;
        }

        /// <summary>
        /// Groups stone-tile / carpet / metal-tile siblings (shared designatorDropdown)
        /// and all smoothed floors into one picker row.
        /// </summary>
        public static string GetPickerGroupKey(TerrainDef terrain)
        {
            if (terrain == null) return string.Empty;
            if (terrain.designatorDropdown != null)
                return "dd:" + terrain.designatorDropdown.defName;
            if (GetMaterialKind(terrain) == FloorMaterialKind.Smoothed)
                return "mat:Smoothed";
            return "td:" + terrain.defName;
        }

        public static string GetPickerGroupLabel(TerrainDef representative, IReadOnlyList<TerrainDef> members)
        {
            if (representative?.designatorDropdown != null)
            {
                string label = representative.designatorDropdown.LabelCap;
                if (!label.NullOrEmpty())
                    return label;
            }

            if (members != null && members.Count > 1 && GetMaterialKind(representative) == FloorMaterialKind.Smoothed)
                return "Smoothed";

            return representative?.LabelCap ?? string.Empty;
        }

        public static void DrawFloorIcon(Rect rect, TerrainDef terrain)
        {
            if (terrain == null) return;
            if (Widgets.CanDrawIconFor(terrain))
            {
                Widgets.DefIcon(rect.ContractedBy(2f), terrain, GetIconStuff(terrain), 0.85f);
                return;
            }

            Widgets.DrawBoxSolid(rect, terrain.DrawColor);
            Widgets.DrawBox(rect);
        }

        private static ThingDef? GetIconStuff(TerrainDef terrain)
        {
            if (terrain.costList == null || terrain.costList.Count == 0) return null;
            ThingDef cost = terrain.costList[0].thingDef;
            return cost?.IsStuff == true ? cost : null;
        }

        private static void EnsurePlayerBuildableCache()
        {
            if (playerBuildableTerrains != null) return;
            playerBuildableTerrains = new HashSet<TerrainDef>();

            foreach (var cat in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
            {
                var designators = cat.AllResolvedDesignators;
                if (designators == null) continue;
                for (int i = 0; i < designators.Count; i++)
                    CollectPlayerBuildableFromDesignator(designators[i], playerBuildableTerrains);
            }

            foreach (var terrain in DefDatabase<TerrainDef>.AllDefsListForReading)
            {
                if (terrain.smoothedTerrain != null)
                    playerBuildableTerrains.Add(terrain.smoothedTerrain);
            }
        }

        private static void CollectPlayerBuildableFromDesignator(Designator des, HashSet<TerrainDef> set)
        {
            if (des is Designator_Build build && build.PlacingDef is TerrainDef td)
                set.Add(td);
            else if (des is Designator_Dropdown dropdown)
            {
                var elements = dropdown.Elements;
                if (elements == null) return;
                for (int i = 0; i < elements.Count; i++)
                    CollectPlayerBuildableFromDesignator(elements[i], set);
            }
        }

        private static bool HasStuffCategory(TerrainDef terrain, StuffCategoryDef category)
        {
            if (terrain.costList == null) return false;
            foreach (var cost in terrain.costList)
            {
                var cats = cost.thingDef?.stuffProps?.categories;
                if (cats != null && cats.Contains(category)) return true;
            }
            return false;
        }

        private static bool ContainsSteel(TerrainDef terrain)
        {
            if (terrain.costList == null) return false;
            foreach (var cost in terrain.costList)
                if (cost.thingDef == ThingDefOf.Steel) return true;
            return false;
        }
    }
}
