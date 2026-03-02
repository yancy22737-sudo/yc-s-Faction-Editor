using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FactionGearCustomizer.Core;
using FactionGearCustomizer.UI;
using FactionGearCustomizer.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.Managers
{
    public static class FactionDefManager
    {
        // Store original values for restoration
        private static Dictionary<FactionDef, OriginalFactionData> originalFactionData = new Dictionary<FactionDef, OriginalFactionData>();
        private static Dictionary<PawnKindDef, string> originalKindLabels = new Dictionary<PawnKindDef, string>();

        // 标记是否已保存所有原始数据（游戏启动时只保存一次）
        private static bool hasSavedAllOriginalData = false;
        private static readonly object originalDataLock = new object();

        private static FieldInfo factionIconField = typeof(FactionDef).GetField("factionIcon", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo cachedDescriptionField = typeof(FactionDef).GetField("cachedDescription", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo xenotypeChancesField = typeof(XenotypeSet).GetField("xenotypeChances", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public class OriginalFactionData
        {
            public string Label;
            public string Description;
            public List<Color> ColorSpectrum;
            public string IconPath;
            public Texture2D Icon; 
            public List<PawnGroupMaker> PawnGroupMakers;
            // Biotech
            public List<XenotypeChance> XenotypeChances;
            // Relation
            public FactionRelationKind? PlayerRelationKind;
        }

        public static List<XenotypeChance> GetXenotypeChances(XenotypeSet set)
        {
            if (set == null) return null;
            return xenotypeChancesField?.GetValue(set) as List<XenotypeChance>;
        }

        public static void SetXenotypeChances(XenotypeSet set, List<XenotypeChance> chances)
        {
            if (set == null) return;
            xenotypeChancesField?.SetValue(set, chances);
        }

        public static void ClearOriginalDataCache()
        {
            lock (originalDataLock)
            {
                originalFactionData.Clear();
                originalKindLabels.Clear();
                hasSavedAllOriginalData = false;
                LogUtils.DebugLog("Original data cache cleared.");
            }
        }

        public static void SaveAllOriginalData()
        {
            lock (originalDataLock)
            {
                if (hasSavedAllOriginalData)
                    return;

                LogUtils.DebugLog("Saving all original faction data...");

                foreach (var factionDef in DefDatabase<FactionDef>.AllDefs)
                {
                    if (factionDef != null && !originalFactionData.ContainsKey(factionDef))
                    {
                        SaveOriginalFactionDataInternal(factionDef);
                    }
                }

                foreach (var kindDef in DefDatabase<PawnKindDef>.AllDefs)
                {
                    if (kindDef != null && !originalKindLabels.ContainsKey(kindDef))
                    {
                        originalKindLabels[kindDef] = kindDef.label;
                    }
                }

                hasSavedAllOriginalData = true;
                LogUtils.DebugLog($"Saved original data for {originalFactionData.Count} factions and {originalKindLabels.Count} kinds.");
            }
        }

        private static void SaveOriginalFactionDataInternal(FactionDef faction)
        {
            if (faction == null) return;

            originalFactionData[faction] = new OriginalFactionData
            {
                Label = faction.label,
                Description = faction.description,
                ColorSpectrum = faction.colorSpectrum != null ? new List<Color>(faction.colorSpectrum) : null,
                IconPath = faction.factionIconPath,
                Icon = faction.FactionIcon,
                PawnGroupMakers = faction.pawnGroupMakers != null ? new List<PawnGroupMaker>(faction.pawnGroupMakers) : null,
                XenotypeChances = (ModsConfig.BiotechActive && faction.humanlikeFaction && faction.xenotypeSet != null) 
                    ? new List<XenotypeChance>(GetXenotypeChances(faction.xenotypeSet) ?? new List<XenotypeChance>()) 
                    : null,
                PlayerRelationKind = null
            };
        }

        public static void EnsureOriginalDataSaved(FactionDef faction)
        {
            if (faction == null) return;

            lock (originalDataLock)
            {
                if (hasSavedAllOriginalData)
                    return;

                if (!originalFactionData.ContainsKey(faction))
                {
                    SaveOriginalFactionDataInternal(faction);
                }
            }
        }

        public static void EnsureOriginalDataSaved(PawnKindDef kind)
        {
            if (kind == null) return;

            lock (originalDataLock)
            {
                if (hasSavedAllOriginalData)
                    return;

                if (!originalKindLabels.ContainsKey(kind))
                {
                    originalKindLabels[kind] = kind.label;
                }
            }
        }

        public static void ApplyFactionChanges(FactionDef faction, FactionGearData data)
        {
            if (faction == null || data == null) return;

            EnsureOriginalDataSaved(faction);

            if (!string.IsNullOrEmpty(data.Label))
            {
                faction.label = data.Label;
                if (Current.Game != null && Find.FactionManager != null)
                {
                    foreach (var f in Find.FactionManager.AllFactions)
                    {
                        if (f.def == faction)
                        {
                            f.Name = data.Label;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(data.Description))
            {
                faction.description = data.Description;
            }

            if (!string.IsNullOrEmpty(data.IconPath))
            {
                Texture2D newIcon = null;
                
                if (data.IconPath.StartsWith("Custom:"))
                {
                    string iconName = data.IconPath.Substring(7);
                    newIcon = CustomIconManager.GetIcon(iconName);
                }
                else
                {
                    newIcon = ContentFinder<Texture2D>.Get(data.IconPath, false);
                }

                if (newIcon != null)
                {
                    faction.factionIconPath = data.IconPath;
                    if (factionIconField != null)
                    {
                        factionIconField.SetValue(faction, newIcon);
                    }
                }
                else
                {
                    Log.Warning($"[FactionGearCustomizer] Could not load icon at path: {data.IconPath} for faction {faction.defName}. The icon file may have been deleted. Skipping icon application.");
                    data.IconPath = null;
                }
            }

            if (data.Color.HasValue)
            {
                faction.colorSpectrum = new List<Color> { data.Color.Value };
                
                if (Current.Game != null && Find.FactionManager != null)
                {
                    foreach (var f in Find.FactionManager.AllFactions)
                    {
                        if (f.def == faction)
                        {
                            f.color = data.Color.Value;
                        }
                    }
                }
            }
            
            if (ModsConfig.BiotechActive && faction.humanlikeFaction)
            {
                if (data.DisableXenotypeChances)
                {
                    // 禁用异种生成概率：清空异种列表
                    if (faction.xenotypeSet != null)
                    {
                        SetXenotypeChances(faction.xenotypeSet, new List<XenotypeChance>());
                    }
                }
                else if (data.XenotypeChances != null && data.XenotypeChances.Count > 0)
                {
                    // 应用自定义异种生成概率
                    if (faction.xenotypeSet == null) faction.xenotypeSet = new XenotypeSet();
                    
                    List<XenotypeChance> newChances = new List<XenotypeChance>();
                    
                    foreach (var kvp in data.XenotypeChances)
                    {
                        XenotypeDef xDef = DefDatabase<XenotypeDef>.GetNamedSilentFail(kvp.Key);
                        if (xDef != null)
                        {
                            newChances.Add(new XenotypeChance(xDef, kvp.Value));
                        }
                    }
                    SetXenotypeChances(faction.xenotypeSet, newChances);
                }
            }

            if (data.groupMakers != null && data.groupMakers.Count > 0)
            {
                LogUtils.DebugLog($"Applying groupMakers for {faction.defName}, count: {data.groupMakers.Count}");
                
                if (faction.pawnGroupMakers == null) faction.pawnGroupMakers = new List<PawnGroupMaker>();
                else faction.pawnGroupMakers.Clear();

                foreach (var gData in data.groupMakers)
                {
                    var maker = gData.ToPawnGroupMaker();
                    if (maker != null)
                    {
                        faction.pawnGroupMakers.Add(maker);
                        LogUtils.DebugLog($"Added pawnGroupMaker: {gData.kindDefName}");
                    }
                    else
                    {
                        Log.Warning($"[FactionGearCustomizer] Failed to create PawnGroupMaker for kindDefName: {gData?.kindDefName}");
                    }
                }
                
                LogUtils.DebugLog($"Applied {faction.pawnGroupMakers.Count} pawnGroupMakers to faction {faction.defName}");
            }
            else
            {
                LogUtils.DebugLog($"groupMakers is null or empty for faction {faction.defName}, skipping pawnGroupMakers modification");
            }

            if (data.PlayerRelationOverride.HasValue && Current.Game != null && Find.FactionManager != null)
            {
                LogUtils.DebugLog($"Applying PlayerRelationOverride: {data.PlayerRelationOverride.Value} for faction def: {faction.defName}");
                int count = 0;
                foreach (var f in Find.FactionManager.AllFactions)
                {
                    if (f.def == faction && !f.IsPlayer)
                    {
                        LogUtils.DebugLog($"Found faction instance: {f.Name}");
                        ApplyFactionRelation(f, data.PlayerRelationOverride.Value);
                        count++;
                    }
                }
                LogUtils.DebugLog($"Applied relation to {count} faction instances");
            }
            else
            {
                LogUtils.DebugLog($"Skipping PlayerRelationOverride. HasValue={data.PlayerRelationOverride.HasValue}, InGame={Current.Game != null}");
            }

            if (cachedDescriptionField != null)
            {
                cachedDescriptionField.SetValue(faction, null);
            }
        }

        private static FieldInfo goodwillField;
        private static FieldInfo baseGoodwillField;
        private static FieldInfo kindField;
        private static FieldInfo naturalGoodwillTimerField;

        private static void InitializeReflectionFields()
        {
            if (goodwillField == null)
            {
                goodwillField = typeof(FactionRelation).GetField("goodwill", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (baseGoodwillField == null)
            {
                baseGoodwillField = typeof(FactionRelation).GetField("baseGoodwill", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (kindField == null)
            {
                kindField = typeof(FactionRelation).GetField("kind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (naturalGoodwillTimerField == null)
            {
                naturalGoodwillTimerField = typeof(Faction).GetField("naturalGoodwillTimer", BindingFlags.Instance | BindingFlags.NonPublic);
            }
        }

        private static FieldInfo checkNaturalGoodwillField;

        private static void ApplyFactionRelation(Faction faction, FactionRelationKind relationKind)
        {
            if (faction == null || faction.IsPlayer) return;

            if (Find.FactionManager == null) return;
            Faction playerFaction = Find.FactionManager.OfPlayer;
            if (playerFaction == null) return;

            InitializeReflectionFields();

            float currentGoodwill = faction.PlayerGoodwill;
            FactionRelationKind currentKind = faction.PlayerRelationKind;

            LogUtils.DebugLog($"ApplyFactionRelation: faction={faction.Name}, currentKind={currentKind}, targetKind={relationKind}, currentGoodwill={currentGoodwill}");

            if (currentKind == relationKind)
            {
                LogUtils.DebugLog("Relation already set, skipping");
                return;
            }

            float targetGoodwill;
            switch (relationKind)
            {
                case FactionRelationKind.Ally:
                    targetGoodwill = 75f;
                    break;
                case FactionRelationKind.Hostile:
                    targetGoodwill = -75f;
                    break;
                case FactionRelationKind.Neutral:
                default:
                    targetGoodwill = 0f;
                    break;
            }

            float goodwillChange = targetGoodwill - currentGoodwill;

            LogUtils.DebugLog($"Applying goodwill change: {goodwillChange} (target: {targetGoodwill})");

            if (goodwillChange != 0 || currentKind != relationKind)
            {
                try
                {
                    FactionRelation relation = faction.RelationWith(playerFaction);
                    if (relation != null)
                    {
                        if (goodwillChange != 0)
                        {
                            faction.TryAffectGoodwillWith(playerFaction, (int)goodwillChange, true, true, null);
                        }

                        if (goodwillField != null)
                        {
                            goodwillField.SetValue(relation, (int)targetGoodwill);
                            LogUtils.DebugLog($"goodwill set via reflection to: {targetGoodwill}");
                        }

                        if (baseGoodwillField != null)
                        {
                            baseGoodwillField.SetValue(relation, (int)targetGoodwill);
                            LogUtils.DebugLog($"baseGoodwill set via reflection to: {targetGoodwill}");
                        }

                        if (kindField != null)
                        {
                            kindField.SetValue(relation, relationKind);
                            LogUtils.DebugLog($"Relation kind set via reflection to: {relationKind}");
                        }

                        if (naturalGoodwillTimerField != null)
                        {
                            naturalGoodwillTimerField.SetValue(faction, 0);
                            LogUtils.DebugLog("naturalGoodwillTimer reset to 0");
                        }

                        if (checkNaturalGoodwillField == null)
                        {
                            checkNaturalGoodwillField = typeof(Faction).GetField("checkNaturalGoodwill", BindingFlags.Instance | BindingFlags.NonPublic);
                        }
                        if (checkNaturalGoodwillField != null)
                        {
                            checkNaturalGoodwillField.SetValue(faction, false);
                            LogUtils.DebugLog("checkNaturalGoodwill set to false");
                        }

                        FactionRelationKind newKind = faction.PlayerRelationKind;
                        LogUtils.DebugLog($"Final goodwill: {faction.PlayerGoodwill}, relation kind: {newKind}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[FactionGearCustomizer] Error applying relation change: {ex.Message}");
                }
            }
        }

        public static void ApplyKindChanges(PawnKindDef kind, KindGearData data)
        {
            if (kind == null || data == null) return;

            EnsureOriginalDataSaved(kind);

            if (!string.IsNullOrEmpty(data.Label))
            {
                kind.label = data.Label;
            }
            else
            {
                if (originalKindLabels.TryGetValue(kind, out string originalLabel))
                {
                    kind.label = originalLabel;
                }
            }
            
            // Xenotype settings are stored in KindGearData and will be applied during Pawn generation via patches
            // Log the settings for debugging
            if (ModsConfig.BiotechActive)
            {
                string xenoInfo = "";
                if (data.DisableXenotypeChances)
                {
                    xenoInfo += " [Disabled]";
                }
                if (!string.IsNullOrEmpty(data.ForcedXenotype))
                {
                    xenoInfo += $" [Forced: {data.ForcedXenotype}]";
                }
                if (data.XenotypeChances != null && data.XenotypeChances.Count > 0)
                {
                    xenoInfo += $" [Chances: {data.XenotypeChances.Count} types]";
                }
                if (!string.IsNullOrEmpty(xenoInfo))
                {
                    LogUtils.DebugLog($"Kind {kind.defName} xenotype settings: {xenoInfo}");
                }
            }
        }

        public static void ResetFaction(FactionDef faction)
        {
            if (faction == null || !originalFactionData.ContainsKey(faction)) return;

            var original = originalFactionData[faction];
            faction.label = original.Label;
            faction.description = original.Description;
            faction.colorSpectrum = original.ColorSpectrum != null ? new List<Color>(original.ColorSpectrum) : null;
            faction.factionIconPath = original.IconPath;
            if (factionIconField != null)
            {
                factionIconField.SetValue(faction, original.Icon);
            }
            
            if (ModsConfig.BiotechActive && faction.humanlikeFaction)
            {
                if (original.XenotypeChances != null)
                {
                    if (faction.xenotypeSet == null) faction.xenotypeSet = new XenotypeSet();
                    SetXenotypeChances(faction.xenotypeSet, new List<XenotypeChance>(original.XenotypeChances));
                }
                else if (faction.xenotypeSet != null)
                {
                    // 恢复原始异种设置（如果原来没有异种设置，则清空）
                    SetXenotypeChances(faction.xenotypeSet, new List<XenotypeChance>());
                }
            }

            if (original.PawnGroupMakers != null)
            {
                faction.pawnGroupMakers = new List<PawnGroupMaker>(original.PawnGroupMakers);
            }

            if (cachedDescriptionField != null)
            {
                cachedDescriptionField.SetValue(faction, null);
            }
        }

        public static void ResetKind(PawnKindDef kind)
        {
            if (kind == null || !originalKindLabels.ContainsKey(kind)) return;

            kind.label = originalKindLabels[kind];
        }

        public static void ResetAllFactions()
        {
            lock (originalDataLock)
            {
                LogUtils.DebugLog("Resetting all factions to original data...");

                int resetFactionCount = 0;
                foreach (var factionDef in DefDatabase<FactionDef>.AllDefs)
                {
                    if (factionDef != null && originalFactionData.ContainsKey(factionDef))
                    {
                        // 【调试日志】记录恢复前后的派系名称
                        string beforeLabel = factionDef.label;
                        string originalLabel = originalFactionData[factionDef].Label;
                        
                        ResetFaction(factionDef);
                        resetFactionCount++;
                        
                        string afterLabel = factionDef.label;
                        
                        // 只在确实有变化时记录日志，避免刷屏
                        if (beforeLabel != originalLabel)
                        {
                            LogUtils.DebugLog($"Reset faction '{factionDef.defName}': '{beforeLabel}' → '{afterLabel}' (original: '{originalLabel}')");
                        }
                    }
                }

                int resetKindCount = 0;
                foreach (var kindDef in DefDatabase<PawnKindDef>.AllDefs)
                {
                    if (kindDef != null && originalKindLabels.ContainsKey(kindDef))
                    {
                        ResetKind(kindDef);
                        resetKindCount++;
                    }
                }

                LogUtils.DebugLog($"Reset complete: {resetFactionCount} factions, {resetKindCount} kinds restored.");
            }
        }

        public static void ApplyAllSettings()
        {
            var gameComponent = FactionGearGameComponent.Instance;
            var saveData = gameComponent?.GetActiveFactionGearData();

            List<FactionGearData> dataSource;
            if (saveData != null)
            {
                dataSource = saveData;
                LogUtils.Info("Applying settings from save game.");
            }
            else
            {
                dataSource = FactionGearCustomizerMod.Settings?.factionGearData;
                if (dataSource != null)
                {
                    LogUtils.Info("Applying global settings (no save-specific settings found).");
                }
            }

            if (dataSource == null) return;

            foreach (var factionData in dataSource)
            {
                FactionDef factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(factionData.factionDefName);
                if (factionDef != null)
                {
                    if (factionData.isModified)
                    {
                        ApplyFactionChanges(factionDef, factionData);
                    }

                    foreach (var kindData in factionData.kindGearData)
                    {
                        PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindData.kindDefName);
                        if (kindDef != null && kindData.isModified)
                        {
                            ApplyKindChanges(kindDef, kindData);
                        }
                    }
                }
            }
        }

        public static void ClearDescriptionCache()
        {
            if (cachedDescriptionField == null) return;
            
            try
            {
                foreach (var faction in DefDatabase<FactionDef>.AllDefs)
                {
                    if (faction != null)
                    {
                        cachedDescriptionField.SetValue(faction, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Error clearing description cache: {ex.Message}");
            }
        }

        public static List<PawnKindDef> GetOriginalFactionKinds(FactionDef factionDef)
        {
            if (factionDef == null) return new List<PawnKindDef>();

            if (originalFactionData.TryGetValue(factionDef, out var originalData))
            {
                return ExtractKindsFromPawnGroupMakers(originalData.PawnGroupMakers);
            }

            EnsureOriginalDataSaved(factionDef);
            return ExtractKindsFromPawnGroupMakers(factionDef.pawnGroupMakers);
        }

        private static List<PawnKindDef> ExtractKindsFromPawnGroupMakers(List<PawnGroupMaker> makers)
        {
            var list = new List<PawnKindDef>();
            if (makers == null) return list;

            var seenKinds = new HashSet<string>();
            foreach (var pgm in makers)
            {
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
            
            list.Sort((a, b) => (a.label ?? a.defName).CompareTo(b.label ?? b.defName));
            return list;
        }
    }
}
