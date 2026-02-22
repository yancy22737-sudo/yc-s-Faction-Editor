using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public class FactionGearCustomizerMod : Mod
    {
        public static FactionGearCustomizerSettings Settings;

        public FactionGearCustomizerMod(ModContentPack content) : base(content)
        {
            LanguageManager.Initialize(content);
            Settings = GetSettings<FactionGearCustomizerSettings>();
            Log.Message("[FactionGearCustomizer] Loading success!");
            var harmony = new Harmony("yancy.factiongearcustomizer");
            harmony.PatchAll();
        }

        public override string SettingsCategory() => "Faction Gear Customizer";

        public override void WriteSettings()
        {
            if (FactionGearEditor.IsDirty)
            {
                // Unsaved changes detected - ask user for confirmation
                Find.WindowStack.Add(new Dialog_MessageBox(
                    LanguageManager.Get("UnsavedChangesMessage"),
                    LanguageManager.Get("Save"),
                    () => {
                        FactionGearEditor.SaveChanges();
                        Messages.Message(LanguageManager.Get("SettingsSaved"), MessageTypeDefOf.TaskCompletion, false);
                    },
                    LanguageManager.Get("Discard"),
                    () => {
                        FactionGearEditor.DiscardChanges();
                        Messages.Message(LanguageManager.Get("SettingsDiscarded"), MessageTypeDefOf.TaskCompletion, false);
                    },
                    LanguageManager.Get("UnsavedChanges"),
                    false,
                    null,
                    null
                ));
            }
            else
            {
                base.WriteSettings();
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            FactionGearEditor.DrawEditor(inRect);
        }
    }
}