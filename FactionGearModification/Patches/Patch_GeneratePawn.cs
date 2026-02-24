using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_GeneratePawn
    {
        [ThreadStatic]
        private static bool isApplyingGear;

        public static void Postfix(Pawn __result, PawnGenerationRequest request)
        {
            if (isApplyingGear)
            {
                return;
            }

            // 在世界生成阶段，玩家派系尚未创建，Faction.OfPlayer 会报错
            // 因此跳过此阶段的装备应用
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            if (__result != null && __result.RaceProps != null && __result.RaceProps.Humanlike)
            {
                Faction faction = request.Faction ?? __result.Faction;
                if (faction != null)
                {
                    try
                    {
                        isApplyingGear = true;
                        GearApplier.ApplyCustomGear(__result, faction);
                    }
                    finally
                    {
                        isApplyingGear = false;
                    }
                }
            }
        }
    }
}