using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MorrowindMenusTrading.UI;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch]
public static class ScrollViewStylePatches
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        foreach (MethodInfo method in typeof(Widgets).GetMethods(flags))
        {
            if (method.Name.Contains("BeginScrollView"))
            {
                yield return method;
            }
        }
    }

    public static void Prefix(object[] __args)
    {
        if (Current.ProgramState != ProgramState.Playing || MorrowindMenusTradingMod.Settings == null || !MorrowindMenusTradingMod.Settings.globalMorrowindWindows)
        {
            return;
        }

        if (__args == null || __args.Length == 0 || __args[0] is not Rect outRect)
        {
            return;
        }

        if (outRect.width < 24f || outRect.height < 24f)
        {
            return;
        }

        MorrowindWindowSkin.DrawPanel(outRect, inset: 2f, darkFill: true);
    }
}
