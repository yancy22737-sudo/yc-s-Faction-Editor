using System;
using UnityEngine;
using Verse;
using RimWorld;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer;
using FactionGearCustomizer.UI.Panels;

namespace FactionGearCustomizer.UI
{
    public class FactionGearSettingsWindow : Window
    {
        public override Vector2 InitialSize => new Vector2(900f, 700f);

        public FactionGearSettingsWindow()
        {
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.draggable = true;
            this.resizeable = true;
            this.optionalTitle = LanguageManager.Get("FactionGearCustomizer");
        }

        public override void PreOpen()
        {
            base.PreOpen();
            this.optionalTitle = "";
            
            // 【修复】检查并清理残留数据
            FactionGearCustomizerMod.CheckAndCleanupResidualData();
            
            FactionGearEditor.InitializeWorkingSettings(true);
        }

        public override void PostClose()
        {
            base.PostClose();
            if (FactionGearEditor.IsDirty)
            {
                // Unsaved changes handling
                 Find.WindowStack.Add(new Dialog_MessageBox(
                    LanguageManager.Get("UnsavedChangesMessage"),
                    LanguageManager.Get("Save"),
                    () => {
                        FactionGearEditor.SaveChanges();
                        Messages.Message(LanguageManager.Get("SettingsSaved"), MessageTypeDefOf.PositiveEvent, false);
                    },
                    LanguageManager.Get("Discard"),
                    () => {
                        FactionGearEditor.DiscardChanges();
                        Messages.Message(LanguageManager.Get("SettingsDiscarded"), MessageTypeDefOf.PositiveEvent, false);
                    },
                    LanguageManager.Get("UnsavedChanges"),
                    false,
                    null,
                    null
                ));
            }
            FactionGearEditor.Cleanup();

            // 如果从创建世界界面打开，关闭时重置标记
            if (Dialogs.Dialog_FactionEditorLite.IsOpenedFromWorldCreation)
            {
                Dialogs.Dialog_FactionEditorLite.ResetWorldCreationFlag();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 检查是否在游戏中
            bool inGame = Current.Game != null;
            
            // Draw Custom Header (Title)
            float headerHeight = 30f;
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, headerHeight);
            DrawCustomHeader(headerRect);

            // 如果不在游戏中，显示提示
            if (!inGame)
            {
                Rect warningRect = new Rect(inRect.x, inRect.y + headerHeight + 5f, inRect.width, 25f);
                GUI.color = new Color(1f, 0.8f, 0.3f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(warningRect, LanguageManager.Get("P2_NotInGameWarning"));
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            // Draw Top Bar (Toolbar)
            float topBarHeight = 40f;
            float topBarY = inRect.y + headerHeight + (inGame ? 0f : 30f);
            Rect topBarRect = new Rect(inRect.x, topBarY, inRect.width, topBarHeight);
            TopBarPanel.Draw(topBarRect);

            // Draw Editor Content below Header and TopBar
            float contentY = topBarY + topBarHeight + 10f;
            Rect contentRect = new Rect(inRect.x + 10f, contentY, inRect.width - 20f, inRect.yMax - contentY - 10f);
            FactionGearEditor.DrawEditor(contentRect);
        }

        private void DrawCustomHeader(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.9f, 0.85f, 0.7f);
            Widgets.Label(inRect, LanguageManager.Get("FactionGearCustomizer"));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.DrawLineHorizontal(inRect.x, inRect.yMax - 2f, inRect.width, new Color(0.3f, 0.3f, 0.3f));
        }
    }
}
