using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI
{
    public class Dialog_TextureBrowser : Window
    {
        private Action<string> onSelect;
        private List<string> allTextures = new List<string>();
        private List<string> filteredTextures = new List<string>();
        private string filter = "";
        private Vector2 scrollPos;
        
        private const float IconSize = 64f;
        private const float Gap = 8f;

        public override Vector2 InitialSize => new Vector2(800f, 600f);

        public Dialog_TextureBrowser(Action<string> onSelect)
        {
            this.onSelect = onSelect;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            
            LoadTextures();
        }

        private void LoadTextures()
        {
            HashSet<string> paths = new HashSet<string>();
            
            // Custom Icons
            foreach (string iconName in CustomIconManager.GetAllIconNames())
            {
                paths.Add("Custom:" + iconName);
            }
            
            // Collect faction icons
            foreach (var f in DefDatabase<FactionDef>.AllDefs)
            {
                if (!string.IsNullOrEmpty(f.factionIconPath)) paths.Add(f.factionIconPath);
                if (!string.IsNullOrEmpty(f.settlementTexturePath)) paths.Add(f.settlementTexturePath);
            }
            
            // Collect settlement/world object textures
            foreach (var w in DefDatabase<WorldObjectDef>.AllDefs)
            {
                if (!string.IsNullOrEmpty(w.texture)) paths.Add(w.texture);
                if (!string.IsNullOrEmpty(w.expandingIconTexture)) paths.Add(w.expandingIconTexture);
            }

            allTextures = paths.OrderBy(x => x).ToList();
            filteredTextures = new List<string>(allTextures);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            
            // Search Bar
            Rect searchRect = new Rect(0, 0, inRect.width - 130f, 30f);
            string newFilter = Widgets.TextField(searchRect, filter);
            if (newFilter != filter)
            {
                filter = newFilter;
                filteredTextures = allTextures
                    .Where(x => x.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            // Import Button
            if (Widgets.ButtonText(new Rect(inRect.width - 120f, 0, 120f, 30f), "Import Icon"))
            {
                Find.WindowStack.Add(new Dialog_ImportIcon(() => 
                {
                    LoadTextures();
                }));
            }

            // Grid
            Rect outRect = new Rect(0, 40f, inRect.width, inRect.height - 40f);
            
            float itemWidth = IconSize + Gap;
            float itemHeight = IconSize + Gap;
            int cols = Mathf.FloorToInt((outRect.width - 16f) / itemWidth);
            if (cols < 1) cols = 1;
            
            int rows = Mathf.CeilToInt((float)filteredTextures.Count / cols);
            float viewHeight = rows * itemHeight;
            
            Rect viewRect = new Rect(0, 0, outRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

            int index = 0;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (index >= filteredTextures.Count) break;
                    
                    string path = filteredTextures[index];
                    Rect itemRect = new Rect(j * itemWidth, i * itemHeight, IconSize, IconSize);

                    // Draw Texture
                    Texture2D tex = null;
                    if (path.StartsWith("Custom:"))
                    {
                        tex = CustomIconManager.GetIcon(path.Substring(7));
                    }
                    else
                    {
                        tex = ContentFinder<Texture2D>.Get(path, false);
                    }

                    if (tex != null)
                    {
                        GUI.DrawTexture(itemRect, tex);
                    }
                    else
                    {
                        Widgets.DrawBoxSolid(itemRect, Color.red);
                    }

                    // Highlight & Tooltip
                    if (Mouse.IsOver(itemRect))
                    {
                        Widgets.DrawHighlight(itemRect);
                        TooltipHandler.TipRegion(itemRect, path);
                        
                        // Right-click to delete custom icon
                        if (path.StartsWith("Custom:") && Event.current.type == EventType.MouseDown && Event.current.button == 1)
                        {
                            Event.current.Use();
                            List<FloatMenuOption> options = new List<FloatMenuOption>();
                            options.Add(new FloatMenuOption("Delete", () => 
                            {
                                CustomIconManager.DeleteIcon(path.Substring(7));
                                LoadTextures();
                            }));
                            Find.WindowStack.Add(new FloatMenu(options));
                        }
                    }

                    // Click
                    if (Widgets.ButtonInvisible(itemRect))
                    {
                        onSelect?.Invoke(path);
                        Close();
                    }

                    index++;
                }
            }

            Widgets.EndScrollView();
        }
    }
}
