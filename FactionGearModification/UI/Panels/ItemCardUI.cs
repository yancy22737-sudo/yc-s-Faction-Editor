using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearModification.UI;
using FactionGearCustomizer.UI;
using FactionGearCustomizer.Validation;
using FactionGearCustomizer.Compat;

namespace FactionGearCustomizer.UI.Panels
{
    public static class ItemCardUI
    {
        private static readonly Color HeaderBgColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color ContentBgColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        private static readonly Color BorderColor = new Color(0.35f, 0.35f, 0.4f);
        private static readonly Color RemoveButtonColor = new Color(0.8f, 0.25f, 0.2f);

        public static float GetCardHeight(SpecRequirementEdit item)
        {
            if (item.PoolType != ItemPoolType.None)
            {
                return 80f; // 物品池卡片更简洁
            }
            
            float height = 36f;
            float rowHeight = 24f;
            float gap = 4f;

            height += rowHeight + gap;

            if (item.Thing != null && item.Thing.MadeFromStuff)
                height += rowHeight + gap;

            if (item.Thing != null && item.Thing.HasComp(typeof(CompQuality)))
                height += rowHeight + gap;

            height += rowHeight + gap;

            if (item.SelectionMode != ApparelSelectionMode.AlwaysTake)
                height += rowHeight + gap;

            height += 12f;

            return height;
        }

        public static void Draw(Listing_Standard ui, SpecRequirementEdit item, int index, Action onRemove, KindGearData kindData = null)
        {
            float cardHeight = GetCardHeight(item);
            Rect area = ui.GetRect(cardHeight);
            
            DrawCardBackground(area);
            area = area.ContractedBy(6f);

            if (item.PoolType != ItemPoolType.None)
            {
                DrawPoolHeader(area, item, onRemove);
                DrawPoolContent(area, item, kindData);
            }
            else
            {
                DrawHeader(area, item, onRemove, kindData);
                DrawContent(area, item, index, kindData);
            }
            
            ui.Gap(8f);
        }

        private static void DrawCardBackground(Rect area)
        {
            Widgets.DrawBoxSolid(area, ContentBgColor);
            GUI.color = BorderColor;
            Widgets.DrawBox(area, 1);
            GUI.color = Color.white;
        }

