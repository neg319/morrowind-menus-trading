using System.Reflection;
using HarmonyLib;
using MorrowindMenusTrading.UI;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch(typeof(Command), "GizmoOnGUIInt")]
public static class CommandGizmoStylePatches
{
    private static readonly FieldInfo IconField = AccessTools.Field(typeof(Command), "icon");
    private static readonly FieldInfo IconDrawColorField = AccessTools.Field(typeof(Command), "iconDrawColor");
    private static readonly FieldInfo DisabledField = AccessTools.Field(typeof(Command), "disabled");
    private static readonly FieldInfo HotKeyField = AccessTools.Field(typeof(Command), "hotKey");

    public static void Postfix(Command __instance, Vector2 topLeft, float maxWidth)
    {
        if (Current.ProgramState != ProgramState.Playing || !MorrowindMenusTradingMod.Settings.globalMorrowindWindows)
        {
            return;
        }

        if (__instance == null)
        {
            return;
        }

        float width = 75f;
        try
        {
            width = __instance.GetWidth(maxWidth);
        }
        catch
        {
        }

        if (width < 40f)
        {
            return;
        }

        Rect rect = new(topLeft.x, topLeft.y, width, 75f);
        bool mouseOver = Mouse.IsOver(rect);
        bool disabled = DisabledField?.GetValue(__instance) is bool value && value;

        Texture icon = GetIcon(__instance);
        Color iconColor = GetIconColor(__instance, disabled);
        string hotkeyLabel = GetHotkeyLabel(__instance);
        string label = GetLabel(__instance);

        MorrowindWindowSkin.DrawCommandGizmo(rect, icon, label, hotkeyLabel, iconColor, mouseOver, disabled);
    }

    private static Texture GetIcon(Command command)
    {
        if (IconField?.GetValue(command) is Texture texture)
        {
            return texture;
        }

        PropertyInfo iconProperty = AccessTools.Property(command.GetType(), "Icon") ?? AccessTools.Property(typeof(Command), "Icon");
        return iconProperty?.GetValue(command, null) as Texture;
    }

    private static Color GetIconColor(Command command, bool disabled)
    {
        if (disabled)
        {
            return MorrowindUiResources.GoldDark;
        }

        if (IconDrawColorField?.GetValue(command) is Color color)
        {
            return color;
        }

        return Color.white;
    }

    private static string GetHotkeyLabel(Command command)
    {
        if (HotKeyField?.GetValue(command) is KeyBindingDef hotKey && hotKey != null)
        {
            try
            {
                return hotKey.MainKeyLabel;
            }
            catch
            {
            }
        }

        return string.Empty;
    }

    private static string GetLabel(Command command)
    {
        try
        {
            if (!command.LabelCap.NullOrEmpty())
            {
                return command.LabelCap;
            }
        }
        catch
        {
        }

        return string.Empty;
    }
}
