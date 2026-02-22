using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearModification.UI;

namespace FactionGearCustomizer.UI.Panels
{
    public static class GearEditPanel
    {
        public static readonly BodyPartGroupDef GroupShoulders = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Shoulders");
        public static readonly BodyPartGroupDef GroupArms = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Arms");
        public static readonly BodyPartGroupDef GroupHands = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Hands");
        public static readonly BodyPartGroupDef GroupWaist = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Waist");
        public static readonly BodyPartGroupDef GroupLegs = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Legs");
        public static readonly BodyPartGroupDef GroupFeet = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Feet");
        public static void Draw(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            // Mode Toggle
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 30f);
            
            bool isAdvanced = EditorSession.CurrentMode == EditorMode.Advanced;
            Widgets.CheckboxLabeled(new Rect(headerRect.x, headerRect.y, 130f, 24f), LanguageManager.Get("Advanced"), ref isAdvanced);
            if (isAdvanced != (EditorSession.CurrentMode == EditorMode.Advanced))
            {
                EditorSession.CurrentMode = isAdvanced ? EditorMode.Advanced : EditorMode.Simple;
            }

            // Undo/Redo Buttons
            float undoX = headerRect.x + 140f;
            Rect undoRect = new Rect(undoX, headerRect.y, 24f, 24f);
            
