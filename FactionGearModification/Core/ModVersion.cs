namespace FactionGearCustomizer
{
    public static class ModVersion
    {
        private const string CurrentVersion = "v1.5.3";

        public static string Current
        {
            get
            {
                return CurrentVersion;
            }
        }
        
        public static string GetChangelog()
        {
            return $"{LanguageManager.Get("Version")}: {Current}\n\n{LanguageManager.Get("Changelog")}:\n- Fix: {LanguageManager.Get("Changelog_FixWikiLang")}\n- Add: {LanguageManager.Get("Changelog_AddWikiUI")}";
        }
    }
}
