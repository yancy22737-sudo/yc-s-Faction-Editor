using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Verse;

namespace FactionGearCustomizer
{
    /// <summary>
    /// Legacy wrapper kept to preserve existing call sites while preferring RimWorld's
    /// native translation pipeline and providing deterministic file-based fallback.
    /// </summary>
    public static class LanguageManager
    {
        private static readonly Dictionary<string, Dictionary<string, string>> FallbackCache =
            new Dictionary<string, Dictionary<string, string>>();

        private static readonly Dictionary<string, string[]> LanguageAliases =
            new Dictionary<string, string[]>
            {
                { "ChineseTraditional", new[] { "ChineseTraditional", "ChineseTraditional繁體中文", "ChineseTraditionalChineseTraditional", "ChineseTraditionalChinese", "ChineseTraditionalTraditional", "ChineseTraditionalzhhant", "ChineseTraditionalzhtw", "ChineseTraditionalcht", "ChineseTraditionaltc", "ChineseTraditionaltraditionalchinese", "ChineseTraditionalchinesetraditional", "ChineseTraditionalhant", "ChineseTraditionalzhtraditional", "ChineseTraditional繁体中文", "ChineseTraditionaltraditional" } },
                { "ChineseSimplified", new[] { "ChineseSimplified", "ChineseSimplified简体中文", "ChineseSimplifiedChineseSimplified", "ChineseSimplifiedChinese", "ChineseSimplifiedSimplified", "ChineseSimplifiedzhhans", "ChineseSimplifiedzhcn", "ChineseSimplifiedchs", "ChineseSimplifiedsc", "ChineseSimplifiedsimplifiedchinese", "ChineseSimplifiedchinesesimplified", "ChineseSimplifiedhans", "ChineseSimplifiedzhsimplified", "ChineseSimplified简体", "ChineseSimplifiedsimplified" } },
                { "English", new[] { "English", "EnglishEnglish", "Englishen", "Englishenus", "Englishengb" } }
            };

        private static readonly Dictionary<string, string> NormalizedAliasMap =
            BuildNormalizedAliasMap();

        private static ModContentPack modContent;
        private static string lastLanguageToken;
        private static string[] activeFallbackLanguages = new[] { "English" };

        public static string CurrentLanguage =>
            LanguageDatabase.activeLanguage?.folderName ?? "English";

        public static IEnumerable<string> AvailableLanguages
        {
            get
            {
                string root = GetLanguageRoot();
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                {
                    return new[] { "English" };
                }

                return Directory.GetDirectories(root)
                    .Select(Path.GetFileName)
                    .Where(folderName => !string.IsNullOrWhiteSpace(folderName))
                    .Distinct()
                    .OrderBy(folderName => folderName)
                    .ToArray();
            }
        }

        public static void Initialize(ModContentPack content = null)
        {
            if (content != null)
            {
                modContent = content;
            }

            FallbackCache.Clear();
            lastLanguageToken = null;
            activeFallbackLanguages = new[] { "English" };
        }

        public static string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return "MISSING_KEY";
            }

            EnsureLanguageState();
            foreach (string language in activeFallbackLanguages)
            {
                if (!TryGetFromFallbackFile(language, key, out string value))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return key.Translate().ToString();
        }

        public static string Get(string key, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return "MISSING_KEY";
            }

            string translated = Get(key);
            if (args == null || args.Length == 0)
            {
                return translated;
            }

