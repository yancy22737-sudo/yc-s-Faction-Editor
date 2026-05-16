using System;
using Verse;

namespace FactionGearCustomizer.Compat
{
    public static class SimpleSidearmsCompat
    {
        public static bool IsActive { get; private set; }

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
    }
}
