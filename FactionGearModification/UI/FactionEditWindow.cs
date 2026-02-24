using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.Core;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer.UI.Panels;
using FactionGearCustomizer.UI.Dialogs;

namespace FactionGearCustomizer.UI
{
    public enum NameValidationResult
    {
        Valid,
        Empty,
        TooLong,
        InvalidChars
    }
    public class FactionEditWindow : Window
    {
        private FactionDef factionDef;
        private PawnKindDef kindDef;
        private FactionGearData factionData;
        private KindGearData kindData;

        private string bufferLabel;
        private string bufferDescription;
        private string bufferIconPath;
        private Color bufferColor;
        private string bufferKindLabel;
        
        // Biotech Buffers
        private Dictionary<string, float> bufferXenotypes = new Dictionary<string, float>();
        private Vector2 xenotypeScrollPosition;
        private Vector2 factionSettingsScrollPosition;

        private Vector2 mainScrollPos;

        // Group Buffers
        private List<PawnGroupMakerData> bufferGroups;

        // Player Relation Buffer
        private FactionRelationKind? bufferPlayerRelation;

        private string pawnKindSearchQuery = "";

        public override Vector2 InitialSize => new Vector2(700f, 800f);

        public FactionEditWindow(FactionDef faction, PawnKindDef kind = null)
        {
            this.factionDef = faction ?? throw new ArgumentNullException(nameof(faction));
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;

            factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(faction.defName);
            
            // Initialize buffers
            bufferLabel = !string.IsNullOrEmpty(factionData.Label) ? factionData.Label : GetInGameFactionName();
            bufferDescription = !string.IsNullOrEmpty(factionData.Description) ? factionData.Description : (faction.description ?? "");
            bufferIconPath = !string.IsNullOrEmpty(factionData.IconPath) ? factionData.IconPath : (faction.factionIconPath ?? "");
            
            // For color, if data has color, use it. If not, use faction default color (or first in spectrum).
            if (factionData.Color.HasValue)
            {
                bufferColor = factionData.Color.Value;
            }
            else
            {
                bufferColor = faction.colorSpectrum != null && faction.colorSpectrum.Any() 
                    ? faction.colorSpectrum.First() 
                    : Color.white;
            }
            
            // Initialize Biotech Buffers
            if (ModsConfig.BiotechActive && faction.humanlikeFaction)
            {
                if (factionData.XenotypeChances != null && factionData.XenotypeChances.Count > 0)
                {
                    bufferXenotypes = new Dictionary<string, float>(factionData.XenotypeChances);
                }
                else if (faction.xenotypeSet != null)
                {
                    var chances = FactionDefManager.GetXenotypeChances(faction.xenotypeSet);
                    if (chances != null)
                    {
                        foreach (var chance in chances)
                        {
                            if (chance.xenotype != null && !bufferXenotypes.ContainsKey(chance.xenotype.defName))
                            {
                                bufferXenotypes.Add(chance.xenotype.defName, chance.chance);
                            }
                        }
                    }
                }
            }

            // Initialize Group Buffers
            if (factionData.groupMakers != null && factionData.groupMakers.Count > 0)
            {
                bufferGroups = new List<PawnGroupMakerData>();
                foreach (var g in factionData.groupMakers)
                {
                    bufferGroups.Add(g.DeepCopy());
                }
            }
            else
            {
                // Load from FactionDef
                bufferGroups = new List<PawnGroupMakerData>();
                if (faction.pawnGroupMakers != null)
                {
                    foreach (var maker in faction.pawnGroupMakers)
                    {
                        bufferGroups.Add(new PawnGroupMakerData(maker));
                    }
                }
            }

            // Initialize Player Relation Buffer
            bufferPlayerRelation = factionData.PlayerRelationOverride;
        }

        private Dictionary<string, string> kindLabelBuffers = new Dictionary<string, string>();

