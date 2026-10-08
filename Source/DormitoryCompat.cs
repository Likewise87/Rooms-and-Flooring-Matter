using System;
using RimWorld;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
	/// <summary>
	/// Soft compatibility with Dormitories (Not Barracks) Continued — no Dormitories.dll reference.
	/// </summary>
	public static class DormitoryCompat
	{
		public const string DormitoriesPackageId = "stevescriggins.Dormitories";
		public const string DormitoryRoleDefName = "Dormitory";

		private static RoomRoleDef? dormitoryDef;
		private static bool lookedUp;

		public static bool DormitoryAvailable => DormitoryDef != null;

		public static RoomRoleDef? DormitoryDef
		{
			get
			{
				if (!lookedUp)
				{
					lookedUp = true;
					if (ModsConfig.IsActive(DormitoriesPackageId))
						dormitoryDef = DefDatabase<RoomRoleDef>.GetNamedSilentFail(DormitoryRoleDefName);
				}

				return dormitoryDef;
			}
		}

		public static bool IsDormitory(Room? room)
		{
			if (room?.Role == null) return false;
			if (DormitoryDef != null && room.Role == DormitoryDef) return true;
			return string.Equals(room.Role.defName, DormitoryRoleDefName, StringComparison.OrdinalIgnoreCase);
		}
	}
}
