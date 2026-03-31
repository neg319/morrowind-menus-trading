using RimWorld;
using System.Collections.Generic;
using System.Linq;
using MorrowindMenusTrading.Systems;
using Verse;

namespace MorrowindMenusTrading.Components;

public enum InventoryTraderRole
{
    Auto,
    None,
    Food,
    Weapons,
    Medicine,
    Apparel,
    Resources,
    Misc,
}

public sealed class MorrowindMenusTradingGameComponent : GameComponent
{
    private const int StockpileCheckInterval = 90;
    private int lastStockpileTick;
    private List<int> roleOverrideKeys = new();
    private List<int> roleOverrideValues = new();
    private Dictionary<int, InventoryTraderRole> roleOverrides = new();

    public MorrowindMenusTradingGameComponent(Game game)
    {
    }

    public override void ExposeData()
    {
        roleOverrideKeys = roleOverrides.Keys.ToList();
        roleOverrideValues = roleOverrides.Values.Select(v => (int)v).ToList();
        Scribe_Collections.Look(ref roleOverrideKeys, nameof(roleOverrideKeys), LookMode.Value);
        Scribe_Collections.Look(ref roleOverrideValues, nameof(roleOverrideValues), LookMode.Value);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            roleOverrides = new Dictionary<int, InventoryTraderRole>();
            int count = roleOverrideKeys?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                int key = roleOverrideKeys[i];
                int raw = i < roleOverrideValues.Count ? roleOverrideValues[i] : 0;
                roleOverrides[key] = (InventoryTraderRole)raw;
            }
        }
    }

    public override void GameComponentTick()
    {
        if (!MorrowindMenusTradingMod.Settings.personalStockpileMode || Find.TickManager.TicksGame - lastStockpileTick < StockpileCheckInterval)
        {
            return;
        }

        lastStockpileTick = Find.TickManager.TicksGame;
        foreach (Map map in Find.Maps)
        {
            PersonalInventoryStockpileSystem.TickMap(map);
        }
    }

    public InventoryTraderRole GetManualRoleOverride(Pawn pawn)
    {
        if (pawn == null)
        {
            return InventoryTraderRole.Auto;
        }

        return roleOverrides.TryGetValue(pawn.thingIDNumber, out InventoryTraderRole role) ? role : InventoryTraderRole.Auto;
    }

    public void SetManualRoleOverride(Pawn pawn, InventoryTraderRole role)
    {
        if (pawn == null)
        {
            return;
        }

        if (role == InventoryTraderRole.Auto)
        {
            roleOverrides.Remove(pawn.thingIDNumber);
            return;
        }

        roleOverrides[pawn.thingIDNumber] = role;
    }

    public InventoryTraderRole GetEffectiveRole(Pawn pawn)
    {
        InventoryTraderRole manual = GetManualRoleOverride(pawn);
        return manual == InventoryTraderRole.Auto ? InferRoleFromSkills(pawn) : manual;
    }

    public InventoryTraderRole InferRoleFromSkills(Pawn pawn)
    {
        if (pawn?.skills?.skills == null || pawn.skills.skills.Count == 0)
        {
            return InventoryTraderRole.Resources;
        }

        SkillRecord best = pawn.skills.skills.OrderByDescending(s => s.Level).ThenByDescending(s => s.passion).FirstOrDefault();
        if (best == null)
        {
            return InventoryTraderRole.Resources;
        }

        string def = best.def.defName;
        return def switch
        {
            "Shooting" => InventoryTraderRole.Weapons,
            "Melee" => InventoryTraderRole.Weapons,
            "Cooking" => InventoryTraderRole.Food,
            "Plants" => InventoryTraderRole.Food,
            "Animals" => InventoryTraderRole.Food,
            "Medicine" => InventoryTraderRole.Medicine,
            "Crafting" => InventoryTraderRole.Apparel,
            "Artistic" => InventoryTraderRole.Misc,
            "Construction" => InventoryTraderRole.Resources,
            "Mining" => InventoryTraderRole.Resources,
            "Intellectual" => InventoryTraderRole.Medicine,
            _ => InventoryTraderRole.Resources,
        };
    }

    public static MorrowindMenusTradingGameComponent Instance => Current.Game?.GetComponent<MorrowindMenusTradingGameComponent>();
}
