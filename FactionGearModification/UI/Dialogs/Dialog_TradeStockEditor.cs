using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using FactionGearCustomizer.Utils;
using FactionGearCustomizer.UI.Pickers;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_TradeStockEditor : Window
    {
        private readonly FactionDef factionDef;
        private readonly FactionGearData factionData;

        private class TraderTypeOption { public string label, category, traderKindDefName; }
        private List<TraderTypeOption> traderTypes;
        private int selectedTraderTypeIdx;
        private string selectedTraderKindDefName;

        private List<TradeStockEntry> bufferTradeStock, bufferBuyStock;
        private bool bufferReplaceVanilla;

        private class Snap { public List<TradeStockEntry> e; public bool r; }
        private Stack<Snap> undo = new Stack<Snap>(), redo = new Stack<Snap>();

        private TradeStockCategory currentCat = TradeStockCategory.All;
        private static readonly TradeStockCategory[] Cats = { TradeStockCategory.All, TradeStockCategory.Weapons, TradeStockCategory.Armor, TradeStockCategory.Apparel, TradeStockCategory.Items };

        private List<ThingDef> allItems = new List<ThingDef>();
        private List<ThingDef> filtered = new List<ThingDef>();
        private ThingPickerFilterState filterState = new ThingPickerFilterState();
        private List<string> allMods = new List<string>();
        private bool filterDirty = true;
        private HashSet<string> configuredDefs = new HashSet<string>();

        private Vector2 libScroll, cfgScroll;
        private const float LibRowH = 52f, CfgRowH = 72f;
        public override Vector2 InitialSize => new Vector2(1060f, 760f);

        public Dialog_TradeStockEditor(FactionDef fDef, FactionGearData fData)
        {
            factionDef = fDef ?? throw new ArgumentNullException(nameof(fDef));
            factionData = fData ?? throw new ArgumentNullException(nameof(fData));
            doCloseX = true; forcePause = true; absorbInputAroundWindow = true;
            draggable = true; resizeable = true; closeOnClickedOutside = true;
            filterState.SortField = "Name"; filterState.SortAscending = true;
            BuildTraderTypes(); selectedTraderTypeIdx = 0;
            bufferBuyStock = new List<TradeStockEntry>();
            LoadTraderType(); BuildAllItems(); RebuildIndex(); ApplyFilter();
        }

        private void BuildTraderTypes()
        {
            traderTypes = new List<TraderTypeOption>();
            traderTypes.Add(new TraderTypeOption { label = "Default (All Types)", category = "", traderKindDefName = null });
            AddTT("Settlement", factionDef.baseTraderKinds); AddTT("Caravan", factionDef.caravanTraderKinds);
            AddTT("Orbital", factionDef.orbitalTraderKinds); AddTT("Visitor", factionDef.visitorTraderKinds);
        }
        private void AddTT(string cat, List<TraderKindDef> kinds) { if (kinds == null) return; foreach (var tk in kinds) if (tk != null) traderTypes.Add(new TraderTypeOption { label = tk.label ?? tk.defName, category = cat, traderKindDefName = tk.defName }); }

        private void PushU() { undo.Push(new Snap { e = bufferTradeStock.Select(x => x.DeepCopy()).ToList(), r = bufferReplaceVanilla }); redo.Clear(); if (undo.Count > 50) undo = new Stack<Snap>(undo.Reverse().Take(50).Reverse()); }
        private void DoU() { if (undo.Count == 0) return; redo.Push(new Snap { e = bufferTradeStock.Select(x => x.DeepCopy()).ToList(), r = bufferReplaceVanilla }); var s = undo.Pop(); bufferTradeStock = s.e; bufferReplaceVanilla = s.r; RebuildIndex(); }
        private void DoR() { if (redo.Count == 0) return; undo.Push(new Snap { e = bufferTradeStock.Select(x => x.DeepCopy()).ToList(), r = bufferReplaceVanilla }); var s = redo.Pop(); bufferTradeStock = s.e; bufferReplaceVanilla = s.r; RebuildIndex(); }

        private void LoadTraderType()
        {
            var dn = selectedTraderKindDefName;
            bufferReplaceVanilla = dn == null ? factionData.ReplaceVanillaTradeStock : (factionData.ReplaceVanillaByType != null && factionData.ReplaceVanillaByType.TryGetValue(dn, out var rv) && rv);
            var src = dn == null ? factionData.CustomTradeStock : (factionData.CustomTradeStockByType != null && factionData.CustomTradeStockByType.TryGetValue(dn, out var l) ? l : null);
            bufferTradeStock = new List<TradeStockEntry>();
            if (src != null) foreach (var e in src) bufferTradeStock.Add(e.DeepCopy());
            RebuildIndex(); undo.Clear(); redo.Clear();
        }

        private static List<TradeStockEntry> ExtractVanilla(TraderKindDef tkd)
        {
            var r = new List<TradeStockEntry>(); if (tkd.stockGenerators == null) return r;
            foreach (var sg in tkd.stockGenerators) { if (sg == null) continue; var cr = sg.countRange;
                var single = Traverse.Create(sg).Field("thingDef").GetValue<ThingDef>(); if (single != null) { r.Add(new TradeStockEntry(single.defName) { countRange = cr }); continue; }
                var multi = Traverse.Create(sg).Field("thingDefs").GetValue<List<ThingDef>>(); if (multi != null) { foreach (var d in multi) if (d != null) r.Add(new TradeStockEntry(d.defName) { countRange = cr }); continue; }
                var tag = Traverse.Create(sg).Field("tradeTag").GetValue<string>(); if (!string.IsNullOrEmpty(tag)) { foreach (var d in DefDatabase<ThingDef>.AllDefs.Where(x => x.tradeTags?.Contains(tag) == true && x.BaseMarketValue > 0).Take(3)) r.Add(new TradeStockEntry(d.defName) { countRange = cr }); continue; }
                var cat = Traverse.Create(sg).Field("categoryDef").GetValue<ThingCategoryDef>(); if (cat != null) { foreach (var d in DefDatabase<ThingDef>.AllDefs.Where(x => x.thingCategories?.Contains(cat) == true && x.BaseMarketValue > 0).Take(3)) r.Add(new TradeStockEntry(d.defName) { countRange = cr }); }
            } return r;
        }

        private void SaveTraderType()
        {
            var dn = selectedTraderKindDefName;
            var list = bufferTradeStock.Count > 0 ? bufferTradeStock.Select(e => e.DeepCopy()).ToList() : null;
            if (dn == null) { factionData.ReplaceVanillaTradeStock = bufferReplaceVanilla; factionData.CustomTradeStock = list; }
            else { if (factionData.CustomTradeStockByType == null) factionData.CustomTradeStockByType = new Dictionary<string, List<TradeStockEntry>>(); if (factionData.ReplaceVanillaByType == null) factionData.ReplaceVanillaByType = new Dictionary<string, bool>(); factionData.ReplaceVanillaByType[dn] = bufferReplaceVanilla; if (list != null) factionData.CustomTradeStockByType[dn] = list; else factionData.CustomTradeStockByType.Remove(dn); }
            var buyList = bufferBuyStock.Count > 0 ? bufferBuyStock.Select(e => e.DeepCopy()).ToList() : null;
            if (dn == null) factionData.CustomBuyStock = buyList;
            else { if (factionData.CustomBuyStockByType == null) factionData.CustomBuyStockByType = new Dictionary<string, List<TradeStockEntry>>(); if (buyList != null) factionData.CustomBuyStockByType[dn] = buyList; else factionData.CustomBuyStockByType.Remove(dn); }
        }

        private void SwitchTT(int idx) { SaveTraderType(); selectedTraderTypeIdx = idx; selectedTraderKindDefName = traderTypes[idx].traderKindDefName; LoadTraderType(); libScroll = cfgScroll = Vector2.zero; }

        private void BuildAllItems()
        {
            allItems = DefDatabase<ThingDef>.AllDefs.Where(IsTradeable).OrderBy(d => d.LabelCap.ToString()).ToList();
            allMods = allItems.Select(d => d.modContentPack?.Name ?? "Core").Distinct().OrderBy(m => m).ToList();
            filterState.SyncAvailableMods(allMods);
            filterState.MaxMarketValue = allItems.Any() ? allItems.Max(d => d.BaseMarketValue) : 100000f;
            filterState.MarketValue = new FloatRange(0f, filterState.MaxMarketValue);
        }
        private static bool IsTradeable(ThingDef d) { if (d == null || d.destroyOnDrop || d.BaseMarketValue <= 0f || d.IsCorpse) return false; if (d.defName.Contains("Corpse") || d.defName.Contains("Filth")) return false; if (d.thingClass != null && d.thingClass.Name.Contains("Corpse")) return false; return true; }

        private void RebuildIndex() { configuredDefs.Clear(); foreach (var e in bufferTradeStock) { var key = e.entryType == TradeEntryType.Animal ? e.pawnKindDefName : e.thingDefName; if (!string.IsNullOrEmpty(key)) configuredDefs.Add(key); } }

        private bool MatchesCategoryFilter(ThingDef d)
        {
            if (!filterState.SelectedCategory.HasValue) return true;
            switch (filterState.SelectedCategory.Value)
            {
                case ItemCategoryFilter.Food:
                    if (!d.IsIngestible || d.ingestible == null) return false;
                    var ft = d.ingestible.foodType;
                    if (ft == FoodTypeFlags.None) return false;
                    if (filterState.SelectedFoodCategories.Count > 0)
                    {
                        bool m = false;
                        if (filterState.SelectedFoodCategories.Contains("Cooked") && ft == FoodTypeFlags.Meal) m = true;
                        if (filterState.SelectedFoodCategories.Contains("Raw") && (ft == FoodTypeFlags.Plant || ft == FoodTypeFlags.AnimalProduct || ft == FoodTypeFlags.Processed)) m = true;
                        if (filterState.SelectedFoodCategories.Contains("Meat") && ft == FoodTypeFlags.Meat) m = true;
                        if (filterState.SelectedFoodCategories.Contains("Vegetable") && ft == FoodTypeFlags.VegetableOrFruit) m = true;
                        if (!m) return false;
                    }
                    return true;
                case ItemCategoryFilter.Medicine: return d.IsMedicine;
                case ItemCategoryFilter.SocialDrug: return d.IsDrug && d.ingestible?.drugCategory == DrugCategory.Social;
                case ItemCategoryFilter.HardDrug: return d.IsDrug && (d.ingestible?.drugCategory == DrugCategory.Hard || d.ingestible?.drugCategory == DrugCategory.Medical);
                case ItemCategoryFilter.Ammo:
                    if (!(d.defName.Contains("Ammo") || d.defName.Contains("Bullet") || d.defName.Contains("Shell") || (d.thingCategories?.Any(c => c.defName == "Ammo") ?? false))) return false;
                    if (filterState.SelectedAmmoSets.Count > 0) { var lbl = d.LabelCap.ToString(); var p = lbl.IndexOf('('); var caliber = p > 0 ? lbl.Substring(0, p).Trim() : lbl; if (!filterState.SelectedAmmoSets.Contains(caliber)) return false; }
                    return true;
            }
            return true;
        }

        private void ApplyFilter()
        {
            var src = allItems.AsEnumerable();
            // Category tabs
            switch (currentCat) { case TradeStockCategory.Weapons: src = src.Where(d => d.IsWeapon); break; case TradeStockCategory.Armor: src = src.Where(d => d.IsApparel && IsArmor(d)); break; case TradeStockCategory.Apparel: src = src.Where(d => d.IsApparel && !IsArmor(d)); break; case TradeStockCategory.Items: src = src.Where(d => !d.IsWeapon && !d.IsApparel); break; }
            // ThingPickerFilterBar category filter (sub-filter)
            src = src.Where(MatchesCategoryFilter);
            if (filterState.SelectedTechLevel.HasValue) src = src.Where(d => (int)d.techLevel <= (int)filterState.SelectedTechLevel.Value);
            if (filterState.SelectedMods.Count > 0 && filterState.SelectedMods.Count < allMods.Count) src = src.Where(d => filterState.SelectedMods.Contains(d.modContentPack?.Name ?? "Core"));
            if (filterState.MarketValue.min > 0f) src = src.Where(d => d.BaseMarketValue >= filterState.MarketValue.min);
            if (filterState.MarketValue.max < filterState.MaxMarketValue) src = src.Where(d => d.BaseMarketValue <= filterState.MarketValue.max);
            if (!string.IsNullOrEmpty(filterState.SearchText)) { var lo = filterState.SearchText.ToLowerInvariant(); src = src.Where(d => (d.LabelCap.ToString()?.ToLowerInvariant()?.Contains(lo) ?? false) || (d.defName?.ToLowerInvariant()?.Contains(lo) ?? false)); }
            var ordered = filterState.SortAscending ? src.OrderBy(SortVal) : src.OrderByDescending(SortVal);
            filtered = ordered.ToList();
            filterDirty = false;
        }
        private object SortVal(ThingDef d) { switch (filterState.SortField) { case "MarketValue": return d.BaseMarketValue; case "TechLevel": return (int)d.techLevel; case "Mass": return d.BaseMass; default: return d.LabelCap.ToString(); } }
        private static bool IsArmor(ThingDef d) { if (d.apparel?.layers == null) return false; return d.apparel.layers.Contains(ApparelLayerDefOf.Shell) || d.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) >= 0.4f; }
        private static string GetTradeTagLabel(ThingDef d) { if (d.tradeTags == null || d.tradeTags.Count == 0) return "—"; return string.Join(", ", d.tradeTags); }

        private void ToggleEntry(ThingDef def) { PushU(); if (configuredDefs.Contains(def.defName)) bufferTradeStock.RemoveAll(e => e.thingDefName == def.defName); else bufferTradeStock.Add(new TradeStockEntry(def.defName)); RebuildIndex(); }
        private void RemoveAt(int i) { PushU(); bufferTradeStock.RemoveAt(i); RebuildIndex(); }
        private int GetConfigCountForType(string tkdn) { if (tkdn == null) return factionData.CustomTradeStock?.Count ?? 0; if (factionData.CustomTradeStockByType != null && factionData.CustomTradeStockByType.TryGetValue(tkdn, out var l)) return l?.Count ?? 0; return 0; }

        private void BatchApply()
        {
            var cands = DefDatabase<FactionDef>.AllDefs.Where(f => f.humanlikeFaction && f != factionDef && !f.hidden).OrderBy(f => f.LabelCap.ToString()).ToList();
            if (cands.Count == 0) { Messages.Message("No other humanlike factions.", MessageTypeDefOf.RejectInput, false); return; }
            var opts = new List<FloatMenuOption>();
            foreach (var fd in cands) { var cfd = fd; opts.Add(new FloatMenuOption(fd.LabelCap, () => CopyTo(cfd))); }
            opts.Add(new FloatMenuOption("Apply to ALL", () => { SaveTraderType(); foreach (var fd in cands) CopyTo(fd); FactionGearCustomizerMod.Settings.Write(); FactionGearEditor.MarkDirty(); }));
            Find.WindowStack.Add(new FloatMenu(opts));
        }
        private void CopyTo(FactionDef tgt)
        {
            SaveTraderType(); var td = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(tgt.defName);
            if (factionData.CustomTradeStock != null) td.CustomTradeStock = factionData.CustomTradeStock.Select(e => e.DeepCopy()).ToList(); else td.CustomTradeStock = null;
            td.ReplaceVanillaTradeStock = factionData.ReplaceVanillaTradeStock;
            if (factionData.CustomTradeStockByType != null) { td.CustomTradeStockByType = new Dictionary<string, List<TradeStockEntry>>(); foreach (var kv in factionData.CustomTradeStockByType) td.CustomTradeStockByType[kv.Key] = kv.Value.Select(e => e.DeepCopy()).ToList(); } else td.CustomTradeStockByType = null;
            if (factionData.ReplaceVanillaByType != null) td.ReplaceVanillaByType = new Dictionary<string, bool>(factionData.ReplaceVanillaByType); else td.ReplaceVanillaByType = null;
            td.isModified = true; TradeStock_DefModifier.ForceRegenerateForFaction(tgt.defName);
        }

        // ═══════════ DoWindowContents ═══════════
        public override void DoWindowContents(Rect inR)
        {
            float y = 0f;
            Text.Font = GameFont.Medium;
            string fName = DefDisplayNameUtility.GetSafeFactionDisplayName(factionDef, "Dialog_TradeStockEditor");
            Widgets.Label(new Rect(0f, y, inR.width, 26f), LanguageManager.Get("TradeStockEditorTitle").Replace("{0}", fName));
            Text.Font = GameFont.Small; y += 30f;

            float rh = 28f;
            var tto = traderTypes[selectedTraderTypeIdx];
            string dl = string.IsNullOrEmpty(tto.category) ? tto.label : $"{tto.category}: {tto.label}";
            if (Widgets.ButtonText(new Rect(0f, y, inR.width * 0.34f, rh), LanguageManager.Get("TradeStockTrader").Replace("{0}", dl)))
            {
                var opts = new List<FloatMenuOption>(); string lc = null;
                for (int i = 0; i < traderTypes.Count; i++) { var t = traderTypes[i]; if (t.category != lc && !string.IsNullOrEmpty(t.category)) { opts.Add(new FloatMenuOption($"<b>{t.category}</b>", null)); lc = t.category; } var ci = i; int cnt = GetConfigCountForType(t.traderKindDefName); opts.Add(new FloatMenuOption("  " + t.label + (cnt > 0 ? $"  ({cnt})" : ""), () => SwitchTT(ci))); }
                Find.WindowStack.Add(new FloatMenu(opts));
            }
            Rect rvRect = new Rect(inR.width * 0.34f + 8f, y, 165f, rh);
            bool prevReplace = bufferReplaceVanilla;
            Widgets.CheckboxLabeled(rvRect, LanguageManager.Get("TradeStockReplaceVanilla"), ref bufferReplaceVanilla);
            if (bufferReplaceVanilla != prevReplace) { bufferReplaceVanilla = prevReplace; Find.WindowStack.Add(new Dialog_MessageBox(LanguageManager.Get("TradeStockReplaceConfirm"), LanguageManager.Get("Yes"), () => { PushU(); bufferReplaceVanilla = !prevReplace; }, LanguageManager.Get("Cancel"), null)); }
            GUI.enabled = undo.Count > 0;
            if (Widgets.ButtonText(new Rect(inR.width - 230f, y, 30f, rh), "<")) DoU();
            TooltipHandler.TipRegion(new Rect(inR.width - 230f, y, 30f, rh), LanguageManager.Get("TradeStockUndo").Replace("{0}", undo.Count.ToString()));
            GUI.enabled = redo.Count > 0;
            if (Widgets.ButtonText(new Rect(inR.width - 198f, y, 30f, rh), ">")) DoR();
            TooltipHandler.TipRegion(new Rect(inR.width - 198f, y, 30f, rh), LanguageManager.Get("TradeStockRedo").Replace("{0}", redo.Count.ToString()));
            GUI.enabled = true;
            if (Widgets.ButtonText(new Rect(inR.width - 125f, y, 115f, rh), LanguageManager.Get("TradeStockApplyToOthers"))) BatchApply();
            y += 34f;

            float leftW = inR.width * 0.58f;
            float rightX = leftW + 14f; float rightW = inR.width - rightX;
            float rightPanelStartY = y; // right panel starts here, alongside filter bar

            // ── ThingPickerFilterBar (full reuse, including category row) ──
            // Collect ammo sets and food sub-categories for sub-filter buttons
            var ammoSets = allItems.Where(d => d.defName.Contains("Ammo") || d.defName.Contains("Bullet") || d.defName.Contains("Shell") || (d.thingCategories?.Any(c => c.defName == "Ammo") ?? false)).Select(d => { var l = d.LabelCap.ToString(); var p = l.IndexOf('('); return p > 0 ? l.Substring(0, p).Trim() : l; }).Distinct().OrderBy(s => s).Take(50).ToList();
            var foodSubs = new List<string> { "Cooked", "Raw", "Meat", "Vegetable" };
            var fCfg = new ThingPickerFilterBarConfig
            {
                AllMods = allMods,
                AllAmmoSets = ammoSets,
                ShowAmmoFilter = ammoSets.Count > 0,
                ShowFoodFilter = true,
                FoodSubCategories = foodSubs,
                ShowRangeDamage = false,
                ShowCategoryFilter = true,
                ShowSortRow = true,
                SortOptions = new List<string> { "Name", "MarketValue", "TechLevel", "Mass" },
                IdSeed = GetHashCode(),
                OnChanged = () => { filterDirty = true; }
            };
            y = ThingPickerFilterBar.Draw(new Rect(0f, y, leftW, inR.height), y, filterState, fCfg);
            ApplyFilter(); // always refresh after filter bar draws (user may have clicked a filter)

            // Category tabs + right header
            float tabW = leftW / Cats.Length;
            for (int i = 0; i < Cats.Length; i++)
            {
                Rect tr = new Rect(i * tabW, y, tabW - 4f, 22f);
                if (currentCat == Cats[i]) Widgets.DrawHighlightSelected(tr); else Widgets.DrawHighlightIfMouseover(tr);
                if (Widgets.ButtonInvisible(tr)) { currentCat = Cats[i]; libScroll = Vector2.zero; ApplyFilter(); }
                Text.Anchor = TextAnchor.MiddleCenter; GUI.color = currentCat == Cats[i] ? Color.white : Color.gray;
                Widgets.Label(tr, LanguageManager.Get("TradeCat_" + Cats[i].ToString()));
                GUI.color = Color.white; Text.Anchor = TextAnchor.UpperLeft;
            }
            Text.Anchor = TextAnchor.MiddleRight; GUI.color = Color.gray;
            Widgets.Label(new Rect(rightX, y, rightW, 22f), LanguageManager.Get("TradeStockCount").Replace("{0}", bufferTradeStock.Count.ToString()));
            GUI.color = Color.white; Text.Anchor = TextAnchor.UpperLeft;
            y += 26f;

            float contH = inR.height - y - 55f;

            // LEFT panel
            Rect lo = new Rect(0f, y, leftW, contH); Widgets.DrawMenuSection(lo);
            Rect li = lo.ContractedBy(6f);
            float lvh = Mathf.Max(filtered.Count * LibRowH + 4f, li.height);
            Widgets.BeginScrollView(li, ref libScroll, new Rect(0f, 0f, li.width - 16f, lvh), true);
            int fv = Mathf.Max(0, Mathf.FloorToInt(libScroll.y / LibRowH) - 1);
            int vc = Mathf.CeilToInt(li.height / LibRowH) + 3;
            int lv = Mathf.Min(filtered.Count, fv + vc);
            for (int i = fv; i < lv; i++)
            {
                var def = filtered[i]; float ry = i * LibRowH;
                Rect row = new Rect(0f, ry, li.width - 16f, LibRowH);
                if (i % 2 == 0) Widgets.DrawAltRect(row);
                bool added = configuredDefs.Contains(def.defName);
                if (added) GUI.color = new Color(0.45f, 0.65f, 0.45f, 0.25f);
                Widgets.DrawHighlightIfMouseover(row); GUI.color = Color.white;
                Rect iconR = new Rect(row.x + 4f, row.y + (LibRowH - 48f) / 2f, 48f, 48f);
                Widgets.ThingIcon(iconR, def);
                Widgets.InfoCardButton(new Rect(iconR.xMax + 2f, row.y + 6f, 22f, 22f), def);
                float nx = iconR.xMax + 26f;
                if (added) GUI.color = new Color(0.5f, 0.8f, 0.5f);
                Widgets.Label(new Rect(nx, row.y + 2f, row.width * 0.34f, 22f), def.LabelCap);
                GUI.color = Color.white;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(nx, row.y + 26f, 80f, 18f), $"${def.BaseMarketValue:N0}");
                Widgets.Label(new Rect(nx + 80f, row.y + 26f, 70f, 18f), def.techLevel.ToString());
                string mn = def.modContentPack?.Name ?? "Core"; if (mn.Length > 22) mn = mn.Substring(0, 21) + "…";
                Widgets.Label(new Rect(nx + 150f, row.y + 26f, 130f, 18f), mn);
                GUI.color = Color.white;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(nx + 150f, row.y + 1f, 80f, 22f), GetTradeTagLabel(def));
                GUI.color = Color.white;
                if (added) { if (Widgets.ButtonText(new Rect(row.x + row.width - 75f, row.y + 10f, 68f, 28f), "− Remove")) ToggleEntry(def); }
                else { if (Widgets.ButtonText(new Rect(row.x + row.width - 58f, row.y + 10f, 52f, 28f), "+ Add")) ToggleEntry(def); }
            }
            Widgets.EndScrollView();

            // RIGHT panel — starts at filter bar level, fills to bottom
            float rightH = y + contH - rightPanelStartY;
            Rect ro = new Rect(rightX, rightPanelStartY, rightW, rightH); Widgets.DrawMenuSection(ro);
            Rect ri = ro.ContractedBy(6f);
            float rvh = Mathf.Max(bufferTradeStock.Count * CfgRowH + 50f, ri.height);
            Widgets.BeginScrollView(ri, ref cfgScroll, new Rect(0f, 0f, ri.width - 16f, rvh), true);
            float cy = 0f;
            for (int i = 0; i < bufferTradeStock.Count; i++)
            {
                var entry = bufferTradeStock[i];
                if (entry.entryType == TradeEntryType.Animal) { cy += CfgRowH; continue; }
                var def = entry.Thing; if (def == null) continue;
                Rect row = new Rect(0f, cy, ri.width - 16f, CfgRowH - 2f);
                if (i % 2 == 0) Widgets.DrawAltRect(row);
                Rect iR = new Rect(row.x + 4f, row.y + (CfgRowH - 36f) / 2f, 36f, 36f); Widgets.ThingIcon(iR, def);
                Widgets.InfoCardButton(new Rect(iR.xMax + 2f, row.y + 8f, 20f, 20f), def);
                float nX = iR.xMax + 24f;
                Widgets.Label(new Rect(nX, row.y + 4f, row.width - nX - 35f, 20f), def.LabelCap);
                string modSrc = def.modContentPack?.Name ?? "Core"; if (modSrc.Length > 20) modSrc = modSrc.Substring(0, 19) + "…";
                GUI.color = Color.gray; Widgets.Label(new Rect(row.x + row.width - 110f, row.y + 4f, 80f, 18f), modSrc); GUI.color = Color.white;
                if (Widgets.ButtonText(new Rect(nX, row.y + 28f, 90f, 22f), entry.quality.ToString()))
                { var qo = new List<FloatMenuOption>(); foreach (QualityCategory q in Enum.GetValues(typeof(QualityCategory))) { var cq = q; qo.Add(new FloatMenuOption(q.ToString(), () => { PushU(); entry.quality = cq; })); } Find.WindowStack.Add(new FloatMenu(qo)); }
                float cx = nX + 96f;
                GUI.color = Color.gray; Widgets.Label(new Rect(cx, row.y + 30f, 10f, 20f), "×"); GUI.color = Color.white;
                var ms = Widgets.TextField(new Rect(cx + 10f, row.y + 29f, 34f, 20f), entry.countRange.min.ToString()); if (int.TryParse(ms, out int nmi) && nmi >= 1 && nmi <= entry.countRange.max) entry.countRange.min = nmi;
                Widgets.Label(new Rect(cx + 44f, row.y + 28f, 12f, 22f), "~");
                var xs = Widgets.TextField(new Rect(cx + 55f, row.y + 29f, 34f, 20f), entry.countRange.max.ToString()); if (int.TryParse(xs, out int nxi) && nxi >= entry.countRange.min && nxi <= 1000) entry.countRange.max = nxi;
                GUI.color = Color.gray; Widgets.Label(new Rect(cx + 94f, row.y + 28f, 16f, 22f), "w"); GUI.color = Color.white;
                entry.weight = Widgets.HorizontalSlider(new Rect(cx + 108f, row.y + 28f, 50f, 22f), entry.weight, 0f, 1f, true, entry.weight.ToString("F1"), roundTo: 0.1f);
                GUI.color = Color.gray; Widgets.Label(new Rect(nX, row.y + 50f, 100f, 18f), GetTradeTagLabel(def)); GUI.color = Color.white;
                if (Widgets.ButtonImage(new Rect(row.x + row.width - 26f, row.y + 48f, 22f, 22f), TexButton.Delete)) { RemoveAt(i); break; }
                TooltipHandler.TipRegion(new Rect(row.x + row.width - 30f, row.y + 46f, 28f, 28f), LanguageManager.Get("TradeStockRemoveItem"));
                cy += CfgRowH;
            }
            if (bufferTradeStock.Count == 0) { GUI.color = Color.gray; Widgets.Label(new Rect(4f, 4f, ri.width - 16f, 30f), LanguageManager.Get("TradeStockNoItems")); GUI.color = Color.white; }
            Widgets.EndScrollView();
            y += contH + 8f;

            float bh = 36f;
            if (Widgets.ButtonText(new Rect(0f, y, 90f, bh), LanguageManager.Get("Preview")))
            { if (bufferTradeStock.Count > 0) Find.WindowStack.Add(new Dialog_TradeStockPreview(bufferTradeStock, DefDisplayNameUtility.GetSafeFactionDisplayName(factionDef, "Dialog_TradeStockEditor"))); else Messages.Message(LanguageManager.Get("TradeStockAddFirst"), MessageTypeDefOf.RejectInput, false); }
            if (bufferTradeStock.Count > 0 && Widgets.ButtonText(new Rect(95f, y, 90f, bh), LanguageManager.Get("TradeStockClearAll")))
                Find.WindowStack.Add(new Dialog_MessageBox(LanguageManager.Get("TradeStockClearAllConfirm").Replace("{0}", bufferTradeStock.Count.ToString()), LanguageManager.Get("TradeStockClear"), () => { PushU(); bufferTradeStock.Clear(); RebuildIndex(); }, LanguageManager.Get("Cancel"), null));
            if (Widgets.ButtonText(new Rect(190f, y, 115f, bh), LanguageManager.Get("TradeStockResetDefaults")))
                Find.WindowStack.Add(new Dialog_MessageBox(LanguageManager.Get("TradeStockResetConfirm"), LanguageManager.Get("Reset"), () => { PushU(); bufferTradeStock.Clear(); bufferReplaceVanilla = false; var dn = selectedTraderKindDefName; var tkd = dn != null ? DefDatabase<TraderKindDef>.GetNamedSilentFail(dn) : factionDef.baseTraderKinds?.FirstOrDefault(); if (tkd != null) foreach (var e in ExtractVanilla(tkd)) bufferTradeStock.Add(e); RebuildIndex(); }, LanguageManager.Get("Cancel"), null));
            float bw = 100f, ax = inR.width - bw;
            if (Widgets.ButtonText(new Rect(ax, y, bw, bh), LanguageManager.Get("Apply"))) ApplyChanges();
            if (Widgets.ButtonText(new Rect(ax - bw - 10f, y, bw, bh), LanguageManager.Get("Cancel"))) Close();
            if (Event.current.type == EventType.KeyDown) { if (Event.current.control && Event.current.keyCode == KeyCode.Z) { DoU(); Event.current.Use(); } else if (Event.current.control && Event.current.keyCode == KeyCode.Y) { DoR(); Event.current.Use(); } else if (Event.current.control && Event.current.keyCode == KeyCode.S) { ApplyChanges(); Event.current.Use(); } }
        }

        private void ApplyChanges()
        {
            SaveTraderType(); factionData.isModified = true;
            foreach (var tto in traderTypes)
            {
                if (string.IsNullOrEmpty(tto.traderKindDefName)) continue;
                var dn = tto.traderKindDefName;
                var sell = (factionData.CustomTradeStockByType != null && factionData.CustomTradeStockByType.TryGetValue(dn, out var sl)) ? sl : factionData.CustomTradeStock;
                bool rep = (factionData.ReplaceVanillaByType != null && factionData.ReplaceVanillaByType.TryGetValue(dn, out var rv)) ? rv : factionData.ReplaceVanillaTradeStock;
                TradeStock_DefModifier.ApplySellStock(dn, sell, rep);
                var buy = (factionData.CustomBuyStockByType != null && factionData.CustomBuyStockByType.TryGetValue(dn, out var bl)) ? bl : factionData.CustomBuyStock;
                TradeStock_DefModifier.ApplyBuyStock(dn, buy);
            }
            FactionGearCustomizerMod.Settings.Write(); FactionGearEditor.MarkDirty();
            int regen = TradeStock_DefModifier.ForceRegenerateForFaction(factionDef.defName);
            Messages.Message(LanguageManager.Get("TradeStockSaved") + (regen > 0 ? $" ({regen} settlements)" : ""), MessageTypeDefOf.PositiveEvent, false);
            Close();
        }
    }
}
