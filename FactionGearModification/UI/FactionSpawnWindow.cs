using System;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using FactionGearCustomizer.Managers;

namespace FactionGearCustomizer.UI
{
    public class FactionSpawnWindow : Window
    {
        private readonly Faction faction;
        private Texture2D mouseAttachment;

        public override Vector2 InitialSize => new Vector2(350f, 180f);

        protected override float Margin => 10f;

        public FactionSpawnWindow(Faction faction)
        {
            this.faction = faction;
            this.draggable = true;
            this.resizeable = false;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = false;
            this.forcePause = false;
            this.preventCameraMotion = false;
            
            // Try to load texture similar to WorldEdit
            this.mouseAttachment = ContentFinder<Texture2D>.Get("World/WorldObjects/Expanding/Town", false); 
        }

        private bool wasMouseOverWindowLastFrame = false;

        public override void PostOpen()
        {
            base.PostOpen();

            // Safety checks before switching tabs
            if (Find.MainTabsRoot != null && MainButtonDefOf.World != null)
            {
                try
                {
                    if (Find.MainTabsRoot.OpenTab != MainButtonDefOf.World)
                    {
                        Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.World);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Warning($"[FactionGearCustomizer] Failed to switch to World tab: {ex.Message}");
                }
            }
        }

        public override void PreClose()
        {
            base.PreClose();
            // Reset the mouse over flag to prevent issues when reopening
            wasMouseOverWindowLastFrame = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            
            string factionName = faction?.Name ?? "Unknown";
            string title = LanguageManager.Get("SpawnSettlementFor", factionName);
            Widgets.Label(new Rect(0, 0, inRect.width, 30f), title);
            
            string instructions = LanguageManager.Get("ManualSelectionDesc") + "\n" + LanguageManager.Get("SettlementPermanentWarning");
            float instructionsHeight = Text.CalcHeight(instructions, inRect.width);
            Widgets.Label(new Rect(0, 35f, inRect.width, instructionsHeight), instructions);

            if (Widgets.ButtonText(new Rect(0, inRect.height - 30f, inRect.width, 30f), LanguageManager.Get("Close")))
            {
                Close();
            }
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();

            // Safety checks
            if (Find.WindowStack == null || Current.Game == null || Find.World == null)
            {
                return;
            }

            // Check if mouse is currently over any window
            bool isMouseOverWindow = Find.WindowStack.GetWindowAt(Verse.UI.MousePositionOnUIInverted) != null;

            // If mouse was over window last frame but not now, it means we just exited a window
            // (e.g., finished dragging). Skip this click to avoid accidental settlement spawn.
            if (wasMouseOverWindowLastFrame && !isMouseOverWindow)
            {
                wasMouseOverWindowLastFrame = isMouseOverWindow;
                return;
            }

            wasMouseOverWindowLastFrame = isMouseOverWindow;

            // If mouse is over ANY window, do not process world clicks
            if (isMouseOverWindow)
            {
                return;
            }

            // Handle Left Click to spawn
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                int tile = GenWorld.MouseTile();
                if (tile >= 0)
                {
                    TrySpawnSettlement(tile);
                }
            }
        }

        public override void WindowOnGUI()
        {
            base.WindowOnGUI();

            // Safety checks
            if (Find.WindowStack == null || mouseAttachment == null)
            {
                return;
            }

            // Draw mouse attachment if not over any window
            if (Find.WindowStack.GetWindowAt(Verse.UI.MousePositionOnUIInverted) == null)
            {
                GenUI.DrawMouseAttachment(mouseAttachment);
            }
        }

        private void TrySpawnSettlement(int tile)
        {
            if (faction == null) return;

            // Check if tile is valid
            if (!TileFinder.IsValidTileForNewSettlement(tile))
            {
                Messages.Message(LanguageManager.Get("CannotSettleHere"), MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Check if there is already a settlement
            if (Find.WorldObjects.AnySettlementAt(tile))
            {
                Messages.Message(LanguageManager.Get("CannotSettleHere"), MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Spawn
            Settlement settlement = FactionSpawnManager.SpawnSettlement(faction, tile);
            if (settlement != null)
            {
                // Play sound
                SoundDefOf.Click.PlayOneShotOnCamera();
                
                // Show message
                Messages.Message(LanguageManager.Get("SettlementCreated", settlement.Name), MessageTypeDefOf.PositiveEvent, false);
            }
        }
    }
}
