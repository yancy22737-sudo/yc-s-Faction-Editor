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
            // Draw Top Bar
            float topBarHeight = 40f;
            Rect topBarRect = new Rect(inRect.x, inRect.y, inRect.width, topBarHeight);
            TopBarPanel.Draw(topBarRect);

            // Draw Editor Content below TopBar
            Rect contentRect = new Rect(inRect.x + 10f, inRect.y + topBarHeight + 10f, inRect.width - 20f, inRect.height - topBarHeight - 20f);
            FactionGearEditor.DrawEditor(contentRect);
        }
    }
}
