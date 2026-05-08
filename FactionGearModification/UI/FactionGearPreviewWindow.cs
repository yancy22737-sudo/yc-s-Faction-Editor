using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using FactionGearModification.UI;
using FactionGearCustomizer.Utils;

namespace FactionGearCustomizer
{
    public class FactionGearPreviewWindow : Window
    {
        private PawnKindDef kindDef;
        private FactionDef factionDef;
        private Pawn previewPawn;
        private Rot4 rotation = Rot4.South;
        private Vector2 scrollPosition = Vector2.zero;
        private string errorMessage = null;

        // Multi-preview support
        private bool isMultiMode = false;
        private List<PawnKindDef> allKinds;
        private Dictionary<PawnKindDef, Pawn> previewPawns = new Dictionary<PawnKindDef, Pawn>();
        private Dictionary<PawnKindDef, string> previewErrors = new Dictionary<PawnKindDef, string>();

        private Queue<PawnKindDef> generationQueue = new Queue<PawnKindDef>();
        private HashSet<PawnKindDef> pendingKinds = new HashSet<PawnKindDef>();
        private int totalToGenerate = 0;
        private int generatedCount = 0;
        private const int BATCH_SIZE = 1;
        
        private Pawn pendingInfoPawn = null;

        // Group filter
        private int selectedGroupIndex = 0;
        private List<string> groupLabels = new List<string>();
        private List<List<PawnKindDef>> groupKindLists = new List<List<PawnKindDef>>();

        // Seed-based random kind selection
        private int seed;
        private string seedBuffer;
        private List<PawnKindDef> displayedKinds;
        private const int DEFAULT_DISPLAY_COUNT = 10;

        public override Vector2 InitialSize => isMultiMode ? new Vector2(1100f, 750f) : new Vector2(450f, 650f);

        public FactionGearPreviewWindow(PawnKindDef kindDef, FactionDef factionDef)
        {
            this.kindDef = kindDef;
            this.factionDef = factionDef;
            this.isMultiMode = false;
            CommonInit();
        }

        public FactionGearPreviewWindow(List<PawnKindDef> kinds, FactionDef factionDef)
        {
            this.allKinds = kinds;
            this.factionDef = factionDef;
            this.isMultiMode = true;
            CommonInit();
        }

        private void CommonInit()
        {
            this.doCloseX = true;
            this.forcePause = true;
            this.draggable = true;
            this.resizeable = true;
            if (isMultiMode)
            {
                this.seed = Rand.Int;
                this.seedBuffer = seed.ToString();
                BuildGroupFilterLists();
                SelectKindsForCurrentSeed();
            }
        }

