using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearModification.UI;
using FactionGearModification; // For SpecRequirementEdit

namespace FactionGearCustomizer.UI.Panels
{
    public static class ApparelCardUI
    {
        private static readonly Color HeaderBgColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color ContentBgColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        private static readonly Color BorderColor = new Color(0.35f, 0.35f, 0.4f);
        private static readonly Color RemoveButtonColor = new Color(0.8f, 0.25f, 0.2f);

        public static void Draw(Listing_Standard ui, SpecRequirementEdit item, int index, Action onRemove)
        {
            float cardHeight = 165f;
            Rect area = ui.GetRect(cardHeight);
            
            DrawCardBackground(area);
            area = area.ContractedBy(6f);

            DrawHeader(area, item, onRemove);
            DrawContent(area, item, index);
            
            ui.Gap(8f);
        }

        private static void DrawCardBackground(Rect area)
        {
            Widgets.DrawBoxSolid(area, ContentBgColor);
            GUI.DrawTexture(area, BaseContent.WhiteTex);
            GUI.color = BorderColor;
            GUI.DrawTexture(area, BaseContent.WhiteTex);
            GUI.color = Color.white;
            Rect innerBorder = area.ContractedBy(1f);
            Widgets.DrawBoxSolid(innerBorder, ContentBgColor);
        }

        private static void DrawHeader(Rect area, SpecRequirementEdit item, Action onRemove)
        {
            Rect headerRect = new Rect(area.x, area.y + 2f, area.width, 28f);
            Widgets.DrawBoxSolid(headerRect, HeaderBgColor);

            // Icon
            Rect iconRect = new Rect(headerRect.x + 6f, headerRect.y + 4f, 20f, 20f);
            Widgets.DrawBoxSolid(iconRect, new Color(0.3f, 0.3f, 0.35f));
            if (item.Thing != null)
            {
                WidgetsUtils.DrawTextureFitted(iconRect, item.Thing.uiIcon, 1f);
            }

            // Title / Item Selector
            Rect titleRect = new Rect(iconRect.xMax + 8f, headerRect.y, headerRect.width - 90f, headerRect.height);
            string titleText = item.Thing?.LabelCap ?? "Select Item";
            
            if (Widgets.ButtonText(titleRect, titleText, false, true, true, TextAnchor.MiddleLeft))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                IEnumerable<ThingDef> candidates = DefDatabase<ThingDef>.AllDefs
                    .Where(t => t.IsApparel || t.IsWeapon)
                    .OrderBy(t => t.label);
                
                foreach (var def in candidates)
                {
                    options.Add(new FloatMenuOption(def.LabelCap, () => 
                    { 
                        item.Thing = def; 
                        FactionGearEditor.MarkDirty(); 
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Remove Button
            DrawRemoveButton(area, onRemove);
        }

        private static void DrawContent(Rect area, SpecRequirementEdit item, int index)
        {
            float contentY = area.y + 36f;
            float rowHeight = 24f;
            float gap = 4f;

            // Row 1: Material
            Rect matRect = new Rect(area.x, contentY, area.width, rowHeight);
            DrawMaterialSelector(matRect, item);
            contentY += rowHeight + gap;

            // Row 2: Quality & Biocode
            Rect qualRect = new Rect(area.x, contentY, area.width * 0.6f, rowHeight);
            DrawQualitySelector(qualRect, item);

            Rect bioRect = new Rect(area.x + area.width * 0.62f, contentY, area.width * 0.38f, rowHeight);
            Widgets.CheckboxLabeled(bioRect, "Biocode", ref item.Biocode);
            contentY += rowHeight + gap;

            // Row 3: Selection Mode
            Rect modeRect = new Rect(area.x, contentY, area.width, rowHeight);
            DrawModeSelector(modeRect, item);
            contentY += rowHeight + gap;

            // Row 4: Chance Slider
            if (item.SelectionMode != ApparelSelectionMode.AlwaysTake)
            {
                Rect chanceRect = new Rect(area.x, contentY, area.width, rowHeight);
                item.SelectionChance = Widgets.HorizontalSlider(chanceRect, item.SelectionChance, 0f, 1f, true, $"Chance: {item.SelectionChance:P0}");
                
                if (Mouse.IsOver(chanceRect))
                {
                    Widgets.DrawBoxSolid(chanceRect.ExpandedBy(2f), new Color(1f, 1f, 1f, 0.05f));
                }
            }
        }

        private static void DrawMaterialSelector(Rect rect, SpecRequirementEdit item)
        {
            string label = item.Material == null ? "Material: Random/Default" : $"Material: {item.Material.LabelCap}";
            if (Widgets.ButtonText(rect, label))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("Random/Default", () => { item.Material = null; FactionGearEditor.MarkDirty(); }));
                
                if (item.Thing != null && item.Thing.MadeFromStuff)
                {
                    foreach (var stuff in GenStuff.AllowedStuffsFor(item.Thing))
                    {
                        options.Add(new FloatMenuOption(stuff.LabelCap, () => { item.Material = stuff; FactionGearEditor.MarkDirty(); }));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void DrawQualitySelector(Rect rect, SpecRequirementEdit item)
        {
            string label = item.Quality.HasValue ? $"Quality: {item.Quality}" : "Quality: Default";
            if (Widgets.ButtonText(rect, label))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("Default", () => { item.Quality = null; FactionGearEditor.MarkDirty(); }));
                foreach (QualityCategory q in Enum.GetValues(typeof(QualityCategory)))
                {
                    options.Add(new FloatMenuOption(q.ToString(), () => { item.Quality = q; FactionGearEditor.MarkDirty(); }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void DrawModeSelector(Rect rect, SpecRequirementEdit item)
        {
            string label = $"Mode: {item.SelectionMode}";
            if (Widgets.ButtonText(rect, label))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (ApparelSelectionMode m in Enum.GetValues(typeof(ApparelSelectionMode)))
                {
                    options.Add(new FloatMenuOption(m.ToString(), () => { item.SelectionMode = m; FactionGearEditor.MarkDirty(); }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void DrawRemoveButton(Rect area, Action onRemove)
        {
            Rect removeBtnRect = new Rect(area.xMax - 70f, area.y + 2f, 64f, 22f);

            Color originalColor = GUI.color;
            bool isMouseOver = Mouse.IsOver(removeBtnRect);
            bool isClicked = Widgets.ButtonInvisible(removeBtnRect);

            if (isClicked)
            {
                onRemove?.Invoke();
            }

            if (isMouseOver)
            {
                GUI.color = RemoveButtonColor;
                Widgets.DrawBoxSolid(removeBtnRect, RemoveButtonColor * 0.3f);
            }
            else
            {
                Widgets.DrawBoxSolid(removeBtnRect, new Color(0.2f, 0.2f, 0.2f));
            }

            Widgets.DrawBox(removeBtnRect, 1);

            Rect labelRect = removeBtnRect.ContractedBy(3f);
            GUI.color = isMouseOver ? Color.white : new Color(0.9f, 0.7f, 0.7f);
            Widgets.Label(labelRect, "Remove");
            GUI.color = originalColor;
        }
    }
}
