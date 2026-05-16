using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.Managers;

namespace FactionGearCustomizer.UI.Panels
{
    public static class TraitCardUI
    {
        private static readonly Color HeaderBgColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color ContentBgColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        private static readonly Color BorderColor = new Color(0.35f, 0.35f, 0.4f);
        private const float CardPad = 6f;
        private const float RowHeight = 24f;

        public static float GetCardHeight(ForcedTrait item)
        {
            bool hasDegrees = item.TraitDef != null && item.TraitDef.degreeDatas != null && item.TraitDef.degreeDatas.Count > 1;
            return 28f + 8f + RowHeight + (hasDegrees ? RowHeight : 0f) + CardPad * 2;
        }

        public static void Draw(Listing_Standard ui, ForcedTrait item, int index, System.Action onRemove)
        {
            try
            {
                float cardHeight = GetCardHeight(item);
                Rect area = ui.GetRect(cardHeight);
                Widgets.DrawBoxSolid(area, ContentBgColor);
                GUI.color = BorderColor;
                Widgets.DrawBox(area, 1);
                GUI.color = Color.white;

                Rect inner = area.ContractedBy(CardPad);
                Rect hdr = new Rect(inner.x, inner.y, inner.width, 28f);
                Widgets.DrawBoxSolid(hdr, HeaderBgColor);

                // Info button — clickable, opens translated detail popup
                if (item.TraitDef != null)
                {
                    try
                    {
                        Rect ir = new Rect(hdr.x + 4f, hdr.y + 4f, 20f, 20f);
                        var previewPawn = TraitPreviewPawnCache.GetOrCreate(Gender.Male);
                        var previewTrait = TraitPreviewPawnCache.PrepareTrait(item.TraitDef, item.degree);
                        string infoTip = previewTrait != null ? previewTrait.TipString(previewPawn) : item.TraitDef.LabelCap.ToString();
                        if (Widgets.ButtonImage(ir, TexButton.Info))
                            Find.WindowStack.Add(new Dialog_MessageBox(infoTip));
                        TooltipHandler.TipRegion(ir, infoTip);
                    }
                    catch { }
                }

                // Label
                string label = item.DegreeLabel;
                if (string.IsNullOrEmpty(label)) label = item.traitDefName ?? "???";
                Rect labelRect = new Rect(hdr.x + 28f, hdr.y, inner.width - 58f, 28f);
                Widgets.Label(labelRect, "<b>" + Truncate(label, inner.width - 58f) + "</b>");

                // Remove
                Rect rmBtn = new Rect(hdr.x + inner.width - 26f, hdr.y + 4f, 20f, 20f);
                if (Widgets.ButtonImage(rmBtn, TexButton.Delete))
                    onRemove?.Invoke();

                // ── Chance slider row ──
                float yRow = hdr.yMax + 4f;
                Rect chLabel = new Rect(inner.x + 6f, yRow, 50f, RowHeight);
                Widgets.Label(chLabel, LanguageManager.Get("Chance"));
                Rect chSlider = new Rect(chLabel.xMax + 4f, yRow, inner.width - 70f - 44f, RowHeight);
                float nc = Widgets.HorizontalSlider(chSlider, item.chance, 0f, 1f, false, null, "0%", "100%");
                Rect chVal = new Rect(chSlider.xMax + 4f, yRow, 38f, RowHeight);
                Widgets.Label(chVal, (item.chance * 100f).ToString("F0") + "%");
                if (System.Math.Abs(nc - item.chance) > 0.001f)
                {
                    item.chance = nc;
                    FactionGearEditor.MarkDirty();
                }

                // Degree selector (below chance)
                if (item.TraitDef != null && item.TraitDef.degreeDatas != null && item.TraitDef.degreeDatas.Count > 1)
                {
                    float yDeg = yRow + RowHeight + 2f;
                    Rect degRow = new Rect(inner.x + 6f, yDeg, inner.width - 12f, RowHeight);
                    Widgets.Label(new Rect(degRow.x, degRow.y, 55f, RowHeight), LanguageManager.Get("Degree") + ":");
                    for (int d = 0; d < item.TraitDef.degreeDatas.Count; d++)
                    {
                        try
                        {
                            string dl = item.TraitDef.degreeDatas[d].LabelCap.ToString();
                            if (string.IsNullOrEmpty(dl)) dl = item.TraitDef.degreeDatas[d].label ?? ("Lv" + d);
                            Rect btn = new Rect(degRow.x + 58f + d * 70f, degRow.y, 65f, RowHeight);
                            if (Widgets.ButtonText(btn, dl))
                            { item.degree = d; FactionGearEditor.MarkDirty(); }
                            if (item.degree == d) Widgets.DrawHighlight(btn);
                        }
                        catch { }
                    }
                }

                // Tooltip (native translation)
                if (item.TraitDef != null)
                {
                    try
                    {
                        string tip = item.TraitDef.LabelCap.ToString();
                        if (!string.IsNullOrEmpty(item.TraitDef.description))
                            tip += "\n\n" + item.TraitDef.description;
                        TooltipHandler.TipRegion(hdr, tip);
                    }
                    catch { }
                }

                ui.Gap(4f);
            }
            catch { }
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
