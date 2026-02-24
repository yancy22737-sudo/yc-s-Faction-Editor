using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.UI;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_SelectFactionInstanceToDelete : Window
    {
        private readonly FactionDef factionDef;
        private readonly Action<Faction> onSelected;
        private readonly List<Faction> instances;
        private Vector2 scrollPosition;
        private Faction selectedInstance;

        public override Vector2 InitialSize => new Vector2(500f, 500f);

        public Dialog_SelectFactionInstanceToDelete(FactionDef factionDef, Action<Faction> onSelected)
        {
            this.factionDef = factionDef ?? throw new ArgumentNullException(nameof(factionDef));
            this.onSelected = onSelected;
            this.doCloseX = true;
            this.forcePause = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;

            // 获取该派系Def的所有实例
            instances = Find.FactionManager?.AllFactions
                .Where(f => f.def == factionDef && !f.IsPlayer)
                .ToList() ?? new List<Faction>();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 35f), 
                LanguageManager.Get("SelectFactionInstanceToDelete", factionDef.LabelCap));
            Text.Font = GameFont.Small;

            // 警告信息
            GUI.color = Color.red;
            Widgets.Label(new Rect(0, 40f, inRect.width, 25f), LanguageManager.Get("DeleteFactionWarning"));
            GUI.color = Color.white;

            // 实例列表
            Rect listRect = new Rect(0, 70f, inRect.width, inRect.height - 120f);
            Widgets.DrawMenuSection(listRect);
            Rect innerRect = listRect.ContractedBy(10f);

            float rowHeight = 50f;
            float viewWidth = innerRect.width - 16f;
            Rect viewRect = new Rect(0, 0, viewWidth, instances.Count * rowHeight);

            Widgets.BeginScrollView(innerRect, ref scrollPosition, viewRect);
            float y = 0f;

            foreach (var instance in instances)
            {
                DrawInstanceRow(new Rect(0, y, viewWidth, rowHeight), instance);
                y += rowHeight;
            }

            Widgets.EndScrollView();

            // 底部按钮
            float btnY = inRect.height - 40f;
            float btnWidth = 120f;

            if (selectedInstance != null)
            {
                GUI.color = Color.red;
                if (Widgets.ButtonText(new Rect(inRect.width / 2f - btnWidth - 10f, btnY, btnWidth, 35f), LanguageManager.Get("Next")))
                {
                    onSelected?.Invoke(selectedInstance);
                    Close();
                }
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(inRect.width / 2f - btnWidth - 10f, btnY, btnWidth, 35f), LanguageManager.Get("SelectInstance"));
                GUI.color = Color.white;
            }

            if (Widgets.ButtonText(new Rect(inRect.width / 2f + 10f, btnY, btnWidth, 35f), LanguageManager.Get("Cancel")))
            {
                Close();
            }
        }

        private void DrawInstanceRow(Rect rect, Faction instance)
        {
            if (instance == selectedInstance)
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                selectedInstance = instance;
            }

            // 图标
            Rect iconRect = new Rect(rect.x + 5f, rect.y + 5f, 40f, 40f);
            Texture2D factionIcon = GetFactionIconSafe(instance.def);
            if (factionIcon != null)
            {
                GUI.color = instance.color ?? Color.white;
                GUI.DrawTexture(iconRect, factionIcon);
                GUI.color = Color.white;
            }
            Widgets.DrawBox(iconRect);

            // 名称
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect nameRect = new Rect(iconRect.xMax + 10f, rect.y, rect.width - iconRect.xMax - 100f, rect.height);
            Widgets.Label(nameRect, $"<b>{instance.Name}</b>");

            // 好感度 - 安全获取，避免null关系导致错误日志
            float goodwill = 0f;
            FactionRelationKind relationKind = FactionRelationKind.Neutral;
            bool relationValid = false;
            
            try
            {
                Faction playerFaction = Find.FactionManager?.OfPlayer;
                if (playerFaction != null)
                {
                    FactionRelation relation = instance.RelationWith(playerFaction, false);
                    if (relation != null)
                    {
                        goodwill = instance.PlayerGoodwill;
                        relationKind = instance.PlayerRelationKind;
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

                Rect goodwillRect = new Rect(rect.xMax - 90f, rect.y + 10f, 80f, 30f);
                GUI.color = goodwillColor;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(goodwillRect, $"{goodwill:F0}");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            // 据点数量
            int settlementCount = Find.WorldObjects?.Settlements.Count(s => s.Faction == instance) ?? 0;
            Rect settlementRect = new Rect(rect.xMax - 90f, rect.y + 28f, 80f, 20f);
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(settlementRect, $"{LanguageManager.Get("Settlements")}: {settlementCount}");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
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
