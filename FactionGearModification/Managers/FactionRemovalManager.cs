using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionGearCustomizer.Managers
{
    public static class FactionRemovalManager
    {
        public static void RemoveFaction(Faction faction)
        {
            if (faction == null) return;
            if (faction.IsPlayer) throw new InvalidOperationException("Cannot remove player faction.");

            try
            {
                // 1. 移除所有据点
                RemoveSettlements(faction);

                // 2. 处理地图上的派系成员
                RemoveFactionPawns(faction);

                // 3. 处理WorldPawns中的派系成员（必须在清理关系之前）
                RemoveWorldPawns(faction);

                // 4. 清理世界对象（如商队、远征队等）
                RemoveWorldObjects(faction);

                // 5. 从派系管理器中移除（必须在清理关系之后）
                RemoveFromFactionManager(faction);

                // 6. 清理相关任务
                CleanupQuests(faction);

                Log.Message($"[FactionGearCustomizer] Successfully removed faction: {faction.Name ?? "Unknown"}");
            }
            catch (Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] Error removing faction {faction.Name ?? "Unknown"}: {ex}");
                throw;
            }
        }

        private static void RemoveSettlements(Faction faction)
        {
            if (Find.WorldObjects == null) return;

            var settlements = Find.WorldObjects.Settlements
                .Where(s => s.Faction == faction)
                .ToList();

            foreach (var settlement in settlements)
            {
                try
                {
                    // 如果据点有地图，先清理地图
                    if (settlement.HasMap)
                    {
                        var map = settlement.Map;
                        if (map != null)
                        {
                            // 清理地图上的所有派系成员
                            var pawns = map.mapPawns.AllPawns
                                .Where(p => p.Faction == faction)
                                .ToList();

                            foreach (var pawn in pawns)
                            {
                                if (!pawn.Destroyed)
                                {
                                    pawn.Destroy();
                                }
                            }

                            // 尝试关闭地图（如果安全的话）
                            if (!map.IsPlayerHome && Find.Maps.Count(m => m != map) > 0)
                            {
                                Find.WorldObjects.Remove(settlement);
                            }
                            else
                            {
                                // 如果是玩家基地或唯一地图，只移除派系归属
                                settlement.SetFaction(null);
                            }
                        }
                    }
                    else
                    {
                        Find.WorldObjects.Remove(settlement);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[FactionGearCustomizer] Error removing settlement {settlement.Name ?? "Unknown"}: {ex.Message}");
                }
            }
        }

        private static void RemoveFactionPawns(Faction faction)
        {
            if (Find.Maps == null) return;

            foreach (var map in Find.Maps)
            {
                if (map?.mapPawns == null) continue;

                var factionPawns = map.mapPawns.AllPawns
                    .Where(p => p.Faction == faction && !p.IsPlayerControlled)
                    .ToList();

                foreach (var pawn in factionPawns)
                {
                    try
                    {
                        if (!pawn.Destroyed)
                        {
                            // 如果是囚犯或奴隶，改变派系为中立
                            if (pawn.IsPrisoner || pawn.IsSlave)
                            {
                                pawn.SetFaction(Faction.OfAncients);
                            }
                            else
                            {
                                pawn.Destroy();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[FactionGearCustomizer] Error removing pawn {pawn.LabelCap ?? "Unknown"}: {ex.Message}");
                    }
                }
            }
        }

        private static void RemoveWorldPawns(Faction faction)
        {
            if (Find.WorldPawns == null) return;

            try
            {
                // 获取所有属于该派系的WorldPawns
                var worldPawns = Find.WorldPawns.AllPawnsAliveOrDead
                    .Where(p => p.Faction == faction)
                    .ToList();

                foreach (var pawn in worldPawns)
                {
                    try
                    {
                        if (!pawn.Destroyed)
                        {
                            // 将派系改为古代人派系，避免null引用
                            pawn.SetFaction(Faction.OfAncients);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[FactionGearCustomizer] Error changing faction for world pawn {pawn.LabelCap ?? "Unknown"}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Error processing world pawns: {ex.Message}");
            }
        }

        private static void RemoveWorldObjects(Faction faction)
        {
            if (Find.WorldObjects == null) return;

            // 获取所有属于该派系的世界对象
            var worldObjects = Find.WorldObjects.AllWorldObjects
                .Where(wo => wo.Faction == faction)
                .ToList();

            foreach (var worldObject in worldObjects)
            {
                try
                {
                    // 对于商队等特殊对象，安全移除
                    if (worldObject is Caravan caravan)
                    {
                        // 解散商队
                        caravan.Destroy();
                    }
                    else
                    {
                        Find.WorldObjects.Remove(worldObject);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[FactionGearCustomizer] Error removing world object {worldObject?.ToString() ?? "Unknown"}: {ex.Message}");
                }
            }
        }

        private static void RemoveFromFactionManager(Faction faction)
        {
            if (Find.FactionManager == null) return;

            try
            {
                // 首先清理所有其他派系与被删除派系的关系（必须在从列表移除之前）
                var allFactionsSnapshot = Find.FactionManager.AllFactionsListForReading.ToList();
                foreach (var otherFaction in allFactionsSnapshot)
                {
                    if (otherFaction != faction)
                    {
                        try
                        {
                            // 使用反射直接移除关系，避免触发RelationWith的null检查
                            var relationsField = typeof(Faction).GetField(
                                "relations",
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);

                            if (relationsField != null)
                            {
                                var relations = relationsField.GetValue(otherFaction) as List<FactionRelation>;
                                relations?.RemoveAll(r => r.other == faction);
                            }
                        }
                        catch (Exception relEx)
                        {
                            Log.Warning($"[FactionGearCustomizer] Error removing relation for {otherFaction.Name ?? "Unknown"}: {relEx.Message}");
                        }
                    }
                }

                // 清理被删除派系自身的关系列表
                try
                {
                    var factionRelationsField = typeof(Faction).GetField(
                        "relations",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (factionRelationsField != null)
                    {
                        var factionRelations = factionRelationsField.GetValue(faction) as List<FactionRelation>;
                        factionRelations?.Clear();
                    }
                }
                catch (Exception selfRelEx)
                {
                    Log.Warning($"[FactionGearCustomizer] Error clearing faction's own relations: {selfRelEx.Message}");
                }

                // 使用反射访问私有字段来移除派系
                var allFactionsField = typeof(FactionManager).GetField(
                    "allFactions",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (allFactionsField != null)
                {
                    var allFactions = allFactionsField.GetValue(Find.FactionManager) as List<Faction>;
                    if (allFactions != null)
                    {
                        allFactions.Remove(faction);
                    }
                }

                // 尝试使用公开的Remove方法（如果存在）
                var removeMethod = typeof(FactionManager).GetMethod(
                    "Remove",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (removeMethod != null)
                {
                    removeMethod.Invoke(Find.FactionManager, new object[] { faction });
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Error removing faction from manager: {ex.Message}");
            }
        }

        private static void CleanupQuests(Faction faction)
        {
            if (Find.QuestManager == null) return;

            try
            {
                var quests = Find.QuestManager.QuestsListForReading.ToList();

                foreach (var quest in quests)
                {
                    if (quest == null) continue;

                    // 检查任务是否涉及该派系
                    bool involvesFaction = false;

                    // 检查任务目标
                    foreach (var objective in quest.QuestLookTargets)
                    {
                        if (objective.HasWorldObject && objective.WorldObject.Faction == faction)
                        {
                            involvesFaction = true;
                            break;
                        }
                    }

                    if (involvesFaction)
                    {
                        try
                        {
                            // 结束任务
                            quest.End(QuestEndOutcome.Fail, false);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"[FactionGearCustomizer] Error ending quest {quest.name ?? "Unknown"}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Error cleaning up quests: {ex.Message}");
            }
        }
    }
}
