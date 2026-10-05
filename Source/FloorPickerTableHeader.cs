using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TSA.RoomsAndFlooringMatter
{
    /// <summary>
    /// Table header filter icon, slate choice tiles, and anchored dropdowns for the floor picker.
    /// </summary>
    public static class FloorPickerTableHeader
    {
        public const float FilterIconSize = 22f;
        public const float TileH = 28f;
        public const float FieldH = 28f;
        public const float Pad = 10f;
        public const float TitleH = 24f;
        public const float ClearBtnH = 28f;
        public const float DefaultDropdownW = 260f;

        private static readonly Texture2D FilterIconTex =
            ContentFinder<Texture2D>.Get("UI/Buttons/OpenSpecificTab", false)
            ?? TexButton.Search
            ?? TexButton.Info;

        private static readonly Color ActiveFilterTint = new Color(0.45f, 0.85f, 1f);
        private static readonly Color IdleFilterTint = new Color(0.85f, 0.85f, 0.85f);

        private static readonly Color NavSlateFill = new Color(0.16f, 0.18f, 0.22f, 0.92f);
        private static readonly Color NavBtnBgHover = new Color(0.22f, 0.26f, 0.32f, 0.96f);
        private static readonly Color NavBtnBgPress = new Color(0.12f, 0.14f, 0.17f, 0.96f);
        private static readonly Color NavBtnBgSelected = new Color(0.22f, 0.26f, 0.32f, 0.96f);
        private static readonly Color NavBtnOutline = new Color(0.55f, 0.62f, 0.72f, 0.42f);
        private static readonly Color NavBtnOutlineHover = new Color(0.78f, 0.84f, 0.92f, 0.72f);
        private static readonly Color NavBtnOutlineSelected = new Color(0.70f, 0.76f, 0.86f, 0.55f);

        private const int DropdownWindowId = 918273646;
        private const string TextControlName = "FloorPickerTableHeader_Text";

        private static bool dropdownOpen;
        private static Rect dropdownScreenRect;
        private static Action<Rect>? dropdownDraw;
        private static bool focusTextNext;
        private static int openedOnFrame = -1;
        private static Vector2 dropdownListScroll;

        public static bool IsDropdownOpen => dropdownOpen;

        public static void CloseDropdown()
        {
            dropdownOpen = false;
            dropdownDraw = null;
            focusTextNext = false;
            dropdownListScroll = Vector2.zero;
        }

        public static void DrawDropdownIfOpen()
        {
            if (!dropdownOpen || dropdownDraw == null) return;
            Rect r = dropdownScreenRect;
            Find.WindowStack.ImmediateWindow(
                DropdownWindowId,
                r,
                WindowLayer.Super,
                () => dropdownDraw?.Invoke(r.AtZero()),
                doBackground: true,
                absorbInputAroundWindow: true,
                shadowAlpha: 1f,
                doClickOutsideFunc: () =>
                {
                    if (Time.frameCount <= openedOnFrame) return;
                    CloseDropdown();
                });
        }

        public static bool DrawSlateChoice(Rect r, string label, bool selected, string? tip = null)
        {
            bool mouseOver = Mouse.IsOver(r);
            bool pressed = mouseOver && Input.GetMouseButton(0);
            Color bg = selected ? NavBtnBgSelected : pressed ? NavBtnBgPress : mouseOver ? NavBtnBgHover : NavSlateFill;
            Widgets.DrawBoxSolid(r, bg);
            GUI.color = selected ? NavBtnOutlineSelected : mouseOver ? NavBtnOutlineHover : NavBtnOutline;
            Widgets.DrawBox(r, 1);
            GUI.color = Color.white;

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(r, label ?? "");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            if (!tip.NullOrEmpty())
                TooltipHandler.TipRegion(r, tip);
            return Widgets.ButtonInvisible(r);
        }

        public static bool DrawFilterableHeader(
            ref float curX,
            float y,
            float width,
            float height,
            string label,
            bool isSorted,
            bool sortAscending,
            TextAnchor labelAnchor,
            bool filterActive,
            string? filterTip,
            Action<Rect>? onFilterClick,
            Action? onSort)
        {
            Rect headerRect = new Rect(curX, y, width, height);
            const float gap = 2f;
            const float edgePad = 2f;
            bool showFilter = onFilterClick != null;

            if (!showFilter)
            {
                if (!label.NullOrEmpty())
                {
                    string arrow = isSorted ? (sortAscending ? " ▲" : " ▼") : "";
                    string headerText = label + arrow;
                    float maxTextW = Mathf.Max(8f, width - edgePad * 2f);
                    if (Text.CalcSize(headerText).x > maxTextW)
                        headerText = label.Truncate(Mathf.Max(8f, maxTextW - 16f)) + arrow;
                    Rect labelRect = IsLeftAnchor(labelAnchor)
                        ? new Rect(headerRect.x + edgePad, y, headerRect.width - edgePad, height)
                        : headerRect;
                    Text.Anchor = labelAnchor;
                    Widgets.Label(labelRect, headerText);
                    Text.Anchor = TextAnchor.UpperLeft;
                }

                if (onSort != null)
                {
                    if (Mouse.IsOver(headerRect)) Widgets.DrawHighlight(headerRect);
                    if (Widgets.ButtonInvisible(headerRect))
                        onSort.Invoke();
                }

                curX += width;
                return false;
            }

            float icon = Mathf.Min(FilterIconSize, height - 2f);
            float iconY = y + (height - icon) * 0.5f;
            Rect iconRect;

            if (label.NullOrEmpty())
            {
                iconRect = new Rect(headerRect.x + (width - icon) * 0.5f, iconY, icon, icon);
            }
            else
            {
                string arrow = isSorted ? (sortAscending ? " ▲" : " ▼") : "";
                string headerText = label + arrow;
                float maxTextW = Mathf.Max(8f, width - icon - gap - edgePad * 2f);
                float textW = Text.CalcSize(headerText).x;
                if (textW > maxTextW)
                {
                    headerText = label.Truncate(Mathf.Max(8f, maxTextW - 16f)) + arrow;
                    textW = Mathf.Min(maxTextW, Text.CalcSize(headerText).x);
                }

                float clusterW = textW + gap + icon;
                float clusterX;
                if (IsRightAnchor(labelAnchor))
                    clusterX = headerRect.xMax - clusterW - edgePad;
                else if (IsLeftAnchor(labelAnchor))
                    clusterX = headerRect.x + edgePad;
                else
                    clusterX = headerRect.x + (width - clusterW) * 0.5f;
                clusterX = Mathf.Clamp(clusterX, headerRect.x + edgePad, Mathf.Max(headerRect.x + edgePad, headerRect.xMax - clusterW - edgePad));

                Rect labelRect = new Rect(clusterX, y, textW, height);
                iconRect = new Rect(clusterX + textW + gap, iconY, icon, icon);
                Text.Anchor = LeftAlignedVertical(labelAnchor);
                Widgets.Label(labelRect, headerText);
                Text.Anchor = TextAnchor.UpperLeft;
            }

            bool overIcon = Mouse.IsOver(iconRect);
            bool overSort = onSort != null && Mouse.IsOver(headerRect) && !overIcon;
            if (overSort) Widgets.DrawHighlight(headerRect);
            if (onSort != null && !overIcon && Widgets.ButtonInvisible(headerRect))
                onSort.Invoke();

            if (Mouse.IsOver(iconRect)) Widgets.DrawHighlight(iconRect);
            Color prev = GUI.color;
            GUI.color = filterActive ? ActiveFilterTint : IdleFilterTint;
            if (FilterIconTex != null)
                GUI.DrawTexture(iconRect, FilterIconTex, ScaleMode.ScaleToFit);
            GUI.color = prev;
            if (!filterTip.NullOrEmpty())
                TooltipHandler.TipRegion(iconRect, filterTip);
            bool filterClicked = Widgets.ButtonInvisible(iconRect);
            if (filterClicked)
            {
                onFilterClick!.Invoke(iconRect);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            curX += width;
            return filterClicked;
        }

        public static void OpenTextDropdown(
            Rect guiAnchor,
            string title,
            string hint,
            Func<string> get,
            Action<string> onChanged,
            Action onCleared,
            float width = DefaultDropdownW)
        {
            float h = Pad * 2f + TitleH + 12f + FieldH + 6f + ClearBtnH;
            Open(guiAnchor, width, h, inner =>
            {
                float y = DrawDropdownTitle(inner, title);
                Rect field = new Rect(inner.x, y, inner.width, FieldH);
                DrawSearchField(field, hint, get?.Invoke() ?? "", onChanged, TextControlName, requestFocus: true);
                Rect clear = new Rect(inner.x, field.yMax + 6f, inner.width, ClearBtnH);
                if (Widgets.ButtonText(clear, "Clear filter"))
                {
                    onCleared?.Invoke();
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
            });
        }

        public static void OpenChoiceDropdown(
            Rect guiAnchor,
            string title,
            IReadOnlyList<FloorPickerFilterChoice> choices,
            float width = DefaultDropdownW)
        {
            if (choices == null || choices.Count == 0) return;
            int separators = 0;
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i].SeparatorAfter) separators++;
            }
            float listH = choices.Count * (TileH + 4f) + separators * 10f;
            float maxListH = 12f * (TileH + 4f);
            bool needScroll = listH > maxListH + 1f;
            float shownListH = needScroll ? maxListH : listH;
            float h = Pad * 2f + TitleH + 12f + shownListH;
            Open(guiAnchor, width, h, inner =>
            {
                float y = DrawDropdownTitle(inner, title);
                if (needScroll)
                {
                    Rect outer = new Rect(inner.x, y, inner.width, shownListH);
                    Rect view = new Rect(0f, 0f, inner.width - 16f, listH);
                    Widgets.BeginScrollView(outer, ref dropdownListScroll, view);
                    DrawChoiceTiles(new Rect(0f, 0f, view.width, listH), choices);
                    Widgets.EndScrollView();
                }
                else
                {
                    DrawChoiceTiles(new Rect(inner.x, y, inner.width, listH), choices);
                }
            });
        }

        private static void DrawChoiceTiles(Rect area, IReadOnlyList<FloorPickerFilterChoice> choices)
        {
            float y = area.y;
            for (int i = 0; i < choices.Count; i++)
            {
                FloorPickerFilterChoice c = choices[i];
                Rect tile = new Rect(area.x, y, area.width, TileH);
                if (DrawSlateChoice(tile, c.Label, c.Selected, c.Tip))
                {
                    c.OnPick?.Invoke();
                    CloseDropdown();
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
                y += TileH + 4f;
                if (c.SeparatorAfter)
                {
                    Color prev = GUI.color;
                    GUI.color = Color.white;
                    Widgets.DrawLineHorizontal(area.x, y + 2f, area.width);
                    GUI.color = prev;
                    y += 10f;
                }
            }
        }

        private static bool IsLeftAnchor(TextAnchor a) =>
            a == TextAnchor.UpperLeft || a == TextAnchor.MiddleLeft || a == TextAnchor.LowerLeft;

        private static bool IsRightAnchor(TextAnchor a) =>
            a == TextAnchor.UpperRight || a == TextAnchor.MiddleRight || a == TextAnchor.LowerRight;

        private static TextAnchor LeftAlignedVertical(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    return TextAnchor.UpperLeft;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    return TextAnchor.LowerLeft;
                default:
                    return TextAnchor.MiddleLeft;
            }
        }

        private static float DrawDropdownTitle(Rect inner, string title)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, TitleH), title ?? "");
            Text.Anchor = TextAnchor.UpperLeft;
            float lineY = inner.y + TitleH + 2f;
            Color prev = GUI.color;
            GUI.color = Color.white;
            Widgets.DrawLineHorizontal(inner.x, lineY, inner.width);
            GUI.color = prev;
            return lineY + 8f;
        }

        private static Rect GuiRectToScreen(Rect gui)
        {
            Vector2 topLeft = UI.GUIToScreenPoint(new Vector2(gui.x, gui.y));
            Vector2 bottomRight = UI.GUIToScreenPoint(new Vector2(gui.xMax, gui.yMax));
            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }

        private static void Open(Rect guiAnchor, float width, float height, Action<Rect> draw)
        {
            Rect iconScreen = GuiRectToScreen(guiAnchor);
            float winW = width;
            float winH = height;
            float x = iconScreen.xMax - winW;
            float y = iconScreen.yMax;
            if (x < 8f) x = 8f;
            if (x + winW > UI.screenWidth - 8f)
                x = UI.screenWidth - winW - 8f;
            if (y + winH > UI.screenHeight - 8f)
                y = iconScreen.y - winH;
            if (y < 8f) y = 8f;

            dropdownScreenRect = new Rect(x, y, winW, winH);
            dropdownDraw = win =>
            {
                Rect inner = win.ContractedBy(Pad);
                draw?.Invoke(inner);
            };
            dropdownOpen = true;
            focusTextNext = true;
            dropdownListScroll = Vector2.zero;
            openedOnFrame = Time.frameCount;
        }

        private static void DrawSearchField(
            Rect rect,
            string hint,
            string current,
            Action<string> onChanged,
            string controlName,
            bool requestFocus)
        {
            string old = current ?? "";
            if (requestFocus && focusTextNext)
            {
                GUI.FocusControl(controlName);
                if (Event.current.type == EventType.Repaint)
                    focusTextNext = false;
            }
            GUI.SetNextControlName(controlName);
            Text.Font = GameFont.Small;
            string next = Widgets.TextField(rect, old);
            if (string.IsNullOrEmpty(next) && !hint.NullOrEmpty())
            {
                GUI.color = new Color(1f, 1f, 1f, 0.4f);
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Tiny;
                Widgets.Label(rect, "  " + hint);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            if (next != old)
                onChanged?.Invoke(next);
        }
    }

    public struct FloorPickerFilterChoice
    {
        public string Label;
        public bool Selected;
        public string? Tip;
        public Action? OnPick;
        public bool SeparatorAfter;

        public FloorPickerFilterChoice(string label, bool selected, Action? onPick, string? tip = null, bool separatorAfter = false)
        {
            Label = label;
            Selected = selected;
            OnPick = onPick;
            Tip = tip;
            SeparatorAfter = separatorAfter;
        }
    }
}
