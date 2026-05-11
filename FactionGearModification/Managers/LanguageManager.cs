using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Verse;

namespace FactionGearCustomizer
{
    public static class LanguageManager
    {
        private static Dictionary<string, string> strings = new Dictionary<string, string>();
        private static string currentLanguage = "English";

        private static readonly Dictionary<string, string[]> LanguageAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "Russian", new[] { "Russian (Русский)", "Русский", "ru", "ru-ru" } },
            { "English", new[] { "English (English)", "en", "en-us", "en-gb" } },
            { "ChineseSimplified", new[] { "Chinese (Simplified)", "ChineseSimplified (简体中文)", "简体中文", "zh-hans", "zh-cn" } },
            { "ChineseTraditional", new[] { "Chinese (Traditional)", "ChineseTraditional (繁體中文)", "繁體中文", "zh-hant", "zh-tw" } }
        };

        private static bool dataLoaded = false;
        private static string lastGameLanguage = null;
        private static string cachedModPath = null;

        public static string CurrentLanguage => currentLanguage;

        public static void Initialize(ModContentPack content = null)
        {
            if (content != null)
                cachedModPath = content.RootDir;
        }

        private static void EnsureLoaded()
        {
            string gameLanguage = LanguageDatabase.activeLanguage?.folderName ?? "English";
            if (dataLoaded && lastGameLanguage == gameLanguage) return;

            try
            {
                currentLanguage = ResolveLanguageOrFallback(gameLanguage);
                LoadStrings(currentLanguage);
                lastGameLanguage = gameLanguage;
                dataLoaded = true;
            }
            catch (Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] Failed to init language '{gameLanguage}': {ex}");
                strings.Clear();
                dataLoaded = true;
            }
        }

        private static void LoadStrings(string language)
        {
            strings.Clear();

            string modPath = GetModPath();
            if (string.IsNullOrEmpty(modPath))
            {
                Log.Error("[FactionGearCustomizer] GetModPath returned null — cannot load language files");
                return;
            }

            string xmlPath = GetKeyedFilePath(modPath, language);
            if (xmlPath == null)
            {
                Log.Error($"[FactionGearCustomizer] Keyed file not found for language '{language}' in mod: {modPath}");
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(xmlPath);

                XmlNode root = doc.SelectSingleNode("LanguageData");
                if (root == null)
                {
                    Log.Error($"[FactionGearCustomizer] No LanguageData root in: {xmlPath}");
                    return;
                }

                int count = 0;
                foreach (XmlNode node in root.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        strings[node.Name] = node.InnerText;
                        count++;
                    }
                }
                Log.Message($"[FactionGearCustomizer] Loaded {count} keys for '{language}'");
            }
            catch (Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] XML parse error for '{xmlPath}': {ex.Message}");
            }
        }

        public static string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "MISSING_KEY";

            EnsureLoaded();

            if (strings.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value))
                return value;

            string fallback = key.Translate();
            if (fallback != key && !string.IsNullOrWhiteSpace(fallback))
                return fallback;

            return key;
        }

        public static string Get(string key, params object[] args)
        {
            string format = Get(key);
            if (args == null || args.Length == 0)
                return format;

            try { return string.Format(format, args); }
            catch { return format; }
        }

        private static string ResolveLanguageOrFallback(string requestedLanguage)
        {
            string modPath = GetModPath();
            if (string.IsNullOrEmpty(modPath)) return "English";

            string resolved = ResolveLanguageFolderName(modPath, requestedLanguage);
            return !string.IsNullOrEmpty(resolved) ? resolved : "English";
        }

        private static string GetKeyedFilePath(string modPath, string language)
        {
            foreach (string root in GetLanguageRoots(modPath))
            {
                string path = Path.Combine(root, language, "Keyed", "FactionGearCustomizer.xml");
                if (File.Exists(path)) return path;
            }
            return null;
        }

        private static IEnumerable<string> GetLanguageRoots(string modPath)
        {
            return new[]
            {
                Path.Combine(modPath, "1.6", "Languages"),
                Path.Combine(modPath, "Languages"),
                Path.Combine(modPath, "1.6", "Language"),
                Path.Combine(modPath, "Language")
            };
        }

        private static bool HasKeyedFile(string modPath, string language)
        {
            return GetKeyedFilePath(modPath, language) != null;
        }

        private static string ResolveLanguageFolderName(string modPath, string requestedLanguage)
        {
            if (string.IsNullOrWhiteSpace(requestedLanguage)) return null;

            if (HasKeyedFile(modPath, requestedLanguage))
                return requestedLanguage;

            foreach (string candidate in GetLanguageCandidates(requestedLanguage))
            {
                if (HasKeyedFile(modPath, candidate))
                    return candidate;
            }

            string normalized = NormalizeLanguageToken(requestedLanguage);
            if (string.IsNullOrEmpty(normalized)) return null;

            foreach (string folder in GetAvailableLanguages(modPath))
            {
                if (NormalizeLanguageToken(folder) == normalized)
                    return folder;
            }

            return null;
        }

        private static IEnumerable<string> GetAvailableLanguages(string modPath)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in GetLanguageRoots(modPath))
            {
                if (!Directory.Exists(root)) continue;
                foreach (string dir in Directory.GetDirectories(root))
                {
                    string folderName = Path.GetFileName(dir);
                    string keyedPath = Path.Combine(dir, "Keyed", "FactionGearCustomizer.xml");
                    if (File.Exists(keyedPath) && !string.IsNullOrWhiteSpace(folderName))
                        names.Add(folderName);
                }
            }

            if (names.Count == 0) names.Add("English");
            return names.OrderBy(x => x).ToArray();
        }

        private static IEnumerable<string> GetLanguageCandidates(string requestedLanguage)
        {
            var candidates = new List<string>();
            string trimmed = requestedLanguage?.Trim();
            if (string.IsNullOrEmpty(trimmed)) return candidates;

            candidates.Add(trimmed);

            int bracketIndex = trimmed.IndexOf(" (", StringComparison.Ordinal);
            if (bracketIndex > 0)
                candidates.Add(trimmed.Substring(0, bracketIndex).Trim());

            foreach (var pair in LanguageAliases)
            {
                if (pair.Key.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    pair.Value.Any(v => v.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
                {
                    candidates.Add(pair.Key);
                    candidates.AddRange(pair.Value);
                }
            }

            return candidates
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizeLanguageToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var chars = value.Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray();
            return new string(chars);
        }

        private static string GetModPath()
        {
            if (!string.IsNullOrEmpty(cachedModPath))
                return cachedModPath;

            var mod = LoadedModManager.RunningMods.FirstOrDefault(
                m => m.PackageId == "yancy.factiongearcustomizer");
            if (mod != null)
                cachedModPath = mod.RootDir;

            return cachedModPath;
        }
    }
}
