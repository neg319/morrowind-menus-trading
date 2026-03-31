using System.Collections.Generic;
using Verse;

namespace MorrowindMenusTrading.UI;

public enum MorrowindInventoryTab
{
    Inventory,
    Equipment,
    Stats,
    Roles,
}

public enum MorrowindSelectionSource
{
    Inventory,
    Equipment,
    Apparel,
}

public enum MorrowindItemCategory
{
    All,
    Food,
    Medicine,
    Weapons,
    Apparel,
    Resources,
    Misc,
}

public sealed class MorrowindInventoryState
{
    public MorrowindInventoryTab activeTab = MorrowindInventoryTab.Inventory;
    public MorrowindItemCategory activeCategory = MorrowindItemCategory.All;
    public MorrowindSelectionSource selectionSource = MorrowindSelectionSource.Inventory;
    public int selectedThingId = -1;
    public UnityEngine.Vector2 inventoryScroll;
    public UnityEngine.Vector2 equipmentScroll;
    public UnityEngine.Vector2 statsScroll;

    public void Select(Thing thing, MorrowindSelectionSource source)
    {
        selectedThingId = thing?.thingIDNumber ?? -1;
        selectionSource = source;
    }

    public void ClearSelection()
    {
        selectedThingId = -1;
        selectionSource = MorrowindSelectionSource.Inventory;
    }
}

public static class MorrowindInventoryStateStore
{
    private static readonly Dictionary<int, MorrowindInventoryState> StatesByPawn = new();

    public static MorrowindInventoryState For(Pawn pawn)
    {
        if (pawn == null)
        {
            return new MorrowindInventoryState();
        }

        if (!StatesByPawn.TryGetValue(pawn.thingIDNumber, out MorrowindInventoryState state))
        {
            state = new MorrowindInventoryState();
            StatesByPawn[pawn.thingIDNumber] = state;
        }

        return state;
    }
}
