using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.UI;

public static class TradeDialogStyleState
{
    private static GUIStyle labelStyle;
    private static GUIStyle textFieldStyle;
    private static Color labelNormal;
    private static Color labelHover;
    private static Color labelActive;
    private static Color labelFocused;
    private static Color textFieldNormal;
    private static Color textFieldHover;
    private static Color textFieldActive;
    private static Color textFieldFocused;
    private static bool captured;

    public static bool Active { get; private set; }

    public static bool ShouldStyle
    {
        get
        {
            return Active && Current.ProgramState == ProgramState.Playing && MorrowindMenusTradingMod.Settings != null && MorrowindMenusTradingMod.Settings.morrowindInventoryUi;
        }
    }

    public static void Begin()
    {
        Active = true;
        CaptureAndApplySkinColors();
    }

    public static void End()
    {
        RestoreSkinColors();
        Active = false;
        GUI.color = Color.white;
        GUI.backgroundColor = Color.white;
        GUI.contentColor = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
    }

    private static void CaptureAndApplySkinColors()
    {
        if (captured || GUI.skin == null)
        {
            return;
        }

        labelStyle = GUI.skin.label;
        textFieldStyle = GUI.skin.textField;

        if (labelStyle != null)
        {
            labelNormal = labelStyle.normal.textColor;
            labelHover = labelStyle.hover.textColor;
            labelActive = labelStyle.active.textColor;
            labelFocused = labelStyle.focused.textColor;
            labelStyle.normal.textColor = MorrowindUiResources.GoldSoft;
            labelStyle.hover.textColor = MorrowindUiResources.TextPrimary;
            labelStyle.active.textColor = MorrowindUiResources.TextPrimary;
            labelStyle.focused.textColor = MorrowindUiResources.TextPrimary;
        }

        if (textFieldStyle != null)
        {
            textFieldNormal = textFieldStyle.normal.textColor;
            textFieldHover = textFieldStyle.hover.textColor;
            textFieldActive = textFieldStyle.active.textColor;
            textFieldFocused = textFieldStyle.focused.textColor;
            textFieldStyle.normal.textColor = MorrowindUiResources.GoldSoft;
            textFieldStyle.hover.textColor = MorrowindUiResources.TextPrimary;
            textFieldStyle.active.textColor = MorrowindUiResources.TextPrimary;
            textFieldStyle.focused.textColor = MorrowindUiResources.TextPrimary;
        }

        captured = true;
    }

    private static void RestoreSkinColors()
    {
        if (!captured)
        {
            return;
        }

        if (labelStyle != null)
        {
            labelStyle.normal.textColor = labelNormal;
            labelStyle.hover.textColor = labelHover;
            labelStyle.active.textColor = labelActive;
            labelStyle.focused.textColor = labelFocused;
        }

        if (textFieldStyle != null)
        {
            textFieldStyle.normal.textColor = textFieldNormal;
            textFieldStyle.hover.textColor = textFieldHover;
            textFieldStyle.active.textColor = textFieldActive;
            textFieldStyle.focused.textColor = textFieldFocused;
        }

        labelStyle = null;
        textFieldStyle = null;
        captured = false;
    }
}