        private void BuildGroupFilterLists()
        {
            groupLabels.Clear();
            groupKindLists.Clear();

            // "All" option
            groupLabels.Add(LanguageManager.Get("CategoryAll"));
            groupKindLists.Add(allKinds);

            // Try custom group makers first, fall back to vanilla pawnGroupMakers
            var settings = FactionGearCustomizerMod.Settings;
            List<PawnGroupMakerData> customGroups = null;

            if (settings?.factionGearData != null)
            {
                foreach (var fd in settings.factionGearData)
                {
                    if (fd.factionDefName == factionDef.defName && fd.groupMakers != null && fd.groupMakers.Count > 0)
                    {
                        customGroups = fd.groupMakers;
                        break;
                    }
                }
            }

            if (customGroups != null)
            {
                foreach (var group in customGroups)
                {
                    if (group == null) continue;

                    string label = !string.IsNullOrEmpty(group.customLabel)
                        ? group.customLabel
                        : group.kindDefName ?? $"Group {groupLabels.Count}";

                    var kindNames = new HashSet<string>();
                    CollectKindNames(group.options, kindNames);
                    CollectKindNames(group.traders, kindNames);
                    CollectKindNames(group.carriers, kindNames);
                    CollectKindNames(group.guards, kindNames);

                    var matched = allKinds.Where(k => kindNames.Contains(k.defName)).ToList();
                    if (matched.Count > 0)
                    {
                        groupLabels.Add($"{label} ({matched.Count})");
                        groupKindLists.Add(matched);
                    }
                }
            }
            else if (factionDef.pawnGroupMakers != null)
            {
                // Fallback to vanilla pawnGroupMakers
                foreach (var maker in factionDef.pawnGroupMakers)
                {
                    if (maker == null || maker.kindDef == null) continue;

                    string label = maker.kindDef.label;
                    if (string.IsNullOrEmpty(label))
                        label = maker.kindDef.defName;
                    if (string.IsNullOrEmpty(label))
                        label = $"Group {groupLabels.Count}";

                    var kindNames = new HashSet<string>();
                    if (maker.options != null)
                    {
                        foreach (var opt in maker.options)
                            if (opt?.kind != null) kindNames.Add(opt.kind.defName);
                    }
                    if (maker.traders != null)
                    {
                        foreach (var opt in maker.traders)
                            if (opt?.kind != null) kindNames.Add(opt.kind.defName);
                    }
                    if (maker.carriers != null)
                    {
                        foreach (var opt in maker.carriers)
                            if (opt?.kind != null) kindNames.Add(opt.kind.defName);
                    }
                    if (maker.guards != null)
                    {
                        foreach (var opt in maker.guards)
                            if (opt?.kind != null) kindNames.Add(opt.kind.defName);
                    }

                    var matched = allKinds.Where(k => kindNames.Contains(k.defName)).ToList();
                    if (matched.Count > 0)
                    {
                        groupLabels.Add($"{label} ({matched.Count})");
                        groupKindLists.Add(matched);
                    }
                }
            }

            // Default to first group if available, otherwise "All"
            selectedGroupIndex = groupLabels.Count > 1 ? 1 : 0;
        }

