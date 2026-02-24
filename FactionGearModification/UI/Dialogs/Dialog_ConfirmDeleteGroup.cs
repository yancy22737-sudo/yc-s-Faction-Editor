using System;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_ConfirmDeleteGroup : Window
    {
        private readonly Action confirmAction;
        private readonly string text;
        private readonly string title;
        private bool dontAskAgain;

        public override Vector2 InitialSize => new Vector2(500f, 200f);

        public Dialog_ConfirmDeleteGroup(string text, Action confirmAction, string title = null)
        {
            this.text = text;
            this.confirmAction = confirmAction;
            this.title = title;
            this.doCloseX = true;
            this.forcePause = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            if (!title.NullOrEmpty())
            {
                Widgets.Label(new Rect(0, 0, inRect.width, 35f), title);
            }
            Text.Font = GameFont.Small;

            Rect textRect = new Rect(0, 40f, inRect.width, 60f);
            Widgets.Label(textRect, text);

            Rect checkboxRect = new Rect(0, 100f, inRect.width, 24f);
            Widgets.CheckboxLabeled(checkboxRect, LanguageManager.Get("DontShowAgain"), ref dontAskAgain);

            float btnWidth = 120f;
            float btnY = inRect.height - 40f;
            
            if (Widgets.ButtonText(new Rect(inRect.width / 2f - btnWidth - 10f, btnY, btnWidth, 35f), LanguageManager.Get("Confirm")))
            {
                if (dontAskAgain)
                {
                    FactionGearCustomizerMod.Settings.suppressDeleteGroupConfirmation = true;
                    FactionGearCustomizerMod.Settings.Write();
                }
                confirmAction?.Invoke();
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.width / 2f + 10f, btnY, btnWidth, 35f), LanguageManager.Get("Cancel")))
            {
                Close();
            }
        }
    }
}