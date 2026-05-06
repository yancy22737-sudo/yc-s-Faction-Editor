using System.Reflection;

namespace FactionGearCustomizer
{
    public static class ModVersion
    {
        private static string _cachedVersion;

        public static string Current
        {
            get
            {
                if (_cachedVersion == null)
                {
                    var attr = Assembly.GetExecutingAssembly()
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                    string ver = attr?.InformationalVersion ?? "unknown";
                    _cachedVersion = ver.StartsWith("v") ? ver : "v" + ver;
                }
                return _cachedVersion;
            }
        }

        public static string GetChangelog()
        {
            return $"{LanguageManager.Get("Version")}: {Current}\n\n{LanguageManager.Get("Changelog")}:\n- Fix: {LanguageManager.Get("Changelog_FixWikiLang")}\n- Add: {LanguageManager.Get("Changelog_AddWikiUI")}";
        }
    }
}
