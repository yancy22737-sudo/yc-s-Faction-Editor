using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FactionGearCustomizer.Core;
using FactionGearCustomizer.UI;
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

        /// <summary>
        /// 清理原始数据缓存，在切换存档或游戏重启时调用
        /// </summary>
        public static void ClearOriginalDataCache()
        {
            lock (originalDataLock)
            {
                originalFactionData.Clear();
                originalKindLabels.Clear();
                hasSavedAllOriginalData = false;
                Log.Message("[FactionGearCustomizer] Original data cache cleared.");
            }
        }

        /// <summary>
        /// 保存所有派系和兵种的原始数据（应在游戏启动时调用，在任何修改之前）
        /// </summary>
        public static void SaveAllOriginalData()
        {
            lock (originalDataLock)
            {
                if (hasSavedAllOriginalData)
                    return;

                Log.Message("[FactionGearCustomizer] Saving all original faction data...");

                // 保存所有派系Def的原始数据
                foreach (var factionDef in DefDatabase<FactionDef>.AllDefs)
                {
                    if (factionDef != null && !originalFactionData.ContainsKey(factionDef))
                    {
                        SaveOriginalFactionDataInternal(factionDef);
                    }
                }

                // 保存所有兵种Def的原始数据
                foreach (var kindDef in DefDatabase<PawnKindDef>.AllDefs)
                {
                    if (kindDef != null && !originalKindLabels.ContainsKey(kindDef))
                    {
                        originalKindLabels[kindDef] = kindDef.label;
                    }
                }

                hasSavedAllOriginalData = true;
                Log.Message($"[FactionGearCustomizer] Saved original data for {originalFactionData.Count} factions and {originalKindLabels.Count} kinds.");
            }
        }

        /// <summary>
        /// 内部方法：保存单个派系的原始数据
        /// </summary>
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
                // 如果已经保存过所有原始数据，不需要重复保存
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
                // 如果已经保存过所有原始数据，不需要重复保存
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

            // Apply Label
            if (!string.IsNullOrEmpty(data.Label))
            {
                faction.label = data.Label;
                // Also update existing world factions
                if (Current.Game != null && Find.FactionManager != null)
                {
                    foreach (var f in Find.FactionManager.AllFactions)
                    {
                        if (f.def == faction)
                        {
                            // Force update the faction name to match the new label
                            // This ensures that the user's change is immediately visible in-game
                            // Note: This will rename ALL factions of this Def to the same name if there are multiple.
                            // But typically FactionDefs like "Empire" or "CivilOutlander" have distinct instances.
                            // If there are multiple, they will all be named the same, which might be weird.
                            // But since we edit the DEF, this is expected behavior for "Def Editing".
                            f.Name = data.Label;
                        }
                    }
                }
            }

            // Apply Description
            if (!string.IsNullOrEmpty(data.Description))
            {
                faction.description = data.Description;
            }

            // Apply Icon
            if (!string.IsNullOrEmpty(data.IconPath))
            {
                // Try to load texture first
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
                    // Clear the invalid IconPath to prevent repeated warnings
                    data.IconPath = null;
                }
            }

            // Apply Color
            if (data.Color.HasValue)
            {
                // We set the spectrum to a single color
                faction.colorSpectrum = new List<Color> { data.Color.Value };
                
                // Update existing factions in game
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
            
            // Apply Xenotypes (Biotech)
            if (ModsConfig.BiotechActive && faction.humanlikeFaction && data.XenotypeChances != null && data.XenotypeChances.Count > 0)
            {
                if (faction.xenotypeSet == null) faction.xenotypeSet = new XenotypeSet();
                
                // Re-create the list
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

            // Apply Pawn Group Makers
            if (data.groupMakers != null && data.groupMakers.Count > 0)
            {
                Log.Message($"[FactionGearCustomizer] Applying groupMakers for {faction.defName}, count: {data.groupMakers.Count}");
                
                if (faction.pawnGroupMakers == null) faction.pawnGroupMakers = new List<PawnGroupMaker>();
                else faction.pawnGroupMakers.Clear();

                foreach (var gData in data.groupMakers)
                {
                    var maker = gData.ToPawnGroupMaker();
                    if (maker != null)
                    {
                        faction.pawnGroupMakers.Add(maker);
                        Log.Message($"[FactionGearCustomizer] Added pawnGroupMaker: {gData.kindDefName}");
                    }
                    else
                    {
                        Log.Warning($"[FactionGearCustomizer] Failed to create PawnGroupMaker for kindDefName: {gData?.kindDefName}");
                    }
                }
                
                Log.Message($"[FactionGearCustomizer] Applied {faction.pawnGroupMakers.Count} pawnGroupMakers to faction {faction.defName}");
            }
            else
            {
                Log.Message($"[FactionGearCustomizer] groupMakers is null or empty for faction {faction.defName}, skipping pawnGroupMakers modification");
            }

            // Apply Player Relation Override
            if (data.PlayerRelationOverride.HasValue && Current.Game != null && Find.FactionManager != null)
            {
                Log.Message($"[FactionGearCustomizer] Applying PlayerRelationOverride: {data.PlayerRelationOverride.Value} for faction def: {faction.defName}");
                int count = 0;
                foreach (var f in Find.FactionManager.AllFactions)
                {
                    if (f.def == faction && !f.IsPlayer)
                    {
                        Log.Message($"[FactionGearCustomizer] Found faction instance: {f.Name}");
                        ApplyFactionRelation(f, data.PlayerRelationOverride.Value);
                        count++;
                    }
                }
                Log.Message($"[FactionGearCustomizer] Applied relation to {count} faction instances");
            }
            else
            {
                Log.Message($"[FactionGearCustomizer] Skipping PlayerRelationOverride. HasValue={data.PlayerRelationOverride.HasValue}, InGame={Current.Game != null}");
            }

            // Clear cache if any
            if (cachedDescriptionField != null)
            {
                cachedDescriptionField.SetValue(faction, null);
            }
        }

        // 缓存反射字段以提高性能
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

        // 缓存更多反射字段用于好感度修复
        private static FieldInfo checkNaturalGoodwillField;

        private static void ApplyFactionRelation(Faction faction, FactionRelationKind relationKind)
        {
            if (faction == null || faction.IsPlayer) return;

            // 世界生成阶段玩家派系可能尚未创建，需要检查
            if (Find.FactionManager == null) return;
            Faction playerFaction = Find.FactionManager.OfPlayer;
            if (playerFaction == null) return;

            // 初始化反射字段
            InitializeReflectionFields();

            // Get current goodwill and relation
            float currentGoodwill = faction.PlayerGoodwill;
            FactionRelationKind currentKind = faction.PlayerRelationKind;

            Log.Message($"[FactionGearCustomizer] ApplyFactionRelation: faction={faction.Name}, currentKind={currentKind}, targetKind={relationKind}, currentGoodwill={currentGoodwill}");

            if (currentKind == relationKind)
            {
                Log.Message($"[FactionGearCustomizer] Relation already set, skipping");
                return; // Already set
            }

            // Calculate target goodwill based on desired relation
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

            // Calculate goodwill change needed
            float goodwillChange = targetGoodwill - currentGoodwill;

            Log.Message($"[FactionGearCustomizer] Applying goodwill change: {goodwillChange} (target: {targetGoodwill})");

            // Apply the change
            if (goodwillChange != 0 || currentKind != relationKind)
            {
                try
                {
                    FactionRelation relation = faction.RelationWith(playerFaction);
                    if (relation != null)
                    {
                        // 使用游戏原生方法应用好感度变化，这样会更稳定
                        // 先尝试使用 TryAffectGoodwill 方法
                        if (goodwillChange != 0)
                        {
                            faction.TryAffectGoodwillWith(playerFaction, (int)goodwillChange, true, true, null);
                        }

                        // 然后使用反射确保值被正确设置
                        // Use reflection to set goodwill field (current effective goodwill)
                        if (goodwillField != null)
                        {
                            goodwillField.SetValue(relation, (int)targetGoodwill);
                            Log.Message($"[FactionGearCustomizer] goodwill set via reflection to: {targetGoodwill}");
                        }

                        // Use reflection to set baseGoodwill field (as seen in WorldEdit 2.0)
                        if (baseGoodwillField != null)
                        {
                            baseGoodwillField.SetValue(relation, (int)targetGoodwill);
                            Log.Message($"[FactionGearCustomizer] baseGoodwill set via reflection to: {targetGoodwill}");
                        }

                        // Also set the kind field directly
                        if (kindField != null)
                        {
                            kindField.SetValue(relation, relationKind);
                            Log.Message($"[FactionGearCustomizer] Relation kind set via reflection to: {relationKind}");
                        }

                        // 关键修复：重置 naturalGoodwillTimer 以防止游戏自动恢复原始好感度
                        if (naturalGoodwillTimerField != null)
                        {
                            naturalGoodwillTimerField.SetValue(faction, 0);
                            Log.Message($"[FactionGearCustomizer] naturalGoodwillTimer reset to 0");
                        }

                        // 额外修复：禁用自然好感度检查
                        if (checkNaturalGoodwillField == null)
                        {
                            checkNaturalGoodwillField = typeof(Faction).GetField("checkNaturalGoodwill", BindingFlags.Instance | BindingFlags.NonPublic);
                        }
                        if (checkNaturalGoodwillField != null)
                        {
                            checkNaturalGoodwillField.SetValue(faction, false);
                            Log.Message($"[FactionGearCustomizer] checkNaturalGoodwill set to false");
                        }

                        // Check if relation kind changed
                        FactionRelationKind newKind = faction.PlayerRelationKind;
                        Log.Message($"[FactionGearCustomizer] Final goodwill: {faction.PlayerGoodwill}, relation kind: {newKind}");
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
                // Restore original label if it exists in cache
                if (originalKindLabels.TryGetValue(kind, out string originalLabel))
                {
                    kind.label = originalLabel;
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
            
            // Reset Xenotypes
            if (ModsConfig.BiotechActive && faction.humanlikeFaction)
            {
                if (original.XenotypeChances != null)
                {
                    if (faction.xenotypeSet == null) faction.xenotypeSet = new XenotypeSet();
                    SetXenotypeChances(faction.xenotypeSet, new List<XenotypeChance>(original.XenotypeChances));
                }
                // If original was null, we don't necessarily want to set it to null as it might break things if it was created.
                // But usually if it was null, it means no xenotypes defined.
            }

            // Reset Pawn Groups
            if (original.PawnGroupMakers != null)
            {
                faction.pawnGroupMakers = new List<PawnGroupMaker>(original.PawnGroupMakers);
            }

            if (cachedDescriptionField != null)
            {
                cachedDescriptionField.SetValue(faction, null);
            }

            // Reset existing factions color?
            // This is tricky because we don't know which color they picked from the spectrum originally.
            // But usually they pick a random one. We can leave them as is or re-pick.
            // For now, let's leave them as is, or maybe re-pick if we want to be thorough.
            // But re-picking might change it to a different color than before the edit if spectrum had multiple colors.
            // That's acceptable.
        }

        public static void ResetKind(PawnKindDef kind)
        {
            if (kind == null || !originalKindLabels.ContainsKey(kind)) return;

            kind.label = originalKindLabels[kind];
        }

        public static void ApplyAllSettings()
        {
            // 优先使用存档级别的设置（如果游戏已加载）
            var gameComponent = FactionGearGameComponent.Instance;
            var saveData = gameComponent?.GetActiveFactionGearData();

            // 确定要使用的数据源
            List<FactionGearData> dataSource;
            if (saveData != null)
            {
                // 使用存档中的设置
                dataSource = saveData;
                Log.Message("[FactionGearCustomizer] Applying settings from save game.");
            }
            else
            {
                // 使用全局设置（兼容旧存档或主菜单预览）
                dataSource = FactionGearCustomizerMod.Settings?.factionGearData;
                if (dataSource != null)
                {
                    Log.Message("[FactionGearCustomizer] Applying global settings (no save-specific settings found).");
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
        /// 清理所有派系Def的描述缓存，确保UI显示最新内容
        /// </summary>
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

        /// <summary>
        /// 获取派系原始的兵种列表（不包含用户在群组中新增的兵种）
        /// </summary>
        public static List<PawnKindDef> GetOriginalFactionKinds(FactionDef factionDef)
        {
            if (factionDef == null) return new List<PawnKindDef>();

            // 如果已经保存了原始数据，从原始数据中获取
            if (originalFactionData.TryGetValue(factionDef, out var originalData))
            {
                return ExtractKindsFromPawnGroupMakers(originalData.PawnGroupMakers);
            }

            // 如果没有保存原始数据，保存当前数据并返回
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
            
            list.Sort((a, b) => (a.label ?? a.defName).CompareTo(b.label ?? b.defName));
            return list;
        }
    }
}
