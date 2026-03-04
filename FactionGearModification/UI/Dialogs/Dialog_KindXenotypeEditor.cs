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
        private Dictionary<string, string> xenotypeTextBuffers = new Dictionary<string, string>();
        
        // 颜色定义
        private static readonly Color CardBgColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        private static readonly Color HeaderBgColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color BorderColor = new Color(0.35f, 0.35f, 0.4f);
        private static readonly Color AccentColor = new Color(0.4f, 0.7f, 1f);
        private static readonly Color SuccessColor = new Color(0.3f, 0.8f, 0.4f);
        private static readonly Color WarningColor = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color DangerColor = new Color(0.9f, 0.3f, 0.3f);
        
        public override Vector2 InitialSize => new Vector2(700f, 800f);

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
                
                bufferXenotypes = new Dictionary<string, float>();
                xenotypeTextBuffers = new Dictionary<string, string>();
                if (data.XenotypeChances != null && data.XenotypeChances.Count > 0)
                {
                    bufferXenotypes = new Dictionary<string, float>(data.XenotypeChances);
                    foreach (var kvp in data.XenotypeChances)
                    {
                        xenotypeTextBuffers[kvp.Key] = (kvp.Value * 100).ToString("F1");
                    }
                }
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            float padding = 10f;
            float cardPadding = 8f;
            float curY = inRect.y + padding;
            
            // ========== 标题区域 ==========
            DrawHeader(new Rect(inRect.x + padding, curY, inRect.width - padding * 2, 50f));
            curY += 60f;
            
            // ========== 强制异种卡片 ==========
            Rect forcedCardRect = new Rect(inRect.x + padding, curY, inRect.width - padding * 2, 90f);
            DrawForcedXenotypeCard(forcedCardRect, cardPadding);
            curY += 100f;
            
            // ========== 异种概率配置卡片 ==========
            float remainingHeight = inRect.height - curY - padding - 50f; // 留50给底部按钮
            Rect configCardRect = new Rect(inRect.x + padding, curY, inRect.width - padding * 2, remainingHeight);
            DrawXenotypeConfigCard(configCardRect, cardPadding);
            
            // ========== 底部按钮 ==========
            DrawBottomButtons(new Rect(inRect.x + padding, inRect.yMax - 45f, inRect.width - padding * 2, 40f));
        }
        
        private void DrawHeader(Rect rect)
        {
            // 背景
            Widgets.DrawBoxSolid(rect, HeaderBgColor);
            GUI.color = BorderColor;
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;
            
            // 标题
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = AccentColor;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 28f), 
                LanguageManager.Get("KindXenotypeSettings"));
            GUI.color = Color.white;
            
            // 副标题（兵种名称）
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x, rect.y + 26f, rect.width, 20f), 
                kindDef.LabelCap + "  (" + kindDef.defName + ")");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
        
        private void DrawForcedXenotypeCard(Rect rect, float padding)
        {
            // 卡片背景
            Widgets.DrawBoxSolid(rect, CardBgColor);
            GUI.color = BorderColor;
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;
            
            Rect inner = rect.ContractedBy(padding);
            float curY = inner.y;
            
            // 卡片标题
            Text.Font = GameFont.Small;
            GUI.color = AccentColor;
            Widgets.Label(new Rect(inner.x, curY, 200f, 24f), 
                "▸ " + LanguageManager.Get("ForcedXenotype"));
            GUI.color = Color.white;
            TooltipHandler.TipRegion(new Rect(inner.x, curY, inner.width, 24f), 
                LanguageManager.Get("ForcedXenotypeTooltip"));
            curY += 28f;
            
            // 强制异种选择器
            float selectorHeight = 32f;
            if (string.IsNullOrEmpty(bufferForcedXenotype))
            {
                // 未选择状态 - 使用虚线边框按钮
                Rect btnRect = new Rect(inner.x, curY, inner.width, selectorHeight);
                GUI.color = new Color(0.3f, 0.3f, 0.35f);
                Widgets.DrawBox(btnRect, 1);
                GUI.color = Color.white;
                
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.6f, 0.6f, 0.65f);
                Widgets.Label(btnRect, "+ " + LanguageManager.Get("SelectXenotype"));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                
                if (Widgets.ButtonInvisible(btnRect))
                {
                    OpenForcedXenotypeSelector();
                }
            }
            else
            {
                // 已选择状态
                XenotypeDef forcedDef = DefDatabase<XenotypeDef>.GetNamedSilentFail(bufferForcedXenotype);
                string label = forcedDef?.LabelCap ?? bufferForcedXenotype;
                
                // 异种信息区域
                float infoWidth = inner.width - 90f;
                Rect infoRect = new Rect(inner.x, curY, infoWidth, selectorHeight);
                Widgets.DrawBoxSolid(infoRect, new Color(0.18f, 0.18f, 0.22f));
                GUI.color = BorderColor;
                Widgets.DrawBox(infoRect, 1);
                GUI.color = Color.white;
                
                // 图标
                if (forcedDef != null)
                {
                    Rect iconRect = new Rect(infoRect.x + 8f, infoRect.y + 4f, 24f, 24f);
                    Widgets.DrawTextureFitted(iconRect, forcedDef.Icon, 1f);
                }
                
                // 名称
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(infoRect.x + 40f, infoRect.y, infoRect.width - 50f, selectorHeight), 
                    "<b>" + label + "</b>");
                Text.Anchor = TextAnchor.UpperLeft;
                
                // 点击切换
                if (Widgets.ButtonInvisible(infoRect))
                {
                    OpenForcedXenotypeSelector();
                }
                
                // 清除按钮
                Rect clearRect = new Rect(inner.xMax - 80f, curY, 80f, selectorHeight);
                GUI.color = DangerColor;
                if (Widgets.ButtonText(clearRect, "✕ " + LanguageManager.Get("ClearForcedXenotype")))
                {
                    bufferForcedXenotype = null;
                }
                GUI.color = Color.white;
            }
        }
        
        private void DrawXenotypeConfigCard(Rect rect, float padding)
        {
            // 卡片背景
            Widgets.DrawBoxSolid(rect, CardBgColor);
            GUI.color = BorderColor;
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;
            
            Rect inner = rect.ContractedBy(padding);
            float curY = inner.y;
            
            // ========== 卡片标题和开关 ==========
            Rect headerRect = new Rect(inner.x, curY, inner.width, 32f);
            
            // 标题
            Text.Font = GameFont.Small;
            GUI.color = AccentColor;
            Widgets.Label(new Rect(headerRect.x, headerRect.y + 4f, 200f, 24f), 
                "▸ " + LanguageManager.Get("XenotypeChances"));
            GUI.color = Color.white;
            
            // 禁用开关（右对齐）
            float toggleWidth = 220f;
            Rect toggleRect = new Rect(headerRect.xMax - toggleWidth, headerRect.y, toggleWidth, 32f);
            bool newDisable = bufferDisableXenotypeChances;
            Widgets.CheckboxLabeled(toggleRect, LanguageManager.Get("DisableXenotypeChances"), ref newDisable);
            if (newDisable != bufferDisableXenotypeChances)
            {
                bufferDisableXenotypeChances = newDisable;
            }
            
            curY += 40f;
            
            // ========== 内容区域 ==========
            if (bufferDisableXenotypeChances)
            {
                // 禁用状态提示
                Rect disabledRect = new Rect(inner.x, curY, inner.width, 60f);
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                Widgets.Label(disabledRect, LanguageManager.Get("Disabled"));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                // 可滚动内容区域
                float contentHeight = inner.height - 50f; // 减去标题和总计行
                Rect scrollRect = new Rect(inner.x, curY, inner.width, contentHeight);
                
                float rowHeight = 48f;
                float viewHeight = bufferXenotypes.Count * rowHeight + 60f; // +60 给添加按钮区域
                Rect viewRect = new Rect(0, 0, scrollRect.width - 16f, Math.Max(viewHeight, scrollRect.height));
                
                Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);
                
                float viewY = 0f;
                
                // 添加按钮（固定在顶部）
                Rect addBtnRect = new Rect(0, viewY, 160f, 36f);
                GUI.color = new Color(0.25f, 0.35f, 0.45f);
                if (Widgets.ButtonText(addBtnRect, "+ " + LanguageManager.Get("AddXenotype")))
                {
                    OpenXenotypeSelectorForAdding();
                }
                GUI.color = Color.white;
                viewY += 48f;
                
                // 分隔线
                GUI.color = BorderColor;
                Widgets.DrawLineHorizontal(0, viewY - 6f, viewRect.width);
                GUI.color = Color.white;
                
                // 异种列表
                var sortedKeys = bufferXenotypes.Keys
                    .Select(k => new { Key = k, Def = DefDatabase<XenotypeDef>.GetNamedSilentFail(k) })
                    .OrderBy(x => x.Def?.LabelCap.ToString() ?? x.Key)
                    .Select(x => x.Key)
                    .ToList();
                
                for (int i = 0; i < sortedKeys.Count; i++)
                {
                    var key = sortedKeys[i];
                    DrawXenotypeRow(new Rect(0, viewY, viewRect.width, rowHeight), key, i);
                    viewY += rowHeight;
                }
                
                Widgets.EndScrollView();
                
                // ========== 总计行（固定在卡片底部） ==========
                float totalY = rect.yMax - padding - 36f;
                Rect totalRect = new Rect(inner.x, totalY, inner.width, 32f);
                
                GUI.color = new Color(0.1f, 0.1f, 0.12f);
                Widgets.DrawBoxSolid(totalRect, new Color(0.1f, 0.1f, 0.12f));
                GUI.color = BorderColor;
                Widgets.DrawBox(totalRect, 1);
                GUI.color = Color.white;
                
                float currentTotal = bufferXenotypes.Values.Sum();
                string totalText = $"{LanguageManager.Get("Total")}: {currentTotal:P0}";
                
                Text.Anchor = TextAnchor.MiddleCenter;
                if (currentTotal > 1.0f)
                {
                    GUI.color = DangerColor;
                    totalText += " (超过100%)";
                }
                else if (Math.Abs(currentTotal - 1.0f) < 0.01f)
                {
                    GUI.color = SuccessColor;
                }
                else
                {
                    GUI.color = WarningColor;
                }
                
                Widgets.Label(totalRect, "<b>" + totalText + "</b>");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }
        
        private void DrawXenotypeRow(Rect rect, string key, int index)
        {
            // 交替行背景
            if (index % 2 == 1)
            {
                Widgets.DrawAltRect(rect);
            }
            
            float val = bufferXenotypes[key];
            
            // 初始化文本缓冲区
            if (!xenotypeTextBuffers.ContainsKey(key))
            {
                xenotypeTextBuffers[key] = (val * 100).ToString("F1");
            }
            
            // 实时计算当前总和（不包括当前值）
            float otherTotal = bufferXenotypes.Values.Sum() - val;
            
            // 图标
            XenotypeDef xDef = DefDatabase<XenotypeDef>.GetNamedSilentFail(key);
            if (xDef != null)
            {
                Rect iconRect = new Rect(rect.x + 8f, rect.y + 12f, 24f, 24f);
                Widgets.DrawTextureFitted(iconRect, xDef.Icon, 1f);
            }
            
            // 名称
            string label = xDef?.LabelCap ?? key;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x + 40f, rect.y, 140f, rect.height), label);
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 滑动条区域
            float sliderX = rect.x + 190f;
            float sliderWidth = 200f;
            Rect sliderRect = new Rect(sliderX, rect.y + 14f, sliderWidth, 20f);
            
            // 绘制滑动条背景
            GUI.color = new Color(0.08f, 0.08f, 0.1f);
            Widgets.DrawBoxSolid(sliderRect, new Color(0.08f, 0.08f, 0.1f));
            GUI.color = Color.white;
            
            // 滑动条
            float newVal = Widgets.HorizontalSlider(sliderRect, val, 0f, 1f, true, null);
            
            // 应用滑动条的值
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
            
            // 百分比显示（在滑动条右侧）
            Rect percentRect = new Rect(sliderX + sliderWidth + 10f, rect.y + 10f, 50f, 24f);
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = AccentColor;
            Widgets.Label(percentRect, $"{(bufferXenotypes[key] * 100):F0}%");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 精确输入框
            Rect inputRect = new Rect(percentRect.xMax + 10f, rect.y + 10f, 60f, 24f);
            GUI.color = new Color(0.08f, 0.08f, 0.1f);
            Widgets.DrawBoxSolid(inputRect, new Color(0.08f, 0.08f, 0.1f));
            GUI.color = BorderColor;
            Widgets.DrawBox(inputRect, 1);
            GUI.color = Color.white;
            
            string newText = Widgets.TextField(inputRect, xenotypeTextBuffers[key]);
            if (newText != xenotypeTextBuffers[key])
            {
                xenotypeTextBuffers[key] = newText;
                if (float.TryParse(newText, out float parsedValue))
                {
                    float inputVal = parsedValue / 100f;
                    float potentialTotal = otherTotal + inputVal;
                    
                    if (potentialTotal <= 1.0f)
                    {
                        bufferXenotypes[key] = inputVal;
                    }
                    else
                    {
                        float maxAllowed = Math.Max(0f, 1.0f - otherTotal);
                        bufferXenotypes[key] = maxAllowed;
                    }
                }
            }
            
            // 删除按钮
            Rect removeRect = new Rect(rect.xMax - 36f, rect.y + 10f, 28f, 28f);
            GUI.color = DangerColor;
            if (Widgets.ButtonText(removeRect, "✕"))
            {
                bufferXenotypes.Remove(key);
                xenotypeTextBuffers.Remove(key);
            }
            GUI.color = Color.white;
        }
        
        private void DrawBottomButtons(Rect rect)
        {
            float btnWidth = 120f;
            float btnHeight = 36f;
            float spacing = 10f;
            
            // 取消按钮（左侧）
            Rect cancelRect = new Rect(rect.x, rect.y, btnWidth, btnHeight);
            GUI.color = new Color(0.3f, 0.3f, 0.35f);
            if (Widgets.ButtonText(cancelRect, LanguageManager.Get("Cancel")))
            {
                Close();
            }
            GUI.color = Color.white;
            
            // 应用按钮（右侧）
            Rect applyRect = new Rect(rect.xMax - btnWidth, rect.y, btnWidth, btnHeight);
            GUI.color = new Color(0.25f, 0.5f, 0.35f);
            if (Widgets.ButtonText(applyRect, LanguageManager.Get("Apply")))
            {
                ApplyChanges();
                Close();
            }
            GUI.color = Color.white;
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
                string label = xDef.LabelCap;
                
                options.Add(new FloatMenuOption(label, () => {
                    bufferForcedXenotype = xDef.defName;
                }, xDef.Icon, Color.white));
            }
            
            if (options.Any())
            {
                Find.WindowStack.Add(new FloatMenu(options));
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
                if (existingKeys.Contains(xDef.defName))
                    continue;
                
                string label = xDef.LabelCap;
                
                options.Add(new FloatMenuOption(label, () => {
                    bufferXenotypes[xDef.defName] = 0f;
                    xenotypeTextBuffers[xDef.defName] = "0.0";
                }, xDef.Icon, Color.white));
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
