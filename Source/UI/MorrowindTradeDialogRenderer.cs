using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MorrowindMenusTrading.UI;

public static class MorrowindTradeDialogRenderer
{
    private const float TitleHeight = 30f;
    private const float TopBarHeight = 54f;
    private const float FooterHeight = 54f;
    private const float CategoryTabsHeight = 26f;
    private const float ActionStripHeight = 82f;
    private const float TransferColumnWidth = 86f;
    private const float SlotSize = 54f;
    private const float SlotGap = 5f;

    private static readonly FieldInfo GiftsOnlyField = AccessTools.Field(typeof(Dialog_Trade), "giftsOnly");
    private static readonly FieldInfo Sorter1Field = AccessTools.Field(typeof(Dialog_Trade), "sorter1");
    private static readonly FieldInfo Sorter2Field = AccessTools.Field(typeof(Dialog_Trade), "sorter2");
    private static readonly Dictionary<int, TradeUiState> States = new();

    public static void Draw(Dialog_Trade dialog, Rect rect)
    {
        if (dialog == null || !TradeSession.Active)
        {
            return;
        }

        try
        {
            TradeSession.deal.UpdateCurrencyCount();
            TradeUiState state = GetState(dialog);
            Tradeable currencyTradeable = GetCurrencyTradeable();
            List<Tradeable> tradeables = BuildTradeables(dialog, state);
            Tradeable selected = ResolveSelectedTradeable(tradeables, state);

            MorrowindWindowSkin.DrawWindow(rect);

            Rect titleRect = new(rect.x + 10f, rect.y + 8f, rect.width - 20f, TitleHeight);
            Rect topBarRect = new(rect.x + 10f, titleRect.yMax + 5f, rect.width - 20f, TopBarHeight);
            Rect contentRect = new(rect.x + 10f, topBarRect.yMax + 8f, rect.width - 20f, rect.height - TitleHeight - TopBarHeight - FooterHeight - 34f);
            Rect footerRect = new(rect.x + 10f, contentRect.yMax + 8f, rect.width - 20f, FooterHeight);

            DrawTitleBar(titleRect);
            DrawTopBar(dialog, topBarRect, state);
            DrawTradePane(contentRect, tradeables, selected, state);
            DrawFooter(dialog, footerRect, currencyTradeable, tradeables);
        }
        finally
        {
            ResetGuiState();
        }
    }

    private static void DrawTitleBar(Rect rect)
    {
        MorrowindWindowSkin.DrawPanel(rect, inset: 3f, darkFill: false);
        string title = TradeSession.giftMode ? "Gift Exchange" : "Barter";
        DrawLabelCentered(rect, title, MorrowindUiResources.TextPrimary, GameFont.Small);
    }

    private static void DrawTopBar(Dialog_Trade dialog, Rect rect, TradeUiState state)
    {
        MorrowindWindowSkin.DrawPanel(rect, inset: 4f);

        Rect leftRect = new(rect.x + 8f, rect.y + 6f, rect.width * 0.34f, rect.height - 12f);
        Rect centerRect = new(leftRect.xMax + 8f, rect.y + 4f, rect.width * 0.28f, rect.height - 8f);
        Rect rightRect = new(centerRect.xMax + 8f, rect.y + 8f, rect.xMax - centerRect.xMax - 16f, rect.height - 16f);

        string negotiatorInfo = TradeSession.playerNegotiator == null
            ? string.Empty
            : $"Negotiator: {TradeSession.playerNegotiator.Name?.ToStringShort ?? TradeSession.playerNegotiator.LabelShort} ({TradeSession.playerNegotiator.GetStatValue(StatDefOf.TradePriceImprovement).ToStringPercent()})";
        string hint = TradeSession.giftMode
            ? "Items in Purchased become gifts."
            : "Double click an icon to add one.";

        DrawLabelLeft(new Rect(leftRect.x, leftRect.y, leftRect.width, 18f), "New Arrivals", MorrowindUiResources.TextPrimary);
        DrawLabelLeft(new Rect(leftRect.x, leftRect.y + 18f, leftRect.width, 16f), negotiatorInfo, MorrowindUiResources.TextMuted, GameFont.Tiny);
        DrawLabelLeft(new Rect(leftRect.x, leftRect.y + 32f, leftRect.width, 14f), hint, MorrowindUiResources.TextMuted, GameFont.Tiny);

        string traderName = TradeSession.trader?.TraderName ?? "Trader";
        string traderKind = TradeSession.trader?.TraderKind?.LabelCap ?? string.Empty;
        DrawLabelCentered(new Rect(centerRect.x, centerRect.y, centerRect.width, 22f), traderName, MorrowindUiResources.TextPrimary);
        DrawLabelCentered(new Rect(centerRect.x, centerRect.y + 18f, centerRect.width, 18f), traderKind, MorrowindUiResources.TextMuted, GameFont.Tiny);

        float gap = 6f;
        float cursorX = rightRect.x;
        Rect sort1Rect = new(cursorX, rightRect.y, 92f, 24f);
        cursorX += sort1Rect.width + gap;
        Rect sort2Rect = new(cursorX, rightRect.y, 106f, 24f);
        cursorX += sort2Rect.width + gap;
        Rect modeRect = new(cursorX, rightRect.y, 86f, 24f);
        cursorX += modeRect.width + gap;
        Rect sellableRect = new(cursorX, rightRect.y, 84f, 24f);

        string primarySort = (GetSorter1(dialog) ?? TransferableSorterDefOf.Category)?.LabelCap ?? "Category";
        if (DrawButton(sort1Rect, primarySort))
        {
            OpenSorterMenu(dialog, primary: true);
        }

        string secondarySort = (GetSorter2(dialog) ?? TransferableSorterDefOf.MarketValue)?.LabelCap ?? "Market value";
        if (DrawButton(sort2Rect, secondarySort))
        {
            OpenSorterMenu(dialog, primary: false);
        }

        if (CanToggleGiftMode(dialog))
        {
            string modeLabel = TradeSession.giftMode ? "Trade Mode" : "Gift Mode";
            if (DrawButton(modeRect, modeLabel))
            {
                TradeSession.giftMode = !TradeSession.giftMode;
                TradeSession.deal.Reset();
            }
        }

        if (TradeSession.trader != null && DrawButton(sellableRect, state.sellableOnly ? "All Items" : "Sellable"))
        {
            state.sellableOnly = !state.sellableOnly;
            state.gridScroll = Vector2.zero;
        }
    }

