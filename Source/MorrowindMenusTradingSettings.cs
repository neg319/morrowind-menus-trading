using Verse;

namespace MorrowindMenusTrading;

public sealed class MorrowindMenusTradingSettings : ModSettings
{
    public bool morrowindInventoryUi = true;
    public bool globalMorrowindWindows = true;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref morrowindInventoryUi, nameof(morrowindInventoryUi), true);
        Scribe_Values.Look(ref globalMorrowindWindows, nameof(globalMorrowindWindows), true);
    }

    public void Reset()
    {
        morrowindInventoryUi = true;
        globalMorrowindWindows = true;
    }
}
