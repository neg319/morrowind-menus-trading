using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MorrowindMenusTrading.UI;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch]
public static class CheckboxStylePatches
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        foreach (MethodInfo method in typeof(Widgets).GetMethods(flags))
        {
            if (method.Name == nameof(Widgets.Checkbox))
            {
                yield return method;
            }
        }
    }

    public static void Postfix(object[] __args)
    {
        if (Current.ProgramState != ProgramState.Playing || MorrowindMenusTradingMod.Settings == null || !MorrowindMenusTradingMod.Settings.globalMorrowindWindows)
        {
            return;
        }

        if (__args == null || __args.Length < 2)
        {
            return;
        }

        Rect rect = GetCheckboxRect(__args);
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        bool isChecked = __args[1] is bool value && value;
        bool mouseOver = Mouse.IsOver(rect);
        MorrowindWindowSkin.DrawCheckbox(rect, isChecked, mouseOver, !GUI.enabled);
    }

    private static Rect GetCheckboxRect(object[] args)
    {
        if (args[0] is Rect rect)
        {
            return rect;
        }

        if (args[0] is Vector2 position)
        {
            float size = 24f;
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] is float floatValue && floatValue > 4f && floatValue <= 64f)
                {
                    size = floatValue;
                    break;
                }
            }

            return new Rect(position.x, position.y, size, size);
        }

        return Rect.zero;
    }
}