        private string GetInGameFactionName()
        {
            if (Current.Game == null || Find.FactionManager == null)
            {
                return factionDef.label;
            }
            var factionInstance = Find.FactionManager.FirstFactionOfDef(factionDef);
            return factionInstance != null ? factionInstance.Name : factionDef.label;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float width = inRect.width;
            
            // Content Area
            float bottomMargin = 50f;
            Rect contentRect = new Rect(inRect.x, inRect.y, width, inRect.height - bottomMargin);
            Widgets.DrawMenuSection(contentRect);
            Rect innerRect = contentRect.ContractedBy(10f);

            // Calculate heights
            float viewWidth = innerRect.width - 16f;
            float settingsHeight = CalculateSettingsHeight();
            float kindListHeight = CalculateKindListHeight();
            float groupListHeight = GroupListPanel.GetViewHeight(bufferGroups);
            
            float totalHeight = settingsHeight + kindListHeight + groupListHeight + 40f; // + padding
            
            Rect viewRect = new Rect(0, 0, viewWidth, totalHeight);
            
            Widgets.BeginScrollView(innerRect, ref mainScrollPos, viewRect);
            
            float curY = 0f;
            
            // Settings
            DrawFactionSettings(new Rect(0, curY, viewWidth, settingsHeight));
            curY += settingsHeight + 10f;
            
            // Kind List
            Widgets.DrawLineHorizontal(0, curY, viewWidth);
            curY += 10f;
            DrawKindList(new Rect(0, curY, viewWidth, kindListHeight));
            curY += kindListHeight + 10f;
            
            // Group List
            Widgets.DrawLineHorizontal(0, curY, viewWidth);
            curY += 10f;
            GroupListPanel.Draw(new Rect(0, curY, viewWidth, groupListHeight), ref bufferGroups, factionDef);
            
            Widgets.EndScrollView();

            // Bottom Buttons
            float btnWidth = 120f;
            float btnHeight = 40f;
            float btnY = inRect.height - btnHeight;
            float btnX = inRect.width - btnWidth;

            // Game Check
            bool inGame = Current.Game != null;
            if (!inGame)
            {
                GUI.color = Color.gray;
            }

            if (Widgets.ButtonText(new Rect(btnX, btnY, btnWidth, btnHeight), LanguageManager.Get("Apply"), true, true, inGame))
            {
                if (inGame)
                {
                    ApplyChanges();
                    Close();
                }
            }
            if (!inGame)
            {
                TooltipHandler.TipRegion(new Rect(btnX, btnY, btnWidth, btnHeight), LanguageManager.Get("OnlyAvailableInGame"));
                GUI.color = Color.white;
            }

            btnX -= (btnWidth + 10f);
            if (Widgets.ButtonText(new Rect(btnX, btnY, btnWidth, btnHeight), LanguageManager.Get("Reset")))
            {
                ResetChanges();
                Close();
            }

            btnX -= (btnWidth + 10f);
            if (Widgets.ButtonText(new Rect(btnX, btnY, btnWidth, btnHeight), LanguageManager.Get("Cancel")))
            {
                Close();
            }
        }

        private float CalculateSettingsHeight()
        {
            float height = 450f; // Base static height
            
            if (ModsConfig.BiotechActive && factionDef.humanlikeFaction) 
            {
                height += 60f; // Header
                if (bufferXenotypes.Count > 0)
                {
                    height += bufferXenotypes.Count * 30f + 20f;
                }
            }
            
            // World Operations
            bool inGame = Current.Game != null && Find.FactionManager != null;
            if (inGame) height += 100f;
            else height += 40f;

            // Player Relation Settings
            if (inGame) height += 80f;

            // Delete Faction Section (Danger Zone) - 减少留白
            if (inGame) height += 45f;
            
            return height;
        }

        private float CalculateKindListHeight()
        {
            List<PawnKindDef> kinds = GetFilteredKinds();
            float headerHeight = 70f; // Header (30) + Search (30) + Spacing (10)
            float rowHeight = 40f;
            return headerHeight + kinds.Count * rowHeight;
        }

        private List<PawnKindDef> GetFilteredKinds()
        {
            // 使用原始兵种列表，不包含用户在群组中新增的兵种
            List<PawnKindDef> kinds = FactionDefManager.GetOriginalFactionKinds(factionDef);
            if (!string.IsNullOrEmpty(pawnKindSearchQuery))
            {
                kinds = kinds.Where(k => k.LabelCap.ToString().ToLower().Contains(pawnKindSearchQuery.ToLower()) || 
                                         k.defName.ToLower().Contains(pawnKindSearchQuery.ToLower())).ToList();
            }
            return kinds;
        }

