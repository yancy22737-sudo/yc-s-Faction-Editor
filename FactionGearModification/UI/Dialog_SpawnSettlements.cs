using System;
using UnityEngine;
using Verse;
using RimWorld;
using FactionGearCustomizer.Managers;

namespace FactionGearCustomizer.UI
{
    public class Dialog_SpawnSettlements : Window
    {
        private readonly Faction faction;
        private int settlementCount = 5;
        private int minDistance = 20;

        public override Vector2 InitialSize => new Vector2(400f, 450f);

        public Dialog_SpawnSettlements(Faction faction)
        {
            this.faction = faction;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;
            float width = inRect.width;

            // Header
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(0, y, width, 30f), LanguageManager.Get("SpawnSettlements"));
            Text.Anchor = TextAnchor.UpperLeft;
            y += 35f;

            // Faction Info
            Text.Font = GameFont.Small;
            string factionLabel = faction != null ? faction.Name : LanguageManager.Get("UnknownFaction");
            Widgets.Label(new Rect(0, y, width, 24f), $"{LanguageManager.Get("TargetFaction")}: {factionLabel}");
            y += 30f;

            Widgets.DrawLineHorizontal(0, y, width);
            y += 15f;

            // Section 1: Random Generation
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, y, width, 30f), LanguageManager.Get("RandomGeneration"));
            Text.Font = GameFont.Small;
            y += 35f;

            // Count Slider
            Widgets.Label(new Rect(0, y, width, 24f), $"{LanguageManager.Get("SettlementCount")}: {settlementCount}");
            y += 25f;
            settlementCount = (int)Widgets.HorizontalSlider(new Rect(0, y, width - 20f, 20f), settlementCount, 1f, 50f, true);
            y += 30f;

            // Distance Slider
            Widgets.Label(new Rect(0, y, width, 24f), $"{LanguageManager.Get("MinDistanceToPlayer")}: {minDistance}");
            y += 25f;
            minDistance = (int)Widgets.HorizontalSlider(new Rect(0, y, width - 20f, 20f), minDistance, 0f, 100f, true);
            y += 35f;

            if (Widgets.ButtonText(new Rect(0, y, width, 40f), LanguageManager.Get("GenerateRandomly")))
            {
                FactionSpawnManager.SpawnRandomSettlements(faction, settlementCount, minDistance);
                Close();
            }
            y += 50f;

            Widgets.DrawLineHorizontal(0, y, width);
            y += 15f;

            // Section 2: Manual Selection
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, y, width, 30f), LanguageManager.Get("ManualSelection"));
            Text.Font = GameFont.Small;
            y += 35f;

            Widgets.Label(new Rect(0, y, width, 40f), LanguageManager.Get("ManualSelectionDesc"));
            y += 45f;

            if (Widgets.ButtonText(new Rect(0, y, width, 40f), LanguageManager.Get("SelectOnWorldMap")))
            {
                Faction factionToSpawn = faction;
                Close();

                // Delay opening to avoid window stack conflicts
                Verse.LongEventHandler.ExecuteWhenFinished(() =>
                {
                    if (Current.Game != null && Find.World != null && factionToSpawn != null)
                    {
                        // Close other windows from this mod before opening the spawn window
                        CloseAllWindowsFromThisMod();
                        Find.WindowStack.Add(new FactionSpawnWindow(factionToSpawn));
                    }
                });
            }
        }

        private void CloseAllWindowsFromThisMod()
        {
            if (Find.WindowStack?.Windows == null) return;

            var windows = Find.WindowStack.Windows;
            var asm = GetType().Assembly;
            for (int i = windows.Count - 1; i >= 0; i--)
            {
                var w = windows[i];
                if (w == null) continue;
                if (w.GetType().Assembly == asm)
                {
                    Find.WindowStack.TryRemove(w, false);
                }
            }
        }
    }
}
