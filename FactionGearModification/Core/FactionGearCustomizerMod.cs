using UnityEngine;
using Verse;
using HarmonyLib;
using System.Reflection;
using FactionGearCustomizer.UI;
using FactionGearCustomizer.UI.Panels;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer.Core;
using FactionGearCustomizer.Utils;

namespace FactionGearCustomizer
{
    public class FactionGearCustomizerMod : Mod
    {
        public static FactionGearCustomizerSettings Settings { get; private set; }
        private bool editorInitialized;
        public static Harmony HarmonyInstance { get; private set; }
        
        // 【修复】用于检测是否需要清理残留数据的静态标志
        // 当从存档返回主菜单时，这个标志会被设置为true
        private static bool needsCleanupAfterReturnToMenu = false;

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
                LogUtils.Info("Harmony patches applied successfully.");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] Failed to apply Harmony patches: {ex}");
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // 【关键修复】只在明确标记了需要清理时才执行清理！
            // 不要在游戏外（主菜单）打开UI时自动清理，这会干扰预设加载功能
            // 【重要】只有在没有活跃预设的情况下才执行清理，避免清除用户刚刚加载的预设
            if (needsCleanupAfterReturnToMenu && string.IsNullOrEmpty(Settings.currentPresetName))
            {
                // 检查是否在主菜单（非游戏状态）
                bool isInMainMenu = Current.Game == null || Current.ProgramState != ProgramState.Playing;
                
                if (isInMainMenu)
                {
                    LogUtils.Info("Detected residual save data in main menu, cleaning up...");
                    
                    // 【关键修复】首先恢复所有FactionDef到原始状态！
                    // 这是解决"派系名称、颜色等数据残留"问题的核心
                    FactionDefManager.ResetAllFactions();
                    
                    // 清理残留数据
                    Settings.factionGearData?.Clear();
                    Settings.factionGearDataDict?.Clear();
                    // 【修复】只在currentPresetName为空时才重置，避免清除用户刚刚加载的预设
                    Settings.currentPresetName = null;
                    
                    // 【关键修复】调用Write()持久化清理操作，防止RimWorld从配置文件重新加载残留数据
                    Settings.Write();
                    
                    // 【关键修复】不要清理原始数据缓存！
                    // 如果清理了原始数据缓存，后续恢复操作就没有数据可用了
                    // FactionDefManager.ClearOriginalDataCache();
                    EditorSession.ResetSession();
                    UndoManager.Clear();
                    
                    // 【修复】清理派系列表缓存
                    FactionListPanel.ClearCache();
                    KindListPanel.ClearCache();
                    
                    // 重置标志
                    needsCleanupAfterReturnToMenu = false;
                    
                    LogUtils.Info("Residual data cleanup completed.");
                }
            }
            else if (needsCleanupAfterReturnToMenu)
            {
                // 如果用户已经有活跃预设，只重置标志而不清理数据
                needsCleanupAfterReturnToMenu = false;
                LogUtils.Info("User has active preset, skipping cleanup to preserve preset selection.");
            }
            
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
        
        /// <summary>
        /// 【修复】标记需要在返回主菜单后清理数据
        /// 由Patch_GameInit在检测到返回主菜单时调用
        /// </summary>
        public static void MarkNeedsCleanupAfterReturnToMenu()
        {
            needsCleanupAfterReturnToMenu = true;
            LogUtils.DebugLog("Marked for cleanup after returning to main menu.");
        }
        
