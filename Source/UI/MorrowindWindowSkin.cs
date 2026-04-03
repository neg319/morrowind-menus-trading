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

        if (icon != null)
        {
            Rect iconRect = rect.ContractedBy(Mathf.Max(3f, Mathf.Min(rect.width, rect.height) * 0.14f));
            Color old = GUI.color;
            GUI.color = active
                ? (mouseOver ? MorrowindUiResources.TextPrimary : MorrowindUiResources.Gold)
                : MorrowindUiResources.GoldDark;
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
            GUI.color = old;
        }
    }

    public static void DrawCenteredText(Rect rect, string label, Color color)
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
        Text.Font = GameFont.Small;
        Widgets.Label(rect, label);
        Text.Anchor = oldAnchor;
        Text.Font = oldFont;
        GUI.color = oldColor;
    }

    public static void ResetTextState()
    {
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        Text.WordWrap = true;
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
