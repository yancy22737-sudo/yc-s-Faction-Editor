using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using FactionGearCustomizer.Core;

namespace FactionGearCustomizer.Managers
{
    /// <summary>
    /// 预设派系导入器 - 负责从预设中选择特定派系导入当前存档
    /// </summary>
    public static class PresetFactionImporter
    {
        public class ImportResult
        {
            public bool Success;
            public List<string> ImportedFactions = new List<string>();
            public List<string> SkippedFactions = new List<string>();
            public List<string> MissingMods = new List<string>();
            public List<string> MissingDLC = new List<string>();
            public string ErrorMessage;
        }

        public enum ImportMode
        {
            Merge,      // 合并到现有设置
            Overwrite   // 覆盖现有设置
        }

        /// <summary>
        /// 验证预设中的派系在当前游戏是否可用        /// </summary>
        public static Dictionary<string, FactionValidationResult> ValidatePresetFactions(FactionGearPreset preset)
        {
            var results = new Dictionary<string, FactionValidationResult>();
            if (preset?.factionGearData == null) return results;

            foreach (var factionData in preset.factionGearData)
            {
                var result = ValidateFactionData(factionData);
                results[factionData.factionDefName] = result;
            }

            return results;
        }

        private static FactionValidationResult ValidateFactionData(FactionGearData factionData)
        {
            var result = new FactionValidationResult
            {
                FactionDefName = factionData.factionDefName,
                IsValid = true
            };

            // 检查派系Def是否存在
            var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(factionData.factionDefName);
            if (factionDef == null)
            {
                result.IsValid = false;
                result.InvalidReason = "FactionDef not found";
                return result;
            }

            result.FactionLabel = factionDef.LabelCap;

            // 检查每个兵种
            foreach (var kindData in factionData.kindGearData)
            {
                var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindData.kindDefName);
                if (kindDef == null)
                {
                    result.InvalidKinds.Add(kindData.kindDefName);
                }
            }

            // 检查所需Mod
            foreach (var kindData in factionData.kindGearData)
            {
                CheckRequiredMods(kindData, result.RequiredMods);
            }

            // 检查DLC需求
            CheckDLCRequirements(factionData, result);

            return result;
        }

        private static void CheckRequiredMods(KindGearData kindData, HashSet<string> requiredMods)
        {
            var allGear = kindData.weapons
                .Concat(kindData.meleeWeapons)
                .Concat(kindData.armors)
                .Concat(kindData.apparel)
                .Concat(kindData.others);

            foreach (var gear in allGear)
            {
                var def = gear.ThingDef;
                if (def?.modContentPack != null && !def.modContentPack.IsCoreMod)
                {
                    requiredMods.Add(def.modContentPack.Name);
                }
            }
        }

        private static void CheckDLCRequirements(FactionGearData factionData, FactionValidationResult result)
        {
            // 检查异种人设置（需要Biotech）
            if (factionData.XenotypeChances?.Any() == true)
            {
                if (!ModsConfig.BiotechActive)
                {
                    result.RequiredDLC.Add("Biotech");
                }
            }

            // 检查特定派系类型的DLC需求
            var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(factionData.factionDefName);
            if (factionDef != null)
            {
                // 检查是否是帝国派系（需要Royalty）
                if (factionDef.defName.Contains("Empire") || factionDef.defName.Contains("Royal"))
                {
                    if (!ModsConfig.RoyaltyActive)
                    {
                        result.RequiredDLC.Add("Royalty");
                    }
                }
            }
        }

        /// <summary>
        /// 导入选中的派系到当前存档
        /// </summary>
        public static ImportResult ImportFactions(
            FactionGearPreset preset,
            List<string> selectedFactionDefNames,
            ImportMode mode)
        {
            var result = new ImportResult();

            if (preset == null)
            {
                result.Success = false;
                result.ErrorMessage = "Preset is null";
                return result;
            }

            if (selectedFactionDefNames?.Any() != true)
            {
                result.Success = false;
                result.ErrorMessage = "No factions selected";
                return result;
            }

            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent == null)
            {
                result.Success = false;
                result.ErrorMessage = "Game component not available";
                return result;
            }

            // 如果是覆盖模式，先清空现有数据
            if (mode == ImportMode.Overwrite)
            {
                gameComponent.savedFactionGearData.Clear();
                FactionGearCustomizerMod.Settings.factionGearData.Clear();
                FactionGearCustomizerMod.Settings.factionGearDataDict?.Clear();
            }

