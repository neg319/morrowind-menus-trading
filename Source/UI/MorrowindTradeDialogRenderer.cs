using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
    private const float LeftPaneWidth = 250f;
    private const float RowHeight = 34f;
    private const float RowGap = 4f;
    private const float ColumnGap = 6f;

    private static readonly FieldInfo ScrollPositionField = AccessTools.Field(typeof(Dialog_Trade), "scrollPosition");
    private static readonly FieldInfo GiftsOnlyField = AccessTools.Field(typeof(Dialog_Trade), "giftsOnly");
    private static readonly FieldInfo Sorter1Field = AccessTools.Field(typeof(Dialog_Trade), "sorter1");
    private static readonly FieldInfo Sorter2Field = AccessTools.Field(typeof(Dialog_Trade), "sorter2");

    public static void Draw(Dialog_Trade dialog, Rect rect)
    {
        if (dialog == null || !TradeSession.Active)
        {
            return;
        }

        try
        {
            TradeSession.deal.UpdateCurrencyCount();
            Tradeable currencyTradeable = GetCurrencyTradeable();
            List<Tradeable> tradeables = BuildTradeables(dialog);
            Vector2 scroll = GetScroll(dialog);

            MorrowindWindowSkin.DrawWindow(rect);

            Rect titleRect = new(rect.x + 10f, rect.y + 8f, rect.width - 20f, TitleHeight);
            Rect topBarRect = new(rect.x + 10f, titleRect.yMax + 5f, rect.width - 20f, TopBarHeight);
            Rect contentRect = new(rect.x + 10f, topBarRect.yMax + 8f, rect.width - 20f, rect.height - TitleHeight - TopBarHeight - FooterHeight - 34f);
            Rect footerRect = new(rect.x + 10f, contentRect.yMax + 8f, rect.width - 20f, FooterHeight);

            DrawTitleBar(titleRect);
            DrawTopBar(dialog, topBarRect);

            Rect leftRect = new(contentRect.x, contentRect.y, LeftPaneWidth, contentRect.height);
            Rect rightRect = new(leftRect.xMax + 10f, contentRect.y, contentRect.width - LeftPaneWidth - 10f, contentRect.height);

            DrawSummaryPane(leftRect, currencyTradeable, tradeables);
            DrawTradeListPane(rightRect, tradeables, ref scroll);
            SetScroll(dialog, scroll);
            DrawFooter(dialog, footerRect, tradeables);
        }
        finally
        {
            MorrowindWindowSkin.ResetTextState();
        }
    }

    private static void DrawTitleBar(Rect rect)
    {
        MorrowindWindowSkin.DrawPanel(rect, inset: 3f, darkFill: false);
        string title = TradeSession.giftMode ? "Gift Exchange" : "Barter";
        DrawLabelCentered(rect, title, MorrowindUiResources.TextPrimary, GameFont.Small);
    }

    private static void DrawTopBar(Dialog_Trade dialog, Rect rect)
    {
        MorrowindWindowSkin.DrawPanel(rect, inset: 4f);
        Rect leftRect = new(rect.x + 8f, rect.y + 6f, rect.width * 0.43f, rect.height - 12f);
        Rect rightRect = new(leftRect.xMax + 8f, rect.y + 6f, rect.xMax - leftRect.xMax - 16f, rect.height - 12f);

        string playerName = Faction.OfPlayer?.Name ?? "Colony";
        string traderName = TradeSession.trader?.TraderName ?? "Trader";
        string traderKind = TradeSession.trader?.TraderKind?.LabelCap ?? string.Empty;
        string negotiatorInfo = TradeSession.playerNegotiator == null
            ? string.Empty
            : $"Negotiator: {TradeSession.playerNegotiator.Name?.ToStringShort ?? TradeSession.playerNegotiator.LabelShort}  ({TradeSession.playerNegotiator.GetStatValue(StatDefOf.TradePriceImprovement).ToStringPercent()})";

        DrawLabelLeft(new Rect(leftRect.x, leftRect.y, leftRect.width, 18f), playerName, MorrowindUiResources.TextPrimary);
        DrawLabelRight(new Rect(leftRect.x, leftRect.y, leftRect.width, 18f), traderName, MorrowindUiResources.TextPrimary);
        DrawLabelLeft(new Rect(leftRect.x, leftRect.y + 18f, leftRect.width, 16f), negotiatorInfo, MorrowindUiResources.TextMuted, GameFont.Tiny);
        DrawLabelRight(new Rect(leftRect.x, leftRect.y + 18f, leftRect.width, 16f), traderKind, MorrowindUiResources.TextMuted, GameFont.Tiny);

        float buttonGap = 6f;
        float buttonWidth = 86f;
        float sortWidth = 92f;
        float cursorX = rightRect.xMax;

        Rect sellableRect = default;
        Rect modeRect = default;

        if (TradeSession.trader != null)
        {
            cursorX -= buttonWidth;
            sellableRect = new Rect(cursorX, rightRect.y + 10f, buttonWidth, 24f);
            cursorX -= buttonGap;
        }

        if (CanToggleGiftMode(dialog))
        {
            cursorX -= buttonWidth;
            modeRect = new Rect(cursorX, rightRect.y + 10f, buttonWidth, 24f);
            cursorX -= buttonGap;
        }

        cursorX -= sortWidth;
        Rect sorter2Rect = new(cursorX, rightRect.y + 10f, sortWidth, 24f);
        cursorX -= buttonGap + sortWidth;
        Rect sorter1Rect = new(cursorX, rightRect.y + 10f, sortWidth, 24f);

        if (TradeSession.trader != null && DrawButton(sellableRect, "Sellable"))
        {
            Find.WindowStack.Add(new Dialog_SellableItems(TradeSession.trader));
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

        DrawSorterButton(dialog, sorter1Rect, true);
        DrawSorterButton(dialog, sorter2Rect, false);
    }

    private static void DrawSorterButton(Dialog_Trade dialog, Rect rect, bool primary)
    {
        TransferableSorterDef sorter = primary ? GetSorter1(dialog) : GetSorter2(dialog);
        string label = sorter?.LabelCap ?? (primary ? "Category" : "Value");
        if (DrawButton(rect, label))
        {
            List<FloatMenuOption> options = DefDatabase<TransferableSorterDef>.AllDefsListForReading
                .OrderBy(def => def.LabelCap)
                .Select(def => new FloatMenuOption(def.LabelCap, () => SetSorter(dialog, primary, def)))
                .ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    private static void DrawSummaryPane(Rect rect, Tradeable currencyTradeable, List<Tradeable> tradeables)
    {
        MorrowindWindowSkin.DrawPanel(rect);
        Rect inner = rect.ContractedBy(8f);
        Rect headerRect = new(inner.x, inner.y, inner.width, 24f);
        DrawLabelCentered(headerRect, "Trade Summary", MorrowindUiResources.TextPrimary);

        float playerFunds = currencyTradeable?.CountHeldBy(Transactor.Colony) ?? 0f;
        float traderFunds = currencyTradeable?.CountHeldBy(Transactor.Trader) ?? 0f;
        string currencyLabel = GetCurrencyLabel(currencyTradeable);
        int soldCount = tradeables.Sum(GetSoldCount);
        int boughtCount = tradeables.Sum(GetBoughtCount);
        float totalSold = tradeables.Where(trad => trad.ActionToDo == TradeAction.PlayerSells).Sum(trad => trad.CurTotalCurrencyCostForDestination);
        float totalBought = tradeables.Where(trad => trad.ActionToDo == TradeAction.PlayerBuys).Sum(trad => trad.CurTotalCurrencyCostForSource);
        float balance = totalSold - totalBought;

        float y = headerRect.yMax + 8f;
        y = DrawSummaryLine(inner, y, $"Your {currencyLabel}", FormatCurrency(playerFunds));
        y = DrawSummaryLine(inner, y, $"Trader {currencyLabel}", FormatCurrency(traderFunds));
        y += 6f;
        y = DrawSummaryLine(inner, y, "Stacks sold", soldCount.ToString());
        y = DrawSummaryLine(inner, y, "Stacks bought", boughtCount.ToString());
        y += 6f;
        y = DrawSummaryLine(inner, y, "Total sold", FormatCurrency(totalSold));
        y = DrawSummaryLine(inner, y, "Total bought", FormatCurrency(totalBought));
        y += 6f;
        y = DrawSummaryLine(inner, y, "Balance", FormatSignedCurrency(balance), MorrowindUiResources.TextPrimary);

        if (TradeSession.giftMode && TradeSession.trader?.Faction != null)
        {
            y += 8f;
            int goodwill = GetGiftGoodwillChange();
            y = DrawSummaryLine(inner, y, "Goodwill", goodwill.ToStringWithSign(), MorrowindUiResources.TextPrimary);
        }

        Rect noteRect = new(inner.x, inner.yMax - 58f, inner.width, 52f);
        MorrowindWindowSkin.DrawFlatPanel(noteRect, MorrowindUiResources.PanelShade);
        DrawWrappedLabel(noteRect.ContractedBy(6f), TradeSession.giftMode
            ? "Gift mode is active. Purchased and sold totals show the items being offered, and goodwill appears above."
            : "Use the Sold and Purchased columns to build the deal. The balance shows whether you are coming out ahead.", MorrowindUiResources.TextMuted, GameFont.Tiny);
    }

    private static float DrawSummaryLine(Rect inner, float y, string left, string right, Color? valueColor = null)
    {
        Rect row = new(inner.x, y, inner.width, 20f);
        DrawLabelLeft(new Rect(row.x, row.y, row.width * 0.58f, row.height), left, MorrowindUiResources.TextMuted);
        DrawLabelRight(new Rect(row.x + row.width * 0.58f, row.y, row.width * 0.42f, row.height), right, valueColor ?? MorrowindUiResources.TextPrimary);
        return y + 21f;
    }

    private static void DrawTradeListPane(Rect rect, List<Tradeable> tradeables, ref Vector2 scroll)
    {
        MorrowindWindowSkin.DrawPanel(rect);
        Rect inner = rect.ContractedBy(8f);
        Rect headerRect = new(inner.x, inner.y, inner.width, 24f);
        DrawTradeHeader(headerRect);

        Rect listRect = new(inner.x, headerRect.yMax + 6f, inner.width, inner.height - headerRect.height - 10f);
        float viewHeight = Math.Max(listRect.height, tradeables.Count * (RowHeight + RowGap) + 4f);
        Rect viewRect = new(0f, 0f, listRect.width - 16f, viewHeight);
        Widgets.BeginScrollView(listRect, ref scroll, viewRect);

        float y = 0f;
        for (int i = 0; i < tradeables.Count; i++)
        {
            Rect rowRect = new(0f, y, viewRect.width, RowHeight);
            DrawTradeRow(rowRect, tradeables[i], i);
            y += RowHeight + RowGap;
        }

        Widgets.EndScrollView();
    }

    private static void DrawTradeHeader(Rect rect)
    {
        MorrowindWindowSkin.DrawFlatPanel(rect, MorrowindUiResources.PanelShade);
        GetColumns(rect, out Rect itemRect, out Rect yoursRect, out Rect traderRect, out Rect soldRect, out Rect boughtRect, out Rect priceRect, out Rect totalRect);
        DrawHeaderLabel(itemRect, "Item");
        DrawHeaderLabel(yoursRect, "Yours");
        DrawHeaderLabel(traderRect, "Trader");
        DrawHeaderLabel(soldRect, "Sold");
        DrawHeaderLabel(boughtRect, "Purchased");
        DrawHeaderLabel(priceRect, "Price");
        DrawHeaderLabel(totalRect, "Total");
    }

    private static void DrawTradeRow(Rect rect, Tradeable trad, int index)
    {
        MorrowindWindowSkin.DrawFlatPanel(rect, index % 2 == 0 ? MorrowindUiResources.PanelShadeSoft : MorrowindUiResources.PanelShade);
        if (Mouse.IsOver(rect))
        {
            GUI.color = MorrowindUiResources.SelectedOverlay;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = Color.white;
        }

        GetColumns(rect.ContractedBy(3f), out Rect itemRect, out Rect yoursRect, out Rect traderRect, out Rect soldRect, out Rect boughtRect, out Rect priceRect, out Rect totalRect);

        DrawTradeItemCell(itemRect, trad, rect);

        DrawValueCell(yoursRect, trad.CountHeldBy(Transactor.Colony).ToStringCached(), MorrowindUiResources.TextPrimary);
        DrawValueCell(traderRect, trad.CountHeldBy(Transactor.Trader).ToStringCached(), MorrowindUiResources.TextPrimary);

        if (trad.TraderWillTrade || TradeSession.giftMode)
        {
            DrawTradeCountControl(soldRect, trad, true);
            DrawTradeCountControl(boughtRect, trad, false);
        }
        else
        {
            DrawValueCell(soldRect, "-", MorrowindUiResources.TextMuted);
            DrawValueCell(boughtRect, "-", MorrowindUiResources.TextMuted);
        }

        DrawValueCell(priceRect, BuildPriceText(trad), MorrowindUiResources.TextMuted, GameFont.Tiny);
        DrawValueCell(totalRect, BuildTotalText(trad), MorrowindUiResources.TextPrimary, GameFont.Tiny);

        if (!trad.TraderWillTrade && !TradeSession.giftMode)
        {
            TooltipHandler.TipRegion(rect, "This trader will not trade this item right now.");
        }
    }

    private static void DrawTradeItemCell(Rect rect, Tradeable trad, Rect rowRect)
    {
        Thing thing = trad.AnyThing;
        Rect iconRect = new(rect.x + 2f, rect.y + 1f, Mathf.Min(30f, rect.height - 2f), Mathf.Min(30f, rect.height - 2f));
        MorrowindWindowSkin.DrawSlot(iconRect, false);
        DrawThingIcon(iconRect.ContractedBy(4f), thing, trad);
        if (thing != null && thing.stackCount > 1)
        {
            DrawLabelRight(iconRect.ContractedBy(3f), thing.stackCount.ToStringCached(), MorrowindUiResources.TextPrimary, GameFont.Tiny);
        }

        Rect labelRect = new(iconRect.xMax + 8f, rect.y, rect.width - (iconRect.width + 10f), rect.height);
        string label = BuildTradeItemLabel(trad, thing);
        DrawLabelLeft(labelRect, label, trad.TraderWillTrade || TradeSession.giftMode ? MorrowindUiResources.TextPrimary : MorrowindUiResources.TextMuted, GameFont.Tiny);
        TooltipHandler.TipRegion(rowRect, BuildTradeItemTooltip(trad, thing));
    }

    private static void DrawThingIcon(Rect rect, Thing thing, Tradeable trad)
    {
        Texture icon = thing?.def?.uiIcon ?? trad?.ThingDef?.uiIcon ?? BaseContent.BadTex;
        Color oldColor = GUI.color;
        GUI.color = thing != null ? thing.DrawColor : Color.white;
        GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
        GUI.color = oldColor;
    }

    private static string BuildTradeItemLabel(Tradeable trad, Thing thing)
    {
        string label = thing != null
            ? thing.LabelCap.ToString()
            : trad != null
                ? trad.LabelCap.ToString()
                : "Unknown item";

        if (thing == null && trad?.ThingDef?.label != null)
        {
            label = trad.ThingDef.label.CapitalizeFirst();
        }

        if (thing != null && QualityUtility.TryGetQuality(thing, out QualityCategory quality))
        {
            label += $" ({quality.GetLabel().CapitalizeFirst()})";
        }

        return label;
    }

    private static string BuildTradeItemTooltip(Tradeable trad, Thing thing)
    {
        StringBuilder builder = new();
        builder.AppendLine(BuildTradeItemLabel(trad, thing));
        builder.AppendLine();
        builder.AppendLine($"Yours: {trad.CountHeldBy(Transactor.Colony)}");
        builder.AppendLine($"Trader: {trad.CountHeldBy(Transactor.Trader)}");
        builder.AppendLine($"Buy price: {FormatCurrency(trad.GetPriceFor(TradeAction.PlayerBuys))}");
        builder.AppendLine($"Sell price: {FormatCurrency(trad.GetPriceFor(TradeAction.PlayerSells))}");

        if (thing != null)
        {
            if (thing.stackCount > 1)
            {
                builder.AppendLine($"Stack: {thing.stackCount}");
            }

            if (thing.MarketValue > 0f)
            {
                builder.AppendLine($"Market value: {thing.MarketValue:0.##}");
            }

            if (QualityUtility.TryGetQuality(thing, out QualityCategory thingQuality))
            {
                builder.AppendLine($"Quality: {thingQuality.GetLabel().CapitalizeFirst()}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static void DrawTradeCountControl(Rect rect, Tradeable trad, bool selling)
    {
        MorrowindWindowSkin.DrawFlatPanel(rect, MorrowindUiResources.PanelShade);
        int count = selling ? GetSoldCount(trad) : GetBoughtCount(trad);
        int max = selling ? trad.CountHeldBy(Transactor.Colony) : trad.CountHeldBy(Transactor.Trader);
        int step = Math.Max(1, GenUI.CurrentAdjustmentMultiplier());

        float allWidth = 24f;
        float buttonWidth = 16f;
        float countWidth = rect.width - allWidth - buttonWidth - buttonWidth - 6f;
        float x = rect.x;
        Rect allRect = new(x, rect.y, allWidth, rect.height);
        x += allWidth + 2f;
        Rect minusRect = new(x, rect.y, buttonWidth, rect.height);
        x += buttonWidth + 2f;
        Rect countRect = new(x, rect.y, countWidth, rect.height);
        x += countWidth + 2f;
        Rect plusRect = new(x, rect.y, buttonWidth, rect.height);

        bool canAll = max > 0;
        bool canMinus = count > 0;
        bool canPlus = count < max;
        if (DrawMiniButton(allRect, "All", canAll))
        {
            SetActionCount(trad, selling ? TradeAction.PlayerSells : TradeAction.PlayerBuys, max);
            PlayAdjustSound(selling, max > count);
        }
        if (DrawMiniButton(minusRect, "-", canMinus))
        {
            SetActionCount(trad, selling ? TradeAction.PlayerSells : TradeAction.PlayerBuys, Math.Max(0, count - step));
            PlayAdjustSound(selling, false);
        }

        MorrowindWindowSkin.DrawPanel(countRect, inset: 2f);
        DrawLabelCentered(countRect, count.ToStringCached(), count > 0 ? MorrowindUiResources.TextPrimary : MorrowindUiResources.TextMuted, GameFont.Tiny);

        if (DrawMiniButton(plusRect, "+", canPlus))
        {
            SetActionCount(trad, selling ? TradeAction.PlayerSells : TradeAction.PlayerBuys, Math.Min(max, count + step));
            PlayAdjustSound(selling, true);
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

    private static string BuildPriceText(Tradeable trad)
    {
        if (!trad.TraderWillTrade && !TradeSession.giftMode)
        {
            return "No trade";
        }

        int soldCount = GetSoldCount(trad);
        int boughtCount = GetBoughtCount(trad);
        if (soldCount > 0)
        {
            return FormatCurrency(trad.GetPriceFor(TradeAction.PlayerSells));
        }

        if (boughtCount > 0)
        {
            return FormatCurrency(trad.GetPriceFor(TradeAction.PlayerBuys));
        }

        return $"B {FormatCurrency(trad.GetPriceFor(TradeAction.PlayerBuys))} / S {FormatCurrency(trad.GetPriceFor(TradeAction.PlayerSells))}";
    }

    private static string BuildTotalText(Tradeable trad)
    {
        return trad.ActionToDo switch
        {
            TradeAction.PlayerSells => FormatSignedCurrency(trad.CurTotalCurrencyCostForDestination),
            TradeAction.PlayerBuys => FormatSignedCurrency(-trad.CurTotalCurrencyCostForSource),
            _ => "0",
        };
    }

    private static void DrawFooter(Dialog_Trade dialog, Rect rect, List<Tradeable> tradeables)
    {
        TradeSession.deal.UpdateCurrencyCount();
        MorrowindWindowSkin.DrawPanel(rect, inset: 3f);

        float totalSold = tradeables.Where(trad => trad.ActionToDo == TradeAction.PlayerSells).Sum(trad => trad.CurTotalCurrencyCostForDestination);
        float totalBought = tradeables.Where(trad => trad.ActionToDo == TradeAction.PlayerBuys).Sum(trad => trad.CurTotalCurrencyCostForSource);
        float balance = totalSold - totalBought;
        string summary = $"Sold {FormatCurrency(totalSold)}    Bought {FormatCurrency(totalBought)}    Balance {FormatSignedCurrency(balance)}";
        DrawLabelLeft(new Rect(rect.x + 10f, rect.y + 8f, rect.width * 0.50f, 32f), summary, MorrowindUiResources.TextPrimary);

        float buttonWidth = 92f;
        float gap = 6f;
        Rect cancelRect = new(rect.xMax - buttonWidth, rect.y + 11f, buttonWidth, 28f);
        Rect resetRect = new(cancelRect.x - gap - buttonWidth, cancelRect.y, buttonWidth, cancelRect.height);
        Rect acceptRect = new(resetRect.x - gap - 126f, cancelRect.y, 126f, cancelRect.height);

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

        string acceptLabel = BuildAcceptLabel();
        if (DrawButton(acceptRect, acceptLabel))
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

    private static string BuildAcceptLabel()
    {
        if (TradeSession.giftMode && TradeSession.trader?.Faction != null)
        {
            int goodwill = GetGiftGoodwillChange();
            return $"Offer ({goodwill.ToStringWithSign()})";
        }

        return "Accept";
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

    private static void DrawHeaderLabel(Rect rect, string text)
    {
        DrawLabelCentered(rect, text, MorrowindUiResources.TextPrimary, GameFont.Tiny);
    }

    private static void DrawValueCell(Rect rect, string text, Color color, GameFont font = GameFont.Small)
    {
        DrawLabelCentered(rect, text, color, font);
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

    private static void DrawWrappedLabel(Rect rect, string text, Color color, GameFont font)
    {
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;
        bool oldWrap = Text.WordWrap;
        Text.Font = font;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.WordWrap = true;
        GUI.color = color;
        Widgets.Label(rect, text);
        Text.WordWrap = oldWrap;
        Text.Anchor = oldAnchor;
        Text.Font = oldFont;
        GUI.color = oldColor;
    }

    private static void GetColumns(Rect rect, out Rect itemRect, out Rect yoursRect, out Rect traderRect, out Rect soldRect, out Rect boughtRect, out Rect priceRect, out Rect totalRect)
    {
        float itemWidth = Mathf.Clamp(rect.width * 0.31f, 176f, 220f);
        float ownedWidth = 44f;
        float controlWidth = Mathf.Clamp(rect.width * 0.15f, 92f, 104f);
        float priceWidth = 84f;

        float x = rect.x;
        itemRect = new Rect(x, rect.y, itemWidth, rect.height);
        x = itemRect.xMax + ColumnGap;
        yoursRect = new Rect(x, rect.y, ownedWidth, rect.height);
        x = yoursRect.xMax + ColumnGap;
        traderRect = new Rect(x, rect.y, ownedWidth, rect.height);
        x = traderRect.xMax + ColumnGap;
        soldRect = new Rect(x, rect.y, controlWidth, rect.height);
        x = soldRect.xMax + ColumnGap;
        boughtRect = new Rect(x, rect.y, controlWidth, rect.height);
        x = boughtRect.xMax + ColumnGap;
        priceRect = new Rect(x, rect.y, priceWidth, rect.height);
        x = priceRect.xMax + ColumnGap;
        totalRect = new Rect(x, rect.y, Math.Max(64f, rect.xMax - x), rect.height);
    }

    private static List<Tradeable> BuildTradeables(Dialog_Trade dialog)
    {
        TransferableSorterDef sorter1 = GetSorter1(dialog) ?? TransferableSorterDefOf.Category;
        TransferableSorterDef sorter2 = GetSorter2(dialog) ?? TransferableSorterDefOf.MarketValue;

        return TradeSession.deal.AllTradeables
            .OfType<Tradeable>()
            .Where(trad => !trad.IsCurrency && (trad.TraderWillTrade || !TradeSession.trader.TraderKind.hideThingsNotWillingToTrade))
            .OrderBy(trad => trad.TraderWillTrade ? 0 : 1)
            .ThenBy(trad => trad, sorter1.Comparer)
            .ThenBy(trad => trad, sorter2.Comparer)
            .ThenBy(trad => TransferableUIUtility.DefaultListOrderPriority(trad))
            .ThenBy(trad => trad.ThingDef?.label ?? trad.LabelCap)
            .ThenBy(trad => trad.AnyThing != null && trad.AnyThing.TryGetQuality(out QualityCategory quality) ? (int)quality : -1)
            .ThenBy(trad => trad.AnyThing?.HitPoints ?? 0)
            .ToList();
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

    private static Vector2 GetScroll(Dialog_Trade dialog)
    {
        return ScrollPositionField != null ? (Vector2)ScrollPositionField.GetValue(dialog) : Vector2.zero;
    }

    private static void SetScroll(Dialog_Trade dialog, Vector2 value)
    {
        ScrollPositionField?.SetValue(dialog, value);
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
            : Mathf.RoundToInt(value).ToString();
    }

    private static string FormatSignedCurrency(float value)
    {
        string sign = value > 0f ? "+" : value < 0f ? "-" : string.Empty;
        return sign + FormatCurrency(Mathf.Abs(value));
    }
}
