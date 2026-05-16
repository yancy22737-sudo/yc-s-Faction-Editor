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
        private static Dictionary<Faction, OriginalFactionInstanceData> originalFactionInstanceData = new Dictionary<Faction, OriginalFactionInstanceData>();
        private static Dictionary<PawnKindDef, float> originalGenerateCommonalities = new Dictionary<PawnKindDef, float>();
        private static FieldInfo generateCommonalityField = null;
        private static bool generateCommonalityOnThingDef = false;
        private static bool generateCommonalityFieldLookedUp = false;

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

        private class OriginalFactionInstanceData
        {
            public string Name;
            public Color? Color;
            public int? PlayerGoodwill;
            public FactionRelationKind? PlayerRelationKind;
        }

        public static List<XenotypeChance> GetXenotypeChances(XenotypeSet set)
        {
            if (set == null) return null;
            if (xenotypeChancesField == null)
            {
                // Re-attempt reflection if failed initially (e.g. if type wasn't fully loaded)
                xenotypeChancesField = typeof(XenotypeSet).GetField("xenotypeChances", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (xenotypeChancesField == null)
                {
                    Log.ErrorOnce("[FactionGearCustomizer] Failed to reflect xenotypeChances field from XenotypeSet. Biotech features may not work.", 9128374);
                    return null;
                }
            }
            return xenotypeChancesField.GetValue(set) as List<XenotypeChance>;
        }

        public static void SetXenotypeChances(XenotypeSet set, List<XenotypeChance> chances)
        {
            if (set == null) return;
            if (xenotypeChancesField == null)
            {
                // Re-attempt reflection
                xenotypeChancesField = typeof(XenotypeSet).GetField("xenotypeChances", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (xenotypeChancesField == null) return;
            }
            xenotypeChancesField.SetValue(set, chances);
        }

        public static void EnsureXenotypeSetExists(FactionDef faction)
        {
            if (faction == null) return;
            if (faction.xenotypeSet == null)
            {
                faction.xenotypeSet = new XenotypeSet();
                // Ensure the list is initialized
                SetXenotypeChances(faction.xenotypeSet, new List<XenotypeChance>());
            }
        }

        public static List<XenotypeChance> GetOriginalXenotypeChances(FactionDef faction)
        {
            if (faction == null || !originalFactionData.ContainsKey(faction)) return null;
            return originalFactionData[faction].XenotypeChances;
        }

        public static void ClearOriginalDataCache()
        {
            lock (originalDataLock)
            {
                originalFactionData.Clear();
                originalKindLabels.Clear();
                originalFactionInstanceData.Clear();
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
                // 只修改游戏内实例名称（Faction.Name），不修改 def 级别的 label
                if (Current.Game != null && Find.FactionManager != null)
                {
                    foreach (var f in Find.FactionManager.AllFactions)
                    {
                        if (f.def == faction)
                        {
                            SaveOriginalFactionInstanceState(f);
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
                            SaveOriginalFactionInstanceState(f);
                            f.color = data.Color.Value;
                        }
                    }
                }
            }
            
            if (ModsConfig.BiotechActive && faction.humanlikeFaction)
            {
                if (data.DisableXenotypeChances)
                {
                    // 禁用异种生成概率控制：恢复原始异种设置
                    if (originalFactionData.TryGetValue(faction, out var originalData) && originalData.XenotypeChances != null)
                    {
                        if (faction.xenotypeSet == null) faction.xenotypeSet = new XenotypeSet();
                        SetXenotypeChances(faction.xenotypeSet, new List<XenotypeChance>(originalData.XenotypeChances));
                        LogUtils.DebugLog($"Restored original xenotype chances for {faction.defName}");
                    }
                    else if (faction.xenotypeSet != null)
                    {
                        // 原始数据也没有异种设置，则清空
                        SetXenotypeChances(faction.xenotypeSet, new List<XenotypeChance>());
                        LogUtils.DebugLog($"Cleared xenotype chances for {faction.defName} (no original data)");
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
                    LogUtils.DebugLog($"Applied custom xenotype chances for {faction.defName}");
                }
            }

            if (data.groupMakers != null && data.groupMakers.Count > 0)
            {
                LogUtils.DebugLog($"Applying groupMakers for {faction.defName}, count: {data.groupMakers.Count}");

                // Collect original group kinds from the stored pristine backup (not current state)
                var originalByKind = new Dictionary<string, PawnGroupMaker>();
                OriginalFactionData originalStored = null;
                originalFactionData.TryGetValue(faction, out originalStored);
                if (originalStored?.PawnGroupMakers != null)
                {
                    foreach (var m in originalStored.PawnGroupMakers)
                    {
                        if (m.kindDef != null && !originalByKind.ContainsKey(m.kindDef.defName))
                            originalByKind[m.kindDef.defName] = m;
                    }
                }
                // Fallback: if no pristine backup, use current pawnGroupMakers
                if (originalByKind.Count == 0 && faction.pawnGroupMakers != null)
                {
                    foreach (var m in faction.pawnGroupMakers)
                    {
                        if (m.kindDef != null && !originalByKind.ContainsKey(m.kindDef.defName))
                            originalByKind[m.kindDef.defName] = m;
                    }
                }

                // Build candidate list from custom data
                var candidates = new List<PawnGroupMaker>();
                var candidateKinds = new HashSet<string>();
                foreach (var gData in data.groupMakers)
                {
                    var maker = gData.ToPawnGroupMaker();
                    if (maker != null)
                    {
                        candidates.Add(maker);
                        if (maker.kindDef != null)
                            candidateKinds.Add(maker.kindDef.defName);
                        LogUtils.DebugLog($"  Candidate: {gData.kindDefName}");
                    }
                    else
                    {
                        Log.Warning($"[FactionGearCustomizer] Failed to convert PawnGroupMaker '{gData?.kindDefName}' for {faction.defName}");
                    }
                }

                // Check which original kinds are missing from the custom data
                var missingKinds = new List<string>();
                foreach (var kv in originalByKind)
                {
                    if (!candidateKinds.Contains(kv.Key))
                        missingKinds.Add(kv.Key);
                }

                if (missingKinds.Count > 0 || originalByKind.Count == 0)
                {
                    Log.Warning($"[FactionGearCustomizer] Auto-resetting corrupted groupMakers for {faction.defName} — missing: {string.Join(", ", missingKinds)}");

                    // Clear corrupted groupMakers from settings
                    data.groupMakers = null;

                    // Also clear from save-level data
                    var gameComponent = FactionGearGameComponent.Instance;
                    var saveData = gameComponent?.savedFactionGearData;
                    if (saveData != null)
                    {
                        var saveEntry = saveData.FirstOrDefault(f => f.factionDefName == faction.defName);
                        if (saveEntry != null)
                            saveEntry.groupMakers = null;
                    }

                    // Re-evaluate isModified (if groupMakers was the only change, unmark)
                    if (data.IsEffectivelyDefault())
                        data.isModified = false;

                    // Restore original pawnGroupMakers from pristine backup
                    if (originalByKind.Count > 0)
                    {
                        faction.pawnGroupMakers = originalByKind.Values.ToList();
                    }

                    Log.Warning($"[FactionGearCustomizer] Corrupted groupMakers for {faction.defName} have been auto-reset. Missing kinds: {string.Join(", ", missingKinds.Count > 0 ? (IEnumerable<string>)missingKinds : new[] {"(all)"})}");
                }
                else
                {
                    // All original kinds are covered — safe to apply
                    faction.pawnGroupMakers = new List<PawnGroupMaker>();
                    foreach (var m in candidates)
                        faction.pawnGroupMakers.Add(m);

                    // Post-apply: verify Combat group actually has usable pawn options.
                    // If all pawn refs are stale (mod removed/updated), the Combat group
                    // exists but with empty options, breaking raid generation.
                    var combatGroup = faction.pawnGroupMakers.FirstOrDefault(m => m.kindDef?.defName == "Combat");
                    if (combatGroup != null && (combatGroup.options == null || combatGroup.options.Count == 0))
                    {
                        Log.Warning($"[FactionGearCustomizer] Combat group for {faction.defName} has no usable pawns after conversion — nuking custom groupMakers.");
                        data.groupMakers = null;
                        var gc = FactionGearGameComponent.Instance;
                        var sd = gc?.savedFactionGearData;
                        if (sd != null) { var e = sd.FirstOrDefault(f => f.factionDefName == faction.defName); if (e != null) e.groupMakers = null; }
                        if (data.IsEffectivelyDefault()) data.isModified = false;
                        if (originalByKind.Count > 0) faction.pawnGroupMakers = originalByKind.Values.ToList();
                    }
                    else
                    {
                        // Fix: inherited vanilla maxTotalPoints can be too restrictive
                        // (e.g. Rakinia Combat group has maxTotalPoints=1000, but raids
                        // regularly request 1500+ points). Auto-expand to unlimited.
                        foreach (var maker in faction.pawnGroupMakers)
                        {
                            if (maker.maxTotalPoints > 0 && maker.maxTotalPoints < 10000)
                            {
                                Log.Warning($"[FactionGearCustomizer] {faction.defName} {maker.kindDef?.defName} group maxTotalPoints={maker.maxTotalPoints} too restrictive — setting to unlimited.");
                                maker.maxTotalPoints = 9999999f;
                            }
                        }

                        LogUtils.DebugLog($"Applied {faction.pawnGroupMakers.Count} groupMakers to {faction.defName}");

                        foreach (var maker in faction.pawnGroupMakers)
                        {
                            EnsureAllKindsCanGenerate(maker.options);
                            EnsureAllKindsCanGenerate(maker.traders);
                            EnsureAllKindsCanGenerate(maker.carriers);
                            EnsureAllKindsCanGenerate(maker.guards);
                        }
                    }
                }
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
                        SaveOriginalFactionInstanceState(f);
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

            // 意识形态应用
            if (ModsConfig.IdeologyActive && !string.IsNullOrEmpty(data.IdeoName)
                && Current.Game != null && Find.FactionManager != null)
            {
                var ideo = Find.IdeoManager?.IdeosListForReading
                    ?.FirstOrDefault(i => i.name == data.IdeoName);
                if (ideo != null)
                {
                    foreach (var f in Find.FactionManager.AllFactions)
                    {
                        if (f.def == faction && !f.IsPlayer)
                        {
                            if (f.ideos == null)
                                f.ideos = new FactionIdeosTracker(f);
                            f.ideos.SetPrimary(ideo);
                            LogUtils.DebugLog($"Applied ideology '{ideo.name}' to faction instance: {f.Name}");
                        }
                    }
                }
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

        /// <summary>
        /// 确保派系与玩家派系存在关系，避免 "null relation with PlayerColony" 报错
        /// </summary>
        private static void EnsureRelationWithPlayer(Faction faction)
        {
            if (faction == null || faction.IsPlayer) return;
            var playerFaction = Find.FactionManager?.OfPlayer;
            if (playerFaction == null) return;
            if (faction.RelationWith(playerFaction, true) == null)
            {
                faction.TryMakeInitialRelationsWith(playerFaction);
            }
        }

        private static void ApplyFactionRelation(Faction faction, FactionRelationKind relationKind)
        {
            if (faction == null || faction.IsPlayer) return;
            SaveOriginalFactionInstanceState(faction);

            if (Find.FactionManager == null) return;
            Faction playerFaction = Find.FactionManager.OfPlayer;
            if (playerFaction == null) return;

            InitializeReflectionFields();

            EnsureRelationWithPlayer(faction);

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
            // 不复原 def 级别的 label，因为已不再修改它
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

            RestoreGenerateCommonalities();

            if (cachedDescriptionField != null)
            {
                cachedDescriptionField.SetValue(faction, null);
            }

            RestoreFactionRuntimeState(faction);
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

        /// <summary>
        /// Validate and repair corrupted groupMakers data.
        /// If a faction's custom groupMakers are missing kinds that exist in
        /// the pristine backup (or the def's current pawnGroupMakers), nuke the
        /// corrupted custom data immediately.
        /// </summary>
        private static void RepairCorruptedGroupMakersIfNeeded(FactionGearData factionData)
        {
            var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(factionData.factionDefName);
            if (factionDef == null) return;

            // Build reference set of expected group kinds
            var expectedKinds = new HashSet<string>();

            // Prefer pristine backup from game start
            OriginalFactionData originalStored = null;
            originalFactionData.TryGetValue(factionDef, out originalStored);
            if (originalStored?.PawnGroupMakers != null)
            {
                foreach (var m in originalStored.PawnGroupMakers)
                    if (m.kindDef != null)
                        expectedKinds.Add(m.kindDef.defName);
            }

            // Fallback: use current def state
            if (expectedKinds.Count == 0 && factionDef.pawnGroupMakers != null)
            {
                foreach (var m in factionDef.pawnGroupMakers)
                    if (m.kindDef != null)
                        expectedKinds.Add(m.kindDef.defName);
            }

            if (expectedKinds.Count == 0) return; // No reference available, can't validate

            // Build set of kinds in custom data, and check converted Combat group usability
            var customKinds = new HashSet<string>();
            bool combatGroupUnusable = false;
            foreach (var gData in factionData.groupMakers)
            {
                if (!string.IsNullOrEmpty(gData.kindDefName))
                    customKinds.Add(gData.kindDefName);
                var maker = gData.ToPawnGroupMaker();
                if (maker?.kindDef != null)
                {
                    customKinds.Add(maker.kindDef.defName);
                    // Check if Combat group converts to something with no usable pawns
                    if (maker.kindDef.defName == "Combat" && (maker.options == null || maker.options.Count == 0))
                        combatGroupUnusable = true;
                }
            }

            // Combat group exists in name but has no usable pawns after conversion
            if (customKinds.Contains("Combat") && combatGroupUnusable)
            {
                expectedKinds.Add("Combat");
                Log.Warning($"[FactionGearCustomizer] Combat group for {factionData.factionDefName} exists but has no usable pawn options after conversion. Will repair.");
            }

            // Check if all expected kinds are present
            expectedKinds.ExceptWith(customKinds);
            if (expectedKinds.Count > 0)
            {
                Log.Warning($"[FactionGearCustomizer] Repairing corrupted groupMakers for {factionData.factionDefName} — missing kinds: {string.Join(", ", expectedKinds)}");

                // Nuke corrupted groupMakers from this factionData
                factionData.groupMakers = null;

                // Also nuke from all storage locations
                var gc = FactionGearGameComponent.Instance;
                var sd = gc?.savedFactionGearData;
                if (sd != null)
                {
                    var entry = sd.FirstOrDefault(f => f.factionDefName == factionData.factionDefName);
                    if (entry != null) entry.groupMakers = null;
                }
                var gs = FactionGearCustomizerMod.Settings?.factionGearData;
                if (gs != null)
                {
                    var entry = gs.FirstOrDefault(f => f.factionDefName == factionData.factionDefName);
                    if (entry != null) entry.groupMakers = null;
                }

                // Restore factionDef.pawnGroupMakers from pristine backup
                if (originalStored?.PawnGroupMakers != null)
                {
                    factionDef.pawnGroupMakers = new List<PawnGroupMaker>(originalStored.PawnGroupMakers);
                }

                // Re-evaluate isModified
                if (factionData.IsEffectivelyDefault())
                    factionData.isModified = false;

                Log.Warning($"[FactionGearCustomizer] Corrupted groupMakers for {factionData.factionDefName} auto-repaired.");
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
            
            list.Sort((a, b) => DefDisplayNameUtility.ComparePawnKinds(a, b, "FactionDefManager.ExtractKindsFromPawnGroupMakers"));
            return list;
        }

        private static void SaveOriginalFactionInstanceState(Faction faction)
        {
            if (faction == null || faction.IsPlayer) return;
            if (originalFactionInstanceData.ContainsKey(faction)) return;

            var snapshot = new OriginalFactionInstanceData
            {
                Name = faction.Name,
                Color = faction.color
            };

            if (Find.FactionManager?.OfPlayer != null)
            {
                EnsureRelationWithPlayer(faction);
                snapshot.PlayerGoodwill = (int)faction.PlayerGoodwill;
                snapshot.PlayerRelationKind = faction.PlayerRelationKind;
            }

            originalFactionInstanceData[faction] = snapshot;
        }

        private static void RestoreFactionRuntimeState(FactionDef factionDef)
        {
            if (factionDef == null || Current.Game == null || Find.FactionManager == null) return;

            foreach (var faction in Find.FactionManager.AllFactions)
            {
                if (faction == null || faction.IsPlayer || faction.def != factionDef) continue;
                if (!originalFactionInstanceData.TryGetValue(faction, out var snapshot)) continue;

                if (!string.IsNullOrEmpty(snapshot.Name))
                {
                    faction.Name = snapshot.Name;
                }

                if (snapshot.Color.HasValue)
                {
                    faction.color = snapshot.Color.Value;
                }

                if (snapshot.PlayerRelationKind.HasValue && snapshot.PlayerGoodwill.HasValue)
                {
                    RestoreFactionRelation(faction, snapshot.PlayerRelationKind.Value, snapshot.PlayerGoodwill.Value);
                }
            }
        }

        private static void RestoreFactionRelation(Faction faction, FactionRelationKind relationKind, int goodwill)
        {
            if (faction == null || faction.IsPlayer) return;
            if (Find.FactionManager == null) return;

            var playerFaction = Find.FactionManager.OfPlayer;
            if (playerFaction == null) return;

            InitializeReflectionFields();

            EnsureRelationWithPlayer(faction);

            try
            {
                FactionRelation relation = faction.RelationWith(playerFaction, true);
                if (relation == null) return;

                int goodwillDelta = goodwill - (int)faction.PlayerGoodwill;
                if (goodwillDelta != 0)
                {
                    faction.TryAffectGoodwillWith(playerFaction, goodwillDelta, true, true, null);
                }

                if (goodwillField != null)
                {
                    goodwillField.SetValue(relation, goodwill);
                }

                if (baseGoodwillField != null)
                {
                    baseGoodwillField.SetValue(relation, goodwill);
                }

                if (kindField != null)
                {
                    kindField.SetValue(relation, relationKind);
                }

                if (naturalGoodwillTimerField != null)
                {
                    naturalGoodwillTimerField.SetValue(faction, 0);
                }

                if (checkNaturalGoodwillField == null)
                {
                    checkNaturalGoodwillField = typeof(Faction).GetField("checkNaturalGoodwill", BindingFlags.Instance | BindingFlags.NonPublic);
                }

                if (checkNaturalGoodwillField != null)
                {
                    checkNaturalGoodwillField.SetValue(faction, false);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Error restoring relation change: {ex.Message}");
            }
        }

        /// <summary>
        /// 确保自定义群组中的兵种可以被生成。
        /// 通过反射访问 PawnKindDef.generateCommonality（原版中机械族Boss等为0），
        /// 若字段存在且值为0则覆盖为非零值。
        /// </summary>
        private static void EnsureAllKindsCanGenerate(List<PawnGenOption> options)
        {
            if (options == null) return;

            if (generateCommonalityField == null && !generateCommonalityFieldLookedUp)
            {
                generateCommonalityFieldLookedUp = true;
                // Try field names on PawnKindDef
                string[] candidateNames = { "generateCommonality", "baseCommonality", "commonality",
                    "ignoreCommonality", "ignoreGroupCommonality" };
                foreach (var name in candidateNames)
                {
                    generateCommonalityField = typeof(PawnKindDef).GetField(name,
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (generateCommonalityField != null)
                    {
                        LogUtils.DebugLog($"Found PawnKindDef field: {name} ({generateCommonalityField.FieldType.Name}).");
                        break;
                    }
                }
                // Also try on PawnKindDef.race (ThingDef)
                if (generateCommonalityField == null)
                {
                    foreach (var name in candidateNames)
                    {
                        generateCommonalityField = typeof(Verse.ThingDef).GetField(name,
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (generateCommonalityField != null)
                        {
                            generateCommonalityOnThingDef = true;
                            LogUtils.DebugLog($"Found ThingDef field: {name} ({generateCommonalityField.FieldType.Name}). Will override via kind.race.");
                            break;
                        }
                    }
                }
                // Also try ignoreGroupCommonality separately (likely a bool on ThingDef)
                if (generateCommonalityField == null || generateCommonalityField.Name != "ignoreGroupCommonality")
                {
                    var igcField = typeof(Verse.ThingDef).GetField("ignoreGroupCommonality",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (igcField != null)
                    {
                        Log.Message($"[FactionGearCustomizer] Found ThingDef.ignoreGroupCommonality ({igcField.FieldType.Name}). Checking...");
                        // If this is a bool, log its value for boss mechs
                        foreach (var opt in options)
                        {
                            if (opt.kind?.race != null && opt.kind.defName.StartsWith("Mech_"))
                            {
                                var igcVal = igcField.GetValue(opt.kind.race);
                                Log.Message($"[FactionGearCustomizer] {opt.kind.defName}.race.ignoreGroupCommonality = {igcVal}");
                            }
                        }
                    }
                }
                if (generateCommonalityField == null)
                {
                    Log.Warning("[FactionGearCustomizer] No generateCommonality-like field found on PawnKindDef or ThingDef. " +
                        "Boss mech spawning fix will be inactive.");
                }
            }

            if (generateCommonalityField == null) return;

            foreach (var opt in options)
            {
                if (opt.kind == null) continue;
                try
                {
                    object target = generateCommonalityOnThingDef ? (object)opt.kind.race : opt.kind;
                    if (target == null) continue;
                    float value = (float)generateCommonalityField.GetValue(target);
                    if (value <= 0f)
                    {
                        if (!originalGenerateCommonalities.ContainsKey(opt.kind))
                            originalGenerateCommonalities[opt.kind] = value;
                        generateCommonalityField.SetValue(target, 0.1f);
                        LogUtils.DebugLog($"Override generateCommonality for {opt.kind.defName}: {value} → 0.1");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[FactionGearCustomizer] Failed to override generateCommonality for {opt.kind.defName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 恢复所有被覆盖的 generateCommonality 到原始值
        /// </summary>
        private static void RestoreGenerateCommonalities()
        {
            if (generateCommonalityField == null) return;

            foreach (var kv in originalGenerateCommonalities)
            {
                try
                {
                    object target = generateCommonalityOnThingDef ? (object)kv.Key.race : kv.Key;
                    if (target != null)
                        generateCommonalityField.SetValue(target, kv.Value);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[FactionGearCustomizer] Failed to restore generateCommonality for {kv.Key.defName}: {ex.Message}");
                }
            }
            originalGenerateCommonalities.Clear();
        }

    }
}
