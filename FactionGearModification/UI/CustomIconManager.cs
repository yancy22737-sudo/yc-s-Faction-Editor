using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI
{
    public static class CustomIconManager
    {
        private static string saveDir;
        private static Dictionary<string, Texture2D> cachedIcons = new Dictionary<string, Texture2D>();

        public static string SaveDir
        {
            get
            {
                if (saveDir == null)
                {
                    saveDir = Path.Combine(GenFilePaths.SaveDataFolderPath, "FactionIcons");
                    if (!Directory.Exists(saveDir))
                    {
                        Directory.CreateDirectory(saveDir);
                    }
                }
                return saveDir;
            }
        }

        public static void ClearCache()
        {
            foreach (var tex in cachedIcons.Values)
            {
                UnityEngine.Object.Destroy(tex);
            }
            cachedIcons.Clear();
        }

        public static List<string> GetAllIconNames()
        {
            if (!Directory.Exists(SaveDir)) return new List<string>();
            
            return Directory.GetFiles(SaveDir, "*.png")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(x => x)
                .ToList();
        }

        public static Texture2D GetIcon(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (cachedIcons.TryGetValue(name, out Texture2D tex) && tex != null)
            {
                return tex;
            }

            try
            {
                string path = Path.Combine(SaveDir, name + ".png");
                if (File.Exists(path))
                {
                    byte[] data = File.ReadAllBytes(path);
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(data))
                    {
                        tex.name = name;
                        tex.filterMode = FilterMode.Bilinear;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        cachedIcons[name] = tex;
                        return tex;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to load icon {name}: {ex.Message}");
            }
            return null;
        }

        public static void SaveIcon(Texture2D tex, string name)
        {
            string path = Path.Combine(SaveDir, name + ".png");
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            
            // Update cache
            if (cachedIcons.ContainsKey(name))
            {
                UnityEngine.Object.Destroy(cachedIcons[name]);
                cachedIcons.Remove(name);
            }
            // Reload immediately to ensure format is correct
            GetIcon(name);
        }

        public static void DeleteIcon(string name)
        {
            string path = Path.Combine(SaveDir, name + ".png");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            
            if (cachedIcons.ContainsKey(name))
            {
                UnityEngine.Object.Destroy(cachedIcons[name]);
                cachedIcons.Remove(name);
            }
        }
    }
}