        private void DrawFactionSettings(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            // Faction Label
            listing.Label($"<b>{LanguageManager.Get("Factions")}: {factionDef.LabelCap}</b>");
            listing.Label($"{LanguageManager.Get("DefName")}: {factionDef.defName}");
            listing.Gap();

            // Icon and Name Row
            Rect headerRect = listing.GetRect(64f);
            
            // Icon (Left)
            Rect iconDrawRect = new Rect(headerRect.x, headerRect.y, 64f, 64f);
            
            Texture2D currentIcon = null;
            if (!string.IsNullOrEmpty(bufferIconPath) && bufferIconPath.StartsWith("Custom:"))
            {
                currentIcon = CustomIconManager.GetIcon(bufferIconPath.Substring(7));
            }
            else
            {
                currentIcon = ContentFinder<Texture2D>.Get(bufferIconPath, false);
            }
            
            if (currentIcon == null) currentIcon = factionDef.FactionIcon;
            if (currentIcon != null)
            {
                GUI.DrawTexture(iconDrawRect, currentIcon);
            }
            Widgets.DrawBox(iconDrawRect);

            // Name (Right)
            // Label
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = new Rect(headerRect.x + 74f, headerRect.y, headerRect.width - 74f, 24f);
            Widgets.Label(labelRect, LanguageManager.Get("FactionName"));
            Text.Anchor = TextAnchor.UpperLeft;

            // Text Entry
            Rect nameRect = new Rect(headerRect.x + 74f, headerRect.y + 28f, headerRect.width - 74f, 30f);
            string newBufferLabel = Widgets.TextField(nameRect, bufferLabel);
            if (newBufferLabel != bufferLabel)
            {
                string sanitized = InputValidator.SanitizeName(newBufferLabel);
                if (InputValidator.IsValidName(sanitized, out string error))
                {
                    bufferLabel = sanitized;
                }
                else
                {
                    bufferLabel = sanitized;
                }
            }

            listing.Gap();

            // Description
            listing.Label(LanguageManager.Get("FactionDescription"));
            bufferDescription = listing.TextEntry(bufferDescription, 4); // 4 lines
            listing.Gap();

            // Icon Path
            listing.Label(LanguageManager.Get("FactionIconPath"));
            Rect iconPathRect = listing.GetRect(30f);
            bufferIconPath = Widgets.TextField(new Rect(iconPathRect.x, iconPathRect.y, iconPathRect.width - 80f, 30f), bufferIconPath);
            if (Widgets.ButtonText(new Rect(iconPathRect.width - 75f, iconPathRect.y, 75f, 30f), LanguageManager.Get("Browse")))
            {
                Find.WindowStack.Add(new Dialog_TextureBrowser((path) => bufferIconPath = path));
            }
            listing.Gap();

            // Color
            listing.Label(LanguageManager.Get("FactionColor"));
            Rect colorRect = listing.GetRect(30f);
            Rect colorBox = new Rect(colorRect.x, colorRect.y, 100f, 30f);
            Widgets.DrawBoxSolid(colorBox, bufferColor);
            if (Widgets.ButtonInvisible(colorBox))
            {
                Find.WindowStack.Add(new Window_ColorPicker(bufferColor, (c) => bufferColor = c));
            }
            listing.Gap();

            // Biotech Xenotypes
            if (ModsConfig.BiotechActive && factionDef.humanlikeFaction)
            {
                listing.GapLine();
                listing.Label($"<b>{LanguageManager.Get("XenotypeChances")}</b>");
                
                Rect addBtnRect = listing.GetRect(30f);
                if (Widgets.ButtonText(new Rect(addBtnRect.width - 150f, addBtnRect.y, 150f, 30f), LanguageManager.Get("AddXenotype")))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    var sortedDefs = DefDatabase<XenotypeDef>.AllDefs
                        .OrderBy(x => x.LabelCap.ToString())
                        .ToList();

                    foreach (var x in sortedDefs)
                    {
                        if (!bufferXenotypes.ContainsKey(x.defName))
                        {
                            options.Add(new FloatMenuOption(x.LabelCap, () => {
                                bufferXenotypes.Add(x.defName, 0.1f);
                            }));
                        }
                    }
                    if (options.Any()) Find.WindowStack.Add(new FloatMenu(options));
                    else Messages.Message(LanguageManager.Get("AllXenotypesAdded"), MessageTypeDefOf.RejectInput, false);
                }
                
                // List of Xenotypes
                var sortedKeys = bufferXenotypes.Keys
                    .Select(k => new { Key = k, Def = DefDatabase<XenotypeDef>.GetNamedSilentFail(k) })
                    .OrderBy(x => x.Def?.LabelCap.ToString() ?? x.Key)
                    .Select(x => x.Key)
                    .ToList();

                // Calculate current total for display
                float currentTotal = bufferXenotypes.Values.Sum();
                string totalText = $"{LanguageManager.Get("Total")}: {currentTotal:P0}";
                GUI.color = currentTotal > 1.0f ? Color.red : Color.green;
                listing.Label(totalText);
                GUI.color = Color.white;
                listing.Gap(5f);

                foreach (var key in sortedKeys)
                {
                    Rect row = listing.GetRect(30f);
                    if (row.y % 60f < 30f) Widgets.DrawAltRect(row);
        
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
                }
            }

