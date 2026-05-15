using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_TradeStockPreview : Window
    {
        private readonly List<TradeStockEntry> entries;
        private readonly string factionName;
        private Vector2 scrollPos;
        private List<(TradeStockEntry entry, Thing thing, int count)> previewItems;

        public override Vector2 InitialSize => new Vector2(500f, 500f);

        public Dialog_TradeStockPreview(List<TradeStockEntry> entries, string factionName)
        {
            this.entries = entries;
            this.factionName = factionName;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            draggable = true;

            GeneratePreview();
        }

        private void GeneratePreview()
        {
            previewItems = new List<(TradeStockEntry, Thing, int)>();
            foreach (var entry in entries)
            {
                var thing = entry.CreateThing();
                if (thing == null) continue;
                int count = entry.countRange.RandomInRange;
                count = Mathf.Min(count, thing.def.stackLimit);
                previewItems.Add((entry, thing, count));
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, inRect.width, 35f),
                $"Trade Stock Preview — {factionName}");
            Text.Font = GameFont.Small;
            y += 40f;

            Widgets.Label(new Rect(0f, y, inRect.width, 24f),
                $"Preview shows {previewItems.Count} items (one random generation).");
            y += 28f;

            float totalValue = 0f;
            foreach (var item in previewItems)
                totalValue += item.thing.MarketValue * item.count;

            Widgets.Label(new Rect(0f, y, inRect.width, 24f),
                $"Total market value: ${totalValue:N0}");
            y += 30f;

            Widgets.DrawLineHorizontal(0f, y, inRect.width);
            y += 8f;

            float contentHeight = previewItems.Count * 48f + 20f;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, contentHeight);
            Widgets.BeginScrollView(new Rect(0f, y, inRect.width, inRect.height - y - 50f), ref scrollPos, viewRect);

            float curY = 0f;
            foreach (var (entry, thing, count) in previewItems)
            {
                Rect row = new Rect(0f, curY, viewRect.width, 44f);
                if (curY % 96f < 48f) Widgets.DrawAltRect(row);

                Rect iconRect = new Rect(row.x + 4f, row.y + 6f, 32f, 32f);
                Widgets.ThingIcon(iconRect, thing.def, thing.Stuff);

                Rect nameRect = new Rect(row.x + 42f, row.y + 2f, 200f, 20f);
                Widgets.Label(nameRect, thing.LabelCap);

                Rect detailRect = new Rect(row.x + 42f, row.y + 22f, 300f, 18f);
                GUI.color = Color.gray;
                Widgets.Label(detailRect, $"Q:{entry.quality}  x{count}  ${thing.MarketValue * count:N0}");
                GUI.color = Color.white;

                curY += 48f;
            }

            if (previewItems.Count == 0)
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "No items configured. Add items to see preview.");
            }

            Widgets.EndScrollView();

            float btnWidth = 100f;
            float btnHeight = 36f;
            if (Widgets.ButtonText(new Rect((inRect.width - btnWidth) / 2f, inRect.height - btnHeight, btnWidth, btnHeight), "Close"))
            {
                Close();
            }
        }
    }
}
