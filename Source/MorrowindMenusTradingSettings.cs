using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using MorrowindMenusTrading.UI;

namespace MorrowindMenusTrading;

public sealed class MorrowindMenusTradingSettings : ModSettings
{
    public bool morrowindInventoryUi = true;
    public bool globalMorrowindWindows = true;
    public List<string> quickTabOrder = new();

    public override void ExposeData()
    {
        Scribe_Values.Look(ref morrowindInventoryUi, nameof(morrowindInventoryUi), true);
        Scribe_Values.Look(ref globalMorrowindWindows, nameof(globalMorrowindWindows), true);
        Scribe_Collections.Look(ref quickTabOrder, nameof(quickTabOrder), LookMode.Value);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            NormalizeQuickTabOrder();
        }
    }

    public void Reset()
    {
        morrowindInventoryUi = true;
        globalMorrowindWindows = true;
        quickTabOrder = GetDefaultQuickTabOrder();
    }

    public IReadOnlyList<MorrowindItemCategory> GetQuickTabs()
    {
        NormalizeQuickTabOrder();
        return quickTabOrder
            .Select(ParseCategory)
            .Where(category => category.HasValue)
            .Select(category => category.Value)
            .Distinct()
            .ToList();
    }

    public bool IsQuickTabEnabled(MorrowindItemCategory category)
    {
        NormalizeQuickTabOrder();
        return quickTabOrder.Contains(category.ToString());
    }

    public void SetQuickTabEnabled(MorrowindItemCategory category, bool enabled)
    {
        NormalizeQuickTabOrder();
        string key = category.ToString();
        bool isEnabled = quickTabOrder.Contains(key);
        if (enabled && !isEnabled)
        {
            quickTabOrder.Add(key);
        }
        else if (!enabled && isEnabled)
        {
            if (quickTabOrder.Count <= 1)
            {
                return;
            }

            quickTabOrder.Remove(key);
        }
    }

    public void MoveQuickTab(MorrowindItemCategory category, int delta)
    {
        NormalizeQuickTabOrder();
        string key = category.ToString();
        int index = quickTabOrder.IndexOf(key);
        if (index < 0)
        {
            return;
        }

        int newIndex = Math.Max(0, Math.Min(quickTabOrder.Count - 1, index + delta));
        if (newIndex == index)
        {
            return;
        }

        quickTabOrder.RemoveAt(index);
        quickTabOrder.Insert(newIndex, key);
    }

    public void NormalizeQuickTabOrder()
    {
        List<string> normalized = new();
        foreach (string key in quickTabOrder ?? Enumerable.Empty<string>())
        {
            MorrowindItemCategory? parsed = ParseCategory(key);
            if (parsed.HasValue)
            {
                string normalizedKey = parsed.Value.ToString();
                if (!normalized.Contains(normalizedKey))
                {
                    normalized.Add(normalizedKey);
                }
            }
        }

        if (normalized.Count == 0)
        {
            normalized = GetDefaultQuickTabOrder();
        }

        quickTabOrder = normalized;
    }

    public static List<string> GetDefaultQuickTabOrder()
    {
        return new List<string>
        {
            MorrowindItemCategory.Weapons.ToString(),
            MorrowindItemCategory.Apparel.ToString(),
            MorrowindItemCategory.Foods.ToString(),
            MorrowindItemCategory.Medicine.ToString(),
            MorrowindItemCategory.Items.ToString(),
            MorrowindItemCategory.RawResources.ToString(),
            MorrowindItemCategory.Manufactured.ToString(),
            MorrowindItemCategory.Misc.ToString(),
        };
    }

    public static IEnumerable<MorrowindItemCategory> AllQuickTabCategories()
    {
        yield return MorrowindItemCategory.Weapons;
        yield return MorrowindItemCategory.Apparel;
        yield return MorrowindItemCategory.Foods;
        yield return MorrowindItemCategory.Medicine;
        yield return MorrowindItemCategory.Items;
        yield return MorrowindItemCategory.RawResources;
        yield return MorrowindItemCategory.Manufactured;
        yield return MorrowindItemCategory.Misc;
    }

    private static MorrowindItemCategory? ParseCategory(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return Enum.TryParse(key, out MorrowindItemCategory category) ? category : null;
    }
}
