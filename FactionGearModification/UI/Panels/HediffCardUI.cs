using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearModification.UI;
using FactionGearCustomizer.Validation;

namespace FactionGearCustomizer.UI.Panels
{
    public static class HediffCardUI
    {
        private static readonly Color HeaderBgColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color ContentBgColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        private static readonly Color BorderColor = new Color(0.35f, 0.35f, 0.4f);
        private static readonly Color RemoveButtonColor = new Color(0.8f, 0.25f, 0.2f);
        private static readonly Color InfoBgColor = new Color(0.08f, 0.08f, 0.1f);
        private static readonly Color PoolTagColor = new Color(0.4f, 0.7f, 1f);

        private const float HeaderHeight = 28f;
        private const float HeaderPadding = 2f;
        private const float RowHeight = 26f;
        private const float RowGap = 4f;
        private const float InfoSectionHeight = 50f;
        private const float BottomPadding = 12f;
        private const float CardPadding = 6f;

        public static float GetCardHeight(ForcedHediff item)
        {
            if (item.IsPool)
            {
                return HeaderPadding + HeaderHeight + RowGap + RowHeight + BottomPadding + CardPadding * 2;
            }
            
            return HeaderPadding + HeaderHeight + RowGap + 
                   RowHeight + RowGap + 
                   RowHeight + RowGap + 
                   InfoSectionHeight + BottomPadding + CardPadding * 2;
        }

        public static void Draw(Listing_Standard ui, ForcedHediff item, int index, System.Action onRemove, KindGearData kindData = null)
        {
            float cardHeight = GetCardHeight(item);
            Rect area = ui.GetRect(cardHeight);
            DrawCardBackground(area);
            area = area.ContractedBy(6f);

            if (item.IsPool)
            {
                DrawPoolHeader(area, item, onRemove);
                DrawPoolContent(area, item, kindData);
            }
            else
            {
                DrawHeader(area, item, index, onRemove);
                DrawContent(area, item, index, kindData);
                DrawInfoSection(area, item);
            }
            
            ui.Gap(8f);
        }

        private static void DrawCardBackground(Rect area)
        {
            Widgets.DrawBoxSolid(area, ContentBgColor);
            GUI.color = BorderColor;
            Widgets.DrawBox(area, 1);
            GUI.color = Color.white;
        }

