using RimWorld;
using UnityEngine;
using Verse;
using FactionGearModification.UI;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_RaidConfig : Window
    {
        private float raidPoints;
        private Faction faction;
        private const float MinPoints = 0f;
        private const float MaxPoints = 10000f;
        private const float SnapInterval = 100f;
        private static int sliderId = 156323;

        public override Vector2 InitialSize => new Vector2(420f, 220f);

        public Dialog_RaidConfig(Faction faction = null)
        {
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.draggable = true;
            this.resizeable = false;

            this.faction = faction;
            raidPoints = Mathf.Clamp(FactionGearCustomizerMod.Settings.cachedRaidPoints, MinPoints, MaxPoints);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Widgets.Label(titleRect, LanguageManager.Get("RaidConfigTitle"));
            Text.Font = GameFont.Small;

            float sliderY = titleRect.yMax + 15f;
            Rect sliderRect = new Rect(inRect.x, sliderY, inRect.width, 28f);

            bool wasTextFocused = GUI.GetNameOfFocusedControl() == $"SliderInput_{sliderId}";
            float newValue = WidgetsUtils.HorizontalSliderWithInput(sliderRect, raidPoints, MinPoints, MaxPoints, sliderId, "F0");

            if (Mathf.Abs(newValue - raidPoints) > 0.001f)
            {
                if (!wasTextFocused)
                    raidPoints = Mathf.Round(newValue / SnapInterval) * SnapInterval;
                else
                    raidPoints = Mathf.Clamp(newValue, MinPoints, MaxPoints);
            }

            Rect valueRect = new Rect(inRect.x, sliderRect.yMax + 5f, inRect.width, 18f);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(valueRect, $"{LanguageManager.Get("RaidPointsLabel")}: {raidPoints:F0}");

            bool isHostile = faction != null && faction.HostileTo(Faction.OfPlayer);
            Rect hintRect = new Rect(inRect.x, valueRect.yMax + 2f, inRect.width, 16f);
            Widgets.Label(hintRect, LanguageManager.Get(isHostile ? "RaidHintHostile" : "RaidHintFriendly"));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            float buttonWidth = 120f;
            float buttonY = inRect.yMax - 40f;

            Rect cancelRect = new Rect(inRect.x, buttonY, buttonWidth, 30f);
            if (Widgets.ButtonText(cancelRect, LanguageManager.Get("Cancel")))
            {
                this.Close();
            }

            Rect confirmRect = new Rect(inRect.xMax - buttonWidth, buttonY, buttonWidth, 30f);
            Color prevColor = GUI.color;
            GUI.color = isHostile ? Color.red : new Color(0.3f, 0.7f, 0.4f);
            if (Widgets.ButtonText(confirmRect, LanguageManager.Get(isHostile ? "RaidConfirm" : "AidConfirm")))
            {
                OnConfirm();
            }
            GUI.color = prevColor;
        }

        private void OnConfirm()
        {
            float capturedPoints = Mathf.Clamp(raidPoints, MinPoints, MaxPoints);

            FactionGearCustomizerMod.Settings.cachedRaidPoints = capturedPoints;
            FactionGearCustomizerMod.Settings.Write();

            this.Close();

            StartRaidTargeting(capturedPoints, faction);
        }

        private static void StartRaidTargeting(float points, Faction faction)
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message(LanguageManager.Get("RaidNoMap"), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Messages.Message(LanguageManager.Get("RaidTargetingHint"), MessageTypeDefOf.NeutralEvent, false);

            Faction capturedFaction = faction;
            Find.Targeter.BeginTargeting(
                new TargetingParameters
                {
                    canTargetLocations = true,
                    canTargetPawns = false,
                    canTargetBuildings = false,
                    canTargetItems = false
                },
                delegate (LocalTargetInfo target)
                {
                    TriggerRaidAt(target.Cell, points, capturedFaction);
                });
        }

        private static void TriggerRaidAt(IntVec3 cell, float points, Faction faction)
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            bool isHostile = faction != null && faction.HostileTo(Faction.OfPlayer);
            IncidentDef incidentDef = isHostile ? IncidentDefOf.RaidEnemy : IncidentDefOf.RaidFriendly;

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            parms.forced = true;
            parms.faction = faction;
            parms.points = points;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.spawnCenter = cell;

            Log.Message($"[FactionGearCustomizer] Triggering {(isHostile ? "raid" : "support")} at {cell} with faction='{faction?.Name ?? "null"}' ({faction?.def?.defName ?? "null"}), points={points}");

            RaidFactionHelper.ForcedFaction = faction;
            bool success = incidentDef.Worker.TryExecute(parms);
            RaidFactionHelper.ForcedFaction = null;
            if (success)
            {
                Messages.Message(
                    string.Format(LanguageManager.Get("RaidTriggered"), points),
                    MessageTypeDefOf.ThreatBig, false);
            }
            else
            {
                Messages.Message(LanguageManager.Get("RaidFailed"), MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
