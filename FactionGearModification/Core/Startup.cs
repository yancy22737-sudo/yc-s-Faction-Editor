using RimWorld;
using Verse;
using FactionGearCustomizer.Managers;

namespace FactionGearCustomizer
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            try 
            {
                // Apply Faction/Kind Def changes on startup
                FactionDefManager.ApplyAllSettings();
                
                // FactionGearCustomizerMod.UpdateMainButtonVisibility();
            }
            catch (System.Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] Critical error in Startup: {ex}");
            }
        }
    }
}