            // Use icon buttons instead of text buttons
            bool canUndo = TexCache.UndoTex != null;
            if (canUndo)
            {
                if (Widgets.ButtonImage(undoRect, TexCache.UndoTex))
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        var kData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                        UndoManager.Undo(kData);
                        FactionGearEditor.MarkDirty();
                    }
                }
            }
            else
            {
                // Fallback to text button if icon not available
                if (Widgets.ButtonText(undoRect, "<"))
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        var kData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                        UndoManager.Undo(kData);
                        FactionGearEditor.MarkDirty();
                    }
                }
            }
            TooltipHandler.TipRegion(undoRect, LanguageManager.Get("UndoTooltip"));

            Rect redoRect = new Rect(undoX + 28f, headerRect.y, 24f, 24f);
            
            // Use icon buttons instead of text buttons
            bool canRedo = TexCache.RedoTex != null;
            if (canRedo)
            {
                if (Widgets.ButtonImage(redoRect, TexCache.RedoTex))
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        var kData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                        UndoManager.Redo(kData);
                        FactionGearEditor.MarkDirty();
                    }
                }
            }
            else
            {
                // Fallback to text button if icon not available
                if (Widgets.ButtonText(redoRect, ">"))
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        var kData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                        UndoManager.Redo(kData);
                        FactionGearEditor.MarkDirty();
                    }
                }
            }
            TooltipHandler.TipRegion(redoRect, LanguageManager.Get("RedoTooltip"));
            
            // Handle Keyboard Shortcuts
            if (Event.current.type == EventType.KeyDown && Event.current.control)
            {
                if (Event.current.keyCode == KeyCode.Z)
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        var kData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                        UndoManager.Undo(kData);
                        FactionGearEditor.MarkDirty();
                        Event.current.Use();
                    }
                }
                else if (Event.current.keyCode == KeyCode.Y)
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        var kData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                        UndoManager.Redo(kData);
                        FactionGearEditor.MarkDirty();
                        Event.current.Use();
                    }
                }
            }

            // Preview Button
            if (Current.ProgramState == ProgramState.Playing)
            {
                Rect previewButtonRect = new Rect(headerRect.xMax - 80f, headerRect.y, 80f, 24f);
                if (Widgets.ButtonText(previewButtonRect, LanguageManager.Get("Preview")))
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
                    {
                        var fDef = DefDatabase<FactionDef>.GetNamedSilentFail(EditorSession.SelectedFactionDefName);
                        if (fDef != null)
                        {
                            if (Find.FactionManager.FirstFactionOfDef(fDef) != null)
                            {
                                var kinds = FactionGearEditor.GetFactionKinds(fDef);
                                Find.WindowStack.Add(new FactionGearPreviewWindow(kinds, fDef));
                            }
                            else
                            {
                                Messages.Message(LanguageManager.Get("CannotPreviewFactionNotPresent"), MessageTypeDefOf.RejectInput, false);
                            }
                        }
                    }
                }
                TooltipHandler.TipRegion(previewButtonRect, LanguageManager.Get("PreviewAllKindsTooltip"));
            }

            Rect contentRect = new Rect(innerRect.x, innerRect.y + 35f, innerRect.width, innerRect.height - 35f);

            if (EditorSession.CurrentMode == EditorMode.Simple)
            {
                DrawSimpleMode(contentRect);
            }
            else
            {
                DrawAdvancedMode(contentRect);
            }
        }

        private static void DrawSimpleMode(Rect innerRect)
        {
            List<GearItem> gearItemsToDraw = new List<GearItem>();
            KindGearData currentKindData = null;
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                currentKindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                gearItemsToDraw = GetCurrentCategoryGear(currentKindData);
            }

            Widgets.Label(new Rect(innerRect.x, innerRect.y, 120f, 24f), LanguageManager.Get("SelectedGear"));

            Rect clearButtonRect = new Rect(innerRect.xMax - 70f, innerRect.y, 70f, 20f);
            if (Widgets.ButtonText(clearButtonRect, LanguageManager.Get("ClearAll")))
            {
                if (currentKindData != null) 
                {
                    Dialog_ConfirmWithCheckbox.ShowIfNotDismissed(
                        "ClearAllGearConfirm",
                        LanguageManager.Get("ClearAllGearConfirmMessage"),
                        delegate { ClearAllGear(currentKindData); },
                        LanguageManager.Get("ClearAll"),
                        LanguageManager.Get("Cancel"),
                        null,
                        true
                    );
                }
            }

            Rect aaButtonRect = new Rect(clearButtonRect.x - 115f, innerRect.y, 110f, 20f);
            if (Widgets.ButtonText(aaButtonRect, LanguageManager.Get("ApplyOthers")))
            {
                if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                {
                    Find.WindowStack.Add(new Dialog_MessageBox(
                        LanguageManager.Get("ApplyToAllKindsConfirm"),
                        LanguageManager.Get("Yes"),
                        delegate
                        {
                            FactionGearEditor.CopyKindDefGear(); 
                            FactionGearEditor.ApplyToAllKindsInFaction(); 
                            Messages.Message(LanguageManager.Get("AppliedToAllKinds"), MessageTypeDefOf.PositiveEvent);
                        },
                        "No",
                        null
                    ));
                }
            }
            TooltipHandler.TipRegion(aaButtonRect, LanguageManager.Get("ApplyToOthersTooltip"));

            Rect tabRowRect = new Rect(innerRect.x, innerRect.y + 24f, innerRect.width, 24f);
            
            Rect clearCatRect = new Rect(tabRowRect.xMax - 70f, tabRowRect.y, 70f, 24f);
            if (Widgets.ButtonText(clearCatRect, LanguageManager.Get("Clear")))
            {
                if (currentKindData != null)
                {
                    Dialog_ConfirmWithCheckbox.ShowIfNotDismissed(
                        "ClearCategoryGearConfirm",
                        LanguageManager.Get("ClearCategoryGearConfirmMessage"),
                        delegate { ClearCategoryGear(currentKindData, EditorSession.SelectedCategory); },
                        LanguageManager.Get("Clear"),
                        LanguageManager.Get("Cancel"),
                        null,
                        true
                    );
                }
            }
            TooltipHandler.TipRegion(clearCatRect, LanguageManager.Get("ClearCategoryTooltip"));

            Rect tabRect = new Rect(tabRowRect.x, tabRowRect.y, tabRowRect.width - 75f, 24f);
            DrawCategoryTabs(tabRect);

            float previewHeight = 0f;
            float layerHeaderHeight = 24f;
            if (EditorSession.SelectedCategory == GearCategory.Apparel || EditorSession.SelectedCategory == GearCategory.Armors)
            {
                previewHeight = 230f; 
            }
            float statsHeight = 24f + (EditorSession.LayerPreviewHidden ? layerHeaderHeight : layerHeaderHeight + previewHeight);
            
            float listStartY = tabRect.yMax + 5f;
            Rect listOutRect = new Rect(innerRect.x, listStartY, innerRect.width, innerRect.height - (listStartY - innerRect.y) - statsHeight);

            // Filter Advanced Items for Current Category
            List<SpecRequirementEdit> advancedItems = new List<SpecRequirementEdit>();
            if (currentKindData != null)
            {
                if (EditorSession.SelectedCategory == GearCategory.Weapons && !currentKindData.SpecificWeapons.NullOrEmpty())
                {
                    advancedItems.AddRange(currentKindData.SpecificWeapons.Where(x => x.Thing != null && x.Thing.IsRangedWeapon));
                }
                else if (EditorSession.SelectedCategory == GearCategory.MeleeWeapons && !currentKindData.SpecificWeapons.NullOrEmpty())
                {
                    advancedItems.AddRange(currentKindData.SpecificWeapons.Where(x => x.Thing != null && x.Thing.IsMeleeWeapon));
                }
                else if (EditorSession.SelectedCategory == GearCategory.Others && !currentKindData.SpecificApparel.NullOrEmpty())
                {
                    advancedItems.AddRange(currentKindData.SpecificApparel.Where(x => x.Thing != null && IsBelt(x.Thing)));
                }
                else if (EditorSession.SelectedCategory == GearCategory.Armors && !currentKindData.SpecificApparel.NullOrEmpty())
                {
                    advancedItems.AddRange(currentKindData.SpecificApparel.Where(x => x.Thing != null && IsArmor(x.Thing)));
                }
                else if (EditorSession.SelectedCategory == GearCategory.Apparel && !currentKindData.SpecificApparel.NullOrEmpty())
                {
                    advancedItems.AddRange(currentKindData.SpecificApparel.Where(x => x.Thing != null && IsApparel(x.Thing)));
                }
            }

            float contentHeight = 0f;
            foreach (var item in gearItemsToDraw) {
                contentHeight += (item == EditorSession.ExpandedGearItem) ? 60f : 30f;
            }
            if (advancedItems.Any())
            {
                contentHeight += 30f; // Header
                foreach (var advItem in advancedItems)
                {
                    contentHeight += (advItem == EditorSession.ExpandedSpecItem) ? 60f : 30f;
                }
            }
            
            Rect listViewRect = new Rect(0, 0, listOutRect.width - 16f, Mathf.Max(contentHeight, listOutRect.height));

            Widgets.BeginScrollView(listOutRect, ref EditorSession.GearListScrollPos, listViewRect);
            Listing_Standard gearListing = new Listing_Standard();
            gearListing.Begin(listViewRect);
            
            if (gearItemsToDraw.Any() && currentKindData != null)
            {
                foreach (var gearItem in gearItemsToDraw.ToList())
                {
                    bool isExpanded = (gearItem == EditorSession.ExpandedGearItem);
                    Rect rowRect = gearListing.GetRect(isExpanded ? 56f : 28f);
                    DrawGearItem(rowRect, gearItem, currentKindData, isExpanded);
                    gearListing.Gap(2f);
                }
            }

            if (advancedItems.Any())
            {
                gearListing.GapLine();
                WidgetsUtils.Label(gearListing, $"<b>{LanguageManager.Get("AdvancedSpecificItems")}</b>");
                foreach (var advItem in advancedItems)
                {
                    bool isAdvExpanded = (advItem == EditorSession.ExpandedSpecItem);
                    Rect rowRect = gearListing.GetRect(isAdvExpanded ? 56f : 28f);
                    DrawAdvancedItemSimple(rowRect, advItem, currentKindData);
                    gearListing.Gap(2f);
                }
            }

            gearListing.End();
            Widgets.EndScrollView();

            if (currentKindData != null)
            {
                float currentY = innerRect.yMax - statsHeight;
                
                Rect statsRect = new Rect(innerRect.x, currentY, innerRect.width, 24f);
                float avgValue = FactionGearEditor.GetAverageValue(currentKindData);
                float avgWeight = FactionGearEditor.GetAverageWeight(currentKindData);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(statsRect, $"{LanguageManager.Get("AvgValue")}: {avgValue:F0} | {LanguageManager.Get("AvgWeight")}: {avgWeight:F1}");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                
                TooltipHandler.TipRegion(statsRect, LanguageManager.Get("WeightMechanicsTooltip"));
            }
        }

        private static void DrawAdvancedMode(Rect rect)
        {
            if (string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) || string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                Widgets.Label(rect, LanguageManager.Get("SelectAKindDefFirst"));
                return;
            }

            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
            var kindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);

            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 24f);
            Widgets.Label(new Rect(headerRect.x, headerRect.y, 120f, 24f), LanguageManager.Get("AdvancedSettings"));

            Rect clearButtonRect = new Rect(headerRect.xMax - 70f, headerRect.y, 70f, 20f);
            if (Widgets.ButtonText(clearButtonRect, LanguageManager.Get("ClearAll")))
            {
                Dialog_ConfirmWithCheckbox.ShowIfNotDismissed(
                    "ClearAllGearConfirm",
                    LanguageManager.Get("ClearAllGearConfirmMessage"),
                    delegate { ClearAllGear(kindData); },
                    LanguageManager.Get("ClearAll"),
                    LanguageManager.Get("Cancel"),
                    null,
                    true
                );
            }

            Rect aaButtonRect = new Rect(clearButtonRect.x - 115f, headerRect.y, 110f, 20f);
            if (Widgets.ButtonText(aaButtonRect, LanguageManager.Get("ApplyOthers")))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    LanguageManager.Get("ApplyToAllKindsConfirm"),
                    LanguageManager.Get("Yes"),
                    delegate
                    {
                        FactionGearEditor.CopyKindDefGear();
                        FactionGearEditor.ApplyToAllKindsInFaction();
                        Messages.Message(LanguageManager.Get("AppliedToAllKinds"), MessageTypeDefOf.PositiveEvent);
                    },
                    "No",
                    null
                ));
            }
            TooltipHandler.TipRegion(aaButtonRect, LanguageManager.Get("ApplyToOthersTooltip"));

            Rect tabRect = new Rect(rect.x, rect.y + 28f, rect.width, 24f);
            float tabWidth = rect.width / 4f;
            
            DrawAdvTab(new Rect(tabRect.x, tabRect.y, tabWidth, tabRect.height), LanguageManager.Get("General"), AdvancedTab.General);
            DrawAdvTab(new Rect(tabRect.x + tabWidth, tabRect.y, tabWidth, tabRect.height), LanguageManager.Get("Apparel"), AdvancedTab.Apparel);
            DrawAdvTab(new Rect(tabRect.x + tabWidth * 2, tabRect.y, tabWidth, tabRect.height), LanguageManager.Get("Weapons"), AdvancedTab.Weapons);
            DrawAdvTab(new Rect(tabRect.x + tabWidth * 3, tabRect.y, tabWidth, tabRect.height), LanguageManager.Get("Hediffs"), AdvancedTab.Hediffs);

            Rect contentRect = new Rect(rect.x, rect.y + 58f, rect.width, rect.height - 58f);
            
            float height = 500f; 

            float gearListHeight = 0f;
            if (EditorSession.CurrentAdvancedTab == AdvancedTab.Apparel || EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons)
            {
                if (EditorSession.CurrentAdvancedTab == AdvancedTab.Apparel && EditorSession.SelectedCategory != GearCategory.Apparel && EditorSession.SelectedCategory != GearCategory.Armors)
                    EditorSession.SelectedCategory = GearCategory.Apparel;
                if (EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons && EditorSession.SelectedCategory != GearCategory.Weapons && EditorSession.SelectedCategory != GearCategory.MeleeWeapons)
                    EditorSession.SelectedCategory = GearCategory.Weapons;

                var gearItems = GetCurrentCategoryGear(kindData);
                foreach (var item in gearItems)
                {
                    gearListHeight += (item == EditorSession.ExpandedGearItem) ? 60f : 30f;
                }
                gearListHeight += 40f; 
            }

            if (EditorSession.CurrentAdvancedTab == AdvancedTab.Apparel)
            {
                 height = gearListHeight + 100f;
                 if (!kindData.SpecificApparel.NullOrEmpty())
                     height += kindData.SpecificApparel.Count * 170f + 100f;
                 else
                     height += 200f;
            }
            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons)
            {
                 height = gearListHeight + 150f;
                 if (!kindData.SpecificWeapons.NullOrEmpty())
                     height += kindData.SpecificWeapons.Count * 170f + 100f;
                 else
                     height += 200f;
            }
            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Hediffs && !kindData.ForcedHediffs.NullOrEmpty())
                 height += kindData.ForcedHediffs.Count * 140f + 100f;
            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Hediffs)
                 height = 200f;
            else 
                 height = 350f;

            Rect viewRect = new Rect(0, 0, contentRect.width - 16f, height);

            Widgets.BeginScrollView(contentRect, ref EditorSession.AdvancedScrollPos, viewRect);
            Listing_Standard ui = new Listing_Standard();
            ui.Begin(viewRect);

            switch (EditorSession.CurrentAdvancedTab)
            {
                case AdvancedTab.General:
                    DrawAdvancedGeneral(ui, kindData);
                    break;
                case AdvancedTab.Apparel:
                    DrawAdvancedApparel(ui, kindData);
                    break;
                case AdvancedTab.Weapons:
                    DrawAdvancedWeapons(ui, kindData);
                    break;
                case AdvancedTab.Hediffs:
                    DrawAdvancedHediffs(ui, kindData);
                    break;
            }

            ui.End();
            Widgets.EndScrollView();
        }

        private static void DrawAdvTab(Rect rect, string label, AdvancedTab tab)
        {
            bool isSelected = EditorSession.CurrentAdvancedTab == tab;
            if (isSelected) Widgets.DrawHighlightSelected(rect);
            else Widgets.DrawHighlightIfMouseover(rect);
            
            if (Widgets.ButtonInvisible(rect))
            {
                if (EditorSession.CurrentAdvancedTab != tab)
                {
                    EditorSession.CurrentAdvancedTab = tab;
                    EditorSession.AdvancedScrollPos = Vector2.zero;
                }
            }
            
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawCategoryTabs(Rect rect)
        {
            float tabWidth = rect.width / 5f;
            DrawTab(new Rect(rect.x, rect.y, tabWidth, rect.height), LanguageManager.Get("Ranged"), GearCategory.Weapons);
            DrawTab(new Rect(rect.x + tabWidth, rect.y, tabWidth, rect.height), LanguageManager.Get("Melee"), GearCategory.MeleeWeapons);
            DrawTab(new Rect(rect.x + tabWidth * 2, rect.y, tabWidth, rect.height), LanguageManager.Get("Armors"), GearCategory.Armors);
            DrawTab(new Rect(rect.x + tabWidth * 3, rect.y, tabWidth, rect.height), LanguageManager.Get("Clothes"), GearCategory.Apparel);
            DrawTab(new Rect(rect.x + tabWidth * 4, rect.y, tabWidth, rect.height), LanguageManager.Get("Others"), GearCategory.Others);
        }

        private static void DrawTab(Rect rect, string label, GearCategory category)
        {
            bool isSelected = EditorSession.SelectedCategory == category;
            
            if (isSelected)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                Widgets.DrawHighlightSelected(rect);
                GUI.color = Color.white;
            }
            else
            {
                Widgets.DrawHighlightIfMouseover(rect);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                if (EditorSession.SelectedCategory != category)
                {
                    EditorSession.SelectedCategory = category;
                    EditorSession.ExpandedGearItem = null;
                    EditorSession.CachedModSources = null;
                    FactionGearEditor.GetUniqueModSources();
                    FactionGearEditor.CalculateBounds();
                }
            }
            
            Text.Anchor = TextAnchor.MiddleCenter;
            if (isSelected) GUI.color = new Color(1f, 0.9f, 0.5f);
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static List<GearItem> GetCurrentCategoryGear(KindGearData kindData)
        {
            switch (EditorSession.SelectedCategory)
            {
                case GearCategory.Weapons: return kindData.weapons;
                case GearCategory.MeleeWeapons: return kindData.meleeWeapons;
                case GearCategory.Armors: return kindData.armors;
                case GearCategory.Apparel: return kindData.apparel;
                case GearCategory.Others: return kindData.others;
                default: return new List<GearItem>();
            }
        }

        private static void ClearAllGear(KindGearData kindData)
        {
            UndoManager.RecordState(kindData);
            kindData.weapons.Clear();
            kindData.meleeWeapons.Clear();
            kindData.armors.Clear();
            kindData.apparel.Clear();
            kindData.others.Clear();
            
            kindData.SpecificApparel?.Clear();
            kindData.SpecificWeapons?.Clear();
            kindData.ForcedHediffs?.Clear();
            
            kindData.isModified = true;
            FactionGearEditor.MarkDirty();
        }

        private static void ClearCategoryGear(KindGearData kindData, GearCategory category)
        {
            UndoManager.RecordState(kindData);
            switch (category)
            {
                case GearCategory.Weapons: kindData.weapons.Clear(); break;
                case GearCategory.MeleeWeapons: kindData.meleeWeapons.Clear(); break;
                case GearCategory.Armors: kindData.armors.Clear(); break;
                case GearCategory.Apparel: kindData.apparel.Clear(); break;
                case GearCategory.Others: kindData.others.Clear(); break;
            }
            kindData.isModified = true;
            FactionGearEditor.MarkDirty();
        }

        // --- Advanced Mode Helpers ---
        
        private static void DrawAdvancedGeneral(Listing_Standard ui, KindGearData kindData)
        {
            WidgetsUtils.Label(ui, $"<b>{LanguageManager.Get("GeneralOverrides")}</b>");
            ui.Gap();

            bool forceIgnore = FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;

            ui.CheckboxLabeled(LanguageManager.Get("ForceNaked"), ref kindData.ForceNaked);
            if (!kindData.ForceNaked)
            {
                // Force Only Selected moved here from Apparel tab
                bool forceOnly = kindData.ForceOnlySelected;
                ui.CheckboxLabeled(LanguageManager.Get("ForceOnlySelected"), ref forceOnly, LanguageManager.Get("ForceOnlySelectedTooltip"));
                if (forceOnly != kindData.ForceOnlySelected)
                {
                    UndoManager.RecordState(kindData);
                    kindData.ForceOnlySelected = forceOnly;
                    FactionGearEditor.MarkDirty();
                }
            }

            ui.Gap();

            DrawOverrideEnum(ui, LanguageManager.Get("ItemQuality"), kindData.ItemQuality, val => { UndoManager.RecordState(kindData); kindData.ItemQuality = val; FactionGearEditor.MarkDirty(); }, q => LanguageManager.Get("Quality" + q));
            DrawOverrideFloatRange(ui, LanguageManager.Get("ApparelBudget"), ref kindData.ApparelMoney, val => { UndoManager.RecordState(kindData); kindData.ApparelMoney = val; FactionGearEditor.MarkDirty(); }, forceIgnore);
            DrawOverrideFloatRange(ui, LanguageManager.Get("WeaponBudget"), ref kindData.WeaponMoney, val => { UndoManager.RecordState(kindData); kindData.WeaponMoney = val; FactionGearEditor.MarkDirty(); }, forceIgnore);
            DrawOverrideFloatRange(ui, LanguageManager.Get("TechBudget"), ref kindData.TechMoney, val => { UndoManager.RecordState(kindData); kindData.TechMoney = val; FactionGearEditor.MarkDirty(); }, forceIgnore);

            Rect colorRect = ui.GetRect(28f);
            Widgets.Label(colorRect.LeftHalf(), LanguageManager.Get("ApparelColor"));
            if (kindData.ApparelColor.HasValue)
            {
                Rect rightHalf = colorRect.RightHalf();
                Rect pickRect = new Rect(rightHalf.x, rightHalf.y, rightHalf.width - 35f, 28f);
                Rect xRect = new Rect(rightHalf.xMax - 30f, rightHalf.y, 30f, 28f);

                if (Widgets.ButtonText(pickRect, LanguageManager.Get("PickColor")))
                {
                    Find.WindowStack.Add(new Window_ColorPicker(kindData.ApparelColor.Value, (c) => { UndoManager.RecordState(kindData); kindData.ApparelColor = c; FactionGearEditor.MarkDirty(); }));
                }
                Widgets.DrawBoxSolid(new Rect(rightHalf.x - 30f, rightHalf.y, 28f, 28f), kindData.ApparelColor.Value);
                if (Widgets.ButtonText(xRect, "X"))
                {
                    UndoManager.RecordState(kindData); kindData.ApparelColor = null; FactionGearEditor.MarkDirty();
                }
            }
            else
            {
                if (Widgets.ButtonText(colorRect.RightHalf(), LanguageManager.Get("SetColor")))
                {
                    UndoManager.RecordState(kindData); kindData.ApparelColor = Color.white; FactionGearEditor.MarkDirty();
                }
            }
        }

        private static void DrawAdvancedApparel(Listing_Standard ui, KindGearData kindData)
        {
            DrawEmbeddedGearList(ui, kindData, GearCategory.Apparel, GearCategory.Armors);
            
            // Moved ForceNaked and ForceOnlySelected to General tab
            
            ui.GapLine();
            WidgetsUtils.Label(ui, $"<b>{LanguageManager.Get("SpecificApparelAdvanced")}</b>");
            if (ui.ButtonText(LanguageManager.Get("AddNewApparel")))
            {
                if (kindData.SpecificApparel == null) kindData.SpecificApparel = new List<SpecRequirementEdit>();
                kindData.SpecificApparel.Add(new SpecRequirementEdit() { Thing = ThingDefOf.Apparel_Parka });
                FactionGearEditor.MarkDirty();
            }
            if (!kindData.SpecificApparel.NullOrEmpty())
            {
                for (int i = 0; i < kindData.SpecificApparel.Count; i++)
                {
                    var item = kindData.SpecificApparel[i];
                    int index = i;
                    ApparelCardUI.Draw(ui, item, index, () => 
                    {
                        kindData.SpecificApparel.RemoveAt(index);
                        FactionGearEditor.MarkDirty();
                    });
                }
            }
        }

        private static void DrawAdvancedWeapons(Listing_Standard ui, KindGearData kindData)
        {
            DrawEmbeddedGearList(ui, kindData, GearCategory.Weapons, GearCategory.MeleeWeapons);
            DrawOverrideEnum(ui, LanguageManager.Get("ForcedWeaponQuality"), kindData.ForcedWeaponQuality, val => { kindData.ForcedWeaponQuality = val; FactionGearEditor.MarkDirty(); }, q => LanguageManager.Get("Quality" + q));
            
            float biocodeChance = kindData.BiocodeWeaponChance ?? 0f;
            float oldBiocode = biocodeChance;
            WidgetsUtils.Label(ui, $"{LanguageManager.Get("BiocodeChance")}: {biocodeChance:P0}");
            biocodeChance = ui.Slider(biocodeChance, 0f, 1f);
            if (Math.Abs(biocodeChance - oldBiocode) > 0.001f)
            {
                kindData.BiocodeWeaponChance = biocodeChance;
                FactionGearEditor.MarkDirty();
            }
            ui.GapLine();
            WidgetsUtils.Label(ui, $"<b>{LanguageManager.Get("SpecificWeaponsAdvanced")}</b>");
            if (ui.ButtonText(LanguageManager.Get("AddNewWeapon")))
            {
                if (kindData.SpecificWeapons == null) kindData.SpecificWeapons = new List<SpecRequirementEdit>();
                kindData.SpecificWeapons.Add(new SpecRequirementEdit() { Thing = ThingDef.Named("Gun_AssaultRifle") });
                FactionGearEditor.MarkDirty();
            }
            if (!kindData.SpecificWeapons.NullOrEmpty())
            {
                for (int i = 0; i < kindData.SpecificWeapons.Count; i++)
                {
                    var item = kindData.SpecificWeapons[i];
                    int index = i;
                    ApparelCardUI.Draw(ui, item, index, () =>
                    {
                        kindData.SpecificWeapons.RemoveAt(index);
                        FactionGearEditor.MarkDirty();
                    });
                }
            }
        }

        private static void DrawAdvancedHediffs(Listing_Standard ui, KindGearData kindData)
        {
            WidgetsUtils.Label(ui, $"<b>{LanguageManager.Get("ForcedHediffs")}</b>");
            if (ui.ButtonText(LanguageManager.Get("AddNewHediff")))
            {
                if (kindData.ForcedHediffs == null) kindData.ForcedHediffs = new List<ForcedHediff>();
                kindData.ForcedHediffs.Add(new ForcedHediff() { HediffDef = HediffDefOf.Scaria });
                FactionGearEditor.MarkDirty();
            }
            if (!kindData.ForcedHediffs.NullOrEmpty())
            {
                for (int i = 0; i < kindData.ForcedHediffs.Count; i++)
                {
                    var item = kindData.ForcedHediffs[i];
                    int index = i;
                    HediffCardUI.Draw(ui, item, index, () =>
                    {
                        kindData.ForcedHediffs.RemoveAt(index);
                        FactionGearEditor.MarkDirty();
                    });
                }
            }
        }

        private static void DrawOverrideEnum<T>(Listing_Standard ui, string label, T? currentValue, Action<T?> setValue, Func<T, string> labelFormatter = null) where T : struct
        {
            Rect rect = ui.GetRect(28f);
            Widgets.Label(rect.LeftHalf(), label + ":");
            
            string GetLabel(T val) => labelFormatter != null ? labelFormatter(val) : val.ToString();
            
            string btnLabel = currentValue.HasValue ? GetLabel(currentValue.Value) : LanguageManager.Get("Default");
            if (Widgets.ButtonText(rect.RightHalf(), btnLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption(LanguageManager.Get("Default"), () => setValue(null)));
                foreach (T val in Enum.GetValues(typeof(T)))
                {
                    options.Add(new FloatMenuOption(GetLabel(val), () => setValue(val)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void DrawOverrideFloatRange(Listing_Standard ui, string label, ref FloatRange? range, Action<FloatRange?> setRange, bool forceIgnore = false)
        {
            Rect rect = ui.GetRect(28f);
            Widgets.Label(rect.LeftHalf(), label + ":");

            if (forceIgnore)
            {
                GUI.color = Color.gray;
                Widgets.Label(rect.RightHalf(), LanguageManager.Get("MaxForceIgnore"));
                GUI.color = Color.white;
                return;
            }

            if (range.HasValue)
            {
                if (Widgets.ButtonText(rect.RightHalf(), $"{range.Value.min}-{range.Value.max}"))
                {
                    setRange(null);
                    return;
                }
                FloatRange val = range.Value;
                FloatRange oldVal = val;
                WidgetsUtils.FloatRange(ui.GetRect(24f), label.GetHashCode(), ref val, 0f, 5000f);
                if (val != oldVal)
                {
                    setRange(val);
                    range = val;
                }
            }
            else
            {
                if (Widgets.ButtonText(rect.RightHalf(), LanguageManager.Get("Default")))
                {
                    setRange(new FloatRange(0, 1000));
                }
            }
        }
        


        private static void DrawEmbeddedGearList(Listing_Standard ui, KindGearData kindData, params GearCategory[] allowedCategories)
        {
            Rect fullRect = ui.GetRect(24f);
            Rect tabRect = new Rect(fullRect.x, fullRect.y, fullRect.width - 75f, fullRect.height);
            Rect clearCatRect = new Rect(fullRect.xMax - 70f, fullRect.y, 70f, fullRect.height);
            float tabWidth = tabRect.width / allowedCategories.Length;
            for (int i = 0; i < allowedCategories.Length; i++)
            {
                var cat = allowedCategories[i];
                string label = cat.ToString();
                if (cat == GearCategory.Weapons) label = LanguageManager.Get("Ranged");
                else if (cat == GearCategory.MeleeWeapons) label = LanguageManager.Get("Melee");
                else if (cat == GearCategory.Armors) label = LanguageManager.Get("Armors");
                else if (cat == GearCategory.Apparel) label = LanguageManager.Get("Clothes");
                
                Rect catRect = new Rect(tabRect.x + tabWidth * i, tabRect.y, tabWidth, tabRect.height);
                bool isSelected = EditorSession.SelectedCategory == cat;
                if (isSelected) { Widgets.DrawHighlightSelected(catRect); }
                else { Widgets.DrawHighlightIfMouseover(catRect); }
                if (Widgets.ButtonInvisible(catRect))
                {
                    if (EditorSession.SelectedCategory != cat)
                    {
                        EditorSession.SelectedCategory = cat;
                        EditorSession.ExpandedGearItem = null;
                        FactionGearEditor.CalculateBounds();
                    }
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(catRect, label);
                Text.Anchor = TextAnchor.UpperLeft;
            }
            if (Widgets.ButtonText(clearCatRect, LanguageManager.Get("Clear")))
            {
                Dialog_ConfirmWithCheckbox.ShowIfNotDismissed(
                    "ClearCategoryGearConfirm",
                    LanguageManager.Get("ClearCategoryGearConfirmMessage"),
                    delegate { ClearCategoryGear(kindData, EditorSession.SelectedCategory); },
                    LanguageManager.Get("Clear"),
                    LanguageManager.Get("Cancel"),
                    null,
                    true
                );
            }
            ui.Gap(5f);
            var gearItems = GetCurrentCategoryGear(kindData);
            if (gearItems.Any())
            {
                foreach (var gearItem in gearItems.ToList())
                {
                    bool isExpanded = (gearItem == EditorSession.ExpandedGearItem);
                    Rect rowRect = ui.GetRect(isExpanded ? 56f : 28f);
                    DrawGearItem(rowRect, gearItem, kindData, isExpanded);
                    ui.Gap(2f);
                }
            }
            else
            {
                WidgetsUtils.Label(ui, LanguageManager.Get("NoItemsInThisCategory"));
            }
            ui.GapLine();
        }

        private static void DrawGearItem(Rect rect, GearItem gearItem, KindGearData kindData, bool isExpanded)
        {
            var thingDef = gearItem.ThingDef;
            if (thingDef == null) return;
            Widgets.DrawHighlightIfMouseover(rect);
            if (gearItem.weight != 1f)
            {
                GUI.color = new Color(1f, 0.8f, 0.4f, 0.3f);
                Widgets.DrawHighlight(rect);
                GUI.color = Color.white;
            }
            Rect row1 = new Rect(rect.x, rect.y, rect.width, 28f);
            Rect iconRect = new Rect(row1.x + 4f, row1.y + (row1.height - 30f) / 2f, 30f, 30f);
            float infoButtonOffset = EditorSession.IsInGame ? 28f : 0f;
            Rect infoButtonRect = new Rect(iconRect.xMax + 4f, row1.y + (row1.height - 24f) / 2f, 24f, 24f);
            Rect removeRect = new Rect(row1.xMax - 26f, row1.y + (row1.height - 24f) / 2f, 24f, 24f);
            Rect nameRect = new Rect(infoButtonRect.xMax + 4f, row1.y, removeRect.x - infoButtonRect.xMax - 8f - infoButtonOffset, 28f);
            
            if (EditorSession.IsInGame)
            {
                Widgets.InfoCardButton(infoButtonRect.x, infoButtonRect.y, thingDef);
            }
            Rect interactRect = new Rect(row1.x, row1.y, row1.width - 30f, 28f);
            if (Widgets.ButtonInvisible(interactRect))
            {
                EditorSession.ExpandedGearItem = (EditorSession.ExpandedGearItem == gearItem) ? null : gearItem;
            }
            Texture2D icon = FactionGearEditor.GetIconWithLazyLoading(thingDef);
            if (icon != null) WidgetsUtils.DrawTextureFitted(iconRect, icon, 1f);
            else if (thingDef.graphic?.MatSingle?.mainTexture != null) WidgetsUtils.DrawTextureFitted(iconRect, thingDef.graphic.MatSingle.mainTexture, 1f);
            
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            float nameWidth = nameRect.width;
            if (!isExpanded && gearItem.weight != 1f) nameWidth -= 45f;
            Widgets.Label(new Rect(nameRect.x, nameRect.y, nameWidth, nameRect.height), thingDef.LabelCap);
            Text.WordWrap = true;
            if (!isExpanded && gearItem.weight != 1f)
            {
                Text.Anchor = TextAnchor.MiddleRight;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(nameRect.xMax - 45f, row1.y, 45f, 28f), $"{LanguageManager.Get("Weight")}:{gearItem.weight:F1}");
                GUI.color = Color.white;
            }
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonImage(removeRect, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
            {
                UndoManager.RecordState(kindData);
                GetCurrentCategoryGear(kindData).Remove(gearItem);
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
                if (EditorSession.ExpandedGearItem == gearItem) EditorSession.ExpandedGearItem = null;
            }
            if (isExpanded)
            {
                Rect row2 = new Rect(rect.x, rect.y + 28f, rect.width, 28f);
                Rect sliderRect = new Rect(row2.x + 10f, row2.y + 4f, row2.width - 60f, 20f);
                Rect weightRect = new Rect(row2.xMax - 40f, row2.y, 35f, 28f);
                if (Event.current.type == EventType.MouseDown && sliderRect.Contains(Event.current.mousePosition)) UndoManager.RecordState(kindData);
                float newWeight = Widgets.HorizontalSlider(sliderRect, gearItem.weight, 0.1f, 10f, true);
                if (newWeight != gearItem.weight)
                {
                    gearItem.weight = newWeight;
                    kindData.isModified = true;
                    FactionGearEditor.MarkDirty();
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(weightRect, gearItem.weight.ToString("F1"));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            TooltipHandler.TipRegion(rect, new TipSignal(() => FactionGearEditor.GetDetailedItemTooltip(thingDef, kindData), thingDef.GetHashCode()));
        }

        private static void DrawLayerPreview(Rect rect, KindGearData kindData)
        {
            if (kindData == null) return;
            List<ThingDef> apparels = new List<ThingDef>();
            foreach (var item in kindData.apparel) if (item.ThingDef != null) apparels.Add(item.ThingDef);
            foreach (var item in kindData.armors) if (item.ThingDef != null) apparels.Add(item.ThingDef);
            foreach (var item in kindData.others) if (item.ThingDef != null) apparels.Add(item.ThingDef);

            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(4f);
            float lineHeight = 24f; // Increased height for better visibility
            float y = inner.y;
            
            // Header
            Rect headerRect = new Rect(inner.x, y, inner.width, lineHeight);
            DrawCompactLayerHeader(headerRect);
            y += lineHeight;

            // Draw divider
            Widgets.DrawLineHorizontal(inner.x, y, inner.width);
            y += 2f;

            int rowIndex = 0;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Head"), BodyPartGroupDefOf.FullHead, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Torso"), BodyPartGroupDefOf.Torso, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Shoulders"), GroupShoulders, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Arms"), GroupArms, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Hands"), GroupHands, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Waist"), GroupWaist, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Legs"), GroupLegs, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Feet"), GroupFeet, apparels, rowIndex++); y += lineHeight;
        }

        private static void DrawCompactLayerHeader(Rect rect)
        {
            Widgets.DrawHighlight(rect);
            
            float labelWidth = 70f; // Width for the "Part" label column
            float x = rect.x + labelWidth;
            float width = (rect.width - labelWidth) / 5f;
            
            // Draw column headers
            DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), LanguageManager.Get("Skin")); x += width;
            DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), LanguageManager.Get("Mid")); x += width;
            DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), LanguageManager.Get("Shell")); x += width;
            DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), LanguageManager.Get("Belt")); x += width;
            DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), LanguageManager.Get("Over")); x += width;
        }
        
        private static void DrawLayerHeaderLabel(Rect rect, string label)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.gray;
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static void DrawCompactLayerRow(Rect rect, string label, BodyPartGroupDef group, List<ThingDef> apparels, int rowIndex)
        {
            if (group == null) return;
            
            // Use standard RimWorld alternating row background
            if (rowIndex % 2 == 1)
            {
                Widgets.DrawAltRect(rect);
            }

            // Draw row label (Body Part)
            float labelWidth = 70f;
            Rect labelRect = new Rect(rect.x + 4f, rect.y, labelWidth - 4f, rect.height);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            
            // Draw vertical line after label
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineVertical(rect.x + labelWidth, rect.y, rect.height);
            GUI.color = Color.white;

            float x = rect.x + labelWidth;
            float width = (rect.width - labelWidth) / 5f; 
            
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.OnSkin, apparels); x += width;
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.Middle, apparels); x += width;
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.Shell, apparels); x += width;
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.Belt, apparels); x += width;
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.Overhead, apparels); x += width;
        }

        private static void DrawCompactLayerCell(Rect rect, BodyPartGroupDef group, ApparelLayerDef layer, List<ThingDef> apparels)
        {
             if (layer == null) return;
             
             // Draw subtle grid borders
             GUI.color = new Color(1f, 1f, 1f, 0.1f);
             Widgets.DrawBox(rect);
             GUI.color = Color.white;

             var coveringApparel = apparels.FirstOrDefault(a => a.apparel != null && a.apparel.bodyPartGroups.Contains(group) && a.apparel.layers.Contains(layer));
             
             if (coveringApparel != null)
             {
                 Rect inner = rect.ContractedBy(2f);
                 
                 // Draw a subtle background for occupied slots
                 GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                 Widgets.DrawBoxSolid(inner, Color.white);
                 GUI.color = Color.white;
                 
                 Texture2D icon = coveringApparel.uiIcon;
                 if (icon != null)
                 {
                     // Use DrawTextureFitted for better aspect ratio
                     Widgets.DrawTextureFitted(inner, icon, 1f);
                 }
                 
                 // Enhanced Tooltip
                 TooltipHandler.TipRegion(rect, new TipSignal(() => 
                 {
                     string text = coveringApparel.LabelCap;
                     float armorSharp = coveringApparel.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp);
                     float armorBlunt = coveringApparel.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt);
                     if (armorSharp > 0 || armorBlunt > 0)
                     {
                         text += $"\nSharp: {armorSharp:P0}\nBlunt: {armorBlunt:P0}";
                     }
                     return text;
                 }, coveringApparel.GetHashCode()));
                 
                 // Interaction: Click to show info card
                 if (Widgets.ButtonInvisible(rect))
                 {
                     Find.WindowStack.Add(new Dialog_InfoCard(coveringApparel));
                 }
                 
                 Widgets.DrawHighlightIfMouseover(rect);
             }
        }

        private static bool IsBelt(ThingDef t) => t.apparel?.layers?.Contains(ApparelLayerDefOf.Belt) ?? false;

        private static bool IsArmor(ThingDef t)
        {
            if (t.apparel == null) return false;
            if (IsBelt(t)) return false;
            return t.apparel.layers.Contains(ApparelLayerDefOf.Shell) || t.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) > 0.4f;
        }

        private static bool IsApparel(ThingDef t)
        {
            if (t.apparel == null) return false;
            if (IsBelt(t)) return false;
            if (IsArmor(t)) return false;
            return true;
        }

        private static void DrawAdvancedItemSimple(Rect rect, SpecRequirementEdit item, KindGearData kindData)
        {
            if (item.Thing == null) return;
            
            bool isExpanded = (item == EditorSession.ExpandedSpecItem);
            Widgets.DrawHighlightIfMouseover(rect);
            if (item.weight != 1f)
            {
                GUI.color = new Color(1f, 0.8f, 0.4f, 0.3f);
                Widgets.DrawHighlight(rect);
                GUI.color = Color.white;
            }
            
            Rect row1 = new Rect(rect.x, rect.y, rect.width, 28f);
            Rect iconRect = new Rect(row1.x + 4f, row1.y + (row1.height - 24f) / 2f, 24f, 24f);
            float infoButtonOffset = EditorSession.IsInGame ? 28f : 0f;
            Rect infoButtonRect = new Rect(iconRect.xMax + 4f, row1.y + (row1.height - 24f) / 2f, 24f, 24f);
            Rect removeRect = new Rect(row1.xMax - 26f, row1.y + (row1.height - 24f) / 2f, 24f, 24f);
            Rect nameRect = new Rect(infoButtonRect.xMax + 4f, row1.y, removeRect.x - infoButtonRect.xMax - 8f - infoButtonOffset, 28f);

            if (EditorSession.IsInGame)
            {
                Widgets.InfoCardButton(infoButtonRect.x, infoButtonRect.y, item.Thing);
            }
            
            Texture2D icon = FactionGearEditor.GetIconWithLazyLoading(item.Thing);
            if (icon != null) WidgetsUtils.DrawTextureFitted(iconRect, icon, 1f);

            Rect interactRect = new Rect(row1.x, row1.y, row1.width - 30f, 28f);
            if (Widgets.ButtonInvisible(interactRect))
            {
                EditorSession.ExpandedSpecItem = (EditorSession.ExpandedSpecItem == item) ? null : item;
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            float nameWidth = nameRect.width;
            if (!isExpanded && item.weight != 1f) nameWidth -= 45f;
            Widgets.Label(new Rect(nameRect.x, nameRect.y, nameWidth, nameRect.height), item.Thing.LabelCap);
            Text.WordWrap = true;
            if (!isExpanded && item.weight != 1f)
            {
                Text.Anchor = TextAnchor.MiddleRight;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(nameRect.xMax - 45f, row1.y, 45f, 28f), $"{LanguageManager.Get("Weight")}:{item.weight:F1}");
                GUI.color = Color.white;
            }
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonImage(removeRect, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
            {
                UndoManager.RecordState(kindData);
                if (kindData.SpecificApparel != null && kindData.SpecificApparel.Contains(item))
                    kindData.SpecificApparel.Remove(item);
                else if (kindData.SpecificWeapons != null && kindData.SpecificWeapons.Contains(item))
                    kindData.SpecificWeapons.Remove(item);
                
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
                if (EditorSession.ExpandedSpecItem == item) EditorSession.ExpandedSpecItem = null;
            }
            
            if (isExpanded)
            {
                Rect row2 = new Rect(rect.x, rect.y + 28f, rect.width, 28f);
                Rect sliderRect = new Rect(row2.x + 10f, row2.y + 4f, row2.width - 60f, 20f);
                Rect weightRect = new Rect(row2.xMax - 40f, row2.y, 35f, 28f);
                if (Event.current.type == EventType.MouseDown && sliderRect.Contains(Event.current.mousePosition)) UndoManager.RecordState(kindData);
                float newWeight = Widgets.HorizontalSlider(sliderRect, item.weight, 0.1f, 10f, true);
                if (newWeight != item.weight)
                {
                    item.weight = newWeight;
                    kindData.isModified = true;
                    FactionGearEditor.MarkDirty();
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(weightRect, item.weight.ToString("F1"));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            TooltipHandler.TipRegion(rect, LanguageManager.Get("AdvancedItemTooltip"));
        }
    }
}
