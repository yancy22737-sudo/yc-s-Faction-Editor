using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer;
using FactionGearCustomizer.UI.Panels;

namespace FactionGearCustomizer.UI
{
    public class FactionGearMainTabWindow : MainTabWindow
    {
        public override Vector2 InitialSize => new Vector2(1024f, 768f); // Default size if not fullscreen
        protected override float Margin => 0f; // Remove default margin so we can draw custom title bar

        public override void PreOpen()
        {
            base.PreOpen();
            this.optionalTitle = null; // 设置为null完全禁用RimWorld的标题绘制
            this.doWindowBackground = true;
            FactionGearEditor.InitializeWorkingSettings(true);
        }

        public override void PostClose()
        {
            base.PostClose();
            if (FactionGearEditor.IsDirty)
            {
                // In MainTabWindow, we can't easily block closing, but we can try to save or notify
                // For now, let's just log a warning and discard, or maybe auto-save?
                // Auto-save is risky if the user made a mistake.
                // Discard is safer but frustrating.
                // Let's rely on the "Apply & Save" button for now, and discard on close.
                // Ideally, we would have a confirmation dialog, but PostClose is too late.
                FactionGearEditor.DiscardChanges(); 
            }
            FactionGearEditor.Cleanup();
        }



        public override void WindowOnGUI()
        {
            // Override to remove the standard window title bar and whitespace
            // Force rect to start from top to eliminate any top whitespace
            Rect rect = new Rect(0f, 0f, Verse.UI.screenWidth, Verse.UI.screenHeight - 35f);
            this.windowRect = rect; // Sync windowRect for event handling
            
            if (this.doWindowBackground)
            {
                Widgets.DrawWindowBackground(rect);
            }
            
            // Handle BeginGroup to clip contents and use local coordinates
            GUI.BeginGroup(rect);
            try
            {
                Rect localRect = new Rect(0f, 0f, rect.width, rect.height);
                this.DoWindowContents(localRect);
            }
            finally
            {
                GUI.EndGroup();
            }
            
            // Ensure we handle window events/focus if necessary (MainTabWindow usually handles this via WindowStack)
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Handle Ctrl+S shortcut for saving
            if (Event.current.type == EventType.KeyDown && Event.current.control && Event.current.keyCode == KeyCode.S)
            {
                if (NoPresetPanel.HasActivePreset())
                {
                    FactionGearEditor.SaveChanges();
                    Event.current.Use();
                }
                else
                {
                    Messages.Message(LanguageManager.Get("NoPresetCannotSave"), MessageTypeDefOf.RejectInput, false);
                    Event.current.Use();
                }
            }

            float padding = 4f; // Reduced padding
            float topBarHeight = 36f; // Height for the top bar

            // Draw Top Bar
            Rect topBarRect = new Rect(inRect.x + padding, inRect.y + padding, inRect.width - padding * 2, topBarHeight);
            FactionGearCustomizer.UI.Panels.TopBarPanel.Draw(topBarRect);

            // Draw Editor Content
            Rect contentRect = new Rect(
                inRect.x + padding,
                inRect.y + padding + topBarHeight + 4f, // Add spacing below top bar
                inRect.width - padding * 2,
                inRect.height - padding * 2 - topBarHeight - 4f
            );

            FactionGearEditor.DrawEditor(contentRect);
        }
    }
}
