using Verse;

namespace MorrowindMenusTrading.UI;

public sealed class MorrowindInventoryEntry
{
    public Thing thing;
    public MorrowindSelectionSource source;
    public bool equipped;

    public MorrowindInventoryEntry(Thing thing, MorrowindSelectionSource source, bool equipped)
    {
        this.thing = thing;
        this.source = source;
        this.equipped = equipped;
    }
}
