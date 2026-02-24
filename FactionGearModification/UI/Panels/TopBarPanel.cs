using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer;

namespace FactionGearCustomizer.UI.Panels
{
    public static class TopBarPanel
    {
        private static Texture2D logo;
        private static bool logoLoaded;

        public static void Draw(Rect inRect)
        {
            if (!logoLoaded)
            {
                logo = ContentFinder<Texture2D>.Get("UI/Logo", false);
                logoLoaded = true;
            }

            // Define areas
            // We'll use a single row layout.
            // Left: Logo, Title
            // Center/Right: Actions
            
            float iconSize = 28f;
            float gap = 10f;
            
            // --- Logo & Title ---
            Rect logoRect = new Rect(inRect.x, inRect.y + (inRect.height - iconSize) / 2f, iconSize, iconSize);
            if (logo != null)
            {
                var oldColor = GUI.color;
                GUI.color = Color.white;
                Widgets.DrawTextureFitted(logoRect, logo, 1f);
                GUI.color = oldColor;
            }

            // Title
            // Removed title to avoid duplication with window title
            /*
            string title = LanguageManager.Get("FactionGearCustomizer");
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Vector2 titleSize = Text.CalcSize(title);
            Rect titleRect = new Rect(logoRect.xMax + gap, inRect.y, titleSize.x, inRect.height);
            Widgets.Label(titleRect, title);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            */

            // --- Right Side Actions ---
            // Combined:
            // [Logo] [Title] | [FactionEdit] [Github] [Version] | [Spacer] | [Save] [Presets] [Utility] [Reset]
            
            float currentX = logoRect.xMax + gap * 2;
            
            // Draw Left-Side Buttons (FactionEdit, Github, Version)
            currentX = DrawInfoButtons(currentX, inRect.y, inRect.height);
            
            // Draw Right-Side Buttons (Save, Presets, etc)
            // We'll draw these aligned to the right edge
            DrawActionButtons(inRect, currentX);
        }

        private static float DrawInfoButtons(float startX, float y, float height)
        {
            float currentX = startX;
            float gap = 8f; // Reduced gap
            float buttonHeight = 24f;
            float buttonY = y + (height - buttonHeight) / 2f;

            // Faction Edit Button
            string factionEditLabel = LanguageManager.Get("FactionEdit");
            Vector2 factionEditSize = Text.CalcSize(factionEditLabel);
            Rect factionEditRect = new Rect(currentX, buttonY, factionEditSize.x + 10f, buttonHeight); // Reduced padding
            
            bool inGame = Current.Game != null;
            if (!inGame) GUI.color = Color.gray;

            if (Widgets.ButtonText(factionEditRect, factionEditLabel, true, false, inGame))
            {
                if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
                {
                    FactionDef faction = DefDatabase<FactionDef>.GetNamedSilentFail(EditorSession.SelectedFactionDefName);
                    PawnKindDef kind = !string.IsNullOrEmpty(EditorSession.SelectedKindDefName)
                        ? DefDatabase<PawnKindDef>.GetNamedSilentFail(EditorSession.SelectedKindDefName)
                        : null;

                    if (faction != null)
                    {
                        Find.WindowStack.Add(new FactionEditWindow(faction, kind));
                    }
                }
            }
            
            if (!inGame)
            {
                TooltipHandler.TipRegion(factionEditRect, LanguageManager.Get("OnlyAvailableInGame"));
                GUI.color = Color.white;
            }
            else
            {
                TooltipHandler.TipRegion(factionEditRect, LanguageManager.Get("FactionEditTooltip"));
            }
            
            currentX = factionEditRect.xMax + gap;

            // Github
            string githubLink = "GitHub";
            Vector2 githubSize = Text.CalcSize(githubLink);
            Rect githubRect = new Rect(currentX, buttonY, githubSize.x + 10f, buttonHeight);
            
            GUI.color = Color.cyan;
            if (Widgets.ButtonText(githubRect, githubLink, true, false, true))
            {
                Application.OpenURL("https://github.com/yancy22737-sudo/FactionGearCustomizer");
            }
            TooltipHandler.TipRegion(githubRect, LanguageManager.Get("GithubLinkTooltip"));
            GUI.color = Color.white;
            
            currentX = githubRect.xMax + gap;

            // Version
            // Simplified version label to save space
            string versionLabel = ModVersion.Current;
            Vector2 verSize = Text.CalcSize(versionLabel);
            Rect verRect = new Rect(currentX, buttonY, verSize.x + 10f, buttonHeight);

            if (Mouse.IsOver(verRect))
            {
                Widgets.DrawHighlight(verRect);
            }
            if (Widgets.ButtonText(verRect, versionLabel, true, false, true))
            {
                Find.WindowStack.Add(new VersionLogWindow());
            }
            TooltipHandler.TipRegion(verRect, LanguageManager.Get("VersionLogTooltip"));

            currentX = verRect.xMax + gap;

            // Wiki Button
            string wikiLabel = "?";
            Vector2 wikiSize = Text.CalcSize(wikiLabel);
            Rect wikiRect = new Rect(currentX, buttonY, wikiSize.x + 14f, buttonHeight);

            GUI.color = Color.yellow;
            if (Widgets.ButtonText(wikiRect, wikiLabel, true, false, true))
            {
                Find.WindowStack.Add(new WikiWindow());
            }
            TooltipHandler.TipRegion(wikiRect, LanguageManager.Get("WikiTooltip"));
            GUI.color = Color.white;

            return wikiRect.xMax + gap;
        }

