using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.Managers;

namespace FactionGearCustomizer.UI
{
    /// <summary>
    /// Flags indicating which gear categories to copy during batch apply.
    /// </summary>
    [System.Flags]
    public enum GearCopyFlags
    {
        None = 0,
        Ranged = 1 << 0,
        Melee = 1 << 1,
        Armors = 1 << 2,
        Clothes = 1 << 3,
        Others = 1 << 4,
        Hediffs = 1 << 5,
        Items = 1 << 6,
        General = 1 << 7,
        All = Ranged | Melee | Armors | Clothes | Others | Hediffs | Items | General
    }

    public class Dialog_BatchApply : Window
    {
        // ── Source ──────────────────────────────────────────────
        private readonly FactionDef sourceFaction;
        private readonly KindGearData sourceData;

        // ── Target faction selector ───────────────────────────
        private FactionDef targetFaction;
        private List<FactionDef> allHumanFactions;

        // ── Pawn kind list ────────────────────────────────────
        private List<PawnKindDef> allKinds;
        private readonly HashSet<PawnKindDef> selectedKinds = new HashSet<PawnKindDef>();
        private string searchText = "";
        private Vector2 scrollPos;

        // ── Copy flags ────────────────────────────────────────
        private GearCopyFlags copyFlags = GearCopyFlags.All;

        public override Vector2 InitialSize => new Vector2(520f, 780f);

        // ── Constructor ───────────────────────────────────────
        public Dialog_BatchApply(FactionDef faction, KindGearData source)
        {
            this.sourceFaction = faction;
            this.sourceData = source;
            this.targetFaction = faction;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;

            allHumanFactions = DefDatabase<FactionDef>.AllDefs
                .Where(f => f.humanlikeFaction)
                .OrderBy(f => f.LabelCap.ToString())
                .ToList();

            RefreshKindList();
        }

        // ── Helpers ───────────────────────────────────────────
        private void RefreshKindList()
        {
            allKinds = FactionGearEditor.GetFactionKinds(targetFaction);
            selectedKinds.Clear();
            scrollPos = Vector2.zero;
        }

        private IEnumerable<PawnKindDef> GetFilteredKinds()
        {
            if (string.IsNullOrEmpty(searchText)) return allKinds;
            string term = searchText.ToLower();
            return allKinds.Where(k =>
                (k.label ?? "").ToLower().Contains(term) ||
                k.defName.ToLower().Contains(term));
        }

        // ── Draw ──────────────────────────────────────────────
        public override void DoWindowContents(Rect inRect)
        {
            // Ctrl+S shortcut
            if (Event.current.type == EventType.KeyDown && Event.current.control &&
                Event.current.keyCode == KeyCode.S)
            {
                TryApplyWithConfirmation();
                Event.current.Use();
            }

            float y = inRect.y;

            // ── Title ──────────────────────────────────────────
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 30f),
                LanguageManager.Get("BatchApplyTitle"));
            Text.Font = GameFont.Small;
            y += 34f;

