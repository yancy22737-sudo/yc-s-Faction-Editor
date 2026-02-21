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

        public static string CurrentLanguage => currentLanguage;

        private static ModContentPack modContent;

        public static void Initialize(ModContentPack content = null)
        {
            if (content != null) modContent = content;
            initialized = true;
        }

        private static void EnsureLoaded()
        {
            if (dataLoaded) return;

            string gameLanguage = LanguageDatabase.activeLanguage?.folderName ?? "English";
            Log.Message($"[FactionGearCustomizer] Detecting language. Game language: {gameLanguage}");
            
            if (gameLanguage.Contains("Chinese") || gameLanguage.Contains("中文") || gameLanguage == "SimplifiedChinese")
            {
                currentLanguage = "SimplifiedChinese";
            }
            else
            {
                currentLanguage = "English";
            }
            
            LoadStrings(currentLanguage);
            dataLoaded = true;
        }

        private static void LoadStrings(string language)
        {
            strings.Clear();
            
            string modPath = modContent?.RootDir;
            
            if (modPath == null)
            {
                modPath = LoadedModManager.RunningMods
                    .FirstOrDefault(m => m.Name == "Faction Gear Customizer")?.RootDir;
            }
            
            if (modPath == null)
            {
                Log.Warning("[FactionGearCustomizer] Could not find mod root directory for language files.");
                return;
            }

            string xmlPath = Path.Combine(modPath, "Language", language, "Strings.xml");
            Log.Message($"[FactionGearCustomizer] Loading language file from: {xmlPath}");
            
            if (!File.Exists(xmlPath))
            {
                Log.Warning($"[FactionGearCustomizer] Language file not found at: {xmlPath}");
                return;
            }

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
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to load language file: {ex.Message}");
            }
        }

        public static string Get(string key)
        {
            EnsureLoaded();

            if (strings.TryGetValue(key, out string value))
            {
                return value;
            }

            return key;
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

        public static IEnumerable<string> AvailableLanguages => new[] { "English", "SimplifiedChinese" };
    }
}
