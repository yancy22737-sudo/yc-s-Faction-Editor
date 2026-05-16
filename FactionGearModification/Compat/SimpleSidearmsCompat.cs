using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using FactionGearCustomizer.Compat.AmmoProviders;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer.Utils;

namespace FactionGearCustomizer.Compat
{
    public static class SimpleSidearmsCompat
    {
        public static bool IsActive { get; private set; }

        // Cooldown tracking: pawn -> last switch tick
        private static readonly Dictionary<Pawn, int> LastSwitchTick = new Dictionary<Pawn, int>();
        private const int SwitchCooldownTicks = 300;
        private static int _cleanupCounter = 0;

        // Cache optimal ranges per ThingDef
        private static readonly Dictionary<ThingDef, float> OptimalRangeCache = new Dictionary<ThingDef, float>();

        // Track which pawns have sidearms to skip inventory scanning
        private static readonly HashSet<Pawn> PawnsWithSidearm = new HashSet<Pawn>();
        private static readonly HashSet<Pawn> PawnsWithoutSidearm = new HashSet<Pawn>();

        static SimpleSidearmsCompat()
        {
            IsActive = ModsConfig.IsActive("PeteTimesSix.SimpleSidearms");
        }

        /// <summary>
        /// Move the pawn's current primary weapon into inventory as a sidearm,
        /// making room for a new primary weapon to be equipped.
        /// </summary>
        public static bool MovePrimaryToSidearm(Pawn pawn)
        {
            if (!IsActive || pawn?.equipment?.Primary == null || pawn.inventory == null)
                return false;

            // Never move if the primary is on the ground (e.g. CE reload)
            if (pawn.equipment.Primary.Spawned)
                return false;

            try
            {
                var primary = pawn.equipment.Primary;

                // Try drop (only if spawned, otherwise direct transfer)
                if (pawn.Spawned && pawn.equipment.TryDropEquipment(primary, out var dropped, pawn.Position, false))
                {
                    if (pawn.inventory.innerContainer.TryAdd(dropped, false))
                    {
                        Log.Message($"[FactionGearCustomizer] SimpleSidearms: moved {dropped.LabelCap} to sidearm inventory");
                        return true;
                    }
                    pawn.equipment.AddEquipment((ThingWithComps)dropped);
                    return false;
                }

                // Direct transfer (unspawned pawn or drop failed)
                pawn.equipment.Remove(primary);
                if (pawn.inventory.innerContainer.TryAdd(primary, false))
                {
                    Log.Message($"[FactionGearCustomizer] SimpleSidearms: moved {primary.LabelCap} to sidearm inventory (direct)");
                    return true;
                }

                pawn.equipment.AddEquipment(primary);
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] SimpleSidearms: MovePrimaryToSidearm failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Calculate the optimal engagement range for a weapon (cached per ThingDef).
        /// Melee → 1.5f. Ranged → maxRange * 0.6f.
        /// </summary>
        public static float CalculateOptimalRange(ThingWithComps weapon)
        {
            if (weapon?.def == null) return 1.5f;

            if (OptimalRangeCache.TryGetValue(weapon.def, out float cached))
                return cached;

            bool isMelee = !weapon.def.IsRangedWeapon &&
                           (weapon.def.Verbs == null || weapon.def.Verbs.All(v => v.range <= 1.0f));
            float result;
            if (isMelee)
            {
                result = 1.5f;
            }
            else
            {
                float maxRange = FactionGearManager.GetWeaponRange(weapon.def);
                result = maxRange > 0f ? maxRange * 0.6f : 1.5f;
            }

            OptimalRangeCache[weapon.def] = result;
            return result;
        }

        /// <summary>
        /// Find a sidearm weapon in the pawn's inventory (not the current primary).
        /// Uses pawn-level caches to skip inventory scanning when possible.
        /// </summary>
        public static ThingWithComps FindSidearmInInventory(Pawn pawn)
        {
            if (pawn?.inventory?.innerContainer == null) return null;

            // Quick negative cache hit
            if (PawnsWithoutSidearm.Contains(pawn)) return null;

            foreach (var thing in pawn.inventory.innerContainer)
            {
                if (thing is ThingWithComps tc && tc.def.IsWeapon && tc != pawn.equipment?.Primary)
                    return tc;
            }
            return null;
        }

        /// <summary>
        /// Register a pawn's sidearm status after gear assignment.
        /// Called from GearApplier to populate the caches proactively.
        /// </summary>
        public static void RegisterPawnSidearmStatus(Pawn pawn, bool hasSidearm)
        {
            if (pawn == null) return;
            if (hasSidearm)
            {
                PawnsWithSidearm.Add(pawn);
                PawnsWithoutSidearm.Remove(pawn);
            }
            else
            {
                PawnsWithoutSidearm.Add(pawn);
                PawnsWithSidearm.Remove(pawn);
            }
        }

        /// <summary>
        /// Swap the pawn's primary weapon with the sidearm in inventory.
        /// Returns true on success.
        /// </summary>
        public static bool SwapWeapons(Pawn pawn)
        {
            if (!IsActive || pawn?.equipment == null || pawn.inventory == null)
                return false;

            ThingWithComps sidearm = FindSidearmInInventory(pawn);
            if (sidearm == null) return false;

            // If pawn has no primary, just equip the sidearm directly
            if (pawn.equipment.Primary == null)
            {
                pawn.inventory.innerContainer.Remove(sidearm);
                pawn.equipment.AddEquipment(sidearm);
                LogUtils.DebugLog($"AutoSwitch: {pawn.LabelShort} equipped sidearm {sidearm.LabelCap} (no primary)");
                UpdatePawnCacheAfterSwap(pawn);
                return true;
            }

            var currentPrimary = pawn.equipment.Primary;
            if (currentPrimary == sidearm) return false;

            // Never swap if the primary weapon is on the ground (e.g. CE reload)
            if (currentPrimary.Spawned)
                return false;

            // Direct transfer: Remove primary from equipment, add to inventory
            // (avoids TryDropEquipment which fails mid-combat)
            pawn.equipment.Remove(currentPrimary);
            if (!pawn.inventory.innerContainer.TryAdd(currentPrimary, false))
            {
                // Restore primary if inventory transfer fails
                pawn.equipment.AddEquipment(currentPrimary);
                LogUtils.DebugLog($"AutoSwitch: Failed to move primary {currentPrimary.LabelCap} to inventory");
                return false;
            }

            // Remove sidearm from inventory and equip as new primary
            pawn.inventory.innerContainer.Remove(sidearm);
            pawn.equipment.AddEquipment(sidearm);

            LogUtils.DebugLog($"AutoSwitch: {pawn.LabelShort} swapped {currentPrimary.LabelCap} <-> {sidearm.LabelCap}");
            UpdatePawnCacheAfterSwap(pawn);
            return true;
        }

        private static void UpdatePawnCacheAfterSwap(Pawn pawn)
        {
            bool stillHasSidearm = pawn.inventory?.innerContainer != null &&
                                   pawn.inventory.innerContainer.Any(t => t is ThingWithComps tc && tc.def.IsWeapon);
            RegisterPawnSidearmStatus(pawn, stillHasSidearm);
        }

        /// <summary>
        /// Check whether the pawn has compatible ammo for the given weapon.
        /// Returns true if the weapon does not need ammo OR matched ammo is present.
        /// </summary>
        public static bool HasAmmoForWeapon(Pawn pawn, ThingWithComps weapon)
        {
            if (weapon?.def == null) return true;
            if (!AmmoProviderManager.WeaponNeedsAmmo(weapon.def)) return true;

            var requiredAmmoDefs = AmmoProviderManager.GetAllAvailableAmmo(weapon.def);
            if (requiredAmmoDefs.Count == 0) return true;

            if (pawn?.inventory?.innerContainer == null) return false;

            foreach (var ammoDef in requiredAmmoDefs)
            {
                if (pawn.inventory.innerContainer.Any(t => t.def == ammoDef && t.stackCount > 0))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if enough ticks have passed since the pawn's last weapon switch.
        /// </summary>
        public static bool IsCooldownElapsed(Pawn pawn, int currentTick)
        {
            if (pawn == null) return true;
            if (LastSwitchTick.TryGetValue(pawn, out int lastTick))
                return (currentTick - lastTick) >= SwitchCooldownTicks;
            return true;
        }

        /// <summary>
        /// Record a weapon switch for the pawn at the given tick.
        /// </summary>
        public static void RecordSwitch(Pawn pawn, int tick)
        {
            if (pawn == null) return;
            LastSwitchTick[pawn] = tick;
            CleanupDeadPawns();
        }

        /// <summary>
        /// Periodically remove entries for dead or destroyed pawns from all caches.
        /// </summary>
        private static void CleanupDeadPawns()
        {
            _cleanupCounter++;
            if (_cleanupCounter < 600) return;
            _cleanupCounter = 0;

            var deadPawns = LastSwitchTick.Keys
                .Where(p => p == null || p.Destroyed || p.Dead)
                .ToList();
            foreach (var p in deadPawns)
                LastSwitchTick.Remove(p);

            PawnsWithSidearm.RemoveWhere(p => p == null || p.Destroyed || p.Dead);
            PawnsWithoutSidearm.RemoveWhere(p => p == null || p.Destroyed || p.Dead);
        }
    }
}
