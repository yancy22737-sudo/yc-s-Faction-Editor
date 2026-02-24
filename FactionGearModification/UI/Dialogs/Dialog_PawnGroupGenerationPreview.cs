using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearModification.UI;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_PawnGroupGenerationPreview : Window
    {
        private enum SectionKind
        {
            Options,
            Traders,
            Carriers,
            Guards
        }

        private class Entry
        {
            public PawnKindDef Kind;
            public string KindDefName;
            public int Count;
            public float Weight;
        }

        private readonly PawnGroupMakerData groupData;
        private readonly FactionDef factionDef;

        private Vector2 scrollPos;
        private bool generatePortraits = true;
        private int seed;
        private string seedBuffer;
        private int drawsPerList = 10;
        private string drawsBuffer = "10";

        private Dictionary<SectionKind, List<Entry>> sections = new Dictionary<SectionKind, List<Entry>>();

        private Dictionary<PawnKindDef, Pawn> previewPawns = new Dictionary<PawnKindDef, Pawn>();
        private Dictionary<PawnKindDef, string> previewErrors = new Dictionary<PawnKindDef, string>();
        private Queue<PawnKindDef> generationQueue = new Queue<PawnKindDef>();
        private HashSet<PawnKindDef> pendingKinds = new HashSet<PawnKindDef>();
        private const int BATCH_SIZE = 1;

        private static System.Reflection.FieldInfo defaultFactionTypeField;

        public override Vector2 InitialSize => new Vector2(820f, 720f);

        public Dialog_PawnGroupGenerationPreview(PawnGroupMakerData groupData, FactionDef factionDef)
        {
            this.groupData = groupData;
            this.factionDef = factionDef;
            this.doCloseX = true;
            this.forcePause = true;
            this.resizeable = true;
            this.draggable = true;

            seed = Rand.Int;
            seedBuffer = seed.ToString();

            if (defaultFactionTypeField == null)
            {
                defaultFactionTypeField = typeof(PawnKindDef).GetField("defaultFactionType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            RebuildPreview();
        }

        public override void PreClose()
        {
            base.PreClose();
            DestroyAllPreviewPawns();
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            if (generatePortraits && generationQueue.Count > 0)
            {
                ProcessGenerationQueue();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 10f, 32f), $"{LanguageManager.Get("PawnGroupPreviewTitle")}: {GetGroupDisplayLabel()}");
            Text.Font = GameFont.Small;

            Rect controlsRect = new Rect(inRect.x, inRect.y + 38f, inRect.width, 34f);
            DrawControls(controlsRect);

            Rect outRect = new Rect(inRect.x, controlsRect.yMax + 6f, inRect.width, inRect.height - (controlsRect.yMax + 6f - inRect.y));
            DrawSections(outRect);
        }

        private void DrawControls(Rect rect)
        {
            float x = rect.x;
            float y = rect.y;

            float labelW = 52f;
            float fieldW = 110f;
            float btnW = 110f;
            float gap = 8f;

            Widgets.Label(new Rect(x, y + 6f, labelW, 24f), LanguageManager.Get("Seed"));
            x += labelW + 4f;

            seedBuffer = Widgets.TextField(new Rect(x, y + 4f, fieldW, 26f), seedBuffer ?? "");
            x += fieldW + gap;

            if (Widgets.ButtonText(new Rect(x, y + 4f, btnW, 26f), LanguageManager.Get("Reroll")))
            {
                seed = Rand.Int;
                seedBuffer = seed.ToString();
                RebuildPreview();
            }
            x += btnW + gap;

            Widgets.Label(new Rect(x, y + 6f, 90f, 24f), LanguageManager.Get("DrawsPerList"));
            x += 90f + 4f;

            drawsBuffer = Widgets.TextField(new Rect(x, y + 4f, 60f, 26f), drawsBuffer ?? "");
            x += 60f + gap;

            if (Widgets.ButtonText(new Rect(x, y + 4f, btnW, 26f), LanguageManager.Get("RebuildPreview")))
            {
                RebuildPreview();
            }
            x += btnW + gap;

            Rect toggleRect = new Rect(x, y + 6f, rect.xMax - x, 24f);
            bool newGeneratePortraits = generatePortraits;
            Widgets.CheckboxLabeled(toggleRect, LanguageManager.Get("GeneratePortraits"), ref newGeneratePortraits);
            if (newGeneratePortraits != generatePortraits)
            {
                generatePortraits = newGeneratePortraits;
                if (generatePortraits)
                    StartPawnGenerationQueue();
                else
                    DestroyAllPreviewPawns();
            }
        }

        private void DrawSections(Rect rect)
        {
            float viewHeight = 0f;
            viewHeight += GetSectionHeight(SectionKind.Options);
            viewHeight += GetSectionHeight(SectionKind.Traders);
            viewHeight += GetSectionHeight(SectionKind.Carriers);
            viewHeight += GetSectionHeight(SectionKind.Guards);

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(viewHeight, rect.height));
            Widgets.BeginScrollView(rect, ref scrollPos, viewRect);

            float curY = 0f;
            curY = DrawSection(new Rect(0f, curY, viewRect.width, 0f), SectionKind.Options, LanguageManager.Get("GroupOptions"));
            curY = DrawSection(new Rect(0f, curY, viewRect.width, 0f), SectionKind.Traders, LanguageManager.Get("Traders"));
            curY = DrawSection(new Rect(0f, curY, viewRect.width, 0f), SectionKind.Carriers, LanguageManager.Get("Carriers"));
            curY = DrawSection(new Rect(0f, curY, viewRect.width, 0f), SectionKind.Guards, LanguageManager.Get("Guards"));

            Widgets.EndScrollView();
        }

        private float GetSectionHeight(SectionKind kind)
        {
            sections.TryGetValue(kind, out var list);
            int count = list?.Count ?? 0;
            float header = 34f;
            float rows = Mathf.Max(1, count) * 54f;
            float padding = 10f;
            return header + rows + padding;
        }

        private float DrawSection(Rect rect, SectionKind kind, string title)
        {
            float headerH = 30f;
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, headerH);
            Widgets.DrawMenuSection(headerRect);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(headerRect.ContractedBy(8f, 0f), $"<b>{title}</b>");
            Text.Anchor = TextAnchor.UpperLeft;

            // Add "Add Kind Def" button in header
            float addBtnWidth = 100f;
            Rect addBtnRect = new Rect(headerRect.xMax - addBtnWidth - 8f, headerRect.y + 3f, addBtnWidth, 24f);
            if (Widgets.ButtonText(addBtnRect, LanguageManager.Get("AddKindDef")))
            {
                OpenKindDefPicker(kind);
            }

            sections.TryGetValue(kind, out var entries);
            if (entries == null) entries = new List<Entry>();

            float y = headerRect.yMax + 4f;
            if (entries.Count == 0)
            {
                Rect emptyRect = new Rect(rect.x, y, rect.width, 54f);
                Widgets.DrawMenuSection(emptyRect);
                Widgets.Label(emptyRect.ContractedBy(8f, 0f), LanguageManager.Get("NoItemsInThisCategory"));
                return emptyRect.yMax + 10f;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                Rect row = new Rect(rect.x, y, rect.width, 50f);
                Widgets.DrawMenuSection(row);

                if (i % 2 == 1) Widgets.DrawAltRect(row);
                if (Mouse.IsOver(row)) Widgets.DrawHighlight(row);

                float x = row.x + 6f;

                if (generatePortraits && e.Kind != null)
                {
                    Rect portraitRect = new Rect(x, row.y + 2f, 46f, 46f);
                    DrawPortrait(portraitRect, e.Kind);
                    x += 52f;
                }

                string label = GetSafeLabel(e.Kind, e.KindDefName);
                int combatPower = (int)(e.Kind?.combatPower ?? 0f);
                string factionLabel = GetDefaultFactionLabel(e.Kind);

                Rect labelRect = new Rect(x, row.y + 2f, row.width - (x - row.x) - 10f, 24f);
                Widgets.Label(labelRect, $"{label} x{e.Count}  ({combatPower})");

                Rect metaRect = new Rect(x, row.y + 26f, row.width - (x - row.x) - 10f, 22f);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(metaRect, $"{LanguageManager.Get("Weight")}: {e.Weight:0.##}    {factionLabel}");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                y += 54f;
            }

            return y + 6f;
        }

        private void OpenKindDefPicker(SectionKind targetSection)
        {
            Find.WindowStack.Add(new Dialog_PawnKindPicker((kinds) =>
            {
                AddKindDefsToSection(kinds, targetSection);
            }, factionDef));
        }

        private void AddKindDefsToSection(List<PawnKindDef> kinds, SectionKind targetSection)
        {
            if (kinds == null || kinds.Count == 0) return;

            List<PawnGenOptionData> targetList = GetTargetList(targetSection);
            if (targetList == null) return;

            foreach (var kind in kinds)
            {
                if (kind == null) continue;

                // Check if already exists
                bool exists = targetList.Any(x => x.kindDefName == kind.defName);
                if (!exists)
                {
                    targetList.Add(new PawnGenOptionData
                    {
                        kindDefName = kind.defName,
                        selectionWeight = 10f
                    });
                }
            }

            RebuildPreview();
        }

        private List<PawnGenOptionData> GetTargetList(SectionKind section)
        {
            if (groupData == null) return null;

            switch (section)
            {
                case SectionKind.Options:
                    return groupData.options;
                case SectionKind.Traders:
                    return groupData.traders;
                case SectionKind.Carriers:
                    return groupData.carriers;
                case SectionKind.Guards:
                    return groupData.guards;
                default:
                    return null;
            }
        }

        private void DrawPortrait(Rect rect, PawnKindDef kind)
        {
            if (kind == null) return;

            if (pendingKinds.Contains(kind))
            {
                Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.25f));
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                Widgets.Label(rect, "...");
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            if (previewErrors.TryGetValue(kind, out var err) && !string.IsNullOrWhiteSpace(err))
            {
                Widgets.DrawBoxSolid(rect, new Color(0.2f, 0f, 0f, 0.35f));
                TooltipHandler.TipRegion(rect, err);
                return;
            }

            if (!previewPawns.TryGetValue(kind, out var pawn) || pawn == null || pawn.Destroyed)
            {
                Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.18f));
                return;
            }

            RenderTexture image = WidgetsUtils.GetPortrait(pawn, new Vector2(rect.width, rect.height), Rot4.South);
            if (image != null)
            {
                GUI.DrawTexture(rect, image);
            }
        }

        private string GetDefaultFactionLabel(PawnKindDef kind)
        {
            if (defaultFactionTypeField == null || kind == null) return "-";
            var f = defaultFactionTypeField.GetValue(kind) as FactionDef;
            return f != null ? f.LabelCap.ToString() : "-";
        }

        private string GetSafeLabel(PawnKindDef kind, string fallbackDefName)
        {
            if (kind == null)
                return fallbackDefName ?? "-";

            try
            {
                if (!string.IsNullOrEmpty(kind.label))
                    return kind.LabelCap;
                return fallbackDefName ?? kind.defName ?? "-";
            }
            catch
            {
                return fallbackDefName ?? kind.defName ?? "-";
            }
        }

        private string GetGroupDisplayLabel()
        {
            if (!string.IsNullOrWhiteSpace(groupData?.customLabel))
                return groupData.customLabel;

            var kind = DefDatabase<PawnGroupKindDef>.GetNamedSilentFail(groupData?.kindDefName);
            if (kind != null)
                return UI.Panels.GroupListPanel.GetTranslatedKindLabel(kind);

            return groupData?.kindDefName ?? LanguageManager.Get("Group");
        }

        private void RebuildPreview()
        {
            if (int.TryParse(seedBuffer, out var parsedSeed))
                seed = parsedSeed;
            else
                seedBuffer = seed.ToString();

            if (int.TryParse(drawsBuffer, out var parsedDraws))
                drawsPerList = Mathf.Clamp(parsedDraws, 0, 200);
            else
                drawsBuffer = drawsPerList.ToString();

            sections.Clear();
            sections[SectionKind.Options] = BuildSectionEntries(groupData?.options, drawsPerList, seed ^ 0x13579BDF);
            sections[SectionKind.Traders] = BuildSectionEntries(groupData?.traders, drawsPerList, seed ^ 0x2468ACE0);
            sections[SectionKind.Carriers] = BuildSectionEntries(groupData?.carriers, drawsPerList, seed ^ 0x5A5A5A5A);
            sections[SectionKind.Guards] = BuildSectionEntries(groupData?.guards, drawsPerList, seed ^ 0x0F0F0F0F);

            if (generatePortraits)
            {
                StartPawnGenerationQueue();
            }
        }

        private List<Entry> BuildSectionEntries(List<PawnGenOptionData> list, int draws, int localSeed)
        {
            if (list == null || list.Count == 0 || draws <= 0)
                return new List<Entry>();

            List<(PawnKindDef kind, string defName, float weight)> candidates = new List<(PawnKindDef, string, float)>();
            foreach (var d in list)
            {
                if (d == null) continue;
                if (d.selectionWeight <= 0f) continue;
                if (string.IsNullOrWhiteSpace(d.kindDefName)) continue;
                var kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(d.kindDefName);
                if (kind == null) continue;
                candidates.Add((kind, d.kindDefName, d.selectionWeight));
            }

            float totalWeight = candidates.Sum(c => c.weight);
            if (totalWeight <= 0.0001f)
                return new List<Entry>();

            Dictionary<PawnKindDef, int> counts = new Dictionary<PawnKindDef, int>();
            Dictionary<PawnKindDef, float> weights = new Dictionary<PawnKindDef, float>();

            Rand.PushState();
            Rand.Seed = localSeed;
            try
            {
                for (int i = 0; i < draws; i++)
                {
                    float roll = Rand.Value * totalWeight;
                    float cur = 0f;
                    PawnKindDef chosen = null;
                    float chosenWeight = 0f;

                    for (int j = 0; j < candidates.Count; j++)
                    {
                        var c = candidates[j];
                        cur += c.weight;
                        if (roll <= cur)
                        {
                            chosen = c.kind;
                            chosenWeight = c.weight;
                            break;
                        }
                    }

                    if (chosen == null) continue;
                    if (!counts.TryGetValue(chosen, out var v)) v = 0;
                    counts[chosen] = v + 1;
                    if (!weights.ContainsKey(chosen)) weights[chosen] = chosenWeight;
                }
            }
            finally
            {
                Rand.PopState();
            }

            List<Entry> entries = new List<Entry>();
            foreach (var kv in counts)
            {
                entries.Add(new Entry
                {
                    Kind = kv.Key,
                    KindDefName = kv.Key.defName,
                    Count = kv.Value,
                    Weight = weights.TryGetValue(kv.Key, out var w) ? w : 0f
                });
            }

            return entries
                .OrderByDescending(e => e.Count)
                .ThenByDescending(e => e.Weight)
                .ThenBy(e => GetSafeLabel(e.Kind, e.KindDefName))
                .ToList();
        }

        private void StartPawnGenerationQueue()
        {
            DestroyAllPreviewPawns();

            HashSet<PawnKindDef> kinds = new HashSet<PawnKindDef>();
            foreach (var kv in sections)
            {
                var list = kv.Value;
                if (list == null) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    var k = list[i]?.Kind;
                    if (k != null) kinds.Add(k);
                }
            }

            generationQueue.Clear();
            pendingKinds.Clear();
            foreach (var k in kinds)
            {
                generationQueue.Enqueue(k);
                pendingKinds.Add(k);
            }
        }

        private void ProcessGenerationQueue()
        {
            Faction faction = GetFactionOrNull();

            for (int i = 0; i < BATCH_SIZE && generationQueue.Count > 0; i++)
            {
                var k = generationQueue.Dequeue();
                pendingKinds.Remove(k);
                try
                {
                    var pawn = GeneratePawnInternal(k, faction);
                    if (pawn != null)
                    {
                        previewPawns[k] = pawn;
                        WidgetsUtils.SetPortraitDirty(pawn);
                    }
                    else
                    {
                        previewErrors[k] = LanguageManager.Get("PreviewFailed_GenFailed");
                    }
                }
                catch (Exception ex)
                {
                    previewErrors[k] = ex.Message;
                    Log.Warning($"[FactionGearCustomizer] Error generating pawn preview for {k?.defName}: {ex}");
                }
            }
        }

        private Pawn GeneratePawnInternal(PawnKindDef kDef, Faction faction)
        {
            if (kDef == null) return null;

            // 跳过 creepjoiner 类型的 PawnKindDef，因为它们需要特殊的生成逻辑
            if (kDef.race?.defName == "CreepJoiner")
            {
                Log.Warning($"[FactionGearCustomizer] Skipping preview for creepjoiner kindDef: {kDef.defName}");
                return null;
            }

            PawnGenerationRequest request = new PawnGenerationRequest(
                kDef,
                faction,
                PawnGenerationContext.NonPlayer,
                -1,
                true,
                false,
                false,
                false,
                false,
                0f,
                false,
                true,
                true,
                false,
                false
            );
            return PawnGenerator.GeneratePawn(request);
        }

        private Faction GetFactionOrNull()
        {
            return Find.FactionManager?.FirstFactionOfDef(factionDef);
        }

        private void DestroyAllPreviewPawns()
        {
            foreach (var p in previewPawns.Values)
            {
                if (p != null && !p.Destroyed) p.Destroy();
            }
            previewPawns.Clear();
            previewErrors.Clear();
            generationQueue.Clear();
            pendingKinds.Clear();
        }
    }
}
