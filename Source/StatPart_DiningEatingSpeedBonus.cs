using RimWorld;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
	/// <summary>
	/// Additive EatingSpeed bonus while a humanlike pawn is in a qualifying DiningRoom.
	/// </summary>
	public class StatPart_DiningEatingSpeedBonus : StatPart
	{
		public override void TransformValue(StatRequest req, ref float val)
		{
			if (parentStat == null || parentStat.defName != "EatingSpeed") return;
			if (!req.HasThing || req.Thing is not Pawn pawn) return;

			if (RoomBonusGates.TryGetDiningEatingBonus(pawn, out float bonus, out _))
				val += bonus;
		}

		public override string ExplanationPart(StatRequest req)
		{
			if (parentStat == null || parentStat.defName != "EatingSpeed") return null;
			if (!req.HasThing || req.Thing is not Pawn pawn) return null;
			if (!pawn.RaceProps.Humanlike) return null;

			RoomBonusGates.TryGetDiningEatingBonus(pawn, out _, out string explain);
			return explain;
		}
	}
}
