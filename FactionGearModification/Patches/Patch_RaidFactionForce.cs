using HarmonyLib;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public static class RaidFactionHelper
    {
        public static Faction ForcedFaction;
    }

    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryResolveRaidFaction")]
    public static class Patch_RaidEnemyFactionForce
    {
        static void Postfix(IncidentParms parms)
        {
            if (RaidFactionHelper.ForcedFaction != null)
            {
                parms.faction = RaidFactionHelper.ForcedFaction;
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_RaidFriendly), "TryResolveRaidFaction")]
    public static class Patch_RaidFriendlyFactionForce
    {
        static void Postfix(IncidentParms parms)
        {
            if (RaidFactionHelper.ForcedFaction != null)
            {
                parms.faction = RaidFactionHelper.ForcedFaction;
            }
        }
    }
}