    private static void OpenSorterMenu(Dialog_Trade dialog, bool primary)
    {
        List<FloatMenuOption> options = DefDatabase<TransferableSorterDef>.AllDefsListForReading
            .OrderBy(def => def.LabelCap)
            .Select(def => new FloatMenuOption(def.LabelCap, () => SetSorter(dialog, primary, def)))
            .ToList();
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private static void DrawTradePane(Rect rect, List<Tradeable> tradeables, Tradeable selected, TradeUiState state)
    {
        MorrowindWindowSkin.DrawPanel(rect);
        Rect inner = rect.ContractedBy(8f);

        Rect tabsRect = new(inner.x, inner.y, inner.width, CategoryTabsHeight);
        DrawCategoryTabs(tabsRect, state);

        Rect totalsRect = new(inner.x, tabsRect.yMax + 4f, inner.width, 18f);
        DrawPaneTotals(totalsRect, tradeables);

        Rect bodyRect = new(inner.x, totalsRect.yMax + 4f, inner.width, inner.height - CategoryTabsHeight - 18f - ActionStripHeight - 14f);
        Rect stripRect = new(inner.x, bodyRect.yMax + 6f, inner.width, ActionStripHeight);

        Rect soldRect = new(bodyRect.x, bodyRect.y, TransferColumnWidth, bodyRect.height);
        Rect boughtRect = new(bodyRect.xMax - TransferColumnWidth, bodyRect.y, TransferColumnWidth, bodyRect.height);
        Rect gridRect = new(soldRect.xMax + 8f, bodyRect.y, bodyRect.width - (TransferColumnWidth * 2f) - 16f, bodyRect.height);

        List<Tradeable> soldItems = tradeables.Where(trad => GetSoldCount(trad) > 0).ToList();
        List<Tradeable> boughtItems = tradeables.Where(trad => GetBoughtCount(trad) > 0).ToList();

        DrawTransferColumn(soldRect, "Sold", soldItems, selected, state, isSoldColumn: true);
        DrawTradeGrid(gridRect, tradeables, selected, state);
        DrawTransferColumn(boughtRect, "Purchased", boughtItems, selected, state, isSoldColumn: false);
        DrawSelectionPanel(stripRect, selected);
    }

    private static void DrawPaneTotals(Rect rect, List<Tradeable> tradeables)
    {
        float totalSold = tradeables.Where(trad => trad.ActionToDo == TradeAction.PlayerSells).Sum(trad => trad.CurTotalCurrencyCostForDestination);
        float totalBought = tradeables.Where(trad => trad.ActionToDo == TradeAction.PlayerBuys).Sum(trad => trad.CurTotalCurrencyCostForSource);
        string left = $"Total Sold {FormatCurrency(totalSold)}";
        string right = $"Total Bought {FormatCurrency(totalBought)}";
        DrawLabelLeft(new Rect(rect.x + 2f, rect.y, rect.width * 0.5f, rect.height), left, MorrowindUiResources.TextMuted, GameFont.Tiny);
        DrawLabelRight(new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.5f - 2f, rect.height), right, MorrowindUiResources.TextMuted, GameFont.Tiny);
    }

