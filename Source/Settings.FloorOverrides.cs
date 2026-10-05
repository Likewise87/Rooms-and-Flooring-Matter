using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
    public class FloorPolicyOverrideState
    {
        public bool customized;
        public List<string> defNames = new();
    }

    public partial class Settings
    {
        private FloorPolicyOverrideState[]? floorOverrides;

        public void EnsureFloorOverridesInitialized()
        {
            int count = Enum.GetValues(typeof(Util_Flooring.FloorPolicy)).Length;
            if (floorOverrides == null || floorOverrides.Length != count)
            {
                var old = floorOverrides;
                floorOverrides = new FloorPolicyOverrideState[count];
                if (old != null)
                {
                    if (old.Length == 7 && count == 8)
                        MigrateLegacyFloorOverrideArray(old);
                    else
                    {
                        for (int i = 0; i < Math.Min(old.Length, count); i++)
                            floorOverrides[i] = old[i];
                    }
                }
            }

            for (int i = 0; i < count; i++)
                floorOverrides[i] ??= new FloorPolicyOverrideState();
        }

        private void MigrateLegacyFloorOverrideArray(FloorPolicyOverrideState[] old)
        {
            floorOverrides![0] = CloneFloorOverrideState(old[0]);
            floorOverrides[1] = CloneFloorOverrideState(old[0]);
            if (old.Length > 1) floorOverrides[2] = old[1];
            if (old.Length > 2) floorOverrides[3] = old[2];
            if (old.Length > 3) floorOverrides[4] = old[3];
            if (old.Length > 4) floorOverrides[5] = old[4];
            if (old.Length > 5) floorOverrides[6] = old[5];
            if (old.Length > 6) floorOverrides[7] = old[6];
        }

        private static FloorPolicyOverrideState CloneFloorOverrideState(FloorPolicyOverrideState? source)
        {
            if (source == null) return new FloorPolicyOverrideState();
            return new FloorPolicyOverrideState
            {
                customized = source.customized,
                defNames = new List<string>(source.defNames)
            };
        }

        public bool IsFloorPolicyCustomized(Util_Flooring.FloorPolicy policy)
        {
            EnsureFloorOverridesInitialized();
            return floorOverrides![(int)policy].customized;
        }

        public bool ContainsFloorOverrideDef(TerrainDef terrain, Util_Flooring.FloorPolicy policy)
        {
            if (terrain?.defName == null) return false;
            EnsureFloorOverridesInitialized();
            var state = floorOverrides![(int)policy];
            if (!state.customized) return false;
            return state.defNames.Contains(terrain.defName);
        }

        public List<string> GetSavedFloorOverrideDefNames(Util_Flooring.FloorPolicy policy)
        {
            EnsureFloorOverridesInitialized();
            return new List<string>(floorOverrides![(int)policy].defNames);
        }

        public void ApplyFloorOverride(Util_Flooring.FloorPolicy policy, HashSet<string> selected, HashSet<string> defaultSnapshot)
        {
            EnsureFloorOverridesInitialized();
            var state = floorOverrides![(int)policy];
            if (Util_Flooring.SetsEqual(selected, defaultSnapshot))
            {
                state.customized = false;
                state.defNames.Clear();
            }
            else
            {
                state.customized = true;
                state.defNames.Clear();
                state.defNames.AddRange(selected);
            }

            Util_Flooring.RebuildGoodFloorCache();
            RoomFloorCache.Clear();
        }

        public void ClearFloorOverride(Util_Flooring.FloorPolicy policy)
        {
            EnsureFloorOverridesInitialized();
            var state = floorOverrides![(int)policy];
            state.customized = false;
            state.defNames.Clear();
            Util_Flooring.RebuildGoodFloorCache();
            RoomFloorCache.Clear();
        }

        public void ClearAllFloorOverrides()
        {
            EnsureFloorOverridesInitialized();
            foreach (var state in floorOverrides!)
            {
                state.customized = false;
                state.defNames.Clear();
            }

            Util_Flooring.RebuildGoodFloorCache();
            RoomFloorCache.Clear();
        }

        public void PruneFloorOverrides()
        {
            EnsureFloorOverridesInitialized();
            bool changed = false;
            foreach (var state in floorOverrides!)
            {
                if (!state.customized) continue;
                for (int i = state.defNames.Count - 1; i >= 0; i--)
                {
                    if (DefDatabase<TerrainDef>.GetNamedSilentFail(state.defNames[i]) == null)
                    {
                        state.defNames.RemoveAt(i);
                        changed = true;
                    }
                }

                if (state.defNames.Count == 0)
                {
                    state.customized = false;
                    changed = true;
                }
            }

            if (changed)
            {
                Util_Flooring.RebuildGoodFloorCache();
                RoomFloorCache.Clear();
            }
        }

        public string GetFloorOverrideStatusLabel(Util_Flooring.FloorPolicy policy)
        {
            if (!IsFloorPolicyCustomized(policy))
                return "Using defaults";
            int n = GetSavedFloorOverrideDefNames(policy).Count;
            return $"Custom ({n} floors)";
        }

        private void ScribeFloorOverrides()
        {
            EnsureFloorOverridesInitialized();
            foreach (Util_Flooring.FloorPolicy policy in Enum.GetValues(typeof(Util_Flooring.FloorPolicy)))
            {
                int idx = (int)policy;
                var state = floorOverrides![idx];
                string key = policy.ToString();
                Scribe_Values.Look(ref state.customized, $"floorOverrideCustomized_{key}", false);
                Scribe_Collections.Look(ref state.defNames, $"floorOverrideDefNames_{key}");
                state.defNames ??= new List<string>();
            }

            MigrateLegacyBedroomOrBarracksOverride();
        }

        private void MigrateLegacyBedroomOrBarracksOverride()
        {
            bool legacyCustomized = false;
            List<string>? legacyDefNames = null;
            Scribe_Values.Look(ref legacyCustomized, "floorOverrideCustomized_BedroomOrBarracks", false);
            Scribe_Collections.Look(ref legacyDefNames, "floorOverrideDefNames_BedroomOrBarracks");
            if (Scribe.mode != LoadSaveMode.LoadingVars || !legacyCustomized)
                return;

            legacyDefNames ??= new List<string>();
            var bedroom = floorOverrides![(int)Util_Flooring.FloorPolicy.Bedroom];
            var barracks = floorOverrides[(int)Util_Flooring.FloorPolicy.Barracks];

            if (!bedroom.customized)
            {
                bedroom.customized = true;
                bedroom.defNames = new List<string>(legacyDefNames);
            }

            if (!barracks.customized)
            {
                barracks.customized = true;
                barracks.defNames = new List<string>(legacyDefNames);
            }
        }
    }
}
