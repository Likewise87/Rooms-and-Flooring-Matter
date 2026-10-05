using System;
using RimWorld;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
	/// <summary>
	/// Shared size / roof / floor-coverage gates for dining eat speed and prison recruit bonuses.
	/// </summary>
	public static class RoomBonusGates
	{
		private static readonly RoomRoleDef? DiningRole =
			DefDatabase<RoomRoleDef>.GetNamedSilentFail("DiningRoom");

		private static readonly RoomRoleDef? DeathrestChamberRole =
			DefDatabase<RoomRoleDef>.GetNamedSilentFail("DeathrestChamber");

		public static bool DeathrestChamberAvailable => DeathrestChamberRole != null;

		public static bool IsDiningRoom(Room room)
		{
			if (room?.Role == null) return false;
			if (DiningRole != null && room.Role == DiningRole) return true;
			return string.Equals(room.Role.defName, "DiningRoom", StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsDeathrestChamber(Room room)
		{
			if (room?.Role == null) return false;
			if (DeathrestChamberRole != null && room.Role == DeathrestChamberRole) return true;
			return string.Equals(room.Role.defName, "DeathrestChamber", StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsPrisonRoom(Room room)
		{
			if (room?.Role == null) return false;
			return room.Role == RoomRoleDefOf.PrisonCell || room.Role == RoomRoleDefOf.PrisonBarracks;
		}

		/// <summary>
		/// Returns true and the additive EatingSpeed bonus when the pawn is in a qualifying DiningRoom.
		/// </summary>
		public static bool TryGetDiningEatingBonus(Pawn pawn, out float bonus, out string explain)
		{
			bonus = 0f;
			explain = string.Empty;

			if (pawn?.Map == null || !pawn.RaceProps.Humanlike)
			{
				explain = "Dining eat bonus: 0% (not a humanlike pawn)";
				return false;
			}

			Room? room = pawn.GetRoom();
			if (room == null)
			{
				explain = "Dining eat bonus: 0% (no valid room)";
				return false;
			}

			if (!IsDiningRoom(room))
			{
				explain = "Dining eat bonus: 0% (not a DiningRoom)";
				return false;
			}

			var s = ModEntry.Settings;
			int size = room.CellCount;
			if (size < s.minSizeDining)
			{
				explain = $"Dining eat bonus: 0% (too small: {size}/{s.minSizeDining})";
				return false;
			}

			var scan = RoomFloorCache.Get(room, pawn.Map, Util_Flooring.FloorPolicy.Dining);
			if (!scan.FullyRoofed)
			{
				explain = "Dining eat bonus: 0% (room not fully roofed)";
				return false;
			}

			if (scan.Coverage.Percent < s.coverageThresholdDining)
			{
				explain =
					$"Dining eat bonus: 0% (insufficient coverage: {Math.Round(scan.Coverage.Percent)}%/{s.coverageThresholdDining:F0}%)";
				return false;
			}

			bonus = s.flooringBonusDining;
			explain =
				$"Dining eat bonus: +{(int)(bonus * 100f)}%pt (size {size} ≥ {s.minSizeDining}, coverage {Math.Round(scan.Coverage.Percent)}% ≥ {s.coverageThresholdDining:F0}%)";
			return bonus > 0f;
		}

		/// <summary>
		/// Returns true and the extra resistance factor (e.g. 0.15) when the room qualifies as a prison.
		/// </summary>
		public static bool TryGetPrisonRecruitBonus(Room? room, Map? map, out float bonusFactor, out string explain)
		{
			bonusFactor = 0f;
			explain = string.Empty;

			if (room == null || map == null)
			{
				explain = "Recruit resistance bonus: 0% (no valid room)";
				return false;
			}

			if (!IsPrisonRoom(room))
			{
				explain = "Recruit resistance bonus: 0% (not a PrisonCell/PrisonBarracks)";
				return false;
			}

			var s = ModEntry.Settings;
			int size = room.CellCount;
			bool isCell = room.Role == RoomRoleDefOf.PrisonCell;
			int needSize = isCell ? s.minSizePrisonCell : s.minSizePrisonBarracks;
			string roleLabel = isCell ? "PrisonCell" : "PrisonBarracks";

			if (size < needSize)
			{
				explain = $"Recruit resistance bonus: 0% ({roleLabel} too small: {size}/{needSize})";
				return false;
			}

			var scan = RoomFloorCache.Get(room, map, Util_Flooring.FloorPolicy.Prison);
			if (!scan.FullyRoofed)
			{
				explain = "Recruit resistance bonus: 0% (room not fully roofed)";
				return false;
			}

			if (scan.Coverage.Percent < s.coverageThresholdPrison)
			{
				explain =
					$"Recruit resistance bonus: 0% (insufficient coverage: {Math.Round(scan.Coverage.Percent)}%/{s.coverageThresholdPrison:F0}%)";
				return false;
			}

			bonusFactor = s.flooringBonusPrison;
			explain =
				$"Recruit resistance bonus: +{(int)(bonusFactor * 100f)}% (size {size} ≥ {needSize}, coverage {Math.Round(scan.Coverage.Percent)}% ≥ {s.coverageThresholdPrison:F0}%)";
			return bonusFactor > 0f;
		}

		public static bool TryGetPrisonRecruitBonus(Pawn prisoner, out float bonusFactor, out string explain)
		{
			if (prisoner?.Map == null)
			{
				bonusFactor = 0f;
				explain = "Recruit resistance bonus: 0% (no valid room)";
				return false;
			}

			return TryGetPrisonRecruitBonus(prisoner.GetRoom(), prisoner.Map, out bonusFactor, out explain);
		}

		/// <summary>
		/// Returns true and the additive efficiency fraction (e.g. 0.25 → multiply by 1.25)
		/// when the room is a qualifying DeathrestChamber.
		/// </summary>
		public static bool TryGetDeathrestBonus(Room? room, Map? map, out float bonus, out string explain)
		{
			bonus = 0f;
			explain = string.Empty;

			if (!DeathrestChamberAvailable)
			{
				explain = "Deathrest chamber bonus: 0% (Biotech deathrest chamber not available)";
				return false;
			}

			if (room == null || map == null)
			{
				explain = "Deathrest chamber bonus: 0% (no valid room)";
				return false;
			}

			if (!IsDeathrestChamber(room))
			{
				explain = "Deathrest chamber bonus: 0% (not a DeathrestChamber)";
				return false;
			}

			var s = ModEntry.Settings;
			int size = room.CellCount;
			if (size < s.minSizeDeathrest)
			{
				explain = $"Deathrest chamber bonus: 0% (too small: {size}/{s.minSizeDeathrest})";
				return false;
			}

			var scan = RoomFloorCache.Get(room, map, Util_Flooring.FloorPolicy.Deathrest);
			if (!scan.FullyRoofed)
			{
				explain = "Deathrest chamber bonus: 0% (room not fully roofed)";
				return false;
			}

			if (scan.Coverage.Percent < s.coverageThresholdDeathrest)
			{
				explain =
					$"Deathrest chamber bonus: 0% (insufficient coverage: {Math.Round(scan.Coverage.Percent)}%/{s.coverageThresholdDeathrest:F0}%)";
				return false;
			}

			bonus = s.flooringBonusDeathrest;
			explain =
				$"Deathrest chamber flooring bonus: +{(int)(bonus * 100f)}% (size {size} ≥ {s.minSizeDeathrest}, coverage {Math.Round(scan.Coverage.Percent)}% ≥ {s.coverageThresholdDeathrest:F0}%)";
			return bonus > 0f;
		}

		public static bool TryGetDeathrestBonus(Pawn? pawn, out float bonus, out string explain)
		{
			if (pawn?.Map == null)
			{
				bonus = 0f;
				explain = "Deathrest chamber bonus: 0% (no valid room)";
				return false;
			}

			return TryGetDeathrestBonus(pawn.GetRoom(), pawn.Map, out bonus, out explain);
		}
	}
}
