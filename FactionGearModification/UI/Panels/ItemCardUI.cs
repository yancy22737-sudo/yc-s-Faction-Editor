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

        private const float HeaderHeight = 28f;
        private const float RowHeight = 24f;
        private const float Gap = 4f;
        private const float CardPad = 6f;

        public static float GetCardHeight(SpecRequirementEdit item)
        {
            if (item.PoolType != ItemPoolType.None)
                return HeaderHeight + Gap + RowHeight + Gap + RowHeight + CardPad * 2;

            float h = HeaderHeight + Gap; // header
            h += RowHeight + Gap; // count range (always present)
            if (item.Thing != null && item.Thing.MadeFromStuff) h += RowHeight + Gap; // material
            if (item.Thing != null && item.Thing.HasComp(typeof(CompQuality))) h += RowHeight + Gap; // quality
            h += RowHeight + Gap; // selection mode
            if (item.SelectionMode != ApparelSelectionMode.AlwaysTake) h += RowHeight + Gap; // chance
            return h + CardPad * 2;
        }

        public static void Draw(Listing_Standard ui, SpecRequirementEdit item, int index, Action onRemove, KindGearData kindData = null)
        {
            float cardHeight = GetCardHeight(item);
            Rect area = ui.GetRect(cardHeight);
            DrawCardBg(area);
            Rect inner = area.ContractedBy(CardPad);

            if (item.PoolType != ItemPoolType.None)
            {
                DrawPoolCard(inner, item, onRemove, kindData);
            }
            else
            {
                DrawItemCard(inner, item, onRemove, kindData);
            }

            ui.Gap(Gap);
        }

        private static void DrawCardBg(Rect area)
        {
            Widgets.DrawBoxSolid(area, ContentBgColor);
            GUI.color = BorderColor;
            Widgets.DrawBox(area, 1);
            GUI.color = Color.white;
        }

        // ── Normal item card ─────────────────────────────────
        private static void DrawItemCard(Rect inner, SpecRequirementEdit item, Action onRemove, KindGearData kindData)
        {
            // Header
            Rect hdr = new Rect(inner.x, inner.y, inner.width, HeaderHeight);
            Widgets.DrawBoxSolid(hdr, HeaderBgColor);

            Rect ico = new Rect(hdr.x + 4f, hdr.y + 4f, 20f, 20f);
            Widgets.DrawBoxSolid(ico, new Color(0.3f, 0.3f, 0.35f));
            if (item.Thing != null) WidgetsUtils.DrawTextureFitted(ico, item.Thing.uiIcon, 1f);

            float titleW = hdr.xMax - 28f - (ico.xMax + 6f);
            Rect title = new Rect(ico.xMax + 6f, hdr.y, titleW, hdr.height);
            string titleText = item.Thing?.LabelCap ?? LanguageManager.Get("SelectItem");
            if (Widgets.ButtonText(title, titleText, false, true, true, TextAnchor.MiddleLeft))
            {
                string initialAmmoSet = null;
                if (item.Thing != null && CECompat.IsCEActive)
                    initialAmmoSet = CECompat.GetCaliberLabelForAmmo(item.Thing);
                Find.WindowStack.Add(new Dialog_InventoryItemPicker(def =>
                {
                    if (kindData != null) { UndoManager.RecordState(kindData); kindData.isModified = true; }
                    item.Thing = def;
                    FactionGearEditor.MarkDirty();
                }, initialAmmoSet));
            }

            Rect del = new Rect(hdr.xMax - 22f, hdr.y + 3f, 22f, 22f);
            if (Widgets.ButtonImage(del, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
                onRemove?.Invoke();

            // Content rows
            float y = hdr.yMax + Gap;

            // Material (if applicable)
            if (item.Thing != null && item.Thing.MadeFromStuff)
            {
                DrawMaterialRow(new Rect(inner.x, y, inner.width, RowHeight), item, kindData);
                y += RowHeight + Gap;
            }

            // Quality (if applicable)
            if (item.Thing != null && item.Thing.HasComp(typeof(CompQuality)))
            {
                DrawQualityRow(new Rect(inner.x, y, inner.width, RowHeight), item, kindData);
                y += RowHeight + Gap;
            }

            // Count range
            Rect countLbl = new Rect(inner.x, y, inner.width * 0.12f, RowHeight);
            Widgets.Label(countLbl, LanguageManager.Get("Count") + ":");
            Rect countRng = new Rect(inner.x + inner.width * 0.13f, y, inner.width * 0.87f, RowHeight);
            DrawCountRange(countRng, item, kindData);
            y += RowHeight + Gap;

            // Selection mode
            Rect modeLbl = new Rect(inner.x, y, inner.width * 0.18f, RowHeight);
            Widgets.Label(modeLbl, LanguageManager.Get("SelectionMode") + ":");
            Rect modeBtn = new Rect(inner.x + inner.width * 0.19f, y, inner.width * 0.81f, RowHeight);
            DrawModeSelector(modeBtn, item, kindData);
            y += RowHeight + Gap;

            // Chance slider (if not AlwaysTake)
            if (item.SelectionMode != ApparelSelectionMode.AlwaysTake)
            {
                Rect chanceLbl = new Rect(inner.x, y, inner.width * 0.12f, RowHeight);
                Widgets.Label(chanceLbl, LanguageManager.Get("Chance") + ":");
                Rect chanceSld = new Rect(inner.x + inner.width * 0.13f, y + 2f, inner.width * 0.87f, RowHeight - 4f);
                float old = item.SelectionChance;
                float val = Widgets.HorizontalSlider(chanceSld, item.SelectionChance, 0f, 1f, true, item.SelectionChance.ToString("P0"));
                if (val != old)
                {
                    if (kindData != null) { UndoManager.RecordState(kindData); kindData.isModified = true; }
                    item.SelectionChance = val;
                    FactionGearEditor.MarkDirty();
                }
            }
        }

        // ── Pool card ────────────────────────────────────────
        private static void DrawPoolCard(Rect inner, SpecRequirementEdit item, Action onRemove, KindGearData kindData)
        {
            Rect hdr = new Rect(inner.x, inner.y, inner.width, HeaderHeight);
            Widgets.DrawBoxSolid(hdr, HeaderBgColor);

            Rect ico = new Rect(hdr.x + 4f, hdr.y + 4f, 20f, 20f);
            Widgets.DrawBoxSolid(ico, new Color(0.3f, 0.3f, 0.35f));
            Texture2D icon = GetPoolIcon(item.PoolType);
            if (icon != null) WidgetsUtils.DrawTextureFitted(ico, icon, 1f);

            float titleW = hdr.xMax - 28f - (ico.xMax + 6f);
            Rect title = new Rect(ico.xMax + 6f, hdr.y, titleW, hdr.height);
            if (Widgets.ButtonText(title, GetPoolLabel(item.PoolType), false, true, true, TextAnchor.MiddleLeft))
                Find.WindowStack.Add(new Dialog_EditPoolContent(item));

            Rect del = new Rect(hdr.xMax - 22f, hdr.y + 3f, 22f, 22f);
            if (Widgets.ButtonImage(del, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
                onRemove?.Invoke();

            // Content: count + mode
            float y = hdr.yMax + Gap;
            Rect countLbl = new Rect(inner.x, y, inner.width * 0.12f, RowHeight);
            Widgets.Label(countLbl, LanguageManager.Get("Count") + ":");
            Rect countRng = new Rect(inner.x + inner.width * 0.13f, y, inner.width * 0.87f, RowHeight);
            IntRange range = item.CountRange;
            Widgets.IntRange(countRng, item.GetHashCode(), ref range, 1, 100);
            if (range != item.CountRange)
            {
                if (kindData != null) { UndoManager.RecordState(kindData); kindData.isModified = true; }
                item.CountRange = range;
                FactionGearEditor.MarkDirty();
            }
            y += RowHeight + Gap;

            Rect modeLbl = new Rect(inner.x, y, inner.width * 0.18f, RowHeight);
            Widgets.Label(modeLbl, LanguageManager.Get("SelectionMode") + ":");
            Rect modeBtn = new Rect(inner.x + inner.width * 0.19f, y, inner.width * 0.81f, RowHeight);
            DrawModeSelector(modeBtn, item, kindData);
        }

        // ── Shared helpers ───────────────────────────────────
        private static void DrawMaterialRow(Rect rect, SpecRequirementEdit item, KindGearData kindData)
        {
            string label = item.Material == null ? LanguageManager.Get("RandomDefault") : LanguageManager.Get("MaterialSelect", item.Material.LabelCap);
            if (Widgets.ButtonText(rect, label))
            {
                var options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption(LanguageManager.Get("RandomDefault"), () =>
                {
                    if (kindData != null) { UndoManager.RecordState(kindData); kindData.isModified = true; }
                    item.Material = null; FactionGearEditor.MarkDirty();
                }));
                if (item.Thing != null && item.Thing.MadeFromStuff)
                {
                    foreach (var stuff in GenStuff.AllowedStuffsFor(item.Thing))
                    {
                        ThingDef cap = stuff;
                        options.Add(new FloatMenuOption(stuff.LabelCap, () =>
                        {
                            if (kindData != null) { UndoManager.RecordState(kindData); kindData.isModified = true; }
                            item.Material = cap; FactionGearEditor.MarkDirty();
                        }));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void DrawQualityRow(Rect rect, SpecRequirementEdit item, KindGearData kindData)
        {
            string label = item.Quality.HasValue
                ? LanguageManager.Get("QualitySelect", GetQualityLabel(item.Quality.Value))
                : LanguageManager.Get("QualitySelect", LanguageManager.Get("Default"));
            if (Widgets.ButtonText(rect, label))
            {
                var options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption(LanguageManager.Get("Default"), () =>
                {
                    if (kindData != null) { UndoManager.RecordState(kindData); kindData.isModified = true; }
                    item.Quality = null; FactionGearEditor.MarkDirty();
                }));
                foreach (QualityCategory q in Enum.GetValues(typeof(QualityCategory)))
                {
                    QualityCategory cap = q;
                    options.Add(new FloatMenuOption(GetQualityLabel(q), () =>
                    {
                        if (kindData != null) { UndoManager.RecordState(kindData); kindData.isModified = true; }
                        item.Quality = cap; FactionGearEditor.MarkDirty();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void DrawCountRange(Rect rect, SpecRequirementEdit item, KindGearData kindData)
        {
            IntRange range = item.CountRange;
            int max = GetMaxCountForItem(item.Thing);
            Widgets.IntRange(rect, item.GetHashCode(), ref range, 1, max);
            if (range != item.CountRange)
            {
                if (kindData != null)
                {
                    var v = InventoryLimitValidator.ValidateItemCountRange(kindData, item, range);
                    if (!v.IsValid) { Messages.Message(LanguageManager.Get(v.ErrorKey, v.ErrorArgs), MessageTypeDefOf.RejectInput, false); return; }
                    if (v.IsWarning) Messages.Message(v.WarningMessage, MessageTypeDefOf.CautionInput, false);
                    UndoManager.RecordState(kindData);
                    kindData.isModified = true;
                }
                item.CountRange = range;
                FactionGearEditor.MarkDirty();
                InventoryLimitValidator.InvalidateCache();
            }
        }

        private static void DrawModeSelector(Rect rect, SpecRequirementEdit item, KindGearData kindData)
        {
            string label = LanguageManager.Get("SelectionMode", item.SelectionMode.ToString());
            if (Widgets.ButtonText(rect, label))
            {
                var options = new List<FloatMenuOption>();
                foreach (ApparelSelectionMode m in Enum.GetValues(typeof(ApparelSelectionMode)))
                {
                    ApparelSelectionMode cap = m;
                    options.Add(new FloatMenuOption(m.ToString(), () =>
                    {
                        if (kindData != null) { UndoManager.RecordState(kindData); kindData.isModified = true; }
                        item.SelectionMode = cap; FactionGearEditor.MarkDirty();
                    }));
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

        private static Texture2D GetPoolIcon(ItemPoolType poolType)
        {
            switch (poolType)
            {
                case ItemPoolType.AnyFood:
                case ItemPoolType.AnyMeal: return ThingDefOf.MealSimple.uiIcon;
                case ItemPoolType.AnyRawFood:
                case ItemPoolType.AnyVegetable: return ThingDefOf.RawPotatoes.uiIcon;
                case ItemPoolType.AnyMeat: return ThingDefOf.Meat_Human.uiIcon;
                case ItemPoolType.AnyMedicine: return ThingDefOf.MedicineHerbal.uiIcon;
                case ItemPoolType.AnySocialDrug: return ThingDefOf.Beer.uiIcon;
                case ItemPoolType.AnyHardDrug: return ThingDefOf.WakeUp.uiIcon;
                default: return null;
            }
        }

        private static string GetPoolLabel(ItemPoolType poolType)
        {
            switch (poolType)
            {
                case ItemPoolType.AnyFood: return LanguageManager.Get("Pool_AnyFood");
                case ItemPoolType.AnyMeal: return LanguageManager.Get("Pool_AnyMeal");
                case ItemPoolType.AnyRawFood: return LanguageManager.Get("Pool_AnyRawFood");
                case ItemPoolType.AnyMeat: return LanguageManager.Get("Pool_AnyMeat");
                case ItemPoolType.AnyVegetable: return LanguageManager.Get("Pool_AnyVegetable");
                case ItemPoolType.AnyMedicine: return LanguageManager.Get("Pool_AnyMedicine");
                case ItemPoolType.AnySocialDrug: return LanguageManager.Get("Pool_AnySocialDrug");
                case ItemPoolType.AnyHardDrug: return LanguageManager.Get("Pool_AnyHardDrug");
                default: return "Item Pool";
            }
        }

        private static int GetMaxCountForItem(ThingDef thingDef)
        {
            if (thingDef == null) return 50;
            int max = CECompat.GetSuggestedMaxCount(thingDef, 50);
            return max > 0 ? max : 50;
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
                tempPoolType = item.PoolType;
                tempCountRange = item.CountRange;
                tempSelectionMode = item.SelectionMode;
                tempSelectionChance = item.SelectionChance;
                doCloseX = true;
            }

            public override Vector2 InitialSize => new Vector2(400f, 300f);

            public override void DoWindowContents(Rect inRect)
            {
                float y = inRect.y;
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 30f), LanguageManager.Get("EditPoolContents"));
                Text.Font = GameFont.Small;
                y += 36f;

                // Pool type
                Widgets.Label(new Rect(inRect.x, y, 100f, 28f), LanguageManager.Get("PoolType") + ":");
                if (Widgets.ButtonText(new Rect(inRect.x + 105f, y, inRect.width - 105f, 28f), GetPoolLabel(tempPoolType)))
                {
                    var opts = new List<FloatMenuOption>();
                    foreach (ItemPoolType t in Enum.GetValues(typeof(ItemPoolType)))
                        if (t != ItemPoolType.None) opts.Add(new FloatMenuOption(GetPoolLabel(t), () => tempPoolType = t));
                    Find.WindowStack.Add(new FloatMenu(opts));
                }
                y += 34f;

                // Count range
                Widgets.Label(new Rect(inRect.x, y, 100f, 28f), LanguageManager.Get("Count") + ":");
                Widgets.IntRange(new Rect(inRect.x + 105f, y, inRect.width - 105f, 28f), GetHashCode(), ref tempCountRange, 1, 100);
                y += 34f;

                // Selection mode
                Widgets.Label(new Rect(inRect.x, y, 100f, 28f), LanguageManager.Get("SelectionMode") + ":");
                if (Widgets.ButtonText(new Rect(inRect.x + 105f, y, inRect.width - 105f, 28f), tempSelectionMode.ToString()))
                {
                    var opts = new List<FloatMenuOption>();
                    foreach (ApparelSelectionMode m in Enum.GetValues(typeof(ApparelSelectionMode)))
                        opts.Add(new FloatMenuOption(m.ToString(), () => tempSelectionMode = m));
                    Find.WindowStack.Add(new FloatMenu(opts));
                }
                y += 34f;

                // Chance
                if (tempSelectionMode != ApparelSelectionMode.AlwaysTake)
                {
                    Widgets.Label(new Rect(inRect.x, y, 100f, 28f), LanguageManager.Get("Chance") + ":");
                    tempSelectionChance = Widgets.HorizontalSlider(new Rect(inRect.x + 105f, y + 4f, inRect.width - 105f, 20f), tempSelectionChance, 0f, 1f, true, tempSelectionChance.ToString("P0"));
                    y += 34f;
                }

                // Buttons
                y += 10f;
                float btnW = (inRect.width - 10f) / 2f;
                if (Widgets.ButtonText(new Rect(inRect.x, y, btnW, 32f), LanguageManager.Get("Cancel")))
                    Close();
                if (Widgets.ButtonText(new Rect(inRect.x + btnW + 10f, y, btnW, 32f), LanguageManager.Get("OK")))
                {
                    item.PoolType = tempPoolType;
                    item.CountRange = tempCountRange;
                    item.SelectionMode = tempSelectionMode;
                    item.SelectionChance = tempSelectionChance;
                    FactionGearEditor.MarkDirty();
                    Close();
                }
            }
        }
    }
}
