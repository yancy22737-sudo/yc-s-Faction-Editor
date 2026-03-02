using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public class PawnGroupMakerData : IExposable, IUndoable
    {
        public string kindDefName;
        public string customLabel;
        public float commonality = 100f;

        // IUndoable implementation
        public string ContextId => this.GetHashCode().ToString();

        public object CreateSnapshot()
        {
            return this.DeepCopy();
        }

        public void RestoreFromSnapshot(object snapshot)
        {
            if (snapshot is PawnGroupMakerData data)
            {
                this.CopyFrom(data);
            }
        }

        public void CopyFrom(PawnGroupMakerData source)
        {
            this.kindDefName = source.kindDefName;
            this.customLabel = source.customLabel;
            this.commonality = source.commonality;
            this.maxTotalPoints = source.maxTotalPoints;

            this.options.Clear();
            foreach (var o in source.options) this.options.Add(o.DeepCopy());

            this.traders.Clear();
            foreach (var o in source.traders) this.traders.Add(o.DeepCopy());

            this.carriers.Clear();
            foreach (var o in source.carriers) this.carriers.Add(o.DeepCopy());

            this.guards.Clear();
            foreach (var o in source.guards) this.guards.Add(o.DeepCopy());
        }
        public List<PawnGenOptionData> options = new List<PawnGenOptionData>();
        public List<PawnGenOptionData> traders = new List<PawnGenOptionData>();
        public List<PawnGenOptionData> carriers = new List<PawnGenOptionData>();
        public List<PawnGenOptionData> guards = new List<PawnGenOptionData>();

        // Optional fields based on PawnGroupMaker
        public float maxTotalPoints = 9999999f;

        public PawnGroupMakerData() { }

        public PawnGroupMakerData(PawnGroupMaker maker)
        {
            if (maker.kindDef != null)
                this.kindDefName = maker.kindDef.defName;
            
            this.commonality = maker.commonality;
            this.maxTotalPoints = maker.maxTotalPoints;

            if (maker.options != null)
            {
                foreach (var opt in maker.options)
                {
                    options.Add(new PawnGenOptionData(opt));
                }
            }

            if (maker.traders != null)
            {
                foreach (var opt in maker.traders)
                {
                    traders.Add(new PawnGenOptionData(opt));
                }
            }

            if (maker.carriers != null)
            {
                foreach (var opt in maker.carriers)
                {
                    carriers.Add(new PawnGenOptionData(opt));
                }
            }

            if (maker.guards != null)
            {
                foreach (var opt in maker.guards)
                {
                    guards.Add(new PawnGenOptionData(opt));
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref kindDefName, "kindDefName");
            Scribe_Values.Look(ref customLabel, "customLabel");
            Scribe_Values.Look(ref commonality, "commonality", 100f);
            Scribe_Values.Look(ref maxTotalPoints, "maxTotalPoints", 9999999f);

            Scribe_Collections.Look(ref options, "options", LookMode.Deep);
            Scribe_Collections.Look(ref traders, "traders", LookMode.Deep);
            Scribe_Collections.Look(ref carriers, "carriers", LookMode.Deep);
            Scribe_Collections.Look(ref guards, "guards", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (options == null) options = new List<PawnGenOptionData>();
                if (traders == null) traders = new List<PawnGenOptionData>();
                if (carriers == null) carriers = new List<PawnGenOptionData>();
                if (guards == null) guards = new List<PawnGenOptionData>();
            }
        }

        public PawnGroupMaker ToPawnGroupMaker()
        {
            PawnGroupMaker maker = new PawnGroupMaker();
            if (!string.IsNullOrEmpty(kindDefName))
            {
                maker.kindDef = DefDatabase<PawnGroupKindDef>.GetNamedSilentFail(kindDefName);
            }

            if (maker.kindDef == null)
            {
                Log.Warning($"[FactionGearCustomizer] PawnGroupMakerData.ToPawnGroupMaker: kindDef is null for kindDefName: {kindDefName}");
                return null;
            }

            maker.commonality = commonality;
            maker.maxTotalPoints = maxTotalPoints;

            maker.options = ConvertToGenOptions(options);
            maker.traders = ConvertToGenOptionsWithValidation(traders, "traders");
            maker.carriers = ConvertToGenOptions(carriers);
            maker.guards = ConvertToGenOptions(guards);

            // 验证 Trader 类型的群组必须有至少一个 trader
            if (maker.kindDef.defName == "Trader" && (maker.traders == null || maker.traders.Count == 0))
            {
                Log.Warning($"[FactionGearCustomizer] PawnGroupMakerData.ToPawnGroupMaker: Trader group '{kindDefName}' has no traders. This will cause caravan generation to fail. Please add at least one trader to the group.");
                // 返回 null 以防止游戏使用无效的 PawnGroupMaker
                return null;
            }

            return maker;
        }

        private List<PawnGenOption> ConvertToGenOptions(List<PawnGenOptionData> dataList)
        {
            List<PawnGenOption> list = new List<PawnGenOption>();
            foreach (var d in dataList)
            {
                var opt = d.ToPawnGenOption();
                if (opt != null) list.Add(opt);
            }
            return list;
        }

        /// <summary>
        /// 转换并验证 PawnGenOption，过滤掉不符合条件的 PawnKind
        /// </summary>
        private List<PawnGenOption> ConvertToGenOptionsWithValidation(List<PawnGenOptionData> dataList, string listType)
        {
            List<PawnGenOption> list = new List<PawnGenOption>();
            foreach (var d in dataList)
            {
                var opt = d.ToPawnGenOption();
                if (opt == null) continue;

                // 对于 traders 列表，验证 PawnKind 是否真的是交易者
                if (listType == "traders" && !IsValidTrader(opt.kind))
                {
                    Log.Warning($"[FactionGearCustomizer] PawnGroupMakerData.ToPawnGroupMaker: PawnKind '{opt.kind.defName}' ({opt.kind.label}) is not a valid trader but was found in the traders list. It will be skipped to prevent caravan generation errors.");
                    continue;
                }

                list.Add(opt);
            }
            return list;
        }

        /// <summary>
        /// 检查 PawnKindDef 是否是有效的交易者
        /// </summary>
        private bool IsValidTrader(PawnKindDef kind)
        {
            if (kind == null) return false;

            // 检查是否是 Trader 类型的 PawnKind（通过 defName 判断）
            if (kind.defName.Contains("Trader"))
                return true;

            // 检查是否有 trader 标签
            if (kind.trader)
                return true;

            // 检查是否是特定派系领袖类型（某些领袖也是交易者）
            if (kind.defName.Contains("Leader"))
                return true;

            // 检查 race 是否是人类类型（非动物）
            // 动物（如雪牛）不应该出现在 traders 列表中
            if (kind.race != null)
            {
                // 如果是动物（有 trainability 或特定标签），则不是有效交易者
                if (kind.race.race != null && kind.race.race.Animal)
                    return false;
            }

            // 默认允许其他情况（可能是自定义交易者）
            return true;
        }

        public PawnGroupMakerData DeepCopy()
        {
            var copy = new PawnGroupMakerData();
            copy.kindDefName = this.kindDefName;
            copy.customLabel = this.customLabel;
            copy.commonality = this.commonality;
            copy.maxTotalPoints = this.maxTotalPoints;

            foreach (var o in this.options) copy.options.Add(o.DeepCopy());
            foreach (var o in this.traders) copy.traders.Add(o.DeepCopy());
            foreach (var o in this.carriers) copy.carriers.Add(o.DeepCopy());
            foreach (var o in this.guards) copy.guards.Add(o.DeepCopy());

            return copy;
        }
    }

    public class PawnGenOptionData : IExposable
    {
        public string kindDefName;
        public float selectionWeight;
        public float? pointsOverride;
        
        // 年龄范围控制
        public float? minAge;
        public float? maxAge;
        
        // AI 特性控制
        public bool? allowAddictions;
        public bool? mustBeCapableOfViolence;

        public PawnGenOptionData() { }

        public PawnGenOptionData(PawnGenOption opt)
        {
            if (opt.kind != null)
                this.kindDefName = opt.kind.defName;
            this.selectionWeight = opt.selectionWeight;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref kindDefName, "kindDefName");
            Scribe_Values.Look(ref selectionWeight, "selectionWeight");
            Scribe_Values.Look(ref pointsOverride, "pointsOverride");
            Scribe_Values.Look(ref minAge, "minAge");
            Scribe_Values.Look(ref maxAge, "maxAge");
            Scribe_Values.Look(ref allowAddictions, "allowAddictions");
            Scribe_Values.Look(ref mustBeCapableOfViolence, "mustBeCapableOfViolence");
        }

        public PawnGenOption ToPawnGenOption()
        {
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindDefName);
            if (kind == null) return null;

            return new PawnGenOption
            {
                kind = kind,
                selectionWeight = selectionWeight
            };
        }

        public float GetEffectivePoints()
        {
            if (pointsOverride.HasValue)
            {
                return pointsOverride.Value;
            }

            var kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindDefName);
            return kind?.combatPower ?? 0f;
        }

        public PawnGenOptionData DeepCopy()
        {
            return new PawnGenOptionData
            {
                kindDefName = this.kindDefName,
                selectionWeight = this.selectionWeight,
                pointsOverride = this.pointsOverride,
                minAge = this.minAge,
                maxAge = this.maxAge,
                allowAddictions = this.allowAddictions,
                mustBeCapableOfViolence = this.mustBeCapableOfViolence
            };
        }
    }
}
