using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using FactionGearCustomizer.Compat;

namespace FactionGearCustomizer
{
    [HarmonyPatch]
    public static class Patch_AutoWeaponSwitch
    {
        private const float SwitchThreshold = 1.5f;

        // Diagnostics counters
        private static bool _patchConfirmed;
        private static int _totalChecks;
        private static int _noSidearm;
        private static int _noValidTarget;
        private static int _belowThreshold;
        private static int _noAmmo;
        private static int _onCooldown;
        private static int _swapFailed;
        private static int _swapsDone;
        private static int _lastReportTick;
        private const int ReportInterval = 300;

        static MethodBase TargetMethod()
        {
            var methods = typeof(Verb).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == "TryStartCastOn")
                .ToList();
            if (methods.Count == 0)
            {
                methods = typeof(Verb).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name == "TryStartCastOn")
                    .ToList();
            }
            foreach (var m in methods)
            {
                var p = m.GetParameters();
                if (p.Length >= 1 && p[0].ParameterType == typeof(LocalTargetInfo))
                {
                    if (p.Length >= 2 && p[1].ParameterType == typeof(LocalTargetInfo))
                        continue;
                    return m;
                }
            }
            return methods.FirstOrDefault();
        }

        public static bool Prefix(Verb __instance, object[] __args)
        {
            try
            {
                // Confirm patch is active (one-time)
                if (!_patchConfirmed)
                {
                    _patchConfirmed = true;
                    Log.Message("[FactionGearCustomizer] AutoSwitch patch ACTIVE — monitoring TryStartCastOn");
                }

                _totalChecks++;

                if (!SimpleSidearmsCompat.IsActive) return true;

                Pawn pawn = __instance.CasterPawn;
                if (pawn == null) return true;
                if (!IsAutoSwitchEnabled(pawn)) return true;
                if (pawn.equipment?.Primary == null || pawn.inventory == null) return true;

                ThingWithComps sidearm = SimpleSidearmsCompat.FindSidearmInInventory(pawn);
                if (sidearm == null) { _noSidearm++; PeriodicReport(); return true; }

                LocalTargetInfo castTarg = default;
                if (__args != null && __args.Length > 0 && __args[0] is LocalTargetInfo t)
                    castTarg = t;
                if (!castTarg.IsValid) { _noValidTarget++; PeriodicReport(); return true; }

                ThingWithComps currentWeapon = pawn.equipment.Primary;
                float distance = castTarg.Cell.DistanceTo(pawn.Position);
                float currentOptimal = SimpleSidearmsCompat.CalculateOptimalRange(currentWeapon);
                float sidearmOptimal = SimpleSidearmsCompat.CalculateOptimalRange(sidearm);
                float currentDeviation = Math.Abs(distance - currentOptimal);
                float sidearmDeviation = Math.Abs(distance - sidearmOptimal);

                // Can-hit check: use actual weapon range without artificial padding
                float currentMaxRange = FactionGearManager.GetWeaponRange(currentWeapon.def);
                float sidearmMaxRange = FactionGearManager.GetWeaponRange(sidearm.def);
                // Melee weapons: generous reach of 2.5 tiles; ranged: actual range
                float effectiveCurrentRange = currentMaxRange <= 2.5f ? 2.5f : currentMaxRange;
                float effectiveSidearmRange = sidearmMaxRange <= 2.5f ? 2.5f : sidearmMaxRange;
                bool currentCanHit = distance <= effectiveCurrentRange;
                bool sidearmCanHit = distance <= effectiveSidearmRange;

                if (!sidearmCanHit)
                {
                    // Sidearm can't reach target → pointless to switch
                    return true;
                }

                if (currentCanHit)
                {
                    // Both can hit → use optimal-range comparison
                    if (currentDeviation - sidearmDeviation < SwitchThreshold)
                    {
                        _belowThreshold++;
                        PeriodicReport();
                        return true;
                    }
                }
                // else: current can't hit, sidearm can → force switch attempt (skip threshold)

                if (!SimpleSidearmsCompat.HasAmmoForWeapon(pawn, sidearm))
                {
                    _noAmmo++;
                    PeriodicReport();
                    return true;
                }

                int currentTick = Find.TickManager.TicksGame;
                if (!SimpleSidearmsCompat.IsCooldownElapsed(pawn, currentTick))
                {
                    _onCooldown++;
                    PeriodicReport();
                    return true;
                }

                bool swapped = SimpleSidearmsCompat.SwapWeapons(pawn);
                if (swapped)
                {
                    _swapsDone++;
                    SimpleSidearmsCompat.RecordSwitch(pawn, currentTick);
                    Log.Message($"[FactionGearCustomizer] AutoSwitch #{_swapsDone}: {pawn.LabelShort} swapped {currentWeapon.LabelCap} -> {sidearm.LabelCap} (dist={distance:F0}, currOpt={currentOptimal:F0}/{currentDeviation:F0}, sideOpt={sidearmOptimal:F0}/{sidearmDeviation:F0})");
                    // Interrupt current job so AI re-evaluates with the new weapon immediately
                    if (pawn.jobs?.curJob != null)
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    return false;
                }
                else
                {
                    _swapFailed++;
                    PeriodicReport();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] AutoSwitch patch error: {ex.Message}");
            }
            return true;
        }

        // Pre-job weapon swap: intercept combat jobs before pawn walks toward target
        [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
        public static class Patch_AutoWeaponSwitch_JobPreSwap
        {
            private static int _preJobChecks;
            private static int _preJobSwaps;

            public static void Prefix(Pawn_JobTracker __instance, Job newJob)
            {
                try
                {
                    if (!SimpleSidearmsCompat.IsActive) return;

                    Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>()
                             ?? Traverse.Create(__instance).Property("pawn").GetValue<Pawn>();
                    if (pawn == null) return;
                    if (!IsAutoSwitchEnabled(pawn)) return;
                    if (_isPlayerOrderedJob && pawn.Faction == Faction.OfPlayer) return;
                    if (pawn.equipment?.Primary == null || pawn.inventory == null) return;

                    // Accept any valid target with a cell (Thing or Cell)
                    if (!newJob.targetA.IsValid) return;
                    float distance = newJob.targetA.Cell.DistanceTo(pawn.Position);
                    if (distance <= 0f) return;

                    ThingWithComps sidearm = SimpleSidearmsCompat.FindSidearmInInventory(pawn);
                    if (sidearm == null) return;

                    ThingWithComps currentWeapon = pawn.equipment.Primary;
                    float currentMaxRange = FactionGearManager.GetWeaponRange(currentWeapon.def);
                    float sidearmMaxRange = FactionGearManager.GetWeaponRange(sidearm.def);

                    _preJobChecks++;
                    if (_preJobChecks % 20 == 1)
                        Log.Message($"[FactionGearCustomizer] AutoSwitch pre-job check #{_preJobChecks}: {pawn.LabelShort} job={newJob.def.defName} tgt={newJob.targetA.Cell} dist={distance:F0} curMax={currentMaxRange:F0} sideMax={sidearmMaxRange:F0}");

                    float currentOpt = SimpleSidearmsCompat.CalculateOptimalRange(currentWeapon);
                    float sidearmOpt = SimpleSidearmsCompat.CalculateOptimalRange(sidearm);
                    float currentEffectiveRange = currentMaxRange <= 2.5f ? 2.5f : currentMaxRange;
                    float sidearmEffectiveRange = sidearmMaxRange <= 2.5f ? 2.5f : sidearmMaxRange;
                    bool currentCanHit = distance <= currentEffectiveRange;
                    bool sidearmCanHit = distance <= sidearmEffectiveRange;

                    bool shouldSwap = false;

                    // Condition 1: current weapon badly out-ranged, sidearm can reach
                    if ((distance - currentMaxRange) > 2f && sidearmMaxRange >= distance)
                        shouldSwap = true;

                    // Condition 2: current can't hit but sidearm can — force swap
                    if (!currentCanHit && sidearmCanHit)
                        shouldSwap = true;

                    // Condition 3: both can hit but sidearm is significantly better at this distance
                    if (currentCanHit && sidearmCanHit)
                    {
                        float currentDeviation = Math.Abs(distance - currentOpt);
                        float sidearmDeviation = Math.Abs(distance - sidearmOpt);
                        if (currentDeviation - sidearmDeviation >= 1.5f)
                            shouldSwap = true;
                    }

                    if (shouldSwap)
                    {
                        if (!SimpleSidearmsCompat.HasAmmoForWeapon(pawn, sidearm)) return;
                        int tick = Find.TickManager.TicksGame;
                        if (!SimpleSidearmsCompat.IsCooldownElapsed(pawn, tick)) return;

                        if (SimpleSidearmsCompat.SwapWeapons(pawn))
                        {
                            _preJobSwaps++;
                            SimpleSidearmsCompat.RecordSwitch(pawn, tick);
                            Log.Message($"[FactionGearCustomizer] AutoSwitch (pre-job) #{_preJobSwaps}: {pawn.LabelShort} swapped {currentWeapon.LabelCap}(max={currentMaxRange:F0}) -> {sidearm.LabelCap}(max={sidearmMaxRange:F0}) (dist={distance:F0}, over={distance - currentMaxRange:F0})");

                            // For player-controlled pawns: retype the job to match the new weapon
                            // (AttackMelee with ranged = walk up and bash; AttackStatic with melee = won't fire)
                            if (pawn.Faction == Faction.OfPlayer)
                            {
                                bool sidearmIsRanged = sidearmMaxRange > 2.5f;
                                bool currentIsRanged = currentMaxRange > 2.5f;
                                if (sidearmIsRanged && !currentIsRanged && newJob.def == JobDefOf.AttackMelee)
                                {
                                    newJob.def = JobDefOf.AttackStatic;
                                    Log.Message($"[FactionGearCustomizer] AutoSwitch (pre-job) retyped job AttackMelee -> AttackStatic for {pawn.LabelShort}");
                                }
                                else if (!sidearmIsRanged && currentIsRanged && newJob.def == JobDefOf.AttackStatic)
                                {
                                    newJob.def = JobDefOf.AttackMelee;
                                    Log.Message($"[FactionGearCustomizer] AutoSwitch (pre-job) retyped job AttackStatic -> AttackMelee for {pawn.LabelShort}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[FactionGearCustomizer] AutoSwitch pre-job error: {ex.Message}");
                }
            }
        }

        // Flag: set by TryTakeOrderedJob prefix so StartJob knows this was a player order.
        private static bool _isPlayerOrderedJob;

        // Minimal intercept on TryTakeOrderedJob — only sets a flag so StartJob
        // can skip auto-swap for player-given orders while still swapping on AI jobs.
        [HarmonyPatch(typeof(Pawn_JobTracker), "TryTakeOrderedJob")]
        public static class Patch_AutoWeaponSwitch_TryTakeOrderedJob
        {
            public static void Prefix(Pawn_JobTracker __instance)
            {
                Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>()
                         ?? Traverse.Create(__instance).Property("pawn").GetValue<Pawn>();
                if (pawn?.Faction == Faction.OfPlayer)
                    _isPlayerOrderedJob = true;
            }

            public static void Postfix()
            {
                _isPlayerOrderedJob = false;
            }
        }

        // Add "Try attack with suitable weapon" right-click option for drafted colonists.
        // Shows when a smarter swap is beneficial (optimal-range check).
        [HarmonyPatch(typeof(FloatMenuMakerMap), "GetProviderOptions")]
        public static class Patch_FloatMenuSuitableWeapon
        {
            private static void Postfix(FloatMenuContext context, List<FloatMenuOption> options)
            {
                try
                {
                    if (!SimpleSidearmsCompat.IsActive) return;
                    if (!FactionGearCustomizerMod.Settings.autoSwitchWeaponByRangeColonist) return;
                    if (options == null) return;

                    Pawn pawn = context.FirstSelectedPawn;
                    if (pawn?.Faction != Faction.OfPlayer) return;
                    if (pawn.equipment?.Primary == null) return;

                    ThingWithComps sidearm = SimpleSidearmsCompat.FindSidearmInInventory(pawn);
                    if (sidearm == null) return;

                    Pawn clickedPawn = context.ClickedPawns?.FirstOrDefault();
                    if (clickedPawn == null || clickedPawn == pawn) return;
                    if (!clickedPawn.HostileTo(Faction.OfPlayer)) return;

                    float distance = clickedPawn.Position.DistanceTo(pawn.Position);
                    if (distance <= 0f) return;

                    ThingWithComps currentWeapon = pawn.equipment.Primary;
                    float curMaxRange = FactionGearManager.GetWeaponRange(currentWeapon.def);
                    float sideMaxRange = FactionGearManager.GetWeaponRange(sidearm.def);
                    float curEffective = curMaxRange <= 2.5f ? 2.5f : curMaxRange;
                    float sideEffective = sideMaxRange <= 2.5f ? 2.5f : sideMaxRange;
                    bool currentCanHit = distance <= curEffective;
                    bool sidearmCanHit = distance <= sideEffective;

                    // Only show option if a swap would actually help
                    if (currentCanHit && !sidearmCanHit) return; // current is already better
                    if (!currentCanHit && !sidearmCanHit) return; // neither can hit
                    if (currentCanHit && sidearmCanHit)
                    {
                        float curDev = Math.Abs(distance - SimpleSidearmsCompat.CalculateOptimalRange(currentWeapon));
                        float sideDev = Math.Abs(distance - SimpleSidearmsCompat.CalculateOptimalRange(sidearm));
                        if (curDev - sideDev < 1.5f) return; // not different enough
                    }

                    Pawn capturedPawn = pawn;
                    Pawn capturedTarget = clickedPawn;
                    ThingWithComps capturedSidearm = sidearm;
                    bool capturedSidearmIsRanged = sideEffective > 2.5f;

                    string label = $"{LanguageManager.Get("TryAttackWithSuitableWeapon")} {clickedPawn.LabelShort}";
                    options.Add(new FloatMenuOption(
                        label,
                        delegate
                        {
                            if (capturedPawn.equipment?.Primary == null || capturedSidearm == null) return;
                            if (SimpleSidearmsCompat.SwapWeapons(capturedPawn))
                                SimpleSidearmsCompat.RecordSwitch(capturedPawn, Find.TickManager.TicksGame);
                            Job attackJob = JobMaker.MakeJob(
                                capturedSidearmIsRanged ? JobDefOf.AttackStatic : JobDefOf.AttackMelee,
                                capturedTarget);
                            capturedPawn.jobs.StartJob(attackJob, JobCondition.InterruptForced);
                        },
                        clickedPawn, Color.white));
                }
                catch (Exception ex)
                {
                    Log.Warning($"[FactionGearCustomizer] FloatMenu suitable-weapon error: {ex.Message}");
                }
            }
        }

        private static bool IsAutoSwitchEnabled(Pawn pawn)
        {
            if (pawn.Faction == Faction.OfPlayer)
                return FactionGearCustomizerMod.Settings.autoSwitchWeaponByRangeColonist;
            return FactionGearCustomizerMod.Settings.autoSwitchWeaponByRange;
        }

        private static void PeriodicReport()
        {
            int tick = Find.TickManager.TicksGame;
            if (tick - _lastReportTick < ReportInterval) return;
            _lastReportTick = tick;
            if (_totalChecks == 0) return;
            Log.Message($"[FactionGearCustomizer] AutoSwitch stats: total={_totalChecks} noSidearm={_noSidearm} noTarget={_noValidTarget} belowThreshold={_belowThreshold} noAmmo={_noAmmo} cooldown={_onCooldown} swapFail={_swapFailed} swapsDone={_swapsDone}");
        }
    }
}
