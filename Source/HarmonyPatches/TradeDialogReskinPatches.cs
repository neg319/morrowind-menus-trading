using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MorrowindMenusTrading.UI;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch]
public static class TradeLabelStylePatches
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        foreach (MethodInfo method in typeof(Widgets).GetMethods(flags))
        {
            if (method.Name == nameof(Widgets.Label))
            {
                yield return method;
            }
        }
    }

    public static void Prefix(out Color __state)
    {
        __state = GUI.color;
        if (!TradeDialogStyleState.ShouldStyle)
        {
            return;
        }

        GUI.color = MorrowindUiResources.GoldSoft;
    }

    public static void Postfix(Color __state)
    {
        GUI.color = __state;
    }
}