    private static void DrawCategoryTabs(Rect rect, TradeUiState state)
    {
        var categories = new[]
        {
            TradeGridCategory.All,
            TradeGridCategory.Weapon,
            TradeGridCategory.Apparel,
            TradeGridCategory.Magic,
            TradeGridCategory.Misc,
        };

        float x = rect.x;
        for (int i = 0; i < categories.Length; i++)
        {
            string label = categories[i] switch
            {
                TradeGridCategory.Weapon => "Weapon",
                TradeGridCategory.Apparel => "Apparel",
                TradeGridCategory.Magic => "Magic",
                TradeGridCategory.Misc => "Misc",
                _ => "All",
            };

            float width = Mathf.Clamp(Text.CalcSize(label).x + 18f, 46f, 92f);
            Rect tabRect = new(x, rect.y, width, rect.height);
            bool active = state.category == categories[i];
            DrawInventoryStyleTab(tabRect, label, active);
            if (Widgets.ButtonInvisible(tabRect))
            {
                state.category = categories[i];
                state.gridScroll = Vector2.zero;
            }

            x += width + 4f;
        }
    }

    private static void DrawInventoryStyleTab(Rect rect, string label, bool active)
    {
        GUI.color = Color.white;
        GUI.DrawTexture(rect, active ? MorrowindUiResources.TabActive : MorrowindUiResources.TabInactive, ScaleMode.StretchToFill, true);
        DrawLabelCentered(rect, label, active ? MorrowindUiResources.ActiveTabText : MorrowindUiResources.InactiveTabText, GameFont.Tiny);
    }

    private static void DrawTransferColumn(Rect rect, string title, List<Tradeable> items, Tradeable selected, TradeUiState state, bool isSoldColumn)
    {
        MorrowindWindowSkin.DrawPanel(rect, inset: 3f);
        DrawLabelCentered(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, 18f), title, MorrowindUiResources.TextPrimary, GameFont.Tiny);

        Rect listRect = new(rect.x + 6f, rect.y + 24f, rect.width - 12f, rect.height - 30f);
        Vector2 scroll = isSoldColumn ? state.soldScroll : state.boughtScroll;
        float rowHeight = SlotSize + 4f;
        float viewHeight = Mathf.Max(listRect.height, items.Count * rowHeight + 2f);
        Rect viewRect = new(0f, 0f, listRect.width - 14f, viewHeight);

        Widgets.BeginScrollView(listRect, ref scroll, viewRect);
        float y = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            Tradeable trad = items[i];
            Thing thing = trad.AnyThing;
            if (thing == null)
            {
                continue;
            }

            Rect slotRect = new((viewRect.width - SlotSize) * 0.5f, y, SlotSize, SlotSize);
            bool isSelected = selected != null && MakeTradeKey(selected) == MakeTradeKey(trad);
            MorrowindWindowSkin.DrawSlot(slotRect, isSelected);
            DrawThingIcon(slotRect.ContractedBy(4f), thing);
            DrawStackCount(slotRect, isSoldColumn ? GetSoldCount(trad) : GetBoughtCount(trad));
            TooltipHandler.TipRegion(slotRect, BuildTradeableTooltip(trad));
            if (Widgets.ButtonInvisible(slotRect))
            {
                state.selectedKey = MakeTradeKey(trad);
            }

