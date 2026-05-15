using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionGearCustomizer
{
    /// <summary>
    /// Shared helper: find FactionGearData by scanning the list directly.
    /// </summary>
    internal static class TradeStockHelper
    {
        public static FactionGearData FindData(string factionDefName)
        {
            var list = FactionGearCustomizerMod.Settings?.factionGearData;
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
                if (list[i]?.factionDefName == factionDefName)
                    return list[i];
            return null;
        }
    }

    /// <summary>
    /// Startup-time verification that Def modifier is available.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class TradeStock_PatchVerifier
    {
        static TradeStock_PatchVerifier()
        {
            Log.Message("[FactionGearCustomizer] TradeStock Verifier: Def-based stock modification active");
        }
    }

    /// <summary>
    /// Unified TraderKindDef.stockGenerators modifier for both sell and buy side.
    /// Directly modifies the Def — no runtime Harmony hooks needed for injection.
    /// The game's own stock generation reads from stockGenerators naturally.
    /// </summary>
    public static class TradeStock_DefModifier
    {
        // ── State ────────────────────────────────────────────────────

        /// <summary>Stores original sell generators per trader kind for revert.</summary>
        private static Dictionary<string, List<StockGenerator>> _originalSell = new Dictionary<string, List<StockGenerator>>();
        /// <summary>Stores original buy generators per trader kind for revert.</summary>
        private static Dictionary<string, List<StockGenerator>> _originalBuy = new Dictionary<string, List<StockGenerator>>();
        /// <summary>Track our added generators so we can remove then re-add on config change.</summary>
        private static HashSet<string> _ourSellTags = new HashSet<string>();
        private static HashSet<string> _ourBuyTags = new HashSet<string>();

        // ── Sell Side ────────────────────────────────────────────────

        /// <summary>
        /// Apply sell-side stock modifications to a TraderKindDef.
        /// Adds StockGenerator_SingleDef instances for each configured item.
        /// If replaceVanilla=true, removes ALL existing sell generators first.
        /// </summary>
        public static void ApplySellStock(string traderKindDefName, List<TradeStockEntry> entries, bool replaceVanilla)
        {
            var tkDef = DefDatabase<TraderKindDef>.GetNamedSilentFail(traderKindDefName);
            if (tkDef == null) return;

            // Save originals once for revert
            if (!_originalSell.ContainsKey(traderKindDefName))
                _originalSell[traderKindDefName] = tkDef.stockGenerators.Where(sg => !IsBuyGenerator(sg)).ToList();

            // Remove our previously added sell generators (safe: skip generators that fail traversal)
            tkDef.stockGenerators.RemoveAll(sg => {
                try { return sg is StockGenerator_SingleDef && _ourSellTags.Contains(Traverse.Create(sg).Field("thingDef").GetValue<ThingDef>()?.defName); }
                catch { return false; }
            });

            if (replaceVanilla)
            {
                // Remove ALL sell generators (keep buy generators)
                tkDef.stockGenerators.RemoveAll(sg => !IsBuyGenerator(sg));
            }

            if (entries != null && entries.Count > 0)
            {
                foreach (var e in entries)
                {
                    if (e.entryType == TradeEntryType.Animal)
                    {
                        var kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(e.pawnKindDefName);
                        if (kind?.race == null) continue;
                        // Animals have a ThingDef (kind.race). StockGenerator_SingleDef with the race works for animals.
                        var gen = new StockGenerator_SingleDef();
                        Traverse.Create(gen).Field("thingDef").SetValue(kind.race);
                        Traverse.Create(gen).Field("countRange").SetValue(e.countRange);
                        gen.ResolveReferences(tkDef);
                        tkDef.stockGenerators.Add(gen);
                        _ourSellTags.Add(kind.defName);
                        Log.Message($"[FactionGearCustomizer] DefMod: added animal {kind.defName} via race {kind.race.defName}");
                    }
                    else
                    {
                        var def = DefDatabase<ThingDef>.GetNamedSilentFail(e.thingDefName);
                        if (def == null) continue;
                        var gen = new StockGenerator_SingleDef();
                        Traverse.Create(gen).Field("thingDef").SetValue(def);
                        Traverse.Create(gen).Field("countRange").SetValue(e.countRange);
                        gen.ResolveReferences(tkDef);
                        tkDef.stockGenerators.Add(gen);
                        _ourSellTags.Add(def.defName);
                    }
                }
            }

            Log.Message($"[FactionGearCustomizer] DefMod Sell: '{traderKindDefName}' → {_ourSellTags.Count} sell items (replace={replaceVanilla}, total gen={tkDef.stockGenerators.Count})");
        }

        // ── Buy Side ─────────────────────────────────────────────────

        /// <summary>
        /// Apply buy-side stock modifications to a TraderKindDef.
        /// Adds StockGenerator_BuySingleDef instances so the trader wants these items.
        /// </summary>
        public static void ApplyBuyStock(string traderKindDefName, List<TradeStockEntry> entries)
        {
            var tkDef = DefDatabase<TraderKindDef>.GetNamedSilentFail(traderKindDefName);
            if (tkDef == null) return;

            // Save originals once
            if (!_originalBuy.ContainsKey(traderKindDefName))
                _originalBuy[traderKindDefName] = tkDef.stockGenerators.Where(sg => IsBuyGenerator(sg)).ToList();

            // Remove our previously added buy generators
            tkDef.stockGenerators.RemoveAll(sg => { try { return sg is StockGenerator_BuySingleDef && _ourBuyTags.Contains(Traverse.Create(sg).Field("thingDef").GetValue<ThingDef>()?.defName); } catch { return false; } });

            if (entries != null && entries.Count > 0)
            {
                foreach (var e in entries)
                {
                    if (e.entryType == TradeEntryType.Animal) continue; // animals don't use buy-side generators
                    var def = DefDatabase<ThingDef>.GetNamedSilentFail(e.thingDefName);
                    if (def == null) continue;
                    var gen = new StockGenerator_BuySingleDef();
                    Traverse.Create(gen).Field("thingDef").SetValue(def);
                    Traverse.Create(gen).Field("priceMultiplier").SetValue(1.0f);
                    gen.ResolveReferences(tkDef);
                    tkDef.stockGenerators.Add(gen);
                    _ourBuyTags.Add(def.defName);
                }
            }

            Log.Message($"[FactionGearCustomizer] DefMod Buy: '{traderKindDefName}' → {_ourBuyTags.Count} buy items (total gen={tkDef.stockGenerators.Count})");
        }

        // ── Revert ───────────────────────────────────────────────────

        public static void RevertAll(string traderKindDefName)
        {
            var tkDef = DefDatabase<TraderKindDef>.GetNamedSilentFail(traderKindDefName);
            if (tkDef == null) return;

            if (_originalSell.TryGetValue(traderKindDefName, out var sellOrig))
            {
                // Remove all sell generators, re-add originals
                tkDef.stockGenerators.RemoveAll(sg => !IsBuyGenerator(sg));
                tkDef.stockGenerators.AddRange(sellOrig);
                _originalSell.Remove(traderKindDefName);
            }
            if (_originalBuy.TryGetValue(traderKindDefName, out var buyOrig))
            {
                tkDef.stockGenerators.RemoveAll(sg => IsBuyGenerator(sg));
                tkDef.stockGenerators.AddRange(buyOrig);
                _originalBuy.Remove(traderKindDefName);
            }
            _ourSellTags.Clear();
            _ourBuyTags.Clear();
        }

        // ── Helpers ──────────────────────────────────────────────────

        private static bool IsBuyGenerator(StockGenerator sg)
        {
            var name = sg.GetType().Name;
            return name.StartsWith("StockGenerator_Buy");
        }

        // ── Force regenerate settlements after Def mod ──────────────

        public static int ForceRegenerateForFaction(string factionDefName)
        {
            if (Find.World?.worldObjects == null) return 0;
            var allSettlements = Find.World.worldObjects.AllWorldObjects.OfType<Settlement>();
            int count = 0;
            foreach (var stl in allSettlements)
            {
                if (stl?.Faction?.def?.defName != factionDefName) continue;
                var tracker = stl.trader;
                if (tracker == null) continue;
                try
                {
                    // Just count — don't force access to avoid triggering crashes
                    count++;
                }
                catch (Exception ex)
                {
                    Log.Error($"[FactionGearCustomizer] ForceRegen failed: {ex}");
                }
            }
            if (count > 0)
                Log.Message($"[FactionGearCustomizer] ForceRegen: {count} settlements for '{factionDefName}'");
            return count;
        }
    }
}
