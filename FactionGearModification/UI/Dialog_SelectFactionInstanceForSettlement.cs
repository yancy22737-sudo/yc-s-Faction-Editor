using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.Managers;

namespace FactionGearCustomizer.UI
{
    public class Dialog_SelectFactionInstanceForSettlement : Window
    {
        private readonly FactionDef factionDef;
        private Vector2 scrollPosition;
        private List<Faction> instances = new List<Faction>();
        private Faction selected;

        public override Vector2 InitialSize => new Vector2(520f, 520f);

        public Dialog_SelectFactionInstanceForSettlement(FactionDef factionDef)
        {
            this.factionDef = factionDef;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            RefreshInstances();
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (Current.Game == null || Find.FactionManager == null)
            {
                Widgets.Label(inRect, LanguageManager.Get("OnlyAvailableInGame"));
                return;
            }

            float y = 0f;
            float width = inRect.width;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, width, 35f), LanguageManager.Get("SelectFactionInstanceTitle", factionDef.LabelCap));
            Text.Font = GameFont.Small;
            y += 40f;

            Widgets.Label(new Rect(0f, y, width, 24f), $"{LanguageManager.Get("InstanceCount")}: {instances.Count}");
            y += 30f;

            float listHeight = Mathf.Max(100f, inRect.height - y - 90f);
            Rect outRect = new Rect(0f, y, width, listHeight);
            float viewHeight = Mathf.Max(outRect.height, instances.Count * 32f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float rowY = 0f;
            for (int i = 0; i < instances.Count; i++)
            {
                Faction fac = instances[i];
                Rect rowRect = new Rect(0f, rowY, viewRect.width, 32f);
                if (Mouse.IsOver(rowRect)) Widgets.DrawHighlight(rowRect);
                if (selected == fac) Widgets.DrawHighlightSelected(rowRect);

                if (Widgets.ButtonInvisible(rowRect))
                {
                    selected = fac;
                }

                Rect labelRect = rowRect.ContractedBy(6f, 0f);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, fac?.Name ?? "???");
                Text.Anchor = TextAnchor.UpperLeft;

                rowY += 32f;
            }
            Widgets.EndScrollView();

            y += listHeight + 10f;

            if (instances.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(0f, y, width, 40f), LanguageManager.Get("NoFactionInstancesFound"));
                GUI.color = Color.white;
                y += 45f;
            }

            float buttonY = inRect.height - 40f;
            float totalButtonWidth = 160f * 3 + 20f;
            float startX = (inRect.width - totalButtonWidth) / 2f;
            float btnWidth = 160f;
            Rect createRect = new Rect(startX, buttonY, btnWidth, 40f);
            if (Widgets.ButtonText(createRect, LanguageManager.Get("CreateNewInstance")))
            {
                TryCreateInstance();
            }

            Rect toolsRect = new Rect(createRect.xMax + 10f, buttonY, btnWidth, 40f);
            if (Widgets.ButtonText(toolsRect, LanguageManager.Get("SpawnSettlements"), true, true, selected != null))
            {
                Find.WindowStack.Add(new Dialog_SpawnSettlements(selected));
                Close();
            }

            Rect selectRect = new Rect(toolsRect.xMax + 10f, buttonY, btnWidth, 40f);
            if (Widgets.ButtonText(selectRect, LanguageManager.Get("SelectOnWorldMap"), true, true, selected != null))
            {
                Faction factionToSpawn = selected;
                Close();

                // Delay opening to avoid window stack conflicts
                if (factionToSpawn != null)
                {
                    Verse.LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        if (Current.Game != null && Find.World != null)
                        {
                            Find.WindowStack.Add(new FactionSpawnWindow(factionToSpawn));
                        }
                    });
                }
            }

            if (selected == null && instances.Count > 0)
            {
                TooltipHandler.TipRegion(toolsRect, LanguageManager.Get("SelectInstanceToContinue"));
                TooltipHandler.TipRegion(selectRect, LanguageManager.Get("SelectInstanceToContinue"));
            }
        }

        private void RefreshInstances()
        {
            if (Find.FactionManager == null)
            {
                instances = new List<Faction>();
                selected = null;
                return;
            }

            instances = Find.FactionManager.AllFactions
                .Where(f => f != null && f.def == factionDef && !f.IsPlayer)
                .OrderBy(f => f.Name)
                .ToList();

            if (selected == null || !instances.Contains(selected))
            {
                selected = instances.FirstOrDefault();
            }
        }

        private void TryCreateInstance()
        {
            if (factionDef == null) return;
            if (Find.FactionManager == null) return;

            int instanceCount = Find.FactionManager.AllFactions.Count(f => f.def == factionDef && !f.IsPlayer);
            if (instanceCount >= 20)
            {
                Messages.Message(LanguageManager.Get("TooManyInstances"), MessageTypeDefOf.RejectInput, false);
                return;
            }

            string nextName = NameGenerator.GenerateName(factionDef.factionNameMaker,
                from fac in Find.FactionManager.AllFactionsVisible
                select fac.Name, false, null);

            Find.WindowStack.Add(new Dialog_MessageBox(
                LanguageManager.Get("ConfirmCreateNewInstance") + "\n\n" +
                LanguageManager.Get("NewInstancePreview", nextName),
                LanguageManager.Get("Confirm"),
                () =>
                {
                    Faction newFaction = FactionSpawnManager.SpawnFactionInstance(factionDef, nextName);
                    RefreshInstances();
                    if (newFaction != null) selected = newFaction;
                },
                LanguageManager.Get("Cancel"),
                null,
                LanguageManager.Get("CreateNewInstance"),
                true,
                null,
                null
            ));
        }
    }
}

