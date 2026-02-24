using System.Reflection;

namespace FactionGearCustomizer
{
    public static class ModVersion
    {
        public static string Current
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                var ver = assembly.GetName().Version;
                return $"v{ver.Major}.{ver.Minor}.{ver.Build}";
            }
        }
        
        public static string GetChangelog()
        {
            return $"{LanguageManager.Get("Version")}: {Current}\n\n{LanguageManager.Get("Changelog")}:\n- Fix: {LanguageManager.Get("Changelog_FixWikiLang")}\n- Add: {LanguageManager.Get("Changelog_AddWikiUI")}";
        }
    }
}
