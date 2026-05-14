using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Panels
{
    public static class SkillCardUI
    {
        private const float RowHeight = 28f;
        private const float BarWidth = 110f;

        private static Texture2D _barFillTex;
        private static Texture2D BarFillTex
        {
            get { if (_barFillTex == null) _barFillTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.25f, 0.55f, 0.9f)); return _barFillTex; }
        }

        public static float GetCardHeight(ForcedSkill item) => RowHeight + 4f;

        public static void Draw(Listing_Standard ui, ForcedSkill item, int index, System.Action onEdit)
        {
            Rect row = ui.GetRect(RowHeight);
            if (index % 2 == 1) Widgets.DrawLightHighlight(row);

            float x = row.x;

            // Enable toggle
            bool en = item.enabled;
            Widgets.Checkbox(new Vector2(x + 2f, row.y + 4f), ref en, 20f, false, true);
            if (en != item.enabled)
            {
                item.enabled = en;
                FactionGearEditor.MarkDirty();
            }
            x += 24f;

            // Passion icon (pawn-editor style)
            Texture2D pi = BaseContent.GreyTex;
            if (item.passion == Passion.Major && SkillUI.PassionMajorIcon != null) pi = SkillUI.PassionMajorIcon;
            else if (item.passion == Passion.Minor && SkillUI.PassionMinorIcon != null) pi = SkillUI.PassionMinorIcon;
            if (Widgets.ButtonImage(new Rect(x + 2f, row.y + 4f, 20f, 20f), pi))
            {
                if (item.passion == Passion.None) item.passion = Passion.Minor;
                else if (item.passion == Passion.Minor) item.passion = Passion.Major;
                else item.passion = Passion.None;
                FactionGearEditor.MarkDirty();
            }
            x += 26f;

            // Skill name
            string nm = "???";
            if (item.SkillDef != null) nm = item.SkillDef.LabelCap.ToString();
            else if (item.skillDefName != null) nm = item.skillDefName;
            Widgets.Label(new Rect(x, row.y, 72f, RowHeight), nm);
            x += 76f;

            // Minus
            if (Widgets.ButtonText(new Rect(x, row.y + 4f, 18f, 20f), "-"))
            {
                item.level = Mathf.Max(0, item.level - 1);
                item.enabled = true;
                item.minLevel = Mathf.Min(item.minLevel, item.level);
                item.maxLevel = Mathf.Max(item.maxLevel, item.level);
                FactionGearEditor.MarkDirty();
            }
            x += 20f;

            // FillableBar (pawn-editor style: no 0/20 labels, just filled bar + level number)
            Rect barRect = new Rect(x, row.y + 4f, BarWidth, RowHeight - 8f);
            float fill = Mathf.Clamp01(item.level / 20f);
            if (fill < 0.01f) fill = 0.01f;
            Widgets.FillableBar(barRect, fill, BarFillTex, null, false);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, item.level.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            x += BarWidth + 2f;

            // Plus
            if (Widgets.ButtonText(new Rect(x, row.y + 4f, 18f, 20f), "+"))
            {
                item.level = Mathf.Min(20, item.level + 1);
                item.enabled = true;
                item.minLevel = Mathf.Min(item.minLevel, item.level);
                item.maxLevel = Mathf.Max(item.maxLevel, item.level);
                FactionGearEditor.MarkDirty();
            }
            x += 24f;

            // Edit "..."
            if (Widgets.ButtonText(new Rect(x, row.y + 2f, 36f, RowHeight - 4f), "..."))
                onEdit?.Invoke();

            TooltipHandler.TipRegion(row, nm + " Lv" + item.level
                + (item.minLevel != item.maxLevel ? "\nRange: " + item.minLevel + "-" + item.maxLevel : "")
                + (item.passion != Passion.None ? "\nPassion: " + item.passion : "")
                + (item.chance < 1f ? "\nChance: " + (item.chance * 100f).ToString("F0") + "%" : ""));
        }
    }
}
