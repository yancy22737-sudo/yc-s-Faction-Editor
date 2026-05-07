using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearModification.UI;
using FactionGearCustomizer.Validation;
using FactionGearCustomizer.UI.Dialogs;

namespace FactionGearCustomizer.UI.Panels
{
    public static class HediffCardUI
    {
        private static readonly Color HeaderBgColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color ContentBgColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        private static readonly Color BorderColor = new Color(0.35f, 0.35f, 0.4f);
        private static readonly Color PoolTagColor = new Color(0.4f, 0.7f, 1f);

        private const float HeaderHeight = 28f;
        private const float RowHeight = 24f;
        private const float Gap = 4f;
        private const float InfoHeight = 40f;
        private const float CardPad = 6f;

        private static string Truncate(string text, float maxW)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (Text.CalcSize(text).x <= maxW) return text;
            for (int i = text.Length - 1; i > 0; i--)
                if (Text.CalcSize(text.Substring(0, i) + "...").x <= maxW)
                    return text.Substring(0, i) + "...";
            return "...";
        }

        public static float GetCardHeight(ForcedHediff item)
        {
            if (item.IsPool)
                return HeaderHeight + Gap + RowHeight + CardPad * 2;

            // Header + Row1(selector+chance) + Row2(parts+severity) + Row3(target) + Info
            return HeaderHeight + Gap + RowHeight + Gap + RowHeight + Gap + RowHeight + Gap + InfoHeight + CardPad * 2;
        }

        public static void Draw(Listing_Standard ui, ForcedHediff item, int index, System.Action onRemove, KindGearData kindData = null)
        {
            float cardHeight = GetCardHeight(item);
            Rect area = ui.GetRect(cardHeight);
            DrawCardBg(area);
            Rect inner = area.ContractedBy(CardPad);

            if (item.IsPool)
            {
                DrawPoolCard(inner, item, onRemove, kindData);
            }
            else
            {
                DrawHediffCard(inner, item, onRemove, kindData);
            }

            ui.Gap(Gap);
        }

        private static void DrawCardBg(Rect area)
        {
            Widgets.DrawBoxSolid(area, ContentBgColor);
            GUI.color = BorderColor;
            Widgets.DrawBox(area, 1);
            GUI.color = Color.white;
        }

