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
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Draw Custom Header (Title)
            float headerHeight = 30f;
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, headerHeight);
            DrawCustomHeader(headerRect);

            // Draw Top Bar (Toolbar)
            float topBarHeight = 40f;
            Rect topBarRect = new Rect(inRect.x, inRect.y + headerHeight, inRect.width, topBarHeight);
            TopBarPanel.Draw(topBarRect);

            // Draw Editor Content below Header and TopBar
            Rect contentRect = new Rect(inRect.x + 10f, inRect.y + headerHeight + topBarHeight + 10f, inRect.width - 20f, inRect.height - headerHeight - topBarHeight - 20f);
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
