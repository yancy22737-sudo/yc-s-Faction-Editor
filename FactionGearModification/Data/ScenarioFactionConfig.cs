using System.Collections.Generic;
using RimWorld;
using Verse;
using FactionGearCustomizer.Core;

namespace FactionGearCustomizer
{
    public class ScenarioFactionConfig : IExposable
    {
        public string presetName;
        public List<string> selectedFactions = new List<string>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref presetName, "presetName");
            Scribe_Collections.Look(ref selectedFactions, "selectedFactions", LookMode.Value);
        }
    }
}