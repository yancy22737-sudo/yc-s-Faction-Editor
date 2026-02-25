using UnityEngine;
using Verse;
using HarmonyLib;
using System.Reflection;
using FactionGearCustomizer.UI;
using FactionGearCustomizer.UI.Panels;
using FactionGearCustomizer.Managers;

namespace FactionGearCustomizer
{
    public class FactionGearCustomizerMod : Mod
    {
        public static FactionGearCustomizerSettings Settings { get; private set; }
        private bool editorInitialized;
        public static Harmony HarmonyInstance { get; private set; }

        public FactionGearCustomizerMod(ModContentPack content) : base(content)
        {
            LanguageManager.Initialize(content);
            Settings = GetSettings<FactionGearCustomizerSettings>();

            // Initialize Harmony patches
            HarmonyInstance = new Harmony("FactionGearCustomizer");
            PatchAllSafely();

            // 在静态构造函数中清理缓存，确保每次游戏启动时都是干净状态
            FactionDefManager.ClearOriginalDataCache();

            // 延迟保存原始数据，等待DefDatabase完全加载
            LongEventHandler.QueueLongEvent(() =>
            {
                FactionDefManager.SaveAllOriginalData();
            }, "SavingFactionOriginalData", false, null);
        }

        private void PatchAllSafely()
        {
            try
            {
                // Patch Patch_GeneratePawn explicitly (core functionality)
                var generatePawnPatch = typeof(Patch_GeneratePawn);
                HarmonyInstance.PatchAll(generatePawnPatch.Assembly);
                Log.Message("[FactionGearCustomizer] Harmony patches applied successfully.");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] Failed to apply Harmony patches: {ex}");
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (!editorInitialized)
            {
                FactionGearEditor.InitializeWorkingSettings(true);
                FactionGearEditor.RefreshAllCaches();
                editorInitialized = true;
            }

            float topBarHeight = 40f;
            Rect topBarRect = new Rect(inRect.x, inRect.y, inRect.width, topBarHeight);
            TopBarPanel.Draw(topBarRect);

            Rect contentRect = new Rect(inRect.x + 10f, inRect.y + topBarHeight + 10f, inRect.width - 20f, inRect.height - topBarHeight - 20f);
            FactionGearEditor.DrawEditor(contentRect);
        }

        public override string SettingsCategory()
        {
            // 此标题会由 RimWorld 自动绘制在 Mod 设置窗口顶部
            // 请勿在 UI 代码中重复绘制，否则会导致标题重叠
            return LanguageManager.Get("FactionGearCustomizer");
        }
    }
}