        private static void DrawHeader(Rect area, ForcedHediff item, int index, System.Action onRemove)
        {
            Rect headerRect = new Rect(area.x, area.y + HeaderPadding, area.width, HeaderHeight);
            Widgets.DrawBoxSolid(headerRect, HeaderBgColor);

            Rect iconRect = new Rect(headerRect.x + 6f, headerRect.y + 4f, 20f, 20f);
            Widgets.DrawBoxSolid(iconRect, new Color(0.3f, 0.3f, 0.35f));

            float rightReserve = 95f;
            Rect titleRect = new Rect(iconRect.xMax + 8f, headerRect.y, headerRect.width - rightReserve, headerRect.height);
            string titleText = item.HediffDef?.LabelCap ?? LanguageManager.Get("SelectHediff");
            Widgets.Label(titleRect, titleText);

            float warningStartX = titleRect.xMax + 4f;
            float warningMaxWidth = headerRect.xMax - rightReserve - warningStartX - 4f;

            if (item.HediffDef != null)
            {
                FloatRange severityRange = item.severityRange;
                if (severityRange == default)
                    severityRange = new FloatRange(0.5f, 1f);
                    
                var warnings = HediffWarningChecker.CheckSingleHediffWithSeverity(item.HediffDef, severityRange);
                if (warnings.Count > 0)
                {
                    float iconSize = 16f;
                    float spacing = 2f;
                    float currentX = warningStartX;
                    int maxWarnings = Mathf.FloorToInt(warningMaxWidth / (iconSize + spacing));
                    int warningCount = Mathf.Min(warnings.Count, maxWarnings);
                    
                    for (int i = 0; i < warningCount; i++)
                    {
                        var warning = warnings[i];
                        Rect warningRect = new Rect(currentX, headerRect.y + 6f, iconSize, iconSize);
                        HediffWarningUI.DrawWarningIcon(warningRect, warning);
                        currentX += iconSize + spacing;
                    }
                    
                    if (warnings.Count > maxWarnings)
                    {
                        Rect moreRect = new Rect(currentX, headerRect.y + 6f, iconSize, iconSize);
                        GUI.color = new Color(0.7f, 0.7f, 0.7f);
                        Widgets.Label(moreRect, $"+{warnings.Count - maxWarnings}");
                        GUI.color = Color.white;
                    }
                }
            }

            Rect removeRect = new Rect(headerRect.xMax - 24f, headerRect.y + (headerRect.height - 24f) / 2f, 24f, 24f);
            if (Widgets.ButtonImage(removeRect, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
            {
                onRemove?.Invoke();
            }

            DrawTypeTag(headerRect, item.HediffDef);
        }

        private static void DrawPoolHeader(Rect area, ForcedHediff item, System.Action onRemove)
        {
            Rect headerRect = new Rect(area.x, area.y + HeaderPadding, area.width, HeaderHeight);
            Widgets.DrawBoxSolid(headerRect, HeaderBgColor);

            Rect iconRect = new Rect(headerRect.x + 6f, headerRect.y + 4f, 20f, 20f);
            Widgets.DrawBoxSolid(iconRect, new Color(0.3f, 0.3f, 0.35f));

            Rect titleRect = new Rect(iconRect.xMax + 8f, headerRect.y, headerRect.width - 100f, headerRect.height);
            string titleText = item.GetDisplayLabel();
            Widgets.Label(titleRect, titleText);

            Rect removeRect = new Rect(headerRect.xMax - 24f, headerRect.y + (headerRect.height - 24f) / 2f, 24f, 24f);
            if (Widgets.ButtonImage(removeRect, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
            {
                onRemove?.Invoke();
            }

            DrawPoolTag(headerRect);
        }

        private static void DrawPoolTag(Rect headerRect)
        {
            string tagText = LanguageManager.Get("HediffPool_Tag").ToUpper();
            Rect tagRect = new Rect(headerRect.xMax - 24f - 55f, headerRect.y + 4f, 50f, 20f);
            Widgets.DrawBoxSolid(tagRect, PoolTagColor * 0.2f);
            Widgets.DrawBox(tagRect, 1);
            Rect tagLabelRect = tagRect.ContractedBy(2f);
            Widgets.Label(tagLabelRect, tagText);
        }

        private static void DrawPoolContent(Rect area, ForcedHediff item, KindGearData kindData)
        {
            float contentY = area.y + HeaderPadding + HeaderHeight + RowGap;

            Rect chanceLabelRect = new Rect(area.x, contentY, area.width * 0.15f, RowHeight);
            Widgets.Label(chanceLabelRect, LanguageManager.Get("Chance") + ":");
            Rect chanceSliderRect = new Rect(area.x + area.width * 0.16f, contentY + 4f, area.width * 0.40f, RowHeight - 8f);
            
            float oldChance = item.chance;
            float newChance = Widgets.HorizontalSlider(chanceSliderRect, item.chance, 0f, 1f, true, $"{(item.chance * 100):F0}%");
            if (newChance != oldChance && kindData != null)
            {
                UndoManager.RecordState(kindData);
                item.chance = newChance;
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
            }

            Rect partsRect = new Rect(area.x + area.width * 0.60f, contentY, area.width * 0.15f, RowHeight);
            Widgets.Label(partsRect, LanguageManager.Get("MaxParts") + ":");
            IntRange partsRange = item.maxPartsRange;
            if (partsRange == default(IntRange))
            {
                partsRange = new IntRange(1, 3);
                item.maxPartsRange = partsRange;
            }
            IntRange oldPartsRange = item.maxPartsRange;
            WidgetsUtils.IntRange(new Rect(area.x + area.width * 0.75f, contentY, area.width * 0.25f, RowHeight), item.GetHashCode() ^ 12340, ref item.maxPartsRange, 1, 10);
            if ((oldPartsRange.min != item.maxPartsRange.min || oldPartsRange.max != item.maxPartsRange.max) && kindData != null)
            {
                UndoManager.RecordState(kindData);
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
            }
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
                Rect tagRect = new Rect(headerRect.xMax - 24f - 69f, headerRect.y + 4f, 65f, 20f);
                Widgets.DrawBoxSolid(tagRect, tagColor * 0.2f);
                Widgets.DrawBox(tagRect, 1);
                Rect tagLabelRect = tagRect.ContractedBy(2f);
                Widgets.Label(tagLabelRect, tagText);
            }
        }

        private static void DrawContent(Rect area, ForcedHediff item, int index, KindGearData kindData)
        {
            float contentY = area.y + HeaderPadding + HeaderHeight + RowGap;

            Rect selectRect = new Rect(area.x, contentY, area.width * 0.55f, RowHeight);
            if (Widgets.ButtonText(selectRect, item.HediffDef?.LabelCap ?? LanguageManager.Get("SelectHediff")))
            {
                Find.WindowStack.Add(new Dialog_HediffPicker(def =>
                {
                    if (kindData != null)
                    {
                        UndoManager.RecordState(kindData);
                        kindData.isModified = true;
                    }
                    item.HediffDef = def;
                    FactionGearEditor.MarkDirty();
                }));
            }

            Rect chanceLabelRect = new Rect(area.x + area.width * 0.57f, contentY, area.width * 0.13f, RowHeight);
            Widgets.Label(chanceLabelRect, LanguageManager.Get("Chance") + ":");
            Rect chanceSliderRect = new Rect(area.x + area.width * 0.70f, contentY + 4f, area.width * 0.30f, RowHeight - 8f);
            
            float oldChance = item.chance;
            float newChance = Widgets.HorizontalSlider(chanceSliderRect, item.chance, 0f, 1f, true, $"{(item.chance * 100):F0}%");
            if (newChance != oldChance && kindData != null)
            {
                UndoManager.RecordState(kindData);
                item.chance = newChance;
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
            }

            float partsY = contentY + RowHeight + RowGap;
            Rect partsLabelRect = new Rect(area.x, partsY, area.width * 0.15f, RowHeight);
            Widgets.Label(partsLabelRect, LanguageManager.Get("MaxParts") + ":");
            
            IntRange partsRange = item.maxPartsRange;
            if (partsRange == default(IntRange))
            {
                partsRange = new IntRange(1, 3);
                item.maxPartsRange = partsRange;
            }
            IntRange oldPartsRange = item.maxPartsRange;
            WidgetsUtils.IntRange(new Rect(area.x + area.width * 0.16f, partsY, area.width * 0.32f, RowHeight), item.GetHashCode() ^ 12340, ref item.maxPartsRange, 1, 10);
            if ((oldPartsRange.min != item.maxPartsRange.min || oldPartsRange.max != item.maxPartsRange.max) && kindData != null)
            {
                UndoManager.RecordState(kindData);
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
            }

            Rect severityLabelRect = new Rect(area.x + area.width * 0.50f, partsY, area.width * 0.15f, RowHeight);
            Widgets.Label(severityLabelRect, LanguageManager.Get("Severity") + ":");
            Rect severitySliderRect = new Rect(area.x + area.width * 0.65f, partsY, area.width * 0.35f, RowHeight);
            if (item.severityRange == default)
            {
                item.severityRange = new FloatRange(0.5f, 1f);
            }
            FloatRange oldSeverity = item.severityRange;
            FloatRange newSeverity = item.severityRange;
            WidgetsUtils.FloatRange(severitySliderRect, item.GetHashCode() ^ 12341, ref newSeverity, 0.01f, 1f);
            if ((oldSeverity.min != newSeverity.min || oldSeverity.max != newSeverity.max) && kindData != null)
            {
                UndoManager.RecordState(kindData);
                item.severityRange = newSeverity;
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
            }
        }

        private static void DrawInfoSection(Rect area, ForcedHediff item)
        {
            float infoY = area.y + HeaderPadding + HeaderHeight + RowGap + RowHeight + RowGap + RowHeight + RowGap;
            Rect infoRect = new Rect(area.x, infoY, area.width, InfoSectionHeight);
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
    }
}
