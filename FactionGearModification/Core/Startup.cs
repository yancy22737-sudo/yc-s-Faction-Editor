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
                
                // 运行 factionLeader 字段测试
                Debug_FactionLeaderTest.RunTest();
                
                // 运行假体应用功能测试
                Debug_HediffApplicationTest.RunTest();
                
                // FactionGearCustomizerMod.UpdateMainButtonVisibility();
            }
            catch (System.Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] Critical error in Startup: {ex}");
            }
        }
    }
}
