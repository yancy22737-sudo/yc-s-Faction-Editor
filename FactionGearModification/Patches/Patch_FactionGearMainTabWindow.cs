using HarmonyLib;
using Verse;
using FactionGearCustomizer.UI;

namespace FactionGearCustomizer.Patches
{
    [HarmonyPatch(typeof(Window), "WindowOnGUI")]
    public static class Patch_FactionGearMainTabWindow
    {
        [HarmonyPriority(Priority.Last)]
        public static void Prefix(Window __instance)
        {
            if (__instance is FactionGearMainTabWindow)
            {
                __instance.doWindowBackground = true;
            }
        }
    }
}
