using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.Core;
using FactionGearCustomizer.Managers;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_KindXenotypeEditor : Window
    {
        private PawnKindDef kindDef;
        private KindGearData kindData;
        private string factionDefName;

        private Dictionary<string, float> bufferXenotypes = new Dictionary<string, float>();
        private bool bufferDisableXenotypeChances;
        private string bufferForcedXenotype;
        private string bufferMinAge;
        private string bufferMaxAge;

        private Dictionary<string, string> xenotypeTextBuffers = new Dictionary<string, string>();

        public override Vector2 InitialSize => new Vector2(600f, 700f);

        public Dialog_KindXenotypeEditor(PawnKindDef kind, KindGearData data, string factionDefName = null)
        {
            this.kindDef = kind;
            this.kindData = data;
            this.factionDefName = factionDefName;

            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            this.doCloseX = true;

            if (ModsConfig.BiotechActive)
            {
                bufferDisableXenotypeChances = data.DisableXenotypeChances;
                bufferForcedXenotype = data.ForcedXenotype;
                bufferMinAge = data.MinAge.HasValue ? data.MinAge.Value.ToString() : "";
                bufferMaxAge = data.MaxAge.HasValue ? data.MaxAge.Value.ToString() : "";

                bufferXenotypes = new Dictionary<string, float>();
                xenotypeTextBuffers = new Dictionary<string, string>();
                if (data.XenotypeChances != null && data.XenotypeChances.Count > 0)
                {
                    bufferXenotypes = new Dictionary<string, float>(data.XenotypeChances);
                    foreach (var kvp in data.XenotypeChances)
                        xenotypeTextBuffers[kvp.Key] = (kvp.Value * 100).ToString("F1");
                }
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            float listingWidth = inRect.width - 20f;
            Rect listingRect = new Rect(10f, 0f, listingWidth, inRect.height - 50f);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(listingRect);

            // Title
            Text.Font = GameFont.Medium;
            listing.Label(LanguageManager.Get("KindXenotypeSettings") + " - " + kindDef.LabelCap);
            Text.Font = GameFont.Small;
            listing.GapLine();
            listing.Gap(6f);

            // === Forced Xenotype ===
            listing.Label(LanguageManager.Get("ForcedXenotype"));
            Rect forcedRect = listing.GetRect(30f);

            if (string.IsNullOrEmpty(bufferForcedXenotype))
            {
                if (Widgets.ButtonText(forcedRect, LanguageManager.Get("SelectXenotype")))
                    OpenForcedXenotypeSelector();
            }
            else
            {
                float btnW = 80f;
                Rect infoRect = new Rect(forcedRect.x, forcedRect.y, forcedRect.width - btnW - 6f, forcedRect.height);
                Rect clearRect = new Rect(infoRect.xMax + 6f, forcedRect.y, btnW, forcedRect.height);

                XenotypeDef forcedDef = DefDatabase<XenotypeDef>.GetNamedSilentFail(bufferForcedXenotype);
                GUI.color = forcedDef != null ? Color.white : Color.yellow;
                if (Widgets.ButtonText(infoRect, forcedDef?.LabelCap ?? bufferForcedXenotype))
                    OpenForcedXenotypeSelector();
                GUI.color = Color.white;

                if (Widgets.ButtonText(clearRect, LanguageManager.Get("ClearForcedXenotype")))
                    bufferForcedXenotype = null;
            }

            listing.Gap(12f);

            // === Age Settings ===
            listing.Label(LanguageManager.Get("AgeSettings"));
            Rect ageRow = listing.GetRect(28f);
            float ageY = ageRow.y;

            Widgets.Label(new Rect(ageRow.x, ageY, 60f, 24f), LanguageManager.Get("MinAge"));
            string newMin = Widgets.TextField(new Rect(ageRow.x + 62f, ageY, 55f, 24f), bufferMinAge);
            bufferMinAge = new string(newMin.Where(c => char.IsDigit(c) || c == '.').ToArray());

            Widgets.Label(new Rect(ageRow.x + 130f, ageY, 60f, 24f), LanguageManager.Get("MaxAge"));
            string newMax = Widgets.TextField(new Rect(ageRow.x + 192f, ageY, 55f, 24f), bufferMaxAge);
            bufferMaxAge = new string(newMax.Where(c => char.IsDigit(c) || c == '.').ToArray());

            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            listing.Label(LanguageManager.Get("AgeSettingsHint"));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            listing.Gap(12f);

            // === Xenotype Chances ===
            bool newDisable = bufferDisableXenotypeChances;
            Widgets.CheckboxLabeled(listing.GetRect(24f), LanguageManager.Get("DisableXenotypeChances"), ref newDisable);
            if (newDisable != bufferDisableXenotypeChances)
                bufferDisableXenotypeChances = newDisable;

            listing.Gap(6f);

            if (!bufferDisableXenotypeChances)
            {
                listing.Label(LanguageManager.Get("XenotypeChances"));

                // Add xenotype button
                Rect addRect = listing.GetRect(28f);
                addRect.width = 160f;
                if (Widgets.ButtonText(addRect, "+ " + LanguageManager.Get("AddXenotype")))
                    OpenXenotypeSelectorForAdding();

                listing.Gap(4f);

                // Xenotype list
                var sortedKeys = bufferXenotypes.Keys
                    .Select(k => new { Key = k, Def = DefDatabase<XenotypeDef>.GetNamedSilentFail(k) })
                    .OrderBy(x => x.Def?.LabelCap.ToString() ?? x.Key)
                    .Select(x => x.Key)
                    .ToList();

                for (int i = 0; i < sortedKeys.Count; i++)
                {
                    string key = sortedKeys[i];
                    float val = bufferXenotypes[key];

                    if (!xenotypeTextBuffers.ContainsKey(key))
                        xenotypeTextBuffers[key] = (val * 100).ToString("F1");

                    float otherTotal = bufferXenotypes.Values.Sum() - val;
                    Rect rowRect = listing.GetRect(32f);

                    if (i % 2 == 1) Widgets.DrawAltRect(rowRect);

                    // Name
                    XenotypeDef xDef = DefDatabase<XenotypeDef>.GetNamedSilentFail(key);
                    Rect nameRect = new Rect(rowRect.x, rowRect.y, 130f, rowRect.height);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(nameRect, xDef?.LabelCap ?? key);
                    Text.Anchor = TextAnchor.UpperLeft;

                    // Slider
                    Rect sliderRect = new Rect(rowRect.x + 135f, rowRect.y + 6f, 200f, 20f);
                    float newVal = Widgets.HorizontalSlider(sliderRect, val, 0f, 1f, true, null);
                    float sliderPotentialTotal = otherTotal + newVal;
                    if (sliderPotentialTotal <= 1.0f)
                    {
                        bufferXenotypes[key] = newVal;
                        xenotypeTextBuffers[key] = (newVal * 100).ToString("F1");
                    }
                    else
                    {
                        float maxAllowed = Math.Max(0f, 1.0f - otherTotal);
                        bufferXenotypes[key] = maxAllowed;
                        xenotypeTextBuffers[key] = (maxAllowed * 100).ToString("F1");
                    }

                    // Percent label
                    Rect pctRect = new Rect(sliderRect.xMax + 6f, rowRect.y + 4f, 45f, 24f);
                    Widgets.Label(pctRect, $"{(bufferXenotypes[key] * 100):F0}%");

                    // Text input
                    Rect inputRect = new Rect(pctRect.xMax + 4f, rowRect.y + 4f, 55f, 24f);
                    string newText = Widgets.TextField(inputRect, xenotypeTextBuffers[key]);
                    if (newText != xenotypeTextBuffers[key])
                    {
                        xenotypeTextBuffers[key] = newText;
                        if (float.TryParse(newText, out float parsedValue))
                        {
                            float inputVal = parsedValue / 100f;
                            float potentialTotal = otherTotal + inputVal;
                            if (potentialTotal <= 1.0f)
                                bufferXenotypes[key] = inputVal;
                            else
                                bufferXenotypes[key] = Math.Max(0f, 1.0f - otherTotal);
                        }
                    }

                    // Remove
                    Rect removeRect = new Rect(inputRect.xMax + 6f, rowRect.y + 4f, 24f, 24f);
                    if (Widgets.ButtonText(removeRect, "✕"))
                    {
                        bufferXenotypes.Remove(key);
                        xenotypeTextBuffers.Remove(key);
                    }
                }

                listing.Gap(8f);

                // Total
                float currentTotal = bufferXenotypes.Values.Sum();
                string totalText = $"{LanguageManager.Get("Total")}: {currentTotal:P0}";
                if (currentTotal > 1.0f)
                {
                    GUI.color = Color.red;
                    totalText += " (" + ">100%" + ")";
                }
                else if (Math.Abs(currentTotal - 1.0f) < 0.01f)
                {
                    GUI.color = Color.green;
                }
                listing.Label(totalText);
                GUI.color = Color.white;
            }

            listing.End();

            // Bottom buttons
            float bottomY = inRect.height - 40f;
            float bottomBtnW = 120f;
            float startX = (inRect.width - (bottomBtnW * 2 + 20f)) / 2f;

            if (Widgets.ButtonText(new Rect(startX, bottomY, bottomBtnW, 32f), LanguageManager.Get("Apply")))
            {
                ApplyChanges();
                Close();
            }
            if (Widgets.ButtonText(new Rect(startX + bottomBtnW + 20f, bottomY, bottomBtnW, 32f), LanguageManager.Get("Cancel")))
            {
                Close();
            }
        }

        private void OpenForcedXenotypeSelector()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            var sortedDefs = DefDatabase<XenotypeDef>.AllDefs
                .OrderBy(x => x.LabelCap.ToString())
                .ToList();

            foreach (var x in sortedDefs)
            {
                XenotypeDef xDef = x;
                options.Add(new FloatMenuOption(xDef.LabelCap, () =>
                {
                    bufferForcedXenotype = xDef.defName;
                    CheckNonCombatantWarning(xDef);
                }, xDef.Icon, Color.white));
            }

            if (options.Any())
                Find.WindowStack.Add(new FloatMenu(options));
        }

        private void CheckNonCombatantWarning(XenotypeDef xDef)
        {
            if (xDef == null) return;
            if (FactionGearCustomizerMod.Settings.IsDialogDismissed("NonCombatantWarning")) return;

            if (!xDef.canGenerateAsCombatant || (xDef.genes != null && xDef.genes.Any(g => g.defName == "ViolenceDisabled")))
            {
                var dialog = new Dialog_MessageBox(
                    LanguageManager.Get("NonCombatantWarningText"),
                    LanguageManager.Get("Yes"), null,
                    LanguageManager.Get("No"), null,
                    LanguageManager.Get("DoNotShowAgain"), false,
                    () => FactionGearCustomizerMod.Settings.DismissDialog("NonCombatantWarning")
                );
                Find.WindowStack.Add(dialog);
            }
        }

        private void OpenXenotypeSelectorForAdding()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            var sortedDefs = DefDatabase<XenotypeDef>.AllDefs
                .OrderBy(x => x.LabelCap.ToString())
                .ToList();

            var existingKeys = new HashSet<string>(bufferXenotypes.Keys);

            foreach (var x in sortedDefs)
            {
                XenotypeDef xDef = x;
                if (existingKeys.Contains(xDef.defName)) continue;

                options.Add(new FloatMenuOption(xDef.LabelCap, () =>
                {
                    bufferXenotypes[xDef.defName] = 0f;
                    xenotypeTextBuffers[xDef.defName] = "0.0";
                }, xDef.Icon, Color.white));
            }

            if (options.Any())
                Find.WindowStack.Add(new FloatMenu(options));
            else
                Messages.Message(LanguageManager.Get("AllXenotypesAdded"), MessageTypeDefOf.RejectInput, false);
        }

        private void ApplyChanges()
        {
            if (bufferXenotypes.Count > 0)
            {
                float totalChance = bufferXenotypes.Values.Sum();
                if (totalChance > 1.0f)
                {
                    Messages.Message(LanguageManager.Get("XenotypeChancesExceed100"), MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }

            kindData.DisableXenotypeChances = bufferDisableXenotypeChances;
            kindData.ForcedXenotype = bufferForcedXenotype;

            if (float.TryParse(bufferMinAge, out float minAge))
            {
                float minAdultAge = GetMinAdultAge(kindDef);
                if (minAge < minAdultAge)
                {
                    minAge = minAdultAge;
                    bufferMinAge = minAdultAge.ToString();
                }
                kindData.MinAge = minAge;
            }
            else
                kindData.MinAge = null;

            if (float.TryParse(bufferMaxAge, out float maxAge))
            {
                float minAdultAge = GetMinAdultAge(kindDef);
                if (maxAge < minAdultAge)
                {
                    maxAge = minAdultAge;
                    bufferMaxAge = minAdultAge.ToString();
                }
                kindData.MaxAge = maxAge;
            }
            else
                kindData.MaxAge = null;

            if (kindData.MinAge.HasValue && kindData.MaxAge.HasValue && kindData.MaxAge.Value < kindData.MinAge.Value)
            {
                kindData.MaxAge = kindData.MinAge;
                bufferMaxAge = kindData.MaxAge.Value.ToString();
            }

            if (bufferXenotypes.Count > 0)
                kindData.XenotypeChances = new Dictionary<string, float>(bufferXenotypes);
            else
                kindData.XenotypeChances?.Clear();

            kindData.isModified = true;
            FactionDefManager.ApplyKindChanges(kindDef, kindData);
            SyncToGameComponent();
            FactionGearCustomizerMod.Settings.Write();
            Messages.Message(LanguageManager.Get("SettingsSaved"), MessageTypeDefOf.PositiveEvent, false);
        }

        private void SyncToGameComponent()
        {
            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent == null) return;

            if (gameComponent.savedFactionGearData == null)
                gameComponent.savedFactionGearData = new List<FactionGearData>();

            FactionGearData savedFactionData = null;
            if (!string.IsNullOrEmpty(factionDefName))
                savedFactionData = gameComponent.savedFactionGearData
                    .FirstOrDefault(f => f.factionDefName == factionDefName);

            if (savedFactionData == null)
                savedFactionData = gameComponent.savedFactionGearData
                    .FirstOrDefault(f => f.kindGearData != null && f.kindGearData.Any(k => k.kindDefName == kindDef.defName));

            if (savedFactionData == null && !string.IsNullOrEmpty(factionDefName))
            {
                savedFactionData = new FactionGearData(factionDefName) { isModified = true };
                gameComponent.savedFactionGearData.Add(savedFactionData);
            }

            if (savedFactionData == null) return;

            var savedKindData = savedFactionData.GetOrCreateKindData(kindDef.defName);
            savedKindData.DisableXenotypeChances = kindData.DisableXenotypeChances;
            savedKindData.ForcedXenotype = kindData.ForcedXenotype;
            savedKindData.MinAge = kindData.MinAge;
            savedKindData.MaxAge = kindData.MaxAge;
            if (kindData.XenotypeChances != null && kindData.XenotypeChances.Count > 0)
                savedKindData.XenotypeChances = new Dictionary<string, float>(kindData.XenotypeChances);
            else
                savedKindData.XenotypeChances?.Clear();
            savedKindData.isModified = true;
            savedFactionData.isModified = true;
        }

        private static float GetMinAdultAge(PawnKindDef kindDef)
        {
            if (kindDef?.race?.race?.lifeStageAges == null) return 18f;

            foreach (var lsa in kindDef.race.race.lifeStageAges)
            {
                if (lsa.def?.defName == "HumanlikeAdult" || lsa.def?.defName == "Adult")
                    return lsa.minAge;
            }

            float maxMinAge = 0f;
            foreach (var lsa in kindDef.race.race.lifeStageAges)
            {
                if (lsa.minAge > maxMinAge)
                    maxMinAge = lsa.minAge;
            }

            return maxMinAge > 0f ? maxMinAge : 18f;
        }
    }
}
