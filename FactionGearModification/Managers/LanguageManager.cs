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

            // If already loaded and language hasn't changed, do nothing
            if (dataLoaded && lastGameLanguage == gameLanguage) return;

            try
            {
                Log.Message($"[yc's Faction Editor] Detecting language change or init. Game language: {gameLanguage}");
                
                if (gameLanguage.Contains("Chinese") || gameLanguage.Contains("中文") || gameLanguage == "ChineseSimplified")
                {
                    currentLanguage = "ChineseSimplified";
                }
                else
                {
                    currentLanguage = "English";
                }
                
                LoadStrings(currentLanguage);
                lastGameLanguage = gameLanguage;
            }
            catch (Exception ex)
            {
                Log.Warning($"[yc's Faction Editor] Failed to initialize language: {ex.Message}");
                // Fallback to avoid null reference later
                strings.Clear();
            }
            finally
            {
                // Always set loaded to true to prevent infinite retry loops if it fails
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

            // Try multiple possible paths (Languages with 's' is RimWorld standard)
            string[] possiblePaths = new[]
            {
                Path.Combine(modPath, "1.6", "Languages", language, "Strings.xml"),
                Path.Combine(modPath, "Languages", language, "Strings.xml"),
                Path.Combine(modPath, "1.6", "Language", language, "Strings.xml"),
                Path.Combine(modPath, "Language", language, "Strings.xml")
            };

            string xmlPath = null;
            foreach (var path in possiblePaths)
            {
                Log.Message($"[FactionGearCustomizer] Checking language file at: {path}");
                if (File.Exists(path))
                {
                    xmlPath = path;
                    break;
                }
            }
            
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
            if (currentLanguage != language)
            {
                currentLanguage = language;
                LoadStrings(language);
            }
        }

        public static IEnumerable<string> AvailableLanguages => new[] { "English", "ChineseSimplified" };
    }
}
