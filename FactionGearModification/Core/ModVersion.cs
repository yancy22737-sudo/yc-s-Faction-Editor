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
            return "Version: " + Current + "\n\nChanges:\n- UI optimizations\n- Layout improvements";
        }
    }
}
