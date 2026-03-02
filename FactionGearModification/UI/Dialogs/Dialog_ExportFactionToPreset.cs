using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_ExportFactionToPreset : Window
    {
        public override Vector2 InitialSize => new Vector2(750f, 600f);

        private readonly List<FactionGearData> sourceFactions;
        private readonly List<FactionGearPreset> availablePresets;
        private FactionGearPreset selectedPreset;
        private readonly FactionGearPreset sourcePreset;
        private bool createNewPreset = false;
        private string newPresetName = "";
        private string newPresetDescription = "";
        private readonly Dictionary<string, bool> factionSelectionStates = new Dictionary<string, bool>();
        private PresetFactionExporter.ExportConflictResolution conflictResolution = PresetFactionExporter.ExportConflictResolution.Skip;

        private Vector2 factionListScrollPos;
        private Vector2 presetListScrollPos;

        public Dialog_ExportFactionToPreset(FactionGearPreset sourcePreset = null)
        {
            this.sourcePreset = sourcePreset;

            if (sourcePreset != null && sourcePreset.factionGearData != null)
            {
                sourceFactions = sourcePreset.factionGearData.ToList();
            }
            else
            {
                var gameComponent = FactionGearGameComponent.Instance;
                if (gameComponent != null)
                {
                    var activeData = gameComponent.GetActiveFactionGearData();
                    if (activeData != null && activeData.Any())
                    {
                        sourceFactions = activeData.ToList();
                    }
                    else
                    {
                        sourceFactions = FactionGearCustomizerMod.Settings.factionGearData?.ToList() ?? new List<FactionGearData>();
                    }
                }
                else
                {
                    sourceFactions = FactionGearCustomizerMod.Settings.factionGearData?.ToList() ?? new List<FactionGearData>();
                }
            }
            
            availablePresets = FactionGearCustomizerMod.Settings.presets?.ToList() ?? new List<FactionGearPreset>();
            doCloseButton = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;

            foreach (var faction in sourceFactions)
            {
                factionSelectionStates[faction.factionDefName] = false;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 35f);
            Widgets.Label(titleRect, LanguageManager.Get("yFE_ExportFactions_Title"));
            Text.Font = GameFont.Small;

            float splitX = inRect.width * 0.4f;
            Rect leftRect = new Rect(inRect.x, titleRect.yMax + 10f, splitX - 10f, inRect.height - titleRect.yMax - 60f);
            Rect rightRect = new Rect(leftRect.xMax + 10f, leftRect.y, inRect.width - splitX, leftRect.height);

            DrawFactionList(leftRect);
            DrawPresetSelection(rightRect);

            DrawBottomButtons(new Rect(inRect.x, inRect.yMax - 45f, inRect.width, 40f));
        }

        private void DrawFactionList(Rect rect)
        {
            Widgets.DrawBox(rect);
            Rect innerRect = rect.ContractedBy(10f);

            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 60f);
            DrawFactionListHeader(headerRect);

            Rect listRect = new Rect(innerRect.x, headerRect.yMax + 5f, innerRect.width, innerRect.height - headerRect.height - 5f);
            DrawFactionListContent(listRect);
        }

        private void DrawFactionListHeader(Rect rect)
        {
            Rect selectAllRect = new Rect(rect.x, rect.y, 100f, 28f);
            if (Widgets.ButtonText(selectAllRect, LanguageManager.Get("yFE_SelectAll")))
            {
                foreach (var key in factionSelectionStates.Keys.ToList())
                {
                    factionSelectionStates[key] = true;
                }
            }

            Rect deselectAllRect = new Rect(selectAllRect.xMax + 5f, rect.y, 100f, 28f);
            if (Widgets.ButtonText(deselectAllRect, LanguageManager.Get("yFE_DeselectAll")))
            {
                foreach (var key in factionSelectionStates.Keys.ToList())
                {
                    factionSelectionStates[key] = false;
                }
            }

            int factionCount = sourceFactions.Count;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Rect countRect = new Rect(rect.x, selectAllRect.yMax + 5f, rect.width, 20f);
            Widgets.Label(countRect, $"{factionCount} {LanguageManager.Get("yFE_Factions_Label")} available");
            GUI.color = Color.white;
        }

        private void DrawFactionListContent(Rect rect)
        {
            float itemHeight = 40f;
            float spacing = 5f;
            float totalHeight = sourceFactions.Count * (itemHeight + spacing);

            Rect viewRect = new Rect(0, 0, rect.width - 20f, totalHeight);
            Widgets.BeginScrollView(rect, ref factionListScrollPos, viewRect);

            float y = 0;
            foreach (var faction in sourceFactions)
            {
                Rect itemRect = new Rect(0, y, viewRect.width, itemHeight);
                DrawFactionItem(itemRect, faction);
                y += itemHeight + spacing;
            }

            Widgets.EndScrollView();
        }

        private void DrawFactionItem(Rect rect, FactionGearData faction)
        {
            bool isSelected = factionSelectionStates.TryGetValue(faction.factionDefName, out bool sel) && sel;

            if (isSelected)
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            Rect contentRect = rect.ContractedBy(5f);

            Rect checkboxRect = new Rect(contentRect.x, contentRect.y + 5f, 24f, 24f);
            bool newSelected = isSelected;
            Widgets.Checkbox(checkboxRect.x, checkboxRect.y, ref newSelected);
            if (newSelected != isSelected)
            {
                factionSelectionStates[faction.factionDefName] = newSelected;
            }

            var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(faction.factionDefName);
            string factionLabel = factionDef?.LabelCap ?? faction.factionDefName;
            Rect nameRect = new Rect(checkboxRect.xMax + 5f, contentRect.y, contentRect.width - 60f, 30f);

            if (isSelected)
            {
                GUI.color = new Color(0.6f, 1f, 0.6f);
            }

            Text.Font = GameFont.Small;
            Widgets.Label(nameRect, factionLabel);
            GUI.color = Color.white;

            int kindCount = faction.kindGearData?.Count ?? 0;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Rect countRect = new Rect(nameRect.x, nameRect.yMax - 12f, nameRect.width, 20f);
            Widgets.Label(countRect, $"{kindCount} {LanguageManager.Get("yFE_Kinds_Label")}");
            GUI.color = Color.white;
        }

        private void DrawPresetSelection(Rect rect)
        {
            Widgets.DrawBox(rect);
            Rect innerRect = rect.ContractedBy(10f);

            float y = innerRect.y;

            Rect modeLabelRect = new Rect(innerRect.x, y, innerRect.width, 24f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(modeLabelRect, LanguageManager.Get("yFE_TargetPreset") + ":");
            Text.Anchor = TextAnchor.UpperLeft;
            y += 28f;

            Rect newPresetRect = new Rect(innerRect.x, y, innerRect.width, 28f);
            if (Widgets.RadioButtonLabeled(newPresetRect, LanguageManager.Get("yFE_ExportToNewPreset"), createNewPreset))
            {
                createNewPreset = true;
                selectedPreset = null;
            }
            y += 32f;

            if (createNewPreset)
            {
                Rect nameLabelRect = new Rect(innerRect.x, y, 80f, 24f);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(nameLabelRect, LanguageManager.Get("yFE_PresetName") + ":");
                Text.Anchor = TextAnchor.UpperLeft;

                Rect nameFieldRect = new Rect(nameLabelRect.xMax + 5f, y, innerRect.width - nameLabelRect.width - 5f, 28f);
                newPresetName = Widgets.TextField(nameFieldRect, newPresetName);
                y += 32f;

                Rect descLabelRect = new Rect(innerRect.x, y, 80f, 24f);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(descLabelRect, LanguageManager.Get("yFE_PresetDescription") + ":");
                Text.Anchor = TextAnchor.UpperLeft;

                Rect descFieldRect = new Rect(descLabelRect.xMax + 5f, y, innerRect.width - descLabelRect.width - 5f, 28f);
                newPresetDescription = Widgets.TextField(descFieldRect, newPresetDescription);
                y += 32f;
            }
            else
            {
                Rect existingPresetRect = new Rect(innerRect.x, y, innerRect.width, 28f);
                if (Widgets.RadioButtonLabeled(existingPresetRect, LanguageManager.Get("yFE_ExportToExistingPreset"), !createNewPreset))
                {
                    createNewPreset = false;
                }
                y += 32f;

                Rect listRect = new Rect(innerRect.x, y, innerRect.width, 120f);
                DrawPresetList(listRect);
                y += 124f;
            }

            Rect conflictLabelRect = new Rect(innerRect.x, y, innerRect.width, 24f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(conflictLabelRect, LanguageManager.Get("yFE_ConflictResolution") + ":");
            Text.Anchor = TextAnchor.UpperLeft;
            y += 28f;

            DrawConflictResolutionOptions(new Rect(innerRect.x, y, innerRect.width, 80f));
        }

        private void DrawPresetList(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f));
            Rect innerRect = rect.ContractedBy(5f);

            float itemHeight = 30f;
            float spacing = 3f;
            float totalHeight = availablePresets.Count * (itemHeight + spacing);

            Rect viewRect = new Rect(0, 0, innerRect.width - 20f, Mathf.Max(totalHeight, innerRect.height));
            Widgets.BeginScrollView(innerRect, ref presetListScrollPos, viewRect);

            float y = 0;
            foreach (var preset in availablePresets)
            {
                Rect itemRect = new Rect(0, y, viewRect.width, itemHeight);
                DrawPresetListItem(itemRect, preset);
                y += itemHeight + spacing;
            }

            Widgets.EndScrollView();
        }

        private void DrawPresetListItem(Rect rect, FactionGearPreset preset)
        {
            bool isSelected = selectedPreset == preset;

            if (isSelected)
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                selectedPreset = preset;
                createNewPreset = false;
            }

            Rect contentRect = rect.ContractedBy(5f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(contentRect, preset.name);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawConflictResolutionOptions(Rect rect)
        {
            float y = rect.y;
            float optionHeight = 24f;

            Rect skipRect = new Rect(rect.x, y, rect.width, optionHeight);
            if (Widgets.RadioButtonLabeled(skipRect, LanguageManager.Get("yFE_ExportConflict_Skip"), conflictResolution == PresetFactionExporter.ExportConflictResolution.Skip))
            {
                conflictResolution = PresetFactionExporter.ExportConflictResolution.Skip;
            }
            y += optionHeight + 4f;

            Rect overwriteRect = new Rect(rect.x, y, rect.width, optionHeight);
            if (Widgets.RadioButtonLabeled(overwriteRect, LanguageManager.Get("yFE_ExportConflict_Overwrite"), conflictResolution == PresetFactionExporter.ExportConflictResolution.Overwrite))
            {
                conflictResolution = PresetFactionExporter.ExportConflictResolution.Overwrite;
            }
            y += optionHeight + 4f;

            Rect mergeRect = new Rect(rect.x, y, rect.width, optionHeight);
            if (Widgets.RadioButtonLabeled(mergeRect, LanguageManager.Get("yFE_ExportConflict_Merge"), conflictResolution == PresetFactionExporter.ExportConflictResolution.Merge))
            {
                conflictResolution = PresetFactionExporter.ExportConflictResolution.Merge;
            }
        }

        private void DrawBottomButtons(Rect rect)
        {
            float buttonWidth = 120f;

            Rect cancelRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(cancelRect, LanguageManager.Get("Cancel")))
            {
                Close();
            }

            int selectedCount = factionSelectionStates.Count(kvp => kvp.Value);
            bool canExport = selectedCount > 0 && (createNewPreset || selectedPreset != null);

            Rect exportRect = new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height);
            GUI.color = canExport ? new Color(0.3f, 0.7f, 0.4f) : Color.gray;

            string exportLabel = LanguageManager.Get("yFE_ExportFactions") + $" ({selectedCount})";
            if (Widgets.ButtonText(exportRect, exportLabel) && canExport)
            {
                ExecuteExport();
            }
            GUI.color = Color.white;
        }

        private void ExecuteExport()
        {
            var selectedFactions = factionSelectionStates
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            if (!selectedFactions.Any())
            {
                Messages.Message(LanguageManager.Get("yFE_ExportNoSelection"), MessageTypeDefOf.CautionInput);
                return;
            }

            FactionGearPreset targetPreset;
            if (createNewPreset)
            {
                if (string.IsNullOrEmpty(newPresetName))
                {
                    newPresetName = "New Preset";
                }
                targetPreset = new FactionGearPreset
                {
                    name = newPresetName,
                    description = newPresetDescription
                };
                FactionGearCustomizerMod.Settings.AddPreset(targetPreset);
            }
            else
            {
                targetPreset = selectedPreset;
            }

            var result = PresetFactionExporter.ExportFactionsToPreset(
                sourceFactions,
                targetPreset,
                selectedFactions,
                conflictResolution);

            if (result.Success)
            {
                string message = string.Format(LanguageManager.Get("yFE_ExportSuccess"), result.ExportedFactions.Count, targetPreset.name);
                Messages.Message(message, MessageTypeDefOf.PositiveEvent);

                if (!createNewPreset)
                {
                    FactionGearCustomizerMod.Settings.UpdatePreset(targetPreset);
                    
                    if (FactionGearCustomizerMod.Settings.currentPresetName == targetPreset.name)
                    {
                        var gameComponent = FactionGearGameComponent.Instance;
                        if (gameComponent != null)
                        {
                            gameComponent.ApplyPresetToSave(targetPreset);
                        }
                    }
                }
                Close();
            }
            else
            {
                string error = result.ErrorMessage ?? LanguageManager.Get("yFE_ExportFailed");
                Messages.Message(error, MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}