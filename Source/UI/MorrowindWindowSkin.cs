using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.UI;

public static class MorrowindWindowSkin
{
    public static void DrawWindow(Rect rect)
    {
        DrawDarkFill(rect, MorrowindUiResources.BackgroundTint);
        DrawOuterFrame(rect);
    }

    public static void DrawPanel(Rect rect, float inset = 6f, bool darkFill = true)
    {
        DrawDarkFill(rect, MorrowindUiResources.PanelShade);
        DrawSimpleFrame(rect);
        if (darkFill && inset > 0f)
        {
            DrawDarkFill(rect.ContractedBy(inset), MorrowindUiResources.PanelShadeSoft);
        }
    }

    public static void DrawSlot(Rect rect, bool selected)
    {
        Color old = GUI.color;
        GUI.color = MorrowindUiResources.SlotShade;
        GUI.DrawTexture(rect, BaseContent.WhiteTex);

        GUI.color = MorrowindUiResources.SlotBorderDark;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), BaseContent.WhiteTex);

        GUI.color = MorrowindUiResources.SlotBorderLight;
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.yMax - 2f, rect.width - 2f, 1f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, 1f, rect.height - 2f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.y + 1f, 1f, rect.height - 2f), BaseContent.WhiteTex);

        if (selected)
        {
            GUI.color = MorrowindUiResources.SelectedOverlay;
            GUI.DrawTexture(rect.ContractedBy(2f), BaseContent.WhiteTex);
        }

        GUI.color = old;
    }

    public static void DrawEquippedOutline(Rect rect)
    {
        Color old = GUI.color;
        GUI.color = MorrowindUiResources.Gold;
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 2f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.yMax - 3f, rect.width - 2f, 2f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, 2f, rect.height - 2f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.xMax - 3f, rect.y + 1f, 2f, rect.height - 2f), BaseContent.WhiteTex);
        GUI.color = old;
    }

    public static void DrawInsetFill(Rect rect)
    {
        DrawDarkFill(rect, MorrowindUiResources.PanelShade);
    }

    public static void DrawFlatPanel(Rect rect, Color? fillOverride = null)
    {
        DrawDarkFill(rect, fillOverride ?? MorrowindUiResources.PanelShadeSoft);
    }

    public static void DrawMainButton(Rect rect, bool mouseOver)
    {
        DrawDarkFill(rect, mouseOver ? MorrowindUiResources.PanelShadeSoft : MorrowindUiResources.PanelShade);
        DrawSimpleFrame(rect);
        if (mouseOver)
        {
            Color old = GUI.color;
            GUI.color = MorrowindUiResources.SelectedOverlay;
            GUI.DrawTexture(rect.ContractedBy(3f), BaseContent.WhiteTex);
            GUI.color = old;
        }
    }

    public static void DrawTextButton(Rect rect, string label, bool mouseOver, bool active)
    {
        Color fill = active
            ? (mouseOver ? MorrowindUiResources.PanelShadeSoft : MorrowindUiResources.PanelShade)
            : new Color(MorrowindUiResources.PanelShade.r * 0.75f, MorrowindUiResources.PanelShade.g * 0.75f, MorrowindUiResources.PanelShade.b * 0.75f, MorrowindUiResources.PanelShade.a);

        DrawDarkFill(rect, fill);
        DrawSimpleFrame(rect);

        if (mouseOver && active)
        {
            Color old = GUI.color;
            GUI.color = MorrowindUiResources.SelectedOverlay;
            GUI.DrawTexture(rect.ContractedBy(3f), BaseContent.WhiteTex);
            GUI.color = old;
        }

        Color textColor = active
            ? (mouseOver ? MorrowindUiResources.TextPrimary : MorrowindUiResources.Gold)
            : MorrowindUiResources.GoldDark;
        DrawCenteredText(rect.ContractedBy(4f), label, textColor);
    }

    public static void DrawIconButton(Rect rect, Texture icon, bool mouseOver, bool active)
    {
        if (icon == null)
        {
            return;
        }

        Rect iconRect = rect.ContractedBy(Mathf.Max(1f, Mathf.Min(rect.width, rect.height) * 0.08f));
        Color old = GUI.color;
        GUI.color = active
            ? (mouseOver ? MorrowindUiResources.GoldSoft : MorrowindUiResources.Gold)
            : MorrowindUiResources.GoldDark;
        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
        GUI.color = old;
    }

    public static void DrawOutlinedIconButton(Rect rect, Texture icon, bool mouseOver, bool active)
    {
        if (icon == null)
        {
            return;
        }

        DrawDarkFill(rect, Color.black);
        Rect iconRect = rect.ContractedBy(Mathf.Max(1f, Mathf.Min(rect.width, rect.height) * 0.08f));
        Color old = GUI.color;
        GUI.color = active
            ? (mouseOver ? MorrowindUiResources.GoldSoft : MorrowindUiResources.Gold)
            : MorrowindUiResources.GoldDark;
        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
        GUI.color = old;
    }

    public static void DrawCenteredText(Rect rect, string label, Color color)
    {
        DrawCenteredText(rect, label, color, GameFont.Small);
    }

    public static void DrawCenteredText(Rect rect, string label, Color color, GameFont font)
    {
        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        Color oldColor = GUI.color;
        TextAnchor oldAnchor = Text.Anchor;
        GameFont oldFont = Text.Font;
        GUI.color = color;
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = font;
        Widgets.Label(rect, label);
        Text.Anchor = oldAnchor;
        Text.Font = oldFont;
        GUI.color = oldColor;
    }


    public static void DrawMainButtonLabel(Rect rect, string label, Color color)
    {
        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        string displayLabel = label.Replace(" ", "\n");
        GameFont font = displayLabel.Length > 10 ? GameFont.Tiny : GameFont.Small;

        Color oldColor = GUI.color;
        TextAnchor oldAnchor = Text.Anchor;
        GameFont oldFont = Text.Font;
        bool oldWordWrap = Text.WordWrap;

        GUI.color = color;
        Text.Anchor = TextAnchor.UpperCenter;
        Text.Font = font;
        Text.WordWrap = true;
        Widgets.Label(rect, displayLabel);

        Text.WordWrap = oldWordWrap;
        Text.Anchor = oldAnchor;
        Text.Font = oldFont;
        GUI.color = oldColor;
    }

    public static void DrawCommandGizmo(Rect rect, Texture icon, string label, string hotkeyLabel, Color iconColor, bool mouseOver, bool disabled)
    {
        DrawDarkFill(rect, Color.black);

        if (mouseOver && !disabled)
        {
            Color old = GUI.color;
            GUI.color = new Color(MorrowindUiResources.Gold.r, MorrowindUiResources.Gold.g, MorrowindUiResources.Gold.b, 0.06f);
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = old;
        }

        if (icon != null)
        {
            float iconInsetX = Mathf.Clamp(rect.width * 0.16f, 8f, 14f);
            Rect iconRect = new Rect(rect.x + iconInsetX, rect.y + 10f, rect.width - (iconInsetX * 2f), 36f);
            Color old = GUI.color;
            GUI.color = iconColor;
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
            GUI.color = old;
        }

        if (!string.IsNullOrEmpty(hotkeyLabel))
        {
            Rect hotkeyRect = new Rect(rect.x + 4f, rect.y + 2f, Mathf.Min(rect.width * 0.4f, 22f), 18f);
            DrawCenteredText(hotkeyRect, hotkeyLabel, disabled ? MorrowindUiResources.GoldDark : MorrowindUiResources.Gold, GameFont.Small);
        }

        if (!string.IsNullOrEmpty(label))
        {
            Rect labelRect = new Rect(rect.x + 3f, rect.yMax - 24f, rect.width - 6f, 20f);
            Color oldColor = GUI.color;
            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            GUI.color = disabled ? MorrowindUiResources.GoldDark : MorrowindUiResources.Gold;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            Widgets.Label(labelRect, label);
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            GUI.color = oldColor;
        }
    }

    public static void DrawArchitectCategoryButton(Rect rect, string label, bool active, bool mouseOver, GameFont font)
    {
        Color textColor = active
            ? MorrowindUiResources.Gold
            : (mouseOver ? MorrowindUiResources.GoldSoft : MorrowindUiResources.Gold);

        if (mouseOver)
        {
            Color old = GUI.color;
            GUI.color = new Color(MorrowindUiResources.Gold.r, MorrowindUiResources.Gold.g, MorrowindUiResources.Gold.b, 0.08f);
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = old;
        }

        DrawCenteredText(rect, label, textColor, font);
    }


    public static void DrawCheckbox(Rect rect, bool isChecked, bool mouseOver, bool disabled)
    {
        DrawDarkFill(rect, disabled ? MorrowindUiResources.PanelShade : MorrowindUiResources.PanelShadeSoft);
        DrawSimpleFrame(rect);

        if (mouseOver && !disabled)
        {
            Color old = GUI.color;
            GUI.color = MorrowindUiResources.SelectedOverlay;
            GUI.DrawTexture(rect.ContractedBy(3f), BaseContent.WhiteTex);
            GUI.color = old;
        }

        if (!isChecked)
        {
            return;
        }

        Color oldColor = GUI.color;
        GUI.color = disabled ? MorrowindUiResources.GoldDark : MorrowindUiResources.Gold;
        Rect inner = rect.ContractedBy(Mathf.Max(5f, Mathf.Min(rect.width, rect.height) * 0.22f));
        DrawDiagonalLine(new Vector2(inner.x + (inner.width * 0.28f), inner.center.y + 1f), inner.width * 0.42f, 2f, 45f);
        DrawDiagonalLine(new Vector2(inner.x + (inner.width * 0.60f), inner.center.y - 1f), inner.width * 0.9f, 2f, -45f);
        GUI.color = oldColor;
    }

    public static void DrawTextFieldFrame(Rect rect, bool focused)
    {
        DrawDarkFill(rect, focused ? MorrowindUiResources.PanelShade : MorrowindUiResources.PanelShadeSoft);
        DrawSimpleFrame(rect);
        if (focused)
        {
            Color old = GUI.color;
            GUI.color = MorrowindUiResources.SelectedOverlay;
            GUI.DrawTexture(rect.ContractedBy(3f), BaseContent.WhiteTex);
            GUI.color = old;
        }
    }

    public static void DrawSliderOverlay(Rect rect, float normalized)
    {
        Rect trackRect = new Rect(rect.x, rect.center.y - 4f, rect.width, 8f);
        DrawDarkFill(trackRect, MorrowindUiResources.PanelShadeSoft);
        DrawSimpleFrame(trackRect.ExpandedBy(1f));

        float knobWidth = Mathf.Clamp(rect.width * 0.08f, 10f, 18f);
        float knobX = Mathf.Lerp(rect.x + 2f, rect.xMax - knobWidth - 2f, Mathf.Clamp01(normalized));
        Rect knobRect = new Rect(knobX, rect.center.y - 10f, knobWidth, 20f);
        DrawDarkFill(knobRect, MorrowindUiResources.PanelShade);
        DrawSimpleFrame(knobRect);
    }

    public static void DrawScrollbarOverlay(Rect rect, float normalized, float sizeFraction, bool vertical)
    {
        DrawDarkFill(rect, MorrowindUiResources.PanelShadeSoft);
        DrawSimpleFrame(rect);

        normalized = Mathf.Clamp01(normalized);
        sizeFraction = Mathf.Clamp(sizeFraction, 0.12f, 0.85f);
        Rect thumbRect;
        if (vertical)
        {
            float thumbHeight = Mathf.Max(14f, (rect.height - 4f) * sizeFraction);
            float y = Mathf.Lerp(rect.y + 2f, rect.yMax - thumbHeight - 2f, normalized);
            thumbRect = new Rect(rect.x + 2f, y, rect.width - 4f, thumbHeight);
        }
        else
        {
            float thumbWidth = Mathf.Max(14f, (rect.width - 4f) * sizeFraction);
            float x = Mathf.Lerp(rect.x + 2f, rect.xMax - thumbWidth - 2f, normalized);
            thumbRect = new Rect(x, rect.y + 2f, thumbWidth, rect.height - 4f);
        }

        DrawDarkFill(thumbRect, MorrowindUiResources.PanelShade);
        DrawSimpleFrame(thumbRect);
    }

    public static void DrawHighlightOverlay(Rect rect, bool strong)
    {
        Color old = GUI.color;
        GUI.color = strong ? new Color(0.35f, 0.28f, 0.1f, 0.22f) : new Color(0.25f, 0.2f, 0.08f, 0.14f);
        GUI.DrawTexture(rect, BaseContent.WhiteTex);
        GUI.color = MorrowindUiResources.GoldDark;
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), BaseContent.WhiteTex);
        GUI.color = old;
    }

    public static bool DrawCloseButton(Rect rect)
    {
        Rect buttonRect = new(rect.xMax - 18f, rect.y, 18f, 18f);
        bool mouseOver = Mouse.IsOver(buttonRect);
        bool clicked = Widgets.ButtonInvisible(buttonRect);

        Color oldColor = GUI.color;
        GUI.color = mouseOver ? MorrowindUiResources.Gold : MorrowindUiResources.GoldSoft;
        GUI.DrawTexture(buttonRect, BaseContent.WhiteTex);

        GUI.color = MorrowindUiResources.GoldDark;
        GUI.DrawTexture(new Rect(buttonRect.x, buttonRect.y, buttonRect.width, 1f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(buttonRect.x, buttonRect.yMax - 1f, buttonRect.width, 1f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(buttonRect.x, buttonRect.y, 1f, buttonRect.height), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(buttonRect.xMax - 1f, buttonRect.y, 1f, buttonRect.height), BaseContent.WhiteTex);

        GUI.color = MorrowindUiResources.PanelShade;
        DrawDiagonalLine(buttonRect.center, 7f, 1.35f, 45f);
        DrawDiagonalLine(buttonRect.center, 7f, 1.35f, -45f);
        GUI.color = oldColor;

        return clicked;
    }

    public static void DrawSubtleDivider(Rect rect)
    {
        Color old = GUI.color;
        GUI.color = MorrowindUiResources.GoldDark;
        GUI.DrawTexture(rect, BaseContent.WhiteTex);
        GUI.color = old;
    }

    private static void DrawDarkFill(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, BaseContent.WhiteTex);
        GUI.color = old;
    }

    private static void DrawDiagonalLine(Vector2 center, float length, float thickness, float angle)
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, center);
        GUI.DrawTexture(new Rect(center.x - (length / 2f), center.y - (thickness / 2f), length, thickness), BaseContent.WhiteTex);
        GUI.matrix = oldMatrix;
    }

    private static void DrawTextureOutline(Texture texture, Rect rect, Color color, float offset)
    {
        Color old = GUI.color;
        GUI.color = color;

        foreach (Vector2 direction in new[]
        {
            new Vector2(-1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, -1f),
            new Vector2(0f, 1f),
            new Vector2(-1f, -1f),
            new Vector2(1f, -1f),
            new Vector2(-1f, 1f),
            new Vector2(1f, 1f)
        })
        {
            GUI.DrawTexture(new Rect(rect.x + (direction.x * offset), rect.y + (direction.y * offset), rect.width, rect.height), texture, ScaleMode.ScaleToFit, true);
        }

        GUI.color = old;
    }

    private static void DrawOuterFrame(Rect rect)
    {
        DrawSimpleFrame(rect);

        float cornerSize = 14f;
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, cornerSize, cornerSize), MorrowindUiResources.FrameCornerTl, ScaleMode.StretchToFill, true);
        GUI.DrawTexture(new Rect(rect.xMax - cornerSize, rect.yMin, cornerSize, cornerSize), MorrowindUiResources.FrameCornerTr, ScaleMode.StretchToFill, true);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - cornerSize, cornerSize, cornerSize), MorrowindUiResources.FrameCornerBl, ScaleMode.StretchToFill, true);
        GUI.DrawTexture(new Rect(rect.xMax - cornerSize, rect.yMax - cornerSize, cornerSize, cornerSize), MorrowindUiResources.FrameCornerBr, ScaleMode.StretchToFill, true);
    }

    private static void DrawSimpleFrame(Rect rect)
    {
        Color old = GUI.color;

        GUI.color = MorrowindUiResources.GoldDark;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), BaseContent.WhiteTex);

        GUI.color = MorrowindUiResources.GoldSoft;
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.yMax - 2f, rect.width - 2f, 1f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, 1f, rect.height - 2f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.y + 1f, 1f, rect.height - 2f), BaseContent.WhiteTex);

        GUI.color = MorrowindUiResources.Gold;
        GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, 1f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.x + 3f, rect.yMax - 4f, rect.width - 6f, 1f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, 1f, rect.height - 6f), BaseContent.WhiteTex);
        GUI.DrawTexture(new Rect(rect.xMax - 4f, rect.y + 3f, 1f, rect.height - 6f), BaseContent.WhiteTex);
        GUI.color = old;
    }
}
