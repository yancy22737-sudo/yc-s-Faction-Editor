using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace FactionGearCustomizer
{
    /// <summary>
    /// 装备详情对话框 - 点击显示角色装备信息，支持滚动查看
    /// </summary>
    public class Dialog_PawnGearInfo : Window
    {
        private Pawn pawn;
        private Vector2 scrollPosition = Vector2.zero;
        
        // 布局常量
        private const float WindowWidth = 450f;
        private const float MaxWindowHeight = 600f;
        private const float MinWindowHeight = 300f;
        private const float RowHeight = 24f;
        private const float IconSize = 20f;
        private const float Padding = 10f;
        private const float SectionGap = 15f;
        
        public override Vector2 InitialSize => new Vector2(WindowWidth, CalculateWindowHeight());
        
        public Dialog_PawnGearInfo(Pawn pawn)
        {
            this.pawn = pawn;
            this.doCloseX = true;
            this.forcePause = true;
            this.draggable = true;
            this.resizeable = false;
            this.closeOnClickedOutside = true;
        }
        
        /// <summary>
        /// 计算窗口高度 - 根据内容自适应，但有最大/最小限制
        /// </summary>
        private float CalculateWindowHeight()
        {
            float height = Padding * 2; // 上下边距
            
            // 标题高度
            height += 30f;
            
            // 基础属性区域
            height += 80f;
            
            // 装备区域
            int equipmentCount = pawn?.equipment?.AllEquipmentListForReading?.Count ?? 0;
            if (equipmentCount > 0)
            {
                height += SectionGap + 25f; // 标题 + 间距
                height += equipmentCount * RowHeight;
            }
            
            // 服装区域
            int apparelCount = pawn?.apparel?.WornApparel?.Count ?? 0;
            if (apparelCount > 0)
            {
                height += SectionGap + 25f; // 标题 + 间距
                height += apparelCount * RowHeight;
            }
            
            // 库存区域
            int inventoryCount = pawn?.inventory?.innerContainer?.Count ?? 0;
            if (inventoryCount > 0)
            {
                height += SectionGap + 25f;
                height += inventoryCount * RowHeight;
            }
            
            // 底部统计
            height += SectionGap + 40f;
            
            // 限制在最小和最大高度之间
            return Mathf.Clamp(height, MinWindowHeight, MaxWindowHeight);
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            float curY = 0f;
            
            // 标题
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0f, curY, inRect.width, 30f), pawn.LabelCap);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            curY += 35f;
            
            // 计算内容总高度
            float totalContentHeight = CalculateContentHeight(inRect.width);
            
            // 如果内容超过窗口高度，使用滚动视图
            float viewHeight = inRect.height - curY - 10f;
            bool needScroll = totalContentHeight > viewHeight;
            
            Rect scrollRect = new Rect(0f, curY, inRect.width, viewHeight);
            Rect contentRect = new Rect(0f, 0f, inRect.width - (needScroll ? 16f : 0f), totalContentHeight);
            
            if (needScroll)
            {
                Widgets.BeginScrollView(scrollRect, ref scrollPosition, contentRect);
            }
            
            float drawY = needScroll ? 0f : curY;
            
            // 绘制基础属性
            drawY = DrawBasicStats(new Rect(Padding, drawY, contentRect.width - Padding * 2, 80f));
            
            // 绘制装备
            drawY = DrawEquipmentSection(new Rect(Padding, drawY + SectionGap, contentRect.width - Padding * 2, 200f));
            
            // 绘制服装
            drawY = DrawApparelSection(new Rect(Padding, drawY + SectionGap, contentRect.width - Padding * 2, 300f));
            
            // 绘制库存
            drawY = DrawInventorySection(new Rect(Padding, drawY + SectionGap, contentRect.width - Padding * 2, 200f));
            
            // 绘制底部统计
            drawY = DrawBottomStats(new Rect(Padding, drawY + SectionGap, contentRect.width - Padding * 2, 40f));
            
            if (needScroll)
            {
                Widgets.EndScrollView();
            }
        }
        
        /// <summary>
        /// 计算内容总高度
        /// </summary>
        private float CalculateContentHeight(float width)
        {
            float height = 0f;
            
            // 基础属性
            height += 80f;
            
            // 装备
            int equipmentCount = pawn?.equipment?.AllEquipmentListForReading?.Count ?? 0;
            if (equipmentCount > 0)
            {
                height += SectionGap + 25f + equipmentCount * RowHeight;
            }
            
            // 服装
            int apparelCount = pawn?.apparel?.WornApparel?.Count ?? 0;
            if (apparelCount > 0)
            {
                height += SectionGap + 25f + apparelCount * RowHeight;
            }
            
            // 库存
            int inventoryCount = pawn?.inventory?.innerContainer?.Count ?? 0;
            if (inventoryCount > 0)
            {
                height += SectionGap + 25f + inventoryCount * RowHeight;
            }
            
            // 底部统计
            height += SectionGap + 40f;
            
            return height;
        }
        
        /// <summary>
        /// 绘制基础属性（负重、温度、护甲）
        /// </summary>
        private float DrawBasicStats(Rect rect)
        {
            float curY = rect.y;
            
            // 负重
            if (pawn?.inventory != null)
            {
                float mass = MassUtility.GearAndInventoryMass(pawn);
                float capacity = MassUtility.Capacity(pawn, null);
                GUI.color = Color.gray;
                Widgets.Label(new Rect(rect.x, curY, rect.width, RowHeight), 
                    $"{LanguageManager.Get("MassCarried")}: {mass:F2} / {capacity:F0} kg");
                GUI.color = Color.white;
                curY += RowHeight;
            }
            
            // 温度范围
            float minTemp = pawn?.GetStatValue(StatDefOf.ComfyTemperatureMin) ?? 0f;
            float maxTemp = pawn?.GetStatValue(StatDefOf.ComfyTemperatureMax) ?? 0f;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(rect.x, curY, rect.width, RowHeight), 
                $"{LanguageManager.Get("ComfortableTemperature")}: {minTemp:F0}C ~ {maxTemp:F0}C");
            GUI.color = Color.white;
            curY += RowHeight + 5f;
            
            // 护甲标题
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, curY, rect.width, RowHeight), LanguageManager.Get("OverallArmor"));
            curY += RowHeight;
            
            // 护甲值
            float sharpArmor = pawn?.GetStatValue(StatDefOf.ArmorRating_Sharp) ?? 0f;
            float bluntArmor = pawn?.GetStatValue(StatDefOf.ArmorRating_Blunt) ?? 0f;
            float heatArmor = pawn?.GetStatValue(StatDefOf.ArmorRating_Heat) ?? 0f;
            
            DrawStatRow(new Rect(rect.x, curY, rect.width, RowHeight), 
                LanguageManager.Get("ArmorSharp"), $"{sharpArmor:P0}");
            curY += RowHeight;
            DrawStatRow(new Rect(rect.x, curY, rect.width, RowHeight), 
                LanguageManager.Get("ArmorBlunt"), $"{bluntArmor:P0}");
            curY += RowHeight;
            DrawStatRow(new Rect(rect.x, curY, rect.width, RowHeight), 
                LanguageManager.Get("ArmorHeat"), $"{heatArmor:P0}");
            curY += RowHeight;
            
            return curY;
        }
        
        /// <summary>
        /// 绘制装备区域
        /// </summary>
        private float DrawEquipmentSection(Rect rect)
        {
            if (pawn?.equipment?.AllEquipmentListForReading == null || 
                !pawn.equipment.AllEquipmentListForReading.Any())
            {
                return rect.y;
            }
            
            float curY = rect.y;
            
            // 标题
            Text.Font = GameFont.Small;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Widgets.Label(new Rect(rect.x, curY, rect.width, 25f), LanguageManager.Get("Equipment"));
            GUI.color = Color.white;
            curY += 25f;
            
            // 绘制分隔线
            Widgets.DrawLineHorizontal(rect.x, curY - 2f, rect.width);
            
            // 装备列表
            foreach (var eq in pawn.equipment.AllEquipmentListForReading)
            {
                DrawThingRow(new Rect(rect.x, curY, rect.width, RowHeight), eq);
                curY += RowHeight;
            }
            
            return curY;
        }
        
        /// <summary>
        /// 绘制服装区域
        /// </summary>
        private float DrawApparelSection(Rect rect)
        {
            if (pawn?.apparel?.WornApparel == null || !pawn.apparel.WornApparel.Any())
            {
                return rect.y;
            }
            
            float curY = rect.y;
            
            // 标题
            Text.Font = GameFont.Small;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Widgets.Label(new Rect(rect.x, curY, rect.width, 25f), LanguageManager.Get("Apparel"));
            GUI.color = Color.white;
            curY += 25f;
            
            // 绘制分隔线
            Widgets.DrawLineHorizontal(rect.x, curY - 2f, rect.width);
            
            // 服装列表
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                DrawThingRow(new Rect(rect.x, curY, rect.width, RowHeight), apparel);
                curY += RowHeight;
            }
            
            return curY;
        }
        
        /// <summary>
        /// 绘制库存区域
        /// </summary>
        private float DrawInventorySection(Rect rect)
        {
            if (pawn?.inventory?.innerContainer == null || pawn.inventory.innerContainer.Count == 0)
            {
                return rect.y;
            }
            
            float curY = rect.y;
            
            // 标题
            Text.Font = GameFont.Small;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Widgets.Label(new Rect(rect.x, curY, rect.width, 25f), LanguageManager.Get("Inventory"));
            GUI.color = Color.white;
            curY += 25f;
            
            // 绘制分隔线
            Widgets.DrawLineHorizontal(rect.x, curY - 2f, rect.width);
            
            // 库存列表
            foreach (var thing in pawn.inventory.innerContainer)
            {
                DrawThingRow(new Rect(rect.x, curY, rect.width, RowHeight), thing);
                curY += RowHeight;
            }
            
            return curY;
        }
        
        /// <summary>
        /// 绘制底部统计信息
        /// </summary>
        private float DrawBottomStats(Rect rect)
        {
            float curY = rect.y;
            
            // 绘制分隔线
            Widgets.DrawLineHorizontal(rect.x, curY, rect.width);
            curY += 5f;
            
            // 总重量
            if (pawn?.inventory != null)
            {
                float mass = MassUtility.GearAndInventoryMass(pawn);
                float capacity = MassUtility.Capacity(pawn, null);
                float percent = capacity > 0f ? mass / capacity : 0f;
                
                Color color = percent > 0.9f ? Color.red : (percent > 0.75f ? Color.yellow : Color.green);
                GUI.color = color;
                Widgets.Label(new Rect(rect.x, curY, rect.width, RowHeight), 
                    $"{LanguageManager.Get("TotalMass")}: {mass:F2} / {capacity:F0} kg ({percent:P0})");
                GUI.color = Color.white;
            }
            
            return curY + RowHeight;
        }
        
        /// <summary>
        /// 绘制单个物品行
        /// </summary>
        private void DrawThingRow(Rect rect, Thing thing)
        {
            float curX = rect.x;
            
            // 图标
            Rect iconRect = new Rect(curX, rect.y + 2f, IconSize, IconSize);
            if (thing.def?.uiIcon != null)
            {
                Widgets.DrawTextureFitted(iconRect, thing.def.uiIcon, 1f);
            }
            curX += IconSize + 5f;
            
            // 名称
            float nameWidth = rect.width - IconSize - 80f;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(curX, rect.y, nameWidth, RowHeight), thing.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 重量
            float mass = thing.GetStatValue(StatDefOf.Mass) * thing.stackCount;
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(rect.xMax - 70f, rect.y, 70f, RowHeight), $"{mass:F2} kg");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 鼠标悬停提示
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, thing.DescriptionDetailed);
            }
            
            // 点击显示详情
            if (Widgets.ButtonInvisible(rect))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(thing));
            }
        }
        
        /// <summary>
        /// 绘制属性行（名称+值）
        /// </summary>
        private void DrawStatRow(Rect rect, string label, string value)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width * 0.6f, RowHeight), label);
            
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(rect.x + rect.width * 0.6f, rect.y, rect.width * 0.4f, RowHeight), value);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
