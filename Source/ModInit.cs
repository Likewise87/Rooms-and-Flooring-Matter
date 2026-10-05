using HarmonyLib;
using RimWorld;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            Util_Flooring.RebuildGoodFloorCache();

            // Beds → bedroom/barracks bonuses (RestEffectiveness + Comfort)
            AttachIfMissing("BedRestEffectiveness", () => new StatPart_BedRoomFlooringBonus());
            AttachIfMissing("Comfort", () => new StatPart_BedRoomFlooringBonus());

            // Chairs/Sofas → Comfort bonus in Dining/Rec
            AttachIfMissing("Comfort", () => new StatPart_SeatingRoomFlooringBonus());

            // DiningRoom → EatingSpeed
            AttachIfMissing("EatingSpeed", () => new StatPart_DiningEatingSpeedBonus());

            // Generic workbenches
            AttachIfMissing("WorkTableWorkSpeedFactor", () => new StatPart_RoomLinkableBonus());

            // Research benches (shared settings with drug lab)
            AttachIfMissing("ResearchSpeedFactor", () => new StatPart_RoomLinkableBonus());

            // Drug lab (shared settings with research)
            AttachIfMissing("DrugCookingSpeed", () => new StatPart_RoomLinkableBonus());

            // Kitchen (stoves + butcher)
            AttachIfMissing("CookSpeed", () => new StatPart_RoomLinkableBonus());
            AttachIfMissing("ButcheryFleshSpeed", () => new StatPart_RoomLinkableBonus());
            AttachIfMissing("ButcheryMechanoidSpeed", () => new StatPart_RoomLinkableBonus());

            // Biotech deathrest chamber flooring → Gene_Deathrest.DeathrestEfficiency
            try
            {
                var harmony = new Harmony("TSA.RoomsAndFlooringMatter.Deathrest");
                Patch_DeathrestEfficiency.Apply(harmony);
            }
            catch (System.Exception e)
            {
                Log.Warning("[RoomsAndFlooringMatter] Deathrest patch failed: " + e);
            }

            Log.Message("[RoomsAndFlooringMatter] Bootstrap completed. StatParts attached.");
        }

        private static void AttachIfMissing(string statDefName, System.Func<StatPart> factory)
        {
            var stat = DefDatabase<StatDef>.GetNamedSilentFail(statDefName);
            if (stat == null)
            {
                Log.Warning($"[RoomsAndFlooringMatter] Could not find StatDef {statDefName}");
                return;
            }

            if (stat.parts == null)
                stat.parts = new System.Collections.Generic.List<StatPart>();

            var inst = factory();
            var newType = inst.GetType();
            foreach (var p in stat.parts)
            {
                if (p != null && p.GetType() == newType)
                    return; // already present
            }

            inst.parentStat = stat; // important: gives context in ExplanationPart
            stat.parts.Add(inst);
        }
    }

    public class ModEntry : Mod
    {
        public static Settings Settings;

        public ModEntry(ModContentPack content) : base(content)
        {
            Settings = GetSettings<Settings>();
            new Harmony("TSA.RoomsAndFlooringMatter").PatchAll();
        }

        public override string SettingsCategory() => "Rooms and Flooring Matter";

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }
    }
}
