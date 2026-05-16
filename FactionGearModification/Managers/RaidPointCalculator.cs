using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.Managers
{
    public static class RaidPointCalculator
    {
        /// <summary>
        /// 根据兵种装备的市场价值自动计算袭击点数。
        /// 公式: pointsOverride = baseCombatPower + totalMarketValue * multiplier
        /// </summary>
        public static float CalculatePointsOverride(string kindDefName, FactionGearData factionData,
            float multiplier = 0.01f)
        {
            var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindDefName);
            float basePoints = kindDef?.combatPower ?? 0f;
            float totalValue = CalculateTotalGearValue(kindDefName, factionData);
            return Mathf.RoundToInt(basePoints + totalValue * multiplier);
        }

        /// <summary>
        /// 计算并设置单个兵种的袭击点数覆盖（写入 KindGearData）。
        /// </summary>
        public static void CalculateAndSetForKind(KindGearData kindData, FactionGearData factionData,
            float multiplier = 0.01f)
        {
            if (kindData == null || string.IsNullOrEmpty(kindData.kindDefName)) return;
            kindData.RaidPointsOverride = CalculatePointsOverride(kindData.kindDefName, factionData, multiplier);
        }

        /// <summary>
        /// 为整个派系所有兵种自动计算袭击点数覆盖（写入各自的 KindGearData）。
        /// </summary>
        public static void AutoCalculateForFaction(FactionGearData factionData, float multiplier = 0.01f)
        {
            if (factionData?.kindGearData == null) return;
            foreach (var kindData in factionData.kindGearData)
            {
                if (kindData == null || !kindData.isModified) continue;
                CalculateAndSetForKind(kindData, factionData, multiplier);
            }
        }

        /// <summary>
        /// 计算指定兵种在自定义装备下的全部装备市场价值总和。
        /// </summary>
        public static float CalculateTotalGearValue(string kindDefName, FactionGearData factionData)
        {
            if (factionData == null || string.IsNullOrEmpty(kindDefName)) return 0f;

            var kindData = factionData.GetKindData(kindDefName);
            if (kindData == null) return 0f;

            float totalValue = 0f;

            // Simple 模式：直接装备列表
            totalValue += SumGearItemValues(kindData.weapons);
            totalValue += SumGearItemValues(kindData.meleeWeapons);
            totalValue += SumGearItemValues(kindData.armors);
            totalValue += SumGearItemValues(kindData.apparel);
            totalValue += SumGearItemValues(kindData.others);

            // Advanced 模式：规格化装备
            totalValue += SumSpecRequirementValues(kindData.SpecificWeapons);
            totalValue += SumSpecRequirementValues(kindData.SpecificApparel);
            totalValue += SumSpecRequirementValues(kindData.InventoryItems);

            return totalValue;
        }

        /// <summary>
        /// 为群组编辑器的选项同步来自 KindGearData 的袭击点数。
        /// （保留 PawnGenOptionData.pointsOverride 用于手动覆盖，自动计算以 KindGearData 为准）
        /// </summary>
        public static void AutoCalculateForGroup(PawnGroupMakerData groupData, FactionGearData factionData,
            float multiplier = 0.01f)
        {
            SyncListFromKinds(groupData.options, factionData, multiplier);
            SyncListFromKinds(groupData.traders, factionData, multiplier);
            SyncListFromKinds(groupData.carriers, factionData, multiplier);
            SyncListFromKinds(groupData.guards, factionData, multiplier);
        }

        private static void SyncListFromKinds(List<PawnGenOptionData> list, FactionGearData factionData,
            float multiplier)
        {
            if (list == null) return;
            foreach (var opt in list)
            {
                if (string.IsNullOrEmpty(opt.kindDefName)) continue;
                // 从 KindGearData 读取自动计算的值，写入 PawnGenOptionData 供显示
                var kindData = factionData?.GetKindData(opt.kindDefName);
                if (kindData != null && kindData.RaidPointsOverride.HasValue)
                {
                    opt.pointsOverride = kindData.RaidPointsOverride.Value;
                }
                else
                {
                    opt.pointsOverride = CalculatePointsOverride(opt.kindDefName, factionData, multiplier);
                }
            }
        }

        private static float SumGearItemValues(List<GearItem> items)
        {
            if (items == null) return 0f;
            float total = 0f;
            foreach (var item in items)
            {
                var def = item?.ThingDef;
                if (def != null)
                    total += def.BaseMarketValue;
            }
            return total;
        }

        private static float SumSpecRequirementValues(List<SpecRequirementEdit> specs)
        {
            if (specs == null) return 0f;
            float total = 0f;
            foreach (var spec in specs)
            {
                var def = spec?.Thing;
                if (def != null)
                    total += def.BaseMarketValue;
            }
            return total;
        }
    }
}
