using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearModification.UI;

namespace FactionGearCustomizer.UI.Panels
{
    public static class HediffCardUI
    {
        private static readonly Color HeaderBgColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color ContentBgColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        private static readonly Color BorderColor = new Color(0.35f, 0.35f, 0.4f);
        private static readonly Color RemoveButtonColor = new Color(0.8f, 0.25f, 0.2f);
        private static readonly Color InfoBgColor = new Color(0.08f, 0.08f, 0.1f);

        public static float GetCardHeight(ForcedHediff item)
        {
            float height = 36f;
            float rowHeight = 26f;
            float gap = 4f;

            height += rowHeight + gap;

            height += rowHeight + gap;

            height += 50f + gap;

            height += 12f;

            return height;
        }

        public static void Draw(Listing_Standard ui, ForcedHediff item, int index, System.Action onRemove)
        {
            float cardHeight = GetCardHeight(item);
            Rect area = ui.GetRect(cardHeight);
            DrawCardBackground(area);
            area = area.ContractedBy(6f);

            DrawHeader(area, item, index, onRemove);
            DrawContent(area, item, index);
            DrawInfoSection(area, item);
            
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

        private static void DrawHeader(Rect area, ForcedHediff item, int index, System.Action onRemove)
        {
            Rect headerRect = new Rect(area.x, area.y + 2f, area.width, 28f);
            Widgets.DrawBoxSolid(headerRect, HeaderBgColor);

            Rect iconRect = new Rect(headerRect.x + 6f, headerRect.y + 4f, 20f, 20f);
            Widgets.DrawBoxSolid(iconRect, new Color(0.3f, 0.3f, 0.35f));

            Rect titleRect = new Rect(iconRect.xMax + 8f, headerRect.y, headerRect.width - 100f, headerRect.height);
            string titleText = item.HediffDef?.LabelCap ?? LanguageManager.Get("SelectHediff");
            Widgets.Label(titleRect, titleText);

            // Remove Button
            Rect removeRect = new Rect(headerRect.xMax - 24f, headerRect.y + (headerRect.height - 24f) / 2f, 24f, 24f);
            if (Widgets.ButtonImage(removeRect, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
            {
                onRemove?.Invoke();
            }

            DrawTypeTag(headerRect, item.HediffDef);
        }

        private static string GetHediffTypeLabel(HediffDef def)
        {
            if (def == null) return "";
            if (def.isBad) return LanguageManager.Get("HediffType_Debuff");
            if (def.makesSickThought) return LanguageManager.Get("HediffType_Illness");
            if (def.countsAsAddedPartOrImplant) return LanguageManager.Get("HediffType_Implant");
            if (def.defName.Contains("Missing")) return LanguageManager.Get("HediffType_Missing");
            return LanguageManager.Get("HediffType_Other");
        }

        private static void DrawTypeTag(Rect headerRect, HediffDef def)
        {
            if (def == null) return;

            string tagText = "";
            Color tagColor = Color.gray;

            if (def.isBad)
            {
                tagText = LanguageManager.Get("HediffType_Debuff").ToUpper();
                tagColor = new Color(0.9f, 0.4f, 0.3f);
            }
            else if (def.countsAsAddedPartOrImplant)
            {
                tagText = LanguageManager.Get("HediffType_Implant").ToUpper();
                tagColor = new Color(0.3f, 0.7f, 0.9f);
            }
            else if (def.makesSickThought)
            {
                tagText = LanguageManager.Get("HediffType_Illness").ToUpper();
                tagColor = new Color(0.9f, 0.8f, 0.3f);
            }

            if (!string.IsNullOrEmpty(tagText))
            {
                // Move tag to the left of the delete button
                Rect tagRect = new Rect(headerRect.xMax - 24f - 69f, headerRect.y + 4f, 65f, 20f);
                Widgets.DrawBoxSolid(tagRect, tagColor * 0.2f);
                Widgets.DrawBox(tagRect, 1);
                Rect tagLabelRect = tagRect.ContractedBy(2f);
                Widgets.Label(tagLabelRect, tagText);
            }
        }

        private static void DrawContent(Rect area, ForcedHediff item, int index)
        {
            float contentY = area.y + 34f;
            float rowHeight = 26f;

            Rect selectRect = new Rect(area.x, contentY, area.width * 0.58f, rowHeight);
            if (Widgets.ButtonText(selectRect, item.HediffDef?.LabelCap ?? LanguageManager.Get("SelectHediff")))
            {
                Find.WindowStack.Add(new Dialog_HediffPicker(def =>
                {
                    item.HediffDef = def;
                    FactionGearEditor.MarkDirty();
                }));
            }

            Rect chanceLabelRect = new Rect(area.x + area.width * 0.60f, contentY, area.width * 0.15f, rowHeight);
            Widgets.Label(chanceLabelRect, LanguageManager.Get("Chance") + ":");
            Rect chanceSliderRect = new Rect(area.x + area.width * 0.75f, contentY + 4f, area.width * 0.25f, rowHeight - 8f);
            float newChance = Widgets.HorizontalSlider(chanceSliderRect, item.chance, 0f, 1f, true, $"{(item.chance * 100):F0}%");
            item.chance = newChance;

            if (Mouse.IsOver(chanceSliderRect))
            {
                Widgets.DrawBoxSolid(chanceSliderRect.ExpandedBy(4f), new Color(1f, 1f, 1f, 0.05f));
            }

            float partsY = contentY + rowHeight + 4f;
            Rect partsRect = new Rect(area.x, partsY, area.width * 0.35f, rowHeight);
            Widgets.Label(partsRect, LanguageManager.Get("MaxParts") + ":");
            IntRange partsRange = item.maxPartsRange;
            if (partsRange == default(IntRange))
            {
                partsRange = new IntRange(1, 3);
                item.maxPartsRange = partsRange;
            }
            WidgetsUtils.IntRange(new Rect(area.x + area.width * 0.28f, partsY, area.width * 0.30f, rowHeight), item.GetHashCode() ^ 12340, ref item.maxPartsRange, 1, 10);

            Rect severityLabelRect = new Rect(area.x + area.width * 0.62f, partsY, area.width * 0.15f, rowHeight);
            Widgets.Label(severityLabelRect, LanguageManager.Get("Severity") + ":");
            Rect severitySliderRect = new Rect(area.x + area.width * 0.77f, partsY + 4f, area.width * 0.23f, rowHeight - 8f);
            if (item.severityRange == default)
            {
                item.severityRange = new FloatRange(0.5f, 1f);
            }
            FloatRange newSeverity = item.severityRange;
            WidgetsUtils.FloatRange(severitySliderRect, item.GetHashCode() ^ 12341, ref newSeverity, 0.01f, 1f);
            item.severityRange = newSeverity;
        }

        private static void DrawInfoSection(Rect area, ForcedHediff item)
        {
            float infoY = area.y + 94f;
            Rect infoRect = new Rect(area.x, infoY, area.width, 50f);
            Widgets.DrawBoxSolid(infoRect, InfoBgColor);
            Widgets.DrawBox(infoRect, 1);

            if (item.HediffDef != null)
            {
                Rect descRect = infoRect.ContractedBy(6f);
                string descText = item.HediffDef.description;
                if (string.IsNullOrEmpty(descText))
                {
                    descText = LanguageManager.Get("NoDescription");
                }
                if (descText.Length > 150)
                {
                    descText = descText.Substring(0, 147) + "...";
                }
                Widgets.Label(descRect, descText);

                Rect detailRect = new Rect(area.x, infoRect.yMax - 18f, area.width, 16f);
                string detailText = $"{LanguageManager.Get("TypePrefix")}: {GetHediffTypeLabel(item.HediffDef)}";
                if (item.HediffDef.lethalSeverity > 0)
                {
                    detailText += $" | {LanguageManager.Get("HediffLethal")}: {item.HediffDef.lethalSeverity:F1}";
                }
                Widgets.Label(detailRect, detailText);
            }
            else
            {
                Rect placeholderRect = infoRect.ContractedBy(6f);
                Widgets.Label(placeholderRect, LanguageManager.Get("SelectHediffToViewDetails"));
            }
        }

        private static void ShowHediffSelectionMenu(ForcedHediff item)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            var hediffGroups = DefDatabase<HediffDef>.AllDefs
                .Where(h => h.isBad || h.makesSickThought || h.countsAsAddedPartOrImplant)
                .OrderBy(d => d.label)
                .GroupBy(d => GetHediffCategory(d));

            foreach (var group in hediffGroups)
            {
                options.Add(new FloatMenuOption(group.Key, null));

                foreach (var def in group)
                {
                    string label = "  " + def.LabelCap;
                    options.Add(new FloatMenuOption(label, () =>
                    {
                        item.HediffDef = def;
                        FactionGearEditor.MarkDirty();
                    }));
                }
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static string GetHediffCategory(HediffDef def)
        {
            if (def.countsAsAddedPartOrImplant) return LanguageManager.Get("HediffType_Implant");
            if (def.isBad && def.defName.Contains("Missing")) return LanguageManager.Get("HediffType_Missing");
            if (def.isBad) return LanguageManager.Get("HediffType_Debuff");
            if (def.makesSickThought) return LanguageManager.Get("HediffType_Illness");
            return LanguageManager.Get("HediffType_Other");
        }

        private static Color GetHediffCategoryColor(HediffDef def)
        {
            if (def.countsAsAddedPartOrImplant) return new Color(0.5f, 0.8f, 1f);
            if (def.isBad) return new Color(1f, 0.6f, 0.5f);
            if (def.makesSickThought) return new Color(1f, 0.9f, 0.5f);
            return Color.gray;
        }
    }
}