            // World Operations
            listing.GapLine();
            listing.Label($"<b>{LanguageManager.Get("WorldOperations")}</b>");

            bool inGame = Current.Game != null && Find.FactionManager != null;
            if (!inGame)
            {
                GUI.color = Color.gray;
                listing.Label(LanguageManager.Get("OnlyAvailableInGame"));
                GUI.color = Color.white;
            }
            else
            {
                var instances = Find.FactionManager.AllFactions.Where(f => f.def == factionDef && !f.IsPlayer).ToList();
                listing.Label($"{LanguageManager.Get("InstanceCount")}: {instances.Count}");

                if (listing.ButtonText(LanguageManager.Get("SpawnSettlements")))
                {
                    Find.WindowStack.Add(new Dialog_SelectFactionInstanceForSettlement(factionDef));
                }
            }

            // Player Relation Settings
            if (inGame)
            {
                listing.GapLine();
                listing.Label($"<b>{LanguageManager.Get("PlayerRelationSettings")}</b>");
                DrawPlayerRelationSelector(listing);
            }

            // Delete Faction
            if (inGame)
            {
                listing.GapLine();
                listing.Label($"<b>{LanguageManager.Get("DangerZone")}</b>");

                var instances = Find.FactionManager.AllFactions.Where(f => f.def == factionDef && !f.IsPlayer).ToList();
                if (instances.Count > 0)
                {
                    GUI.color = Color.red;
                    if (listing.ButtonText(LanguageManager.Get("DeleteFaction")))
                    {
                        ShowDeleteFactionDialog(instances);
                    }
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = Color.gray;
                    listing.Label(LanguageManager.Get("NoFactionInstanceToDelete"));
                    GUI.color = Color.white;
                }
            }

            listing.End();
        }

        private void ShowDeleteFactionDialog(List<Faction> instances)
        {
            if (instances.Count == 1)
            {
                // 只有一个实例，直接显示确认对话框
                Find.WindowStack.Add(new Dialog_ConfirmDeleteFaction(instances[0], () => {
                    Close();
                }));
            }
            else
            {
                // 多个实例，先选择要删除的实例
                Find.WindowStack.Add(new Dialog_SelectFactionInstanceToDelete(factionDef, (selectedFaction) => {
                    Find.WindowStack.Add(new Dialog_ConfirmDeleteFaction(selectedFaction, () => {
                        Close();
                    }));
                }));
            }
        }

