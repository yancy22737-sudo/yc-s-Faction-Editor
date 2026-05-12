using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.Core;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer.UI.Dialogs;
using FactionGearCustomizer.UI.Panels;
using FactionGearCustomizer.Utils;

namespace FactionGearCustomizer.UI
{
    public class Window_FactionGroupEditor : Window
    {
        private readonly FactionDef factionDef;
        private readonly FactionGearData factionData;
        private List<PawnGroupMakerData> bufferGroups;
        private bool groupsModified;
        private Vector2 scrollPosition;

        // Kind list state
        private List<PawnKindDef> cachedKinds;
        private Dictionary<string, string> kindLabelBuffers = new Dictionary<string, string>();

        private const float KindRowHeight = 32f;

        public override Vector2 InitialSize => new Vector2(680f, 650f);

        public Window_FactionGroupEditor(FactionDef faction)
        {
            factionDef = faction;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            draggable = true;
            resizeable = true;

            factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(faction.defName);

            if (factionData.groupMakers != null && factionData.groupMakers.Count > 0)
            {
                bufferGroups = new List<PawnGroupMakerData>();
                foreach (var g in factionData.groupMakers)
                    bufferGroups.Add(g.DeepCopy());
            }
            else
            {
                bufferGroups = new List<PawnGroupMakerData>();
                if (faction.pawnGroupMakers != null)
                {
                    foreach (var maker in faction.pawnGroupMakers)
                        bufferGroups.Add(new PawnGroupMakerData(maker));
                }
            }

            // Load kinds
            cachedKinds = FactionGearEditor.GetFactionKinds(factionDef);
            var existingNames = new HashSet<string>(cachedKinds.Select(k => k.defName));
            if (factionData.kindGearData != null)
            {
                foreach (var kd in factionData.kindGearData)
                {
                    if (kd != null && !string.IsNullOrEmpty(kd.kindDefName) && !existingNames.Contains(kd.kindDefName))
                    {
                        var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kd.kindDefName);
                        if (kindDef != null)
                        {
                            cachedKinds.Add(kindDef);
                            existingNames.Add(kd.kindDefName);
                        }
                    }
                }
            }
            cachedKinds.Sort((a, b) => DefDisplayNameUtility.ComparePawnKinds(a, b, "Window_FactionGroupEditor"));

            foreach (var kind in cachedKinds)
            {
                var kd = factionData.GetKindData(kind.defName);
                kindLabelBuffers[kind.defName] = (kd != null && !string.IsNullOrEmpty(kd.Label)) ? kd.Label : kind.label;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Title
            string factionName = factionData.Label ?? factionDef.LabelCap.ToString();
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 30f),
                LanguageManager.Get("FactionGroupEditorTitle", factionName));
            Text.Font = GameFont.Small;

