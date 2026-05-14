using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Panels
{
    public static class GeneCardUI
    {
        private static readonly Color HeaderBgColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color ContentBgColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        private static readonly Color BorderColor = new Color(0.35f, 0.35f, 0.4f);

        private const float CardPad = 6f;
        private const float RowHeight = 24f;
        private const float HeaderHeight = 28f;

        public static float GetCardHeight(ForcedGene item)
        {
            return HeaderHeight + 8f + RowHeight * 2 + CardPad * 2;
        }

        public static void Draw(Listing_Standard ui, ForcedGene item, int index, System.Action onRemove)
        {
            float cardHeight = GetCardHeight(item);
            Rect area = ui.GetRect(cardHeight);
            Widgets.DrawBoxSolid(area, ContentBgColor);
            GUI.color = BorderColor;
            Widgets.DrawBox(area, 1);
            GUI.color = Color.white;

            Rect inner = area.ContractedBy(CardPad);

            // Header
            Rect hdr = new Rect(inner.x, inner.y, inner.width, HeaderHeight);
            Widgets.DrawBoxSolid(hdr, HeaderBgColor);

            // Vanilla InfoCard button
            if (item.GeneDef != null)
                Widgets.InfoCardButton(hdr.x + 4f, hdr.y + 4f, item.GeneDef);

            // Gene icon (24x24)
            float iconX = hdr.x + 26f;
            if (item.GeneDef?.Icon != null)
            {
                Rect iconR = new Rect(iconX, hdr.y + 2f, 24f, 24f);
                GUI.color = item.GeneDef.IconColor;
                GUI.DrawTexture(iconR, item.GeneDef.Icon, ScaleMode.ScaleToFit);
                GUI.color = Color.white;
            }

            float labelX = hdr.x + (item.GeneDef?.Icon != null ? 54f : 30f);
            string label = item.GeneDef?.LabelCap ?? item.geneDefName ?? "???";
            float labelW = inner.width - (labelX - hdr.x) - 60f;
            Rect labelRect = new Rect(labelX, hdr.y, labelW, HeaderHeight);
            Widgets.Label(labelRect, $"<b>{Truncate(label, labelW)}</b>");

            // Metabolism tag
            if (item.GeneDef != null)
            {
                float met = item.GeneDef.biostatMet;
                string metStr = met > 0 ? $"+{met:F0}" : $"{met:F0}";
                Color metColor = met > 0 ? new Color(1f, 0.4f, 0.3f) : (met < 0 ? new Color(0.3f, 0.85f, 0.3f) : Color.gray);
                Rect metRect = new Rect(hdr.x + labelW + 4f, hdr.y + 2f, 40f, HeaderHeight - 4f);
                GUI.color = metColor;
                Widgets.Label(metRect, metStr);
                GUI.color = Color.white;
            }

            // Remove (trash icon)
            Rect rmBtn = new Rect(hdr.x + inner.width - 30f, hdr.y + 4f, 20f, 20f);
            if (Widgets.ButtonImage(rmBtn, TexButton.Delete))
                onRemove?.Invoke();
            TooltipHandler.TipRegion(rmBtn, LanguageManager.Get("Delete"));

            // Row 1: Chance slider (matching TraitCardUI style)
            float y1 = hdr.yMax + 4f;
            Rect chLabel = new Rect(inner.x + 8f, y1, 50f, RowHeight);
            Widgets.Label(chLabel, LanguageManager.Get("Chance"));
            Rect chSlider = new Rect(chLabel.xMax + 4f, y1, inner.width - 70f - 44f, RowHeight);
            float nc = Widgets.HorizontalSlider(chSlider, item.chance, 0f, 1f, false, null, "0%", "100%");
            Rect chVal = new Rect(chSlider.xMax + 4f, y1, 38f, RowHeight);
            Widgets.Label(chVal, (item.chance * 100f).ToString("F0") + "%");
            if (System.Math.Abs(nc - item.chance) > 0.001f)
            { item.chance = nc; FactionGearEditor.MarkDirty(); }

            // Row 2: Endogene toggle
            float y2 = y1 + RowHeight;
            Rect endoRect = new Rect(inner.x + 8f, y2, 100f, RowHeight);
            bool prevEndogene = item.asEndogene;
            Widgets.CheckboxLabeled(endoRect, LanguageManager.Get("GeneEndogene"), ref item.asEndogene);
            if (item.asEndogene != prevEndogene) FactionGearEditor.MarkDirty();

            ui.Gap(4f);
        }

        private static string Truncate(string text, float maxW)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (Text.CalcSize(text).x <= maxW) return text;
            for (int i = text.Length - 1; i > 0; i--)
                if (Text.CalcSize(text.Substring(0, i) + "...").x <= maxW)
                    return text.Substring(0, i) + "...";
            return "...";
        }
    }
}
