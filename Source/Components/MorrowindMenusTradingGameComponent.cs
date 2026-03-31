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
    private const int StockpileCheckInterval = 30;
    private const int DefaultAllowedRoleMask =
        (1 << (int)InventoryTraderRole.Food) |
        (1 << (int)InventoryTraderRole.Weapons) |
        (1 << (int)InventoryTraderRole.Medicine) |
        (1 << (int)InventoryTraderRole.Apparel) |
        (1 << (int)InventoryTraderRole.Resources) |
        (1 << (int)InventoryTraderRole.Misc);

    private int lastStockpileTick;
    private List<int> roleOverrideKeys = new();
    private List<int> roleOverrideValues = new();
    private Dictionary<int, InventoryTraderRole> roleOverrides = new();
    private List<int> allowedMaskKeys = new();
    private List<int> allowedMaskValues = new();
    private Dictionary<int, int> allowedRoleMasks = new();

    public MorrowindMenusTradingGameComponent(Game game)
    {
    }

    public override void ExposeData()
    {
        roleOverrideKeys = roleOverrides.Keys.ToList();
        roleOverrideValues = roleOverrides.Values.Select(v => (int)v).ToList();
        allowedMaskKeys = allowedRoleMasks.Keys.ToList();
        allowedMaskValues = allowedRoleMasks.Values.ToList();

        Scribe_Collections.Look(ref roleOverrideKeys, nameof(roleOverrideKeys), LookMode.Value);
        Scribe_Collections.Look(ref roleOverrideValues, nameof(roleOverrideValues), LookMode.Value);
        Scribe_Collections.Look(ref allowedMaskKeys, nameof(allowedMaskKeys), LookMode.Value);
        Scribe_Collections.Look(ref allowedMaskValues, nameof(allowedMaskValues), LookMode.Value);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            roleOverrides = new Dictionary<int, InventoryTraderRole>();
            int roleCount = roleOverrideKeys?.Count ?? 0;
            for (int i = 0; i < roleCount; i++)
            {
                int key = roleOverrideKeys[i];
                int raw = i < roleOverrideValues.Count ? roleOverrideValues[i] : 0;
                roleOverrides[key] = (InventoryTraderRole)raw;
            }

            allowedRoleMasks = new Dictionary<int, int>();
            int maskCount = allowedMaskKeys?.Count ?? 0;
            for (int i = 0; i < maskCount; i++)
            {
                int key = allowedMaskKeys[i];
                int raw = i < allowedMaskValues.Count ? allowedMaskValues[i] : DefaultAllowedRoleMask;
                allowedRoleMasks[key] = raw;
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

    public int GetAllowedRoleMask(Pawn pawn)
    {
        if (pawn == null)
        {
            return DefaultAllowedRoleMask;
        }

        if (!allowedRoleMasks.TryGetValue(pawn.thingIDNumber, out int mask))
        {
            mask = DefaultAllowedRoleMask;
            allowedRoleMasks[pawn.thingIDNumber] = mask;
        }

        return mask;
    }

    public bool IsRoleAllowed(Pawn pawn, InventoryTraderRole role)
    {
        if (role is InventoryTraderRole.Auto or InventoryTraderRole.None)
        {
            return false;
        }

        int bit = RoleBit(role);
        if (bit == 0)
        {
            return false;
        }

        return (GetAllowedRoleMask(pawn) & bit) != 0;
    }

    public void SetRoleAllowed(Pawn pawn, InventoryTraderRole role, bool allowed)
    {
        if (pawn == null || role is InventoryTraderRole.Auto or InventoryTraderRole.None)
        {
            return;
        }

        int bit = RoleBit(role);
        if (bit == 0)
        {
            return;
        }

        int mask = GetAllowedRoleMask(pawn);
        mask = allowed ? mask | bit : mask & ~bit;
        allowedRoleMasks[pawn.thingIDNumber] = mask;
    }

    public void ToggleRoleAllowed(Pawn pawn, InventoryTraderRole role)
    {
        SetRoleAllowed(pawn, role, !IsRoleAllowed(pawn, role));
    }

    public string AllowedRoleSummary(Pawn pawn)
    {
        List<string> labels = new();
        foreach (InventoryTraderRole role in AllowedRoles())
        {
            if (IsRoleAllowed(pawn, role))
            {
                labels.Add(role.ToString());
            }
        }

        return labels.Count == 0 ? "Nothing" : string.Join(", ", labels);
    }

    public static IEnumerable<InventoryTraderRole> AllowedRoles()
    {
        yield return InventoryTraderRole.Food;
        yield return InventoryTraderRole.Weapons;
        yield return InventoryTraderRole.Medicine;
        yield return InventoryTraderRole.Apparel;
        yield return InventoryTraderRole.Resources;
        yield return InventoryTraderRole.Misc;
    }

    private static int RoleBit(InventoryTraderRole role)
    {
        return role switch
        {
            InventoryTraderRole.Food => 1 << (int)InventoryTraderRole.Food,
            InventoryTraderRole.Weapons => 1 << (int)InventoryTraderRole.Weapons,
            InventoryTraderRole.Medicine => 1 << (int)InventoryTraderRole.Medicine,
            InventoryTraderRole.Apparel => 1 << (int)InventoryTraderRole.Apparel,
            InventoryTraderRole.Resources => 1 << (int)InventoryTraderRole.Resources,
            InventoryTraderRole.Misc => 1 << (int)InventoryTraderRole.Misc,
            _ => 0,
        };
    }

    public static MorrowindMenusTradingGameComponent Instance => Current.Game?.GetComponent<MorrowindMenusTradingGameComponent>();
}
