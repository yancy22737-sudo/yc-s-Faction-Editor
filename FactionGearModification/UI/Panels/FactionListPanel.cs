using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.UI; // For Dialog_CreateInstance

namespace FactionGearCustomizer.UI.Panels
{
    public static class FactionListPanel
    {
        // Cache variables to avoid resorting every frame
        private static List<(FactionDef def, Faction worldFaction, bool isModified, string displayName, Color color, float priority)> cachedFactionList = null;
        private static bool dirtyCache = true;
        private static ProgramState lastProgramState = ProgramState.Entry;
        private static int lastFactionCount = -1;

        public static void MarkDirty()
        {
            dirtyCache = true;
        }

        public static void Draw(Rect rect)
        {
            if (Current.ProgramState != lastProgramState)
            {
                lastProgramState = Current.ProgramState;
                MarkDirty();
                EditorSession.ResetSession();
            }
            
            // 检测派系数量变化，自动刷新缓存
            if (Current.ProgramState == ProgramState.Playing && Find.FactionManager != null)
            {
                int currentFactionCount = Find.FactionManager.AllFactions.Count();
                if (currentFactionCount != lastFactionCount)
                {
                    lastFactionCount = currentFactionCount;
                    MarkDirty();
                }
            }

            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            
            // New Faction Button (only available in game) - Draw first to reserve space
            bool inGame = Current.Game != null;
            
            string newFactionLabel = LanguageManager.Get("NewFaction");
            Text.Font = GameFont.Small;
            Vector2 newFactionSize = Text.CalcSize(newFactionLabel);
            float btnWidth = Mathf.Max(newFactionSize.x + 10f, 40f); // Minimum width 40f
            Rect newFactionRect = new Rect(innerRect.xMax - btnWidth, innerRect.y + 3f, btnWidth, 24f);
            
            if (!inGame)
            {
                GUI.color = Color.gray;
            }
            
            if (Widgets.ButtonText(newFactionRect, newFactionLabel, true, false, inGame))
            {
                if (inGame)
                {
                    Find.WindowStack.Add(new Dialog_CreateInstance());
                }
            }
            
            if (!inGame)
            {
                TooltipHandler.TipRegion(newFactionRect, LanguageManager.Get("OnlyAvailableInGame"));
                GUI.color = Color.white;
            }
            else
            {
                TooltipHandler.TipRegion(newFactionRect, LanguageManager.Get("CreateNewInstance"));
            }
            
            Text.Font = GameFont.Medium;
            
            // Title "Factions" - Draw after button to avoid overlap
            Vector2 titleSize = Text.CalcSize(LanguageManager.Get("Factions"));
            Rect titleRect = new Rect(innerRect.x, innerRect.y, titleSize.x + 10f, 30f);
            Widgets.Label(titleRect, LanguageManager.Get("Factions"));
            
            // "Real Name" checkbox - Position relative to button to avoid overlap
            if (Current.ProgramState == ProgramState.Playing)
            {
                Rect checkboxRect = new Rect(titleRect.xMax, innerRect.y + 3f, 24f, 24f);
                bool tempUseInGameNames = EditorSession.UseInGameNames;
                Widgets.Checkbox(new Vector2(checkboxRect.x, checkboxRect.y), ref tempUseInGameNames);
                if (tempUseInGameNames != EditorSession.UseInGameNames)
                {
                    EditorSession.UseInGameNames = tempUseInGameNames;
                    MarkDirty();
                }
                
                Text.Font = GameFont.Tiny;
                string labelText = LanguageManager.Get("InGame");
                Vector2 labelSize = Text.CalcSize(labelText);
                Rect labelRect = new Rect(checkboxRect.xMax + 4f, innerRect.y + 8f, labelSize.x + 5f, 20f);
                GUI.color = Color.gray;
                Widgets.Label(labelRect, labelText);
                GUI.color = Color.white;
                
                string tip = LanguageManager.Get("ShowInGameFactionNamesTooltip");
                TooltipHandler.TipRegion(checkboxRect, tip);
                TooltipHandler.TipRegion(labelRect, tip);
                
                Text.Font = GameFont.Medium;
            }
            
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            
            // Calculate list area
            // The original code used innerRect.height * 0.6f which is odd for a full panel draw.
            // But since we are passing 'rect' which is the left panel rect, we should use most of it.
            // Wait, the original code had 3 panels, so DrawLeftPanel took the left rect.
            // But inside DrawLeftPanel, it calculated `factionListHeight = innerRect.height * 0.6f;`
            // Why 0.6? Maybe there was something below it?
            // Checking the original code... it seems there was nothing below it in DrawLeftPanel.
            // Wait, let me check FactionGearEditor.cs again to see if there is anything else in DrawLeftPanel.
            // Lines 367-600 only show DrawLeftPanel drawing the list.
            // Ah, line 413: float factionListHeight = innerRect.height * 0.6f;
            // And line 414: Rect factionListOutRect = new Rect(..., factionListHeight - 35f);
            // It seems it only uses 60% of the height.
            // But wait, the method ends at line 600.
            // I should check if there is more code after line 600 in DrawLeftPanel.
            // The Read tool only read up to 600.
            // I need to check if DrawLeftPanel continues after 600.
            
            DrawFactionList(innerRect);
        }