        private static void DrawActionButtons(Rect containerRect, float minX)
        {
            // We use a WidgetRow but we want it right-aligned. 
            // Since we don't know exact width, we can estimate or draw from right to left manually.
            // Or we can just start at a safe distance if we assume screen is wide enough.
            // But to be safe and clean, let's use a fixed width for the right area or calculate it.
            
            // Let's use a WidgetRow starting at roughly the center or enough space for the buttons.
            // Actually, let's just place them manually from right to left.
            
            float buttonHeight = 24f;
            float buttonY = containerRect.y + (containerRect.height - buttonHeight) / 2f;
            float gap = 8f; // Reduced gap
            float currentX = containerRect.xMax - 5f; // Reduced padding from right edge

            // 1. Reset Menu (Rightmost)
            // Simplified label: "Reset v"
            string resetLabel = $"{LanguageManager.Get("Reset")} v";
            float resetWidth = Text.CalcSize(resetLabel).x + 12f;
            Rect resetRect = new Rect(currentX - resetWidth, buttonY, resetWidth, buttonHeight);
            if (Widgets.ButtonText(resetRect, resetLabel))
            {
                List<FloatMenuOption> options = BuildResetMenuOptions();
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(resetRect, LanguageManager.Get("ResetMenuTooltip"));
            currentX = resetRect.x - gap;

            // 2. Force Ignore
            bool forceIgnore = FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;
            // Simplified label: "Force: ON"
            string forceLabel = $"{LanguageManager.Get("ForceIgnore")}: {(forceIgnore ? "ON" : "OFF")}";
            // If text is too long, we can shorten it in translation or logic here, but let's keep it for now as it's critical info.
            // Maybe just "Force: ON"?
            // Let's try to detect if we are running out of space.
            
            float forceWidth = Text.CalcSize(forceLabel).x + 12f;
            Rect forceRect = new Rect(currentX - forceWidth, buttonY, forceWidth, buttonHeight);
            if (Widgets.ButtonText(forceRect, forceLabel))
            {
                FactionGearCustomizerMod.Settings.forceIgnoreRestrictions = !FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;
                FactionGearEditor.MarkDirty();
            }
            TooltipHandler.TipRegion(forceRect, LanguageManager.Get("ForceIgnoreTooltip"));
            currentX = forceRect.x - gap;

            // 2.5 Options
            string optionsLabel = LanguageManager.Get("Options");
            float optionsWidth = Text.CalcSize(optionsLabel).x + 12f;
            Rect optionsRect = new Rect(currentX - optionsWidth, buttonY, optionsWidth, buttonHeight);
            
            if (Widgets.ButtonText(optionsRect, optionsLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                
                // Show Hidden Factions
                string showHiddenLabel = $"{(FactionGearCustomizerMod.Settings.ShowHiddenFactions ? "✔ " : "")}{LanguageManager.Get("ShowHiddenFactions")}";
                options.Add(new FloatMenuOption(showHiddenLabel, () => {
                    FactionGearCustomizerMod.Settings.ShowHiddenFactions = !FactionGearCustomizerMod.Settings.ShowHiddenFactions;
                    FactionGearCustomizerMod.Settings.Write();
                    FactionGearEditor.MarkDirty();
                    FactionListPanel.MarkDirty();
                }));

                string showInMainTabLabel = $"{(FactionGearCustomizerMod.Settings.ShowInMainTab ? "✔ " : "")}{LanguageManager.Get("ShowInMainTab")}";
                options.Add(new FloatMenuOption(showInMainTabLabel, () =>
                {
                    bool enabled = !FactionGearCustomizerMod.Settings.ShowInMainTab;
                    FactionGearCustomizerMod.Settings.ShowInMainTab = enabled;
                    FactionGearCustomizerMod.Settings.Write();

                    if (!enabled)
                    {
                        var mainTabsRoot = Find.MainTabsRoot;
                        if (mainTabsRoot != null)
                        {
                            var myTab = DefDatabase<MainButtonDef>.GetNamedSilentFail("FactionGear_MainButton");
                            if (myTab != null && mainTabsRoot.OpenTab == myTab)
                            {
                                mainTabsRoot.SetCurrentTab(MainButtonDefOf.World);
                            }
                        }
                    }
                }));
                
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(optionsRect, LanguageManager.Get("AdvancedOptionsTooltip"));
            currentX = optionsRect.x - gap;

            // 3. Manage Presets
            string presetsLabel = LanguageManager.Get("Presets");
            float presetsWidth = Text.CalcSize(presetsLabel).x + 12f;
            Rect presetsRect = new Rect(currentX - presetsWidth, buttonY, presetsWidth, buttonHeight);
            
            GUI.color = Color.cyan;
            if (Widgets.ButtonText(presetsRect, presetsLabel))
            {
                Find.WindowStack.Add(new PresetManagerWindow());
            }
            TooltipHandler.TipRegion(presetsRect, LanguageManager.Get("ManagePresetsTooltip"));
            GUI.color = Color.white;
            currentX = presetsRect.x - gap;

            // 4. Save Button
            string saveLabel = LanguageManager.Get("Save");
            float saveWidth = Text.CalcSize(saveLabel).x + 12f;
            Rect saveRect = new Rect(currentX - saveWidth, buttonY, saveWidth, buttonHeight);
            
            GUI.color = FactionGearEditor.IsDirty ? Color.green : Color.white;
            if (Widgets.ButtonText(saveRect, saveLabel))
            {
                HandleSaveButton();
            }
            
            string saveTooltip = LanguageManager.Get("SaveChangesToGame");
            saveTooltip += $"\n{LanguageManager.Get("SaveImmediateEffectTooltip")}";
            if (!string.IsNullOrEmpty(FactionGearCustomizerMod.Settings.currentPresetName))
            {
                saveTooltip += $"\n{LanguageManager.Get("PresetUpdated", FactionGearCustomizerMod.Settings.currentPresetName)}";
            }
            TooltipHandler.TipRegion(saveRect, saveTooltip);
            GUI.color = Color.white;
            
            currentX = saveRect.x - gap;

            // 5. Refresh Button (Left of Save)
            string refreshLabel = LanguageManager.Get("Refresh");
            float refreshWidth = Text.CalcSize(refreshLabel).x + 12f;
            Rect refreshRect = new Rect(currentX - refreshWidth, buttonY, refreshWidth, buttonHeight);
            
            GUI.color = Color.cyan;
            if (Widgets.ButtonText(refreshRect, refreshLabel))
            {
                FactionGearEditor.RefreshAllCaches();
                Messages.Message(LanguageManager.Get("CachesRefreshed"), MessageTypeDefOf.NeutralEvent, false);
            }
            TooltipHandler.TipRegion(refreshRect, LanguageManager.Get("RefreshTooltip"));
            GUI.color = Color.white;
            currentX = refreshRect.x - gap;

            // 6. Active Preset Label (Left of Refresh)
            string currentPresetName = FactionGearCustomizerMod.Settings.currentPresetName;
            string labelText;
            if (string.IsNullOrEmpty(currentPresetName))
            {
                labelText = LanguageManager.Get("NoPreset");
                GUI.color = Color.gray;
            }
            else
            {
                labelText = LanguageManager.Get("ActivePreset", currentPresetName);
                GUI.color = new Color(0.6f, 1f, 0.6f);
            }
            
            float labelWidth = Text.CalcSize(labelText).x + 10f;
            float availableWidth = currentX - minX - gap;

            if (availableWidth > 50f)
            {
                Rect labelRect = new Rect(minX, buttonY, availableWidth, buttonHeight);
                
                // 使用省略号截断过长的文本
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, labelText.Truncate(labelRect.width));
                Text.Anchor = TextAnchor.UpperLeft;
                
                currentX = minX - gap;

                if (Mouse.IsOver(labelRect) && !string.IsNullOrEmpty(currentPresetName))
                {
                    TooltipHandler.TipRegion(labelRect, currentPresetName);
                }
            }
            GUI.color = Color.white;
        }

        private static void HandleSaveButton()
        {
            if (string.IsNullOrEmpty(FactionGearCustomizerMod.Settings.currentPresetName))
            {
                // No preset active - open preset manager and show message
                Find.WindowStack.Add(new PresetManagerWindow());
                Messages.Message(LanguageManager.Get("NoPresetCannotSave"), MessageTypeDefOf.NegativeEvent);
            }
            else
            {
                FactionGearEditor.SaveChanges();
                Messages.Message(LanguageManager.Get("SettingsSaved"), MessageTypeDefOf.PositiveEvent);
            }
        }

        private static List<FloatMenuOption> BuildResetMenuOptions()
        {
            return new List<FloatMenuOption>
            {
                new FloatMenuOption(LanguageManager.Get("ResetFilters"), EditorSession.ResetFilters),
                new FloatMenuOption(LanguageManager.Get("ResetCurrentKind"), () => {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        var kindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                        kindData.ResetToDefault();
                        FactionGearManager.LoadKindDefGear(DefDatabase<PawnKindDef>.GetNamedSilentFail(EditorSession.SelectedKindDefName), kindData);
                        FactionGearEditor.MarkDirty();
                    }
                }),
                new FloatMenuOption(LanguageManager.Get("LoadDefaultFaction"), () => {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
                    {
                        FactionGearManager.LoadDefaultPresets(EditorSession.SelectedFactionDefName);
                        FactionGearEditor.MarkDirty();
                    }
                }),
                new FloatMenuOption(LanguageManager.Get("ResetCurrentFaction"), () => {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        factionData.ResetToDefault();
                        FactionGearEditor.MarkDirty();
                        Log.Message($"[FactionGearCustomizer] Reset faction settings to default: {EditorSession.SelectedFactionDefName}");
                    }
                }),
                new FloatMenuOption(LanguageManager.Get("ResetEVERYTHING"), () => {
                    FactionGearCustomizerMod.Settings.ResetToDefault();
                    FactionGearEditor.RefreshAllCaches();
                    FactionGearEditor.MarkDirty();
                }, MenuOptionPriority.High, null, null, 0f, null, null, true, 0)
            };
        }
    }
}