        // ── Normal hediff card ──────────────────────────────
        private static void DrawHediffCard(Rect inner, ForcedHediff item, System.Action onRemove, KindGearData kindData)
        {
            // Header
            Rect hdr = new Rect(inner.x, inner.y, inner.width, HeaderHeight);
            Widgets.DrawBoxSolid(hdr, HeaderBgColor);

            // Icon
            Rect ico = new Rect(hdr.x + 4f, hdr.y + 4f, 20f, 20f);
            Widgets.DrawBoxSolid(ico, new Color(0.3f, 0.3f, 0.35f));

            // Title (truncated)
            float titleW = hdr.width - 100f;
            Rect title = new Rect(ico.xMax + 6f, hdr.y, titleW, hdr.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(title, Truncate(item.HediffDef?.LabelCap ?? LanguageManager.Get("SelectHediff"), titleW));
            Text.Anchor = TextAnchor.UpperLeft;

            // Type tag (compact)
            DrawTypeTag(hdr, item.HediffDef);

            // Remove button
            Rect del = new Rect(hdr.xMax - 22f, hdr.y + 3f, 22f, 22f);
            if (Widgets.ButtonImage(del, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
                onRemove?.Invoke();

            // Row 1: selector + chance
            float y = hdr.yMax + Gap;
            Rect selBtn = new Rect(inner.x, y, inner.width * 0.55f, RowHeight);
            if (Widgets.ButtonText(selBtn, item.HediffDef?.LabelCap ?? LanguageManager.Get("SelectHediff")))
            {
                Find.WindowStack.Add(new Dialog_HediffPicker(def =>
                {
                    if (kindData != null) { UndoManager.RecordState(kindData); kindData.isModified = true; }
                    item.HediffDef = def;
                    FactionGearEditor.MarkDirty();
                }));
            }
            Rect chanceLbl = new Rect(inner.x + inner.width * 0.57f, y, inner.width * 0.10f, RowHeight);
            Widgets.Label(chanceLbl, LanguageManager.Get("Chance") + ":");
            Rect chanceSld = new Rect(inner.x + inner.width * 0.67f, y + 2f, inner.width * 0.33f, RowHeight - 4f);
            DrawChanceSlider(chanceSld, item, kindData);

            // Row 2: parts range + severity range
            y += RowHeight + Gap;
            Rect partsLbl = new Rect(inner.x, y, inner.width * 0.12f, RowHeight);
            Widgets.Label(partsLbl, LanguageManager.Get("MaxParts") + ":");
            Rect partsRng = new Rect(inner.x + inner.width * 0.13f, y, inner.width * 0.35f, RowHeight);
            DrawPartsRange(partsRng, item, kindData);

            Rect sevLbl = new Rect(inner.x + inner.width * 0.50f, y, inner.width * 0.14f, RowHeight);
            Widgets.Label(sevLbl, LanguageManager.Get("Severity") + ":");
            Rect sevSld = new Rect(inner.x + inner.width * 0.64f, y + 2f, inner.width * 0.36f, RowHeight - 4f);
            DrawSeveritySlider(sevSld, item, kindData);

            // Row 3: target parts
            y += RowHeight + Gap;
            Rect tpLbl = new Rect(inner.x, y, inner.width * 0.12f, RowHeight);
            Widgets.Label(tpLbl, LanguageManager.Get("TargetParts") + ":");
            bool isManual = item.parts != null && item.parts.Count > 0;
            Rect modeBtn = new Rect(inner.x + inner.width * 0.13f, y, inner.width * 0.14f, RowHeight);
            string modeLabel = isManual ? LanguageManager.Get("HediffPartModeManual") : LanguageManager.Get("HediffPartModeAuto");
            if (Widgets.ButtonText(modeBtn, modeLabel) && isManual)
            {
                if (kindData != null) { UndoManager.RecordState(kindData); kindData.isModified = true; }
                item.parts = null;
                FactionGearEditor.MarkDirty();
            }
            Rect tpBtn = new Rect(inner.x + inner.width * 0.28f, y, inner.width * 0.72f, RowHeight);
            if (Widgets.ButtonText(tpBtn, GetTargetPartsSummary(item)))
            {
                var selected = item.parts?.ToList() ?? new List<BodyPartDef>();
                Find.WindowStack.Add(new Dialog_BodyPartSelector(selected, result =>
                {
                    if (kindData != null) { UndoManager.RecordState(kindData); kindData.isModified = true; }
                    item.parts = result != null && result.Count > 0 ? result : null;
                    FactionGearEditor.MarkDirty();
                }));
            }

            // Info section
            y += RowHeight + Gap;
            Rect info = new Rect(inner.x, y, inner.width, InfoHeight);
            Widgets.DrawBoxSolid(info, new Color(0.08f, 0.08f, 0.1f));
            Widgets.DrawBox(info, 1);
            if (item.HediffDef != null)
            {
                string desc = item.HediffDef.description ?? "";
                if (desc.Length > 120) desc = desc.Substring(0, 117) + "...";
                Widgets.Label(info.ContractedBy(4f), desc);
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(info, LanguageManager.Get("SelectHediffToViewDetails"));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        // ── Pool card ────────────────────────────────────────
        private static void DrawPoolCard(Rect inner, ForcedHediff item, System.Action onRemove, KindGearData kindData)
        {
            Rect hdr = new Rect(inner.x, inner.y, inner.width, HeaderHeight);
            Widgets.DrawBoxSolid(hdr, HeaderBgColor);

            Rect ico = new Rect(hdr.x + 4f, hdr.y + 4f, 20f, 20f);
            Widgets.DrawBoxSolid(ico, new Color(0.3f, 0.3f, 0.35f));

            float titleW = hdr.width - 80f;
            Rect title = new Rect(ico.xMax + 6f, hdr.y, titleW, hdr.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(title, Truncate(item.GetDisplayLabel(), titleW));
            Text.Anchor = TextAnchor.UpperLeft;

            // Pool tag
            Rect tag = new Rect(hdr.xMax - 70f, hdr.y + 4f, 46f, 20f);
            Widgets.DrawBoxSolid(tag, PoolTagColor * 0.2f);
            Widgets.DrawBox(tag, 1);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(tag, LanguageManager.Get("HediffPool_Tag"));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect del = new Rect(hdr.xMax - 22f, hdr.y + 3f, 22f, 22f);
            if (Widgets.ButtonImage(del, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
                onRemove?.Invoke();

            // Content row: chance + parts
            float y = hdr.yMax + Gap;
            Rect chanceLbl = new Rect(inner.x, y, inner.width * 0.12f, RowHeight);
            Widgets.Label(chanceLbl, LanguageManager.Get("Chance") + ":");
            Rect chanceSld = new Rect(inner.x + inner.width * 0.13f, y + 2f, inner.width * 0.38f, RowHeight - 4f);
            DrawChanceSlider(chanceSld, item, kindData);

            Rect partsLbl = new Rect(inner.x + inner.width * 0.55f, y, inner.width * 0.15f, RowHeight);
            Widgets.Label(partsLbl, LanguageManager.Get("MaxParts") + ":");
            Rect partsRng = new Rect(inner.x + inner.width * 0.70f, y, inner.width * 0.30f, RowHeight);
            DrawPartsRange(partsRng, item, kindData);
        }

        // ── Shared helpers ───────────────────────────────────
        private static void DrawChanceSlider(Rect rect, ForcedHediff item, KindGearData kindData)
        {
            float old = item.chance;
            float val = Widgets.HorizontalSlider(rect, item.chance, 0f, 1f, true, $"{item.chance * 100:F0}%");
            if (val != old && kindData != null)
            {
                UndoManager.RecordState(kindData);
                item.chance = val;
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
            }
        }

        private static void DrawPartsRange(Rect rect, ForcedHediff item, KindGearData kindData)
        {
            if (item.maxPartsRange == default) item.maxPartsRange = new IntRange(1, 3);
            IntRange old = item.maxPartsRange;
            WidgetsUtils.IntRange(rect, item.GetHashCode() ^ 12340, ref item.maxPartsRange, 1, 10);
            if ((old.min != item.maxPartsRange.min || old.max != item.maxPartsRange.max) && kindData != null)
            {
                UndoManager.RecordState(kindData);
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
            }
        }

        private static void DrawSeveritySlider(Rect rect, ForcedHediff item, KindGearData kindData)
        {
            if (item.severityRange == default) item.severityRange = new FloatRange(0.5f, 1f);
            FloatRange old = item.severityRange;
            FloatRange val = item.severityRange;
            WidgetsUtils.FloatRange(rect, item.GetHashCode() ^ 12341, ref val, 0.01f, 1f);
            if ((old.min != val.min || old.max != val.max) && kindData != null)
            {
                UndoManager.RecordState(kindData);
                item.severityRange = val;
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
            }
        }

        private static void DrawTypeTag(Rect hdr, HediffDef def)
        {
            if (def == null) return;
            string text = "";
            Color col = Color.gray;
            if (def.isBad) { text = LanguageManager.Get("HediffType_Debuff"); col = new Color(0.9f, 0.4f, 0.3f); }
            else if (def.countsAsAddedPartOrImplant) { text = LanguageManager.Get("HediffType_Implant"); col = new Color(0.3f, 0.7f, 0.9f); }
            else if (def.makesSickThought) { text = LanguageManager.Get("HediffType_Illness"); col = new Color(0.9f, 0.8f, 0.3f); }
            if (string.IsNullOrEmpty(text)) return;

            Text.Font = GameFont.Tiny;
            float w = Text.CalcSize(text).x + 8f;
            Text.Font = GameFont.Small;
            Rect tag = new Rect(hdr.xMax - 24f - w - 4f, hdr.y + 4f, w, 20f);
            Widgets.DrawBoxSolid(tag, col * 0.2f);
            Widgets.DrawBox(tag, 1);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(tag, text);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static string GetTargetPartsSummary(ForcedHediff item)
        {
            if (item?.parts == null || item.parts.Count == 0)
                return LanguageManager.Get("HediffPartModeAutoDescription");
            var labels = item.parts.Where(p => p != null).Select(p => string.IsNullOrEmpty(p.label) ? p.defName : p.LabelCap.ToString()).Distinct().ToList();
            if (labels.Count == 0) return LanguageManager.Get("HediffPartModeAutoDescription");
            if (labels.Count <= 3) return string.Join(", ", labels);
            return string.Format(LanguageManager.Get("HediffPartSummaryFormat"), labels[0], labels[1], labels.Count - 2);
        }
    }
}
