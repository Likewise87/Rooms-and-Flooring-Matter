using System;
using UnityEngine;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
    public enum SliderFormat { Fixed0, Fixed1, Fixed2, Percent, PercentDecimal }

    public partial class Settings : ModSettings
    {
        // Bedroom
        public float baseBonusBedroom = 0.10f;          // +10%pt
        public float flooringBonusBedroom = 0.10f;      // +10%pt
        public int minSizeBedroom = 17;
        public float coverageThresholdBedroom = 80f;

        // Barracks
        public float baseBonusBarracks = 0.075f;        // +7.5%pt
        public float flooringBonusBarracks = 0.10f;     // +10%pt
        public int minSizeBarracks = 21;
        public float coverageThresholdBarracks = 80f;

        // Workshop (machining, tailoring, smithing, stonecutting, smelter, biofuel refinery, etc.)
        public float flooringBonusWorkshop = 0.20f;     // +20%pt
        public int minSizeWorkshop = 25;
        public float coverageThresholdWorkshop = 80f;

        // Research + Drug lab (shared)
        public float flooringBonusResearch = 0.20f;     // +20%pt
        public int minSizeResearch = 25;
        public float coverageThresholdResearch = 80f;

        // Kitchen (stoves + butcher tables)
        public float flooringBonusKitchen = 0.20f;      // +20%pt
        public int minSizeKitchen = 25;
        public float coverageThresholdKitchen = 80f;

        // Dining/Rec seating (chairs/sofas) → Comfort (now fully configurable)
        public float flooringBonusSeating = 0.15f;      // +15%pt Comfort (default)
        public int minSizeSeating = 31;
        public float coverageThresholdSeating = 80f;

        // Dining – eating speed
        public float flooringBonusDining = 0.15f;       // +15%pt EatingSpeed
        public int minSizeDining = 31;
        public float coverageThresholdDining = 80f;

        // Prison – extra resistance removed on recruit chats
        public float flooringBonusPrison = 0.15f;       // +15% of the resistance just dropped
        public int minSizePrisonCell = 8;
        public int minSizePrisonBarracks = 21;
        public float coverageThresholdPrison = 80f;

        // Deathrest chamber (Biotech)
        public float flooringBonusDeathrest = 0.25f;    // +25%pt efficiency multiplier
        public int minSizeDeathrest = 25;
        public float coverageThresholdDeathrest = 80f;

        // Misc / Performance
        public int ttlTicks = 600; // ~10s at 60 TPS

        // UI
        private Vector2 scrollPos = Vector2.zero;

        private bool bedroomExpanded = true;
        private bool barracksExpanded = true;
        private bool workshopExpanded = true;
        private bool researchExpanded = true;
        private bool kitchenExpanded = true;
        private bool seatingExpanded = true;
        private bool diningExpanded = true;
        private bool prisonExpanded = true;
        private bool deathrestExpanded = true;
        private bool miscExpanded = true;

        public override void ExposeData()
        {
            // Bedroom
            Scribe_Values.Look(ref baseBonusBedroom, "baseBonusBedroom", 0.10f);
            Scribe_Values.Look(ref flooringBonusBedroom, "flooringBonusBedroom", 0.10f);
            Scribe_Values.Look(ref minSizeBedroom, "minSizeBedroom", 17);
            Scribe_Values.Look(ref coverageThresholdBedroom, "coverageThresholdBedroom", 80f);

            // Barracks
            Scribe_Values.Look(ref baseBonusBarracks, "baseBonusBarracks", 0.075f);
            Scribe_Values.Look(ref flooringBonusBarracks, "flooringBonusBarracks", 0.10f);
            Scribe_Values.Look(ref minSizeBarracks, "minSizeBarracks", 21);
            Scribe_Values.Look(ref coverageThresholdBarracks, "coverageThresholdBarracks", 80f);

            // Workshop
            Scribe_Values.Look(ref flooringBonusWorkshop, "flooringBonusWorkshop", 0.20f);
            Scribe_Values.Look(ref minSizeWorkshop, "minSizeWorkshop", 25);
            Scribe_Values.Look(ref coverageThresholdWorkshop, "coverageThresholdWorkshop", 80f);

            // Research + Drug lab (shared)
            Scribe_Values.Look(ref flooringBonusResearch, "flooringBonusResearch", 0.20f);
            Scribe_Values.Look(ref minSizeResearch, "minSizeResearch", 25);
            Scribe_Values.Look(ref coverageThresholdResearch, "coverageThresholdResearch", 80f);

            // Kitchen
            Scribe_Values.Look(ref flooringBonusKitchen, "flooringBonusKitchen", 0.20f);
            Scribe_Values.Look(ref minSizeKitchen, "minSizeKitchen", 25);
            Scribe_Values.Look(ref coverageThresholdKitchen, "coverageThresholdKitchen", 80f);

            // Seating (Comfort in Dining/Rec)
            Scribe_Values.Look(ref flooringBonusSeating, "flooringBonusSeating", 0.15f);
            Scribe_Values.Look(ref minSizeSeating, "minSizeSeating", 31);
            Scribe_Values.Look(ref coverageThresholdSeating, "coverageThresholdSeating", 80f);

            // Dining eating speed
            Scribe_Values.Look(ref flooringBonusDining, "flooringBonusDining", 0.15f);
            Scribe_Values.Look(ref minSizeDining, "minSizeDining", 31);
            Scribe_Values.Look(ref coverageThresholdDining, "coverageThresholdDining", 80f);

            // Prison recruit resistance
            Scribe_Values.Look(ref flooringBonusPrison, "flooringBonusPrison", 0.15f);
            Scribe_Values.Look(ref minSizePrisonCell, "minSizePrisonCell", 8);
            Scribe_Values.Look(ref minSizePrisonBarracks, "minSizePrisonBarracks", 21);
            Scribe_Values.Look(ref coverageThresholdPrison, "coverageThresholdPrison", 80f);

            // Deathrest chamber
            Scribe_Values.Look(ref flooringBonusDeathrest, "flooringBonusDeathrest", 0.25f);
            Scribe_Values.Look(ref minSizeDeathrest, "minSizeDeathrest", 25);
            Scribe_Values.Look(ref coverageThresholdDeathrest, "coverageThresholdDeathrest", 80f);

            // Misc
            Scribe_Values.Look(ref ttlTicks, "ttlTicks", 600);

            ScribeFloorOverrides();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                PruneFloorOverrides();
        }

        public void DoWindowContents(Rect inRect)
        {
            float contentWidth = inRect.width - 24f;
            float viewHeight = EstimateScrollHeight();
            Rect viewRect = new Rect(0f, 0f, contentWidth, viewHeight);

            Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            SettingsUiUtil.DrawMenuTopBar(listing, "Restore defaults", RestoreDefaults,
                () =>
                {
                    bedroomExpanded = barracksExpanded = workshopExpanded = researchExpanded =
                        kitchenExpanded = seatingExpanded = diningExpanded = prisonExpanded =
                        deathrestExpanded = miscExpanded = true;
                },
                () =>
                {
                    bedroomExpanded = barracksExpanded = workshopExpanded = researchExpanded =
                        kitchenExpanded = seatingExpanded = diningExpanded = prisonExpanded =
                        deathrestExpanded = miscExpanded = false;
                });

            if (SettingsUiUtil.DrawCollapsibleHeader(listing, "Bedroom bonuses", ref bedroomExpanded, SettingsUiUtil.SectionHeaderColor,
                "Rest and comfort gain base bonuses in bedrooms and guest rooms (Hospitality). Extra +%pt if enough 'good' flooring (fully roofed required)."))
            {
                baseBonusBedroom = SettingsUiUtil.LabeledSlider(listing, "Base bedroom bonus (+%pt)", baseBonusBedroom, 0f, 0.5f,
                    "Base Rest/Comfort bonus applied in bedrooms.", 0.01f, SliderFormat.Percent, 0.10f);
                flooringBonusBedroom = SettingsUiUtil.LabeledSlider(listing, "Additional flooring bonus (+%pt)", flooringBonusBedroom, 0f, 0.5f,
                    "Extra bonus when good-floor coverage meets the threshold.", 0.01f, SliderFormat.Percent, 0.10f);
                minSizeBedroom = Mathf.RoundToInt(SettingsUiUtil.LabeledSlider(listing, "Minimum room size (tiles)", minSizeBedroom, 1f, 100f,
                    "Rooms smaller than this never receive bedroom bonuses.", 1f, SliderFormat.Fixed0, 17f));
                coverageThresholdBedroom = SettingsUiUtil.LabeledSlider(listing, "Minimum good floor coverage", coverageThresholdBedroom, 0f, 100f,
                    "Percent of room tiles that must be good flooring to apply the flooring bonus.", 1f, SliderFormat.Fixed0, 80f);
                DrawFloorPolicyControls(listing, Util_Flooring.FloorPolicy.Bedroom);
            }

            if (SettingsUiUtil.DrawCollapsibleHeader(listing, "Barracks bonuses", ref barracksExpanded, SettingsUiUtil.SectionHeaderColor,
                "Rest and comfort gain base bonuses in barracks. Extra +%pt if enough 'good' flooring (fully roofed required)."))
            {
                baseBonusBarracks = SettingsUiUtil.LabeledSlider(listing, "Base barracks bonus (+%pt)", baseBonusBarracks, 0f, 0.5f,
                    "Base Rest/Comfort bonus applied in barracks.", 0.005f, SliderFormat.PercentDecimal, 0.075f);
                flooringBonusBarracks = SettingsUiUtil.LabeledSlider(listing, "Additional flooring bonus (+%pt)", flooringBonusBarracks, 0f, 0.5f,
                    "Extra bonus when good-floor coverage meets the threshold.", 0.01f, SliderFormat.Percent, 0.10f);
                minSizeBarracks = Mathf.RoundToInt(SettingsUiUtil.LabeledSlider(listing, "Minimum room size (tiles)", minSizeBarracks, 1f, 100f,
                    "Rooms smaller than this never receive barracks bonuses.", 1f, SliderFormat.Fixed0, 21f));
                coverageThresholdBarracks = SettingsUiUtil.LabeledSlider(listing, "Minimum good floor coverage", coverageThresholdBarracks, 0f, 100f,
                    "Percent of room tiles that must be good flooring to apply the flooring bonus.", 1f, SliderFormat.Fixed0, 80f);
                DrawFloorPolicyControls(listing, Util_Flooring.FloorPolicy.Barracks);
            }

            if (SettingsUiUtil.DrawCollapsibleHeader(listing, "Workshop bonuses", ref workshopExpanded, SettingsUiUtil.SectionHeaderColor,
                "Affects general workbenches (machining, tailoring, smithing, stonecutting, smelter, biofuel refinery, etc.). Allowed floors: stone, steel, or smoothed. Fully roofed required."))
            {
                flooringBonusWorkshop = SettingsUiUtil.LabeledSlider(listing, "Flooring bonus (+%pt)", flooringBonusWorkshop, 0f, 0.5f,
                    "Work speed bonus when coverage threshold is met.", 0.01f, SliderFormat.Percent, 0.20f);
                minSizeWorkshop = Mathf.RoundToInt(SettingsUiUtil.LabeledSlider(listing, "Minimum room size (tiles)", minSizeWorkshop, 1f, 100f,
                    "Rooms smaller than this never receive workshop bonuses.", 1f, SliderFormat.Fixed0, 25f));
                coverageThresholdWorkshop = SettingsUiUtil.LabeledSlider(listing, "Minimum good floor coverage", coverageThresholdWorkshop, 0f, 100f,
                    "Percent of room tiles that must be good flooring to apply the bonus.", 1f, SliderFormat.Fixed0, 80f);
                DrawFloorPolicyControls(listing, Util_Flooring.FloorPolicy.Workshop);
            }

            if (SettingsUiUtil.DrawCollapsibleHeader(listing, "Research and drug lab bonuses", ref researchExpanded, SettingsUiUtil.SectionHeaderColor,
                "Affects research benches and drug labs. Any human-made floor qualifies. Fully roofed required."))
            {
                flooringBonusResearch = SettingsUiUtil.LabeledSlider(listing, "Flooring bonus (+%pt)", flooringBonusResearch, 0f, 0.5f,
                    "Research and drug lab speed bonus when coverage threshold is met.", 0.01f, SliderFormat.Percent, 0.20f);
                minSizeResearch = Mathf.RoundToInt(SettingsUiUtil.LabeledSlider(listing, "Minimum room size (tiles)", minSizeResearch, 1f, 100f,
                    "Rooms smaller than this never receive research or drug lab bonuses.", 1f, SliderFormat.Fixed0, 25f));
                coverageThresholdResearch = SettingsUiUtil.LabeledSlider(listing, "Minimum good floor coverage", coverageThresholdResearch, 0f, 100f,
                    "Percent of room tiles that must be good flooring to apply the bonus.", 1f, SliderFormat.Fixed0, 80f);
                DrawFloorPolicyControls(listing, Util_Flooring.FloorPolicy.Research);
            }

            if (SettingsUiUtil.DrawCollapsibleHeader(listing, "Kitchen bonuses", ref kitchenExpanded, SettingsUiUtil.SectionHeaderColor,
                "Affects stoves and butcher tables. Any human-made floor qualifies. Fully roofed required."))
            {
                flooringBonusKitchen = SettingsUiUtil.LabeledSlider(listing, "Flooring bonus (+%pt)", flooringBonusKitchen, 0f, 0.5f,
                    "Cook/butcher speed bonus when coverage threshold is met.", 0.01f, SliderFormat.Percent, 0.20f);
                minSizeKitchen = Mathf.RoundToInt(SettingsUiUtil.LabeledSlider(listing, "Minimum room size (tiles)", minSizeKitchen, 1f, 100f,
                    "Rooms smaller than this never receive kitchen bonuses.", 1f, SliderFormat.Fixed0, 25f));
                coverageThresholdKitchen = SettingsUiUtil.LabeledSlider(listing, "Minimum good floor coverage", coverageThresholdKitchen, 0f, 100f,
                    "Percent of room tiles that must be good flooring to apply the bonus.", 1f, SliderFormat.Fixed0, 80f);
                DrawFloorPolicyControls(listing, Util_Flooring.FloorPolicy.Kitchen);
            }

            if (SettingsUiUtil.DrawCollapsibleHeader(listing, "Dining and recreation (seating comfort)", ref seatingExpanded, SettingsUiUtil.SectionHeaderColor,
                "Chairs and sofas gain Comfort when dining or recreation rooms are large and well floored. Any human-made floor qualifies. Fully roofed required."))
            {
                flooringBonusSeating = SettingsUiUtil.LabeledSlider(listing, "Comfort bonus for chairs/sofas (+%pt)", flooringBonusSeating, 0f, 0.5f,
                    "Comfort bonus when coverage threshold is met.", 0.01f, SliderFormat.Percent, 0.15f);
                minSizeSeating = Mathf.RoundToInt(SettingsUiUtil.LabeledSlider(listing, "Minimum room size (tiles)", minSizeSeating, 1f, 100f,
                    "Rooms smaller than this never receive seating comfort bonuses.", 1f, SliderFormat.Fixed0, 31f));
                coverageThresholdSeating = SettingsUiUtil.LabeledSlider(listing, "Minimum good floor coverage", coverageThresholdSeating, 0f, 100f,
                    "Percent of room tiles that must be good flooring to apply the bonus.", 1f, SliderFormat.Fixed0, 80f);
                DrawFloorPolicyControls(listing, Util_Flooring.FloorPolicy.Seating);
            }

            if (SettingsUiUtil.DrawCollapsibleHeader(listing, "Dining (eating speed)", ref diningExpanded, SettingsUiUtil.SectionHeaderColor,
                "Humanlike pawns eat faster in a proper dining room. Any human-made floor qualifies. Fully roofed required."))
            {
                flooringBonusDining = SettingsUiUtil.LabeledSlider(listing, "Eating speed bonus (+%pt)", flooringBonusDining, 0f, 0.5f,
                    "Additive EatingSpeed bonus while eating in a qualifying DiningRoom.", 0.01f, SliderFormat.Percent, 0.15f);
                minSizeDining = Mathf.RoundToInt(SettingsUiUtil.LabeledSlider(listing, "Minimum room size (tiles)", minSizeDining, 1f, 100f,
                    "DiningRooms smaller than this never receive the eating speed bonus.", 1f, SliderFormat.Fixed0, 31f));
                coverageThresholdDining = SettingsUiUtil.LabeledSlider(listing, "Minimum good floor coverage", coverageThresholdDining, 0f, 100f,
                    "Percent of room tiles that must be good flooring to apply the bonus.", 1f, SliderFormat.Fixed0, 80f);
                DrawFloorPolicyControls(listing, Util_Flooring.FloorPolicy.Dining);
            }

            if (SettingsUiUtil.DrawCollapsibleHeader(listing, "Prison (recruiting)", ref prisonExpanded, SettingsUiUtil.SectionHeaderColor,
                "Extra % of the resistance just removed by a recruit attempt, if the prisoner's PrisonCell/PrisonBarracks is large, roofed, and well-floored. Shown on prison beds; mote appears when the extra drop applies."))
            {
                flooringBonusPrison = SettingsUiUtil.LabeledSlider(listing, "Extra resistance removed (+%)", flooringBonusPrison, 0f, 0.5f,
                    "Extra percent of the resistance just removed by a warden recruit chat (e.g. 15% of a 2.0 drop = +0.3). Does not affect Torture/Archotech pulses.", 0.01f, SliderFormat.Percent, 0.15f);
                minSizePrisonCell = Mathf.RoundToInt(SettingsUiUtil.LabeledSlider(listing, "Minimum PrisonCell size (tiles)", minSizePrisonCell, 1f, 100f,
                    "PrisonCells smaller than this never receive the recruit bonus.", 1f, SliderFormat.Fixed0, 8f));
                minSizePrisonBarracks = Mathf.RoundToInt(SettingsUiUtil.LabeledSlider(listing, "Minimum PrisonBarracks size (tiles)", minSizePrisonBarracks, 1f, 100f,
                    "PrisonBarracks smaller than this never receive the recruit bonus.", 1f, SliderFormat.Fixed0, 21f));
                coverageThresholdPrison = SettingsUiUtil.LabeledSlider(listing, "Minimum good floor coverage", coverageThresholdPrison, 0f, 100f,
                    "Percent of room tiles that must be good flooring to apply the bonus.", 1f, SliderFormat.Fixed0, 80f);
                DrawFloorPolicyControls(listing, Util_Flooring.FloorPolicy.Prison);
            }

            if (RoomBonusGates.DeathrestChamberAvailable &&
                SettingsUiUtil.DrawCollapsibleHeader(listing, "Deathrest chamber", ref deathrestExpanded, SettingsUiUtil.SectionHeaderColor,
                    "Biotech deathrest recovers faster in a proper DeathrestChamber. Allowed floors: stone, steel, or smoothed. Fully roofed required. Multiplies deathrest efficiency (stacks with casket/accelerators)."))
            {
                flooringBonusDeathrest = SettingsUiUtil.LabeledSlider(listing, "Deathrest efficiency bonus (+%)", flooringBonusDeathrest, 0f, 0.5f,
                    "Multiplies Gene_Deathrest.DeathrestEfficiency when coverage threshold is met (e.g. +25% → ×1.25).", 0.01f, SliderFormat.Percent, 0.25f);
                minSizeDeathrest = Mathf.RoundToInt(SettingsUiUtil.LabeledSlider(listing, "Minimum room size (tiles)", minSizeDeathrest, 1f, 100f,
                    "DeathrestChambers smaller than this never receive the efficiency bonus.", 1f, SliderFormat.Fixed0, 25f));
                coverageThresholdDeathrest = SettingsUiUtil.LabeledSlider(listing, "Minimum good floor coverage", coverageThresholdDeathrest, 0f, 100f,
                    "Percent of room tiles that must be good flooring to apply the bonus.", 1f, SliderFormat.Fixed0, 80f);
                DrawFloorPolicyControls(listing, Util_Flooring.FloorPolicy.Deathrest);
            }

            if (SettingsUiUtil.DrawCollapsibleHeader(listing, "Misc", ref miscExpanded, SettingsUiUtil.SectionHeaderColor,
                "How long room floor coverage and roof checks are cached. Higher values = fewer room scans (lower CPU), but bonuses update more slowly after you change floors or roofs."))
            {
                ttlTicks = Mathf.RoundToInt(SettingsUiUtil.LabeledSlider(listing, "Cache lifetime in ticks", ttlTicks, 60f, 3600f,
                    "Default 600 ≈ 10 seconds at 60 TPS.", 10f, SliderFormat.Fixed0, 600f));
            }

            listing.End();
            Widgets.EndScrollView();
        }

        private float EstimateScrollHeight()
        {
            float h = 80f;
            if (bedroomExpanded) h += 194f;
            if (barracksExpanded) h += 194f;
            if (workshopExpanded) h += 164f;
            if (researchExpanded) h += 164f;
            if (kitchenExpanded) h += 164f;
            if (seatingExpanded) h += 164f;
            if (diningExpanded) h += 164f;
            if (prisonExpanded) h += 194f;
            if (RoomBonusGates.DeathrestChamberAvailable && deathrestExpanded) h += 164f;
            if (miscExpanded) h += 70f;
            h += 10 * 50f;
            return Mathf.Max(h, 400f);
        }

        private void RestoreDefaults()
        {
            baseBonusBedroom = 0.10f;
            flooringBonusBedroom = 0.10f;
            minSizeBedroom = 17;
            coverageThresholdBedroom = 80f;

            baseBonusBarracks = 0.075f;
            flooringBonusBarracks = 0.10f;
            minSizeBarracks = 21;
            coverageThresholdBarracks = 80f;

            flooringBonusWorkshop = 0.20f;
            minSizeWorkshop = 25;
            coverageThresholdWorkshop = 80f;

            flooringBonusResearch = 0.20f;
            minSizeResearch = 25;
            coverageThresholdResearch = 80f;

            flooringBonusKitchen = 0.20f;
            minSizeKitchen = 25;
            coverageThresholdKitchen = 80f;

            flooringBonusSeating = 0.15f;
            minSizeSeating = 31;
            coverageThresholdSeating = 80f;

            flooringBonusDining = 0.15f;
            minSizeDining = 31;
            coverageThresholdDining = 80f;

            flooringBonusPrison = 0.15f;
            minSizePrisonCell = 8;
            minSizePrisonBarracks = 21;
            coverageThresholdPrison = 80f;

            flooringBonusDeathrest = 0.25f;
            minSizeDeathrest = 25;
            coverageThresholdDeathrest = 80f;

            ttlTicks = 600;
            ClearAllFloorOverrides();
        }

        private void DrawFloorPolicyControls(Listing_Standard listing, Util_Flooring.FloorPolicy policy)
        {
            listing.Gap(6f);
            Rect row = listing.GetRect(30f);
            string status = GetFloorOverrideStatusLabel(policy);
            if (IsFloorPolicyCustomized(policy))
                status = status.Colorize(Color.yellow);

            // Match LabeledSlider: label in left 50%, controls in right 50%.
            float btnW = row.width * 0.22f;
            Rect btnRect = new Rect(row.xMax - btnW, row.y, btnW, row.height);
            Rect rightCol = row.RightPart(0.5f);
            Rect statusRect = new Rect(rightCol.x, row.y, btnRect.x - rightCol.x - 4f, row.height);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(row.LeftPart(0.5f), "Good floors");
            Widgets.Label(statusRect, status);
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonText(btnRect, "Edit good floors"))
                Find.WindowStack.Add(new Dialog_GoodFloorsPicker(this, policy));
            string tip = SettingsUiUtil.PrefixSettingTooltip(
                FloorPolicyLabels.GetDefaultDescription(policy),
                IsFloorPolicyCustomized(policy));
            TooltipHandler.TipRegion(row, tip);
        }
    }
}
