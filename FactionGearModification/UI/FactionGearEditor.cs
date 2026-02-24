using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.UI.Panels;
using FactionGearCustomizer.Compat;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer.UI;

namespace FactionGearCustomizer
{
    public static class FactionGearEditor
    {
        // ================== Caching & State ==================

        // Icon Cache
        private static Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();
        private static int maxCacheSize = 500;

        // Faction Kinds Cache
        private static Dictionary<string, List<PawnKindDef>> factionKindsCache = new Dictionary<string, List<PawnKindDef>>();

        // Dirty State
        public static bool IsDirty = false;
        private static FactionGearCustomizerSettings backupSettings = null;

        // ================== Public Methods ==================

        public static void ClearFactionKindsCache()
        {
            factionKindsCache.Clear();
        }

        public static List<PawnKindDef> GetFactionKinds(FactionDef factionDef)
        {
            if (factionDef == null) return new List<PawnKindDef>();

            // 使用缓存避免重复计算
            string cacheKey = factionDef.defName;
            if (factionKindsCache.TryGetValue(cacheKey, out var cachedList))
            {
                return cachedList;
            }

            var list = new List<PawnKindDef>();
            if (factionDef.pawnGroupMakers != null)
            {
                var seenKinds = new HashSet<string>();
                foreach (var pgm in factionDef.pawnGroupMakers)
                {
                    // 从 options 读取
                    if (pgm.options != null)
                    {
                        foreach (var opt in pgm.options)
                        {
                            if (opt.kind != null && !seenKinds.Contains(opt.kind.defName))
                            {
                                list.Add(opt.kind);
                                seenKinds.Add(opt.kind.defName);
                            }
                        }
                    }
                    
                    // 从 traders 读取
                    if (pgm.traders != null)
                    {
                        foreach (var opt in pgm.traders)
                        {
                            if (opt.kind != null && !seenKinds.Contains(opt.kind.defName))
                            {
                                list.Add(opt.kind);
                                seenKinds.Add(opt.kind.defName);
                            }
                        }
                    }
                    
                    // 从 carriers 读取
                    if (pgm.carriers != null)
                    {
                        foreach (var opt in pgm.carriers)
                        {
                            if (opt.kind != null && !seenKinds.Contains(opt.kind.defName))
                            {
                                list.Add(opt.kind);
                                seenKinds.Add(opt.kind.defName);
                            }
                        }
                    }
                    
                    // 从 guards 读取
                    if (pgm.guards != null)
                    {
                        foreach (var opt in pgm.guards)
                        {
                            if (opt.kind != null && !seenKinds.Contains(opt.kind.defName))
                            {
                                list.Add(opt.kind);
                                seenKinds.Add(opt.kind.defName);
                            }
                        }
                    }
                }
            }
            list.Sort((a, b) => (a.label ?? a.defName).CompareTo(b.label ?? b.defName));
            
            // 存储到缓存
            factionKindsCache[cacheKey] = list;
            
            return list;
        }

        public static void MarkDirty()
        {
            if (!IsDirty)
            {
                backupSettings = FactionGearCustomizerMod.Settings.DeepCopy();
                IsDirty = true;
            }

            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                var kindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                if (kindData != null) kindData.isModified = true;
            }
            
            FactionListPanel.MarkDirty(); // Notify panel to refresh list if needed
        }

