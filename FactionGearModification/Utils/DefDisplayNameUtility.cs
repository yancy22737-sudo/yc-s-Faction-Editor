using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionGearCustomizer.Utils
{
    public static class DefDisplayNameUtility
    {
        private const string UnknownFactionFallback = "[UnnamedFaction]";
        private const string UnknownPawnKindFallback = "[UnnamedPawnKind]";
        private static readonly StringComparer SortComparer = StringComparer.OrdinalIgnoreCase;
        private static readonly HashSet<string> LoggedFallbackKeys = new HashSet<string>();

        public static string GetSafeFactionDisplayName(FactionDef factionDef, string context = null)
        {
            return GetSafeDefDisplayName(
                "FactionDef",
                factionDef?.defName,
                factionDef?.label,
                TryGetLabelCap(() => factionDef?.LabelCap.ToString()),
                UnknownFactionFallback,
                context);
        }

        public static string GetSafeFactionDisplayName(Faction faction, string context = null)
        {
            if (faction == null)
            {
                return UnknownFactionFallback;
            }

            string displayName = FirstNonEmpty(
                faction.Name,
                TryGetLabelCap(() => faction.def?.LabelCap.ToString()),
                faction.def?.label,
                faction.def?.defName,
                UnknownFactionFallback);

            if (displayName == UnknownFactionFallback)
            {
                WarnFallback("FactionInstance", faction.def?.defName ?? "null", faction.Name, context, displayName);
            }

            return displayName;
        }

        public static string GetSafePawnKindDisplayName(PawnKindDef kindDef, string context = null)
        {
            return GetSafeDefDisplayName(
                "PawnKindDef",
                kindDef?.defName,
                kindDef?.label,
                TryGetLabelCap(() => kindDef?.LabelCap.ToString()),
                UnknownPawnKindFallback,
                context);
        }

        public static string GetSafeFactionSortKey(FactionDef factionDef, string context = null)
        {
            return GetSafeFactionDisplayName(factionDef, context);
        }

        public static string GetSafeFactionSortKey(Faction faction, string context = null)
        {
            return GetSafeFactionDisplayName(faction, context);
        }

        public static string GetSafePawnKindSortKey(PawnKindDef kindDef, string context = null)
        {
            return GetSafePawnKindDisplayName(kindDef, context);
        }

        public static int CompareFactionDefs(FactionDef left, FactionDef right, string context = null)
        {
            return SortComparer.Compare(
                GetSafeFactionSortKey(left, context),
                GetSafeFactionSortKey(right, context));
        }

        public static int ComparePawnKinds(PawnKindDef left, PawnKindDef right, string context = null)
        {
            return SortComparer.Compare(
                GetSafePawnKindSortKey(left, context),
                GetSafePawnKindSortKey(right, context));
        }

        private static string GetSafeDefDisplayName(string defType, string defName, string rawLabel, string labelCap, string fallback, string context)
        {
            string displayName = FirstNonEmpty(labelCap, rawLabel, defName, fallback);
            if (displayName == fallback)
            {
                WarnFallback(defType, defName, rawLabel, context, fallback);
            }

            return displayName;
        }

        private static string TryGetLabelCap(Func<string> getter)
        {
            try
            {
                return getter?.Invoke();
            }
            catch (Exception ex)
            {
                LogUtils.Warning($"Safe display name failed while reading LabelCap: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }

        private static void WarnFallback(string defType, string defName, string rawLabel, string context, string fallback)
        {
            string key = $"{defType}:{defName ?? "null"}";
            if (!LoggedFallbackKeys.Add(key))
            {
                return;
            }

            LogUtils.Warning(
                $"Safe display name fallback used. Type={defType}, DefName={defName ?? "null"}, RawLabel={rawLabel ?? "null"}, Context={context ?? "unknown"}, Fallback={fallback}");
        }
    }
}