        private static void CollectKindNames(List<PawnGenOptionData> options, HashSet<string> result)
        {
            if (options == null) return;
            foreach (var opt in options)
            {
                if (!string.IsNullOrEmpty(opt.kindDefName))
                    result.Add(opt.kindDefName);
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            if (isMultiMode)
            {
                GenerateAllPreviewPawns();
            }
            else
            {
                GenerateSinglePreviewPawn();
            }
        }

        private List<PawnKindDef> GetBaseKindList()
        {
            if (groupKindLists.Count > 0 && selectedGroupIndex >= 0 && selectedGroupIndex < groupKindLists.Count)
                return groupKindLists[selectedGroupIndex];
            return allKinds;
        }

        private List<PawnKindDef> GetFilteredKinds()
        {
            if (displayedKinds != null && displayedKinds.Count > 0)
                return displayedKinds;
            return GetBaseKindList();
        }

        private void SelectKindsForCurrentSeed()
        {
            var source = GetBaseKindList();
            if (source == null || source.Count == 0) return;

            // 如果种类数不超过默认显示数，全部显示
            if (source.Count <= DEFAULT_DISPLAY_COUNT)
            {
                displayedKinds = new List<PawnKindDef>(source);
                return;
            }

            Rand.PushState();
            try
            {
                Rand.Seed = seed;
                var pool = new List<PawnKindDef>(source);
                displayedKinds = new List<PawnKindDef>();
                int count = Mathf.Min(DEFAULT_DISPLAY_COUNT, pool.Count);

                for (int i = 0; i < count; i++)
                {
                    int idx = Rand.Range(0, pool.Count);
                    displayedKinds.Add(pool[idx]);
                    pool.RemoveAt(idx);
                }
            }
            finally
            {
                Rand.PopState();
            }
        }

        private void GenerateAllPreviewPawns()
        {
            if (allKinds == null) return;

            var filtered = GetFilteredKinds();

            // Clear existing
            foreach (var p in previewPawns.Values)
            {
                if (p != null && !p.Destroyed)
                {
                    SafeClearApparel(p);
                    p.Destroy();
                }
            }
            previewPawns.Clear();
            previewErrors.Clear();

            generationQueue.Clear();
            pendingKinds.Clear();
            foreach (var k in filtered)
            {
                generationQueue.Enqueue(k);
                pendingKinds.Add(k);
            }
            totalToGenerate = filtered.Count;
            generatedCount = 0;

            Faction faction = GetFaction();
            if (faction == null && Current.ProgramState == ProgramState.Playing)
            {
                errorMessage = "Cannot preview: Faction not found in current game.";
                generationQueue.Clear();
                pendingKinds.Clear();
                return;
            }
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            if (isMultiMode && generationQueue.Count > 0)
            {
                ProcessGenerationQueue();
            }
        }

        private void ProcessGenerationQueue()
        {
            Faction faction = GetFaction();
            if (faction == null && Current.ProgramState == ProgramState.Playing) return;

            for (int i = 0; i < BATCH_SIZE && generationQueue.Count > 0; i++)
            {
                var k = generationQueue.Dequeue();
                pendingKinds.Remove(k);
                generatedCount++;
                
                try
                {
                    Pawn p = GeneratePawnInternal(k, faction);
                    if (p != null)
                    {
                        if (previewPawns.TryGetValue(k, out var existing) && existing != null && !existing.Destroyed)
                        {
                            try { SafeClearApparel(existing); } catch { }
                            try { existing.Destroy(); } catch { }
                        }
                        previewPawns[k] = p;
                        try { WidgetsUtils.SetPortraitDirty(p); } catch { }
                    }
                    else
                    {
                        previewErrors[k] = "Failed to generate";
                    }
                }
                catch (Exception ex)
                {
                    previewErrors[k] = ex.Message;
                    Log.Warning($"[FactionGearCustomizer] Error generating preview for {k.defName}: {ex}");
                }
            }
        }

        private void GenerateSinglePreviewPawn()
        {
            errorMessage = null;
            if (previewPawn != null)
            {
                if (!previewPawn.Destroyed)
                {
                    SafeClearApparel(previewPawn);
                    previewPawn.Destroy();
                }
                previewPawn = null;
            }

            try
            {
                Faction faction = GetFaction();
                if (faction == null && Current.ProgramState == ProgramState.Playing)
                {
                    Log.Warning($"[FactionGearCustomizer] Could not find active faction for {factionDef.defName}. Preview might fail.");
                    errorMessage = LanguageManager.Get("PreviewFailed_FactionNotFound");
                    return;
                }

                previewPawn = GeneratePawnInternal(kindDef, faction);
                
                if (previewPawn == null)
                {
                    Log.Error($"[FactionGearCustomizer] PawnGenerator.GeneratePawn returned null for {kindDef.defName}");
                    errorMessage = LanguageManager.Get("PreviewFailed_GenFailed");
                    return;
                }

                WidgetsUtils.SetPortraitDirty(previewPawn);
            }
            catch (Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] Failed to generate preview pawn: {ex}");
                errorMessage = $"Error: {ex.Message}";
            }
        }

        private Faction GetFaction()
        {
            if (Find.FactionManager != null)
            {
                return Find.FactionManager.FirstFactionOfDef(factionDef);
            }
            return null;
        }

