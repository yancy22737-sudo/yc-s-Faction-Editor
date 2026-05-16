using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Dialogs
{
    // Stable trait detail popup inspired by pawn-editor style:
    // direct translated label + description + degree list.
    public class Dialog_TraitInfoCard : Window
    {
        private readonly TraitDef trait;

        public override Vector2 InitialSize => new Vector2(480f, 320f);

        public Dialog_TraitInfoCard(TraitDef trait)
        {
            this.trait = trait;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            draggable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (trait == null)
            {
                Close();
                return;
            }

            try
            {
                float y = 0f;
                Text.Font = GameFont.Medium;
                string label = "???";
                try { label = trait.LabelCap.ToString(); } catch { label = trait.defName ?? "???"; }
                Widgets.Label(new Rect(0f, y, inRect.width, 32f), label);
                Text.Font = GameFont.Small;
                y += 36f;

                string desc = "";
                try { desc = trait.description ?? ""; } catch { }
                float descH = Text.CalcHeight(desc, inRect.width - 8f);
                Widgets.Label(new Rect(4f, y, inRect.width - 8f, descH), desc);
                y += descH + 12f;

                if (trait.degreeDatas != null && trait.degreeDatas.Count > 1)
                {
                    Widgets.Label(new Rect(4f, y, inRect.width - 8f, 24f), "<b>" + "Degrees" + ":</b>");
                    y += 26f;
                    for (int i = 0; i < trait.degreeDatas.Count; i++)
                    {
                        try
                        {
                            string dl = trait.degreeDatas[i].LabelCap.ToString();
                            if (string.IsNullOrEmpty(dl)) dl = trait.degreeDatas[i].label ?? ("Lv" + i);
                            string dd = trait.degreeDatas[i].description ?? "";
                            string line = dl + (string.IsNullOrEmpty(dd) ? "" : " — " + dd);
                            float lh = Text.CalcHeight(line, inRect.width - 16f);
                            Widgets.Label(new Rect(8f, y, inRect.width - 16f, lh), line);
                            y += lh + 4f;
                        }
                        catch { y += 20f; }
                    }
                }
            }
            catch { }

            Rect closeRect = new Rect(inRect.width / 2f - 60f, inRect.height - 34f, 120f, 28f);
            if (Widgets.ButtonText(closeRect, "Close"))
                Close();
        }
    }
}