            y += rowHeight;
        }
        Widgets.EndScrollView();

        if (isSoldColumn)
        {
            state.soldScroll = scroll;
        }
        else
        {
            state.boughtScroll = scroll;
        }

        if (items.Count == 0)
        {
            DrawLabelCentered(new Rect(rect.x + 4f, rect.center.y - 8f, rect.width - 8f, 18f), "Empty", MorrowindUiResources.TextMuted, GameFont.Tiny);
        }
    }

    private static void DrawTradeGrid(Rect rect, List<Tradeable> tradeables, Tradeable selected, TradeUiState state)
    {
        MorrowindWindowSkin.DrawPanel(rect, inset: 3f);
        DrawGridBackground(rect.ContractedBy(4f));
        Rect inner = rect.ContractedBy(8f);

        int columns = Mathf.Max(1, Mathf.FloorToInt((inner.width - 4f) / (SlotSize + SlotGap)));
        int rows = Mathf.Max(1, Mathf.CeilToInt(tradeables.Count / (float)columns));
        float viewHeight = Mathf.Max(inner.height, rows * (SlotSize + SlotGap) + 4f);
        Rect viewRect = new(0f, 0f, inner.width - 16f, viewHeight);

        Widgets.BeginScrollView(inner, ref state.gridScroll, viewRect);
        for (int index = 0; index < tradeables.Count; index++)
        {
            Tradeable trad = tradeables[index];
            Thing thing = trad.AnyThing;
            if (thing == null)
            {
                continue;
            }

            int row = index / columns;
            int col = index % columns;
            Rect cell = new(col * (SlotSize + SlotGap), row * (SlotSize + SlotGap), SlotSize, SlotSize);
            bool isSelected = selected != null && MakeTradeKey(selected) == MakeTradeKey(trad);
            MorrowindWindowSkin.DrawSlot(cell, isSelected);
            DrawThingIcon(cell.ContractedBy(4f), thing);
            DrawStackCount(cell, thing.stackCount);
            DrawHeldCounts(cell, trad);
            DrawTradeMarks(cell, trad);
            TooltipHandler.TipRegion(cell, BuildTradeableTooltip(trad));

            if (Widgets.ButtonInvisible(cell))
            {
                state.selectedKey = MakeTradeKey(trad);
                if (Event.current != null && Event.current.clickCount > 1)
                {
                    TryQuickAdjust(trad);
                }
            }
        }
        Widgets.EndScrollView();
    }

    private static void DrawGridBackground(Rect rect)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.13f);
        GUI.DrawTexture(rect, MorrowindUiResources.Background, ScaleMode.StretchToFill, true);
        GUI.color = oldColor;
    }

    private static void DrawTradeMarks(Rect rect, Tradeable trad)
    {
        int sold = GetSoldCount(trad);
        int bought = GetBoughtCount(trad);
        if (sold > 0)
        {
            DrawBadge(new Rect(rect.x + 2f, rect.yMax - 15f, rect.width * 0.45f, 12f), $"S{sold}");
        }
        if (bought > 0)
        {
            DrawBadge(new Rect(rect.x + rect.width * 0.52f, rect.yMax - 15f, rect.width * 0.43f, 12f), $"P{bought}");
        }
    }

    private static void DrawBadge(Rect rect, string text)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(rect, BaseContent.WhiteTex);
        GUI.color = oldColor;
        DrawLabelCentered(rect, text, MorrowindUiResources.TextPrimary, GameFont.Tiny);
    }

    private static void DrawSelectionPanel(Rect rect, Tradeable selected)
    {
        MorrowindWindowSkin.DrawPanel(rect, inset: 3f);
        if (selected?.AnyThing == null)
        {
            DrawLabelCentered(rect, "Select an item from the grid", MorrowindUiResources.TextMuted, GameFont.Small);
            return;
        }

        Thing thing = selected.AnyThing;
        Rect inner = rect.ContractedBy(8f);
        Rect iconRect = new(inner.x + 2f, inner.y + 8f, 54f, 54f);
        MorrowindWindowSkin.DrawSlot(iconRect, false);
        DrawThingIcon(iconRect.ContractedBy(4f), thing);

        float textWidth = Mathf.Max(180f, inner.width * 0.34f);
        Rect nameRect = new(iconRect.xMax + 8f, inner.y + 4f, textWidth, 22f);
        Rect infoRect = new(nameRect.x, nameRect.yMax + 2f, textWidth, 18f);
        Rect priceRect = new(nameRect.x, infoRect.yMax + 2f, textWidth, 18f);
        DrawLabelLeft(nameRect, thing.LabelCap, MorrowindUiResources.TextPrimary);
        DrawLabelLeft(infoRect, $"Yours: {selected.CountHeldBy(Transactor.Colony)}   Trader: {selected.CountHeldBy(Transactor.Trader)}", MorrowindUiResources.TextMuted, GameFont.Tiny);
        DrawLabelLeft(priceRect, $"Buy {FormatCurrency(selected.GetPriceFor(TradeAction.PlayerBuys))}   Sell {FormatCurrency(selected.GetPriceFor(TradeAction.PlayerSells))}", MorrowindUiResources.TextMuted, GameFont.Tiny);

        float groupWidth = 138f;
        Rect soldGroupRect = new(inner.xMax - (groupWidth * 2f) - 12f, inner.y + 6f, groupWidth, 58f);
        Rect boughtGroupRect = new(soldGroupRect.xMax + 8f, inner.y + 6f, groupWidth, 58f);
        DrawAdjustGroup(soldGroupRect, selected, TradeAction.PlayerSells, "Sold", selected.CountHeldBy(Transactor.Colony), GetSoldCount(selected), true);
        DrawAdjustGroup(boughtGroupRect, selected, TradeAction.PlayerBuys, "Purchased", selected.CountHeldBy(Transactor.Trader), GetBoughtCount(selected), selected.TraderWillTrade || TradeSession.giftMode);
    }

    private static void DrawAdjustGroup(Rect rect, Tradeable trad, TradeAction action, string label, int max, int count, bool enabled)
    {
        MorrowindWindowSkin.DrawFlatPanel(rect, MorrowindUiResources.PanelShade);
        DrawLabelCentered(new Rect(rect.x, rect.y, rect.width, 16f), label, MorrowindUiResources.TextPrimary, GameFont.Tiny);

        float allWidth = 28f;
        float buttonWidth = 18f;
        float countWidth = Mathf.Max(28f, rect.width - allWidth - buttonWidth - buttonWidth - 8f);
        Rect allRect = new(rect.x + 2f, rect.y + 24f, allWidth, 22f);
        Rect minusRect = new(allRect.xMax + 2f, allRect.y, buttonWidth, 22f);
        Rect countRect = new(minusRect.xMax + 2f, allRect.y, countWidth, 22f);
        Rect plusRect = new(countRect.xMax + 2f, allRect.y, buttonWidth, 22f);

        bool canAll = enabled && max > 0;
        bool canMinus = enabled && count > 0;
        bool canPlus = enabled && count < max;
        if (DrawMiniButton(allRect, "All", canAll))
        {
            SetActionCount(trad, action, max);
            PlayAdjustSound(action == TradeAction.PlayerSells, true);
        }
        if (DrawMiniButton(minusRect, "-", canMinus))
        {
            SetActionCount(trad, action, Math.Max(0, count - Math.Max(1, GenUI.CurrentAdjustmentMultiplier())));
            PlayAdjustSound(action == TradeAction.PlayerSells, false);
        }

        MorrowindWindowSkin.DrawPanel(countRect, inset: 2f);
        DrawLabelCentered(countRect, count.ToStringCached(), count > 0 ? MorrowindUiResources.TextPrimary : MorrowindUiResources.TextMuted, GameFont.Tiny);

        if (DrawMiniButton(plusRect, "+", canPlus))
        {
            SetActionCount(trad, action, Math.Min(max, count + Math.Max(1, GenUI.CurrentAdjustmentMultiplier())));
            PlayAdjustSound(action == TradeAction.PlayerSells, true);
        }
    }

    private static void DrawFooter(Dialog_Trade dialog, Rect rect, Tradeable currencyTradeable, List<Tradeable> tradeables)
    {
        TradeSession.deal.UpdateCurrencyCount();
        MorrowindWindowSkin.DrawPanel(rect, inset: 3f);

        float totalSold = tradeables.Where(trad => trad.ActionToDo == TradeAction.PlayerSells).Sum(trad => trad.CurTotalCurrencyCostForDestination);
        float totalBought = tradeables.Where(trad => trad.ActionToDo == TradeAction.PlayerBuys).Sum(trad => trad.CurTotalCurrencyCostForSource);
        float balance = totalSold - totalBought;
        float playerFunds = currencyTradeable?.CountHeldBy(Transactor.Colony) ?? 0f;
        float traderFunds = currencyTradeable?.CountHeldBy(Transactor.Trader) ?? 0f;
        string currencyLabel = GetCurrencyLabel(currencyTradeable).CapitalizeFirst();

        Rect leftRect = new(rect.x + 10f, rect.y + 6f, rect.width * 0.38f, rect.height - 12f);
        Rect centerRect = new(leftRect.xMax + 10f, rect.y + 6f, rect.width * 0.22f, rect.height - 12f);
        Rect rightRect = new(centerRect.xMax + 10f, rect.y + 10f, rect.xMax - centerRect.xMax - 20f, rect.height - 20f);

        DrawLabelLeft(new Rect(leftRect.x, leftRect.y, leftRect.width, 16f), $"Total Sold {FormatCurrency(totalSold)}", MorrowindUiResources.TextPrimary, GameFont.Tiny);
        DrawLabelLeft(new Rect(leftRect.x, leftRect.y + 15f, leftRect.width, 16f), $"Total Bought {FormatCurrency(totalBought)}", MorrowindUiResources.TextPrimary, GameFont.Tiny);
        DrawLabelLeft(new Rect(leftRect.x, leftRect.y + 30f, leftRect.width, 16f), $"Balance {FormatSignedCurrency(balance)}", MorrowindUiResources.TextPrimary, GameFont.Tiny);

        DrawLabelLeft(new Rect(centerRect.x, centerRect.y, centerRect.width, 16f), $"Your {currencyLabel} {FormatCurrency(playerFunds)}", MorrowindUiResources.TextPrimary, GameFont.Tiny);
        DrawLabelLeft(new Rect(centerRect.x, centerRect.y + 15f, centerRect.width, 16f), $"Seller {currencyLabel} {FormatCurrency(traderFunds)}", MorrowindUiResources.TextPrimary, GameFont.Tiny);
        if (TradeSession.giftMode && TradeSession.trader?.Faction != null)
        {
            DrawLabelLeft(new Rect(centerRect.x, centerRect.y + 30f, centerRect.width, 16f), $"Goodwill {GetGiftGoodwillChange().ToStringWithSign()}", MorrowindUiResources.TextPrimary, GameFont.Tiny);
        }

        float buttonWidth = 92f;
        float gap = 6f;
        Rect cancelRect = new(rightRect.xMax - buttonWidth, rightRect.y, buttonWidth, 28f);
        Rect resetRect = new(cancelRect.x - gap - buttonWidth, rightRect.y, buttonWidth, 28f);
        Rect acceptRect = new(resetRect.x - gap - 126f, rightRect.y, 126f, 28f);

        if (DrawButton(cancelRect, "Cancel"))
        {
            dialog.Close();
            Event.current?.Use();
        }

        if (DrawButton(resetRect, "Reset"))
        {
            TradeSession.deal.Reset();
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }

        if (DrawButton(acceptRect, BuildAcceptLabel()))
        {
            Action action = () =>
            {
                if (TradeSession.deal.TryExecute(out bool actuallyTraded))
                {
                    if (actuallyTraded)
                    {
                        SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
                        dialog.Close(doCloseSound: false);
                    }
                    else
                    {
                        dialog.Close();
                    }
                }
            };

            if (TradeSession.deal.DoesTraderHaveEnoughSilver())
            {
                action();
            }
            else
            {
                Dialog_Trade.lastCurrencyFlashTime = Time.time;
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmTraderShortFunds".Translate(), action));
            }

            Event.current?.Use();
        }
    }

    private static bool DrawButton(Rect rect, string label)
    {
        bool mouseOver = Mouse.IsOver(rect);
        MorrowindWindowSkin.DrawMainButton(rect, mouseOver);
        DrawLabelCentered(rect, label, mouseOver ? MorrowindUiResources.TextPrimary : MorrowindUiResources.Gold, GameFont.Tiny);
        return Widgets.ButtonInvisible(rect);
    }

    private static bool DrawMiniButton(Rect rect, string label, bool enabled)
    {
        bool oldEnabled = GUI.enabled;
        GUI.enabled = enabled;
        bool mouseOver = enabled && Mouse.IsOver(rect);
        MorrowindWindowSkin.DrawMainButton(rect, mouseOver);
        DrawLabelCentered(rect, label, enabled ? MorrowindUiResources.TextPrimary : MorrowindUiResources.TextMuted, GameFont.Tiny);
        bool clicked = enabled && Widgets.ButtonInvisible(rect);
        GUI.enabled = oldEnabled;
        return clicked;
    }

    private static void DrawStackCount(Rect rect, int stackCount)
    {
        if (stackCount <= 1)
        {
            return;
        }

        Rect labelRect = rect.ContractedBy(4f);
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.LowerRight;
        GUI.color = MorrowindUiResources.TextPrimary;
        Widgets.Label(labelRect, stackCount.ToStringCached());
        Text.Font = oldFont;
        Text.Anchor = oldAnchor;
        GUI.color = oldColor;
    }

    private static void DrawHeldCounts(Rect rect, Tradeable trad)
    {
        int yours = trad.CountHeldBy(Transactor.Colony);
        int trader = trad.CountHeldBy(Transactor.Trader);
        if (yours > 0)
        {
            DrawCornerCount(new Rect(rect.x + 2f, rect.y + 1f, rect.width * 0.46f, 12f), $"Y{yours}", topRight: false);
        }
        if (trader > 0)
        {
            DrawCornerCount(new Rect(rect.x + rect.width * 0.44f, rect.y + 1f, rect.width * 0.52f, 12f), $"T{trader}", topRight: true);
        }
    }

    private static void DrawCornerCount(Rect rect, string text, bool topRight)
    {
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;
        Text.Font = GameFont.Tiny;
        Text.Anchor = topRight ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        GUI.color = MorrowindUiResources.TextMuted;
        Widgets.Label(rect, text);
        Text.Font = oldFont;
        Text.Anchor = oldAnchor;
        GUI.color = oldColor;
    }

    private static void DrawThingIcon(Rect rect, Thing thing)
    {
        Texture icon = thing?.def?.uiIcon ?? BaseContent.BadTex;
        Color oldColor = GUI.color;
        GUI.color = thing?.DrawColor ?? Color.white;
        GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
        GUI.color = oldColor;
    }

    private static string BuildTradeableTooltip(Tradeable trad)
    {
        if (trad?.AnyThing == null)
        {
            return string.Empty;
        }

        Thing thing = trad.AnyThing;
        List<string> lines = new()
        {
            thing.LabelCap.ToString(),
            $"Yours: {trad.CountHeldBy(Transactor.Colony)}",
            $"Trader: {trad.CountHeldBy(Transactor.Trader)}",
            $"Buy: {FormatCurrency(trad.GetPriceFor(TradeAction.PlayerBuys))}",
            $"Sell: {FormatCurrency(trad.GetPriceFor(TradeAction.PlayerSells))}",
        };

        if (GetSoldCount(trad) > 0)
        {
            lines.Add($"Sold: {GetSoldCount(trad)}");
        }
        if (GetBoughtCount(trad) > 0)
        {
            lines.Add($"Purchased: {GetBoughtCount(trad)}");
        }
        if (!trad.TraderWillTrade && !TradeSession.giftMode)
        {
            lines.Add("Trader will not trade this item right now.");
        }

        return string.Join("\n", lines);
    }

    private static void TryQuickAdjust(Tradeable trad)
    {
        if (trad == null)
        {
            return;
        }

        if (trad.CountHeldBy(Transactor.Colony) > 0)
        {
            SetActionCount(trad, TradeAction.PlayerSells, Math.Min(trad.CountHeldBy(Transactor.Colony), GetSoldCount(trad) + 1));
            PlayAdjustSound(selling: true, increased: true);
            return;
        }

        if ((trad.TraderWillTrade || TradeSession.giftMode) && trad.CountHeldBy(Transactor.Trader) > 0)
        {
            SetActionCount(trad, TradeAction.PlayerBuys, Math.Min(trad.CountHeldBy(Transactor.Trader), GetBoughtCount(trad) + 1));
            PlayAdjustSound(selling: false, increased: true);
        }
    }

    private static void PlayAdjustSound(bool selling, bool increased)
    {
        if (increased)
        {
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }
        else
        {
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }
    }

    private static string BuildAcceptLabel()
    {
        if (TradeSession.giftMode && TradeSession.trader?.Faction != null)
        {
            return $"Offer ({GetGiftGoodwillChange().ToStringWithSign()})";
        }

        return "Accept";
    }

    private static void DrawLabelCentered(Rect rect, string text, Color color, GameFont font = GameFont.Small)
    {
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;
        Text.Font = font;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = color;
        Widgets.Label(rect, text);
        Text.Anchor = oldAnchor;
        Text.Font = oldFont;
        GUI.color = oldColor;
    }

    private static void DrawLabelLeft(Rect rect, string text, Color color, GameFont font = GameFont.Small)
    {
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;
        Text.Font = font;
        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = color;
        Widgets.Label(rect, text);
        Text.Anchor = oldAnchor;
        Text.Font = oldFont;
        GUI.color = oldColor;
    }

    private static void DrawLabelRight(Rect rect, string text, Color color, GameFont font = GameFont.Small)
    {
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;
        Text.Font = font;
        Text.Anchor = TextAnchor.MiddleRight;
        GUI.color = color;
        Widgets.Label(rect, text);
        Text.Anchor = oldAnchor;
        Text.Font = oldFont;
        GUI.color = oldColor;
    }

    private static List<Tradeable> BuildTradeables(Dialog_Trade dialog, TradeUiState state)
    {
        TransferableSorterDef sorter1 = GetSorter1(dialog) ?? TransferableSorterDefOf.Category;
        TransferableSorterDef sorter2 = GetSorter2(dialog) ?? TransferableSorterDefOf.MarketValue;

        IEnumerable<Tradeable> query = TradeSession.deal.AllTradeables
            .OfType<Tradeable>()
            .Where(trad => !trad.IsCurrency && (trad.TraderWillTrade || !TradeSession.trader.TraderKind.hideThingsNotWillingToTrade));

        if (state.sellableOnly)
        {
            query = query.Where(trad => trad.TraderWillTrade);
        }

        query = query.Where(trad => MatchesCategory(trad, state.category));

        return query
            .OrderBy(trad => trad.TraderWillTrade ? 0 : 1)
            .ThenBy(trad => trad, sorter1.Comparer)
            .ThenBy(trad => trad, sorter2.Comparer)
            .ThenBy(trad => TransferableUIUtility.DefaultListOrderPriority(trad))
            .ThenBy(trad => trad.ThingDef?.label ?? trad.LabelCap)
            .ThenBy(trad => trad.AnyThing != null && trad.AnyThing.TryGetQuality(out QualityCategory quality) ? (int)quality : -1)
            .ThenBy(trad => trad.AnyThing?.HitPoints ?? 0)
            .ToList();
    }

    private static bool MatchesCategory(Tradeable trad, TradeGridCategory category)
    {
        Thing thing = trad?.AnyThing;
        ThingDef def = thing?.def;
        if (def == null)
        {
            return false;
        }

        return category switch
        {
            TradeGridCategory.Weapon => def.IsWeapon,
            TradeGridCategory.Apparel => thing is Apparel,
            TradeGridCategory.Magic => def.IsMedicine || (def.ingestible != null && def.ingestible.drugCategory != DrugCategory.None) || (def.thingCategories != null && def.thingCategories.Any(categoryDef => categoryDef.defName.Contains("Psy", StringComparison.OrdinalIgnoreCase) || categoryDef.defName.Contains("Book", StringComparison.OrdinalIgnoreCase))),
            TradeGridCategory.Misc => !def.IsWeapon && thing is not Apparel && !def.IsMedicine && (def.ingestible == null || def.ingestible.drugCategory == DrugCategory.None),
            _ => true,
        };
    }

    private static Tradeable GetCurrencyTradeable()
    {
        return TradeSession.deal.AllTradeables
            .OfType<Tradeable>()
            .FirstOrDefault(trad => trad.IsCurrency && (TradeSession.TradeCurrency != TradeCurrency.Favor || trad.IsFavor));
    }

    private static int GetSoldCount(Tradeable trad)
    {
        return trad.ActionToDo == TradeAction.PlayerSells ? trad.CountToTransferToDestination : 0;
    }

    private static int GetBoughtCount(Tradeable trad)
    {
        return trad.ActionToDo == TradeAction.PlayerBuys ? trad.CountToTransferToSource : 0;
    }

    private static void SetActionCount(Tradeable trad, TradeAction action, int count)
    {
        count = Math.Max(0, count);
        int value;
        if (action == TradeAction.PlayerSells)
        {
            value = trad.PositiveCountDirection == TransferablePositiveCountDirection.Source ? -count : count;
        }
        else if (action == TradeAction.PlayerBuys)
        {
            value = trad.PositiveCountDirection == TransferablePositiveCountDirection.Source ? count : -count;
        }
        else
        {
            value = 0;
        }

        trad.AdjustTo(value);
        TradeSession.deal.UpdateCurrencyCount();
    }

    private static bool CanToggleGiftMode(Dialog_Trade dialog)
    {
        bool giftsOnly = GiftsOnlyField != null && (bool)GiftsOnlyField.GetValue(dialog);
        return TradeSession.trader?.Faction != null && !giftsOnly && !TradeSession.trader.Faction.def.permanentEnemy;
    }

    private static TransferableSorterDef GetSorter1(Dialog_Trade dialog)
    {
        return Sorter1Field?.GetValue(dialog) as TransferableSorterDef;
    }

    private static TransferableSorterDef GetSorter2(Dialog_Trade dialog)
    {
        return Sorter2Field?.GetValue(dialog) as TransferableSorterDef;
    }

    private static void SetSorter(Dialog_Trade dialog, bool primary, TransferableSorterDef sorter)
    {
        if (primary)
        {
            Sorter1Field?.SetValue(dialog, sorter);
        }
        else
        {
            Sorter2Field?.SetValue(dialog, sorter);
        }
    }

    private static int GetGiftGoodwillChange()
    {
        if (!TradeSession.giftMode || TradeSession.trader?.Faction == null || TradeSession.deal == null)
        {
            return 0;
        }

        try
        {
            Type utilityType = AccessTools.TypeByName("RimWorld.FactionGiftUtility") ?? AccessTools.TypeByName("FactionGiftUtility");
            MethodInfo goodwillMethod = utilityType != null
                ? AccessTools.Method(utilityType, "GetGoodwillChange", new[] { typeof(List<Tradeable>), typeof(Faction) })
                : null;

            if (goodwillMethod == null)
            {
                return 0;
            }

            object result = goodwillMethod.Invoke(null, new object[] { TradeSession.deal.AllTradeables, TradeSession.trader.Faction });
            return result is int goodwill ? goodwill : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static TradeUiState GetState(Dialog_Trade dialog)
    {
        int key = dialog.GetHashCode();
        if (!States.TryGetValue(key, out TradeUiState state))
        {
            state = new TradeUiState();
            States[key] = state;
        }

        return state;
    }

    private static Tradeable ResolveSelectedTradeable(List<Tradeable> tradeables, TradeUiState state)
    {
        if (tradeables == null || tradeables.Count == 0)
        {
            state.selectedKey = null;
            return null;
        }

        Tradeable selected = string.IsNullOrEmpty(state.selectedKey)
            ? null
            : tradeables.FirstOrDefault(trad => MakeTradeKey(trad) == state.selectedKey);

        if (selected == null)
        {
            selected = tradeables[0];
            state.selectedKey = MakeTradeKey(selected);
        }

        return selected;
    }

    private static string MakeTradeKey(Tradeable trad)
    {
        if (trad == null)
        {
            return string.Empty;
        }

        return trad.AnyThing != null
            ? $"thing:{trad.AnyThing.thingIDNumber}"
            : $"trad:{trad.GetHashCode()}:{trad.LabelCap}";
    }

    private static string GetCurrencyLabel(Tradeable currencyTradeable)
    {
        if (TradeSession.TradeCurrency == TradeCurrency.Favor)
        {
            return "favor";
        }

        return currencyTradeable?.LabelCap ?? "silver";
    }

    private static string FormatCurrency(float value)
    {
        return TradeSession.TradeCurrency == TradeCurrency.Silver
            ? value.ToStringMoney()
            : Mathf.RoundToInt(value).ToStringCached();
    }

    private static string FormatSignedCurrency(float value)
    {
        string sign = value > 0f ? "+" : value < 0f ? "-" : string.Empty;
        return sign + FormatCurrency(Mathf.Abs(value));
    }

    private static void ResetGuiState()
    {
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.WordWrap = true;
    }

    private sealed class TradeUiState
    {
        public Vector2 gridScroll = Vector2.zero;
        public Vector2 soldScroll = Vector2.zero;
        public Vector2 boughtScroll = Vector2.zero;
        public string selectedKey;
        public TradeGridCategory category = TradeGridCategory.All;
        public bool sellableOnly;
    }

    private enum TradeGridCategory
    {
        All,
        Weapon,
        Apparel,
        Magic,
        Misc,
    }
}