        private Pawn GeneratePawnInternal(PawnKindDef kDef, Faction faction)
        {
            // 跳过 creepjoiner 类型的 PawnKindDef，因为它们需要特殊的生成逻辑
            if (kDef?.race?.defName == "CreepJoiner")
            {
                Log.Warning($"[FactionGearCustomizer] Skipping preview for creepjoiner kindDef: {kDef.defName}");
                return null;
            }

            PawnGenerationRequest request = new PawnGenerationRequest(
                kDef,
                faction,
                PawnGenerationContext.NonPlayer,
                -1,
                true, // forceGenerateNewPawn
                false, // newborn
                false, // allowDead
                false, // allowDowned
                true, // canGeneratePawnRelations
                1f, // colonistRelationChanceFactor
                false, // mustBeCapableOfViolence
                true, // forceAddFreeWarmLayerIfNeeded
                true, // allowGay
                false, // allowFood
                false // allowAddictions
            );

            // 使用种子驱动随机装备/特征
            Rand.PushState();
            Rand.Seed = seed ^ kDef.GetHashCode();
            Pawn result;
            try
            {
                result = PawnGenerator.GeneratePawn(request);
            }
            finally
            {
                Rand.PopState();
            }
            return result;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (isMultiMode)
            {
                DoMultiWindowContents(inRect);
            }
            else
            {
                DoSingleWindowContents(inRect);
            }
        }