            // ── Source info label ──────────────────────────────
            GUI.color = Color.gray;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                $"{LanguageManager.Get("BatchApplySource")}: [{sourceFaction.LabelCap}] {sourceData.kindDefName}");
            GUI.color = Color.white;
            y += 26f;

            // ── Copy flags (checkboxes) ────────────────────────
            DrawCopyFlagRow(inRect.x, ref y, inRect.width);
            y += 6f;

            // ── Target faction selector ────────────────────────
            DrawFactionSelector(inRect.x, ref y, inRect.width);
            y += 6f;

            // ── Search ────────────────────────────────────────
            Rect searchRect = new Rect(inRect.x, y, inRect.width, 24f);
            string oldSearch = searchText;
            searchText = Widgets.TextField(searchRect, searchText);
            if (searchText != oldSearch) scrollPos = Vector2.zero;
            y += 30f;

            // ── Select All / None ─────────────────────────────
            Rect btnRow = new Rect(inRect.x, y, inRect.width, 24f);
            if (Widgets.ButtonText(new Rect(btnRow.x, btnRow.y, 100f, 24f),
                    LanguageManager.Get("SelectAll")))
            {
                selectedKinds.Clear();
                foreach (var k in GetFilteredKinds()) selectedKinds.Add(k);
            }
            if (Widgets.ButtonText(new Rect(btnRow.x + 110f, btnRow.y, 100f, 24f),
                    LanguageManager.Get("SelectNone")))
            {
                selectedKinds.Clear();
            }

            // Selected count (right-aligned)
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(btnRow.xMax - 200f, btnRow.y, 200f, 24f),
                $"{LanguageManager.Get("Selected")}: {selectedKinds.Count}");
            Text.Anchor = TextAnchor.UpperLeft;
            y += 30f;

            // ── Kind list ─────────────────────────────────────
            Rect listRect = new Rect(inRect.x, y, inRect.width, inRect.height - y - 40f);
            var filtered = GetFilteredKinds().ToList();
            const float MaxViewRectHeight = 100000f;
            float totalContentHeight = filtered.Count * 28f;
            float clampedViewHeight = Mathf.Min(totalContentHeight, MaxViewRectHeight);
            Rect viewRect = new Rect(0, 0, listRect.width - 16f, clampedViewHeight);

            Widgets.BeginScrollView(listRect, ref scrollPos, viewRect);
            float curY = 0f;
            foreach (var kind in filtered)
            {
                Rect rowRect = new Rect(0, curY, viewRect.width, 24f);
                if ((int)(curY / 28f) % 2 == 1) Widgets.DrawAltRect(rowRect);

                bool selected = selectedKinds.Contains(kind);
                bool oldSelected = selected;

                // Shade source kind
                bool isSelf = kind.defName == sourceData.kindDefName &&
                              targetFaction.defName == sourceFaction.defName;
                if (isSelf) GUI.color = new Color(1f, 1f, 0.5f, 0.7f);

                Widgets.CheckboxLabeled(rowRect, kind.LabelCap, ref selected);

                if (isSelf) GUI.color = Color.white;

                if (selected != oldSelected)
                {
                    if (selected) selectedKinds.Add(kind);
                    else selectedKinds.Remove(kind);
                }
                curY += 28f;
            }
            Widgets.EndScrollView();

            // ── Apply button ──────────────────────────────────
            Rect applyRect = new Rect(inRect.x, inRect.height - 30f, inRect.width - 100f, 30f);
            Rect historyRect = new Rect(inRect.xMax - 90f, inRect.height - 30f, 90f, 30f);

            if (Widgets.ButtonText(applyRect, LanguageManager.Get("Apply")))
            {
                TryApplyWithConfirmation();
            }
            if (Widgets.ButtonText(historyRect, LanguageManager.Get("History")))
            {
                Close();
                Find.WindowStack.Add(new Dialog_BatchHistory());
            }
        }

        // ── Draw helpers ──────────────────────────────────────

        private void DrawCopyFlagRow(float x, ref float y, float width)
        {
            // Label
            Widgets.Label(new Rect(x, y, 130f, 22f), LanguageManager.Get("BatchCopyCategories") + ":");
            y += 24f;

            // 7 checkboxes in two rows
            (GearCopyFlags flag, string labelKey)[] flags =
            {
                (GearCopyFlags.Ranged,  "Ranged"),
                (GearCopyFlags.Melee,   "Melee"),
                (GearCopyFlags.Armors,  "Armors"),
                (GearCopyFlags.Clothes, "Clothes"),
                (GearCopyFlags.Others,  "Others"),
                (GearCopyFlags.Hediffs, "Hediffs"),
                (GearCopyFlags.Items,   "Items"),
                (GearCopyFlags.General, "General"),
            };

            float colW = width / 4f;
            float rowH = 24f;
            int col = 0;
            float startY = y;

            foreach (var (flag, labelKey) in flags)
            {
                int row = col / 4;
                int colInRow = col % 4;
                float cx = x + colInRow * colW;
                float cy = startY + row * rowH;

                Rect cbRect = new Rect(cx, cy, colW - 4f, 22f);
                bool val = (copyFlags & flag) != 0;
                bool oldVal = val;
                Widgets.CheckboxLabeled(cbRect, LanguageManager.Get(labelKey), ref val);
                if (val != oldVal)
                    copyFlags = val ? (copyFlags | flag) : (copyFlags & ~flag);

                col++;
            }

            // Advance y by 2 rows
            y = startY + (flags.Length > 4 ? rowH * 2 : rowH) + 2f;

            // "All" / "None" quick buttons
            float btnW = 54f;
            if (Widgets.ButtonText(new Rect(x, y, btnW, 20f), LanguageManager.Get("All")))
                copyFlags = GearCopyFlags.All;
            if (Widgets.ButtonText(new Rect(x + btnW + 4f, y, btnW, 20f), LanguageManager.Get("None")))
                copyFlags = GearCopyFlags.None;
            y += 26f;
        }

        private void DrawFactionSelector(float x, ref float y, float width)
        {
            float labelW = 120f;
            float buttonW = width - labelW - 4f;

            Widgets.Label(new Rect(x, y, labelW, 24f),
                LanguageManager.Get("BatchTargetFaction") + ":");

            string factionLabel = targetFaction?.LabelCap.ToString() ?? "?";
            if (Widgets.ButtonText(new Rect(x + labelW + 4f, y, buttonW, 24f), factionLabel))
            {
                var options = allHumanFactions.Select(f =>
                    new FloatMenuOption(f.LabelCap, () =>
                    {
                        if (targetFaction != f)
                        {
                            targetFaction = f;
                            RefreshKindList();
                        }
                    })
                ).ToList();
                Find.WindowStack.Add(new FloatMenu(options));
            }
            y += 30f;
        }

        // ── Apply logic ───────────────────────────────────────

        private void TryApplyWithConfirmation()
        {
            if (selectedKinds.Count > 0)
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    string.Format(LanguageManager.Get("BatchApplyConfirm"), selectedKinds.Count),
                    LanguageManager.Get("Yes"),
                    () => { Apply(); Close(); },
                    LanguageManager.Get("No"),
                    null, null, false, null, null));
            }
            else
            {
                Close();
            }
        }

        private void Apply()
        {
            if (copyFlags == GearCopyFlags.None)
            {
                Messages.Message(LanguageManager.Get("BatchApplyNoCategorySelected"),
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

            var targetFactionData =
                FactionGearCustomizerMod.Settings.GetOrCreateFactionData(targetFaction.defName);

            // Collect targets (skip self)
            var targets = new List<KindGearData>();
            foreach (var kind in selectedKinds)
            {
                if (kind.defName == sourceData.kindDefName &&
                    targetFaction.defName == sourceFaction.defName)
                    continue;

                var target = targetFactionData.GetOrCreateKindData(kind.defName);
                if (target != null) targets.Add(target);
            }

            if (targets.Count == 0) { Close(); return; }

            // Take snapshots before modifying
            var preApplySnapshots = new List<KindGearData>(targets.Count);
            foreach (var target in targets)
            {
                preApplySnapshots.Add(target.DeepCopy());
            }

            // Partial copy
            foreach (var target in targets)
            {
                CopyPartial(sourceData, target, copyFlags);
                target.isModified = true;
            }

            // Record to BatchHistory instead of UndoManager
            var record = new BatchApplyRecord
            {
                AppliedAt = DateTime.Now,
                SourceKind = sourceData.kindDefName,
                SourceFaction = sourceFaction.LabelCap,
                Flags = copyFlags,
                TargetKinds = targets.Select(x => x.kindDefName).ToList(),
                PreApplySnapshots = preApplySnapshots,
                TargetRefs = targets
            };
            BatchHistoryManager.Record(record);

            FactionGearEditor.MarkDirty();
            Messages.Message(
                string.Format(LanguageManager.Get("BatchApplied"), targets.Count),
                MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>
        /// Copies only the data categories indicated by <paramref name="flags"/> from
        /// <paramref name="src"/> into <paramref name="dst"/>, leaving all other fields
        /// untouched.
        /// </summary>
        private static void CopyPartial(KindGearData src, KindGearData dst, GearCopyFlags flags)
        {
            if (ReferenceEquals(src, dst)) return;

            // ── Ranged weapons ────────────────────────────────
            if ((flags & GearCopyFlags.Ranged) != 0)
            {
                dst.weapons = src.weapons
                    ?.Select(g => g != null ? new GearItem(g.thingDefName, g.weight) : null)
                    .Where(g => g != null).ToList()
                    ?? new List<GearItem>();

                // SpecificWeapons that are ranged
                var srcRanged = src.SpecificWeapons?.Where(
                    x => x.Thing != null && x.Thing.IsRangedWeapon).ToList();
                if (srcRanged != null && srcRanged.Count > 0)
                {
                    if (dst.SpecificWeapons == null)
                        dst.SpecificWeapons = new List<SpecRequirementEdit>();
                    else
                        dst.SpecificWeapons.RemoveAll(x => x.Thing != null && x.Thing.IsRangedWeapon);

                    foreach (var item in srcRanged)
                        dst.SpecificWeapons.Add(CloneSpecReq(item));
                }
                else if (dst.SpecificWeapons != null)
                {
                    dst.SpecificWeapons.RemoveAll(x => x.Thing != null && x.Thing.IsRangedWeapon);
                    if (dst.SpecificWeapons.Count == 0) dst.SpecificWeapons = null;
                }
            }

            // ── Melee weapons ─────────────────────────────────
            if ((flags & GearCopyFlags.Melee) != 0)
            {
                dst.meleeWeapons = src.meleeWeapons
                    ?.Select(g => g != null ? new GearItem(g.thingDefName, g.weight) : null)
                    .Where(g => g != null).ToList()
                    ?? new List<GearItem>();

                var srcMelee = src.SpecificWeapons?.Where(
                    x => x.Thing != null && x.Thing.IsMeleeWeapon).ToList();
                if (srcMelee != null && srcMelee.Count > 0)
                {
                    if (dst.SpecificWeapons == null)
                        dst.SpecificWeapons = new List<SpecRequirementEdit>();
                    else
                        dst.SpecificWeapons.RemoveAll(x => x.Thing != null && x.Thing.IsMeleeWeapon);

                    foreach (var item in srcMelee)
                        dst.SpecificWeapons.Add(CloneSpecReq(item));
                }
                else if (dst.SpecificWeapons != null)
                {
                    dst.SpecificWeapons.RemoveAll(x => x.Thing != null && x.Thing.IsMeleeWeapon);
                    if (dst.SpecificWeapons.Count == 0) dst.SpecificWeapons = null;
                }
            }

            // ── Armor ─────────────────────────────────────────
            if ((flags & GearCopyFlags.Armors) != 0)
            {
                dst.armors = src.armors
                    ?.Select(g => g != null ? new GearItem(g.thingDefName, g.weight) : null)
                    .Where(g => g != null).ToList()
                    ?? new List<GearItem>();

                CopySpecificApparelByPredicate(src, dst,
                    x => x.Thing != null && IsArmor(x.Thing));
            }

            // ── Clothes / Apparel ─────────────────────────────
            if ((flags & GearCopyFlags.Clothes) != 0)
            {
                dst.apparel = src.apparel
                    ?.Select(g => g != null ? new GearItem(g.thingDefName, g.weight) : null)
                    .Where(g => g != null).ToList()
                    ?? new List<GearItem>();

                CopySpecificApparelByPredicate(src, dst,
                    x => x.Thing != null && IsApparel(x.Thing));
            }

            // ── Others (belts, etc.) ──────────────────────────
            if ((flags & GearCopyFlags.Others) != 0)
            {
                dst.others = src.others
                    ?.Select(g => g != null ? new GearItem(g.thingDefName, g.weight) : null)
                    .Where(g => g != null).ToList()
                    ?? new List<GearItem>();

                CopySpecificApparelByPredicate(src, dst,
                    x => x.Thing != null && IsBelt(x.Thing));
            }

            // ── Forced Hediffs ────────────────────────────────
            if ((flags & GearCopyFlags.Hediffs) != 0)
            {
                if (src.ForcedHediffs != null)
                {
                    dst.ForcedHediffs = new List<ForcedHediff>();
                    foreach (var item in src.ForcedHediffs)
                    {
                        if (item == null) continue;
                        var n = new ForcedHediff
                        {
                            HediffDef = item.HediffDef,
                            PoolType = item.PoolType,
                            maxParts = item.maxParts,
                            maxPartsRange = item.maxPartsRange,
                            chance = item.chance,
                            severityRange = item.severityRange
                        };
                        if (item.parts != null)
                            n.parts = new List<BodyPartDef>(item.parts);
                        dst.ForcedHediffs.Add(n);
                    }
                }
                else
                {
                    dst.ForcedHediffs = null;
                }
            }

            // ── Inventory Items ───────────────────────────────
            if ((flags & GearCopyFlags.Items) != 0)
            {
                if (src.InventoryItems != null)
                {
                    dst.InventoryItems = new List<SpecRequirementEdit>();
                    foreach (var item in src.InventoryItems)
                    {
                        if (item != null)
                            dst.InventoryItems.Add(CloneSpecReq(item));
                    }
                }
                else
                {
                    dst.InventoryItems = null;
                }
            }

            // ── General/Advanced ──────────────────────────────
            if ((flags & GearCopyFlags.General) != 0)
            {
                dst.ForceNaked = src.ForceNaked;
                dst.ForceOnlySelected = src.ForceOnlySelected;
                dst.OutfitFirstBudgetStrategy = src.OutfitFirstBudgetStrategy;
                dst.ForceIgnoreRestrictions = src.ForceIgnoreRestrictions;
                dst.ItemQuality = src.ItemQuality;
                dst.ForcedWeaponQuality = src.ForcedWeaponQuality;
                dst.BiocodeWeaponChance = src.BiocodeWeaponChance;
                dst.BiocodeApparelChance = src.BiocodeApparelChance;
                dst.TechHediffChance = src.TechHediffChance;
                dst.TechHediffsMaxAmount = src.TechHediffsMaxAmount;
                dst.ApparelMoney = src.ApparelMoney;
                dst.WeaponMoney = src.WeaponMoney;
                dst.TechLevelLimit = src.TechLevelLimit;
                dst.ApparelColor = src.ApparelColor;

                dst.TechHediffTags = src.TechHediffTags == null ? null : new List<string>(src.TechHediffTags);
                dst.TechHediffDisallowedTags = src.TechHediffDisallowedTags == null ? null : new List<string>(src.TechHediffDisallowedTags);
                dst.WeaponTags = src.WeaponTags == null ? null : new List<string>(src.WeaponTags);
                dst.ApparelTags = src.ApparelTags == null ? null : new List<string>(src.ApparelTags);
                dst.ApparelDisallowedTags = src.ApparelDisallowedTags == null ? null : new List<string>(src.ApparelDisallowedTags);

                if (src.ApparelRequired != null)
                {
                    dst.ApparelRequired = new List<ThingDef>(src.ApparelRequired);
                }
                else
                {
                    dst.ApparelRequired = null;
                }

                if (src.TechRequired != null)
                {
                    dst.TechRequired = new List<ThingDef>(src.TechRequired);
                }
                else
                {
                    dst.TechRequired = null;
                }

                // Xenotype checks
                if (src.XenotypeChances != null)
                {
                    dst.XenotypeChances = new Dictionary<string, float>();
                    foreach (var kvp in src.XenotypeChances)
                    {
                        dst.XenotypeChances[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    dst.XenotypeChances = new Dictionary<string, float>();
                }
                dst.DisableXenotypeChances = src.DisableXenotypeChances;
                dst.ForcedXenotype = src.ForcedXenotype;
            }
        }

        // ── Static helpers ────────────────────────────────────

        private static void CopySpecificApparelByPredicate(
            KindGearData src, KindGearData dst,
            System.Func<SpecRequirementEdit, bool> predicate)
        {
            var srcItems = src.SpecificApparel?.Where(predicate).ToList();
            if (srcItems != null && srcItems.Count > 0)
            {
                if (dst.SpecificApparel == null)
                    dst.SpecificApparel = new List<SpecRequirementEdit>();
                else
                    dst.SpecificApparel.RemoveAll(x => predicate(x));

                foreach (var item in srcItems)
                    dst.SpecificApparel.Add(CloneSpecReq(item));
            }
            else if (dst.SpecificApparel != null)
            {
                dst.SpecificApparel.RemoveAll(x => predicate(x));
                if (dst.SpecificApparel.Count == 0) dst.SpecificApparel = null;
            }
        }

        private static SpecRequirementEdit CloneSpecReq(SpecRequirementEdit item)
        {
            return new SpecRequirementEdit
            {
                Thing = item.Thing,
                Material = item.Material,
                Style = item.Style,
                Quality = item.Quality,
                Biocode = item.Biocode,
                Color = item.Color,
                SelectionMode = item.SelectionMode,
                SelectionChance = item.SelectionChance,
                CountRange = item.CountRange,
                PoolType = item.PoolType,
                weight = item.weight
            };
        }

        // Mirror helpers from GearEditPanel (kept private to avoid coupling)
        private static bool IsBelt(ThingDef t) =>
            t.apparel?.layers?.Contains(ApparelLayerDefOf.Belt) ?? false;

        private static bool IsArmor(ThingDef t)
        {
            if (t.apparel == null) return false;
            if (IsBelt(t)) return false;
            return t.apparel.layers.Contains(ApparelLayerDefOf.Shell) ||
                   t.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) > 0.4f;
        }

        private static bool IsApparel(ThingDef t)
        {
            if (t.apparel == null) return false;
            if (IsBelt(t)) return false;
            if (IsArmor(t)) return false;
            return true;
        }
    }
}
