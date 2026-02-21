using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer;

namespace FactionGearCustomizer.UI
{
    public class FactionGearMainTabWindow : MainTabWindow
    {
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

        public override void DoWindowContents(Rect inRect)
        {
            FactionGearEditor.DrawEditor(inRect);
        }
    }
}
