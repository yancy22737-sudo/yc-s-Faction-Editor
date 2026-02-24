using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using FactionGearCustomizer.Managers;

namespace FactionGearCustomizer.UI
{
    public class Dialog_CreateInstance : Window
    {
        private Vector2 scrollPosition;
        private FactionDef selectedFactionDef;
        private List<FactionDef> availableFactionDefs;
        private List<FactionDef> disabledFactionDefs;
        private string searchBuffer = "";

        public override Vector2 InitialSize => new Vector2(500f, 600f);

        public Dialog_CreateInstance()
        {
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            
            // 获取游戏中已存在的派系Def（非玩家派系）
            disabledFactionDefs = new List<FactionDef>();
            if (Current.Game != null && Find.FactionManager != null)
            {
                disabledFactionDefs = Find.FactionManager.AllFactions
                    .Where(f => !f.IsPlayer && f.def != null)
                    .Select(f => f.def)
                    .Distinct()
                    .ToList();
            }
            
            // Filter available faction defs
            availableFactionDefs = DefDatabase<FactionDef>.AllDefsListForReading
                .Where(f => f.humanlikeFaction && !f.isPlayer && !f.hidden)
                .OrderBy(f => f.LabelCap.ToString() ?? "")
                .ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), LanguageManager.Get("CreateNewInstance"));
            Text.Font = GameFont.Small;

            // Search bar
            float searchY = 40f;
            string search = Widgets.TextField(new Rect(0f, searchY, inRect.width, 30f), searchBuffer);
            if (search != searchBuffer)
            {
                searchBuffer = search;
            }

            // List
            float listY = searchY + 35f;
            float listHeight = inRect.height - listY - 60f; // Reserve space for buttons
            Rect outRect = new Rect(0f, listY, inRect.width, listHeight);
            
            var filteredDefs = availableFactionDefs
                .Where(f => searchBuffer.NullOrEmpty() || f.LabelCap.ToString().ToLower().Contains(searchBuffer.ToLower()))
                .ToList();

            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, filteredDefs.Count * 30f);
            
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            
            float y = 0f;
            foreach (var def in filteredDefs)
            {
                Rect rowRect = new Rect(0f, y, viewRect.width, 30f);
                
                // 检查该派系是否已在游戏中存在
                bool isDisabled = disabledFactionDefs.Contains(def);
                
                if (isDisabled)
                {
                    // 已存在的派系显示为灰色背景
                    Widgets.DrawLightHighlight(rowRect);
                    GUI.color = new Color(1f, 1f, 1f, 0.3f); // 降低透明度
                }
                else if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }
                
                if (selectedFactionDef == def && !isDisabled)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }

                // 只有未禁用的派系才可以被选择
                if (!isDisabled && Widgets.ButtonInvisible(rowRect))
                {
                    selectedFactionDef = def;
                }

                Rect iconRect = new Rect(rowRect.x, rowRect.y + 3f, 24f, 24f);
                Texture2D factionIcon = GetFactionIconSafe(def);
                if (factionIcon != null)
                {
                    GUI.color = def.colorSpectrum != null && def.colorSpectrum.Count > 0 ? def.colorSpectrum[0] : Color.white;
                    if (isDisabled) GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.3f);
                    GUI.DrawTexture(iconRect, factionIcon);
                    GUI.color = Color.white;
                }

                Rect labelRect = new Rect(iconRect.xMax + 5f, rowRect.y, rowRect.width - iconRect.xMax - 5f, 30f);
                Text.Anchor = TextAnchor.MiddleLeft;
                
                // 已存在的派系显示灰色文字并添加提示
                if (isDisabled)
                {
                    GUI.color = Color.gray;
                    string labelText = def.LabelCap + " (" + LanguageManager.Get("AlreadyExists") + ")";
                    Widgets.Label(labelRect, labelText);
                    TooltipHandler.TipRegion(rowRect, LanguageManager.Get("FactionAlreadyExistsTooltip"));
                }
                else
                {
                    Widgets.Label(labelRect, def.LabelCap);
                }
                
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                y += 30f;
            }
            
            Widgets.EndScrollView();

            // Bottom Buttons
            float buttonY = inRect.height - 40f;
            
            if (selectedFactionDef != null)
            {
                // Instance count warning logic
                int instanceCount = Find.FactionManager.AllFactions.Count(f => f.def == selectedFactionDef && !f.IsPlayer);
                string countLabel = $"{LanguageManager.Get("InstanceCount")}: {instanceCount}";
                
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(0f, buttonY, 150f, 40f), countLabel);
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonText(new Rect(inRect.width - 120f, buttonY, 120f, 40f), LanguageManager.Get("Create")))
                {
                    TryCreateInstance();
                }
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(0f, buttonY, inRect.width, 40f), LanguageManager.Get("SelectFactionToCreate"));
                GUI.color = Color.white;
            }
        }

        private void TryCreateInstance()
        {
            if (selectedFactionDef == null) return;

            int instanceCount = Find.FactionManager.AllFactions.Count(f => f.def == selectedFactionDef && !f.IsPlayer);
            if (instanceCount >= 20)
            {
                Messages.Message(LanguageManager.Get("TooManyInstances"), MessageTypeDefOf.RejectInput, false);
                return;
            }

            string nextName = NameGenerator.GenerateName(selectedFactionDef.factionNameMaker, 
                from fac in Find.FactionManager.AllFactionsVisible
                select fac.Name, false, null);

            // Store reference to self for callback
            Dialog_CreateInstance self = this;

            Find.WindowStack.Add(new Dialog_MessageBox(
                LanguageManager.Get("ConfirmCreateNewInstance") + "\n\n" +
                LanguageManager.Get("NewInstancePreview", nextName),
                LanguageManager.Get("Confirm"),
                () => {
                    Faction faction = FactionSpawnManager.SpawnFactionInstance(selectedFactionDef, nextName);
                    self.Close();

                    if (faction != null)
                    {
                        // Delay opening the next dialog to avoid window stack conflicts
                        Verse.LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            Find.WindowStack.Add(new Dialog_MessageBox(
                                LanguageManager.Get("ConfirmCreateSettlement"),
                                LanguageManager.Get("Confirm"),
                                () => {
                                    // Ensure we're in a stable state before opening FactionSpawnWindow
                                    if (Current.Game != null && Find.World != null)
                                    {
                                        Find.WindowStack.Add(new FactionSpawnWindow(faction));
                                    }
                                },
                                LanguageManager.Get("Cancel"),
                                null,
                                LanguageManager.Get("CreateNewInstance"),
                                true,
                                null,
                                null
                            ));
                        });
                    }
                },
                LanguageManager.Get("Cancel"),
                null,
                LanguageManager.Get("CreateNewInstance"),
                true,
                null,
                null
            ));
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

            // 对于非自定义路径，使用ContentFinder安全加载
            if (!def.factionIconPath.NullOrEmpty())
            {
                Texture2D icon = ContentFinder<Texture2D>.Get(def.factionIconPath, false);
                if (icon != null)
                {
                    return icon;
                }
            }

            // 最后尝试使用FactionIcon属性，但捕获可能的异常
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
