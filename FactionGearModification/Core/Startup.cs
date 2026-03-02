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
                // 【修复】关键：在任何修改 Def 之前先保存原始数据
                FactionDefManager.SaveAllOriginalData();
                
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
