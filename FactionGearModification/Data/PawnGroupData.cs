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
            
            if (maker.kindDef == null) return null;

            maker.commonality = commonality;
            maker.maxTotalPoints = maxTotalPoints;

            maker.options = ConvertToGenOptions(options);
            maker.traders = ConvertToGenOptions(traders);
            maker.carriers = ConvertToGenOptions(carriers);
            maker.guards = ConvertToGenOptions(guards);

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

        public PawnGenOptionData DeepCopy()
        {
            return new PawnGenOptionData
            {
                kindDefName = this.kindDefName,
                selectionWeight = this.selectionWeight
            };
        }
    }
}
