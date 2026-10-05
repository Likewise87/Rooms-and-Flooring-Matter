using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
	/// <summary>
	/// After a recruit chat reduces resistance, apply an extra % of that drop when the
	/// prisoner's PrisonCell/PrisonBarracks meets size/roof/floor gates. Also updates
	/// lastResistanceInteractionData so the Guest ITab stays accurate.
	/// </summary>
	[HarmonyPatch(typeof(InteractionWorker_RecruitAttempt), nameof(InteractionWorker_RecruitAttempt.Interacted))]
	internal static class Patch_RecruitAttempt_Interacted
	{
		private static readonly FieldInfo? LastResistanceDataField =
			AccessTools.Field(typeof(Pawn_GuestTracker), "lastResistanceInteractionData");

		private static readonly FieldInfo? ResistanceReductionField =
			AccessTools.Field(typeof(ResistanceInteractionData), "resistanceReduction");

		static void Prefix(Pawn recipient, ref float __state)
		{
			__state = recipient?.guest != null ? recipient.guest.resistance : -1f;
		}

		static void Postfix(Pawn recipient, float __state)
		{
			if (__state < 0f || recipient?.guest == null || recipient.Map == null) return;

			float after = recipient.guest.resistance;
			float dropped = __state - after;
			if (dropped <= 0.0001f) return;

			if (!RoomBonusGates.TryGetPrisonRecruitBonus(recipient, out float bonusFactor, out _))
				return;

			float extra = dropped * bonusFactor;
			if (extra <= 0.0001f) return;

			recipient.guest.resistance = Mathf.Max(0f, after - extra);

			if (LastResistanceDataField != null && ResistanceReductionField != null)
			{
				object? data = LastResistanceDataField.GetValue(recipient.guest);
				if (data != null)
				{
					float recorded = (float)ResistanceReductionField.GetValue(data)!;
					ResistanceReductionField.SetValue(data, recorded + extra);
				}
			}

			MoteMaker.ThrowText(
				recipient.DrawPos,
				recipient.Map,
				$"-{extra:0.##} resistance (room)",
				2.4f);
		}
	}

	/// <summary>
	/// Shows recruit resistance bonus status on beds in PrisonCell / PrisonBarracks.
	/// </summary>
	[HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.GetInspectString))]
	internal static class Patch_PrisonBed_GetInspectString
	{
		static void Postfix(Building_Bed __instance, ref string __result)
		{
			if (__instance?.Map == null) return;

			Room? room = __instance.GetRoom();
			if (room == null || !RoomBonusGates.IsPrisonRoom(room)) return;

			RoomBonusGates.TryGetPrisonRecruitBonus(room, __instance.Map, out _, out string explain);
			if (explain.NullOrEmpty()) return;

			var sb = new StringBuilder(__result ?? string.Empty);
			if (sb.Length > 0) sb.AppendLine();
			sb.Append(explain);
			__result = sb.ToString();
		}
	}
}
