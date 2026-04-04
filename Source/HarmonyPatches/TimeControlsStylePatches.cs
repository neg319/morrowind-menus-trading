using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MorrowindMenusTrading.UI;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch]
public static class TimeControlsStylePatches
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        Type timeControlsType = AccessTools.TypeByName("RimWorld.TimeControls");
        if (timeControlsType == null)
        {
            yield break;
        }

        foreach (string methodName in new[] { "TimeControlsOnGUI", "DoTimeControlsGUI" })
        {
            MethodInfo method = AccessTools.Method(timeControlsType, methodName);
            if (method != null)
            {
                yield return method;
            }
        }
    }

    public static void Prefix()
    {
        if (Current.ProgramState != ProgramState.Playing || MorrowindMenusTradingMod.Settings == null || !MorrowindMenusTradingMod.Settings.globalMorrowindWindows)
        {
            return;
        }

        TimeControlsStyleState.Push();
    }

    public static void Postfix()
    {
        TimeControlsStyleState.Pop();
    }
}