            // Calculate content height
            float groupsHeight = GroupListPanel.GetViewHeight(bufferGroups);
            float kindsHeight = 40f + cachedKinds.Count * KindRowHeight + 20f;
            float totalContentHeight = groupsHeight + kindsHeight;
            float scrollAreaHeight = inRect.height - 78f;
            float contentHeight = Mathf.Max(totalContentHeight, scrollAreaHeight);

            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, contentHeight);
            Rect scrollRect = new Rect(0f, 38f, inRect.width, scrollAreaHeight);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            // === Section 1: Groups ===
            GroupListPanel.Draw(new Rect(0f, 0f, viewRect.width, groupsHeight),
                ref bufferGroups, factionDef, () => groupsModified = true);

            // === Section 2: Kind List ===
            float kindSectionY = groupsHeight + 12f;
            Rect kindSectionRect = new Rect(0f, kindSectionY, viewRect.width, kindsHeight);
            DrawKindListSection(kindSectionRect);

            Widgets.EndScrollView();

            // Bottom buttons
            float bottomY = inRect.height - 35f;
            float btnW = 120f;
            float gap = 20f;
            float startX = (inRect.width - (btnW * 2 + gap)) / 2f;

            if (Widgets.ButtonText(new Rect(startX, bottomY, btnW, 30f), LanguageManager.Get("Apply")))
            {
                ApplyChanges();
                Close();
            }
            if (Widgets.ButtonText(new Rect(startX + btnW + gap, bottomY, btnW, 30f), LanguageManager.Get("Cancel")))
            {
                Close();
            }
        }

        private void DrawKindListSection(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(8f);

            // Header
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 28f),
                LanguageManager.Get("KindDefs") + " (" + cachedKinds.Count + ")");
            Text.Font = GameFont.Small;

            float y = inner.y + 32f;
            for (int i = 0; i < cachedKinds.Count; i++)
            {
                var kind = cachedKinds[i];
                Rect row = new Rect(inner.x, y, inner.width, KindRowHeight);

                if (i % 2 == 1) Widgets.DrawAltRect(row);

                // Editable name field
                float editX = inner.x;
                Rect nameRect = new Rect(editX, row.y + 4f, inner.width * 0.45f, 24f);
                string currentVal = kindLabelBuffers.TryGetValue(kind.defName, out string cv) ? cv : kind.label;
                string newVal = Widgets.TextField(nameRect, currentVal);
                if (newVal != currentVal)
                    kindLabelBuffers[kind.defName] = InputValidator.SanitizeName(newVal);

                // defName
                Rect defNameRect = new Rect(nameRect.xMax + 10f, row.y + 8f, inner.width * 0.35f, 18f);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(defNameRect, kind.defName);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                // Edit button — hover only
                if (ModsConfig.BiotechActive && Mouse.IsOver(row))
                {
                    Rect editBtnRect = new Rect(inner.xMax - 65f, row.y + 4f, 55f, 24f);
                    var kd = factionData.GetKindData(kind.defName);
                    bool hasSettings = kd != null &&
                        (kd.DisableXenotypeChances ||
                         (kd.XenotypeChances != null && kd.XenotypeChances.Count > 0) ||
                         !string.IsNullOrEmpty(kd.ForcedXenotype));
                    if (hasSettings) GUI.color = Color.cyan;

                    if (Widgets.ButtonText(editBtnRect, LanguageManager.Get("XenoButtonLabel")))
                    {
                        var dataToEdit = factionData.GetOrCreateKindData(kind.defName);
                        Find.WindowStack.Add(new Dialog_KindXenotypeEditor(kind, dataToEdit, factionDef.defName));
                    }
                    GUI.color = Color.white;
                    TooltipHandler.TipRegion(editBtnRect, LanguageManager.Get("KindXenotypeTooltip"));
                }

                y += KindRowHeight;
            }
        }

        private void ApplyChanges()
        {
            // Save groups
            if (groupsModified && bufferGroups != null)
            {
                factionData.groupMakers = new List<PawnGroupMakerData>();
                foreach (var g in bufferGroups)
                    factionData.groupMakers.Add(g.DeepCopy());
                factionData.isModified = true;

                var gameComponent = FactionGearGameComponent.Instance;
                if (gameComponent?.savedFactionGearData != null)
                {
                    var saveFactionData = gameComponent.savedFactionGearData
                        .FirstOrDefault(f => f.factionDefName == factionData.factionDefName);
                    if (saveFactionData != null)
                    {
                        saveFactionData.groupMakers = new List<PawnGroupMakerData>();
                        foreach (var g in bufferGroups)
                            saveFactionData.groupMakers.Add(g.DeepCopy());
                        saveFactionData.isModified = true;
                    }
                }
            }

            // Save kind labels
            bool kindModified = false;
            foreach (var kvp in kindLabelBuffers)
            {
                var kd = factionData.GetOrCreateKindData(kvp.Key);
                string originalLabel = kd.Label;
                var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kvp.Key);
                string defaultLabel = kindDef?.label ?? kvp.Key;

                if (kvp.Value != defaultLabel && kvp.Value != originalLabel)
                {
                    kd.Label = kvp.Value;
                    kd.isModified = true;
                    kindModified = true;
                }
                else if (kvp.Value == defaultLabel && !string.IsNullOrEmpty(originalLabel))
                {
                    kd.Label = null;
                    kd.isModified = true;
                    kindModified = true;
                }
            }

            if (groupsModified || kindModified)
            {
                factionData.isModified = true;
                FactionDefManager.ApplyFactionChanges(factionDef, factionData);
                FactionGearEditor.MarkDirty();
            }
        }
    }
}