        public static void OnFactionSelected()
        {
            EditorSession.KindListScrollPos = Vector2.zero;
            EditorSession.GearListScrollPos = Vector2.zero;
            
            // Lazy load default data for the selected faction if not present
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                // Check if data seems empty (no kinds loaded), if so, load defaults
                if (factionData.kindGearData.NullOrEmpty())
                {
                    FactionGearManager.LoadDefaultPresets(EditorSession.SelectedFactionDefName);
                }
            }
        }

        public static void DiscardChanges()
        {
            if (backupSettings != null)
            {
                FactionGearCustomizerMod.Settings.RestoreFrom(backupSettings);
                backupSettings = null;
            }
            IsDirty = false;
            
            // Clear caches
            EditorSession.CachedFilteredItems = null;
            EditorSession.CachedModSources = null;
            KindListPanel.ClearCache();
            
            EditorSession.NeedCalculateBounds = true;
            CalculateBounds();
            Log.Message("[FactionGearCustomizer] Settings changes discarded.");
        }

        public static void SaveChanges()
        {
            if (!string.IsNullOrEmpty(FactionGearCustomizerMod.Settings.currentPresetName))
            {
                var preset = FactionGearCustomizerMod.Settings.presets.FirstOrDefault(p => p.name == FactionGearCustomizerMod.Settings.currentPresetName);
                if (preset != null)
                {
                    preset.SaveFromCurrentSettings(FactionGearCustomizerMod.Settings.factionGearData);
                    Messages.Message(LanguageManager.Get("PresetUpdated").Replace("{0}", preset.name), MessageTypeDefOf.PositiveEvent, false);
                }
            }

            FactionGearCustomizerMod.Settings.Write();
            IsDirty = false;
            backupSettings = null; 
            Log.Message("[FactionGearCustomizer] Settings saved successfully.");
        }

        public static void DrawEditor(Rect inRect)
        {
            // Initialize
            if (EditorSession.NeedCalculateBounds)
            {
                CalculateBounds();
                EditorSession.NeedCalculateBounds = false;
            }

            Text.Font = GameFont.Small;

            // Check if user has an active preset
            if (!NoPresetPanel.HasActivePreset())
            {
                // Draw the normal UI first (so it's visible behind the overlay)
                DrawEditorContent(inRect);
                // Then draw the "No Preset" overlay on top
                NoPresetPanel.Draw(inRect);
                return;
            }

            // Default Selection Logic
            if (string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                var allFactions = DefDatabase<FactionDef>.AllDefs.Where(f => f.humanlikeFaction && !f.hidden).OrderBy(f => f.LabelCap.ToString()).ToList();
                if (allFactions.Any())
                {
                    EditorSession.SelectedFactionDefName = allFactions.First().defName;
                    var factionDef = allFactions.First();
                    if (factionDef.pawnGroupMakers != null)
                    {
                        foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                        {
                            if (pawnGroupMaker.options != null && pawnGroupMaker.options.Any())
                            {
                                var firstOption = pawnGroupMaker.options.First();
                                if (firstOption.kind != null)
                                {
                                    EditorSession.SelectedKindDefName = firstOption.kind.defName;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            DrawEditorContent(inRect);
        }

        private static void DrawEditorContent(Rect inRect)
        {
            // Layout
            Rect mainRect = inRect;
            float totalWidth = mainRect.width - 20f;

            Rect leftPanel = new Rect(mainRect.x, mainRect.y, totalWidth * 0.22f, mainRect.height);
            Rect middlePanel = new Rect(leftPanel.xMax + 10f, mainRect.y, totalWidth * 0.40f, mainRect.height);
            Rect rightPanel = new Rect(middlePanel.xMax + 10f, mainRect.y, totalWidth * 0.38f, mainRect.height);

            // Left Panel: Split into Faction List (Top) and Kind List (Bottom)
            // Original code used a fixed height for Faction List or ratio?
            // "float factionListHeight = innerRect.height * 0.6f;" in DrawLeftPanel
            // So we split leftPanel manually.
            Widgets.DrawMenuSection(leftPanel);
            Rect leftInner = leftPanel.ContractedBy(5f);
            float factionHeight = leftInner.height * 0.6f;
            Rect factionRect = new Rect(leftInner.x, leftInner.y, leftInner.width, factionHeight);

            // Faction List
            // Since FactionListPanel.Draw expects the container rect and draws a MenuSection,
            // we should pass a rect that INCLUDES the border if we want it to look like before?
            // Actually FactionListPanel.Draw calls Widgets.DrawMenuSection(rect).
            // So we should pass a rect for the Faction List Panel.
            // But wait, the original code had ONE MenuSection for the whole Left Panel, and then drew contents inside.
            // My FactionListPanel.Draw draws a MenuSection.
            // If I want to split it, I should probably change FactionListPanel to NOT draw MenuSection, or draw two MenuSections.
            // Drawing two MenuSections (one for Faction, one for Kind) might look better or different.
            // Let's stick to the component design: Panel draws its own section.

            // Adjust rects for separate panels
            Rect leftTopPanelRect = new Rect(leftPanel.x, leftPanel.y, leftPanel.width, leftPanel.height * 0.6f);
            Rect leftBottomPanelRect = new Rect(leftPanel.x, leftTopPanelRect.yMax + 4f, leftPanel.width, leftPanel.height * 0.4f - 4f);

            FactionListPanel.Draw(leftTopPanelRect);
            KindListPanel.Draw(leftBottomPanelRect);

            GearEditPanel.Draw(middlePanel);
            ItemLibraryPanel.Draw(rightPanel);
        }





        // ================== Shared Logic ==================

        public static List<ThingDef> GetCachedAllWeapons() => FactionGearManager.GetAllWeapons();
        // ... (Other GetCached methods delegated to FactionGearManager directly or cached in Session)
        // Actually EditorSession doesn't have these caches, I should probably keep them in EditorSession if I want to remove static fields from here.
        // Or just use FactionGearManager directly since it handles caching too?
        // FactionGearManager has "cachedAllWeapons" etc.
        // So I can just call FactionGearManager.GetAllWeapons() and it will handle it.
        // The local cache in FactionGearEditor was maybe redundant?
        // Let's check FactionGearManager. It has "EnsureCacheInitialized".
        // So yes, I can just call FactionGearManager methods.

        public static List<ThingDef> GetCachedAllMeleeWeapons() => FactionGearManager.GetAllMeleeWeapons();
        public static List<ThingDef> GetCachedAllArmors() => FactionGearManager.GetAllArmors();
        public static List<ThingDef> GetCachedAllApparel() => FactionGearManager.GetAllApparel();
        public static List<ThingDef> GetCachedAllOthers() => FactionGearManager.GetAllOthers();

        public static void CalculateBounds()
        {
            if (EditorSession.CachedModSources == null && EditorSession.SelectedModSources.Count > 0)
            {
                GetUniqueModSources();
            }

            List<ThingDef> allItems = new List<ThingDef>();
            switch (EditorSession.SelectedCategory)
            {
                case GearCategory.Weapons: allItems = GetCachedAllWeapons(); break;
                case GearCategory.MeleeWeapons: allItems = GetCachedAllMeleeWeapons(); break;
                case GearCategory.Armors: allItems = GetCachedAllArmors(); break;
                case GearCategory.Apparel: allItems = GetCachedAllApparel(); break;
                case GearCategory.Others: allItems = GetCachedAllOthers(); break;
            }

            if (EditorSession.SelectedModSources.Count > 0)
            {
                allItems = allItems.Where(t => EditorSession.SelectedModSources.Contains(FactionGearManager.GetModSource(t))).ToList();
            }

            if (EditorSession.SelectedTechLevel.HasValue)
            {
                allItems = allItems.Where(t => t.techLevel == EditorSession.SelectedTechLevel.Value).ToList();
            }

            if (allItems.Any())
            {
                EditorSession.MinMarketValue = 0f;
                float maxVal = allItems.Max(t => t.BaseMarketValue);
                float newMaxMarketValue = Mathf.Ceil(maxVal / 100f) * 100f;
                if (newMaxMarketValue < 100f) newMaxMarketValue = 100f;

                // Check if current filter is covering the full range (within small tolerance)
                bool wasFullRange = EditorSession.MarketValueFilter.max >= EditorSession.MaxMarketValue - 0.1f;
                
                EditorSession.MaxMarketValue = newMaxMarketValue;

                if (wasFullRange || EditorSession.MarketValueFilter.max > EditorSession.MaxMarketValue) 
                {
                    EditorSession.MarketValueFilter.max = EditorSession.MaxMarketValue;
                }

                if (EditorSession.MarketValueFilter.min > EditorSession.MarketValueFilter.max)
                {
                    EditorSession.MarketValueFilter.min = EditorSession.MinMarketValue;
                    EditorSession.MarketValueFilter.max = EditorSession.MaxMarketValue;
                }
            }
            else
            {
                EditorSession.MinMarketValue = 0f;
                EditorSession.MaxMarketValue = 100000f;
                EditorSession.MarketValueFilter = new FloatRange(0f, 100000f);
            }

            if (EditorSession.SelectedCategory == GearCategory.Weapons || EditorSession.SelectedCategory == GearCategory.MeleeWeapons)
            {
                if (allItems.Any())
                {
                    EditorSession.MinRange = 0f;
                    float maxR = allItems.Max(t => FactionGearManager.GetWeaponRange(t));
                    float newMaxRange = Mathf.Ceil(maxR / 5f) * 5f;
                    if (newMaxRange < 40f) newMaxRange = 40f;

                    bool wasFullRangeR = EditorSession.RangeFilter.max >= EditorSession.MaxRange - 0.1f;
                    EditorSession.MaxRange = newMaxRange;

                    if (wasFullRangeR || EditorSession.RangeFilter.max > EditorSession.MaxRange) 
                        EditorSession.RangeFilter.max = EditorSession.MaxRange;
                    
                    if (EditorSession.RangeFilter.min > EditorSession.RangeFilter.max)
                    {
                        EditorSession.RangeFilter.min = EditorSession.MinRange;
                        EditorSession.RangeFilter.max = EditorSession.MaxRange;
                    }

                    EditorSession.MinDamage = 0f;
                    float maxD = allItems.Max(t => FactionGearManager.GetWeaponDamage(t));
                    float newMaxDamage = Mathf.Ceil(maxD / 5f) * 5f;
                    if (newMaxDamage < 50f) newMaxDamage = 50f;

                    bool wasFullRangeD = EditorSession.DamageFilter.max >= EditorSession.MaxDamage - 0.1f;
                    EditorSession.MaxDamage = newMaxDamage;

                    if (wasFullRangeD || EditorSession.DamageFilter.max > EditorSession.MaxDamage) 
                        EditorSession.DamageFilter.max = EditorSession.MaxDamage;
                    
                    if (EditorSession.DamageFilter.min > EditorSession.DamageFilter.max)
                    {
                        EditorSession.DamageFilter.min = EditorSession.MinDamage;
                        EditorSession.DamageFilter.max = EditorSession.MaxDamage;
                    }
                }
                else
                {
                    EditorSession.MinRange = 0f; EditorSession.MaxRange = 100f;
                    EditorSession.MinDamage = 0f; EditorSession.MaxDamage = 100f;
                }
            }
        }

        public static List<string> GetUniqueModSources()
        {
            var rawSources = new HashSet<string>();
            List<ThingDef> items = null;

            switch (EditorSession.SelectedCategory)
            {
                case GearCategory.Weapons: items = GetCachedAllWeapons(); break;
                case GearCategory.MeleeWeapons: items = GetCachedAllMeleeWeapons(); break;
                case GearCategory.Armors: items = GetCachedAllArmors(); break;
                case GearCategory.Apparel: items = GetCachedAllApparel(); break;
                case GearCategory.Others: items = GetCachedAllOthers(); break;
            }

            if (items != null)
            {
                foreach (var item in items)
                {
                    rawSources.Add(FactionGearManager.GetModSource(item));
                }
            }

            EditorSession.CachedModSources = rawSources.OrderBy(s => s).ToList();
            return EditorSession.CachedModSources;
        }

        public static Texture2D GetIconWithLazyLoading(ThingDef thingDef)
        {
            if (thingDef == null) return null;
            if (iconCache.TryGetValue(thingDef.defName, out Texture2D tex)) return tex;

            if (iconCache.Count >= maxCacheSize)
            {
                iconCache.Clear();
            }

            Texture2D resolvedIcon = null;
            if (thingDef.uiIcon != null) resolvedIcon = thingDef.uiIcon;
            else if (thingDef.graphic != null) resolvedIcon = thingDef.graphic.MatSingle?.mainTexture as Texture2D;
            
            if (resolvedIcon != null)
            {
                iconCache[thingDef.defName] = resolvedIcon;
            }
            return resolvedIcon;
        }

        public static string GetDetailedItemTooltip(ThingDef item, KindGearData kindData)
        {
            if (item == null) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>{item.LabelCap}</b>");
            sb.AppendLine($"<color=gray>{item.description}</color>");
            sb.AppendLine();
            sb.AppendLine($"{LanguageManager.Get("BaseMarketValue")}: {item.BaseMarketValue:F0}");
            sb.AppendLine($"{LanguageManager.Get("Weight")}: {item.BaseMass:F2} kg");
            
            if (CECompat.IsCEActive)
            {
                float bulk = CECompat.GetBulk(item);
                if (bulk > 0)
                {
                    sb.AppendLine($"Bulk: {bulk:F2}");
                }
            }
            
            if (item.IsWeapon)
            {
                float damage = FactionGearManager.GetWeaponDamage(item);
                float range = FactionGearManager.GetWeaponRange(item);
                float dps = FactionGearManager.CalculateWeaponDPS(item);
                sb.AppendLine($"{LanguageManager.Get("Damage")}: {damage}");
                sb.AppendLine($"{LanguageManager.Get("Range")}: {range}");
                sb.AppendLine($"{LanguageManager.Get("DPS")}: {dps:F1}");
            }
            else if (item.IsApparel)
            {
                float sharp = FactionGearManager.GetArmorRatingSharp(item);
                float blunt = FactionGearManager.GetArmorRatingBlunt(item);
                sb.AppendLine($"{LanguageManager.Get("ArmorSharp")}: {sharp:P0}");
                sb.AppendLine($"{LanguageManager.Get("ArmorBlunt")}: {blunt:P0}");
            }
            return sb.ToString();
        }

        public static List<GearItem> GetCurrentCategoryGear(KindGearData kindData, GearCategory category)
        {
            switch (category)
            {
                case GearCategory.Weapons: return kindData.weapons;
                case GearCategory.MeleeWeapons: return kindData.meleeWeapons;
                case GearCategory.Armors: return kindData.armors;
                case GearCategory.Apparel: return kindData.apparel;
                case GearCategory.Others: return kindData.others;
                default: return new List<GearItem>();
            }
        }
        
        public static float GetAverageValue(KindGearData kindData)
        {
            var gear = GetCurrentCategoryGear(kindData, EditorSession.SelectedCategory);
            if (!gear.Any()) return 0f;
            float total = gear.Sum(g => (g.ThingDef?.BaseMarketValue ?? 0) * g.weight);
            return total / gear.Count;
        }
        
        public static float GetAverageWeight(KindGearData kindData)
        {
            var gear = GetCurrentCategoryGear(kindData, EditorSession.SelectedCategory);
            if (!gear.Any()) return 0f;
            return gear.Average(g => g.weight);
        }

        public static void CopyKindDefGear()
        {
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                var kindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                EditorSession.CopiedKindGearData = kindData.DeepCopy();
                Messages.Message(LanguageManager.Get("CopiedKindGearMessage"), MessageTypeDefOf.NeutralEvent, false);
            }
        }

        public static void ApplyToAllKindsInFaction()
        {
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && EditorSession.CopiedKindGearData != null)
            {
                var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(EditorSession.SelectedFactionDefName);
                if (factionDef != null && factionDef.pawnGroupMakers != null)
                {
                    var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                    foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                    {
                        if (pawnGroupMaker.options != null)
                        {
                            foreach (var option in pawnGroupMaker.options)
                            {
                                if (option.kind != null)
                                {
                                    var targetKindData = factionData.GetOrCreateKindData(option.kind.defName);
                                    if (targetKindData != null)
                                    {
                                        targetKindData.CopyFrom(EditorSession.CopiedKindGearData);
                                        targetKindData.isModified = true;
                                    }
                                }
                            }
                        }
                    }
                    MarkDirty();
                }
            }
        }

        public static void InitializeWorkingSettings(bool force = false)
        {
            if (force)
            {
                // Reset state to ensure clean slate if forced
                IsDirty = false;
                backupSettings = null;
            }
        }

        /// <summary>
        /// 刷新所有UI缓存，包括派系列表、兵种列表、图标等
        /// </summary>
        public static void RefreshAllCaches()
        {
            // 清除派系列表缓存
            FactionListPanel.MarkDirty();
            
            // 清除兵种列表缓存
            KindListPanel.ClearCache();
            
            // 清除图标缓存
            iconCache.Clear();
            
            // 清除自定义图标缓存
            CustomIconManager.ClearCache();
            
            // 清除派系描述缓存
            FactionDefManager.ClearDescriptionCache();
            
            // 清除会话缓存
            EditorSession.CachedAllWeapons = null;
            EditorSession.CachedFilteredItems = null;
            EditorSession.CachedModSources = null;
            
            // 重新计算边界
            EditorSession.NeedCalculateBounds = true;
            CalculateBounds();
            
            Log.Message("[FactionGearCustomizer] All caches refreshed.");
        }

        public static void Cleanup()
        {
            // Clear caches to free memory
            iconCache.Clear();
            EditorSession.CachedAllWeapons = null;
            EditorSession.CachedFilteredItems = null;
            EditorSession.CachedModSources = null;
        }


    }
}
