using System;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI
{
    public class Dialog_ConfirmWithCheckbox : Window
    {
        private string text;
        private Action confirmedAction;
        private string confirmButtonText;
        private string cancelButtonText;
        private Action cancelledAction;
        private string dialogId;
        private bool doNotShowAgain;
        private float buttonHeight = 40f;
        private Vector2 windowSize = new Vector2(500f, 180f);
        private string checkboxLabel;
        private Action<bool> onCheckboxChanged;

        public Dialog_ConfirmWithCheckbox(
            string text,
            string dialogId,
            Action confirmedAction,
            string confirmButtonText = "Confirm",
            string cancelButtonText = "Cancel",
            Action cancelledAction = null,
            bool isDeleteWarning = false)
        {
            this.text = text;
            this.dialogId = dialogId;
            this.confirmedAction = confirmedAction;
            this.confirmButtonText = confirmButtonText;
            this.cancelButtonText = cancelButtonText;
            this.cancelledAction = cancelledAction;
            this.doNotShowAgain = false;
            this.checkboxLabel = null;

            if (isDeleteWarning)
            {
                windowSize = new Vector2(500f, 220f);
            }

            forcePause = false;
            doCloseX = true;
            doCloseButton = false;
            closeOnCancel = true;
            absorbInputAroundWindow = true;

            windowRect = new Rect((Verse.UI.screenWidth - windowSize.x) / 2, (Verse.UI.screenHeight - windowSize.y) / 2, windowSize.x, windowSize.y);
        }

        public static Dialog_ConfirmWithCheckbox CreateWithAutoSaveOption(
            string text,
            string checkboxLabel,
            Action onSaveAndConfirm,
            Action onConfirmOnly,
            Action<bool> onCheckboxChanged)
        {
            var dialog = new Dialog_ConfirmWithCheckbox(text, "", onConfirmOnly);
            dialog.confirmedAction = onSaveAndConfirm;
            dialog.checkboxLabel = checkboxLabel;
            dialog.onCheckboxChanged = onCheckboxChanged;
            dialog.windowSize = new Vector2(500f, 200f);
            dialog.closeOnCancel = true;
            return dialog;
        }

        public static bool ShowIfNotDismissed(string dialogId, string text, Action confirmedAction, string confirmButtonText = "Confirm", string cancelButtonText = "Cancel", Action cancelledAction = null, bool isDeleteWarning = false)
        {
            var settings = FactionGearCustomizerMod.Settings;
            if (settings.IsDialogDismissed(dialogId))
            {
                confirmedAction?.Invoke();
                return false;
            }

            var dialog = new Dialog_ConfirmWithCheckbox(text, dialogId, confirmedAction, confirmButtonText, cancelButtonText, cancelledAction, isDeleteWarning);
            Find.WindowStack.Add(dialog);
            return true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, LanguageManager.Get("Confirm"));

            Rect textRect = new Rect(inRect.x, titleRect.yMax + 10f, inRect.width, inRect.height - 100f);
            Text.Font = GameFont.Small;
            Widgets.Label(textRect, text);

            float checkboxY = textRect.yMax + 15f;
            
            if (checkboxLabel != null)
            {
                Rect checkboxRect = new Rect(inRect.x, checkboxY, inRect.width, 24f);
                Widgets.CheckboxLabeled(checkboxRect, checkboxLabel, ref doNotShowAgain);
                onCheckboxChanged?.Invoke(doNotShowAgain);
            }
            else
            {
                Rect checkboxRect = new Rect(inRect.x, checkboxY, inRect.width, 24f);
                Widgets.CheckboxLabeled(checkboxRect, LanguageManager.Get("DontShowAgain"), ref doNotShowAgain);
            }

            float buttonY = inRect.yMax - buttonHeight - 10f;
            float buttonWidth = (inRect.width - 30f) / 2f;

            if (checkboxLabel != null)
            {
                Rect saveButtonRect = new Rect(inRect.x, buttonY, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(saveButtonRect, LanguageManager.Get("Save")))
                {
                    confirmedAction?.Invoke();
                    Close();
                }

                Rect confirmButtonRect = new Rect(inRect.x + buttonWidth + 30f, buttonY, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(confirmButtonRect, LanguageManager.Get("Preview")))
                {
                    var confirmOnlyAction = cancelledAction;
                    confirmOnlyAction?.Invoke();
                    Close();
                }
            }
            else
            {
                Rect cancelButtonRect = new Rect(inRect.x, buttonY, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(cancelButtonRect, cancelButtonText))
                {
                    if (doNotShowAgain && !string.IsNullOrEmpty(dialogId))
                    {
                        FactionGearCustomizerMod.Settings.DismissDialog(dialogId);
                    }
                    cancelledAction?.Invoke();
                    Close();
                }

                Rect confirmButtonRect = new Rect(inRect.x + buttonWidth + 30f, buttonY, buttonWidth, buttonHeight);
                Color originalColor = GUI.color;
                if (confirmButtonText == "Delete" || confirmButtonText == "Overwrite" || confirmButtonText == "Yes (Risk it)")
                {
                    GUI.color = new Color(1f, 0.3f, 0.3f);
                }
                if (Widgets.ButtonText(confirmButtonRect, confirmButtonText))
                {
                    if (doNotShowAgain && !string.IsNullOrEmpty(dialogId))
                    {
                        FactionGearCustomizerMod.Settings.DismissDialog(dialogId);
                    }
                    confirmedAction?.Invoke();
                    Close();
                }
                GUI.color = originalColor;
            }
        }
    }
}
