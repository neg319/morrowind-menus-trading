using HarmonyLib;
using Verse;

namespace MorrowindMenusTrading;

[StaticConstructorOnStartup]
public static class Startup
{
    static Startup()
    {
        var harmony = new Harmony("vyberware.morrowindmenusandtrading");
        harmony.PatchAll();
    }
}
