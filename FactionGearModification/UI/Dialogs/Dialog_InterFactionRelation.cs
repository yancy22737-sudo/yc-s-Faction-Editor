using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer.Utils;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_InterFactionRelation : Window
    {
        private FactionDef sourceFactionDef;
        private List<FactionRelationOverride> existingOverrides;
        private Action<List<FactionRelationOverride>> onConfirm;

        private Dictionary<string, bool> selectionStates = new Dictionary<string, bool>();
        private FactionRelationKind selectedRelationKind = FactionRelationKind.Neutral;
        private Vector2 scrollPosition;
        private string searchText = "";
        private bool showInGameNames = true;

        private List<FactionDef> availableTargets;

        public override Vector2 InitialSize => new Vector2(700f, 550f);

        public Dialog_InterFactionRelation(FactionDef sourceFactionDef, List<FactionRelationOverride> existingOverrides,
            Action<List<FactionRelationOverride>> onConfirm)
        {
            this.sourceFactionDef = sourceFactionDef;
            this.existingOverrides = existingOverrides ?? new List<FactionRelationOverride>();
            this.onConfirm = onConfirm;

            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;

            showInGameNames = EditorSession.UseInGameNames;

            BuildAvailableTargets();
        }

        private void BuildAvailableTargets()
        {
            availableTargets = DefDatabase<FactionDef>.AllDefs
                .Where(f => f.defName != sourceFactionDef.defName
                    && !f.isPlayer
                    && f.humanlikeFaction)
                .OrderBy(f => DefDisplayNameUtility.GetSafeFactionDisplayName(f, "Dialog_InterFactionRelation"))
                .ToList();

            foreach (var def in availableTargets)
            {
                if (!selectionStates.ContainsKey(def.defName))
                {
                    bool alreadyExists = existingOverrides.Any(r => r.targetFactionDefName == def.defName);
                    selectionStates[def.defName] = alreadyExists;
                }
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            float topHeight = 80f;
            float bottomHeight = 80f;

            // Title
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 35f);
            Widgets.Label(titleRect, LanguageManager.Get("SelectTargetFaction"));
            Text.Font = GameFont.Small;

            // Top bar: Search + Toggle + Select All
            Rect topRect = new Rect(inRect.x, inRect.y + 35f, inRect.width, topHeight - 35f);
            DrawTopBar(topRect);

            // Faction list
            Rect listRect = new Rect(inRect.x, inRect.y + topHeight, inRect.width, inRect.height - topHeight - bottomHeight - 10f);
            DrawFactionList(listRect);

            // Bottom bar: Relation kind row + Action buttons row
            Rect bottomRect = new Rect(inRect.x, inRect.y + inRect.height - bottomHeight, inRect.width, bottomHeight);
            DrawBottomBar(bottomRect);
        }

        private void DrawTopBar(Rect rect)
        {
            // Search field
            Rect searchRect = new Rect(rect.x, rect.y, rect.width * 0.45f, 30f);
            searchText = Widgets.TextField(searchRect, searchText);
            if (string.IsNullOrEmpty(searchText))
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(searchRect.ContractedBy(5f), LanguageManager.Get("Search") + "...");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            // Show in-game names toggle
            Rect namesRect = new Rect(searchRect.xMax + 15f, rect.y + 3f, 24f, 24f);
            bool tempShowInGame = showInGameNames;
            Widgets.Checkbox(new Vector2(namesRect.x, namesRect.y), ref tempShowInGame);
            if (tempShowInGame != showInGameNames)
            {
                showInGameNames = tempShowInGame;
            }
            Rect namesLabelRect = new Rect(namesRect.x + 24f, rect.y + 3f, 150f, 24f);
            Widgets.Label(namesLabelRect, LanguageManager.Get("ShowInGameName"));
            TooltipHandler.TipRegion(new Rect(namesRect.x, namesRect.y, namesLabelRect.xMax - namesRect.x, 24f),
                LanguageManager.Get("ShowInGameFactionNamesTooltip"));

            // Select All / Deselect All
            float btnWidth = 110f;
            Rect selectAllRect = new Rect(rect.x + rect.width - btnWidth * 2 - 10f, rect.y, btnWidth, 30f);
            if (Widgets.ButtonText(selectAllRect, LanguageManager.Get("SelectAll")))
            {
                foreach (var def in GetFilteredTargets())
                    selectionStates[def.defName] = true;
            }

            Rect deselectAllRect = new Rect(selectAllRect.xMax + 10f, rect.y, btnWidth, 30f);
            if (Widgets.ButtonText(deselectAllRect, LanguageManager.Get("DeselectAll")))
            {
                foreach (var def in GetFilteredTargets())
                    selectionStates[def.defName] = false;
            }
        }

        private List<FactionDef> GetFilteredTargets()
        {
            if (string.IsNullOrEmpty(searchText))
                return availableTargets;

            string lower = searchText.ToLowerInvariant();
            return availableTargets.Where(f =>
            {
                string displayName = GetDisplayName(f);
                return (displayName ?? "").ToLowerInvariant().Contains(lower)
                    || (f.defName ?? "").ToLowerInvariant().Contains(lower);
            }).ToList();
        }

        private string GetDisplayName(FactionDef def)
        {
            if (showInGameNames && Current.Game != null && Find.FactionManager != null)
            {
                var instance = Find.FactionManager.AllFactions
                    .FirstOrDefault(f => f.def == def && !f.IsPlayer);
                if (instance != null)
                    return DefDisplayNameUtility.GetSafeFactionDisplayName(instance, "Dialog_InterFactionRelation");
            }
            return DefDisplayNameUtility.GetSafeFactionDisplayName(def, "Dialog_InterFactionRelation");
        }

        private void DrawFactionList(Rect rect)
        {
            var targets = GetFilteredTargets();
            float rowHeight = 40f;
            float viewHeight = targets.Count * rowHeight;
            Rect viewRect = new Rect(0, 0, rect.width - 16f, viewHeight);

            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float curY = 0f;
            foreach (var def in targets)
            {
                Rect rowRect = new Rect(0, curY, viewRect.width, rowHeight - 2f);
                if (curY % (rowHeight * 2f) < rowHeight)
                    Widgets.DrawAltRect(rowRect);

                DrawFactionRow(rowRect, def);
                curY += rowHeight;
            }

            if (targets.Count == 0)
            {
                Rect emptyRect = new Rect(0, 0, viewRect.width, 40f);
                GUI.color = Color.gray;
                Widgets.Label(emptyRect, LanguageManager.Get("NoTargetFactionsAvailable"));
                GUI.color = Color.white;
            }

            Widgets.EndScrollView();
        }

        private void DrawFactionRow(Rect rect, FactionDef def)
        {
            bool isSelected = selectionStates.TryGetValue(def.defName, out bool sel) && sel;
            bool alreadyExists = existingOverrides.Any(r => r.targetFactionDefName == def.defName);

            // Checkbox
            Rect checkRect = new Rect(rect.x + 5f, rect.y + 8f, 24f, 24f);
            Widgets.Checkbox(new Vector2(checkRect.x, checkRect.y), ref isSelected);
            selectionStates[def.defName] = isSelected;

            // Faction icon
            Rect iconRect = new Rect(checkRect.xMax + 5f, rect.y + 4f, 32f, 32f);
            Texture2D icon = def.FactionIcon;
            if (icon != null)
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            Widgets.DrawBox(iconRect);

            // Faction name
            string displayName = GetDisplayName(def);
            Rect nameRect = new Rect(iconRect.xMax + 8f, rect.y + 2f, rect.width * 0.4f, 20f);
            if (alreadyExists)
                GUI.color = new Color(0.7f, 0.7f, 0.3f); // yellow-ish for existing
            else if (isSelected)
                GUI.color = Color.green;
            Widgets.Label(nameRect, displayName);
            GUI.color = Color.white;

            // Def name (small, gray)
            Rect defNameRect = new Rect(iconRect.xMax + 8f, rect.y + 22f, rect.width * 0.4f, 16f);
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            Widgets.Label(defNameRect, def.defName);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // Existing override indicator
            if (alreadyExists)
            {
                var existing = existingOverrides.First(r => r.targetFactionDefName == def.defName);
                Rect existingRect = new Rect(nameRect.xMax + 10f, rect.y + 8f, 100f, 24f);
                GUI.color = existing.relationKind == FactionRelationKind.Ally ? Color.green
                    : existing.relationKind == FactionRelationKind.Hostile ? Color.red
                    : Color.cyan;
                Widgets.Label(existingRect, $"({LanguageManager.Get(GetRelationKey(existing.relationKind))})");
                GUI.color = Color.white;
            }

            // Tooltip with detailed info
            TooltipHandler.TipRegion(rect, BuildTooltip(def));
        }

        private string BuildTooltip(FactionDef def)
        {
            string desc = def.description ?? "";
            if (desc.Length > 200)
                desc = desc.Substring(0, 200) + "...";

            string permanentEnemyInfo = "";
            if (def.permanentEnemy)
                permanentEnemyInfo = $"\n{LanguageManager.Get("RelationPermanentEnemy")}";

            string existingInfo = "";
            var existing = existingOverrides.FirstOrDefault(r => r.targetFactionDefName == def.defName);
            if (existing != null)
                existingInfo = $"\n{LanguageManager.Get("CurrentRelation")}: {LanguageManager.Get(GetRelationKey(existing.relationKind))}";

            return $"{def.defName}\n{desc}{permanentEnemyInfo}{existingInfo}";
        }

        private void DrawBottomBar(Rect rect)
        {
            Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width);

            // Row 1: Relation kind selector (left-aligned)
            float btnWidth = 110f;
            Rect allyRect = new Rect(rect.x, rect.y + 3f, btnWidth, 30f);
            bool isAlly = selectedRelationKind == FactionRelationKind.Ally;
            GUI.color = isAlly ? Color.green : Color.gray;
            if (Widgets.ButtonText(allyRect, LanguageManager.Get("RelationAlly")))
                selectedRelationKind = FactionRelationKind.Ally;
            GUI.color = Color.white;

            Rect neutralRect = new Rect(rect.x + btnWidth + 10f, rect.y + 3f, btnWidth, 30f);
            bool isNeutral = selectedRelationKind == FactionRelationKind.Neutral;
            GUI.color = isNeutral ? Color.cyan : Color.gray;
            if (Widgets.ButtonText(neutralRect, LanguageManager.Get("RelationNeutral")))
                selectedRelationKind = FactionRelationKind.Neutral;
            GUI.color = Color.white;

            Rect hostileRect = new Rect(rect.x + (btnWidth + 10f) * 2, rect.y + 3f, btnWidth, 30f);
            bool isHostile = selectedRelationKind == FactionRelationKind.Hostile;
            GUI.color = isHostile ? Color.red : Color.gray;
            if (Widgets.ButtonText(hostileRect, LanguageManager.Get("RelationHostile")))
                selectedRelationKind = FactionRelationKind.Hostile;
            GUI.color = Color.white;

            // Row 2: Cancel and Apply buttons (right-aligned)
            float actionBtnWidth = 120f;
            float row2Y = rect.y + 42f;
            int selectedCount = selectionStates.Values.Count(v => v);
            string applyLabel = selectedCount > 0
                ? $"{LanguageManager.Get("Apply")} ({selectedCount})"
                : LanguageManager.Get("Apply");

            Rect applyRect = new Rect(rect.xMax - actionBtnWidth, row2Y, actionBtnWidth, 30f);
            if (Widgets.ButtonText(applyRect, applyLabel))
            {
                ApplyChanges();
            }

            Rect cancelRect = new Rect(applyRect.x - actionBtnWidth - 10f, row2Y, actionBtnWidth, 30f);
            if (Widgets.ButtonText(cancelRect, LanguageManager.Get("Cancel")))
            {
                Close();
            }
        }

        private void ApplyChanges()
        {
            // Build result: merge new selections with existing overrides
            // Remove unchecked overrides that were previously selected, add/update checked ones
            var result = new List<FactionRelationOverride>();

            // Keep existing overrides for factions not in the selection
            foreach (var existing in existingOverrides)
            {
                if (!selectionStates.TryGetValue(existing.targetFactionDefName, out bool sel) || !sel)
                    continue; // removed
                // Update to new relation kind
                result.Add(new FactionRelationOverride(existing.targetFactionDefName, selectedRelationKind));
            }

            // Add newly selected factions
            foreach (var kvp in selectionStates)
            {
                if (!kvp.Value) continue;
                if (existingOverrides.Any(r => r.targetFactionDefName == kvp.Key)) continue;
                result.Add(new FactionRelationOverride(kvp.Key, selectedRelationKind));
            }

            onConfirm?.Invoke(result);
            Close();
        }

        private string GetRelationKey(FactionRelationKind kind)
        {
            switch (kind)
            {
                case FactionRelationKind.Ally: return "RelationAlly";
                case FactionRelationKind.Neutral: return "RelationNeutral";
                case FactionRelationKind.Hostile: return "RelationHostile";
                default: return "RelationNeutral";
            }
        }
    }
}
