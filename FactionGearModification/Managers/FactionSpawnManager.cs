using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using FactionGearCustomizer.UI;
using FactionGearCustomizer.UI.Panels;

namespace FactionGearCustomizer.Managers
{
    public static class FactionSpawnManager
    {
        public static FactionDef TargetFactionDef = null;

        public static void EnterSpawningMode(FactionDef factionDef)
        {
            if (factionDef == null) return;
            TargetFactionDef = factionDef;
            
            // Switch to World Tab if not already
            if (Find.MainTabsRoot.OpenTab != MainButtonDefOf.World)
            {
                Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.World);
            }
            
            Messages.Message(LanguageManager.Get("EnterSpawningModeMessage", factionDef.LabelCap), MessageTypeDefOf.NeutralEvent, false);
        }

        public static Faction SpawnFactionInstance(FactionDef factionDef, string factionName = null)
        {
            if (factionDef == null) return null;
            if (Current.Game == null || Find.FactionManager == null)
            {
                Messages.Message(LanguageManager.Get("OnlyAvailableInGame"), MessageTypeDefOf.RejectInput, false);
                return null;
            }

            Faction faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(factionDef));
            string resolvedName = !factionName.NullOrEmpty()
                ? factionName
                : NameGenerator.GenerateName(faction.def.factionNameMaker,
                    from fac in Find.FactionManager.AllFactionsVisible
                    select fac.Name, false, null);
            faction.Name = resolvedName;
            
            Find.FactionManager.Add(faction);
            
            // Generate relations
            foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
            {
                if (other != faction)
                {
                    faction.TryMakeInitialRelationsWith(other);
                }
            }

            // Apply PlayerRelationOverride if set for this faction def
            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(factionDef.defName);
            if (factionData.PlayerRelationOverride.HasValue)
            {
                FactionDefManager.ApplyFactionChanges(factionDef, factionData);
            }

            FactionListPanel.MarkDirty();
            Messages.Message(LanguageManager.Get("FactionInstanceCreated", faction.Name), MessageTypeDefOf.PositiveEvent, false);
            return faction;
        }

        public static Settlement SpawnSettlement(Faction faction, int tile)
        {
            if (faction == null || tile < 0) return null;

            Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
            settlement.SetFaction(faction);
            settlement.Tile = tile;
            settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement, faction.def.settlementNameMaker);
            
            Find.WorldObjects.Add(settlement);
            
            return settlement;
        }

        public static void SpawnRandomSettlements(Faction faction, int count, int minDistanceToPlayer)
        {
            if (faction == null) return;
            
            if (TryFindSettlementTiles(count, minDistanceToPlayer, faction, out List<int> tiles))
            {
                foreach (int tile in tiles)
                {
                    SpawnSettlement(faction, tile);
                }
                Messages.Message(LanguageManager.Get("SettlementsCreated", tiles.Count), MessageTypeDefOf.PositiveEvent, false);
                
                if (tiles.Count < count)
                {
                    Log.Warning($"[FactionGearCustomizer] Only found {tiles.Count} valid tiles for {count} requested settlements (minDistance: {minDistanceToPlayer})");
                }
            }
            else
            {
                Messages.Message(LanguageManager.Get("NoValidTileFound"), MessageTypeDefOf.RejectInput, false);
            }
        }

        public static bool TryFindSettlementTiles(int count, int minDistanceToPlayer, Faction faction, out List<int> tiles)
        {
            tiles = new List<int>();
            
            for (int i = 0; i < count; i++)
            {
                int tile = -1;
                // Retry loop to find a valid tile that meets distance requirements
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    // Pass null as validator to avoid compilation issues with Predicate<PlanetTile> vs Predicate<int>
                    int candidate = TileFinder.RandomSettlementTileFor(faction, true, null);
                    
                    if (candidate != -1)
                    {
                        // Custom validation: Check distance to player settlements
                        bool valid = true;
                        if (minDistanceToPlayer > 0 && Find.WorldObjects != null)
                        {
                            foreach (Settlement s in Find.WorldObjects.Settlements)
                            {
                                if (s.Faction != null && s.Faction.IsPlayer)
                                {
                                    if (Find.WorldGrid.ApproxDistanceInTiles(candidate, s.Tile) < minDistanceToPlayer)
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                            }
                        }

                        if (valid)
                        {
                            tile = candidate;
                            break;
                        }
                    }
                }

                if (tile != -1)
                {
                    tiles.Add(tile);
                }
            }
            
            return tiles.Count > 0;
        }

        [Obsolete("Use FactionSpawnWindow instead.")]
        public static void BeginTargetingForSettlement(Faction faction)
        {
            if (faction == null) return;
            
            // Switch to World Tab if not already
            if (Find.MainTabsRoot.OpenTab != MainButtonDefOf.World)
            {
                Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.World);
            }

            Find.WindowStack.Add(new FactionSpawnWindow(faction));
        }
    }
}
