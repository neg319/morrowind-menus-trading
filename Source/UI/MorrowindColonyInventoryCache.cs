using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MorrowindMenusTrading.UI;

public static class MorrowindColonyInventoryCache
{
    private sealed class CacheEntry
    {
        public int lastRefreshTick = -99999;
        public List<Thing> items = new();
    }

    private static readonly Dictionary<int, CacheEntry> CacheByMapId = new();
    private const int RefreshTicks = 120;

    public static List<Thing> GetItemsForMap(Map map)
    {
        if (map == null)
        {
            return new List<Thing>();
        }

        if (!CacheByMapId.TryGetValue(map.uniqueID, out CacheEntry entry))
        {
            entry = new CacheEntry();
            CacheByMapId[map.uniqueID] = entry;
        }

        int currentTick = Find.TickManager?.TicksGame ?? 0;
        if (currentTick - entry.lastRefreshTick >= RefreshTicks)
        {
            Refresh(entry, map);
        }

        return entry.items;
    }

    private static void Refresh(CacheEntry entry, Map map)
    {
        entry.lastRefreshTick = Find.TickManager?.TicksGame ?? 0;
        entry.items = map.listerThings.AllThings
            .Where(thing => IsColonyInventoryThing(thing, map))
            .ToList();
    }

    private static bool IsColonyInventoryThing(Thing thing, Map map)
    {
        if (thing == null || thing.Destroyed)
        {
            return false;
        }

        if (!thing.Spawned || thing.MapHeld != map)
        {
            return false;
        }

        if (thing.def == null || thing.def.category != ThingCategory.Item)
        {
            return false;
        }

        if (thing.stackCount <= 0 || thing.IsForbidden(Faction.OfPlayer))
        {
            return false;
        }

        if (thing.PositionHeld.Fogged(map))
        {
            return false;
        }

        return StoreUtility.CurrentStoragePriorityOf(thing) != StoragePriority.Unstored;
    }
}
