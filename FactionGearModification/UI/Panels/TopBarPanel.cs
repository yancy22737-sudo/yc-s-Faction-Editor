using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer;
using FactionGearCustomizer.UI.Panels;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer.Utils;

namespace FactionGearCustomizer.UI.Panels
{
    [StaticConstructorOnStartup]
    public static class TopBarPanel
    {
        private static Texture2D logo;

        static TopBarPanel()
        {
            logo = ContentFinder<Texture2D>.Get("UI/Logo", false);
        }

        public static void Draw(Rect inRect)
        {

            float iconSize = 28f;
            float gap = 10f;
            
            Rect logoRect = new Rect(inRect.x, inRect.y + (inRect.height - iconSize) / 2f, iconSize, iconSize);
            if (logo != null)
            {
                var oldColor = GUI.color;
                GUI.color = Color.white;
                Widgets.DrawTextureFitted(logoRect, logo, 1f);
                GUI.color = oldColor;
            }

            float currentX = logoRect.xMax + gap * 2;
            
            currentX = DrawInfoButtons(currentX, inRect.y, inRect.height);
            
            DrawActionButtons(inRect, currentX);
        }

        private static float DrawInfoButtons(float startX, float y, float height)
        {
            float currentX = startX;
            float gap = 8f;
            float buttonHeight = 24f;
            float buttonY = y + (height - buttonHeight) / 2f;

            string factionEditLabel = LanguageManager.Get("FactionEdit");
            Vector2 factionEditSize = Text.CalcSize(factionEditLabel);
            Rect factionEditRect = new Rect(currentX, buttonY, factionEditSize.x + 10f, buttonHeight);

            bool inGame = Current.Game != null;
            bool canEditFaction = inGame && !Dialogs.Dialog_FactionEditorLite.IsOpenedFromWorldCreation;
            if (!canEditFaction) GUI.color = Color.gray;

            if (Widgets.ButtonText(factionEditRect, factionEditLabel, true, false, canEditFaction))
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
            }
            else if (Dialogs.Dialog_FactionEditorLite.IsOpenedFromWorldCreation)
            {
                TooltipHandler.TipRegion(factionEditRect, LanguageManager.Get("FactionEditDisabledInLiteMode"));
            }
            else
            {
                TooltipHandler.TipRegion(factionEditRect, LanguageManager.Get("FactionEditTooltip"));
            }

            GUI.color = Color.white;
            
            currentX = factionEditRect.xMax + gap;

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
            float buttonHeight = 24f;
            float buttonY = containerRect.y + (containerRect.height - buttonHeight) / 2f;
            float gap = 8f;
            float currentX = containerRect.xMax - 5f;

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

            string optionsLabel = LanguageManager.Get("Options");
            float optionsWidth = Text.CalcSize(optionsLabel).x + 12f;
            Rect optionsRect = new Rect(currentX - optionsWidth, buttonY, optionsWidth, buttonHeight);
            
            if (Widgets.ButtonText(optionsRect, optionsLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                
                bool forceIgnore = FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;
                string forceIgnoreLabel = $"{(forceIgnore ? "✔ " : "")}{LanguageManager.Get("ForceIgnore")}";
                options.Add(new FloatMenuOption(forceIgnoreLabel, () => {
                    FactionGearCustomizerMod.Settings.forceIgnoreRestrictions = !FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;
                    FactionGearCustomizerMod.Settings.Write();
                    FactionGearEditor.MarkDirty();
                }));
                
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

                string debugLogLabel = $"{(FactionGearCustomizerMod.Settings.enableDebugLog ? "✔ " : "")}{LanguageManager.Get("EnableDebugLog")}";
                options.Add(new FloatMenuOption(debugLogLabel, () => {
                    FactionGearCustomizerMod.Settings.enableDebugLog = !FactionGearCustomizerMod.Settings.enableDebugLog;
                    FactionGearCustomizerMod.Settings.Write();
                    FactionGearCustomizer.Utils.LogUtils.Info(debugLogLabel);
                }));
                
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(optionsRect, LanguageManager.Get("AdvancedOptionsTooltip"));
            currentX = optionsRect.x - gap;

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
                        // 【关键修复】首先执行完整的深度清理
                        FactionGearCustomizerMod.PerformDeepCleanup("ResetCurrentKind");
                        
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
                        // 【关键修复】首先执行完整的深度清理
                        FactionGearCustomizerMod.PerformDeepCleanup("LoadDefaultFaction");
                        
                        FactionGearManager.LoadDefaultPresets(EditorSession.SelectedFactionDefName);
                        FactionGearEditor.MarkDirty();
                    }
                }),
                new FloatMenuOption(LanguageManager.Get("ResetCurrentFaction"), () => {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
                    {
                        // 【关键修复】首先执行完整的深度清理
                        FactionGearCustomizerMod.PerformDeepCleanup("ResetCurrentFaction");
                        
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        factionData.ResetToDefault();
                        FactionGearEditor.MarkDirty();
                        LogUtils.DebugLog($"Reset faction settings to default: {EditorSession.SelectedFactionDefName}");
                    }
                }),
                new FloatMenuOption(LanguageManager.Get("ResetEVERYTHING"), () => {
                    // 【深度清理】执行完整的深度清理流程
                    FactionGearCustomizerMod.PerformDeepCleanup("ResetEVERYTHING");
                    
                    // 重置设置
                    FactionGearCustomizerMod.Settings.ResetToDefault();
                    
                    FactionGearEditor.MarkDirty();
                }, MenuOptionPriority.High, null, null, 0f, null, null, true, 0)
            };
        }
    }
}
