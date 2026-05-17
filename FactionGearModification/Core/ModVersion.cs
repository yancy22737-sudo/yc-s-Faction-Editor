using System.IO;
using System.Linq;
using System.Xml;
using Verse;

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
                    _cachedVersion = "v" + ReadModVersionFromAbout();
                }
                return _cachedVersion;
            }
        }

        private static string ReadModVersionFromAbout()
        {
            try
            {
                var mod = LoadedModManager.RunningMods.FirstOrDefault(
                    m => m.PackageId == "yancy.factiongearcustomizer");
                if (mod == null) return "unknown";

                string aboutPath = Path.Combine(mod.RootDir, "About", "About.xml");
                if (!File.Exists(aboutPath)) return "unknown";

                XmlDocument doc = new XmlDocument();
                doc.Load(aboutPath);
                var node = doc.SelectSingleNode("ModMetaData/modVersion");
                return node?.InnerText ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        public static string GetChangelog()
        {
            return $"{LanguageManager.Get("Version")}: {Current}\n\n{LanguageManager.Get("Changelog")}:\n- Fix: {LanguageManager.Get("Changelog_FixWikiLang")}\n- Add: {LanguageManager.Get("Changelog_AddWikiUI")}";
        }
    }
}