            // 导入选中的派系
            foreach (var factionDefName in selectedFactionDefNames)
            {
                var factionData = preset.factionGearData
                    .FirstOrDefault(f => f.factionDefName == factionDefName);

                if (factionData == null)
                {
                    result.SkippedFactions.Add(factionDefName);
                    continue;
                }

                // 验证派系是否可用
                var validation = ValidateFactionData(factionData);
                if (!validation.IsValid)
                {
                    result.SkippedFactions.Add($"{factionDefName} ({validation.InvalidReason})");
                    continue;
                }

                // 检查缺失的Mod
                var missingMods = validation.RequiredMods
                    .Where(mod => !LoadedModManager.RunningMods.Any(m => m.Name == mod))
                    .ToList();

                if (missingMods.Any())
                {
                    result.MissingMods.AddRange(missingMods);
                }

                // 检查缺失的DLC
                var missingDLC = validation.RequiredDLC
                    .Where(dlc => !IsDLCActive(dlc))
                    .ToList();

                if (missingDLC.Any())
                {
                    result.MissingDLC.AddRange(missingDLC);
                    // DLC缺失时仍然导入，但记录警告。
                }

                // 深拷贝并导入
                var clonedData = factionData.DeepCopy();

                // 移除无效的兵种数据
                clonedData.kindGearData.RemoveAll(k => DefDatabase<PawnKindDef>.GetNamedSilentFail(k.kindDefName) == null);

                // 添加到存档组
                var existingIndex = gameComponent.savedFactionGearData
                    .FindIndex(f => f.factionDefName == factionDefName);

                if (existingIndex >= 0)
                {
                    gameComponent.savedFactionGearData[existingIndex] = clonedData;
                }
                else
                {
                    gameComponent.savedFactionGearData.Add(clonedData);
                }

                // 同步到全局设置
                var globalExistingIndex = FactionGearCustomizerMod.Settings.factionGearData
                    .FindIndex(f => f.factionDefName == factionDefName);

                if (globalExistingIndex >= 0)
                {
                    FactionGearCustomizerMod.Settings.factionGearData[globalExistingIndex] = clonedData;
                }
                else
                {
                    FactionGearCustomizerMod.Settings.factionGearData.Add(clonedData);
                }

                result.ImportedFactions.Add(factionDefName);
            }

            // 更新存档组件状态
            if (result.ImportedFactions.Any())
            {
                gameComponent.useCustomSettings = true;
                gameComponent.activePresetName = null; // 部分导入不绑定特定预设
                FactionGearCustomizerMod.Settings.currentPresetName = null;
                FactionGearCustomizerMod.Settings.factionGearDataDict?.Clear();
                FactionGearCustomizerMod.Settings.Write();

                // 应用设置到游戏
                FactionDefManager.ApplyAllSettings();
            }

            result.Success = result.ImportedFactions.Any();
            return result;
        }

        private static bool IsDLCActive(string dlcName)
        {
            switch (dlcName)
            {
                case "Royalty": return ModsConfig.RoyaltyActive;
                case "Ideology": return ModsConfig.IdeologyActive;
                case "Biotech": return ModsConfig.BiotechActive;
                case "Anomaly": return ModsConfig.AnomalyActive;
                default: return false;
            }
        }

        /// <summary>
        /// 获取导入预览信息（用于UI显示）        /// </summary>
        public static ImportPreview GetImportPreview(
            FactionGearPreset preset,
            List<string> selectedFactionDefNames)
        {
            var preview = new ImportPreview();

            if (preset?.factionGearData == null || selectedFactionDefNames?.Any() != true)
                return preview;

            foreach (var factionDefName in selectedFactionDefNames)
            {
                var factionData = preset.factionGearData
                    .FirstOrDefault(f => f.factionDefName == factionDefName);

                if (factionData == null) continue;

                var validation = ValidateFactionData(factionData);
                var factionPreview = new FactionImportPreview
                {
                    FactionDefName = factionDefName,
                    FactionLabel = validation.FactionLabel ?? factionDefName,
                    IsValid = validation.IsValid,
                    InvalidReason = validation.InvalidReason,
                    KindCount = factionData.kindGearData?.Count ?? 0,
                    MissingMods = validation.RequiredMods
                        .Where(mod => !LoadedModManager.RunningMods.Any(m => m.Name == mod))
                        .ToList(),
                    MissingDLC = validation.RequiredDLC
                        .Where(dlc => !IsDLCActive(dlc))
                        .ToList()
                };

                preview.Factions.Add(factionPreview);
            }

            return preview;
        }
    }

    public class FactionValidationResult
    {
        public string FactionDefName;
        public string FactionLabel;
        public bool IsValid;
        public string InvalidReason;
        public List<string> InvalidKinds = new List<string>();
        public HashSet<string> RequiredMods = new HashSet<string>();
        public List<string> RequiredDLC = new List<string>();
    }

    public class ImportPreview
    {
        public List<FactionImportPreview> Factions = new List<FactionImportPreview>();
    }

    public class FactionImportPreview
    {
        public string FactionDefName;
        public string FactionLabel;
        public bool IsValid;
        public string InvalidReason;
        public int KindCount;
        public List<string> MissingMods = new List<string>();
        public List<string> MissingDLC = new List<string>();
    }
}

