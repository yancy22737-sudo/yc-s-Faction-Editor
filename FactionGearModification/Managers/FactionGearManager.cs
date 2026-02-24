using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public static class FactionGearManager
    {
        // 静态缓�?- 只在游戏启动时计算一次，大幅提升性能
        private static List<ThingDef> cachedAllWeapons = null;
        private static List<ThingDef> cachedAllMeleeWeapons = null;
        private static List<ThingDef> cachedAllHelmets = null;
        private static List<ThingDef> cachedAllArmors = null;
        private static List<ThingDef> cachedAllApparel = null;
        private static List<ThingDef> cachedAllOthers = null;
        private static bool cacheInitialized = false;
        private static readonly object cacheLock = new object();

        // 确保缓存已初始化
        private static void EnsureCacheInitialized()
        {
            if (cacheInitialized) return;
            
            lock (cacheLock)
            {
                if (cacheInitialized) return;
                
                var allDefs = DefDatabase<ThingDef>.AllDefs.ToList();

                // --- Weapon 分类逻辑 ---
                var allWeapons = allDefs.Where(t => t.IsWeapon).ToList();
                var processedWeapons = new HashSet<ThingDef>();

                // 1. 远程武器：IsRangedWeapon (或者任何有 range > 0 Verb 的武�?
                cachedAllWeapons = allWeapons
                    .Where(t => t.IsRangedWeapon || (t.Verbs != null && t.Verbs.Any(v => v.range > 0)))
                    .ToList();
                foreach (var t in cachedAllWeapons) processedWeapons.Add(t);

                // 2. 近战武器：所有剩下的 IsWeapon 物品作为兜底 (包括 IsMeleeWeapon，或者既不是远程也不是明确近战但�?IsWeapon 标签的怪东�?
                cachedAllMeleeWeapons = allWeapons
                    .Where(t => !processedWeapons.Contains(t))
                    .ToList();
                
                // --- Apparel 分类逻辑 ---
                var allApparel = allDefs.Where(t => t.IsApparel).ToList();
                var processedApparel = new HashSet<ThingDef>();

                // 1. 头盔 (Helmets): IsHelmet
                cachedAllHelmets = allApparel.Where(t => IsHelmet(t)).ToList();
                foreach (var t in cachedAllHelmets) processedApparel.Add(t);
                
                // 2. 护甲 (Armors): IsArmor (且未处理)
                cachedAllArmors = allApparel.Where(t => !processedApparel.Contains(t) && IsArmor(t)).ToList();
                foreach (var t in cachedAllArmors) processedApparel.Add(t);
                
                // 3. 普通服�?(Apparel): IsStandardApparel (且未处理)
                cachedAllApparel = allApparel.Where(t => !processedApparel.Contains(t) && IsStandardApparel(t)).ToList();
                foreach (var t in cachedAllApparel) processedApparel.Add(t);

                // 4. 其他/配件 (Others): 剩下的所�?Apparel
                cachedAllOthers = allApparel.Where(t => !processedApparel.Contains(t)).ToList();
                
                cacheInitialized = true;
            }
        }

        public static void LoadDefaultPresets()
        {
            // Removed full loading on startup to improve performance
            // LoadDefaultPresets(null); 
        }

        public static void LoadDefaultPresets(string factionDefName)
        {
            var factionDefs = factionDefName != null 
                ? new List<FactionDef> { DefDatabase<FactionDef>.GetNamedSilentFail(factionDefName) }
                : new List<FactionDef>(); // Empty list if no specific faction requested

            foreach (var factionDef in factionDefs)
            {
                if (factionDef == null)
                    continue;

                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(factionDef.defName);
                if (factionDef.pawnGroupMakers != null)
                {
                    foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                    {
                        if (pawnGroupMaker.options != null)
                        {
                            foreach (var option in pawnGroupMaker.options)
                            {
                                if (option.kind != null)
                                {
                                    var kindData = factionData.GetOrCreateKindData(option.kind.defName);
                                    LoadKindDefGear(option.kind, kindData);
                                }
                            }
                        }
                    }
                }
            }
        }

        // [修复] �?LoadKindDefGear 改为 public，供重置单兵种时重新抓取数据使用
        public static void LoadKindDefGear(PawnKindDef kindDef, KindGearData kindData)
        {
            // 1. 读取原版武器标签
            if (kindDef.weaponTags != null)
            {
                foreach (var tag in kindDef.weaponTags)
                {
                    var weapons = DefDatabase<ThingDef>.AllDefs.Where(t => t.IsWeapon && t.weaponTags != null && t.weaponTags.Contains(tag)).ToList();
                    foreach (var weapon in weapons)
                    {
                        if (IsRangedWeapon(weapon))
                        {
                            if (!kindData.weapons.Any(g => g.thingDefName == weapon.defName))
                                kindData.weapons.Add(new GearItem(weapon.defName));
                        }
                        else
                        {
                            // 剩下的所有武器都归为近战 (作为兜底)
                            if (!kindData.meleeWeapons.Any(g => g.thingDefName == weapon.defName))
                                kindData.meleeWeapons.Add(new GearItem(weapon.defName));
                        }
                    }
                }
            }

            // 2. 【核心修复】读取原版服装标�?(apparelTags)
            if (kindDef.apparelTags != null)
            {
                var apparels = DefDatabase<ThingDef>.AllDefs.Where(t => t.IsApparel && t.apparel.tags != null && t.apparel.tags.Intersect(kindDef.apparelTags).Any()).ToList();
                foreach (var app in apparels)
                {
                    // 使用统一的分类逻辑
                    if (IsArmor(app))
                    {
                        if (!kindData.armors.Any(g => g.thingDefName == app.defName)) kindData.armors.Add(new GearItem(app.defName));
                    }
                    else if (IsStandardApparel(app) && !IsHelmet(app)) // 排除头盔，避免普通帽子混入 Apparel（视具体需求而定，或者帽子就该在 Apparel）
                    {
                        if (!kindData.apparel.Any(g => g.thingDefName == app.defName)) kindData.apparel.Add(new GearItem(app.defName));
                    }
                    else
                    {
                        // 兜底：包括 Belt, 鞋子, 以及可能的低护甲头盔(如果没进Apparel)
                        if (!kindData.others.Any(g => g.thingDefName == app.defName)) kindData.others.Add(new GearItem(app.defName));
                    }
                }
            }
        }

        // --- 辅助分类方法 ---
        private static bool IsRangedWeapon(ThingDef t) => t.IsRangedWeapon || (t.Verbs != null && t.Verbs.Any(v => v.range > 0));

        private static bool IsHelmet(ThingDef t) => t.apparel?.layers?.Contains(ApparelLayerDefOf.Overhead) ?? false;

        private static bool IsArmor(ThingDef t)
        {
            if (t.apparel?.layers == null) return false;
            // 包含 Shell 层 或 锐利护甲 >= 40%
            return t.apparel.layers.Contains(ApparelLayerDefOf.Shell) || 
                   t.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) >= 0.4f;
        }

        private static bool IsStandardApparel(ThingDef t)
        {
            if (t.apparel?.layers == null) return false;
            return t.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) || 
                   t.apparel.layers.Contains(ApparelLayerDefOf.Middle);
        }

        public static List<ThingDef> GetAllWeapons() { EnsureCacheInitialized(); return cachedAllWeapons; }
        public static List<ThingDef> GetAllMeleeWeapons() { EnsureCacheInitialized(); return cachedAllMeleeWeapons; }
        public static List<ThingDef> GetAllHelmets() { EnsureCacheInitialized(); return cachedAllHelmets; }
        public static List<ThingDef> GetAllArmors() { EnsureCacheInitialized(); return cachedAllArmors; }
        public static List<ThingDef> GetAllApparel() { EnsureCacheInitialized(); return cachedAllApparel; }
        public static List<ThingDef> GetAllOthers() { EnsureCacheInitialized(); return cachedAllOthers; }

        public static float GetWeaponRange(ThingDef weaponDef)
        {
            // 检查武器定义是否有效
            if (weaponDef == null)
                return 0f;

            // 检查武器是否有Verbs属性
            if (weaponDef.Verbs != null && weaponDef.Verbs.Count > 0)
            {
                // 返回第一个Verb的射�?                return weaponDef.Verbs[0].range;
            }

            // 对于没有Verbs的武器（如某些特殊武器），返�?
            return 0f;
        }

        public static float GetWeaponDamage(ThingDef weaponDef)
        {
            // 检查武器定义是否有效
            if (weaponDef == null)
                return 0f;

            float maxDamage = 0f;

            // 1. 读取远程武器的子弹伤害
            if (weaponDef.IsRangedWeapon && weaponDef.Verbs != null && weaponDef.Verbs.Count > 0)
            {
                var verb = weaponDef.Verbs[0];
                if (verb != null)
                {
                    // 检查是否有默认投射物
                    var projectileDef = verb.defaultProjectile;
                    if (projectileDef != null && projectileDef.projectile != null)
                    {
                        try
                        {
                            // 优先尝试反射获取 damageAmountBase (1.5+)
                            var field = projectileDef.projectile.GetType().GetField("damageAmountBase");
                            if (field != null)
                            {
                                maxDamage = (int)field.GetValue(projectileDef.projectile);
                            }
                            else
                            {
                                // 回退尝试反射调用 GetDamageAmount (1.4-)
                                var method = projectileDef.projectile.GetType().GetMethod("GetDamageAmount");
                                if (method != null)
                                {
                                    maxDamage = (int)method.Invoke(projectileDef.projectile, new object[] { null });
                                }
                                else
                                {
                                    maxDamage = 0f;
                                }
                            }
                        }
                        catch
                        {
                            // 兼容性异常处理
                            maxDamage = 0f;
                        }
                    }
                }
            }

            // 2. 读取武器的近战伤害(近战武器，或是远程武器的枪托砸击)
            if (weaponDef.tools != null && weaponDef.tools.Count > 0)
            {
                float meleeDamage = weaponDef.tools.Max(tool => tool.power);
                if (meleeDamage > maxDamage)
                {
                    maxDamage = meleeDamage;
                }
            }

            return maxDamage;
        }

        public static float GetWeaponAccuracy(ThingDef weaponDef)
        {
            if (weaponDef == null)
                return 0f;
                
            try
            {
                return weaponDef.GetStatValueAbstract(StatDefOf.AccuracyMedium);
            }
            catch
            {
                return 0f;
            }
        }
        
        public static float CalculateWeaponDPS(ThingDef weaponDef)
        {
            if (weaponDef == null)
                return 0f;
                
            float damage = GetWeaponDamage(weaponDef);
            float cooldown = 1f; // 默认冷却时间
            
            try
            {
                // 获取武器的射击 攻击间隔
                if (weaponDef.IsRangedWeapon && weaponDef.Verbs != null && weaponDef.Verbs.Count > 0)
                {
                    var verb = weaponDef.Verbs.FirstOrDefault(v => v.isPrimary) ?? weaponDef.Verbs.First();
                    // 使用预热时间作为主要冷却指标（避免访问可能不存在的 CooldownTime 属性）
                    cooldown = verb.warmupTime + 0.5f; // 假设射击间隔为 0.5 秒
                }
                else if (weaponDef.IsMeleeWeapon && weaponDef.tools != null && weaponDef.tools.Count > 0)
                {
                    // 近战武器使用工具的平均冷却时间
                    cooldown = weaponDef.tools.Average(tool => tool.cooldownTime);
                }
            }
            catch
            {
                cooldown = 1f;
            }
            
            if (cooldown <= 0)
                cooldown = 1f;
                
            return damage / cooldown;
        }
        
        public static float GetArmorRatingSharp(ThingDef apparelDef)
        {
            if (apparelDef == null)
                return 0f;
                
            try
            {
                return apparelDef.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp);
            }
            catch
            {
                return 0f;
            }
        }
        
        public static float GetArmorRatingBlunt(ThingDef apparelDef)
        {
            if (apparelDef == null)
                return 0f;
                
            try
            {
                return apparelDef.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt);
            }
            catch
            {
                return 0f;
            }
        }
        
        public static TechLevel GetTechLevel(ThingDef thingDef) => thingDef.techLevel;
        public static string GetModSource(ThingDef thingDef) => thingDef.modContentPack?.Name ?? "Unknown";
    }
}