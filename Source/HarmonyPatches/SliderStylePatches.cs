using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MorrowindMenusTrading.UI;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch]
public static class SliderStylePatches
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        foreach (MethodInfo method in typeof(Widgets).GetMethods(flags))
        {
            if (method.Name == nameof(Widgets.HorizontalSlider) && method.ReturnType == typeof(float))
            {
                yield return method;
            }
        }
    }

    public static void Postfix(object[] __args, float __result)
    {
        if (Current.ProgramState != ProgramState.Playing || MorrowindMenusTradingMod.Settings == null || !MorrowindMenusTradingMod.Settings.globalMorrowindWindows)
        {
            return;
        }

        if (__args == null || __args.Length < 4 || __args[0] is not Rect rect)
        {
            return;
        }

        float leftValue = __args[2] is float left ? left : 0f;
        float rightValue = __args[3] is float right ? right : 1f;
        float normalized = Mathf.Approximately(leftValue, rightValue)
            ? 0f
            : Mathf.InverseLerp(leftValue, rightValue, __result);

        MorrowindWindowSkin.DrawSliderOverlay(rect, normalized);
    }
}
