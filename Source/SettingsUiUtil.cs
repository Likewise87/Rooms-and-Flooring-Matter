using System;
using UnityEngine;
using Verse;

namespace TSA.RoomsAndFlooringMatter
{
    public static class SettingsUiUtil
    {
        public static readonly Color SectionHeaderColor = new Color(0.55f, 0.85f, 1f);

        public static bool DrawCollapsibleHeader(Listing_Standard l, string label, ref bool expanded, Color? color = null, string? tip = null)
        {
            l.Gap(12f);
            Rect r = l.GetRect(26f);
            Widgets.DrawHighlightIfMouseover(r);
            if (!tip.NullOrEmpty()) TooltipHandler.TipRegion(r, tip);
            if (Widgets.ButtonInvisible(r)) expanded = !expanded;
            Color c = color ?? Color.white;
            string arrow = expanded ? "▼" : "▶";
            Widgets.Label(r, $"<b><color=#{ColorUtility.ToHtmlStringRGBA(c)}>{arrow}  {label}</color></b>");
            if (expanded) { l.GapLine(6f); l.Gap(4f); }
            else l.Gap(4f);
            return expanded;
        }

        public static void DrawMenuTopBar(Listing_Standard l, string resetLabel, Action? onReset, Action? expandAll, Action? collapseAll)
        {
            if (onReset == null) return;
            l.Gap(4f);
            Rect row = l.GetRect(30f);
            float sideBtnW = 110f;
            float rightPairW = sideBtnW * 2f + 8f;
            float resetW = Mathf.Min(250f, row.width - rightPairW - 24f);
            if (resetW < 120f) resetW = Mathf.Max(100f, row.width * 0.35f);
            Rect resetRect = new Rect(row.x, row.y, resetW, row.height);
            Rect collapseRect = new Rect(row.xMax - sideBtnW, row.y, sideBtnW, row.height);
            Rect expandRect = new Rect(collapseRect.x - 8f - sideBtnW, row.y, sideBtnW, row.height);
            DrawRedButton(resetRect, resetLabel, onReset);
            if (expandAll != null && Widgets.ButtonText(expandRect, "Expand all")) expandAll();
            if (collapseAll != null && Widgets.ButtonText(collapseRect, "Collapse all")) collapseAll();
            l.Gap(6f);
        }

        public static void DrawRedButton(Rect rect, string label, Action onClick)
        {
            Color prev = GUI.color;
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (Widgets.ButtonText(rect, label)) onClick();
            GUI.color = prev;
        }

        public static string FormatValue(float val, SliderFormat format) => format switch
        {
            SliderFormat.Fixed0 => val.ToString("F0"),
            SliderFormat.Fixed1 => val.ToString("F1"),
            SliderFormat.Fixed2 => val.ToString("F2"),
            SliderFormat.Percent => (val * 100f).ToString("F0") + " %",
            SliderFormat.PercentDecimal => (val * 100f).ToString("F1") + " %",
            _ => val.ToString()
        };

        public static string TooltipWithDefault(string tooltip, float? defaultValue, SliderFormat format = SliderFormat.Fixed1)
        {
            if (!defaultValue.HasValue) return tooltip ?? string.Empty;
            string line = $"Default value: {FormatValue(defaultValue.Value, format)}";
            return tooltip.NullOrEmpty() ? line : tooltip + "\n" + line;
        }

        public static string PrefixSettingTooltip(string tooltip, bool isCustom)
        {
            string prefix = isCustom ? "Custom settings:" : "Default settings:";
            return tooltip.NullOrEmpty() ? prefix : prefix + "\n" + tooltip;
        }

        public static string TooltipForSetting(string? tooltip, float val, float? defaultValue, SliderFormat format = SliderFormat.Fixed1)
        {
            bool modified = ValueDiffersFromDefault(val, defaultValue, format);
            return PrefixSettingTooltip(TooltipWithDefault(tooltip, defaultValue, format), modified);
        }

        public static string AppendDefault(string tooltip, string def)
        {
            string line = $"Default: {def}";
            return tooltip.NullOrEmpty() ? line : tooltip + "\n" + line;
        }

        public static string ColorizeIfModified(string text, bool modified) =>
            modified ? text.Colorize(Color.yellow) : text.Colorize(Color.cyan);

        public static bool ValueDiffersFromDefault(float val, float? defaultValue, SliderFormat format)
        {
            if (!defaultValue.HasValue) return false;
            return format switch
            {
                SliderFormat.Fixed0 => Mathf.RoundToInt(val) != Mathf.RoundToInt(defaultValue.Value),
                SliderFormat.Percent or SliderFormat.PercentDecimal =>
                    !Mathf.Approximately(val, defaultValue.Value),
                _ => !Mathf.Approximately(val, defaultValue.Value)
            };
        }

        public static float LabeledSlider(Listing_Standard l, string label, float val, float min, float max, string? tooltip = null, float step = 0.1f, SliderFormat format = SliderFormat.Fixed1, float? defaultValue = null)
        {
            l.Gap(2f);
            Rect r = l.GetRect(24f);
            TooltipHandler.TipRegion(r, TooltipForSetting(tooltip, val, defaultValue, format));
            string valueText = FormatValue(val, format);
            bool modified = ValueDiffersFromDefault(val, defaultValue, format);
            Widgets.Label(r.LeftPart(0.5f), $"{label}: {ColorizeIfModified(valueText, modified)}");
            return Widgets.HorizontalSlider(r.RightPart(0.5f), val, min, max, false, null, null, null, step);
        }
    }
}
