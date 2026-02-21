using RimWorld;
using Verse;
using FactionGearCustomizer.UI;

namespace FactionGearCustomizer
{
    public class MainButtonWorker_FactionGear : MainButtonWorker
    {
        public override bool Visible
        {
            get
            {
                return FactionGearCustomizerMod.Settings.ShowInMainTab && base.Visible;
            }
        }

        public override void Activate()
        {
            Find.WindowStack.Add(new FactionGearMainTabWindow());
        }
    }
}
