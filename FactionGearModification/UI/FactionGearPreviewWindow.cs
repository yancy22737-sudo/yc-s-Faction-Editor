using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using FactionGearModification.UI;

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

        private void GenerateAllPreviewPawns()
        {
            if (allKinds == null) return;

            // Clear existing
            foreach (var p in previewPawns.Values)
            {
                if (p != null && !p.Destroyed) p.Destroy();
            }
            previewPawns.Clear();
            previewErrors.Clear();

            Faction faction = GetFaction();
            if (faction == null && Current.ProgramState == ProgramState.Playing)
            {
                errorMessage = "Cannot preview: Faction not found in current game.";
                return;
            }

            foreach (var k in allKinds)
            {
                try
                {
                    Pawn p = GeneratePawnInternal(k, faction);
                    if (p != null)
                    {
                        previewPawns[k] = p;
                        WidgetsUtils.SetPortraitDirty(p);
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
                if (!previewPawn.Destroyed) previewPawn.Destroy();
                previewPawn = null;
            }

            try
            {
                Faction faction = GetFaction();
                if (faction == null && Current.ProgramState == ProgramState.Playing)
                {
                    Log.Warning($"[FactionGearCustomizer] Could not find active faction for {factionDef.defName}. Preview might fail.");
                    errorMessage = "Cannot preview: Faction not found in current game.";
                    return;
                }

                previewPawn = GeneratePawnInternal(kindDef, faction);
                
                if (previewPawn == null)
                {
                    Log.Error($"[FactionGearCustomizer] PawnGenerator.GeneratePawn returned null for {kindDef.defName}");
                    errorMessage = "Failed to generate preview pawn.";
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
            return PawnGenerator.GeneratePawn(request);
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
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 150f, 30f), LanguageManager.Get("Preview") + ": " + factionDef.LabelCap);
            
            Rect refreshRect = new Rect(inRect.width - 140f, inRect.y, 140f, 30f);
            if (Widgets.ButtonText(refreshRect, LanguageManager.Get("Reroll")))
            {
                GenerateAllPreviewPawns();
            }
            Text.Font = GameFont.Small;

            if (errorMessage != null)
            {
                 Widgets.Label(new Rect(inRect.x, inRect.y + 40f, inRect.width, 30f), errorMessage);
                 return;
            }

            Rect outRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f);
            
            // Compact Grid Calculation
            float cardWidth = 160f;
            float cardHeight = 240f; // Adjusted for name + portrait
            float spacing = 8f;
            int columns = Mathf.FloorToInt((outRect.width - 16f) / (cardWidth + spacing));
            if (columns < 1) columns = 1;
            
            int rows = Mathf.CeilToInt((float)allKinds.Count / columns);
            float viewHeight = rows * (cardHeight + spacing);

            Widgets.BeginScrollView(outRect, ref scrollPosition, new Rect(0, 0, outRect.width - 16f, viewHeight));
            
            for (int i = 0; i < allKinds.Count; i++)
            {
                var k = allKinds[i];
                int col = i % columns;
                int row = i / columns;
                
                Rect cardRect = new Rect(col * (cardWidth + spacing), row * (cardHeight + spacing), cardWidth, cardHeight);
                DrawPawnCard(cardRect, k);
            }

            Widgets.EndScrollView();
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

            // Click to select
            if (Widgets.ButtonInvisible(rect))
            {
                EditorSession.SelectedKindDefName = k.defName;
                EditorSession.GearListScrollPos = Vector2.zero;
                // Optional: Close window on select? User didn't specify, but for "Preview" usually we want to see.
                // If this is a "Gallery" replacement for the sidebar, we might keep it open.
                // But since it's a modal window, maybe we should close it?
                // The user said "Preview all -> Preview", implying this IS the preview.
                // I'll keep it open so they can browse. Selection updates the editor in background.
            }

            Rect inner = rect.ContractedBy(4f);
            
            // Header
            Rect headerRect = new Rect(inner.x, inner.y, inner.width, 22f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(headerRect, k.LabelCap);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // Portrait
            Rect portraitRect = new Rect(inner.x, inner.y + 24f, inner.width, inner.height - 24f);
            
            if (previewPawns.TryGetValue(k, out Pawn p) && p != null)
            {
                // Draw Pawn
                RenderTexture image = WidgetsUtils.GetPortrait(p, new Vector2(portraitRect.width, portraitRect.height), rotation, new Vector3(0f, 0f, 0f), 1f);
                if (image != null)
                {
                    GUI.DrawTexture(portraitRect, image);
                }

                // Draw Weapon Thumbnail
                if (p.equipment != null && p.equipment.Primary != null)
                {
                    Thing weapon = p.equipment.Primary;
                    Rect weaponRect = new Rect(inner.xMax - 40f, inner.yMax - 40f, 36f, 36f);
                    
                    // Draw background for weapon to make it visible
                    Widgets.DrawBoxSolid(weaponRect, new Color(0f, 0f, 0f, 0.5f));
                    
                    // Draw icon
                    if (weapon.def.uiIcon != null)
                    {
                        WidgetsUtils.DrawTextureFitted(weaponRect, weapon.def.uiIcon, 1f);
                    }
                    
                    TooltipHandler.TipRegion(weaponRect, weapon.LabelCap);
                }

                // Hover Tooltip
                TooltipHandler.TipRegion(rect, new TipSignal(() => GetPawnTooltip(p), k.GetHashCode()));
            }
            else
            {
                string err = previewErrors.ContainsKey(k) ? previewErrors[k] : "No Pawn";
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(portraitRect, err);
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private string GetPawnTooltip(Pawn p)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>{p.LabelCap}</b>");
            sb.AppendLine();
            
            sb.AppendLine("<b>Weapons:</b>");
            if (p.equipment != null && p.equipment.AllEquipmentListForReading.Any())
            {
                foreach (var eq in p.equipment.AllEquipmentListForReading)
                {
                     sb.AppendLine($"- {eq.LabelCap}");
                }
            }
            else
            {
                sb.AppendLine("- None");
            }
            
            sb.AppendLine();
            sb.AppendLine("<b>Apparel:</b>");
            if (p.apparel != null && p.apparel.WornApparel.Any())
            {
                foreach (var app in p.apparel.WornApparel)
                {
                    sb.AppendLine($"- {app.LabelCap}");
                }
            }
            else
            {
                sb.AppendLine("- None");
            }

            if (p.health != null && p.health.hediffSet.hediffs.Any())
            {
                var visibleHediffs = p.health.hediffSet.hediffs.Where(h => h.Visible).ToList();
                if (visibleHediffs.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("<b>Hediffs:</b>");
                    foreach (var h in visibleHediffs)
                    {
                        sb.AppendLine($"- {h.LabelCap}");
                    }
                }
            }

            return sb.ToString();
        }

        private void DoSingleWindowContents(Rect inRect)
        {
            // Original implementation
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), LanguageManager.Get("Preview") + ": " + kindDef.LabelCap);
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
            RenderTexture image = WidgetsUtils.GetPortrait(previewPawn, new Vector2(200f, 300f), rotation, new Vector3(0f, 0f, 0f), 1f);
            if (image != null)
            {
                GUI.DrawTexture(pawnRect, image);
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
                    previewPawn.Destroy();
                    previewPawn = null;
                }
                
                if (previewPawns != null)
                {
                    foreach (var p in previewPawns.Values)
                    {
                        if (p != null && !p.Destroyed) p.Destroy();
                    }
                    previewPawns.Clear();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Error destroying preview pawn: {ex.Message}");
            }
        }
    }
}