        /// <summary>
        /// 【修复】检查并清理残留数据
        /// 在打开任何UI窗口前调用，确保数据干净
        /// </summary>
        public static void CheckAndCleanupResidualData()
        {
            // 【关键修复】只在明确标记了需要清理时才执行清理！
            // 不要在游戏外（主菜单）打开UI时自动清理，这会干扰预设加载功能
            // 【重要】只有在没有活跃预设的情况下才执行清理，避免清除用户刚刚加载的预设
            if (!needsCleanupAfterReturnToMenu)
            {
                return;
            }
            
            // 如果用户已经有活跃预设，只重置标志而不清理数据
            if (!string.IsNullOrEmpty(Settings.currentPresetName))
            {
                needsCleanupAfterReturnToMenu = false;
                LogUtils.Info("CheckAndCleanupResidualData: User has active preset, skipping cleanup to preserve preset selection.");
                return;
            }
            
            // 检查是否在主菜单（非游戏状态）
            bool isInMainMenu = Current.Game == null || Current.ProgramState != ProgramState.Playing;
            
            if (isInMainMenu)
            {
                LogUtils.Info("CheckAndCleanupResidualData: Cleaning up residual save data...");
                
                // 【关键修复】首先恢复所有FactionDef到原始状态！
                // 这是解决"派系名称、颜色等数据残留"问题的核心
                FactionDefManager.ResetAllFactions();
                
                // 清理残留数据
                Settings.factionGearData?.Clear();
                Settings.factionGearDataDict?.Clear();
                // 【修复】只在currentPresetName为空时才重置，避免清除用户刚刚加载的预设
                Settings.currentPresetName = null;
                
                // 【关键修复】调用Write()持久化清理操作，防止RimWorld从配置文件重新加载残留数据
                Settings.Write();
                
                // 【关键修复】不要清理原始数据缓存！
                // 如果清理了原始数据缓存，后续恢复操作就没有数据可用了
                // FactionDefManager.ClearOriginalDataCache();
                
                EditorSession.ResetSession();
                UndoManager.Clear();
                
                // 【修复】清理派系列表缓存
                FactionListPanel.ClearCache();
                KindListPanel.ClearCache();
                
                // 重置标志
                needsCleanupAfterReturnToMenu = false;
                
                LogUtils.Info("CheckAndCleanupResidualData: Cleanup completed and persisted.");
            }
        }

        /// <summary>
        /// 执行完整的深度清理（包括恢复DefDatabase、清理设置、刷新缓存等）
        /// 这个方法用于所有需要深度清理的场景
        /// </summary>
        /// <param name="reason">清理原因，用于日志输出</param>
        /// <param name="resetPresetName">是否重置当前预设名称，默认为true</param>
        public static void PerformDeepCleanup(string reason = "", bool resetPresetName = true)
        {
            try
            {
                string logPrefix = string.IsNullOrEmpty(reason) ? "" : $" ({reason})";
                LogUtils.Info($"Detected return to main menu, cleaning up save data...{logPrefix}");
                LogUtils.Info($"Starting comprehensive cleanup on return to main menu...{logPrefix}");
                
                // 【关键修复】首先恢复所有FactionDef到原始状态！
                // 这是解决"派系名称、颜色等数据残留"问题的核心
                FactionDefManager.ResetAllFactions();
                
                // 【关键修复】重置游戏组件中的派系数据，防止上次界面修改的残留数据影响新游戏
                var gameComponent = FactionGearGameComponent.Instance;
                if (gameComponent != null)
                {
                    gameComponent.ResetFactionData();
                    LogUtils.Info($"Faction data reset.{logPrefix}");
                }
                
                // 【修复】标记需要在返回主菜单后清理数据
                MarkNeedsCleanupAfterReturnToMenu();
                
                // 同时立即执行清理（以防设置窗口当前已打开）
                // 1. 清理全局设置中的存档数据
                if (Settings.factionGearData != null)
                {
                    Settings.factionGearData.Clear();
                }
                
                if (Settings.factionGearDataDict != null)
                {
                    Settings.factionGearDataDict.Clear();
                }
                
                // 重置当前预设名称（可选）
                if (resetPresetName)
                {
                    Settings.currentPresetName = null;
                }
                
                // 【关键修复】调用Write()持久化清理操作，防止RimWorld从配置文件重新加载残留数据
                Settings.Write();
                
                // 3. 重置编辑器会话状态
                EditorSession.ResetSession();
                
                // 4. 清理撤销管理器
                UndoManager.Clear();
                
                // 5. 刷新UI缓存 - 使用ClearCache彻底清理，而不是MarkDirty
                FactionListPanel.ClearCache();
                KindListPanel.ClearCache();
                FactionGearEditor.RefreshAllCaches();
                
                LogUtils.Info($"All caches refreshed.{logPrefix}");
                LogUtils.Info($"Save data cleanup completed and persisted.{logPrefix}");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Error during deep cleanup: {ex.Message}");
            }
        }
    }
}
