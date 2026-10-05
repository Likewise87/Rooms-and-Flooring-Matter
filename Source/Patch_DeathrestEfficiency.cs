using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
	/// <summary>
	/// Soft Biotech hook: multiplies Gene_Deathrest.DeathrestEfficiency when the pawn's
	/// DeathrestChamber qualifies. Mirrors bedroom Rest effectiveness UX on the casket's
	/// "Deathrest effectiveness" row: left value is final, right side is Base / bonus / Final.
	/// </summary>
	internal static class Patch_DeathrestEfficiency
	{
		private static readonly Type? GeneDeathrestType =
			AccessTools.TypeByName("RimWorld.Gene_Deathrest");

		private static readonly Type? CompPropsDeathrestBindableType =
			AccessTools.TypeByName("RimWorld.CompProperties_DeathrestBindable");

		private static readonly FieldInfo? DeathrestEffectivenessFactorField =
			CompPropsDeathrestBindableType != null
				? AccessTools.Field(CompPropsDeathrestBindableType, "deathrestEffectivenessFactor")
				: null;

		private static readonly FieldInfo? LabelIntField =
			AccessTools.Field(typeof(StatDrawEntry), "labelInt");

		private static readonly FieldInfo? OverrideReportTextField =
			AccessTools.Field(typeof(StatDrawEntry), "overrideReportText");

		private static readonly FieldInfo? OverrideReportTitleField =
			AccessTools.Field(typeof(StatDrawEntry), "overrideReportTitle");

		private static readonly FieldInfo? HyperlinksField =
			AccessTools.Field(typeof(StatDrawEntry), "hyperlinks");

		private static readonly FieldInfo? ForceUnfinalizedField =
			AccessTools.Field(typeof(StatDrawEntry), "forceUnfinalizedMode");

		private static readonly FieldInfo? OverridesHideStatsField =
			AccessTools.Field(typeof(StatDrawEntry), "overridesHideStats");

		internal static void Apply(Harmony harmony)
		{
			if (GeneDeathrestType == null)
			{
				Log.Message("[RoomsAndFlooringMatter] Gene_Deathrest not found; deathrest chamber bonus skipped.");
				return;
			}

			MethodInfo? efficiencyGetter = AccessTools.PropertyGetter(GeneDeathrestType, "DeathrestEfficiency");
			if (efficiencyGetter == null)
			{
				Log.Warning("[RoomsAndFlooringMatter] Gene_Deathrest.DeathrestEfficiency getter missing; deathrest bonus skipped.");
				return;
			}

			harmony.Patch(
				efficiencyGetter,
				postfix: new HarmonyMethod(typeof(Patch_DeathrestEfficiency), nameof(DeathrestEfficiency_Postfix)));

			if (CompPropsDeathrestBindableType != null)
			{
				MethodInfo? buildingStats = AccessTools.Method(
					CompPropsDeathrestBindableType,
					"SpecialDisplayStats",
					new[] { typeof(StatRequest) });
				if (buildingStats == null)
					buildingStats = AccessTools.Method(CompPropsDeathrestBindableType, "SpecialDisplayStats");

				if (buildingStats != null)
				{
					harmony.Patch(
						buildingStats,
						postfix: new HarmonyMethod(typeof(Patch_DeathrestEfficiency), nameof(BuildingSpecialDisplayStats_Postfix)));
				}
				else
				{
					Log.Warning("[RoomsAndFlooringMatter] CompProperties_DeathrestBindable.SpecialDisplayStats missing.");
				}
			}

			Log.Message("[RoomsAndFlooringMatter] Deathrest chamber efficiency patches applied.");
		}

		private static Pawn? PawnFromGene(object gene)
		{
			if (gene is Gene g)
				return g.pawn;
			return null;
		}

		static void DeathrestEfficiency_Postfix(object __instance, ref float __result)
		{
			Pawn? pawn = PawnFromGene(__instance);
			if (pawn == null) return;

			if (!RoomBonusGates.TryGetDeathrestBonus(pawn, out float bonus, out _))
				return;

			__result *= (1f + bonus);
		}

		static void BuildingSpecialDisplayStats_Postfix(StatRequest req, ref IEnumerable<StatDrawEntry> __result)
		{
			if (!req.HasThing || req.Thing?.Map == null)
				return;

			Thing thing = req.Thing;
			Room? room = thing.GetRoom();
			RoomBonusGates.TryGetDeathrestBonus(room, thing.Map, out float bonus, out string explain);

			float baseFactor = TryGetBaseEffectivenessFactor(thing);
			__result = RewriteDeathrestEffectiveness(__result, baseFactor, bonus, explain);
		}

		private static float TryGetBaseEffectivenessFactor(Thing thing)
		{
			if (DeathrestEffectivenessFactorField == null || CompPropsDeathrestBindableType == null)
				return 1f;

			foreach (CompProperties cp in thing.def.comps)
			{
				if (CompPropsDeathrestBindableType.IsInstanceOfType(cp))
					return (float)DeathrestEffectivenessFactorField.GetValue(cp);
			}

			return 1f;
		}

		private static IEnumerable<StatDrawEntry> RewriteDeathrestEffectiveness(
			IEnumerable<StatDrawEntry> source, float baseFactor, float bonus, string explain)
		{
			float finalFactor = bonus > 0f ? baseFactor * (1f + bonus) : baseFactor;

			foreach (var e in source)
			{
				if (!IsDeathrestEffectivenessEntry(e))
				{
					yield return e;
					continue;
				}

				string label = GetLabelInt(e) ?? e.LabelCap;
				string baseReport = GetOverrideReportText(e) ?? string.Empty;
				string bonusLine = explain.NullOrEmpty()
					? "Deathrest chamber flooring bonus: 0%"
					: explain;

				var sb = new StringBuilder();
				if (!baseReport.NullOrEmpty())
				{
					sb.AppendLine(baseReport);
					sb.AppendLine();
				}

				// Match StatWorker-style Base / StatPart / Final layout used by Rest effectiveness.
				sb.AppendLine("StatsReport_BaseValue".Translate() + ": " + baseFactor.ToStringPercent());
				sb.AppendLine(bonusLine);
				sb.AppendLine("StatsReport_FinalValue".Translate() + ": " + finalFactor.ToStringPercent());

				yield return CloneEntry(e, label, finalFactor.ToStringPercent(), sb.ToString().TrimEnd());
			}
		}

		private static StatDrawEntry CloneEntry(
			StatDrawEntry source, string label, string valueString, string reportText)
		{
			return new StatDrawEntry(
				source.category,
				label,
				valueString,
				reportText,
				source.DisplayPriorityWithinCategory,
				GetOverrideReportTitle(source),
				GetHyperlinks(source),
				GetForceUnfinalized(source),
				GetOverridesHideStats(source));
		}

		private static bool IsDeathrestEffectivenessEntry(StatDrawEntry entry)
		{
			const string key = "StatsReport_DeathrestEffectiveness";
			string translated = key.Translate();

			string? labelInt = GetLabelInt(entry);
			if (!labelInt.NullOrEmpty())
			{
				if (string.Equals(labelInt, key, StringComparison.Ordinal))
					return true;
				if (string.Equals(labelInt, translated, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			try
			{
				string cap = entry.LabelCap;
				if (!cap.NullOrEmpty() && string.Equals(cap, translated, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			catch
			{
				// ignore
			}

			return false;
		}

		private static string? GetLabelInt(StatDrawEntry entry) =>
			LabelIntField?.GetValue(entry) as string;

		private static string? GetOverrideReportText(StatDrawEntry entry) =>
			OverrideReportTextField?.GetValue(entry) as string;

		private static string? GetOverrideReportTitle(StatDrawEntry entry) =>
			OverrideReportTitleField?.GetValue(entry) as string;

		private static IEnumerable<Dialog_InfoCard.Hyperlink>? GetHyperlinks(StatDrawEntry entry) =>
			HyperlinksField?.GetValue(entry) as IEnumerable<Dialog_InfoCard.Hyperlink>;

		private static bool GetForceUnfinalized(StatDrawEntry entry) =>
			ForceUnfinalizedField != null && (bool)ForceUnfinalizedField.GetValue(entry);

		private static bool GetOverridesHideStats(StatDrawEntry entry) =>
			OverridesHideStatsField != null && (bool)OverridesHideStatsField.GetValue(entry);
	}
}
