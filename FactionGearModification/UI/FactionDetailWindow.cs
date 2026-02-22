using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using FactionGearCustomizer;

namespace FactionGearCustomizer.UI
{
    public class FactionDetailWindow : Window
    {
        private FactionDef factionDef;
        private string tempLabel;
        private string tempDescription;
        private TechLevel tempTechLevel;

        public override Vector2 InitialSize => new Vector2(500f, 600f);

        public FactionDetailWindow(FactionDef factionDef)
        {
            if (factionDef == null)
            {
                Log.Error("[FactionGearCustomizer] FactionDetailWindow opened with null factionDef.");
                return;
            }
            this.factionDef = factionDef;
            this.tempLabel = factionDef.label ?? factionDef.defName;
            this.tempDescription = factionDef.description ?? "";
            this.tempTechLevel = factionDef.techLevel;
            
            this.doCloseX = true;
            this.forcePause = true;
            this.draggable = true;
            this.resizeable = false;
            this.closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (factionDef == null)
            {
                Close();
                return;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), LanguageManager.Get("EditFactionDetails") + ": " + factionDef.defName);
            Text.Font = GameFont.Small;

            Listing_Standard listing = new Listing_Standard();
            Rect outRect = new Rect(0f, 40f, inRect.width, inRect.height - 90f);
            listing.Begin(outRect);

            // Label
            listing.Label(LanguageManager.Get("Label") + ":");
            tempLabel = listing.TextEntry(tempLabel);
            listing.Gap();

            // Tech Level
            listing.Label(LanguageManager.Get("TechLevel") + ":");
            string techLabel = ((string)("TechLevel." + tempTechLevel).Translate());
            if (listing.ButtonText(techLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (TechLevel level in Enum.GetValues(typeof(TechLevel)))
                {
                    options.Add(new FloatMenuOption((string)("TechLevel." + level).Translate(), () => tempTechLevel = level));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            listing.Gap();

            // Description
            listing.Label(LanguageManager.Get("Description") + ":");
            Rect descRect = listing.GetRect(200f);
            tempDescription = Widgets.TextArea(descRect, tempDescription);
            listing.Gap();

            listing.End();

            // Buttons
            float btnWidth = 120f;
            float btnHeight = 40f;
            float spacing = 20f;
            float startX = (inRect.width - (btnWidth * 2 + spacing)) / 2f;
            float btnY = inRect.height - btnHeight;

            if (Widgets.ButtonText(new Rect(startX, btnY, btnWidth, btnHeight), "Apply"))
            {
                ApplyChanges();
                Close();
            }

            if (Widgets.ButtonText(new Rect(startX + btnWidth + spacing, btnY, btnWidth, btnHeight), "Cancel"))
            {
                Close();
            }
        }

        private void ApplyChanges()
        {
            factionDef.label = tempLabel;
            factionDef.description = tempDescription;
            factionDef.techLevel = tempTechLevel;
            
            // Mark dirty to update UI
            FactionGearEditor.MarkDirty();
            Messages.Message("Faction details updated (In-Memory Only)", MessageTypeDefOf.TaskCompletion, false);
        }
    }
}
