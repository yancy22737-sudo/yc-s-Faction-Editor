using Verse;

namespace FactionGearCustomizer.Utils
{
    public static class LogUtils
    {
        private const string LOG_PREFIX = "[FactionGearCustomizer] ";

        public static void DebugLog(string message)
        {
            if (FactionGearCustomizerMod.Settings != null && FactionGearCustomizerMod.Settings.enableDebugLog)
            {
                Log.Message(LOG_PREFIX + message);
            }
        }

        public static void DebugLog(string format, params object[] args)
        {
            if (FactionGearCustomizerMod.Settings != null && FactionGearCustomizerMod.Settings.enableDebugLog)
            {
                Log.Message(LOG_PREFIX + string.Format(format, args));
            }
        }

        public static void Info(string message)
        {
            Log.Message(LOG_PREFIX + message);
        }

        public static void Warning(string message)
        {
            Log.Warning(LOG_PREFIX + message);
        }

        public static void Error(string message)
        {
            Log.Error(LOG_PREFIX + message);
        }

        public static void Error(string message, System.Exception ex)
        {
            Log.Error(LOG_PREFIX + message + "\n" + ex.ToString());
        }
    }
}
