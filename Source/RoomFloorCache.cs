using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
    public struct CoverageResult
    {
        public int TotalTiles;
        public int GoodTiles;
        public float Percent => TotalTiles > 0 ? (GoodTiles / (float)TotalTiles) * 100f : 0f;
    }

    public struct RoomFloorScan
    {
        public CoverageResult Coverage;
        public bool FullyRoofed;
    }

    public static class RoomFloorCache
    {
        private const int MaxEntries = 256;

        // key: room.ID * 16 + policyIndex
        private static readonly Dictionary<int, (int lastTick, RoomFloorScan result)> cache = new();

        public static RoomFloorScan Get(Room room, Map map, Util_Flooring.FloorPolicy policy)
        {
            if (room == null || map == null) return default;

            int key = room.ID * 16 + (int)policy;
            int now = Find.TickManager.TicksGame;

            if (cache.TryGetValue(key, out var entry))
            {
                if (now - entry.lastTick <= ModEntry.Settings.ttlTicks)
                    return entry.result;
            }

            int total = 0, good = 0;
            bool fullyRoofed = true;
            foreach (var c in room.Cells)
            {
                total++;
                if (fullyRoofed && !c.Roofed(map))
                    fullyRoofed = false;

                var t = c.GetTerrain(map);
                if (Util_Flooring.IsGoodFloor(t, policy)) good++;
            }

            var res = new RoomFloorScan
            {
                Coverage = new CoverageResult { TotalTiles = total, GoodTiles = good },
                FullyRoofed = fullyRoofed
            };

            if (cache.Count >= MaxEntries)
                cache.Clear();

            cache[key] = (now, res);
            return res;
        }

        public static void Clear() => cache.Clear();
    }
}
