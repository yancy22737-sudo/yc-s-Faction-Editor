using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI
{
    public class WikiWindow : Window
    {
        private Vector2 scrollPosition;
        private int selectedTab;
        private readonly List<string> tabLabels = new List<string>();
        
        private static readonly string[] MascotTexturePaths = new string[]
        {
            "UI/Slipin' Coati - Friendly Waving looking left",
            "UI/Slipin' Coati - Standing Neutral Loking Right",
            "UI/Slipin' Coati - Standing Neutral  tilted Looking Right",
            "UI/Slipin' Coati - Excited Jumping looking left"
        };

        private const string SteamPageUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=3670833973";
        private const string GithubUrl = "https://github.com/yancy22737-sudo/FactionGearCustomizer";
        private const float MascotSize = 96f;
        private const float MascotMargin = 10f;

        public override Vector2 InitialSize => new Vector2(750f, 650f);

        public WikiWindow()
        {
            this.doCloseX = true;
            this.forcePause = false;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
            this.resizeable = true;
            this.closeOnClickedOutside = true;

            tabLabels.Add(LanguageManager.Get("WikiTab_Principles"));
            tabLabels.Add(LanguageManager.Get("WikiTab_Terminology"));
            tabLabels.Add(LanguageManager.Get("WikiTab_Workflow"));
            tabLabels.Add(LanguageManager.Get("WikiTab_Notes"));
        }

        public override void DoWindowContents(Rect inRect)
        {
            float headerHeight = 50f;
            float tabHeight = 38f;
            float contentY = headerHeight + tabHeight + 12f;

            DrawHeader(inRect, headerHeight);
            DrawTabs(new Rect(inRect.x, inRect.y + headerHeight, inRect.width, tabHeight));
            DrawContent(new Rect(inRect.x, inRect.y + contentY, inRect.width, inRect.height - contentY - 10f));
        }

        private void DrawHeader(Rect inRect, float height)
        {
            Rect titleRect = new Rect(inRect.x + 15f, inRect.y + 10f, inRect.width - 30f, 30f);

            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.9f, 0.85f, 0.7f);
            Widgets.Label(titleRect, "Wiki");
            Text.Font = GameFont.Small;
            GUI.color = Color.gray;

            Rect subtitleRect = new Rect(inRect.x + 15f, inRect.y + 32f, inRect.width - 30f, 20f);
            Widgets.Label(subtitleRect, LanguageManager.Get("FactionGearCustomizer"));
            GUI.color = Color.white;

            Widgets.DrawLineHorizontal(inRect.x + 10f, inRect.y + height - 5f, inRect.width - 20f, new Color(0.3f, 0.3f, 0.3f));
        }

        private void DrawTabs(Rect tabRect)
        {
            float tabWidth = (tabRect.width - 20f) / tabLabels.Count;
            float startX = tabRect.x + 10f;

            for (int i = 0; i < tabLabels.Count; i++)
            {
                Rect singleTabRect = new Rect(startX + i * tabWidth, tabRect.y, tabWidth - 4f, tabRect.height);
                bool isSelected = selectedTab == i;

                if (isSelected)
                {
                    Widgets.DrawBoxSolid(singleTabRect, new Color(0.25f, 0.35f, 0.55f));
                    GUI.color = Color.white;
                }
                else
                {
                    Widgets.DrawBoxSolid(singleTabRect, new Color(0.15f, 0.15f, 0.17f));
                    GUI.color = new Color(0.7f, 0.7f, 0.7f);
                }

                Rect textRect = singleTabRect;
                textRect.height = Text.LineHeight;
                textRect.y += (tabRect.height - textRect.height) / 2;

                if (Widgets.ButtonText(singleTabRect, tabLabels[i], false))
                {
                    selectedTab = i;
                    scrollPosition = Vector2.zero;
                }
            }
            GUI.color = Color.white;
        }

        private void DrawContent(Rect contentRect)
        {
            string content = GetContentForTab(selectedTab);
            
            float contentWidth = contentRect.width - 30f;
            float rightMargin = MascotSize + MascotMargin * 2f;
            float calculatedHeight = CalculateFormattedHeightWithWrap(content, contentWidth, rightMargin);
            Rect viewRect = new Rect(0f, 0f, contentRect.width - 20f, calculatedHeight);

            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);

            DrawMascotWithInteraction(viewRect.width);
            
            DrawFormattedContentWithWrap(content, viewRect, 0f);

            Widgets.EndScrollView();
        }

        private void DrawMascotWithInteraction(float viewWidth)
        {
            if (selectedTab < 0 || selectedTab >= MascotTexturePaths.Length)
                return;
                
            Texture2D mascotTex = ContentFinder<Texture2D>.Get(MascotTexturePaths[selectedTab], false);
            if (mascotTex == null)
                return;
                
            float x = viewWidth - MascotSize - MascotMargin;
            Rect mascotRect = new Rect(x, MascotMargin, MascotSize, MascotSize);
            GUI.DrawTexture(mascotRect, mascotTex, ScaleMode.ScaleToFit, true);
            
            string mascotTip = LanguageManager.Get("WikiMascotTip");
            TooltipHandler.TipRegion(mascotRect, mascotTip);
            
            if (Widgets.ButtonInvisible(mascotRect, false))
            {
                Application.OpenURL(SteamPageUrl);
            }
        }

        private float CalculateFormattedHeightWithWrap(string content, float width, float rightMargin)
        {
            float lineHeight = Text.LineHeight * 1.4f;
            float fullWidth = width + 30f;
            float leftWidth = fullWidth - rightMargin - 30f;
            string[] lines = content.Split('\n');
            float totalHeight = 20f;

            foreach (var line in lines)
            {
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    totalHeight += lineHeight * 1.8f;
                }
                else if (line.StartsWith("•") || line.StartsWith("- "))
                {
                    string textToMeasure = line.StartsWith("•") ? line : "• " + line.Substring(2);
                    float wrappedHeight = Text.CalcHeight(textToMeasure, leftWidth - 15f);
                    totalHeight += wrappedHeight + lineHeight * 0.1f;
                }
                else if (line.Trim() == "")
                {
                    totalHeight += lineHeight * 0.3f;
                }
                else if (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3.") ||
                         line.StartsWith("4.") || line.StartsWith("5.") || line.StartsWith("6.") || line.StartsWith("7."))
                {
                    int dotIndex = line.IndexOf('.');
                    string rest = line.Substring(dotIndex + 1);
                    float restHeight = Text.CalcHeight(rest, leftWidth - 25f);
                    totalHeight += Mathf.Max(lineHeight * 1.5f, restHeight + lineHeight * 0.1f);
                }
                else if (line.Contains(":"))
                {
                    int colonIndex = line.IndexOf(':');
                    string value = line.Substring(colonIndex + 1);
                    float valueHeight = Text.CalcHeight(value, leftWidth - 120f);
                    totalHeight += Mathf.Max(lineHeight, valueHeight + lineHeight * 0.1f);
                }
                else
                {
                    float wrappedHeight = Text.CalcHeight(line, leftWidth);
                    totalHeight += wrappedHeight + lineHeight * 0.1f;
                }
            }

            return totalHeight;
        }

        private void DrawFormattedContentWithWrap(string content, Rect viewRect, float yOffset = 0f)
        {
            float x = 15f;
            float y = yOffset;
            float fullWidth = viewRect.width - 30f;
            float rightMargin = MascotSize + MascotMargin * 2f;
            float leftWidth = fullWidth - rightMargin - 30f;
            float lineHeight = Text.LineHeight * 1.4f;

            Text.Font = GameFont.Small;
            GUI.color = new Color(0.85f, 0.85f, 0.82f);

            string[] lines = content.Split('\n');

            foreach (var line in lines)
            {
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    GUI.color = new Color(0.6f, 0.75f, 0.9f);
                    Text.Font = GameFont.Medium;
                    Widgets.Label(new Rect(x, y, fullWidth, lineHeight * 1.5f), line.Substring(1, line.Length - 2));
                    Text.Font = GameFont.Small;
                    GUI.color = new Color(0.85f, 0.85f, 0.82f);
                    y += lineHeight * 1.8f;
                }
                else if (line.StartsWith("•"))
                {
                    GUI.color = new Color(0.7f, 0.85f, 0.7f);
                    float wrappedHeight = Text.CalcHeight(line, leftWidth - 15f);
                    Widgets.Label(new Rect(x + 15f, y, leftWidth - 15f, wrappedHeight), line);
                    y += wrappedHeight + lineHeight * 0.1f;
                }
                else if (line.StartsWith("- "))
                {
                    GUI.color = new Color(0.7f, 0.85f, 0.7f);
                    string bulletLine = "• " + line.Substring(2);
                    float wrappedHeight = Text.CalcHeight(bulletLine, leftWidth - 15f);
                    Widgets.Label(new Rect(x + 15f, y, leftWidth - 15f, wrappedHeight), bulletLine);
                    y += wrappedHeight + lineHeight * 0.1f;
                }
                else if (line.Trim() == "")
                {
                    y += lineHeight * 0.3f;
                }
                else if (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3.") ||
                         line.StartsWith("4.") || line.StartsWith("5.") || line.StartsWith("6.") || line.StartsWith("7."))
                {
                    GUI.color = new Color(0.9f, 0.8f, 0.6f);
                    int dotIndex = line.IndexOf('.');
                    string num = line.Substring(0, dotIndex + 1);
                    string rest = line.Substring(dotIndex + 1);
                    float restHeight = Text.CalcHeight(rest, leftWidth - 25f);
                    Widgets.Label(new Rect(x, y, 25f, lineHeight), num);
                    GUI.color = new Color(0.85f, 0.85f, 0.82f);
                    Widgets.Label(new Rect(x + 25f, y, leftWidth - 25f, restHeight), rest);
                    y += Mathf.Max(lineHeight * 1.5f, restHeight + lineHeight * 0.1f);
                }
                else if (IsUrlLine(line))
                {
                    y += DrawClickableUrlLine(line, x, y, leftWidth);
                }
                else if (line.Contains(":"))
                {
                    int colonIndex = line.IndexOf(':');
                    string key = line.Substring(0, colonIndex + 1);
                    string value = line.Substring(colonIndex + 1);
                    float valueHeight = Text.CalcHeight(value, leftWidth - 120f);
                    GUI.color = new Color(0.75f, 0.8f, 0.95f);
                    Widgets.Label(new Rect(x, y, 120f, lineHeight), key);
                    GUI.color = new Color(0.85f, 0.85f, 0.82f);
                    Widgets.Label(new Rect(x + 120f, y, leftWidth - 120f, valueHeight), value);
                    y += Mathf.Max(lineHeight, valueHeight + lineHeight * 0.1f);
                }
                else
                {
                    GUI.color = new Color(0.85f, 0.85f, 0.82f);
                    float wrappedHeight = Text.CalcHeight(line, leftWidth);
                    Widgets.Label(new Rect(x, y, leftWidth, wrappedHeight), line);
                    y += wrappedHeight + lineHeight * 0.1f;
                }
            }

            GUI.color = Color.white;
        }

        private bool IsUrlLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            string trimmed = line.Trim();
            return trimmed.StartsWith("http://") || trimmed.StartsWith("https://");
        }

        private float DrawClickableUrlLine(string line, float x, float y, float width)
        {
            float lineHeight = Text.LineHeight * 1.4f;
            string url = line.Trim();
            string displayText = GetDisplayTextForUrl(url);
            
            GUI.color = new Color(0.4f, 0.7f, 1f);
            float textWidth = Text.CalcSize(displayText).x + 10f;
            Rect linkRect = new Rect(x, y, Mathf.Min(textWidth, width), lineHeight);
            
            GUI.DrawTexture(linkRect, BaseContent.WhiteTex);
            GUI.color = new Color(0.2f, 0.4f, 0.8f);
            Widgets.Label(linkRect, displayText);
            
            if (Mouse.IsOver(linkRect))
            {
                GUI.color = new Color(0.6f, 0.8f, 1f);
                Widgets.Label(linkRect, displayText);
                if (Widgets.ButtonInvisible(linkRect, false))
                {
                    Application.OpenURL(url);
                }
            }
            
            TooltipHandler.TipRegion(linkRect, url);
            GUI.color = Color.white;
            
            return lineHeight * 1.2f;
        }

        private string GetDisplayTextForUrl(string url)
        {
            if (url.Contains("steamcommunity.com"))
                return "[Steam创意工坊]";
            if (url.Contains("github.com"))
                return "[GitHub]";
            if (url.Contains("qq.com") || url.Contains("qq"))
                return "[QQ群]";
            return "[打开链接]";
        }

        private string GetContentForTab(int tab)
        {
            switch (tab)
            {
                case 0: return GetPrinciplesContent();
                case 1: return GetTerminologyContent();
                case 2: return GetWorkflowContent();
                case 3: return GetNotesContent();
                default: return "";
            }
        }

        private string GetPrinciplesContent()
        {
            return LanguageManager.Get("Wiki_Principles_Content");
        }

        private string GetTerminologyContent()
        {
            return LanguageManager.Get("Wiki_Terminology_Content");
        }

        private string GetWorkflowContent()
        {
            return LanguageManager.Get("Wiki_Workflow_Content");
        }

        private string GetNotesContent()
        {
            return LanguageManager.Get("Wiki_Notes_Content");
        }
    }
}
