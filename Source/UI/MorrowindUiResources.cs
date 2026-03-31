using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.UI;

public static class MorrowindUiResources
{
    private static Texture2D background;
    private static Texture2D tabActive;
    private static Texture2D tabInactive;
    private static Texture2D slotFrame;
    private static Texture2D frameCornerTl;
    private static Texture2D frameCornerTr;
    private static Texture2D frameCornerBl;
    private static Texture2D frameCornerBr;
    private static Texture2D paperdollSilhouette;

    public static Texture2D Background => background ??= ContentFinder<Texture2D>.Get("UI/Morrowind/inventory_window_bg", true);
    public static Texture2D TabActive => tabActive ??= ContentFinder<Texture2D>.Get("UI/Morrowind/tab_active", true);
    public static Texture2D TabInactive => tabInactive ??= ContentFinder<Texture2D>.Get("UI/Morrowind/tab_inactive", false) ?? TabActive;
    public static Texture2D SlotFrame => slotFrame ??= ContentFinder<Texture2D>.Get("UI/Morrowind/slot_frame", true);
    public static Texture2D FrameCornerTl => frameCornerTl ??= ContentFinder<Texture2D>.Get("UI/Morrowind/frame_corner_tl", true);
    public static Texture2D FrameCornerTr => frameCornerTr ??= ContentFinder<Texture2D>.Get("UI/Morrowind/frame_corner_tr", false) ?? FrameCornerTl;
    public static Texture2D FrameCornerBl => frameCornerBl ??= ContentFinder<Texture2D>.Get("UI/Morrowind/frame_corner_bl", false) ?? FrameCornerTl;
    public static Texture2D FrameCornerBr => frameCornerBr ??= ContentFinder<Texture2D>.Get("UI/Morrowind/frame_corner_br", false) ?? FrameCornerTl;
    public static Texture2D PaperdollSilhouette => paperdollSilhouette ??= ContentFinder<Texture2D>.Get("UI/Morrowind/paperdoll_silhouette", true);

    public static readonly Color Gold = new(0.72f, 0.62f, 0.42f);
    public static readonly Color GoldSoft = new(0.53f, 0.44f, 0.27f);
    public static readonly Color GoldDark = new(0.26f, 0.20f, 0.10f);
    public static readonly Color BackgroundTint = new(0.01f, 0.01f, 0.01f, 0.98f);
    public static readonly Color PanelShade = new(0.005f, 0.005f, 0.005f, 0.96f);
    public static readonly Color PanelShadeSoft = new(0.02f, 0.02f, 0.02f, 0.90f);
    public static readonly Color SlotShade = new(0f, 0f, 0f, 0.88f);
    public static readonly Color SlotBorderDark = new(0.07f, 0.06f, 0.05f, 1f);
    public static readonly Color SlotBorderLight = new(0.15f, 0.13f, 0.10f, 1f);
    public static readonly Color SelectedOverlay = new(0.96f, 0.83f, 0.34f, 0.16f);
    public static readonly Color ActiveTabText = new(0.44f, 0.48f, 0.86f);
    public static readonly Color CarryWeightFill = new(0.12f, 0.18f, 0.42f, 0.78f);
    public static readonly Color InactiveTabText = new(0.88f, 0.84f, 0.73f);
    public static readonly Color TextPrimary = new(0.92f, 0.88f, 0.80f);
    public static readonly Color TextMuted = new(0.74f, 0.68f, 0.58f);
}
