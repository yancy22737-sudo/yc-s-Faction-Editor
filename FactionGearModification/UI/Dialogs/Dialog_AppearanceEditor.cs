using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_AppearanceEditor : Window
    {
        private readonly ForcedAppearance appearance;
        private Vector2 scrollPos;
        private Vector2 skinScrollPos;

        private enum MainTab { Shape, Hair, Skin, Genes }
        private enum ShapeSub { Body, Head }
        private enum HairSub { Hair, Beard }

        private MainTab mainTab = MainTab.Shape;
        private ShapeSub shapeSub = ShapeSub.Body;
        private HairSub hairSub = HairSub.Hair;

        private const int IconsPerRow = 6;

        public override Vector2 InitialSize => new Vector2(700f, 600f);

        public Dialog_AppearanceEditor(ForcedAppearance appearance)
        {
            this.appearance = appearance;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            draggable = true;
            resizeable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, inRect.width, 32f), LanguageManager.Get("AppearanceEditorTitle"));
            Text.Font = GameFont.Small;
            y += 36f;

            // Main tabs
            string[] mainNames = { LanguageManager.Get("AppearanceTab_Shape"), LanguageManager.Get("AppearanceTab_Hair"), LanguageManager.Get("AppearanceTab_Skin"), LanguageManager.Get("AppearanceTab_Genes") };
            MainTab[] mainVals = { MainTab.Shape, MainTab.Hair, MainTab.Skin, MainTab.Genes };
            float mainTabW = inRect.width / 4f;
            for (int i = 0; i < 4; i++)
            {
                Rect tr = new Rect(mainTabW * i, y, mainTabW, 26f);
                if (mainTab == mainVals[i]) Widgets.DrawHighlightSelected(tr);
                else Widgets.DrawHighlightIfMouseover(tr);
                if (Widgets.ButtonInvisible(tr)) { mainTab = mainVals[i]; scrollPos = Vector2.zero; skinScrollPos = Vector2.zero; }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(tr, mainNames[i]);
                Text.Anchor = TextAnchor.UpperLeft;
            }
            y += 30f;

            // Sub-tabs
            if (mainTab == MainTab.Shape)
            {
                float subW = inRect.width / 2f;
                DrawSubTab(new Rect(0f, y, subW, 22f), LanguageManager.Get("AppearanceTab_Body"), shapeSub == ShapeSub.Body, () => { shapeSub = ShapeSub.Body; scrollPos = Vector2.zero; });
                DrawSubTab(new Rect(subW, y, subW, 22f), LanguageManager.Get("AppearanceTab_Head"), shapeSub == ShapeSub.Head, () => { shapeSub = ShapeSub.Head; scrollPos = Vector2.zero; });
                y += 24f;
            }
            else if (mainTab == MainTab.Hair)
            {
                float subW = inRect.width / 2f;
                DrawSubTab(new Rect(0f, y, subW, 22f), LanguageManager.Get("AppearanceTab_Hair"), hairSub == HairSub.Hair, () => { hairSub = HairSub.Hair; scrollPos = Vector2.zero; });
                DrawSubTab(new Rect(subW, y, subW, 22f), LanguageManager.Get("AppearanceTab_Beard"), hairSub == HairSub.Beard, () => { hairSub = HairSub.Beard; scrollPos = Vector2.zero; });
                y += 24f;
            }

            // Content area
            Rect contentRect = new Rect(0f, y, inRect.width, inRect.height - y - 50f);
            Widgets.DrawMenuSection(contentRect);

            switch (mainTab)
            {
                case MainTab.Shape:
                    if (shapeSub == ShapeSub.Body) DrawGrid(contentRect, DefDatabase<BodyTypeDef>.AllDefs.OrderBy(d => d.LabelCap.ToString()).ToList(), d => GetBodyTex(d), d => d.LabelCap.ToString(), d => d.defName == appearance.bodyTypeDefName, d => { appearance.bodyTypeDefName = d.defName; FactionGearEditor.MarkDirty(); });
                    else DrawGrid(contentRect, DefDatabase<HeadTypeDef>.AllDefs.OrderBy(d => d.LabelCap.ToString()).ToList(), d => GetHeadTex(d), d => d.LabelCap.ToString(), d => d.defName == appearance.headTypeDefName, d => { appearance.headTypeDefName = d.defName; FactionGearEditor.MarkDirty(); });
                    break;
                case MainTab.Hair:
                    if (hairSub == HairSub.Hair) DrawGrid(contentRect, DefDatabase<HairDef>.AllDefs.OrderBy(d => d.LabelCap.ToString()).ToList(), d => d.Icon, d => d.LabelCap.ToString(), d => d.defName == appearance.hairDefName, d => { appearance.hairDefName = d.defName; FactionGearEditor.MarkDirty(); });
                    else DrawGrid(contentRect, DefDatabase<BeardDef>.AllDefs.OrderBy(d => d.LabelCap.ToString()).ToList(), d => d.Icon, d => d.LabelCap.ToString(), d => d.defName == appearance.beardDefName, d => { appearance.beardDefName = d.defName; FactionGearEditor.MarkDirty(); });
                    break;
                case MainTab.Skin:
                    DrawSkinTab(contentRect.ContractedBy(6f));
                    break;
                case MainTab.Genes:
                    DrawCosmeticGenes(contentRect);
                    break;
            }

            // Bottom
            Rect bottom = new Rect(0f, inRect.height - 42f, inRect.width, 36f);
            if (Widgets.ButtonText(new Rect(bottom.x, bottom.y, 100f, 30f), LanguageManager.Get("Clear")))
            {
                appearance.hairDefName = null; appearance.beardDefName = null;
                appearance.bodyTypeDefName = null; appearance.headTypeDefName = null;
                appearance.skinColor = null; appearance.hairColor = null;
                FactionGearEditor.MarkDirty();
            }
            if (Widgets.ButtonText(new Rect(bottom.center.x - 50f, bottom.y, 100f, 30f), LanguageManager.Get("Close")))
                Close();
        }

        private void DrawSubTab(Rect rect, string label, bool selected, System.Action onClick)
        {
            if (selected) Widgets.DrawHighlightSelected(rect);
            else Widgets.DrawHighlightIfMouseover(rect);
            if (Widgets.ButtonInvisible(rect)) onClick();
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawGrid<T>(Rect rect, List<T> options, System.Func<T, Texture> getIcon, System.Func<T, string> getLabel, System.Func<T, bool> isSelected, System.Action<T> onSelect) where T : class
        {
            if (options.Count == 0) return;
            Rect inner = rect.ContractedBy(8f);
            float gridW = inner.width - 16f;
            float cellSize = gridW / IconsPerRow;
            int rows = Mathf.CeilToInt((float)options.Count / IconsPerRow);
            float viewH = rows * (cellSize + 4f) + 6f;
            Rect viewR = new Rect(0f, 0f, gridW, viewH);
            Widgets.BeginScrollView(inner, ref scrollPos, viewR);
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                int col = i % IconsPerRow; int row = i / IconsPerRow;
                Rect cell = new Rect(col * cellSize, row * (cellSize + 4f), cellSize, cellSize).ContractedBy(4f);
                Rect texR = cell.ContractedBy(9f);
                if (Mouse.IsOver(cell)) Widgets.DrawLightHighlight(cell);
                if (isSelected(opt)) { Widgets.DrawBox(cell); Widgets.DrawHighlight(cell); }
                if (Widgets.ButtonInvisible(cell)) onSelect(opt);
                Texture tex = getIcon(opt);
                if (tex != null) GUI.DrawTexture(texR, tex, ScaleMode.ScaleToFit);
                TooltipHandler.TipRegion(cell, getLabel(opt));
            }
            Widgets.EndScrollView();
        }

        // ── SKIN TAB — using Listing_Standard for guaranteed layout ──
        private void DrawSkinTab(Rect rect)
        {
            float viewH = 270f;
            Rect viewR = new Rect(0f, 0f, rect.width - 16f, viewH);
            Widgets.BeginScrollView(rect, ref skinScrollPos, viewR);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewR);

            listing.Label("<b>" + LanguageManager.Get("SkinColor") + "</b>");
            Color curSkin = appearance.skinColor ?? new Color(0.8f, 0.6f, 0.45f);
            Rect skinRow = listing.GetRect(36f);
            Widgets.DrawBoxSolid(new Rect(skinRow.x, skinRow.y + 2f, 50f, 32f), curSkin);
            if (Widgets.ButtonText(new Rect(skinRow.x + 58f, skinRow.y + 4f, 120f, 28f), LanguageManager.Get("PickColor")))
                Find.WindowStack.Add(new Window_ColorPicker(curSkin, c => { appearance.skinColor = c; FactionGearEditor.MarkDirty(); }));
            if (Widgets.ButtonText(new Rect(skinRow.x + 184f, skinRow.y + 4f, 60f, 28f), LanguageManager.Get("Clear")))
            { appearance.skinColor = null; FactionGearEditor.MarkDirty(); }
            listing.Gap(12f);

            listing.Label("<b>" + LanguageManager.Get("HairColor") + "</b>");
            Color curHair = appearance.hairColor ?? new Color(0.2f, 0.12f, 0.05f);
            Rect hairRow = listing.GetRect(36f);
            Widgets.DrawBoxSolid(new Rect(hairRow.x, hairRow.y + 2f, 50f, 32f), curHair);
            if (Widgets.ButtonText(new Rect(hairRow.x + 58f, hairRow.y + 4f, 120f, 28f), LanguageManager.Get("PickColor")))
                Find.WindowStack.Add(new Window_ColorPicker(curHair, c => { appearance.hairColor = c; FactionGearEditor.MarkDirty(); }));
            if (Widgets.ButtonText(new Rect(hairRow.x + 184f, hairRow.y + 4f, 60f, 28f), LanguageManager.Get("Clear")))
            { appearance.hairColor = null; FactionGearEditor.MarkDirty(); }
            listing.Gap(12f);

            listing.End();
            Widgets.EndScrollView();
        }

        private void DrawCosmeticGenes(Rect rect)
        {
            if (!ModsConfig.BiotechActive) { Widgets.Label(new Rect(0f, 0f, rect.width, 28f), "Biotech DLC required."); return; }
            var genes = DefDatabase<GeneDef>.AllDefs.Where(g => g.displayCategory != null && (g.displayCategory.defName.Contains("Cosmetic") || (g.biostatCpx == 0 && g.biostatMet == 0))).OrderBy(g => g.LabelCap.ToString()).ToList();
            if (genes.Count == 0) { Widgets.Label(new Rect(0f, 0f, rect.width, 28f), "No cosmetic genes found."); return; }
            Rect inner = rect.ContractedBy(8f);
            float gridW = inner.width - 16f;
            float cellSize = gridW / IconsPerRow;
            int rows = Mathf.CeilToInt((float)genes.Count / IconsPerRow);
            float viewH = rows * (cellSize + 4f) + 6f;
            Widgets.BeginScrollView(inner, ref scrollPos, new Rect(0f, 0f, gridW, viewH));
            for (int i = 0; i < genes.Count; i++)
            {
                var gene = genes[i]; int col = i % IconsPerRow; int row = i / IconsPerRow;
                Rect cell = new Rect(col * cellSize, row * (cellSize + 4f), cellSize, cellSize).ContractedBy(4f);
                Rect texR = cell.ContractedBy(8f);
                if (Mouse.IsOver(cell)) Widgets.DrawLightHighlight(cell);
                if (gene.Icon != null) { GUI.color = gene.IconColor; GUI.DrawTexture(texR, gene.Icon, ScaleMode.ScaleToFit); GUI.color = Color.white; }
                TooltipHandler.TipRegion(cell, gene.LabelCap + "\n" + (gene.description ?? ""));
            }
            Widgets.EndScrollView();
        }

        private static Texture2D GetBodyTex(BodyTypeDef def)
        {
            if (def == null) return BaseContent.GreyTex;
            try { var g = GraphicDatabase.Get<Graphic_Multi>(def.bodyNakedGraphicPath, ShaderDatabase.CutoutSkin, Vector2.one, Color.white); return g?.MatSouth?.mainTexture as Texture2D ?? BaseContent.GreyTex; }
            catch { return BaseContent.GreyTex; }
        }
        private static Texture2D GetHeadTex(HeadTypeDef def)
        {
            if (def == null) return BaseContent.GreyTex;
            try { var g = GraphicDatabase.Get<Graphic_Multi>(def.graphicPath, ShaderDatabase.CutoutSkin, Vector2.one, Color.white); return g?.MatSouth?.mainTexture as Texture2D ?? BaseContent.GreyTex; }
            catch { return BaseContent.GreyTex; }
        }
    }
}
