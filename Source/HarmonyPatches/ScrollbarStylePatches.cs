using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MorrowindMenusTrading.UI;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch]
public static class ScrollbarStylePatches
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        foreach (MethodInfo method in typeof(Widgets).GetMethods(flags))
        {
            if ((method.Name.Contains("Scrollbar") || method.Name.Contains("ScrollBar")) && method.ReturnType == typeof(float))
            {
                yield return method;
            }
        }
    }

    public static void Postfix(MethodBase __originalMethod, object[] __args, float __result)
    {
        if (Current.ProgramState != ProgramState.Playing || MorrowindMenusTradingMod.Settings == null || !MorrowindMenusTradingMod.Settings.globalMorrowindWindows)
        {
            return;
        }

        string name = __originalMethod?.Name ?? string.Empty;
        if (!name.Contains("Scrollbar") && !name.Contains("ScrollBar"))
        {
            return;
        }

        if (__args == null || __args.Length == 0 || __args[0] is not Rect rect)
        {
            return;
        }

        List<float> floatArgs = __args.OfType<float>().ToList();
        if (floatArgs.Count == 0)
        {
            return;
        }

        bool vertical = rect.height >= rect.width;
        float value = __result;
        float size = floatArgs.Count > 1 ? Mathf.Abs(floatArgs[1]) : (vertical ? rect.height * 0.2f : rect.width * 0.2f);
        float min = floatArgs.Count > 2 ? floatArgs[2] : 0f;
        float max = floatArgs.Count > 3 ? floatArgs[3] : Mathf.Max(min + 1f, floatArgs[0] + 1f);
        float range = Mathf.Max(0.001f, max - min);
        float normalized = Mathf.Clamp01((value - min) / range);
        float sizeFraction = Mathf.Clamp01(size / (range + Mathf.Max(size, 0.001f)));
        if (sizeFraction <= 0.01f)
        {
            sizeFraction = 0.2f;
        }

        MorrowindWindowSkin.DrawScrollbarOverlay(rect, normalized, sizeFraction, vertical);
    }
}
