using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public class FactionGearPreset : IExposable
    {
        public string name = "New Preset";
        public string description = "";
        // 存储包含修改的派系数据
        public List<FactionGearData> factionGearData = new List<FactionGearData>();
        // 存储该预设所需的模组列表
        public List<string> requiredMods = new List<string>();

        public FactionGearPreset() { }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name", "New Preset");
            Scribe_Values.Look(ref description, "description", "");
            Scribe_Collections.Look(ref factionGearData, "factionGearData", LookMode.Deep);
            Scribe_Collections.Look(ref requiredMods, "requiredMods", LookMode.Value);

            if (factionGearData == null) factionGearData = new List<FactionGearData>();
            if (requiredMods == null) requiredMods = new List<string>();
        }

        // 自动计算并更新所需的模组列表
        public void CalculateRequiredMods()
        {
            HashSet<string> mods = new HashSet<string>();
            foreach (var faction in factionGearData)
            {
                foreach (var kind in faction.kindGearData)
                {
                    // 尝试加载原始数据进行比对
                    PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kind.kindDefName);
                    HashSet<string> originalGearNames = new HashSet<string>();

                    if (kindDef != null)
                    {
                        KindGearData originalData = new KindGearData(kind.kindDefName);
                        // 使用 FactionGearManager 加载默认配置
                        FactionGearManager.LoadKindDefGear(kindDef, originalData);

                        var originalAllGear = originalData.weapons
                            .Concat(originalData.meleeWeapons)
                            .Concat(originalData.armors)
                            .Concat(originalData.apparel)
                            .Concat(originalData.others);

                        foreach (var gear in originalAllGear)
                        {
                            if (!string.IsNullOrEmpty(gear.thingDefName))
                            {
                                originalGearNames.Add(gear.thingDefName);
                            }
                        }
                    }

                    var allGear = kind.weapons.Concat(kind.meleeWeapons).Concat(kind.armors).Concat(kind.apparel).Concat(kind.others);
                    foreach (var gear in allGear)
                    {
                        var def = gear.ThingDef;
                        // 如果 def 为空，或者是核心模组，则跳过
                        if (def == null || def.modContentPack == null || def.modContentPack.IsCoreMod)
                            continue;

                        // 如果该物品存在于原始配置中，则跳过（说明未在修改中涉及）
                        if (originalGearNames.Contains(def.defName))
                            continue;

                        mods.Add(def.modContentPack.Name);
                    }
                }
            }
            requiredMods = mods.ToList();
        }

        // 从当前游戏设置中抓取被修改的数据并深拷贝保存
        public void SaveFromCurrentSettings(List<FactionGearData> currentSettingsData)
        {
            SaveFullFactionData(currentSettingsData, false);
        }

        // 保存完整的派系配置（所有派系和兵种，不仅仅是修改过的）
        public void SaveFullFactionData(List<FactionGearData> currentSettingsData, bool saveAll = true)
        {
            factionGearData.Clear();
            foreach (var faction in currentSettingsData)
            {
                // 检查是否保存所有派系，或者只保存修改过的派系
                var modifiedKinds = faction.kindGearData.Where(k => k.isModified).ToList();
                
                if (saveAll || faction.isModified || modifiedKinds.Any())
                {
                    // 创建深拷贝
                    FactionGearData newFactionData = new FactionGearData(faction.factionDefName);
                    newFactionData.isModified = faction.isModified;
                    newFactionData.Label = faction.Label;
                    newFactionData.Description = faction.Description;
                    newFactionData.IconPath = faction.IconPath;
                    newFactionData.Color = faction.Color;
                    newFactionData.XenotypeChances = faction.XenotypeChances != null ? new Dictionary<string, float>(faction.XenotypeChances) : null;
                    newFactionData.PlayerRelationOverride = faction.PlayerRelationOverride;

                    // 保存 groupMakers
                    if (faction.groupMakers != null)
                    {
                        foreach (var group in faction.groupMakers)
                        {
                            newFactionData.groupMakers.Add(group.DeepCopy());
                        }
                    }

                    // 保存所有兵种或只保存修改过的兵种
                    if (saveAll)
                    {
                        foreach (var kind in faction.kindGearData)
                        {
                            KindGearData newKindData = kind.DeepCopy();
                            newFactionData.kindGearData.Add(newKindData);
                        }
                    }
                    else
                    {
                        foreach (var kind in modifiedKinds)
                        {
                            KindGearData newKindData = kind.DeepCopy();
                            newFactionData.kindGearData.Add(newKindData);
                        }
                    }

                    factionGearData.Add(newFactionData);
                }
            }
            
            // 【修复】保存后解析所有引用
            foreach (var factionData in factionGearData)
            {
                factionData?.ResolveReferences();
            }
            
            CalculateRequiredMods();
        }

        public FactionGearPreset DeepCopy()
        {
            var copy = new FactionGearPreset();
            copy.name = this.name;
            copy.description = this.description;
            copy.requiredMods = new List<string>(this.requiredMods);
            foreach (var faction in this.factionGearData)
            {
                copy.factionGearData.Add(faction.DeepCopy());
            }
            return copy;
        }
    }
}