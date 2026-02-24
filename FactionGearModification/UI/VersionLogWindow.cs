using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public class VersionLogWindow : Window
    {
        private string versionLogContent;
        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(500f, 600f);

        public VersionLogWindow()
        {
            this.doCloseX = true;
            this.forcePause = false;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
            this.resizeable = true;
            LoadVersionLog();
        }

        private void LoadVersionLog()
        {
            try
            {
                Assembly assembly = Assembly.GetAssembly(typeof(VersionLogWindow));
                string modDir = Path.GetDirectoryName(assembly.Location);

                string gameLanguage = LanguageDatabase.activeLanguage?.folderName ?? "English";
                bool isChinese = gameLanguage.Contains("Chinese") || gameLanguage.Contains("中文") || gameLanguage == "ChineseSimplified";
                string preferredFile = isChinese ? "VersionLog.txt" : "VersionLog_en.txt";

                string[] possiblePaths = new string[]
                {
                    Path.Combine(modDir, preferredFile),
                    Path.Combine(modDir, "..", preferredFile),
                    Path.Combine(modDir, "..", "..", preferredFile),
                    Path.Combine(modDir, "VersionLog.txt"),
                    Path.Combine(modDir, "..", "VersionLog.txt"),
                    Path.Combine(modDir, "..", "..", "VersionLog.txt")
                };

                string versionLogPath = null;
                foreach (var path in possiblePaths)
                {
                    string fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        versionLogPath = fullPath;
                        break;
                    }
                }

                if (versionLogPath != null)
                {
                    versionLogContent = File.ReadAllText(versionLogPath);
                }
                else
                {
                    versionLogContent = "VersionLog.txt not found.\n" +
                        "Tried paths:\n" + 
                        string.Join("\n", possiblePaths.Select(p => Path.GetFullPath(p)));
                }
            }
            catch (Exception ex)
            {
                versionLogContent = $"Error loading VersionLog: {ex.Message}";
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            float buttonHeight = 40f;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, LanguageManager.Get("VersionLog"));
            Text.Font = GameFont.Small;

            Rect scrollRect = new Rect(inRect.x, titleRect.yMax + 10f, inRect.width, inRect.height - buttonHeight - titleRect.height - 30f);
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, Text.CalcHeight(versionLogContent, scrollRect.width - 16f));

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);
            Widgets.Label(new Rect(0f, 0f, viewRect.width, viewRect.height), versionLogContent);
            Widgets.EndScrollView();

            Rect buttonRect = new Rect(inRect.x, scrollRect.yMax + 10f, inRect.width, buttonHeight);
            if (Widgets.ButtonText(buttonRect, LanguageManager.Get("GiveMeALike"), true, true, true))
            {
                Application.OpenURL("https://steamcommunity.com/comment/5/bounce/76561199183744193/3670833973/?feature2=18446744073709551615&tscn=1771753489");
            }
        }
    }
}
