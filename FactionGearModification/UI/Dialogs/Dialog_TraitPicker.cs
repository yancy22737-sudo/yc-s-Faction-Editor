using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.Managers;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_TraitPicker : Window
    {
        private readonly List<ForcedTrait> targetList;
        private List<TraitDef> allTraits = new List<TraitDef>();
        private HashSet<TraitDef> selected = new HashSet<TraitDef>();
        private HashSet<string> existingDefNames;
        private Vector2 scrollPos;
        private string searchText = "";
        private bool skipExisting = true;
        private const float RowHeight = 30f;

        public override Vector2 InitialSize => new Vector2(640f, 720f);

        public Dialog_TraitPicker(List<ForcedTrait> targetList)
        {
            this.targetList = targetList;
            doCloseX = true; forcePause = true;
            absorbInputAroundWindow = true; draggable = true; resizeable = true;
            try
            {
                allTraits = DefDatabase<TraitDef>.AllDefs
                    .Select(t =>
                    {
                        try { return DefDatabase<TraitDef>.GetNamedSilentFail(t.defName); }
                        catch { return null; }
                    })
                    .Where(t => t != null)
                    .OrderBy(t => { try { return t.LabelCap.ToString(); } catch { return t.defName ?? ""; } })
                    .ToList();
            }
            catch { }
            try
            {
                existingDefNames = new HashSet<string>(targetList.Where(x => { try { return x?.TraitDef != null; } catch { return false; } }).Select(x => { try { return x.TraitDef.defName; } catch { return ""; } }));
            }
            catch { existingDefNames = new HashSet<string>(); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                float y = 0f;
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0f, y, inRect.width, 35f), LanguageManager.Get("TraitPickerTitle"));
                Text.Font = GameFont.Small; y += 40f;

                Rect sr = new Rect(0f, y, inRect.width, 28f);
                string ps = searchText; searchText = Widgets.TextField(sr, searchText);
                if (searchText != ps) scrollPos = Vector2.zero;
                y += 34f;

                if (existingDefNames.Count > 0)
                {
                    Widgets.CheckboxLabeled(new Rect(0f, y, inRect.width, 24f), LanguageManager.Get("SkipExistingItems"), ref skipExisting);
                    y += 28f;
                }

                Rect listR = new Rect(0f, y, inRect.width, inRect.height - y - 40f);
                DrawList(listR);
                DrawBottom(inRect);
            }
            catch { }
        }

        private void DrawList(Rect rect)
        {
            try
            {
                Widgets.DrawMenuSection(rect);
                Rect inner = rect.ContractedBy(4f);
                if (inner.width < 10f || inner.height < 10f) return;
                string q = (searchText ?? "").Trim().ToLowerInvariant();
                var filtered = allTraits;
                if (allTraits == null) return;
                if (!string.IsNullOrEmpty(q))
                    filtered = allTraits.Where(t =>
                    {
                        try { string l = t.LabelCap.ToString(); if (string.IsNullOrEmpty(l)) l = t.defName ?? ""; return l.ToLowerInvariant().Contains(q) || (t.defName ?? "").ToLowerInvariant().Contains(q); }
                        catch { return (t.defName ?? "").ToLowerInvariant().Contains(q); }
                    }).ToList();

                float ch = Mathf.Max(filtered.Count * RowHeight, 1f);
                float vw = Mathf.Max(inner.width - 16f, 1f);
                Rect vr = new Rect(0f, 0f, vw, ch);
                int first = Mathf.Max(0, Mathf.FloorToInt(scrollPos.y / RowHeight) - 1);
                int vis = Mathf.CeilToInt(inner.height / RowHeight) + 3;
                int last = Mathf.Min(filtered.Count, first + vis);

                Widgets.BeginScrollView(inner, ref scrollPos, vr);

            for (int i = first; i < last; i++)
            {
                var t = filtered[i];
                try
                {
                    Rect row = new Rect(0f, i * RowHeight, vr.width, RowHeight);
                    if (i % 2 == 1) Widgets.DrawAltRect(row);
                    if (Mouse.IsOver(row)) Widgets.DrawHighlight(row);

                    // Build translated display label via ForcedTrait logic (same path as card UI)
                    var tempForced = new ForcedTrait { TraitDef = t, degree = 0, chance = 1f };
                    string label = tempForced.DegreeLabel;
                    if (string.IsNullOrEmpty(label)) label = t.defName;
                    bool exists = existingDefNames.Contains(t.defName);

                    // Info button keeps richer preview-pawn path
                    string infoTip = label;
                    try
                    {
                        var previewPawn = TraitPreviewPawnCache.GetOrCreate(Gender.Male);
                        var previewTrait = TraitPreviewPawnCache.PrepareTrait(t, 0);
                        if (previewTrait != null)
                        {
                            string tip = previewTrait.TipString(previewPawn);
                            if (!string.IsNullOrEmpty(tip)) infoTip = tip;
                        }
                    }
                    catch { }
                    if (string.IsNullOrEmpty(infoTip))
                    {
                        infoTip = label;
                        if (!string.IsNullOrEmpty(t.description))
                            infoTip += "\n\n" + t.description;
                    }

                    // Info button (clickable, same popup content path as card)
                    Rect ir = new Rect(row.x + 2f, row.y + 5f, 20f, 20f);
                    if (Widgets.ButtonImage(ir, TexButton.Info))
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox(infoTip));
                    }
                    TooltipHandler.TipRegion(ir, infoTip);

                    string modSource = "Unknown";
                    try { modSource = t.modContentPack?.Name ?? "Unknown"; } catch { }
                    modSource = Truncate(modSource, 78f);

                    float cx = row.x + 26f;
                    if (exists && skipExisting)
                    {
                        GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                        Widgets.Label(new Rect(cx, row.y, row.width - cx - 90f, RowHeight), label + " (" + LanguageManager.Get("AlreadyAdded") + ")");
                        GUI.color = Color.white;
                    }
                    else
                    {
                        bool sel = selected.Contains(t);
                        Widgets.CheckboxLabeled(new Rect(cx, row.y, row.width - cx - 90f, RowHeight), label, ref sel, false);
                        if (sel != selected.Contains(t)) { if (sel) selected.Add(t); else selected.Remove(t); }
                    }

                    // Mod source column (same pattern as item pickers)
                    GUI.color = Color.gray;
                    Text.Anchor = TextAnchor.MiddleRight;
                    Widgets.Label(new Rect(row.x + row.width - 84f, row.y, 80f, RowHeight), modSource);
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;

                    // Tooltip: safe pure-text path (no preview pawn mutation on hover)
                    string rowTip = label;
                    try
                    {
                        if (!string.IsNullOrEmpty(t.description))
                            rowTip += "\n\n" + t.description;
                        if (t.degreeDatas != null && t.degreeDatas.Count > 1)
                        {
                            rowTip += "\n\n" + LanguageManager.Get("Degree") + ": ";
                            var parts = new List<string>();
                            for (int d = 0; d < t.degreeDatas.Count; d++)
                            {
                                string dl = "Lv" + d;
                                try { dl = t.degreeDatas[d].LabelCap.ToString(); if (string.IsNullOrEmpty(dl)) dl = t.degreeDatas[d].label ?? ("Lv" + d); } catch { }
                                parts.Add(dl);
                            }
                            rowTip += string.Join(", ", parts);
                        }
                    }
                    catch { }
                    TooltipHandler.TipRegion(row, rowTip);
                }
                catch { }
            }
            Widgets.EndScrollView();
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

        private void DrawBottom(Rect inRect)
        {
            Rect br = new Rect(0f, inRect.height - 40f, inRect.width, 40f);
            GUI.BeginGroup(br);
            float bw = (br.width - 20f) / 2f;
            if (Widgets.ButtonText(new Rect(0f, 4f, bw, 32f), LanguageManager.Get("AddSelected"))) AddSelected();
            if (Widgets.ButtonText(new Rect(bw + 16f, 4f, bw, 32f), LanguageManager.Get("Cancel"))) Close();
            GUI.EndGroup();
        }

        private void AddSelected()
        {
            int added = 0, skipped = 0;
            foreach (var def in selected)
            {
                if (existingDefNames.Contains(def.defName)) { skipped++; continue; }
                targetList.Add(new ForcedTrait { TraitDef = def, degree = 0, chance = 1f });
                added++;
            }
            if (added > 0) FactionGearEditor.MarkDirty();
            Messages.Message(LanguageManager.Get("TraitsAddedMessage", added, skipped), MessageTypeDefOf.PositiveEvent, false);
            selected.Clear();
        }
    }
}