        private static void DrawHeader(Rect area, SpecRequirementEdit item, Action onRemove, KindGearData kindData)
        {
            Rect headerRect = new Rect(area.x, area.y + 2f, area.width, 28f);
            Widgets.DrawBoxSolid(headerRect, HeaderBgColor);

            // Icon
            Rect iconRect = new Rect(headerRect.x + 6f, headerRect.y + 4f, 20f, 20f);
            Widgets.DrawBoxSolid(iconRect, new Color(0.3f, 0.3f, 0.35f));
            if (item.Thing != null)
            {
                WidgetsUtils.DrawTextureFitted(iconRect, item.Thing.uiIcon, 1f);
            }

            // Title / Item Selector
            Rect titleRect = new Rect(iconRect.xMax + 8f, headerRect.y, headerRect.width - 60f, headerRect.height);
            string titleText = item.Thing?.LabelCap ?? LanguageManager.Get("SelectItem");
            
            if (Widgets.ButtonText(titleRect, titleText, false, true, true, TextAnchor.MiddleLeft))
            {
                Find.WindowStack.Add(new Dialog_InventoryItemPicker(def =>
                {
                    if (kindData != null)
                    {
                        UndoManager.RecordState(kindData);
                        kindData.isModified = true;
                    }
                    item.Thing = def;
                    FactionGearEditor.MarkDirty();
                }));
            }

            // Remove Button
            Rect removeRect = new Rect(headerRect.xMax - 24f, headerRect.y + (headerRect.height - 24f) / 2f, 24f, 24f);
            if (Widgets.ButtonImage(removeRect, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
            {
                onRemove?.Invoke();
            }
        }

        private static void DrawContent(Rect area, SpecRequirementEdit item, int index, KindGearData kindData)
        {
            float contentY = area.y + 36f;
            float rowHeight = 24f;
            float gap = 4f;

            // Row 1: Material (If applicable)
            if (item.Thing != null && item.Thing.MadeFromStuff)
            {
                Rect matRect = new Rect(area.x, contentY, area.width, rowHeight);
                DrawMaterialSelector(matRect, item, kindData);
                contentY += rowHeight + gap;
            }

            // Row 2: Quality (If applicable)
            if (item.Thing != null && item.Thing.HasComp(typeof(CompQuality)))
            {
                Rect qualRect = new Rect(area.x, contentY, area.width, rowHeight);
                DrawQualitySelector(qualRect, item, kindData);
                contentY += rowHeight + gap;
            }

            // Row 3: Count Range
            Rect countRect = new Rect(area.x, contentY, area.width, rowHeight);
            Widgets.Label(countRect.LeftHalf(), LanguageManager.Get("Count") + ":");
            IntRange range = item.CountRange;
            IntRange oldRange = item.CountRange;
            int maxCount = GetMaxCountForItem(item.Thing);
            Widgets.IntRange(countRect.RightHalf(), item.GetHashCode(), ref range, 1, maxCount);
            if (range != oldRange)
            {
                if (kindData != null)
                {
                    var validation = InventoryLimitValidator.ValidateItemCountRange(kindData, item, range);
                    if (!validation.IsValid)
                    {
                        Messages.Message(LanguageManager.Get(validation.ErrorKey, validation.ErrorArgs), MessageTypeDefOf.RejectInput, false);
                        range = oldRange;
                    }
                    else if (validation.IsWarning)
                    {
                        Messages.Message(validation.WarningMessage, MessageTypeDefOf.CautionInput, false);
                    }

                    UndoManager.RecordState(kindData);
                    kindData.isModified = true;
                }
                item.CountRange = range;
                FactionGearEditor.MarkDirty();
                InventoryLimitValidator.InvalidateCache();
            }
            contentY += rowHeight + gap;

            // Row 4: Selection Mode
            Rect modeRect = new Rect(area.x, contentY, area.width, rowHeight);
            DrawModeSelector(modeRect, item, kindData);
            contentY += rowHeight + gap;

            // Row 5: Chance Slider
            if (item.SelectionMode != ApparelSelectionMode.AlwaysTake)
            {
                Rect chanceRect = new Rect(area.x, contentY, area.width, rowHeight);
                float oldChance = item.SelectionChance;
                float newChance = Widgets.HorizontalSlider(chanceRect, item.SelectionChance, 0f, 1f, true, LanguageManager.Get("Chance") + ": " + item.SelectionChance.ToString("P0"));
                if (newChance != oldChance)
                {
                    if (kindData != null)
                    {
                        UndoManager.RecordState(kindData);
                        kindData.isModified = true;
                    }
                    item.SelectionChance = newChance;
                    FactionGearEditor.MarkDirty();
                }
                
                if (Mouse.IsOver(chanceRect))
                {
                    Widgets.DrawBoxSolid(chanceRect.ExpandedBy(2f), new Color(1f, 1f, 1f, 0.05f));
                }
            }
        }

        private static void DrawMaterialSelector(Rect rect, SpecRequirementEdit item, KindGearData kindData)
        {
            string label = item.Material == null ? LanguageManager.Get("RandomDefault") : LanguageManager.Get("MaterialSelect", item.Material.LabelCap);
            if (Widgets.ButtonText(rect, label))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption(LanguageManager.Get("RandomDefault"), () => 
                { 
                    if (kindData != null)
                    {
                        UndoManager.RecordState(kindData);
                        kindData.isModified = true;
                    }
                    item.Material = null; 
                    FactionGearEditor.MarkDirty(); 
                }));
                
                if (item.Thing != null && item.Thing.MadeFromStuff)
                {
                    foreach (var stuff in GenStuff.AllowedStuffsFor(item.Thing))
                    {
                        ThingDef capturedStuff = stuff;
                        options.Add(new FloatMenuOption(stuff.LabelCap, () => 
                        { 
                            if (kindData != null)
                            {
                                UndoManager.RecordState(kindData);
                                kindData.isModified = true;
                            }
                            item.Material = capturedStuff; 
                            FactionGearEditor.MarkDirty(); 
                        }));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static string GetQualityLabel(QualityCategory q)
        {
            switch (q)
            {
                case QualityCategory.Awful: return LanguageManager.Get("QualityAwful");
                case QualityCategory.Poor: return LanguageManager.Get("QualityPoor");
                case QualityCategory.Normal: return LanguageManager.Get("QualityNormal");
                case QualityCategory.Good: return LanguageManager.Get("QualityGood");
                case QualityCategory.Excellent: return LanguageManager.Get("QualityExcellent");
                case QualityCategory.Masterwork: return LanguageManager.Get("QualityMasterwork");
                case QualityCategory.Legendary: return LanguageManager.Get("QualityLegendary");
                default: return q.ToString();
            }
        }

        private static void DrawQualitySelector(Rect rect, SpecRequirementEdit item, KindGearData kindData)
        {
            string label = item.Quality.HasValue ? LanguageManager.Get("QualitySelect", GetQualityLabel(item.Quality.Value)) : LanguageManager.Get("QualitySelect", LanguageManager.Get("Default"));
            if (Widgets.ButtonText(rect, label))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption(LanguageManager.Get("Default"), () => 
                { 
                    if (kindData != null)
                    {
                        UndoManager.RecordState(kindData);
                        kindData.isModified = true;
                    }
                    item.Quality = null; 
                    FactionGearEditor.MarkDirty(); 
                }));
                foreach (QualityCategory q in Enum.GetValues(typeof(QualityCategory)))
                {
                    QualityCategory capturedQ = q;
                    options.Add(new FloatMenuOption(GetQualityLabel(q), () => 
                    { 
                        if (kindData != null)
                        {
                            UndoManager.RecordState(kindData);
                            kindData.isModified = true;
                        }
                        item.Quality = capturedQ; 
                        FactionGearEditor.MarkDirty(); 
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void DrawModeSelector(Rect rect, SpecRequirementEdit item, KindGearData kindData)
        {
            string label = LanguageManager.Get("SelectionMode", item.SelectionMode.ToString());
            if (Widgets.ButtonText(rect, label))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (ApparelSelectionMode m in Enum.GetValues(typeof(ApparelSelectionMode)))
                {
                    ApparelSelectionMode capturedM = m;
                    options.Add(new FloatMenuOption(m.ToString(), () => 
                    { 
                        if (kindData != null)
                        {
                            UndoManager.RecordState(kindData);
                            kindData.isModified = true;
                        }
                        item.SelectionMode = capturedM; 
                        FactionGearEditor.MarkDirty(); 
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void DrawPoolHeader(Rect area, SpecRequirementEdit item, Action onRemove)
        {
            Rect headerRect = new Rect(area.x, area.y + 2f, area.width, 28f);
            Widgets.DrawBoxSolid(headerRect, HeaderBgColor);

            // Icon
            Rect iconRect = new Rect(headerRect.x + 6f, headerRect.y + 4f, 20f, 20f);
            Widgets.DrawBoxSolid(iconRect, new Color(0.3f, 0.3f, 0.35f));
            
            // 显示通用图标
            Texture2D icon = GetPoolIcon(item.PoolType);
            if (icon != null)
            {
                WidgetsUtils.DrawTextureFitted(iconRect, icon, 1f);
            }

            // Title
            Rect titleRect = new Rect(iconRect.xMax + 8f, headerRect.y, headerRect.width - 60f, headerRect.height);
            string titleText = GetPoolLabel(item.PoolType);
            
            if (Widgets.ButtonText(titleRect, titleText, false, true, true, TextAnchor.MiddleLeft))
            {
                // 编辑物品池内容
                EditPoolContent(item);
            }

            // Remove Button
            Rect removeRect = new Rect(headerRect.xMax - 24f, headerRect.y + (headerRect.height - 24f) / 2f, 24f, 24f);
            if (Widgets.ButtonImage(removeRect, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
            {
                onRemove?.Invoke();
            }
        }

        private static void DrawPoolContent(Rect area, SpecRequirementEdit item, KindGearData kindData)
        {
            float contentY = area.y + 36f;
            float rowHeight = 24f;
            float gap = 4f;

            // Count Range
            Rect countRect = new Rect(area.x, contentY, area.width, rowHeight);
            Widgets.Label(countRect.LeftHalf(), LanguageManager.Get("Count") + ":");
            IntRange range = item.CountRange;
            IntRange oldRange = item.CountRange;
            Widgets.IntRange(countRect.RightHalf(), item.GetHashCode(), ref range, 1, 100);
            if (range != oldRange)
            {
                if (kindData != null)
                {
                    UndoManager.RecordState(kindData);
                    kindData.isModified = true;
                }
                item.CountRange = range;
                FactionGearEditor.MarkDirty();
            }
            contentY += rowHeight + gap;

            // Selection Mode
            Rect modeRect = new Rect(area.x, contentY, area.width, rowHeight);
            DrawModeSelector(modeRect, item, kindData);
        }

        private static Texture2D GetPoolIcon(ItemPoolType poolType)
        {
            switch (poolType)
            {
                case ItemPoolType.AnyFood:
                case ItemPoolType.AnyMeal:
                    return ThingDefOf.MealSimple.uiIcon;
                case ItemPoolType.AnyRawFood:
                case ItemPoolType.AnyVegetable:
                    return ThingDefOf.RawPotatoes.uiIcon;
                case ItemPoolType.AnyMeat:
                    return ThingDefOf.Meat_Human.uiIcon;
                case ItemPoolType.AnyMedicine:
                    return ThingDefOf.MedicineHerbal.uiIcon;
                case ItemPoolType.AnySocialDrug:
                    return ThingDefOf.Beer.uiIcon;
                case ItemPoolType.AnyHardDrug:
                    return ThingDefOf.WakeUp.uiIcon;
                default:
                    return null;
            }
        }

        private static string GetPoolLabel(ItemPoolType poolType)
        {
            switch (poolType)
            {
                case ItemPoolType.AnyFood:
                    return LanguageManager.Get("Pool_AnyFood");
                case ItemPoolType.AnyMeal:
                    return LanguageManager.Get("Pool_AnyMeal");
                case ItemPoolType.AnyRawFood:
                    return LanguageManager.Get("Pool_AnyRawFood");
                case ItemPoolType.AnyMeat:
                    return LanguageManager.Get("Pool_AnyMeat");
                case ItemPoolType.AnyVegetable:
                    return LanguageManager.Get("Pool_AnyVegetable");
                case ItemPoolType.AnyMedicine:
                    return LanguageManager.Get("Pool_AnyMedicine");
                case ItemPoolType.AnySocialDrug:
                    return LanguageManager.Get("Pool_AnySocialDrug");
                case ItemPoolType.AnyHardDrug:
                    return LanguageManager.Get("Pool_AnyHardDrug");
                default:
                    return "Item Pool";
            }
        }

        private static void EditPoolContent(SpecRequirementEdit item)
        {
            // 创建一个详细的编辑对话框
            Find.WindowStack.Add(new Dialog_EditPoolContent(item));
        }

        private class Dialog_EditPoolContent : Window
        {
            private SpecRequirementEdit item;
            private ItemPoolType tempPoolType;
            private IntRange tempCountRange;
            private ApparelSelectionMode tempSelectionMode;
            private float tempSelectionChance;

            public Dialog_EditPoolContent(SpecRequirementEdit item)
            {
                this.item = item;
                this.tempPoolType = item.PoolType;
                this.tempCountRange = item.CountRange;
                this.tempSelectionMode = item.SelectionMode;
                this.tempSelectionChance = item.SelectionChance;
                this.closeOnCancel = true;
                this.closeOnAccept = false;
                this.doCloseButton = false;
                this.doCloseX = true;
            }

            public override Vector2 InitialSize => new Vector2(400f, 350f);

            public override void DoWindowContents(Rect inRect)
            {
                // 标题
                Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
                Text.Font = GameFont.Medium;
                Widgets.Label(titleRect, LanguageManager.Get("EditPoolContents"));
                Text.Font = GameFont.Small;
                
                Listing_Standard listing = new Listing_Standard();
                listing.Begin(new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f));

                // 物品池类型
                Rect poolTypeRect = listing.GetRect(32f);
                Widgets.Label(poolTypeRect, LanguageManager.Get("PoolType") + ":");
                poolTypeRect.x += 120f;
                poolTypeRect.width -= 120f;
                
                if (Widgets.ButtonText(poolTypeRect, GetPoolLabel(tempPoolType)))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (ItemPoolType type in Enum.GetValues(typeof(ItemPoolType)))
                    {
                        if (type != ItemPoolType.None)
                        {
                            options.Add(new FloatMenuOption(GetPoolLabel(type), () => { tempPoolType = type; }));
                        }
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }

                listing.Gap(8f);

                // 数量范围
                Rect countRect = listing.GetRect(32f);
                Widgets.Label(countRect.LeftHalf(), LanguageManager.Get("Count") + ":");
                Widgets.IntRange(countRect.RightHalf(), GetHashCode(), ref tempCountRange, 1, 100);

                listing.Gap(8f);

                // 选择模式
                Rect modeRect = listing.GetRect(32f);
                Widgets.Label(modeRect, LanguageManager.Get("SelectionMode") + ":");
                modeRect.x += 120f;
                modeRect.width -= 120f;
                
                if (Widgets.ButtonText(modeRect, tempSelectionMode.ToString()))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (ApparelSelectionMode mode in Enum.GetValues(typeof(ApparelSelectionMode)))
                    {
                        options.Add(new FloatMenuOption(mode.ToString(), () => { tempSelectionMode = mode; }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }

                listing.Gap(8f);

                // 选择概率
                if (tempSelectionMode != ApparelSelectionMode.AlwaysTake)
                {
                    Rect chanceRect = listing.GetRect(32f);
                    Widgets.Label(chanceRect, LanguageManager.Get("Chance") + ":");
                    chanceRect.x += 120f;
                    chanceRect.width -= 120f;
                    tempSelectionChance = Widgets.HorizontalSlider(chanceRect, tempSelectionChance, 0f, 1f, true, tempSelectionChance.ToString("P0"));
                }

                listing.Gap(20f);

                // 按钮
                Rect buttonRect = listing.GetRect(36f);
                Rect cancelRect = new Rect(buttonRect.x, buttonRect.y, buttonRect.width / 2 - 5f, 36f);
                Rect okRect = new Rect(buttonRect.x + buttonRect.width / 2 + 5f, buttonRect.y, buttonRect.width / 2 - 5f, 36f);

                if (Widgets.ButtonText(cancelRect, LanguageManager.Get("Cancel")))
                {
                    Close();
                }

                if (Widgets.ButtonText(okRect, LanguageManager.Get("OK")))
                {
                    // 保存更改
                    item.PoolType = tempPoolType;
                    item.CountRange = tempCountRange;
                    item.SelectionMode = tempSelectionMode;
                    item.SelectionChance = tempSelectionChance;
                    FactionGearEditor.MarkDirty();
                    Close();
                }

                listing.End();
            }
        }

        private static int GetMaxCountForItem(ThingDef thingDef)
        {
            if (thingDef == null) return 50;
            
            int maxCount = CECompat.GetSuggestedMaxCount(thingDef, 50);
            
            return maxCount > 0 ? maxCount : 50;
        }
    }
}
