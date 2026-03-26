using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
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
        private static bool initialized = false;
        private static bool dataLoaded = false;
        private static string lastGameLanguage = null;

        public static string CurrentLanguage => currentLanguage;

        private static ModContentPack modContent;

        public static void Initialize(ModContentPack content = null)
        {
            if (content != null) modContent = content;
            initialized = true;
        }

        private static void EnsureLoaded()
        {
            string gameLanguage = LanguageDatabase.activeLanguage?.folderName ?? "English";
            if (dataLoaded && lastGameLanguage == gameLanguage) return;

            try
            {
                Log.Message($"[yc's Faction Editor] Detecting language change or init. Game language: {gameLanguage}");
                currentLanguage = ResolveLanguageOrFallback(gameLanguage);
                LoadStrings(currentLanguage);
                lastGameLanguage = gameLanguage;
            }
            catch (Exception ex)
            {
                Log.Warning($"[yc's Faction Editor] Failed to initialize language: {ex.Message}");
                strings.Clear();
            }
            finally
            {
                dataLoaded = true;
            }
        }

        private static void LoadStrings(string language)
        {
            strings.Clear();
            
            string modPath = modContent?.RootDir;
            Log.Message($"[yc's Faction Editor] LoadStrings called. modContent null: {modContent == null}, modPath: {modPath}");
            
            // Try to find mod path if not initialized
            if (modPath == null)
            {
                var mod = LoadedModManager.RunningMods
                    .FirstOrDefault(m => m.Name == "yc's Faction Editor" || 
                                         m.PackageId == "yancy.factiongearcustomizer");
                modPath = mod?.RootDir;
                Log.Message($"[yc's Faction Editor] Found mod via fallback. Name: {mod?.Name}, RootDir: {modPath}");
            }

            // If still null, list all running mods for debugging
            if (modPath == null)
            {
                Log.Warning("[yc's Faction Editor] Could not find mod. All running mods:");
                foreach (var m in LoadedModManager.RunningMods)
                {
                    Log.Message($"  - {m.Name}: {m.RootDir}");
                }
                return;
            }

            string xmlPath = GetStringsFilePath(modPath, language);
            if (xmlPath == null)
            {
                Log.Warning($"[FactionGearCustomizer] Language file not found for: {language}");
                return;
            }

            Log.Message($"[yc's Faction Editor] Loading language file from: {xmlPath}");

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(xmlPath);
                
                XmlNode root = doc.SelectSingleNode("LanguageData");
                if (root != null)
                {
                    int count = 0;
                    foreach (XmlNode node in root.ChildNodes)
                    {
                        if (node.NodeType == XmlNodeType.Element)
                        {
                            strings[node.Name] = node.InnerText;
                            count++;
                        }
                    }
                    Log.Message($"[FactionGearCustomizer] Loaded {count} strings for language: {language}");
                }
                else
                {
                    Log.Warning($"[yc's Faction Editor] Language file has no LanguageData root: {xmlPath}");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[yc's Faction Editor] Failed to load language file: {ex.Message}");
            }
        }

        public static string Get(string key)
        {
            EnsureLoaded();

            if (strings.TryGetValue(key, out string value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    return key ?? "MISSING_KEY";

                return value;
            }

            return key ?? "MISSING_KEY";
        }

        public static string Get(string key, params object[] args)
        {
            string format = Get(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }

        public static void SetLanguage(string language)
        {
            string resolvedLanguage = ResolveLanguageOrFallback(language);
            if (currentLanguage != resolvedLanguage)
            {
                currentLanguage = resolvedLanguage;
                LoadStrings(resolvedLanguage);
            }
        }

        public static IEnumerable<string> AvailableLanguages
        {
            get
            {
                string modPath = GetModPath();
                if (string.IsNullOrEmpty(modPath))
                {
                    return new[] { "English" };
                }

                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string root in GetLanguageRoots(modPath))
                {
                    if (!Directory.Exists(root)) continue;
                    foreach (string dir in Directory.GetDirectories(root))
                    {
                        string folderName = Path.GetFileName(dir);
                        string stringsPath = Path.Combine(dir, "Strings.xml");
                        if (!File.Exists(stringsPath) || string.IsNullOrWhiteSpace(folderName)) continue;
                        names.Add(folderName);
                    }
                }

                if (names.Count == 0) names.Add("English");
                return names.OrderBy(x => x).ToArray();
            }
        }

        private static string ResolveLanguageOrFallback(string requestedLanguage)
        {
            string modPath = GetModPath();
            if (string.IsNullOrEmpty(modPath))
            {
                Log.Warning("[FactionGearCustomizer] Mod path missing. Fail-fast fallback to English.");
                return "English";
            }

            string resolved = ResolveLanguageFolderName(modPath, requestedLanguage);
            if (!string.IsNullOrEmpty(resolved))
            {
                return resolved;
            }

            Log.Warning($"[FactionGearCustomizer] Missing language '{requestedLanguage}'. Fail-fast fallback to English.");
            return "English";
        }

        private static string GetStringsFilePath(string modPath, string language)
        {
            foreach (string root in GetLanguageRoots(modPath))
            {
                string path = Path.Combine(root, language, "Strings.xml");
                Log.Message($"[FactionGearCustomizer] Checking language file at: {path}");
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

        private static bool HasStringsFile(string modPath, string language)
        {
            return GetStringsFilePath(modPath, language) != null;
        }

        private static string ResolveLanguageFolderName(string modPath, string requestedLanguage)
        {
            if (string.IsNullOrWhiteSpace(requestedLanguage)) return null;

            if (HasStringsFile(modPath, requestedLanguage))
            {
                return requestedLanguage;
            }

            foreach (string candidate in GetLanguageCandidates(requestedLanguage))
            {
                if (HasStringsFile(modPath, candidate))
                {
                    Log.Message($"[FactionGearCustomizer] Resolved language '{requestedLanguage}' to folder '{candidate}'.");
                    return candidate;
                }
            }

            string normalized = NormalizeLanguageToken(requestedLanguage);
            if (string.IsNullOrEmpty(normalized)) return null;
            foreach (string folder in AvailableLanguages)
            {
                if (NormalizeLanguageToken(folder) != normalized) continue;
                Log.Message($"[FactionGearCustomizer] Resolved language '{requestedLanguage}' to folder '{folder}' by normalized alias match.");
                return folder;
            }

            return null;
        }

        private static IEnumerable<string> GetLanguageCandidates(string requestedLanguage)
        {
            var candidates = new List<string>();
            string trimmed = requestedLanguage?.Trim();
            if (string.IsNullOrEmpty(trimmed)) return candidates;

            candidates.Add(trimmed);

            int bracketIndex = trimmed.IndexOf(" (", StringComparison.Ordinal);
            if (bracketIndex > 0)
            {
                candidates.Add(trimmed.Substring(0, bracketIndex).Trim());
            }

            int slashIndex = trimmed.IndexOf(" / ", StringComparison.Ordinal);
            if (slashIndex > 0)
            {
                candidates.Add(trimmed.Substring(0, slashIndex).Trim());
            }

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
            string modPath = modContent?.RootDir;
            if (!string.IsNullOrEmpty(modPath)) return modPath;

            var mod = LoadedModManager.RunningMods.FirstOrDefault(
                m => m.Name == "yc's Faction Editor" || m.PackageId == "yancy.factiongearcustomizer");
            return mod?.RootDir;
        }
    }
}