        private void DoMultiWindowContents(Rect inRect)
        {
            float y = inRect.y;

            // Top row: faction name | progress | seed | reroll
            float rowH = 32f;
            float rerollW = 100f;
            float seedW = 90f;

            // Reroll + Seed input (far right)
            Rect refreshRect = new Rect(inRect.xMax - rerollW, y, rerollW, rowH);
            Rect seedRect = new Rect(refreshRect.x - seedW - 4f, y + 4f, seedW, 24f);
            seedBuffer = Widgets.TextField(seedRect, seedBuffer ?? "");

            if (Widgets.ButtonText(refreshRect, LanguageManager.Get("Reroll")))
            {
                seed = Rand.Int;
                seedBuffer = seed.ToString();
                SelectKindsForCurrentSeed();
                GenerateAllPreviewPawns();
            }

            // Faction name (left, 固定宽度，超出加省略号)
            float nameW = Mathf.Min(300f, inRect.width * 0.3f);
            Text.Font = GameFont.Medium;
            string factionName;
            if (Current.ProgramState == ProgramState.Playing && EditorSession.UseInGameNames && Find.FactionManager != null)
            {
                var instance = Find.FactionManager.FirstFactionOfDef(factionDef);
                factionName = instance != null
                    ? DefDisplayNameUtility.GetSafeFactionDisplayName(instance, "FactionGearPreviewWindow.DrawMulti")
                    : DefDisplayNameUtility.GetSafeFactionDisplayName(factionDef, "FactionGearPreviewWindow.DrawMulti");
            }
            else
            {
                factionName = DefDisplayNameUtility.GetSafeFactionDisplayName(factionDef, "FactionGearPreviewWindow.DrawMulti");
            }
            string fullName = LanguageManager.Get("Preview") + ": " + factionName;
            string displayName = fullName;
            if (Text.CalcSize(fullName).x > nameW)
            {
                for (int ci = fullName.Length - 1; ci > 0; ci--)
                {
                    if (Text.CalcSize(fullName.Substring(0, ci) + "...").x <= nameW)
                    {
                        displayName = fullName.Substring(0, ci) + "...";
                        break;
                    }
                }
            }
            Widgets.Label(new Rect(inRect.x, y, nameW, rowH), displayName);
            Text.Font = GameFont.Small;

            // Progress bar (name 和 seed 之间)
            if (generationQueue.Count > 0)
            {
                float progressX = inRect.x + nameW + 4f;
                float progressW = seedRect.x - progressX - 4f;
                if (progressW > 60f)
                {
                    float progress = (float)generatedCount / totalToGenerate;
                    Rect progressRect = new Rect(progressX, y + 6f, progressW, 20f);
                    Widgets.FillableBar(progressRect, progress);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(progressRect, $"{generatedCount}/{totalToGenerate}");
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            }
            y += rowH + 4f;

            // Group filter dropdown (second row, compact)
            bool hasMultipleGroups = groupLabels.Count > 1;
            {
                Rect labelRect = new Rect(inRect.x, y, 40f, 22f);
                Rect dropdownRect = new Rect(inRect.x + 44f, y, 180f, 22f);

                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(labelRect, LanguageManager.Get("Group") + ":");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;

                if (!hasMultipleGroups)
                {
                    GUI.color = Color.gray;
                    Widgets.ButtonText(dropdownRect, groupLabels[selectedGroupIndex], active: false);
                    GUI.color = Color.white;
                }
                else if (Widgets.ButtonText(dropdownRect, groupLabels[selectedGroupIndex]))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    for (int i = 0; i < groupLabels.Count; i++)
                    {
                        int captured = i;
                        options.Add(new FloatMenuOption(groupLabels[i], () =>
                        {
                            selectedGroupIndex = captured;
                            displayedKinds = null;
                            GenerateAllPreviewPawns();
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                y += 26f;
            }

            if (errorMessage != null)
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 30f), errorMessage);
                return;
            }

            Rect outRect = new Rect(inRect.x, y + 20f, inRect.width, inRect.height - (y - inRect.y) - 20f);

            var filteredKinds = GetFilteredKinds();

            // Compact Grid Calculation
            float cardWidth = 160f;
            float cardHeight = 240f; // Adjusted for name + portrait
            float spacing = 8f;
            int columns = Mathf.FloorToInt((outRect.width - 16f) / (cardWidth + spacing));
            if (columns < 1) columns = 1;

            int gridRows = Mathf.CeilToInt((float)filteredKinds.Count / columns);
            float viewHeight = gridRows * (cardHeight + spacing);

            Widgets.BeginScrollView(outRect, ref scrollPosition, new Rect(0, 0, outRect.width - 16f, viewHeight));

            for (int i = 0; i < filteredKinds.Count; i++)
            {
                var k = filteredKinds[i];
                int col = i % columns;
                int row = i / columns;

                Rect cardRect = new Rect(col * (cardWidth + spacing), row * (cardHeight + spacing), cardWidth, cardHeight);
                DrawPawnCard(cardRect, k);
            }

            Widgets.EndScrollView();
            
            if (pendingInfoPawn != null)
            {
                Find.WindowStack.Add(new Dialog_PawnGearInfo(pendingInfoPawn));
                pendingInfoPawn = null;
            }
        }

        private void DrawPawnCard(Rect rect, PawnKindDef k)
        {
            // Highlight if selected
            bool isSelected = EditorSession.SelectedKindDefName == k.defName;
            if (isSelected)
            {
                GUI.color = new Color(1f, 0.9f, 0.5f);
                WidgetsUtils.DrawBox(rect, 2);
                GUI.color = Color.white;
            }
            else
            {
                WidgetsUtils.DrawMenuSection(rect);
            }

            // Click to select (leave space for info button on the right)
            Rect buttonRect = new Rect(rect.x, rect.y, rect.width - 35f, rect.height);
            if (Widgets.ButtonInvisible(buttonRect))
            {
                EditorSession.SelectedKindDefName = k.defName;
                EditorSession.GearListScrollPos = Vector2.zero;
            }

            Rect inner = rect.ContractedBy(4f);
            
            // Header
            Rect headerRect = new Rect(inner.x, inner.y, inner.width - 24f, 22f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            
            string label = k.LabelCap;
            var factionData = FactionGearCustomizerMod.Settings.factionGearData.FirstOrDefault(f => f.factionDefName == factionDef.defName);
            if (factionData != null)
            {
                var kindData = factionData.kindGearData.FirstOrDefault(kd => kd.kindDefName == k.defName);
                if (kindData != null && !string.IsNullOrEmpty(kindData.Label))
                {
                    label = kindData.Label;
                }
            }
            
            Widgets.Label(headerRect, label);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Pawn p = null;
            previewPawns.TryGetValue(k, out p);

            // Portrait
            Rect portraitRect = new Rect(inner.x, inner.y + 24f, inner.width, inner.height - 24f);
            
            if (p != null)
            {
                // Draw Pawn
                RenderTexture image = null;
                try
                {
                    image = WidgetsUtils.GetPortrait(p, new Vector2(portraitRect.width, portraitRect.height), rotation, new Vector3(0f, 0f, 0f), 1f);
                }
                catch (Exception ex)
                {
                    // 捕获任何未处理的渲染异常
                    Log.Warning($"[FactionGearCustomizer] 绘制 {k.defName} 肖像时发生未处理异常：{ex.Message}");
                }
                
                if (image != null)
                {
                    try
                    {
                        GUI.DrawTexture(portraitRect, image);
                    }
                    catch (Exception drawEx)
                    {
                        Log.Warning($"[FactionGearCustomizer] 绘制肖像纹理失败：{drawEx.Message}");
                        image = null;
                    }
                }

                if (image == null)
                {
                    // 渲染失败时显示占位符，避免空白区域
                    Widgets.DrawBoxSolid(portraitRect, new Color(0.12f, 0.12f, 0.12f, 0.7f));
                    if (k.race?.uiIcon != null)
                    {
                        try
                        {
                            Rect iconRect = new Rect(portraitRect.center.x - 24f, portraitRect.center.y - 24f, 48f, 48f);
                            WidgetsUtils.DrawTextureFitted(iconRect, k.race.uiIcon, 1f);
                        }
                        catch { }
                    }
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(new Rect(portraitRect.x, portraitRect.yMax - 22f, portraitRect.width, 20f), LanguageManager.Get("PreviewFailed_NoPawn"));
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                }

                // Draw Weapon Thumbnail
                try
                {
                    if (p.equipment != null && p.equipment.Primary != null)
                    {
                        Thing weapon = p.equipment.Primary;
                        Rect weaponRect = new Rect(inner.xMax - 40f, inner.yMax - 40f, 36f, 36f);
                        Widgets.DrawBoxSolid(weaponRect, new Color(0f, 0f, 0f, 0.5f));
                        if (weapon.def?.uiIcon != null)
                        {
                            WidgetsUtils.DrawTextureFitted(weaponRect, weapon.def.uiIcon, 1f);
                        }
                        TooltipHandler.TipRegion(weaponRect, weapon.LabelCap);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[FactionGearCustomizer] 绘制武器缩略图失败 ({k.defName}): {ex.Message}");
                }

                // Raid points display - bottom left
                try
                {
                    float combatPower = k.combatPower;
                    var settings = FactionGearCustomizerMod.Settings;
                    if (settings?.factionGearData != null)
                    {
                        foreach (var fd in settings.factionGearData)
                        {
                            if (fd.factionDefName != factionDef.defName) continue;
                            if (fd.groupMakers == null) continue;
                            foreach (var gm in fd.groupMakers)
                            {
                                if (gm.options == null) continue;
                                foreach (var opt in gm.options)
                                {
                                    if (opt.kindDefName == k.defName && opt.pointsOverride.HasValue)
                                    {
                                        combatPower = opt.pointsOverride.Value;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    Rect raidPointsRect = new Rect(inner.x + 2f, inner.yMax - 18f, inner.width - 44f, 16f);
                    Text.Anchor = TextAnchor.LowerLeft;
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(raidPointsRect, $"点数：{(int)combatPower}");
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                catch { }

                // Carry weight display - above raid points
                try
                {
                    if (p.inventory != null)
                    {
                        float mass = MassUtility.GearAndInventoryMass(p);
                        float capacity = MassUtility.Capacity(p, null);
                        float percent = capacity > 0f ? mass / capacity : 0f;
                        Rect weightRect = new Rect(inner.x + 2f, inner.yMax - 32f, inner.width - 44f, 16f);
                        Color weightColor = percent > 0.9f ? new Color(0.8f, 0.29f, 0.32f) : (percent > 0.75f ? new Color(1f, 0.69f, 0.1f) : new Color(0.2f, 0.76f, 0.57f));
                        GUI.color = weightColor;
                        Widgets.Label(weightRect, $"负重: {percent:P0}");
                        GUI.color = Color.white;
                    }
                }
                catch { }

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;

                // 点击显示装备详情
                try
                {
                    Rect infoButtonRect = new Rect(rect.xMax - 30f, rect.y + 30f, 26f, 26f);
                    if (Widgets.InfoCardButton(infoButtonRect.x, infoButtonRect.y, p))
                    {
                        pendingInfoPawn = p;
                    }
                }
                catch { }

                // 提示点击可以查看详情
                if (Mouse.IsOver(rect))
                {
                    TooltipHandler.TipRegion(rect, LanguageManager.Get("ClickToViewGearInfo"));
                    Widgets.DrawHighlight(rect);
                }
            }
            else
            {
                string err = previewErrors.ContainsKey(k) ? previewErrors[k] : 
                            (pendingKinds.Contains(k) ? "..." : LanguageManager.Get("PreviewFailed_NoPawn"));
                
                Text.Anchor = TextAnchor.MiddleCenter;
                
                if (pendingKinds.Contains(k))
                {
                     GUI.color = Color.gray;
                     Widgets.Label(portraitRect, "...");
                     GUI.color = Color.white;
                }
                else
                {
                    Widgets.Label(portraitRect, err);
                }
                
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DoSingleWindowContents(Rect inRect)
        {
            // Original implementation
            Text.Font = GameFont.Medium;
            
            string label = kindDef.LabelCap;
            var factionData = FactionGearCustomizerMod.Settings.factionGearData.FirstOrDefault(f => f.factionDefName == factionDef.defName);
            if (factionData != null)
            {
                var kindData = factionData.kindGearData.FirstOrDefault(kd => kd.kindDefName == kindDef.defName);
                if (kindData != null && !string.IsNullOrEmpty(kindData.Label))
                {
                    label = kindData.Label;
                }
            }
            
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), LanguageManager.Get("Preview") + ": " + label);
            Text.Font = GameFont.Small;

            if (previewPawn == null || errorMessage != null)
            {
                string errorText = errorMessage ?? LanguageManager.Get("FailedToGeneratePreview");
                Widgets.Label(new Rect(inRect.x, inRect.y + 40f, inRect.width - 20f, 60f), errorText);
                
                Rect retryRect = new Rect(inRect.x + (inRect.width - 120f) / 2f, inRect.y + 100f, 120f, 30f);
                if (Widgets.ButtonText(retryRect, LanguageManager.Get("Retry")))
                {
                    GenerateSinglePreviewPawn();
                }
                return;
            }

            // Draw Pawn
            Rect pawnRect = new Rect(inRect.x + (inRect.width - 200f) / 2f, inRect.y + 40f, 200f, 300f);
            WidgetsUtils.DrawWindowBackground(pawnRect);

            // Render Pawn
            RenderTexture image = null;
            try
            {
                image = WidgetsUtils.GetPortrait(previewPawn, new Vector2(200f, 300f), rotation, new Vector3(0f, 0f, 0f), 1f);
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] 渲染单个预览Pawn时发生异常：{ex.Message}");
            }
            
            if (image != null)
            {
                try
                {
                    GUI.DrawTexture(pawnRect, image);
                }
                catch (Exception drawEx)
                {
                    Log.Warning($"[FactionGearCustomizer] 绘制单个预览纹理失败：{drawEx.Message}");
                }
                
                // Info Card Button
                Widgets.InfoCardButton(pawnRect.xMax - 24f, pawnRect.y, previewPawn);
            }
            else
            {
                Widgets.Label(pawnRect, LanguageManager.Get("PortraitUnavailable"));
            }

            // Rotation Buttons
            Rect rotRect = new Rect(pawnRect.x, pawnRect.yMax + 5f, pawnRect.width, 24f);
            if (Widgets.ButtonText(rotRect.LeftHalf(), "< " + LanguageManager.Get("Rotate")))
            {
                rotation.Rotate(RotationDirection.Counterclockwise);
                WidgetsUtils.SetPortraitDirty(previewPawn);
            }
            if (Widgets.ButtonText(rotRect.RightHalf(), LanguageManager.Get("Rotate") + " >"))
            {
                rotation.Rotate(RotationDirection.Clockwise);
                WidgetsUtils.SetPortraitDirty(previewPawn);
            }
            
            // Refresh Button
            Rect refreshRect = new Rect(inRect.x + (inRect.width - 120f) / 2f, rotRect.yMax + 10f, 120f, 30f);
            if (Widgets.ButtonText(refreshRect, LanguageManager.Get("RerollPawn")))
            {
                GenerateSinglePreviewPawn();
            }

            // Gear List Summary
            float listY = refreshRect.yMax + 10f;
            Rect listRect = new Rect(inRect.x, listY, inRect.width, inRect.height - listY);
            
            Widgets.BeginScrollView(listRect, ref scrollPosition, new Rect(0, 0, listRect.width - 16f, 500f));
            Listing_Standard list = new Listing_Standard();
            list.Begin(new Rect(0, 0, listRect.width - 16f, 500f));
            
            WidgetsUtils.Label(list, LanguageManager.Get("EquippedGear"));
            if (previewPawn.equipment != null)
            {
                foreach (var eq in previewPawn.equipment.AllEquipmentListForReading)
                {
                    var qualityComp = eq.GetComp<CompQuality>();
                    string qualityStr = qualityComp != null ? LanguageManager.Get("Quality" + qualityComp.Quality.ToString()) : LanguageManager.Get("QualityNormal");
                    WidgetsUtils.Label(list, "- " + eq.LabelCap + " (" + qualityStr + ")");
                }
            }
            
            list.Gap();
            WidgetsUtils.Label(list, LanguageManager.Get("ApparelWorn"));
            if (previewPawn.apparel != null)
            {
                foreach (var app in previewPawn.apparel.WornApparel)
                {
                    var qualityComp = app.GetComp<CompQuality>();
                    string qualityStr = qualityComp != null ? LanguageManager.Get("Quality" + qualityComp.Quality.ToString()) : LanguageManager.Get("QualityNormal");
                    WidgetsUtils.Label(list, "- " + app.LabelCap + " (" + qualityStr + ")");
                }
            }

            list.End();
            Widgets.EndScrollView();
        }

        public override void PreClose()
        {
            base.PreClose();
            try
            {
                if (previewPawn != null && !previewPawn.Destroyed)
                {
                    // 安全地移除服装，跳过不可销毁的物品
                    SafeClearApparel(previewPawn);
                    previewPawn.Destroy();
                    previewPawn = null;
                }
                
                if (previewPawns != null)
                {
                    foreach (var p in previewPawns.Values)
                    {
                        if (p != null && !p.Destroyed)
                        {
                            // 安全地移除服装，跳过不可销毁的物品
                            SafeClearApparel(p);
                            p.Destroy();
                        }
                    }
                    previewPawns.Clear();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Error destroying preview pawn: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全地清空服装，跳过不可销毁的物品
        /// </summary>
        private void SafeClearApparel(Pawn pawn)
        {
            if (pawn?.apparel?.WornApparel == null) return;

            var apparelList = pawn.apparel.WornApparel.ToList();
            foreach (var apparel in apparelList)
            {
                if (apparel == null) continue;

                try
                {
                    // 对于不可销毁的物品，只移除不销毁
                    if (apparel.def != null && !apparel.def.destroyable)
                    {
                        pawn.apparel.Remove(apparel);
                        continue;
                    }

                    pawn.apparel.Remove(apparel);
                    if (!apparel.Destroyed)
                    {
                        apparel.Destroy();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[FactionGearCustomizer] Failed to destroy apparel in preview: {ex.Message}");
                }
            }
        }
    }
}
