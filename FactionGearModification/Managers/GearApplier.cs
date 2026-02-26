using System;
using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.Core;
using FactionGearCustomizer.Compat;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public static class GearApplier
    {
        // Added for Preview functionality
        public static FactionGearPreset PreviewPreset = null;

        public static void ApplyCustomGear(Pawn pawn, Faction faction)
        {
            try
            {
                if (pawn == null || faction == null) return;

                FactionGearData factionData = null;
                if (PreviewPreset != null)
                {
                    // 预览模式：使用预览预设
                    factionData = PreviewPreset.factionGearData?.FirstOrDefault(f => f.factionDefName == faction.def?.defName);
                }
                else
                {
                    // 正常模式：优先使用存档设置，否则使用全局设置
                    var gameComponent = FactionGearGameComponent.Instance;
                    var saveData = gameComponent?.GetActiveFactionGearData();

                    if (saveData != null)
                    {
                        // 使用存档中的设置
                        factionData = saveData.FirstOrDefault(f => f.factionDefName == faction.def?.defName);
                    }
                    else
                    {
                        // 使用全局设置（兼容旧存档或主菜单）
                        factionData = FactionGearCustomizerMod.Settings?.GetOrCreateFactionData(faction.def?.defName);
                    }
                }

                if (factionData == null) return;

                var kindDefName = pawn.kindDef?.defName;

                if (!string.IsNullOrEmpty(kindDefName))
                {
                    var kindData = factionData.GetKindData(kindDefName);
                    if (kindData != null)
                    {
                        ApplyWeapons(pawn, kindData);
                        ApplyApparel(pawn, kindData);
                        ApplyInventory(pawn, kindData);
                        ApplyHediffs(pawn, kindData);
                        // Log.Message($"[FactionGearCustomizer] Successfully applied custom gear to pawn {pawn.Name.ToStringFull} ({pawn.kindDef.defName}) of faction {faction.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Error in ApplyCustomGear for pawn {pawn?.Name?.ToStringFull ?? "Unknown"}: {ex}");
            }
        }

        /// <summary>
        /// 检查服装是否适合pawn穿戴（包括身体部位和年龄限制）
        /// </summary>
        private static bool CanWearApparel(Pawn pawn, ThingDef apparelDef)
        {
            if (pawn == null || apparelDef == null || !apparelDef.IsApparel)
                return false;

            // 检查身体部位
            if (!ApparelUtility.HasPartsToWear(pawn, apparelDef))
                return false;

            // 检查年龄限制（儿童/成人服装限制）
            if (!apparelDef.apparel.PawnCanWear(pawn))
                return false;

            return true;
        }

        private static void ApplyInventory(Pawn pawn, KindGearData kindData)
        {
            if (kindData.InventoryItems.NullOrEmpty() && !kindData.ForceOnlySelected) return;

            if (kindData.ForceOnlySelected && pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                pawn.inventory.innerContainer.ClearAndDestroyContents();
            }

            if (kindData.InventoryItems.NullOrEmpty()) return;

            // 预先计算所有要生成的物品及其数量，避免多次调用导致数量叠加
            var itemsToGenerate = CalculateItemsToGenerate(kindData.InventoryItems);
            ApplyInventoryItems(pawn, kindData, itemsToGenerate);
        }

        private static List<(SpecRequirementEdit spec, ThingDef thingDef, int count)> CalculateItemsToGenerate(List<SpecRequirementEdit> inventoryItems)
        {
            var result = new List<(SpecRequirementEdit spec, ThingDef thingDef, int count)>();

            foreach (var item in GetWhatToGive(inventoryItems, null))
            {
                if (item.Thing == null) continue;
                if (IsSpecialItem(item.Thing)) continue;

                int count = item.CountRange.RandomInRange;
                if (count <= 0) count = 1;

                result.Add((item, item.Thing, count));
            }

            return result;
        }

        private static void ApplyInventoryItems(Pawn pawn, KindGearData kindData, List<(SpecRequirementEdit spec, ThingDef thingDef, int count)> itemsToGenerate)
        {
            if (pawn == null || pawn.inventory == null || pawn.inventory.innerContainer == null) return;

            foreach (var (spec, thingDef, count) in itemsToGenerate)
            {
                int currentCount = pawn.inventory.innerContainer.Where(t => t.def == thingDef).Sum(t => t.stackCount);
                if (currentCount >= count) continue;

                int toAdd = count - currentCount;
                if (toAdd <= 0) continue;

                if (thingDef.stackLimit == 1)
                {
                    for (int i = 0; i < toAdd; i++)
                    {
                        var created = GenerateItem(pawn, spec, kindData);
                        if (created != null && created.holdingOwner == null)
                        {
                            pawn.inventory.innerContainer.TryAdd(created, true);
                        }
                    }
                }
                else
                {
                    var created = GenerateItem(pawn, spec, kindData);
                    if (created != null && created.holdingOwner == null)
                    {
                        created.stackCount = toAdd;
                        while (created.stackCount > 0)
                        {
                            int originalCount = created.stackCount;
                            if (pawn.inventory.innerContainer.TryAdd(created, true))
                            {
                                break;
                            }
                            if (created.stackCount == originalCount)
                            {
                                created.Destroy();
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static bool IsSpecialItem(ThingDef def)
        {
            if (def == null) return false;
            if (def.defName.ToLower().Contains("corpse")) return true;
            return false;
        }

        private static void ApplyHediffs(Pawn pawn, KindGearData kindData)
        {
            if (kindData.ForceOnlySelected && pawn.health != null)
            {
                var hediffsToRemove = pawn.health.hediffSet.hediffs
                    .Where(h => h.def != null && !h.def.defName.StartsWith("Mech"))
                    .ToList();
                
                foreach (var hediff in hediffsToRemove)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }

            if (kindData.ForcedHediffs.NullOrEmpty()) return;

            foreach (var forcedHediff in kindData.ForcedHediffs)
            {
                if (!Rand.Chance(forcedHediff.chance)) continue;

                if (forcedHediff.IsPool)
                {
                    ApplyHediffFromPool(pawn, forcedHediff);
                    continue;
                }

                if (forcedHediff.HediffDef == null) continue;

                ApplySingleHediff(pawn, forcedHediff);
            }
        }

        private static void ApplySingleHediff(Pawn pawn, ForcedHediff forcedHediff)
        {
            HediffDef def = forcedHediff.HediffDef;
            
            if (HediffNeedsBodyPart(def))
            {
                ApplyHediffWithPart(pawn, forcedHediff);
            }
            else
            {
                ApplyHediffWithoutPart(pawn, forcedHediff);
            }
        }

        private static bool HediffNeedsBodyPart(HediffDef def)
        {
            if (def == null) return false;
            
            if (def.countsAsAddedPartOrImplant) return true;
            
            if (def.hediffClass == typeof(Hediff_AddedPart)) return true;
            
            if (def.hediffClass == typeof(Hediff_MissingPart)) return true;
            
            if (def.defName.Contains("Missing")) return true;
            
            return false;
        }

        private static void ApplyHediffWithoutPart(Pawn pawn, ForcedHediff forcedHediff)
        {
            HediffDef def = forcedHediff.HediffDef;
            
            if (pawn.health.hediffSet.HasHediff(def)) return;
            
            try
            {
                Hediff hediff = HediffMaker.MakeHediff(def, pawn);
                ApplyHediffSeverity(hediff, forcedHediff);
                pawn.health.AddHediff(hediff);
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to apply hediff {def.defName} without part: {ex.Message}. Trying with a body part...");
                
                try
                {
                    var fallbackPart = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault();
                    if (fallbackPart != null)
                    {
                        Hediff hediff = HediffMaker.MakeHediff(def, pawn, fallbackPart);
                        ApplyHediffSeverity(hediff, forcedHediff);
                        pawn.health.AddHediff(hediff);
                    }
                }
                catch (Exception ex2)
                {
                    Log.Warning($"[FactionGearCustomizer] Also failed with fallback body part: {ex2.Message}");
                }
            }
        }

        private static void ApplyHediffWithPart(Pawn pawn, ForcedHediff forcedHediff)
        {
            HediffDef def = forcedHediff.HediffDef;
            
            int count = forcedHediff.maxParts > 0 ? forcedHediff.maxParts : forcedHediff.maxPartsRange.RandomInRange;
            if (count <= 0) count = 1;

            List<BodyPartRecord> userSpecifiedParts = GetUserSpecifiedBodyParts(pawn, forcedHediff);
            
            if (userSpecifiedParts.NullOrEmpty())
            {
                List<BodyPartRecord> validParts = GetValidBodyPartsForHediff(pawn, def);
                
                if (validParts.NullOrEmpty())
                {
                    Log.Warning($"[FactionGearCustomizer] Cannot find valid body parts for hediff {def.defName} on pawn {pawn.Name.ToStringShort}");
                    return;
                }
                
                for (int i = 0; i < count && validParts.Count > 0; i++)
                {
                    if (pawn.health.hediffSet.GetHediffCount(def) >= count) break;
                    
                    BodyPartRecord part = validParts.RandomElement();
                    validParts.Remove(part);
                    
                    if (pawn.health.hediffSet.HasHediff(def, part)) continue;
                    
                    try
                    {
                        Hediff hediff = HediffMaker.MakeHediff(def, pawn, part);
                        ApplyHediffSeverity(hediff, forcedHediff);
                        pawn.health.AddHediff(hediff);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[FactionGearCustomizer] Failed to apply hediff {def.defName} to part {part.Label}: {ex.Message}");
                    }
                }
            }
            else
            {
                count = Mathf.Min(count, userSpecifiedParts.Count);
                
                for (int i = 0; i < count && userSpecifiedParts.Count > 0; i++)
                {
                    BodyPartRecord part = userSpecifiedParts.RandomElement();
                    userSpecifiedParts.Remove(part);
                    
                    if (pawn.health.hediffSet.HasHediff(def, part)) continue;
                    
                    try
                    {
                        Hediff hediff = HediffMaker.MakeHediff(def, pawn, part);
                        ApplyHediffSeverity(hediff, forcedHediff);
                        pawn.health.AddHediff(hediff);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[FactionGearCustomizer] Failed to apply hediff {def.defName} to part {part.Label}: {ex.Message}");
                    }
                }
            }
        }

        private static List<BodyPartRecord> GetUserSpecifiedBodyParts(Pawn pawn, ForcedHediff forcedHediff)
        {
            List<BodyPartRecord> result = new List<BodyPartRecord>();
            
            if (!forcedHediff.parts.NullOrEmpty())
            {
                foreach (var partDef in forcedHediff.parts)
                {
                    var parts = pawn.RaceProps.body.GetPartsWithDef(partDef)
                        .Where(p => !pawn.health.hediffSet.PartIsMissing(p))
                        .ToList();
                    result.AddRange(parts);
                }
            }
            
            return result;
        }

        private static List<BodyPartRecord> GetValidBodyPartsForHediff(Pawn pawn, HediffDef def)
        {
            List<BodyPartRecord> result = new List<BodyPartRecord>();
            
            if (pawn?.RaceProps?.body == null || def == null) return result;
            
            IEnumerable<BodyPartRecord> allParts = pawn.RaceProps.body.AllParts
                .Where(p => !pawn.health.hediffSet.PartIsMissing(p));
            
            foreach (BodyPartRecord part in allParts)
            {
                try
                {
                    if (def.countsAsAddedPartOrImplant || def.hediffClass == typeof(Hediff_AddedPart))
                    {
                        if (part.def != null && part.def.IsSolid(part, pawn.health.hediffSet.hediffs))
                            continue;
                    }
                    
                    if (!pawn.health.hediffSet.HasHediff(def, part))
                    {
                        result.Add(part);
                    }
                }
                catch
                {
                }
            }
            
            return result;
        }

        private static void ApplyHediffFromPool(Pawn pawn, ForcedHediff poolHediff)
        {
            HediffDef selectedDef = GetRandomHediffFromPool(poolHediff.PoolType);
            if (selectedDef == null) return;

            var tempForcedHediff = new ForcedHediff
            {
                HediffDef = selectedDef,
                chance = 1f,
                maxParts = poolHediff.maxParts,
                maxPartsRange = poolHediff.maxPartsRange,
                severityRange = poolHediff.severityRange,
                parts = poolHediff.parts
            };

            ApplySingleHediff(pawn, tempForcedHediff);
        }

        private static HediffDef GetRandomHediffFromPool(HediffPoolType poolType)
        {
            List<HediffDef> candidates = new List<HediffDef>();

            switch (poolType)
            {
                case HediffPoolType.AnyDebuff:
                    candidates = DefDatabase<HediffDef>.AllDefsListForReading
                        .Where(def => def.isBad && !def.makesSickThought && !def.countsAsAddedPartOrImplant && !def.defName.Contains("Missing"))
                        .ToList();
                    break;

                case HediffPoolType.AnyDrugHigh:
                    candidates = DefDatabase<HediffDef>.AllDefsListForReading
                        .Where(def => def.hediffClass == typeof(Hediff_High) || def.defName.ToLower().Contains("high"))
                        .ToList();
                    break;

                case HediffPoolType.AnyAddiction:
                    candidates = DefDatabase<HediffDef>.AllDefsListForReading
                        .Where(def => !def.isBad && (def.defName.ToLower().Contains("addiction") || def.defName.ToLower().Contains("tolerance")))
                        .ToList();
                    break;

                case HediffPoolType.AnyImplant:
                    candidates = DefDatabase<HediffDef>.AllDefsListForReading
                        .Where(def => def.countsAsAddedPartOrImplant)
                        .ToList();
                    break;

                case HediffPoolType.AnyIllness:
                    candidates = DefDatabase<HediffDef>.AllDefsListForReading
                        .Where(def => def.makesSickThought)
                        .ToList();
                    break;

                case HediffPoolType.AnyBuff:
                    candidates = DefDatabase<HediffDef>.AllDefsListForReading
                        .Where(def => !def.isBad && 
                                     !def.defName.ToLower().Contains("addiction") && 
                                     !def.defName.ToLower().Contains("tolerance") && 
                                     def.hediffClass != typeof(Hediff_High) &&
                                     !def.countsAsAddedPartOrImplant &&
                                     def.hediffClass != typeof(Hediff_AddedPart) &&
                                     def.hediffClass != typeof(Hediff_MissingPart))
                        .ToList();
                    break;
            }

            return candidates.NullOrEmpty() ? null : candidates.RandomElement();
        }

        private static void ApplyHediffSeverity(Hediff hediff, ForcedHediff forcedHediff)
        {
            if (forcedHediff.severityRange == default(FloatRange)) return;
            
            float severity = forcedHediff.severityRange.RandomInRange;
            
            if (hediff.def == null) return;
            
            if (hediff.def.lethalSeverity > 0f)
            {
                severity = Mathf.Min(severity, hediff.def.lethalSeverity * 0.95f);
            }
            
            if (!hediff.def.stages.NullOrEmpty())
            {
                float maxStageSeverity = hediff.def.stages.Max(s => s.minSeverity);
                if (maxStageSeverity > 0f && severity > maxStageSeverity)
                {
                    severity = Mathf.Min(severity, maxStageSeverity);
                }
            }
            
            if (severity < 0f) severity = 0f;
            
            hediff.Severity = severity;
        }

        private static void ApplyWeapons(Pawn pawn, KindGearData kindData)
        {
            // Use kind-specific setting if available, otherwise use global setting
            bool forceIgnore = kindData.ForceIgnoreRestrictions ?? FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;

            // Get effective tech level limit
            TechLevel? techLevelLimit = GetEffectiveTechLevelLimit(kindData, pawn);

            // Clear existing weapons if configured or if we are forcing new ones
            if (pawn.equipment != null)
            {
                // Check if we should clear everything
                bool clearAll = kindData.ForceNaked || kindData.ForceOnlySelected;

                var equipmentToDestroy = pawn.equipment.AllEquipmentListForReading
                    .Where(eq => clearAll || eq.def.destroyOnDrop)
                    .ToList();

                foreach (var equipment in equipmentToDestroy)
                {
                    if (pawn.Spawned && pawn.Position.IsValid)
                    {
                        if (!pawn.equipment.TryDropEquipment(equipment, out _, pawn.Position, true))
                        {
                            equipment.Destroy();
                        }
                    }
                    else
                    {
                        equipment.Destroy();
                    }
                }
            }

            // Advanced Mode: SpecificWeapons
            if (!kindData.SpecificWeapons.NullOrEmpty())
            {
                foreach (var item in GetWhatToGive(kindData.SpecificWeapons, pawn))
                {
                    if (item.Thing == null) continue;

                    // Tech level check
                    if (!forceIgnore && !IsTechLevelAllowed(item.Thing, techLevelLimit))
                    {
                        continue;
                    }

                    var created = GenerateItem(pawn, item, kindData);
                    if (created is ThingWithComps weapon && pawn.equipment != null)
                    {
                        if (weapon.def.equipmentType == EquipmentType.Primary && pawn.equipment.Primary != null)
                        {
                             pawn.equipment.Remove(pawn.equipment.Primary);
                        }
                        pawn.equipment.AddEquipment(weapon);
                        
                        // CE 兼容性：为需要弹药的武器生成弹药
                        if (CECompat.WeaponNeedsAmmo(weapon))
                        {
                            CECompat.GenerateAndAddAmmoForWeapon(pawn, weapon);
                        }
                    }
                    else if (created != null)
                    {
                        created.Destroy();
                    }
                }
            }
            // Simple Mode: Old Lists (Fallback)
            else if (kindData.weapons.Any() || kindData.meleeWeapons.Any())
            {
                // ... Existing simple logic ...
                if (kindData.weapons.Any())
                {
                    // Filter by tech level
                    var validWeapons = forceIgnore
                        ? kindData.weapons
                        : kindData.weapons.Where(w => IsTechLevelAllowed(w.ThingDef, techLevelLimit)).ToList();

                    if (validWeapons.Any())
                    {
                        var weaponItem = GetRandomGearItem(validWeapons);
                        if (weaponItem?.ThingDef != null)
                        {
                            // Handle conflicting apparel
                            if (forceIgnore && pawn.apparel != null)
                            {
                                var conflictingApparel = pawn.apparel.WornApparel
                                    .Where(a => {
                                        var comp = a.GetComp<CompShield>();
                                        return comp != null && weaponItem.ThingDef.IsRangedWeapon;
                                    })
                                    .ToList();
                                foreach (var apparel in conflictingApparel)
                                {
                                    pawn.apparel.Remove(apparel);
                                    apparel.Destroy();
                                }
                            }

                            var weapon = GenerateSimpleItem(pawn, weaponItem.ThingDef, kindData, true);
                            if (weapon is ThingWithComps wc)
                            {
                                if (wc.def.equipmentType == EquipmentType.Primary && pawn.equipment.Primary != null)
                                {
                                    ThingWithComps primary = pawn.equipment.Primary;
                                    if (pawn.Spawned && pawn.equipment.TryDropEquipment(primary, out var dropped, pawn.Position, false))
                                    {
                                        dropped.Destroy();
                                    }
                                    else
                                    {
                                        pawn.equipment.Remove(primary);
                                        primary.Destroy();
                                    }
                                }
                                pawn.equipment.AddEquipment(wc);
                                
                                // CE 兼容性：为需要弹药的武器生成弹药
                                if (CECompat.WeaponNeedsAmmo(wc))
                                {
                                    CECompat.GenerateAndAddAmmoForWeapon(pawn, wc);
                                }
                            }
                        }
                    }
                }

                if (kindData.meleeWeapons.Any())
                {
                    // Filter by tech level
                    var validMelee = forceIgnore
                        ? kindData.meleeWeapons
                        : kindData.meleeWeapons.Where(w => IsTechLevelAllowed(w.ThingDef, techLevelLimit)).ToList();

                    if (validMelee.Any())
                    {
                        var meleeItem = GetRandomGearItem(validMelee);
                        if (meleeItem?.ThingDef != null)
                        {
                             var weapon = GenerateSimpleItem(pawn, meleeItem.ThingDef, kindData, true);
                            if (weapon is ThingWithComps wc)
                            {
                                if (wc.def.equipmentType == EquipmentType.Primary && pawn.equipment.Primary != null)
                                {
                                    ThingWithComps primary = pawn.equipment.Primary;
                                    if (pawn.Spawned && pawn.equipment.TryDropEquipment(primary, out var dropped, pawn.Position, false))
                                    {
                                        dropped.Destroy();
                                    }
                                    else
                                    {
                                        pawn.equipment.Remove(primary);
                                        primary.Destroy();
                                    }
                                }
                                pawn.equipment.AddEquipment(wc);
                                
                                // CE 兼容性：为需要弹药的武器生成弹药（某些近战武器可能需要）
                                if (CECompat.WeaponNeedsAmmo(wc))
                                {
                                    CECompat.GenerateAndAddAmmoForWeapon(pawn, wc);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void ApplyApparel(Pawn pawn, KindGearData kindData)
        {
            if (kindData.ForceNaked)
            {
                pawn.apparel?.DestroyAll();
                return;
            }

            // Check if we have ANY data to apply. If not, don't strip (unless forced).
            bool hasAdvancedData = !kindData.SpecificApparel.NullOrEmpty() || !kindData.ApparelRequired.NullOrEmpty();
            bool hasSimpleData = !kindData.armors.NullOrEmpty() || !kindData.apparel.NullOrEmpty() || !kindData.others.NullOrEmpty();

            if (!hasAdvancedData && !hasSimpleData && !kindData.ForceOnlySelected)
            {
                return;
            }

            // Get effective tech level limit
            TechLevel? techLevelLimit = GetEffectiveTechLevelLimit(kindData, pawn);
            bool forceIgnore = kindData.ForceIgnoreRestrictions ?? FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;

            // Strip logic
            if (kindData.ForceOnlySelected)
            {
                 // Strip everything that isn't required (complex check, simplified here to strip all then re-add)
                 // Actually, TotalControl logic is: "Destroy what is worn that is NOT in the allowed list".
                 // For now, let's stick to: Strip all, then add what we want.
                 pawn.apparel?.DestroyAll();
            }
            else
            {
                // Only destroy "destroyOnDrop" items (vanilla behavior for pawns)
                var apparelToDestroy = pawn.apparel.WornApparel
                    .Where(app => app.def.destroyOnDrop)
                    .ToList();
                foreach (var apparel in apparelToDestroy)
                {
                    pawn.apparel.Remove(apparel);
                    apparel.Destroy();
                }
            }

            // Advanced Mode: SpecificApparel
            if (hasAdvancedData)
            {
                // Budget Logic (Advanced Mode)
                float budget = kindData.ApparelMoney?.RandomInRange ?? pawn.kindDef?.apparelMoney.RandomInRange ?? 0f;
                float currentSpent = 0f;
                if (pawn.apparel != null)
                {
                    currentSpent = pawn.apparel.WornApparel.Sum(a => a.MarketValue);
                }

                // Simple Required List
                if (!kindData.ApparelRequired.NullOrEmpty())
                {
                    foreach (var def in kindData.ApparelRequired)
                    {
                        // Tech level check
                        if (!forceIgnore && !IsTechLevelAllowed(def, techLevelLimit))
                        {
                            continue;
                        }

                        var item = new SpecRequirementEdit() { Thing = def, SelectionMode = ApparelSelectionMode.AlwaysTake };
                        var created = GenerateItem(pawn, item, kindData, forceIgnore ? -1f : (budget - currentSpent));
                        if (created is Apparel app && CanWearApparel(pawn, app.def))
                        {
                            if (!forceIgnore && (currentSpent + app.MarketValue > budget))
                            {
                                // Log.Warning($"[FactionGearCustomizer] Budget exceeded for {pawn}: Budget={budget}, Current={currentSpent}, Item={app.LabelCap} ({app.MarketValue}). Skipping.");
                                created.Destroy();
                                continue;
                            }

                            try
                            {
                                pawn.apparel.Wear(app, true);
                                currentSpent += app.MarketValue;
                            }
                            catch (Exception ex)
                            {
                                Log.Warning($"[FactionGearCustomizer] Failed to wear {app} on {pawn}: {ex.Message}");
                                app.Destroy();
                            }
                        }
                    }
                }

                // Complex Specific List
                if (!kindData.SpecificApparel.NullOrEmpty())
                {
                    foreach (var item in GetWhatToGive(kindData.SpecificApparel, pawn))
                    {
                         if (item.Thing == null) continue;

                         // Tech level check
                         if (!forceIgnore && !IsTechLevelAllowed(item.Thing, techLevelLimit))
                         {
                             continue;
                         }

                         var created = GenerateItem(pawn, item, kindData, forceIgnore ? -1f : (budget - currentSpent));
                         if (created is Apparel app && CanWearApparel(pawn, app.def))
                         {
                             // Check Budget
                             if (!forceIgnore && (currentSpent + app.MarketValue > budget))
                             {
                                 // Log.Warning($"[FactionGearCustomizer] Budget exceeded for {pawn}: Budget={budget}, Current={currentSpent}, Item={app.LabelCap} ({app.MarketValue}). Skipping.");
                                 created.Destroy();
                                 continue;
                             }

                             try
                             {
                                 pawn.apparel.Wear(app, true);
                                 currentSpent += app.MarketValue;
                             }
                             catch (Exception ex)
                             {
                                 Log.Warning($"[FactionGearCustomizer] Failed to wear {app} on {pawn}: {ex.Message}");
                                 app.Destroy();
                             }
                         }
                    }
                }
            }
            // Simple Mode: Old Lists (Fallback)
            if (hasSimpleData)
            {
                // Fix: Always try to equip multiple items from the list.
                // This allows users to specify multiple items (e.g. helmet + armor) in the same list
                // and have them all applied if they don't conflict.
                // Previously this was only enabled for ForceIgnore, but it should be the default behavior
                // as users expect "what they list is what they get" (limited by slots/conflicts).
                bool tryEquipMultiple = true;

                // Budget Logic
                float budget = kindData.ApparelMoney?.RandomInRange ?? pawn.kindDef?.apparelMoney.RandomInRange ?? 0f;
                float currentSpent = 0f;
                if (pawn.apparel != null)
                {
                    currentSpent = pawn.apparel.WornApparel.Sum(a => a.MarketValue);
                }

                // Log budget info for debugging
                // Log.Message($"[FactionGearCustomizer] Budget Check for {pawn}: Budget={budget}, CurrentSpent={currentSpent}, ForceIgnore={forceIgnore}");

                void EquipApparelList(List<GearItem> gearList)
                {
                    if (gearList.NullOrEmpty()) return;

                    // Filter by tech level
                    var validList = forceIgnore
                        ? gearList
                        : gearList.Where(g => IsTechLevelAllowed(g.ThingDef, techLevelLimit)).ToList();

                    if (validList.NullOrEmpty()) return;

                    if (tryEquipMultiple)
                    {
                        var workingList = validList.ToList();
                        // Max attempts to prevent any infinite loops, though list size is natural limit
                        int maxAttempts = workingList.Count * 2;
                        int attempts = 0;

                        while (workingList.Any() && attempts < maxAttempts)
                        {
                            attempts++;
                            var item = GetRandomGearItem(workingList);
                            if (item == null) break;

                            // Remove from working list so we don't pick it again
                            workingList.Remove(item);

                            if (item.ThingDef != null)
                            {
                                var created = GenerateSimpleItem(pawn, item.ThingDef, kindData, false, forceIgnore ? -1f : (budget - currentSpent));
                                if (created is Apparel app)
                                {
                                    // Check Budget (unless ignored)
                                    if (!forceIgnore && (currentSpent + app.MarketValue > budget))
                                    {
                                        // Log.Warning($"[FactionGearCustomizer] Budget exceeded for {pawn}: Budget={budget}, Current={currentSpent}, Item={app.LabelCap} ({app.MarketValue}). Skipping.");
                                        created.Destroy();
                                        continue;
                                    }

                                    if (CanWearApparel(pawn, app.def))
                                    {
                                        try
                                        {
                                            pawn.apparel.Wear(app, true);
                                            currentSpent += app.MarketValue;
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Warning($"[FactionGearCustomizer] Failed to wear {app} on {pawn}: {ex.Message}");
                                            app.Destroy();
                                        }
                                    }
                                    else
                                    {
                                        created.Destroy();
                                    }
                                }
                                else if (created != null)
                                {
                                    created.Destroy();
                                }
                            }
                        }
                    }
                    else
                    {
                        var item = GetRandomGearItem(validList);
                        if (item?.ThingDef != null)
                        {
                            var created = GenerateSimpleItem(pawn, item.ThingDef, kindData, false, forceIgnore ? -1f : (budget - currentSpent));
                            if (created is Apparel app)
                            {
                                // Check Budget (unless ignored)
                                if (!forceIgnore && (currentSpent + app.MarketValue > budget))
                                {
                                    // Log.Warning($"[FactionGearCustomizer] Budget exceeded for {pawn}: Budget={budget}, Current={currentSpent}, Item={app.LabelCap} ({app.MarketValue}). Skipping.");
                                    created.Destroy();
                                    return;
                                }

                                if (CanWearApparel(pawn, app.def))
                                {
                                    try
                                    {
                                        pawn.apparel.Wear(app, true);
                                        currentSpent += app.MarketValue;
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Warning($"[FactionGearCustomizer] Failed to wear {app} on {pawn}: {ex.Message}");
                                        app.Destroy();
                                    }
                                }
                                else
                                {
                                    created.Destroy();
                                }
                            }
                            else if (created != null)
                            {
                                created.Destroy();
                            }
                        }
                    }
                }

                EquipApparelList(kindData.armors);
                EquipApparelList(kindData.apparel);
                EquipApparelList(kindData.others);
            }
        }

        private static float GetEstimatedMarketValue(ThingDef def, ThingDef stuff, QualityCategory quality)
        {
            float baseValue = def.GetStatValueAbstract(StatDefOf.MarketValue, stuff);
            float qualityFactor = GetQualityPriceMultiplier(quality);
            return baseValue * qualityFactor;
        }

        private static float GetQualityPriceMultiplier(QualityCategory quality)
        {
            switch (quality)
            {
                case QualityCategory.Awful: return 0.2f;
                case QualityCategory.Poor: return 0.5f;
                case QualityCategory.Normal: return 1.0f;
                case QualityCategory.Good: return 1.4f;
                case QualityCategory.Excellent: return 1.8f;
                case QualityCategory.Masterwork: return 2.5f;
                case QualityCategory.Legendary: return 4.0f;
                default: return 1.0f;
            }
        }

        // Helper for Simple Mode items
        private static Thing GenerateSimpleItem(Pawn pawn, ThingDef def, KindGearData kindData, bool isWeapon, float maxPrice = -1f)
        {
            if (def == null)
            {
                Log.Error("[FactionGearCustomizer] GenerateSimpleItem called with null def.");
                return null;
            }
            if (kindData == null)
            {
                Log.Error("[FactionGearCustomizer] GenerateSimpleItem called with null kindData.");
                return null;
            }

            ThingDef stuff = def.MadeFromStuff ? GenStuff.RandomStuffFor(def) : null;
            if (def.MadeFromStuff && stuff == null)
            {
                Log.Warning($"[FactionGearCustomizer] Could not find stuff for {def.defName}. Skipping generation.");
                return null;
            }

            // Quality Logic (Pre-calculation)
            QualityCategory q = QualityCategory.Normal;
            if (isWeapon && kindData.ForcedWeaponQuality.HasValue) q = kindData.ForcedWeaponQuality.Value;
            else if (kindData.ItemQuality.HasValue) q = kindData.ItemQuality.Value;

            // Budget Check
            if (maxPrice >= 0f)
            {
                // Only check quality multiplier if the item actually has quality, otherwise it's just base * stuff
                // But GetEstimatedMarketValue applies quality multiplier anyway.
                // If the item doesn't have CompQuality, it shouldn't have quality. 
                // However, checking CompQuality requires instantiation or checking def.HasComp(typeof(CompQuality)).
                
                // If def doesn't have quality comp, we should treat quality multiplier as 1.
                if (!def.HasComp(typeof(CompQuality)))
                {
                    // If no quality, force Normal (1.0x) for calculation
                    if (GetEstimatedMarketValue(def, stuff, QualityCategory.Normal) > maxPrice) return null;
                }
                else
                {
                    if (GetEstimatedMarketValue(def, stuff, q) > maxPrice) return null;
                }
            }

            var thing = ThingMaker.MakeThing(def, stuff);

            // Quality
            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                compQuality.SetQuality(q, ArtGenerationContext.Outsider);
            }
            
            // Color
            if (kindData.ApparelColor.HasValue && thing.TryGetComp<CompColorable>() != null)
            {
                thing.SetColor(kindData.ApparelColor.Value);
            }
            
            // Biocode (Royalty DLC)
            if (ModsConfig.RoyaltyActive && isWeapon && kindData.BiocodeWeaponChance.HasValue)
            {
                var code = thing.TryGetComp<CompBiocodable>();
                if (code != null && code.Biocodable && Rand.Chance(kindData.BiocodeWeaponChance.Value))
                {
                    code.CodeFor(pawn);
                }
            }

            return thing;
        }

        // Helper for Advanced Mode items
        private static Thing GenerateItem(Pawn pawn, SpecRequirementEdit spec, KindGearData kindData, float maxPrice = -1f)
        {
            ThingDef stuff = spec.Material;
            if (stuff == null && spec.Thing.MadeFromStuff)
            {
                 stuff = GenStuff.RandomStuffFor(spec.Thing);
            }

            // Quality Logic (Pre-calculation)
            QualityCategory q = spec.Quality ?? kindData.ItemQuality ?? QualityCategory.Normal;
            if (spec.Thing.IsWeapon && kindData.ForcedWeaponQuality.HasValue) q = kindData.ForcedWeaponQuality.Value;

            // Budget Check
            if (maxPrice >= 0f)
            {
                 if (!spec.Thing.HasComp(typeof(CompQuality)))
                 {
                     if (GetEstimatedMarketValue(spec.Thing, stuff, QualityCategory.Normal) > maxPrice) return null;
                 }
                 else
                 {
                     if (GetEstimatedMarketValue(spec.Thing, stuff, q) > maxPrice) return null;
                 }
            }

            Thing thing = ThingMaker.MakeThing(spec.Thing, stuff);
            if (thing == null) return null;

            if (ModsConfig.IdeologyActive && spec.Style != null)
            {
                thing.SetStyleDef(spec.Style);
            }

            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                compQuality.SetQuality(q, ArtGenerationContext.Outsider);
            }

            Color color = spec.Color != default ? spec.Color : (kindData.ApparelColor ?? default);
            if (color != default)
            {
                thing.SetColor(color, false);
            }

            // Biocode (Royalty DLC)
            if (ModsConfig.RoyaltyActive)
            {
                CompBiocodable code = thing.TryGetComp<CompBiocodable>();
                if (code != null && code.Biocodable)
                {
                    if (code.Biocoded) code.UnCode();
                    
                    bool shouldBiocode = spec.Biocode;
                    if (!shouldBiocode && thing.def.IsWeapon && kindData.BiocodeWeaponChance.HasValue)
                    {
                        shouldBiocode = Rand.Chance(kindData.BiocodeWeaponChance.Value);
                    }

                    if (shouldBiocode)
                    {
                        code.CodeFor(pawn);
                    }
                }
            }

            return thing;
        }

        private static SpecRequirementEdit GeneratePoolItem(SpecRequirementEdit itemPool, Pawn pawn)
        {
            ThingDef thingDef = null;
            switch (itemPool.PoolType)
            {
                case ItemPoolType.AnyFood:
                    thingDef = GetRandomFoodItem();
                    break;
                case ItemPoolType.AnyMeal:
                    thingDef = GetRandomMealItem();
                    break;
                case ItemPoolType.AnyRawFood:
                    thingDef = GetRandomRawFoodItem();
                    break;
                case ItemPoolType.AnyMeat:
                    thingDef = GetRandomMeatItem();
                    break;
                case ItemPoolType.AnyVegetable:
                    thingDef = GetRandomVegetableItem();
                    break;
                case ItemPoolType.AnyMedicine:
                    thingDef = GetRandomMedicineItem();
                    break;
                case ItemPoolType.AnySocialDrug:
                    thingDef = GetRandomSocialDrugItem();
                    break;
                case ItemPoolType.AnyHardDrug:
                    thingDef = GetRandomHardDrugItem();
                    break;
            }

            if (thingDef == null) return null;

            // Create a new SpecRequirementEdit with the generated item
            var result = new SpecRequirementEdit
            {
                Thing = thingDef,
                SelectionMode = itemPool.SelectionMode,
                SelectionChance = itemPool.SelectionChance,
                weight = itemPool.weight,
                CountRange = itemPool.CountRange,
                PoolType = itemPool.PoolType
            };

            return result;
        }

        private static ThingDef GetRandomFoodItem()
        {
            var foodDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsIngestible
                    && def.IsNutritionGivingIngestible
                    && !def.IsDrug
                    && !def.IsMedicine
                    && !def.IsPlant
                    && def.category == ThingCategory.Item)
                .ToList();

            return foodDefs.NullOrEmpty() ? null : foodDefs.RandomElement();
        }

        private static ThingDef GetRandomMealItem()
        {
            var mealDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsIngestible
                    && def.IsNutritionGivingIngestible
                    && !def.IsDrug
                    && !def.IsMedicine
                    && !def.IsPlant
                    && def.category == ThingCategory.Item
                    && def.IsProcessedFood)
                .ToList();

            return mealDefs.NullOrEmpty() ? null : mealDefs.RandomElement();
        }

        private static ThingDef GetRandomRawFoodItem()
        {
            var rawDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsIngestible
                    && def.IsNutritionGivingIngestible
                    && !def.IsDrug
                    && !def.IsMedicine
                    && !def.IsPlant
                    && def.category == ThingCategory.Item
                    && !def.IsProcessedFood
                    && !def.IsMeat
                    && !def.IsAnimalProduct)
                .ToList();

            return rawDefs.NullOrEmpty() ? null : rawDefs.RandomElement();
        }

        private static ThingDef GetRandomMeatItem()
        {
            var meatDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsIngestible
                    && def.IsNutritionGivingIngestible
                    && !def.IsDrug
                    && !def.IsMedicine
                    && !def.IsPlant
                    && def.category == ThingCategory.Item
                    && def.IsMeat)
                .ToList();

            return meatDefs.NullOrEmpty() ? null : meatDefs.RandomElement();
        }

        private static ThingDef GetRandomVegetableItem()
        {
            var vegDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsIngestible
                    && def.IsNutritionGivingIngestible
                    && !def.IsDrug
                    && !def.IsMedicine
                    && !def.IsPlant
                    && def.category == ThingCategory.Item
                    && (def.IsFungus || def.defName.ToLower().Contains("vegetable") || def.defName.ToLower().Contains("mushroom")))
                .ToList();

            return vegDefs.NullOrEmpty() ? null : vegDefs.RandomElement();
        }

        private static ThingDef GetRandomMedicineItem()
        {
            var medicineDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsMedicine)
                .ToList();

            return medicineDefs.NullOrEmpty() ? null : medicineDefs.RandomElement();
        }

        private static ThingDef GetRandomSocialDrugItem()
        {
            var socialDrugDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsDrug)
                .Select(def => new { Def = def, DrugProps = def.GetCompProperties<CompProperties_Drug>() })
                .Where(x => x.DrugProps != null && x.DrugProps.addictiveness < 0.1f)
                .Select(x => x.Def)
                .ToList();

            return socialDrugDefs.NullOrEmpty() ? null : socialDrugDefs.RandomElement();
        }

        private static ThingDef GetRandomHardDrugItem()
        {
            var hardDrugDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsDrug)
                .Select(def => new { Def = def, DrugProps = def.GetCompProperties<CompProperties_Drug>() })
                .Where(x => x.DrugProps != null && x.DrugProps.addictiveness >= 0.1f)
                .Select(x => x.Def)
                .ToList();

            return hardDrugDefs.NullOrEmpty() ? null : hardDrugDefs.RandomElement();
        }

        private static IEnumerable<SpecRequirementEdit> GetWhatToGive(List<SpecRequirementEdit> allSpecs, Pawn pawn)
        {
            var always = allSpecs.Where(x => x.SelectionMode == ApparelSelectionMode.AlwaysTake).ToList();
            var chance = allSpecs.Where(x => x.SelectionMode == ApparelSelectionMode.RandomChance).ToList();
            var pool1 = allSpecs.Where(x => x.SelectionMode == ApparelSelectionMode.FromPool1).ToList();
            var pool2 = allSpecs.Where(x => x.SelectionMode == ApparelSelectionMode.FromPool2).ToList();
            var pool3 = allSpecs.Where(x => x.SelectionMode == ApparelSelectionMode.FromPool3).ToList();
            var pool4 = allSpecs.Where(x => x.SelectionMode == ApparelSelectionMode.FromPool4).ToList();
            var itemPools = allSpecs.Where(x => x.PoolType != ItemPoolType.None).ToList();

            foreach (var item in always) yield return item;

            foreach (var item in chance)
                if (Rand.Chance(item.SelectionChance))
                    yield return item;

            var selected = PickFromPool(pool1, pawn);
            if (selected != null) yield return selected;

            selected = PickFromPool(pool2, pawn);
            if (selected != null) yield return selected;

            selected = PickFromPool(pool3, pawn);
            if (selected != null) yield return selected;

            selected = PickFromPool(pool4, pawn);
            if (selected != null) yield return selected;

            foreach (var itemPool in itemPools)
            {
                // 物品池的处理：如果是 AlwaysTake 模式，总是生成；否则根据 SelectionChance 概率生成
                bool shouldGenerate = itemPool.SelectionMode == ApparelSelectionMode.AlwaysTake
                    || Rand.Chance(itemPool.SelectionChance);

                if (shouldGenerate)
                {
                    var poolItem = GeneratePoolItem(itemPool, pawn);
                    if (poolItem != null)
                    {
                        yield return poolItem;
                    }
                }
            }
        }

        private static SpecRequirementEdit PickFromPool(List<SpecRequirementEdit> pool, Pawn pawn)
        {
            if (pool.NullOrEmpty()) return null;
            
            var valid = pool.Where(a => a.Thing != null && (a.Thing.IsApparel ? a.Thing.apparel.PawnCanWear(pawn) : true));
            
            var validList = valid.ToList();
            if (validList.NullOrEmpty()) return null;
            
            float totalWeight = validList.Sum(i => i.weight * i.SelectionChance);
            if (totalWeight <= 0f) return validList.RandomElement();
            
            float randomValue = Rand.Value * totalWeight;
            float currentWeight = 0f;
            foreach (var item in validList)
            {
                currentWeight += item.weight * item.SelectionChance;
                if (randomValue <= currentWeight) return item;
            }
            return validList.First();
        }

        private static GearItem GetRandomGearItem(List<GearItem> items)
        {
            if (!items.Any()) return null;
            var totalWeight = items.Sum(item => item.weight);
            if (totalWeight <= 0f) return items.RandomElement();
            var randomValue = Rand.Value * totalWeight;
            var currentWeight = 0f;
            foreach (var item in items)
            {
                currentWeight += item.weight;
                if (randomValue <= currentWeight) return item;
            }
            return items.First();
        }

        /// <summary>
        /// 检查物品的科技等级是否符合限制
        /// </summary>
        private static bool IsTechLevelAllowed(ThingDef def, TechLevel? limit)
        {
            if (!limit.HasValue) return true;
            if (def == null) return true;

            // 获取物品的科技等级，如果没有设置则使用默认值
            TechLevel itemTechLevel = def.techLevel;

            // 如果物品没有设置科技等级，允许通过
            if (itemTechLevel == TechLevel.Undefined) return true;

            // 只允许科技等级小于等于限制等级的物品
            return itemTechLevel <= limit.Value;
        }

        /// <summary>
        /// 获取生效的科技等级限制（考虑派系默认值）
        /// </summary>
        private static TechLevel? GetEffectiveTechLevelLimit(KindGearData kindData, Pawn pawn)
        {
            // 如果设置了明确的限制，使用设置的值
            if (kindData.TechLevelLimit.HasValue)
                return kindData.TechLevelLimit.Value;

            // 否则使用派系的默认科技等级
            FactionDef factionDef = pawn.Faction?.def;
            if (factionDef != null && factionDef.techLevel != TechLevel.Undefined)
                return factionDef.techLevel;

            // 如果没有派系，尝试从兵种定义获取
            // 使用反射获取 defaultFactionType 字段（RimWorld 1.6 中可能是非公开的）
            var defaultFactionTypeField = typeof(PawnKindDef).GetField("defaultFactionType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (defaultFactionTypeField != null && pawn.kindDef != null)
            {
                var kindFactionDef = defaultFactionTypeField.GetValue(pawn.kindDef) as FactionDef;
                if (kindFactionDef != null && kindFactionDef.techLevel != TechLevel.Undefined)
                    return kindFactionDef.techLevel;
            }

            return null;
        }
    }
}