        private static void DrawFactionList(Rect innerRect)
        {
            // Use full height for now, unless there is a reason to use 0.6f
            // If the original intention was to leave space for something else, it might be unused now.
            // I will use full available height minus title.
            float listY = innerRect.y + 35f;
            Rect factionListOutRect = new Rect(innerRect.x, listY, innerRect.width, innerRect.height - 35f);

            if (dirtyCache || cachedFactionList == null)
            {
                BuildFactionList();
                dirtyCache = false;
            }

            // Calculate content height
            float totalHeight = 0f;
            float viewWidth = factionListOutRect.width - 16f;
            foreach (var data in cachedFactionList)
            {
                float maxRowWidth = viewWidth - 62f; 
                if (data.worldFaction != null) maxRowWidth -= 44f;
                float height = Text.CalcHeight(data.displayName, maxRowWidth);
                totalHeight += Mathf.Max(height + 8f, 32f);
            }
            
            Rect factionListViewRect = new Rect(0, 0, viewWidth, totalHeight);
            Widgets.BeginScrollView(factionListOutRect, ref EditorSession.FactionListScrollPos, factionListViewRect);
            
            float y = 0;
            float infoButtonOffset = EditorSession.IsInGame ? 24f : 0f;
            foreach (var data in cachedFactionList)
            {
                float maxRowWidth = factionListViewRect.width - 62f;
                if (data.worldFaction != null) maxRowWidth -= 44f;

                float rowHeight = Mathf.Max(Text.CalcHeight(data.displayName, maxRowWidth) + 8f, 32f);
                Rect rowRect = new Rect(0, y, factionListViewRect.width, rowHeight);

                // Handle Selection
                // Exclude InfoCard button area (24px on the right) to avoid click conflict
                Rect selectionRect = new Rect(rowRect.x, rowRect.y, rowRect.width - infoButtonOffset, rowRect.height);
                
                bool isSelected = false;
                if (EditorSession.SelectedFactionInstance != null)
                {
                    if (data.worldFaction == EditorSession.SelectedFactionInstance) isSelected = true;
                }
                else
                {
                    // Fallback to Def matching if no instance selected (e.g. uninstantiated defs or main menu)
                    // But ensure we don't highlight instances if we meant to select the uninstantiated one?
                    // Actually, if we are in game, and have instances, we should always have SelectedFactionInstance set if we clicked one.
                    // If we clicked a gray one (worldFaction == null), SelectedFactionInstance is null.
                    if (data.worldFaction == null && EditorSession.SelectedFactionDefName == data.def.defName) isSelected = true;
                    
                    // Legacy/Fallback: if we are not in game, or just starting, we might only have DefName set.
                    // In that case, highlight the first matching def?
                    // Or if we are in Main Menu, worldFaction is always null, so the above check works.
                }

                if (isSelected)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else if (Mouse.IsOver(selectionRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                if (Widgets.ButtonInvisible(selectionRect))
                {
                    if (EditorSession.SelectedFactionDefName != data.def.defName || EditorSession.SelectedFactionInstance != data.worldFaction)
                    {
                        EditorSession.SelectedFactionDefName = data.def.defName;
                        EditorSession.SelectedFactionInstance = data.worldFaction;
                        EditorSession.SelectedKindDefName = ""; // Reset kind selection
                        // Trigger any other updates needed
                        FactionGearEditor.OnFactionSelected();
                    }
                }

                // Draw Row Content
                DrawFactionRow(rowRect, data, rowHeight);

                y += rowHeight;
            }
            
            Widgets.EndScrollView();
        }

        private static void DrawFactionRow(Rect rowRect, (FactionDef def, Faction worldFaction, bool isModified, string displayName, Color color, float priority) data, float rowHeight)
        {
            // Icon
            Rect iconRect = new Rect(rowRect.x + 4f, rowRect.y + (rowHeight - 24f) / 2f, 24f, 24f);
            Texture2D iconToDraw = GetFactionIconSafe(data.def);
            if (iconToDraw != null)
            {
                GUI.color = data.def.colorSpectrum != null && data.def.colorSpectrum.Any() ? data.def.colorSpectrum.First() : Color.white;
                GUI.DrawTexture(iconRect, iconToDraw);
                GUI.color = Color.white;
            }

            // Info Button (only show in game)
            float infoButtonOffset = EditorSession.IsInGame ? 24f : 0f;
            if (EditorSession.IsInGame)
            {
                Rect infoButtonRect = new Rect(rowRect.xMax - 24f, rowRect.y + (rowHeight - 24f) / 2f, 24f, 24f);
                Widgets.InfoCardButton(infoButtonRect.x, infoButtonRect.y, data.def);
                TooltipHandler.TipRegion(infoButtonRect, LanguageManager.Get("FactionInfoCardTooltip"));
            }

            // Goodwill
            float goodwillWidth = 0f;
            if (data.worldFaction != null && !data.worldFaction.IsPlayer)
            {
                goodwillWidth = 40f;
                Rect goodwillRect = new Rect(rowRect.xMax - infoButtonOffset - goodwillWidth - 4f, rowRect.y, goodwillWidth, rowHeight);
                
                // 安全获取派系关系，避免null关系导致错误日志
                float goodwill = 0f;
                FactionRelationKind relationKind = FactionRelationKind.Neutral;
                bool relationValid = false;
                
                try
                {
                    // 先检查与玩家派系的关系是否存在，避免触发RimWorld的null关系错误日志
                    Faction playerFaction = Find.FactionManager?.OfPlayer;
                    if (playerFaction != null)
                    {
                        FactionRelation relation = data.worldFaction.RelationWith(playerFaction, false);
                        if (relation != null)
                        {
                            goodwill = data.worldFaction.PlayerGoodwill;
                            relationKind = data.worldFaction.PlayerRelationKind;
                            relationValid = true;
                        }
                    }
                }
                catch
                {
                    relationValid = false;
                }
                
                if (relationValid)
                {
                    Color goodwillColor = relationKind == FactionRelationKind.Ally ? Color.green : 
                                          (relationKind == FactionRelationKind.Hostile ? Color.red : Color.cyan);
                    
                    Text.Anchor = TextAnchor.MiddleRight;
                    GUI.color = goodwillColor;
                    Widgets.Label(goodwillRect, goodwill.ToString("F0"));
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;
                    
                    TooltipHandler.TipRegion(goodwillRect, $"{relationKind}: {goodwill:F0}");
                }
            }

            // Name
            float nameX = iconRect.xMax + 6f;
            float nameWidth = rowRect.width - nameX - infoButtonOffset - 4f;
            if (data.worldFaction != null && !data.worldFaction.IsPlayer) nameWidth -= (goodwillWidth + 4f);
            
            Rect nameRect = new Rect(nameX, rowRect.y, nameWidth, rowHeight);
            
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = data.color;
            Widgets.Label(nameRect, data.displayName);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void BuildFactionList()
        {
            cachedFactionList = new List<(FactionDef, Faction, bool, string, Color, float)>();
            var allDefs = DefDatabase<FactionDef>.AllDefsListForReading;
            
            bool useInstances = Current.ProgramState == ProgramState.Playing && EditorSession.UseInGameNames && Find.FactionManager != null;
            var processedDefs = new HashSet<FactionDef>();
            var processedFactions = new HashSet<Faction>();

            if (useInstances)
            {
                foreach (var faction in Find.FactionManager.AllFactions)
                {
                    if (faction == null || faction.def == null) continue;
                    if (processedFactions.Contains(faction)) continue;
                    processedFactions.Add(faction);
                    
                    if (TryCreateFactionEntry(faction.def, faction, out var entry))
                    {
                        cachedFactionList.Add(entry);
                        processedDefs.Add(faction.def);
                    }
                }
            }

            foreach (var def in allDefs)
            {
                if (processedDefs.Contains(def)) continue;
                
                if (TryCreateFactionEntry(def, null, out var entry))
                {
                    cachedFactionList.Add(entry);
                }
            }
            
            cachedFactionList.Sort((a, b) => {
                int priorityCompare = a.priority.CompareTo(b.priority);
                if (priorityCompare != 0) return priorityCompare;
                return a.displayName.CompareTo(b.displayName);
            });
        }

        private static bool TryCreateFactionEntry(FactionDef def, Faction instance, out (FactionDef, Faction, bool, string, Color, float) entry)
        {
            entry = default;
            
            // Filter logic
            bool isHidden = !def.humanlikeFaction || def.hidden;
            if (!FactionGearCustomizerMod.Settings.ShowHiddenFactions && isHidden) return false;
            
            if (def.pawnGroupMakers == null || !def.pawnGroupMakers.Any(pgm => pgm.options != null && pgm.options.Any(o => o.kind != null))) return false;

            string displayName;
            Color color;
            float priority;

            if (instance != null)
            {
                displayName = instance.Name;
                color = instance.color ?? Color.white;
                priority = GetFactionPriority(def);
            }
            else
            {
                displayName = def.LabelCap.ToString();
                // If instance is null but we are in game, use Gray to indicate "No Instance"
                // If not in game (Main Menu), use def color
                if (Current.ProgramState == ProgramState.Playing && EditorSession.UseInGameNames)
                {
                    color = Color.gray;
                    priority = 9999f;
                }
                else
                {
                    color = def.colorSpectrum != null && def.colorSpectrum.Any() ? def.colorSpectrum.First() : Color.white;
                    priority = GetFactionPriority(def);
                }
            }

            bool isModified = IsFactionModified(def);
            if (isModified) displayName = "<color=yellow>*</color> " + displayName;

            entry = (def, instance, isModified, displayName, color, priority);
            return true;
        }

        private static bool IsFactionModified(FactionDef def)
        {
            var factionData = FactionGearCustomizerMod.Settings.factionGearData.FirstOrDefault(f => f.factionDefName == def.defName);
            if (factionData != null)
            {
                return factionData.isModified || factionData.kindGearData.Any(k => k.isModified);
            }
            return false;
        }

        private static float GetFactionPriority(FactionDef factionDef)
        {
            if (factionDef.defName == "PlayerColony") return -100f;
            if (factionDef.defName == "PlayerTribe") return -99f;
            return 0f;
        }

        /// <summary>
        /// 安全获取派系图标，正确处理自定义图标路径（Custom:前缀）
        /// </summary>
        private static Texture2D GetFactionIconSafe(FactionDef def)
        {
            if (def == null) return null;

            // 检查是否是自定义图标路径
            if (!def.factionIconPath.NullOrEmpty() && def.factionIconPath.StartsWith("Custom:"))
            {
                string iconName = def.factionIconPath.Substring(7);
                Texture2D customIcon = CustomIconManager.GetIcon(iconName);
                if (customIcon != null)
                {
                    return customIcon;
                }
                // 如果自定义图标加载失败，返回默认错误纹理
                return BaseContent.BadTex;
            }

            // 对于非自定义路径，使用原始的FactionIcon属性
            try
            {
                return def.FactionIcon;
            }
            catch
            {
                return BaseContent.BadTex;
            }
        }
    }
}
