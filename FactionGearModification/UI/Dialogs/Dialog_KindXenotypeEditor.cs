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
        
        private Dictionary<string, float> bufferXenotypes = new Dictionary<string, float>();
        private bool bufferDisableXenotypeChances;
        private string bufferForcedXenotype;
        
        private Vector2 scrollPosition;
        
        public override Vector2 InitialSize => new Vector2(600f, 700f);

        public Dialog_KindXenotypeEditor(PawnKindDef kind, KindGearData data)
        {
            this.kindDef = kind;
            this.kindData = data;
            
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            this.doCloseX = true;
            
            // Initialize buffers from data
            if (ModsConfig.BiotechActive)
            {
                bufferDisableXenotypeChances = data.DisableXenotypeChances;
                bufferForcedXenotype = data.ForcedXenotype;
                
                if (data.XenotypeChances != null && data.XenotypeChances.Count > 0)
                {
                    bufferXenotypes = new Dictionary<string, float>(data.XenotypeChances);
                }
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 40f), 
                LanguageManager.Get("KindXenotypeSettings") + " - " + kindDef.LabelCap);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            
            float curY = 45f;
            
            // Info section
            Rect infoRect = new Rect(inRect.x, curY, inRect.width, 50f);
            Widgets.DrawMenuSection(infoRect);
            Rect infoInner = infoRect.ContractedBy(10f);
            Widgets.Label(infoInner, kindDef.defName);
            curY += 60f;
            
            // Forced Xenotype Section
            if (ModsConfig.BiotechActive)
            {
                Rect forcedRect = new Rect(inRect.x, curY, inRect.width, 80f);
                Widgets.DrawMenuSection(forcedRect);
                Rect forcedInner = forcedRect.ContractedBy(10f);
                
                Widgets.Label(forcedInner, $"<b>{LanguageManager.Get("ForcedXenotype")}</b>");
                TooltipHandler.TipRegion(forcedInner, LanguageManager.Get("ForcedXenotypeTooltip"));
                
                curY += 35f;
                
                // Forced Xenotype Selector
                Rect selectorRect = new Rect(inRect.x + 10f, curY, inRect.width - 20f, 30f);
                
                if (string.IsNullOrEmpty(bufferForcedXenotype))
                {
                    if (Widgets.ButtonText(selectorRect, LanguageManager.Get("SelectXenotype")))
                    {
                        OpenXenotypeSelector();
                    }
                }
                else
                {
                    XenotypeDef forcedDef = DefDatabase<XenotypeDef>.GetNamedSilentFail(bufferForcedXenotype);
                    string label = forcedDef?.LabelCap ?? bufferForcedXenotype;
                    
                    if (Widgets.ButtonText(selectorRect, label))
                    {
                        OpenXenotypeSelector();
                    }
                    
                    // Clear button
                    Rect clearBtnRect = new Rect(selectorRect.xMax - 80f, selectorRect.y, 70f, 30f);
                    if (Widgets.ButtonText(clearBtnRect, LanguageManager.Get("ClearForcedXenotype")))
                    {
                        bufferForcedXenotype = null;
                    }
                }
                
                curY += 40f;
                
                // Disable Xenotype Chances Toggle
                Rect toggleRect = new Rect(inRect.x + 10f, curY, inRect.width - 20f, 30f);
                bool newDisable = bufferDisableXenotypeChances;
                Widgets.CheckboxLabeled(toggleRect, LanguageManager.Get("DisableXenotypeChances"), ref newDisable);
                if (newDisable != bufferDisableXenotypeChances)
                {
                    bufferDisableXenotypeChances = newDisable;
                }
                curY += 35f;
                
                if (!bufferDisableXenotypeChances)
                {
                    // Add Xenotype Button
                    Rect addBtnRect = new Rect(inRect.x + 10f, curY, 150f, 30f);
                    if (Widgets.ButtonText(addBtnRect, LanguageManager.Get("AddXenotype")))
                    {
                        OpenXenotypeSelector();
                    }
                    curY += 35f;
                    
                    // Xenotype List
                    Rect scrollViewRect = new Rect(inRect.x, curY, inRect.width, inRect.height - curY - 50f);
                    Widgets.DrawMenuSection(scrollViewRect);
                    Rect scrollViewInner = scrollViewRect.ContractedBy(5f);
                    
                    float viewWidth = scrollViewInner.width - 16f;
                    float viewHeight = bufferXenotypes.Count * 30f + 20f;
                    Rect viewRect = new Rect(0, 0, viewWidth, Math.Max(viewHeight, scrollViewInner.height));
                    
                    Widgets.BeginScrollView(scrollViewInner, ref scrollPosition, viewRect);
                    
                    float curViewY = 0f;
                    
                    // Calculate current total for display
                    float currentTotal = bufferXenotypes.Values.Sum();
                    string totalText = $"{LanguageManager.Get("Total")}: {currentTotal:P0}";
                    GUI.color = currentTotal > 1.0f ? Color.red : Color.green;
                    Widgets.Label(new Rect(0, curViewY, viewWidth, 24f), totalText);
                    GUI.color = Color.white;
                    curViewY += 25f;
                    
                    var sortedKeys = bufferXenotypes.Keys
                        .Select(k => new { Key = k, Def = DefDatabase<XenotypeDef>.GetNamedSilentFail(k) })
                        .OrderBy(x => x.Def?.LabelCap.ToString() ?? x.Key)
                        .Select(x => x.Key)
                        .ToList();
                    
                    foreach (var key in sortedKeys)
                    {
                        Rect row = new Rect(0, curViewY, viewWidth, 30f);
                        if (curViewY % 60f < 30f) Widgets.DrawAltRect(row);
                        
                        XenotypeDef xDef = DefDatabase<XenotypeDef>.GetNamedSilentFail(key);
                        string label = xDef?.LabelCap ?? key;
                        
                        // Icon
                        if (xDef != null)
                        {
                            Rect xIconRect = new Rect(row.x, row.y, 24f, 24f);
                            Widgets.DrawTextureFitted(xIconRect, xDef.Icon, 1f);
                        }
                        
                        Widgets.Label(new Rect(row.x + 30f, row.y + 3f, 150f, 24f), label);
                        
                        float val = bufferXenotypes[key];
                        float newVal = Widgets.HorizontalSlider(new Rect(row.x + 190f, row.y + 5f, 200f, 20f), val, 0f, 1f, true, val.ToString("P0"));
                        
                        // Calculate what the total would be with the new value
                        float otherTotal = currentTotal - val;
                        float potentialTotal = otherTotal + newVal;
                        
                        // Only apply the new value if it doesn't exceed 100%
                        if (potentialTotal <= 1.0f)
                        {
                            bufferXenotypes[key] = newVal;
                        }
                        else
                        {
                            // Limit to remaining available percentage
                            float maxAllowed = Math.Max(0f, 1.0f - otherTotal);
                            bufferXenotypes[key] = maxAllowed;
                        }
                        
                        if (Widgets.ButtonText(new Rect(row.x + 400f, row.y, 30f, 24f), "X"))
                        {
                            bufferXenotypes.Remove(key);
                        }
                        
                        curViewY += 30f;
                    }
                    
                    Widgets.EndScrollView();
                }
                else
                {
                    Rect disabledRect = new Rect(inRect.x + 10f, curY, inRect.width - 20f, 30f);
                    Widgets.Label(disabledRect, LanguageManager.Get("Disabled"));
                    curY += 35f;
                }
            }
            
            // Bottom Buttons
            float btnWidth = 120f;
            float btnHeight = 40f;
            float btnY = inRect.height - btnHeight;
            float btnX = inRect.width - btnWidth;
            
            if (Widgets.ButtonText(new Rect(btnX, btnY, btnWidth, btnHeight), LanguageManager.Get("Apply")))
            {
                ApplyChanges();
                Close();
            }
            
            btnX -= (btnWidth + 10f);
            if (Widgets.ButtonText(new Rect(btnX, btnY, btnWidth, btnHeight), LanguageManager.Get("Cancel")))
            {
                Close();
            }
        }
        
        private void OpenXenotypeSelector()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            var sortedDefs = DefDatabase<XenotypeDef>.AllDefs
                .OrderBy(x => x.LabelCap.ToString())
                .ToList();
            
            foreach (var x in sortedDefs)
            {
                XenotypeDef xDef = x;
                string label = xDef.LabelCap;
                
                // For forced xenotype selector
                options.Add(new FloatMenuOption(label, () => {
                    bufferForcedXenotype = xDef.defName;
                }));
            }
            
            if (options.Any())
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
            else
            {
                Messages.Message(LanguageManager.Get("AllXenotypesAdded"), MessageTypeDefOf.RejectInput, false);
            }
        }
        
        private void ApplyChanges()
        {
            // Validate Xenotype Chances - sum must not exceed 100%
            if (bufferXenotypes.Count > 0)
            {
                float totalChance = bufferXenotypes.Values.Sum();
                if (totalChance > 1.0f)
                {
                    Messages.Message(LanguageManager.Get("XenotypeChancesExceed100"), MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }
            
            // Apply to kind data
            kindData.DisableXenotypeChances = bufferDisableXenotypeChances;
            kindData.ForcedXenotype = bufferForcedXenotype;
            
            if (bufferXenotypes.Count > 0)
            {
                kindData.XenotypeChances = new Dictionary<string, float>(bufferXenotypes);
            }
            else
            {
                kindData.XenotypeChances?.Clear();
            }
            
            kindData.isModified = true;
            
            // Apply changes to game
            FactionDefManager.ApplyKindChanges(kindDef, kindData);
            
            FactionGearCustomizerMod.Settings.Write();
            Messages.Message(LanguageManager.Get("SettingsSaved"), MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
