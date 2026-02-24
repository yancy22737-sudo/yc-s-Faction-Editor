using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;
using FactionGearCustomizer.Managers;

namespace FactionGearCustomizer.Patches
{
    [HarmonyPatch]
    public static class Patch_FloatMenuMakerWorld
    {
        private static MethodBase TargetMethodCache;

        static Patch_FloatMenuMakerWorld()
        {
            // Find the specific ChoicesAtFor method with parameters (int, Pawn)
            // This is the most common signature in RimWorld 1.6
            var methods = AccessTools.GetDeclaredMethods(typeof(FloatMenuMakerWorld))
                .Where(m => m.Name == "ChoicesAtFor")
                .Where(m =>
                {
                    var p = m.GetParameters();
                    return p.Length == 2 && p[0].ParameterType == typeof(int) && p[1].ParameterType == typeof(Pawn);
                })
                .Cast<MethodBase>()
                .ToList();

            TargetMethodCache = methods.FirstOrDefault();

            if (TargetMethodCache == null)
            {
                // Fallback: try to find any ChoicesAtFor with int as first parameter
                methods = AccessTools.GetDeclaredMethods(typeof(FloatMenuMakerWorld))
                    .Where(m => m.Name == "ChoicesAtFor")
                    .Where(m =>
                    {
                        var p = m.GetParameters();
                        return p.Length >= 1 && p[0].ParameterType == typeof(int);
                    })
                    .Cast<MethodBase>()
                    .ToList();

                TargetMethodCache = methods.FirstOrDefault();
            }
        }

        private static MethodBase TargetMethod()
        {
            return TargetMethodCache;
        }

        public static bool Prepare()
        {
            try
            {
                return TargetMethodCache != null;
            }
            catch
            {
                return false;
            }
        }

        public static void Postfix(int __0, Pawn __1, ref List<FloatMenuOption> __result)
        {
            int tile = __0;
            Pawn pawn = __1;
            if (FactionSpawnManager.TargetFactionDef == null) return;
            if (Current.ProgramState != ProgramState.Playing || Current.Game == null || Find.FactionManager == null) return;

            List<FloatMenuOption> resultList = __result;
            if (resultList == null) return;

            FactionDef factionDef = FactionSpawnManager.TargetFactionDef;
            
            // Add header or separator? No need for context menu.
            
            // Check if tile is valid for settlement
            if (!TileFinder.IsValidTileForNewSettlement(tile))
            {
                // Maybe add disabled option explaining why?
                // Or just don't show spawn option.
                // Usually disabled option is better for feedback.
                FloatMenuOption failOption = new FloatMenuOption(LanguageManager.Get("CannotSpawnHere"), null);
                failOption.Disabled = true;
                resultList.Add(failOption);
            }
            else
            {
                // Get instances
                var instances = Find.FactionManager.AllFactions.Where(f => f.def == factionDef && !f.IsPlayer).ToList();

                if (instances.Count == 0)
                {
                    // Option to create new instance and spawn
                    FloatMenuOption createOption = new FloatMenuOption(LanguageManager.Get("CreateAndSpawn", factionDef.LabelCap), () =>
                    {
                        Faction newFaction = FactionSpawnManager.SpawnFactionInstance(factionDef);
                        if (newFaction != null)
                        {
                            FactionSpawnManager.SpawnSettlement(newFaction, tile);
                        }
                    });
                    resultList.Add(createOption);
                }
                else if (instances.Count == 1)
                {
                    Faction instance = instances[0];
                    FloatMenuOption spawnOption = new FloatMenuOption(LanguageManager.Get("SpawnSettlementFor", instance.Name), () =>
                    {
                        FactionSpawnManager.SpawnSettlement(instance, tile);
                    });
                    resultList.Add(spawnOption);
                }
                else
                {
                    // Submenu for multiple instances
                    List<FloatMenuOption> subOptions = new List<FloatMenuOption>();
                    foreach (var instance in instances)
                    {
                        subOptions.Add(new FloatMenuOption(instance.Name, () =>
                        {
                            FactionSpawnManager.SpawnSettlement(instance, tile);
                        }));
                    }
                    
                    // Create a parent option that opens the submenu?
                    // RimWorld doesn't support nested FloatMenus easily in this context without custom handling.
                    // Usually we just list them all: "Spawn Base (Faction 1)", "Spawn Base (Faction 2)"
                    
                    foreach (var instance in instances)
                    {
                         FloatMenuOption spawnOption = new FloatMenuOption(LanguageManager.Get("SpawnSettlementFor", instance.Name), () =>
                        {
                            FactionSpawnManager.SpawnSettlement(instance, tile);
                        });
                        resultList.Add(spawnOption);
                    }
                }
            }

            // Add option to exit spawning mode
            FloatMenuOption exitOption = new FloatMenuOption(LanguageManager.Get("ExitSpawningMode"), () =>
            {
                FactionSpawnManager.TargetFactionDef = null;
                Messages.Message(LanguageManager.Get("SpawningModeExited"), MessageTypeDefOf.NeutralEvent, false);
            });
            resultList.Add(exitOption);
        }
    }
}