            try
            {
                return string.Format(translated, args);
            }
            catch
            {
                return translated;
            }
        }

        public static void SetLanguage(string language)
        {
            Log.Warning("[FactionGearCustomizer] LanguageManager.SetLanguage is obsolete. Use RimWorld language settings instead.");
        }

        private static void EnsureLanguageState()
        {
            string currentToken = CurrentLanguage;
            if (string.Equals(lastLanguageToken, currentToken))
            {
                return;
            }

            lastLanguageToken = currentToken;
            string current = ResolveCanonicalLanguage(currentToken);
            if (string.Equals(current, "ChineseTraditional"))
            {
                activeFallbackLanguages = new[] { "ChineseTraditional", "ChineseSimplified", "English" };
                return;
            }

            activeFallbackLanguages = string.Equals(current, "English")
                ? new[] { "English" }
                : new[] { current, "English" };
        }

        private static bool TryGetFromFallbackFile(string language, string key, out string value)
        {
            value = null;
            Dictionary<string, string> data = GetLanguageFallbackData(language);
            return data != null && data.TryGetValue(key, out value);
        }

        private static Dictionary<string, string> GetLanguageFallbackData(string language)
        {
            string canonicalLanguage = ResolveCanonicalLanguage(language);
            if (string.IsNullOrWhiteSpace(canonicalLanguage))
            {
                return null;
            }

            if (FallbackCache.TryGetValue(canonicalLanguage, out Dictionary<string, string> cached))
            {
                return cached;
            }

            string path = GetKeyedFilePath(canonicalLanguage);
            var data = new Dictionary<string, string>();
            if (!File.Exists(path))
            {
                FallbackCache[canonicalLanguage] = data;
                return data;
            }

            try
            {
                var document = new XmlDocument();
                document.Load(path);
                XmlNode root = document.SelectSingleNode("LanguageData");
                if (root == null)
                {
                    FallbackCache[canonicalLanguage] = data;
                    return data;
                }

                foreach (XmlNode node in root.ChildNodes)
                {
                    if (node.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }

                    data[node.Name] = node.InnerText;
                }
            }
            catch (XmlException ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to parse fallback language file '{path}': {ex.Message}");
            }
            catch (IOException ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to read fallback language file '{path}': {ex.Message}");
            }

            FallbackCache[canonicalLanguage] = data;
            return data;
        }

        private static string GetKeyedFilePath(string language)
        {
            return Path.Combine(GetLanguageRoot(), language, "Keyed", "FactionGearCustomizer.xml");
        }

        private static string GetLanguageRoot()
        {
            if (modContent == null)
            {
                return string.Empty;
            }

            return Path.Combine(modContent.RootDir, "1.6", "Languages");
        }

        private static string ResolveCanonicalLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return string.Empty;
            }

            string normalized = NormalizeLanguageToken(language);
            if (LooksLikeTraditionalChinese(normalized))
            {
                return "ChineseTraditional";
            }

            if (LooksLikeSimplifiedChinese(normalized))
            {
                return "ChineseSimplified";
            }

            if (NormalizedAliasMap.TryGetValue(normalized, out string canonical))
            {
                return canonical;
            }

            return language.Trim();
        }

        private static string NormalizeLanguageToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private static bool LooksLikeTraditionalChinese(string normalized)
        {
            return normalized.Contains("zhtw") ||
                   normalized.Contains("zhhant") ||
                   normalized.Contains("traditionalchinese") ||
                   normalized.Contains("chinesetraditional") ||
                   (normalized.Contains("traditional") && normalized.Contains("chinese")) ||
                   normalized.Contains("cht") ||
                   normalized.Contains("tc") ||
                   normalized.Contains("hant");
        }

        private static bool LooksLikeSimplifiedChinese(string normalized)
        {
            return normalized.Contains("zhcn") ||
                   normalized.Contains("zhhans") ||
                   normalized.Contains("simplifiedchinese") ||
                   normalized.Contains("chinesesimplified") ||
                   (normalized.Contains("simplified") && normalized.Contains("chinese")) ||
                   normalized.Contains("chs") ||
                   normalized.Contains("sc") ||
                   normalized.Contains("hans");
        }

        private static Dictionary<string, string> BuildNormalizedAliasMap()
        {
            var map = new Dictionary<string, string>();
            foreach (var pair in LanguageAliases)
            {
                map[NormalizeLanguageToken(pair.Key)] = pair.Key;
                foreach (string alias in pair.Value)
                {
                    map[NormalizeLanguageToken(alias)] = pair.Key;
                }
            }

            return map;
        }
    }
}