        private void DrawPlayerRelationSelector(Listing_Standard listing)
        {
            // Check if faction is permanently hostile to player
            // 需要检查玩家派系是否存在（世界生成阶段可能尚未创建）
            Faction playerFaction = Find.FactionManager?.OfPlayer;
            if (playerFaction == null) return;
            
            bool isPermanentlyHostile = factionDef.PermanentlyHostileTo(playerFaction.def);
            
            Rect rowRect = listing.GetRect(30f);
            float btnWidth = (rowRect.width - 20f) / 3f;

            // Ally Button
            Rect allyRect = new Rect(rowRect.x, rowRect.y, btnWidth, 30f);
            // Neutral Button
            Rect neutralRect = new Rect(rowRect.x + btnWidth + 10f, rowRect.y, btnWidth, 30f);
            // Hostile Button
            Rect hostileRect = new Rect(rowRect.x + (btnWidth + 10f) * 2, rowRect.y, btnWidth, 30f);

            if (isPermanentlyHostile)
            {
                // 永久敌对派系：显示敌对按钮为红色（选中状态），其他为灰色
                GUI.color = Color.gray;
                Widgets.ButtonText(allyRect, LanguageManager.Get("RelationAlly"), false);
                GUI.color = Color.gray;
                Widgets.ButtonText(neutralRect, LanguageManager.Get("RelationNeutral"), false);
                GUI.color = Color.red;
                Widgets.ButtonText(hostileRect, LanguageManager.Get("RelationHostile"), false);
                GUI.color = Color.white;
                
                // Show warning tooltip
                TooltipHandler.TipRegion(rowRect, LanguageManager.Get("RelationPermanentEnemyTooltip"));
                return;
            }

            bool isAlly = bufferPlayerRelation == FactionRelationKind.Ally;
            GUI.color = isAlly ? Color.green : Color.gray;
            if (Widgets.ButtonText(allyRect, LanguageManager.Get("RelationAlly")))
            {
                bufferPlayerRelation = isAlly ? (FactionRelationKind?)null : FactionRelationKind.Ally;
            }
            GUI.color = Color.white;

            bool isNeutral = bufferPlayerRelation == FactionRelationKind.Neutral;
            GUI.color = isNeutral ? Color.cyan : Color.gray;
            if (Widgets.ButtonText(neutralRect, LanguageManager.Get("RelationNeutral")))
            {
                bufferPlayerRelation = isNeutral ? (FactionRelationKind?)null : FactionRelationKind.Neutral;
            }
            GUI.color = Color.white;

            bool isHostile = bufferPlayerRelation == FactionRelationKind.Hostile;
            GUI.color = isHostile ? Color.red : Color.gray;
            if (Widgets.ButtonText(hostileRect, LanguageManager.Get("RelationHostile")))
            {
                bufferPlayerRelation = isHostile ? (FactionRelationKind?)null : FactionRelationKind.Hostile;
            }
            GUI.color = Color.white;

            // Current status tooltip
            string relationKey = bufferPlayerRelation.HasValue 
                ? GetRelationLanguageKey(bufferPlayerRelation.Value)
                : "RelationDefault";
            string currentStatus = bufferPlayerRelation.HasValue 
                ? LanguageManager.Get("RelationCurrent").Replace("{0}", LanguageManager.Get(relationKey))
                : LanguageManager.Get("RelationDefault");
            TooltipHandler.TipRegion(rowRect, currentStatus);
        }

        private string GetRelationLanguageKey(FactionRelationKind kind)
        {
            switch (kind)
            {
                case FactionRelationKind.Ally:
                    return "RelationAlly";
                case FactionRelationKind.Neutral:
                    return "RelationNeutral";
                case FactionRelationKind.Hostile:
                    return "RelationHostile";
                default:
                    return "RelationDefault";
            }
        }

        private void DrawKindList(Rect rect)
        {
            // Header
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 30f);
            Widgets.Label(headerRect, $"<b>{LanguageManager.Get("PawnKinds")}</b>");

            // Search Bar
            Rect searchRect = new Rect(rect.x, rect.y + 30f, rect.width, 30f);
            pawnKindSearchQuery = Widgets.TextField(searchRect, pawnKindSearchQuery);
            if (string.IsNullOrEmpty(pawnKindSearchQuery))
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(searchRect.ContractedBy(5f), LanguageManager.Get("Search") + "...");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            List<PawnKindDef> kinds = GetFilteredKinds();
            
            float rowHeight = 40f; 
            
