using System.Collections.Generic;
using Verse;

namespace MorrowindMenusTrading.UI;

public enum MorrowindInventoryTab
{
    Inventory,
    Equipment,
    Stats,
}

public enum MorrowindSelectionSource
{
    Inventory,
    Colony,
    Equipment,
    Apparel,
}

public enum MorrowindItemCategory
{
    All,
    Foods,
    Manufactured,
    RawResources,
    Items,
    Weapons,
    Apparel,
    Medicine,
    Misc,
}

public sealed class MorrowindInventoryState
{
    public MorrowindInventoryTab activeTab = MorrowindInventoryTab.Inventory;
    public MorrowindItemCategory activeCategory = MorrowindItemCategory.All;
    public readonly HashSet<string> selectedExtraCategoryDefs = new();
    public MorrowindSelectionSource selectionSource = MorrowindSelectionSource.Colony;
    public int selectedThingId = -1;
    public UnityEngine.Vector2 inventoryScroll;
    public UnityEngine.Vector2 equipmentScroll;
    public UnityEngine.Vector2 statsScroll;

    public bool HasExtraCategorySelection => selectedExtraCategoryDefs.Count > 0;

    public void Select(Thing thing, MorrowindSelectionSource source)
    {
        selectedThingId = thing?.thingIDNumber ?? -1;
        selectionSource = source;
    }

    public void ClearSelection()
    {
        selectedThingId = -1;
        selectionSource = MorrowindSelectionSource.Colony;
    }

    public void ToggleExtraCategory(string categoryDefName)
    {
        if (string.IsNullOrEmpty(categoryDefName))
        {
            return;
        }

        if (!selectedExtraCategoryDefs.Add(categoryDefName))
        {
            selectedExtraCategoryDefs.Remove(categoryDefName);
        }
    }

    public void ClearExtraCategories()
    {
        selectedExtraCategoryDefs.Clear();
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
