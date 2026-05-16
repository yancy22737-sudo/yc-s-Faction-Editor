using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
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
                if (!FactionGearCustomizerMod.Settings.autoSwitchWeaponByRange) return true;

                Pawn pawn = __instance.CasterPawn;
                if (pawn == null) return true;
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
                    if (!FactionGearCustomizerMod.Settings.autoSwitchWeaponByRange) return;

                    Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>()
                             ?? Traverse.Create(__instance).Property("pawn").GetValue<Pawn>();
                    if (pawn?.equipment?.Primary == null || pawn.inventory == null) return;

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

                    // Swap if current weapon is badly out-ranged AND sidearm can reach
                    if ((distance - currentMaxRange) > 2f && sidearmMaxRange >= distance)
                    {
                        if (!SimpleSidearmsCompat.HasAmmoForWeapon(pawn, sidearm)) return;
                        int tick = Find.TickManager.TicksGame;
                        if (!SimpleSidearmsCompat.IsCooldownElapsed(pawn, tick)) return;

                        float currentOpt = SimpleSidearmsCompat.CalculateOptimalRange(currentWeapon);
                        float sidearmOpt = SimpleSidearmsCompat.CalculateOptimalRange(sidearm);
                        if (SimpleSidearmsCompat.SwapWeapons(pawn))
                        {
                            _preJobSwaps++;
                            SimpleSidearmsCompat.RecordSwitch(pawn, tick);
                            Log.Message($"[FactionGearCustomizer] AutoSwitch (pre-job) #{_preJobSwaps}: {pawn.LabelShort} swapped {currentWeapon.LabelCap}(max={currentMaxRange:F0}) -> {sidearm.LabelCap}(max={sidearmMaxRange:F0}) (dist={distance:F0}, over={distance - currentMaxRange:F0})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[FactionGearCustomizer] AutoSwitch pre-job error: {ex.Message}");
                }
            }
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
