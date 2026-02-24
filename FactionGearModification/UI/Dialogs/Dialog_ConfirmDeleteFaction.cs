using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer.UI.Panels;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_ConfirmDeleteFaction : Window
    {
        private readonly Faction targetFaction;
        private readonly Action onConfirm;
        private readonly List<Settlement> settlements = new List<Settlement>();
        private readonly List<Pawn> relatedPawns = new List<Pawn>();
        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(550f, 450f);

        public Dialog_ConfirmDeleteFaction(Faction faction, Action onConfirm)
        {
            this.targetFaction = faction ?? throw new ArgumentNullException(nameof(faction));
            this.onConfirm = onConfirm;
            this.doCloseX = true;
            this.forcePause = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;

            CollectRelatedData();
        }

        private void CollectRelatedData()
        {
            if (targetFaction == null) return;

            // 收集派系的所有据点
            if (Find.WorldObjects != null)
            {
                settlements.AddRange(Find.WorldObjects.Settlements.Where(s => s.Faction == targetFaction));
            }

            // 收集地图上的相关角色（该派系的成员）
            if (Find.Maps != null)
            {
                foreach (var map in Find.Maps)
                {
                    if (map.mapPawns != null)
                    {
                        relatedPawns.AddRange(map.mapPawns.AllPawns.Where(p => p.Faction == targetFaction));
                    }
                }
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 35f), LanguageManager.Get("DeleteFactionTitle"));
            Text.Font = GameFont.Small;

            float curY = 40f;

            // 警告信息
            GUI.color = Color.red;
            Widgets.Label(new Rect(0, curY, inRect.width, 25f), LanguageManager.Get("DeleteFactionWarning"));
            GUI.color = Color.white;
            curY += 30f;

            // 派系名称
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0, curY, inRect.width, 25f), 
                $"{LanguageManager.Get("TargetFaction")}: <b>{targetFaction.Name}</b> ({targetFaction.def.LabelCap})");
            curY += 30f;

            // 影响内容列表
            Rect listRect = new Rect(0, curY, inRect.width, inRect.height - curY - 50f);
            Widgets.DrawMenuSection(listRect);
            Rect innerRect = listRect.ContractedBy(10f);

            float contentHeight = CalculateContentHeight(innerRect.width);
            Rect viewRect = new Rect(0, 0, innerRect.width - 16f, contentHeight);

            Widgets.BeginScrollView(innerRect, ref scrollPosition, viewRect);
            float y = 0f;

            // 据点列表
            y = DrawSectionHeader(y, viewRect.width, LanguageManager.Get("SettlementsToRemove"), settlements.Count);
            foreach (var settlement in settlements)
            {
                y = DrawListItem(y, viewRect.width, $"  • {settlement.Name}", settlement.Tile.ToString());
            }
            y += 10f;

            // 角色列表
            y = DrawSectionHeader(y, viewRect.width, LanguageManager.Get("PawnsToRemove"), relatedPawns.Count);
            foreach (var pawn in relatedPawns)
            {
                string location = pawn.Map != null ? pawn.Map.Parent.Label : LanguageManager.Get("UnknownLocation");
                y = DrawListItem(y, viewRect.width, $"  • {pawn.LabelCap}", location);
            }
            y += 10f;

            // 其他影响
            y = DrawSectionHeader(y, viewRect.width, LanguageManager.Get("OtherEffects"), 0);
            y = DrawListItem(y, viewRect.width, $"  • {LanguageManager.Get("FactionRelationsRemoved")}", "");
            y = DrawListItem(y, viewRect.width, $"  • {LanguageManager.Get("ActiveQuestsAffected")}", "");

            Widgets.EndScrollView();

            // 底部按钮
            float btnY = inRect.height - 40f;
            float btnWidth = 120f;

            GUI.color = Color.red;
            if (Widgets.ButtonText(new Rect(inRect.width / 2f - btnWidth - 10f, btnY, btnWidth, 35f), LanguageManager.Get("DeleteFaction")))
            {
                ExecuteDelete();
            }
            GUI.color = Color.white;

            if (Widgets.ButtonText(new Rect(inRect.width / 2f + 10f, btnY, btnWidth, 35f), LanguageManager.Get("Cancel")))
            {
                Close();
            }
        }

        private float CalculateContentHeight(float width)
        {
            float height = 0f;
            height += 25f; // Settlements header
            height += settlements.Count * 22f;
            height += 10f;
            height += 25f; // Pawns header
            height += relatedPawns.Count * 22f;
            height += 10f;
            height += 25f; // Other effects header
            height += 3 * 22f; // 3 other effect items
            return Mathf.Max(height, 100f);
        }

        private float DrawSectionHeader(float y, float width, string label, int count)
        {
            Rect rect = new Rect(0, y, width, 25f);
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Widgets.DrawLightHighlight(rect);
            GUI.color = Color.white;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            string text = count > 0 ? $"{label} ({count})" : label;
            Widgets.Label(rect.ContractedBy(5f, 0), $"<b>{text}</b>");
            Text.Anchor = TextAnchor.UpperLeft;
            return y + 25f;
        }

        private float DrawListItem(float y, float width, string label, string detail)
        {
            Rect rect = new Rect(0, y, width, 22f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect, label);

            if (!string.IsNullOrEmpty(detail))
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(rect, detail);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Small;
            return y + 22f;
        }

        private void ExecuteDelete()
        {
            try
            {
                FactionRemovalManager.RemoveFaction(targetFaction);
                Messages.Message(LanguageManager.Get("FactionDeleted", targetFaction.Name), MessageTypeDefOf.NeutralEvent, false);
                onConfirm?.Invoke();
                Close();
            }
            catch (Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] Failed to delete faction: {ex}");
                Messages.Message(LanguageManager.Get("FactionDeleteFailed"), MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