            float y = rect.y + 65f;
            foreach (var kind in kinds)
            {
                Rect row = new Rect(rect.x, y, rect.width, rowHeight);
                if (y % 80f < 40f) Widgets.DrawAltRect(row);

                // Labels
                Rect labelRect = new Rect(row.x + 10f, row.y + 2f, row.width * 0.4f, 20f);
                Widgets.Label(labelRect, $"<b>{kind.LabelCap}</b>");
                
                Rect defNameRect = new Rect(row.x + 10f, row.y + 22f, row.width * 0.4f, 16f);
                GUI.color = Color.gray;
                Text.Font = GameFont.Tiny;
                Widgets.Label(defNameRect, kind.defName);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;

                // Edit Box (Custom Label)
                Rect editRect = new Rect(row.x + row.width * 0.5f, row.y + 5f, row.width * 0.45f, 30f);
                
                string currentVal;
                if (!kindLabelBuffers.TryGetValue(kind.defName, out currentVal))
                {
                    // Check if already modified in settings
                    var kd = factionData.GetKindData(kind.defName);
                    if (kd != null && !string.IsNullOrEmpty(kd.Label))
                    {
                        currentVal = kd.Label;
                    }
                    else
                    {
                        currentVal = kind.label;
                    }
                    kindLabelBuffers[kind.defName] = currentVal;
                }

                string newVal = Widgets.TextField(editRect, currentVal);
                if (newVal != currentVal)
                {
                    string sanitized = InputValidator.SanitizeName(newVal);
                    kindLabelBuffers[kind.defName] = sanitized;
                }

                y += rowHeight;
            }
        }


        private void ApplyChanges()
        {
            // Validate Xenotype Chances - sum must not exceed 100%
            if (ModsConfig.BiotechActive && factionDef.humanlikeFaction && bufferXenotypes.Count > 0)
            {
                float totalChance = bufferXenotypes.Values.Sum();
                if (totalChance > 1.0f)
                {
                    Messages.Message(LanguageManager.Get("XenotypeChancesExceed100"), MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }

            // Faction Changes
            bool factionModified = false;
            
            if (bufferLabel != factionDef.label)
            {
                factionData.Label = bufferLabel;
                factionModified = true;
            }
            string currentDesc = factionDef.description ?? "";
            if (bufferDescription != currentDesc)
            {
                factionData.Description = bufferDescription;
                factionModified = true;
            }
            string currentIcon = factionDef.factionIconPath ?? "";
            if (bufferIconPath != currentIcon)
            {
                factionData.IconPath = bufferIconPath;
                factionModified = true;
            }
            // Color comparison approximate
            if (factionData.Color != bufferColor)
            {
                factionData.Color = bufferColor;
                factionModified = true;
            }

            // Biotech Xenotypes
            if (ModsConfig.BiotechActive && factionDef.humanlikeFaction)
            {
                bool xenoModified = false;
                if (factionData.XenotypeChances == null && bufferXenotypes.Count > 0) xenoModified = true;
                else if (factionData.XenotypeChances != null)
                {
                    if (factionData.XenotypeChances.Count != bufferXenotypes.Count) xenoModified = true;
                    else
                    {
                        foreach (var kvp in bufferXenotypes)
                        {
                            if (!factionData.XenotypeChances.TryGetValue(kvp.Key, out float val) || Math.Abs(val - kvp.Value) > 0.001f)
                            {
                                xenoModified = true;
                                break;
                            }
                        }
                    }
                }

                if (xenoModified)
                {
                    factionData.XenotypeChances = new Dictionary<string, float>(bufferXenotypes);
                    factionModified = true;
                }
            }

            // Player Relation Override
            bool relationChanged = factionData.PlayerRelationOverride != bufferPlayerRelation;
            if (relationChanged)
            {
                factionData.PlayerRelationOverride = bufferPlayerRelation;
                factionModified = true;
            }

            if (factionModified)
            {
                factionData.isModified = true;
                FactionDefManager.ApplyFactionChanges(factionDef, factionData);
            }

            // Apply relation changes immediately even if no other faction changes
            if (relationChanged && Current.Game != null && Find.FactionManager != null)
            {
                FactionDefManager.ApplyFactionChanges(factionDef, factionData);
            }

            // Group Changes
            // Check if bufferGroups is different from factionData.groupMakers
            // Actually, we can just overwrite if bufferGroups is not empty.
            // If bufferGroups matches original exactly, we could potentially set to null, but that's hard to check.
            // So we just save what we have.
            
            // Simple check: if we have bufferGroups, we save them.
            // Unless they are identical to original?
            // For now, let's assume if the user opened the tab and modified something, they want to save.
            // Since we are not tracking "isDirty" for groups per se, we can compare with current applied?
            // Let's just always save bufferGroups to factionData.groupMakers if they exist.
            
            if (bufferGroups != null)
            {
                // We should check if it's different from current to set isModified flag correctly
                // But setting isModified = true is safe.
                factionData.groupMakers = new List<PawnGroupMakerData>();
                foreach (var g in bufferGroups)
                {
                    factionData.groupMakers.Add(g.DeepCopy());
                }
                factionData.isModified = true;
                
                // Also update save-level data if it exists
                var gameComponent = FactionGearGameComponent.Instance;
                if (gameComponent?.savedFactionGearData != null)
                {
                    var saveFactionData = gameComponent.savedFactionGearData.FirstOrDefault(f => f.factionDefName == factionData.factionDefName);
                    if (saveFactionData != null)
                    {
                        saveFactionData.groupMakers = new List<PawnGroupMakerData>();
                        foreach (var g in bufferGroups)
                        {
                            saveFactionData.groupMakers.Add(g.DeepCopy());
                        }
                        saveFactionData.isModified = true;
                    }
                }
                
                // Re-apply entire faction changes to ensure groups are updated
                FactionDefManager.ApplyFactionChanges(factionDef, factionData);
            }

            // Kind Changes
            foreach (var kvp in kindLabelBuffers)
            {
                PawnKindDef kDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kvp.Key);
                if (kDef == null) continue;

                // Check if changed from original or current
                string newVal = kvp.Value;
                // If it's different from default label, it's a modification
                // But we need to check if it's different from what's currently in settings to set isModified
                // Or just always update if buffer exists.
                
                var kd = factionData.GetOrCreateKindData(kvp.Key);
                string existingLabel = kd.Label ?? "";
                if (existingLabel != newVal)
                {
                    kd.Label = string.IsNullOrEmpty(newVal) ? null : newVal;
                    kd.isModified = true;
                    FactionDefManager.ApplyKindChanges(kDef, kd);
                }
            }

            FactionGearCustomizerMod.Settings.Write();
            FactionListPanel.MarkDirty();
            KindListPanel.ClearCache();
            // Force refresh of main editor
            FactionGearEditor.IsDirty = true;
            FactionGearEditor.MarkDirty();
            Messages.Message(LanguageManager.Get("SettingsSaved"), MessageTypeDefOf.PositiveEvent, false);
        }

        private void ResetChanges()
        {
            // Reset Faction
            factionData.Label = null;
            factionData.Description = null;
            factionData.IconPath = null;
            factionData.Color = null;
            factionData.XenotypeChances?.Clear();
            factionData.groupMakers?.Clear();
            factionData.PlayerRelationOverride = null;
            factionData.isModified = false; 
            
            FactionDefManager.ResetFaction(factionDef);

            // Reset buffers
            bufferLabel = GetInGameFactionName();
            bufferDescription = factionDef.description ?? "";
            bufferIconPath = factionDef.factionIconPath ?? "";
            bufferColor = factionDef.colorSpectrum != null && factionDef.colorSpectrum.Any() ? factionDef.colorSpectrum.First() : Color.white;
            
            bufferGroups.Clear();
            if (factionDef.pawnGroupMakers != null)
            {
                foreach (var maker in factionDef.pawnGroupMakers)
                {
                    bufferGroups.Add(new PawnGroupMakerData(maker));
                }
            }

            bufferPlayerRelation = null;

            bufferXenotypes.Clear();
            if (ModsConfig.BiotechActive && factionDef.humanlikeFaction && factionDef.xenotypeSet != null)
            {
                var chances = FactionDefManager.GetXenotypeChances(factionDef.xenotypeSet);
                if (chances != null)
                {
                    foreach (var chance in chances)
                    {
                        if (chance.xenotype != null)
                        {
                            bufferXenotypes[chance.xenotype.defName] = chance.chance;
                        }
                    }
                }
            }

            // Reset Kinds
            // We need to reset all kinds that are in the buffer or in the settings for this faction
            List<PawnKindDef> kinds = FactionGearEditor.GetFactionKinds(factionDef);
            foreach (var kind in kinds)
            {
                var kd = factionData.GetKindData(kind.defName);
                if (kd != null)
                {
                    kd.Label = null;
                    // Only reset isModified if no gear is modified
                    // Check if gear lists are empty or match default? 
                    // Actually isModified tracks ANY modification. 
                    // If we reset label, we should check if other things are modified.
                    // If everything else is default, set isModified = false.
                    // But checking "everything else" is complex.
                    // For now, just keep isModified true if we don't know.
                    // Or, if the user clicks "Reset" in this window, maybe they expect ONLY static data reset?
                    // Yes, "Reset Changes" usually means "Reset what I changed here".
                    
                    FactionDefManager.ResetKind(kind);
                }
                
                // Clear buffer
                if (kindLabelBuffers.ContainsKey(kind.defName))
                {
                    kindLabelBuffers.Remove(kind.defName);
                }
            }
            
            FactionGearCustomizerMod.Settings.Write();
            FactionListPanel.MarkDirty();
            Messages.Message(LanguageManager.Get("ResetChanges"), MessageTypeDefOf.NeutralEvent, false);
        }
    }
}
