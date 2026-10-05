using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TSA.RoomsAndFlooringMatter
{
    public class Dialog_GoodFloorsPicker : Window
    {
        private readonly Settings settings;
        private readonly Util_Flooring.FloorPolicy policy;
        private readonly HashSet<string> defaultSnapshot;
        private readonly HashSet<string> workingSelection;
        private readonly List<FloorRowEntry> allRows = new();
        private readonly List<FloorRowEntry> visibleRows = new();

        private Vector2 scrollPos;
        private string nameSearchTerm = string.Empty;
        private FloorMaterialKind? materialFilter;
        private BuildableColumnFilter buildableFilter = BuildableColumnFilter.All;

        private string sortColumn = "Name";
        private bool sortAscending = true;

        private enum BuildableColumnFilter { All, Yes, No }

        private const float HeaderHeight = 30f;
        private const float RowHeight = 32f;
        private const float RowIconSize = 32f;
        private const float BottomH = 48f;
        private const float ToolbarRowH = 28f;
        private const float ColSelect = 40f;
        private const float ColImage = 44f;
        private const float ColName = 180f;
        private const float ColData = 92f;
        private const float ResetBtnW = 130f;
        /// <summary>Material, Buildable, Value, and Default share ColData.</summary>
        private const float TableWidth = ColSelect + ColImage + ColName + ColData * 4f;
        /// <summary>Reserved so the vertical scrollbar does not cover the Default column.</summary>
        private const float ScrollbarPad = 16f;
        /// <summary>RimWorld Window default margin is 18 on each side.</summary>
        private const float WindowChromeX = 36f;

        private float tableWidth;

        private class FloorRowEntry
        {
            public TerrainDef Terrain = null!;
            public List<TerrainDef> Members = new();
            public List<string> MemberDefNames = new();
            public string GroupKey = string.Empty;
            public string NameLabel = string.Empty;
            public FloorMaterialKind MaterialKind;
            public string MaterialLabel = string.Empty;
            public bool Buildable;
            public float? MarketValue;
            public string ValueLabel = string.Empty;
            public bool IsDefault;

            public bool IsFullySelected(HashSet<string> selection)
            {
                if (MemberDefNames.Count == 0) return false;
                for (int i = 0; i < MemberDefNames.Count; i++)
                {
                    if (!selection.Contains(MemberDefNames[i]))
                        return false;
                }
                return true;
            }

            public void ApplySelection(HashSet<string> selection, bool selected)
            {
                for (int i = 0; i < MemberDefNames.Count; i++)
                {
                    if (selected) selection.Add(MemberDefNames[i]);
                    else selection.Remove(MemberDefNames[i]);
                }
            }
        }

        public override Vector2 InitialSize => new Vector2(TableWidth + WindowChromeX + ScrollbarPad + 10f, 680f);

        public Dialog_GoodFloorsPicker(Settings settings, Util_Flooring.FloorPolicy policy)
        {
            this.settings = settings;
            this.policy = policy;
            doCloseButton = false;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;

            defaultSnapshot = Util_Flooring.GetDefaultDefNameSnapshot(policy);
            workingSelection = new HashSet<string>(StringComparer.Ordinal);

            if (settings.IsFloorPolicyCustomized(policy))
            {
                foreach (string defName in settings.GetSavedFloorOverrideDefNames(policy))
                {
                    if (DefDatabase<TerrainDef>.GetNamedSilentFail(defName) != null)
                        workingSelection.Add(defName);
                }
            }
            else
            {
                foreach (string defName in defaultSnapshot)
                    workingSelection.Add(defName);
            }

            BuildAllRows();
        }

        public override void PostClose()
        {
            base.PostClose();
            FloorPickerTableHeader.CloseDropdown();
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;
            Text.Font = GameFont.Medium;
            LabelAnchored(new Rect(0f, y, inRect.width, 32f),
                $"Good floors: {FloorPolicyLabels.GetLabel(policy)}",
                TextAnchor.MiddleLeft);
            y += 36f;

            Text.Font = GameFont.Small;
            const float resetW = ResetBtnW;
            float tableRight = TableWidth;
            Rect toolbarRow = new Rect(0f, y, inRect.width, ToolbarRowH);
            GUI.color = Color.gray;
            LabelAnchored(new Rect(0f, toolbarRow.y, tableRight - resetW - 8f, ToolbarRowH),
                FloorPolicyLabels.GetDefaultSummaryLabel(policy),
                TextAnchor.MiddleLeft);
            GUI.color = Color.white;
            SettingsUiUtil.DrawRedButton(new Rect(tableRight - resetW, toolbarRow.y, resetW, ToolbarRowH),
                "Reset to defaults", ResetToDefaults);
            y += ToolbarRowH + 8f;

            RebuildVisibleRows();

            bool usingDefaults = Util_Flooring.SetsEqual(workingSelection, defaultSnapshot);
            string status = usingDefaults ? "Using defaults" : "Custom";
            int selectedGroups = 0;
            for (int i = 0; i < allRows.Count; i++)
            {
                if (allRows[i].IsFullySelected(workingSelection))
                    selectedGroups++;
            }
            string selectionLine = $"Selected: {selectedGroups} of {allRows.Count} ({status})";
            if (!usingDefaults)
                selectionLine = selectionLine.Colorize(Color.yellow);
            LabelAnchored(new Rect(0f, y, inRect.width, 22f), selectionLine, TextAnchor.MiddleLeft);
            y += 24f;

            scrollPos.x = 0f;
            tableWidth = TableWidth;

            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Rect hRect = new Rect(0f, y, tableWidth, HeaderHeight);
            DrawTableHeader(hRect);
            GUI.color = Color.white;
            Widgets.DrawLineHorizontal(0f, hRect.yMax, tableWidth);
            y = hRect.yMax + 4f;

            float listBottom = inRect.height - BottomH - 8f;
            float totalScrollHeight = visibleRows.Count * RowHeight;
            Rect scrollOuter = new Rect(0f, y, inRect.width, listBottom - y);
            Rect viewRect = new Rect(0f, 0f, tableWidth, Mathf.Max(totalScrollHeight, scrollOuter.height - 1f));

            Widgets.BeginScrollView(scrollOuter, ref scrollPos, viewRect);
            for (int i = 0; i < visibleRows.Count; i++)
                DrawTableRow(0f, i * RowHeight, visibleRows[i], i);
            Widgets.EndScrollView();

            Rect btnRow = new Rect(0f, inRect.height - BottomH, inRect.width, 36f);
            if (Widgets.ButtonText(btnRow.LeftHalf().ContractedBy(2f), "Cancel"))
                Close();
            if (Widgets.ButtonText(btnRow.RightHalf().ContractedBy(2f), "Accept"))
            {
                settings.ApplyFloorOverride(policy, workingSelection, defaultSnapshot);
                Close();
            }

            FloorPickerTableHeader.DrawDropdownIfOpen();
        }

        private void DrawTableHeader(Rect hRect)
        {
            float curX = 0f;

            DrawSelectHeader(ref curX, hRect);

            curX += ColImage;

            FloorPickerTableHeader.DrawFilterableHeader(
                ref curX, hRect.y, ColName, hRect.height,
                "Name",
                sortColumn == "Name", sortAscending,
                TextAnchor.MiddleLeft,
                !nameSearchTerm.NullOrEmpty(),
                "Filter by floor name",
                icon => FloorPickerTableHeader.OpenTextDropdown(
                    icon,
                    "Filter by name",
                    "Search floors...",
                    () => nameSearchTerm,
                    v => nameSearchTerm = v ?? "",
                    () => nameSearchTerm = ""),
                () => SetSort("Name"));

            FloorPickerTableHeader.DrawFilterableHeader(
                ref curX, hRect.y, ColData, hRect.height,
                "Material",
                sortColumn == "Material", sortAscending,
                TextAnchor.MiddleCenter,
                materialFilter.HasValue,
                "Filter by material",
                icon => FloorPickerTableHeader.OpenChoiceDropdown(
                    icon,
                    "Filter by material",
                    BuildMaterialChoices()),
                () => SetSort("Material"));

            FloorPickerTableHeader.DrawFilterableHeader(
                ref curX, hRect.y, ColData, hRect.height,
                "Buildable",
                sortColumn == "Buildable", sortAscending,
                TextAnchor.MiddleCenter,
                buildableFilter != BuildableColumnFilter.All,
                "Filter by buildable",
                icon => FloorPickerTableHeader.OpenChoiceDropdown(
                    icon,
                    "Filter by buildable",
                    BuildBuildableChoices()),
                () => SetSort("Buildable"));

            DrawSortOnlyHeader(ref curX, hRect, "Value", "Value", ColData);
            DrawSortOnlyHeader(ref curX, hRect, "Default", "Default", ColData);
        }

        private void DrawSelectHeader(ref float curX, Rect hRect)
        {
            Rect selHdr = new Rect(curX, hRect.y, ColSelect, hRect.height);
            if (Mouse.IsOver(selHdr)) Widgets.DrawHighlight(selHdr);

            const float iconSz = 18f;
            float iconX = selHdr.x + (ColSelect - iconSz) * 0.5f;
            float iconY = selHdr.y + (hRect.height - iconSz) * 0.5f;
            Rect iconRect = new Rect(iconX, iconY, iconSz, iconSz);

            bool disabled = visibleRows.Count == 0;
            bool allSelected = AreAllVisibleSelected();

            if (allSelected)
            {
                Texture2D? deleteTex = TexButton.Delete;
                if (deleteTex != null)
                {
                    GUI.color = disabled ? new Color(1f, 0.35f, 0.35f, 0.35f) : new Color(1f, 0.35f, 0.35f);
                    GUI.DrawTexture(iconRect, deleteTex, ScaleMode.ScaleToFit);
                    GUI.color = Color.white;
                }
                else
                {
                    LabelAnchored(iconRect, "X", TextAnchor.MiddleCenter);
                }
                TooltipHandler.TipRegion(selHdr, "Clear all visible floors");
            }
            else
            {
                Widgets.CheckboxDraw(iconRect.x, iconRect.y, true, disabled, iconSz);
                TooltipHandler.TipRegion(selHdr, "Select all visible floors");
            }

            if (!disabled && Widgets.ButtonInvisible(selHdr))
            {
                if (allSelected) ClearAllVisible();
                else SelectAllVisible();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            curX += ColSelect;
        }

        private bool AreAllVisibleSelected()
        {
            if (visibleRows.Count == 0) return false;
            foreach (var entry in visibleRows)
            {
                if (!entry.IsFullySelected(workingSelection))
                    return false;
            }
            return true;
        }

        private void DrawSortOnlyHeader(ref float curX, Rect hRect, string label, string tag, float width)
        {
            Rect headerRect = new Rect(curX, hRect.y, width, hRect.height);
            string arrow = sortColumn == tag ? (sortAscending ? " ▲" : " ▼") : "";
            string headerText = label + arrow;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(headerRect, headerText.Truncate(width - 4f));
            Text.Anchor = TextAnchor.UpperLeft;
            if (Mouse.IsOver(headerRect)) Widgets.DrawHighlight(headerRect);
            if (Widgets.ButtonInvisible(headerRect))
                SetSort(tag);
            curX += width;
        }

        private void DrawTableRow(float x, float y, FloorRowEntry entry, int rowIndex)
        {
            Rect rowRect = new Rect(x, y, tableWidth, RowHeight);
            if (rowIndex % 2 == 0) Widgets.DrawHighlight(rowRect);
            if (Mouse.IsOver(rowRect)) Widgets.DrawLightHighlight(rowRect);

            float rX = x;

            bool on = entry.IsFullySelected(workingSelection);
            Widgets.Checkbox(new Vector2(rX + (ColSelect - 24f) * 0.5f, y + (RowHeight - 24f) * 0.5f), ref on);
            entry.ApplySelection(workingSelection, on);
            rX += ColSelect;

            float contentY = y + RowHeight * 0.5f;
            Rect iconRect = new Rect(rX + (ColImage - RowIconSize) * 0.5f, contentY - RowIconSize * 0.5f, RowIconSize, RowIconSize);
            FloorTerrainUtil.DrawFloorIcon(iconRect, entry.Terrain);
            rX += ColImage;

            Rect nameRect = new Rect(rX + 4f, y, ColName - 8f, RowHeight);
            LabelAnchored(nameRect, entry.NameLabel.Truncate(ColName - 12f), TextAnchor.MiddleLeft);
            string tip = entry.MemberDefNames.Count <= 1
                ? entry.MemberDefNames[0]
                : $"{entry.NameLabel}\n{entry.MemberDefNames.Count} variants:\n" + string.Join("\n", entry.MemberDefNames);
            TooltipHandler.TipRegion(nameRect, tip);
            rX += ColName;

            LabelAnchored(new Rect(rX, y, ColData, RowHeight), entry.MaterialLabel, TextAnchor.MiddleCenter);
            rX += ColData;

            LabelAnchored(new Rect(rX, y, ColData, RowHeight),
                entry.Buildable ? "Yes" : "No",
                TextAnchor.MiddleCenter);
            rX += ColData;

            LabelAnchored(new Rect(rX, y, ColData, RowHeight), entry.ValueLabel, TextAnchor.MiddleCenter);
            rX += ColData;

            LabelAnchored(new Rect(rX, y, ColData, RowHeight),
                entry.IsDefault ? "Yes" : "No",
                TextAnchor.MiddleCenter);
        }

        private List<FloorPickerFilterChoice> BuildMaterialChoices()
        {
            return new List<FloorPickerFilterChoice>
            {
                new FloorPickerFilterChoice("All", !materialFilter.HasValue,
                    () => materialFilter = null, separatorAfter: true),
                new FloorPickerFilterChoice("Stone", materialFilter == FloorMaterialKind.Stone,
                    () => materialFilter = FloorMaterialKind.Stone),
                new FloorPickerFilterChoice("Metal", materialFilter == FloorMaterialKind.Metal,
                    () => materialFilter = FloorMaterialKind.Metal),
                new FloorPickerFilterChoice("Fabrics", materialFilter == FloorMaterialKind.Fabrics,
                    () => materialFilter = FloorMaterialKind.Fabrics),
                new FloorPickerFilterChoice("Smoothed", materialFilter == FloorMaterialKind.Smoothed,
                    () => materialFilter = FloorMaterialKind.Smoothed),
                new FloorPickerFilterChoice("Wood", materialFilter == FloorMaterialKind.Wood,
                    () => materialFilter = FloorMaterialKind.Wood),
                new FloorPickerFilterChoice("Other", materialFilter == FloorMaterialKind.Other,
                    () => materialFilter = FloorMaterialKind.Other)
            };
        }

        private List<FloorPickerFilterChoice> BuildBuildableChoices()
        {
            return new List<FloorPickerFilterChoice>
            {
                new FloorPickerFilterChoice("All", buildableFilter == BuildableColumnFilter.All,
                    () => buildableFilter = BuildableColumnFilter.All, separatorAfter: true),
                new FloorPickerFilterChoice("Yes", buildableFilter == BuildableColumnFilter.Yes,
                    () => buildableFilter = BuildableColumnFilter.Yes),
                new FloorPickerFilterChoice("No", buildableFilter == BuildableColumnFilter.No,
                    () => buildableFilter = BuildableColumnFilter.No)
            };
        }

        private void SetSort(string col)
        {
            if (sortColumn == col) sortAscending = !sortAscending;
            else
            {
                sortColumn = col;
                sortAscending = true;
            }
            SoundDefOf.Click.PlayOneShotOnCamera();
        }

        private void BuildAllRows()
        {
            allRows.Clear();
            var groups = new Dictionary<string, List<TerrainDef>>(StringComparer.Ordinal);
            var groupOrder = new List<string>();

            foreach (var terrain in Util_Flooring.GetFloorCandidates())
            {
                string key = FloorTerrainUtil.GetPickerGroupKey(terrain);
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<TerrainDef>();
                    groups[key] = list;
                    groupOrder.Add(key);
                }
                list.Add(terrain);
            }

            foreach (string key in groupOrder)
            {
                var members = groups[key];
                members.Sort((a, b) => string.Compare(a.defName, b.defName, StringComparison.OrdinalIgnoreCase));
                TerrainDef rep = members[0];
                var kind = FloorTerrainUtil.GetMaterialKind(rep);
                var marketValue = FloorTerrainUtil.GetMarketValue(rep);
                var memberNames = new List<string>(members.Count);
                bool allDefault = true;
                bool anyBuildable = false;
                for (int i = 0; i < members.Count; i++)
                {
                    memberNames.Add(members[i].defName);
                    if (!defaultSnapshot.Contains(members[i].defName))
                        allDefault = false;
                    if (FloorTerrainUtil.IsPlayerBuildable(members[i]))
                        anyBuildable = true;
                }

                allRows.Add(new FloorRowEntry
                {
                    Terrain = rep,
                    Members = members,
                    MemberDefNames = memberNames,
                    GroupKey = key,
                    NameLabel = FloorTerrainUtil.GetPickerGroupLabel(rep, members),
                    MaterialKind = kind,
                    MaterialLabel = FloorTerrainUtil.GetMaterialLabel(kind),
                    Buildable = anyBuildable,
                    MarketValue = marketValue,
                    ValueLabel = FloorTerrainUtil.FormatMarketValue(rep),
                    IsDefault = allDefault
                });
            }

            NormalizeSelectionToGroups();
        }

        /// <summary>If any variant of a group is selected, select the whole group (keeps overrides consistent).</summary>
        private void NormalizeSelectionToGroups()
        {
            foreach (var entry in allRows)
            {
                bool any = false;
                for (int i = 0; i < entry.MemberDefNames.Count; i++)
                {
                    if (workingSelection.Contains(entry.MemberDefNames[i]))
                    {
                        any = true;
                        break;
                    }
                }
                if (any)
                    entry.ApplySelection(workingSelection, true);
            }
        }

        private void RebuildVisibleRows()
        {
            visibleRows.Clear();
            string q = nameSearchTerm?.Trim() ?? string.Empty;
            string? qLower = q.Length > 0 ? q.ToLowerInvariant() : null;

            foreach (var entry in allRows)
            {
                if (qLower != null)
                {
                    bool nameHit = entry.NameLabel.ToLowerInvariant().IndexOf(qLower, StringComparison.Ordinal) >= 0;
                    bool defHit = false;
                    if (!nameHit)
                    {
                        for (int i = 0; i < entry.MemberDefNames.Count; i++)
                        {
                            if (entry.MemberDefNames[i].ToLowerInvariant().IndexOf(qLower, StringComparison.Ordinal) >= 0)
                            {
                                defHit = true;
                                break;
                            }
                        }
                    }
                    if (!nameHit && !defHit)
                        continue;
                }
                if (materialFilter.HasValue && entry.MaterialKind != materialFilter.Value)
                    continue;
                if (buildableFilter == BuildableColumnFilter.Yes && !entry.Buildable)
                    continue;
                if (buildableFilter == BuildableColumnFilter.No && entry.Buildable)
                    continue;
                visibleRows.Add(entry);
            }

            visibleRows.Sort(CompareRows);
        }

        private int CompareRows(FloorRowEntry a, FloorRowEntry b)
        {
            int cmp = sortColumn switch
            {
                "Name" => string.Compare(a.NameLabel, b.NameLabel, StringComparison.OrdinalIgnoreCase),
                "Material" => string.Compare(a.MaterialLabel, b.MaterialLabel, StringComparison.OrdinalIgnoreCase),
                "Buildable" => a.Buildable.CompareTo(b.Buildable),
                "Value" => Nullable.Compare(a.MarketValue, b.MarketValue),
                "Default" => a.IsDefault.CompareTo(b.IsDefault),
                _ => string.Compare(a.NameLabel, b.NameLabel, StringComparison.OrdinalIgnoreCase)
            };
            return sortAscending ? cmp : -cmp;
        }

        private static void LabelAnchored(Rect rect, string text, TextAnchor anchor)
        {
            TextAnchor prev = Text.Anchor;
            Text.Anchor = anchor;
            Widgets.Label(rect, text);
            Text.Anchor = prev;
        }

        private void ResetToDefaults()
        {
            workingSelection.Clear();
            foreach (string defName in defaultSnapshot)
                workingSelection.Add(defName);
        }

        private void SelectAllVisible()
        {
            foreach (var entry in visibleRows)
                entry.ApplySelection(workingSelection, true);
        }

        private void ClearAllVisible()
        {
            foreach (var entry in visibleRows)
                entry.ApplySelection(workingSelection, false);
        }
    }
}
