using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.Validation
{
    public static class HediffWarningChecker
    {
        private const float CriticalThreshold = 0.1f;
        private const float WarningThreshold = 0.3f;

        public struct HediffWarning
        {
            public HediffDef Def;
            public WarningType Type;
            public string Message;
            public Severity SeverityLevel;
            public float MinSeverity;
            public float MaxSeverity;

            public bool IsCritical => SeverityLevel == Severity.Critical;
        }

        public enum WarningType
        {
            Consciousness,
            Moving,
            Manipulation
        }

        public enum Severity
        {
            Warning,
            Critical
        }

        public static List<HediffWarning> CheckHediffWarnings(List<ForcedHediff> hediffs)
        {
            if (hediffs == null || hediffs.Count == 0)
                return new List<HediffWarning>();

            var warnings = new List<HediffWarning>();
            foreach (var forcedHediff in hediffs)
            {
                if (forcedHediff?.HediffDef == null)
                    continue;

                FloatRange severityRange = forcedHediff.severityRange;
                if (severityRange == default)
                    severityRange = new FloatRange(0.5f, 1f);

                var hediffWarnings = CheckSingleHediffWithSeverity(forcedHediff.HediffDef, severityRange);
                warnings.AddRange(hediffWarnings);
            }

            return warnings;
        }

        public static List<HediffWarning> CheckSingleHediff(HediffDef def)
        {
            return CheckSingleHediffWithSeverity(def, new FloatRange(0f, 1f));
        }

        public static List<HediffWarning> CheckSingleHediffWithSeverity(HediffDef def, FloatRange severityRange)
        {
            var warnings = new List<HediffWarning>();
            if (def == null || def.stages == null || def.stages.Count == 0)
                return warnings;

            float minSeverity = Mathf.Clamp(severityRange.min, 0f, 1f);
            float maxSeverity = Mathf.Clamp(severityRange.max, 0f, 1f);

            var criticalStages = GetCriticalStagesInRange(def, minSeverity, maxSeverity);
            
            var warningByType = new Dictionary<WarningType, HediffWarning>();
            
            foreach (var stageInfo in criticalStages)
            {
                var warning = CreateWarningForStage(def, stageInfo, minSeverity, maxSeverity);
                if (warning.HasValue)
                {
                    var type = warning.Value.Type;
                    if (!warningByType.ContainsKey(type) || 
                        warning.Value.SeverityLevel == Severity.Critical ||
                        (warningByType[type].SeverityLevel != Severity.Critical && 
                         warning.Value.MinSeverity < warningByType[type].MinSeverity))
                    {
                        warningByType[type] = warning.Value;
                    }
                }
            }

            warnings.AddRange(warningByType.Values);
            return warnings;
        }

        public static bool HasCriticalWarnings(List<ForcedHediff> hediffs)
        {
            if (hediffs == null || hediffs.Count == 0)
                return false;

            foreach (var forcedHediff in hediffs)
            {
                if (forcedHediff?.HediffDef == null)
                    continue;

                FloatRange severityRange = forcedHediff.severityRange;
                if (severityRange == default)
                    severityRange = new FloatRange(0.5f, 1f);

                var warnings = CheckSingleHediffWithSeverity(forcedHediff.HediffDef, severityRange);
                if (warnings.Any(w => w.IsCritical))
                    return true;
            }

            return false;
        }

        private static List<StageCriticalInfo> GetCriticalStagesInRange(HediffDef def, float minSeverity, float maxSeverity)
        {
            var result = new List<StageCriticalInfo>();

            for (int i = 0; i < def.stages.Count; i++)
            {
                var stage = def.stages[i];
                if (stage?.capMods == null)
                    continue;

                float stageMin = stage.minSeverity;
                float stageMax = (i + 1 < def.stages.Count) ? def.stages[i + 1].minSeverity : 1.001f;

                bool overlapsMin = stageMin <= minSeverity && minSeverity < stageMax;
                bool overlapsMax = stageMin <= maxSeverity && maxSeverity < stageMax;
                bool containsRange = stageMin >= minSeverity && stageMax <= maxSeverity + 0.001f;
                bool withinRange = stageMin >= minSeverity && stageMin <= maxSeverity;

                if (overlapsMin || overlapsMax || containsRange || withinRange)
                {
                    foreach (var capMod in stage.capMods)
                    {
                        if (capMod?.capacity == null)
                            continue;

                        if (IsCriticalCapacity(capMod.capacity))
                        {
                            result.Add(new StageCriticalInfo
                            {
                                StageIndex = i,
                                Capacity = capMod.capacity,
                                Offset = capMod.offset,
                                SetMax = capMod.setMax,
                                StageMinSeverity = stageMin,
                                StageMaxSeverity = stageMax
                            });
                        }
                    }
                }
            }

            return result;
        }

        private static bool IsCriticalCapacity(PawnCapacityDef capacity)
        {
            return capacity == PawnCapacityDefOf.Consciousness ||
                   capacity == PawnCapacityDefOf.Moving ||
                   capacity == PawnCapacityDefOf.Manipulation;
        }

        private static HediffWarning? CreateWarningForStage(HediffDef def, StageCriticalInfo info, float rangeMin, float rangeMax)
        {
            float effectiveValue = info.Offset;
            if (info.SetMax.HasValue && info.SetMax.Value < effectiveValue)
            {
                effectiveValue = info.SetMax.Value;
            }

            if (effectiveValue >= WarningThreshold)
                return null;

            WarningType warningType;
            if (info.Capacity == PawnCapacityDefOf.Consciousness)
                warningType = WarningType.Consciousness;
            else if (info.Capacity == PawnCapacityDefOf.Moving)
                warningType = WarningType.Moving;
            else if (info.Capacity == PawnCapacityDefOf.Manipulation)
                warningType = WarningType.Manipulation;
            else
                return null;

            Severity severityLevel = effectiveValue <= CriticalThreshold ? Severity.Critical : Severity.Warning;
            string messageKey = GetWarningMessageKey(warningType, severityLevel);

            return new HediffWarning
            {
                Def = def,
                Type = warningType,
                Message = messageKey,
                SeverityLevel = severityLevel,
                MinSeverity = info.StageMinSeverity,
                MaxSeverity = info.StageMaxSeverity
            };
        }

        private static string GetWarningMessageKey(WarningType type, Severity severity)
        {
            if (severity == Severity.Critical)
            {
                switch (type)
                {
                    case WarningType.Consciousness: return "HediffWarning_ConsciousnessZero";
                    case WarningType.Moving: return "HediffWarning_MovingZero";
                    case WarningType.Manipulation: return "HediffWarning_ManipulationZero";
                }
            }
            else
            {
                switch (type)
                {
                    case WarningType.Consciousness: return "HediffWarning_ConsciousnessLow";
                    case WarningType.Moving: return "HediffWarning_MovingLow";
                    case WarningType.Manipulation: return "HediffWarning_ManipulationLow";
                }
            }
            return "";
        }

        private struct StageCriticalInfo
        {
            public int StageIndex;
            public PawnCapacityDef Capacity;
            public float Offset;
            public float? SetMax;
            public float StageMinSeverity;
            public float StageMaxSeverity;
        }
    }

    public static class HediffWarningUI
    {
        private static Texture2D _warningIcon;
        private static Texture2D WarningIcon
        {
            get
            {
                if (_warningIcon == null)
                {
                    _warningIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);
                }
                return _warningIcon;
            }
        }

        public static void DrawWarningIcon(Rect rect, HediffWarningChecker.HediffWarning warning)
        {
            Color iconColor = warning.SeverityLevel == HediffWarningChecker.Severity.Critical
                ? new Color(0.9f, 0.2f, 0.2f)
                : new Color(0.9f, 0.7f, 0.2f);

            GUI.color = iconColor;
            if (WarningIcon != null)
            {
                GUI.DrawTexture(rect, WarningIcon);
            }
            else
            {
                Widgets.DrawBoxSolid(rect, iconColor);
            }
            GUI.color = Color.white;

            string tooltip = GetWarningTooltip(warning);
            TooltipHandler.TipRegion(rect, tooltip);
        }

        public static void DrawWarningsRow(Rect rect, List<HediffWarningChecker.HediffWarning> warnings)
        {
            if (warnings == null || warnings.Count == 0)
                return;

            float iconSize = 20f;
            float spacing = 4f;
            float x = rect.x;

            foreach (var warning in warnings)
            {
                Rect iconRect = new Rect(x, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize);
                DrawWarningIcon(iconRect, warning);
                x += iconSize + spacing;
            }
        }

        private static string GetWarningTooltip(HediffWarningChecker.HediffWarning warning)
        {
            string baseText = LanguageManager.Get(warning.Message);
            string typeText;
            
            switch (warning.Type)
            {
                case HediffWarningChecker.WarningType.Consciousness:
                    typeText = LanguageManager.Get("HediffWarningType_Consciousness");
                    break;
                case HediffWarningChecker.WarningType.Moving:
                    typeText = LanguageManager.Get("HediffWarningType_Moving");
                    break;
                case HediffWarningChecker.WarningType.Manipulation:
                    typeText = LanguageManager.Get("HediffWarningType_Manipulation");
                    break;
                default:
                    typeText = "";
                    break;
            }

            string severityInfo = "";
            if (warning.MinSeverity > 0f || warning.MaxSeverity < 1f)
            {
                severityInfo = $"\n{LanguageManager.Get("SeverityRange")}: {warning.MinSeverity:P0} - {warning.MaxSeverity:P0}";
            }

            return $"{typeText}: {baseText}{severityInfo}";
        }
    }
}
