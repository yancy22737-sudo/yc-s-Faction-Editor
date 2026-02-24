using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public class FactionGearData : IExposable
    {
        public string factionDefName;
        public List<KindGearData> kindGearData = new List<KindGearData>();
        
        // Faction Edit Fields
        public bool isModified = false;
        public string Label;
        public string Description;
        public string IconPath;
        public Color? Color;
        
        // Xenotype Settings (Biotech DLC)
        // Key: XenotypeDefName, Value: Chance (0.0 - 1.0)
        public Dictionary<string, float> XenotypeChances = new Dictionary<string, float>();

        // Pawn Group Makers (Raids, Caravans, etc.)
        public List<PawnGroupMakerData> groupMakers = new List<PawnGroupMakerData>();

        // Player Relation Override (Ally/Neutral/Hostile)
        public FactionRelationKind? PlayerRelationOverride;

        // 优化查询效率的字典索引        [Unsaved]
        private Dictionary<string, KindGearData> kindGearDataDict;

        public FactionGearData() { }

        public FactionGearData(string factionDefName)
        {
            this.factionDefName = factionDefName;
            InitializeDictionary();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref factionDefName, "factionDefName");
            Scribe_Collections.Look(ref kindGearData, "kindGearData", LookMode.Deep);
            
            Scribe_Values.Look(ref isModified, "isModified", false);
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref Description, "description");
            Scribe_Values.Look(ref IconPath, "iconPath");
            Scribe_Values.Look(ref Color, "color");
            
            Scribe_Collections.Look(ref XenotypeChances, "xenotypeChances", LookMode.Value, LookMode.Value);
            if (XenotypeChances == null) XenotypeChances = new Dictionary<string, float>();

            Scribe_Collections.Look(ref groupMakers, "groupMakers", LookMode.Deep);
            if (groupMakers == null) groupMakers = new List<PawnGroupMakerData>();

            Scribe_Values.Look(ref PlayerRelationOverride, "playerRelationOverride");

            if (kindGearData == null)
                kindGearData = new List<KindGearData>();
                
            // 重新初始化字典索�?            InitializeDictionary();
        }
        
        private void InitializeDictionary()
        {
            kindGearDataDict = new Dictionary<string, KindGearData>();
            foreach (var kindData in kindGearData)
            {
                if (kindData != null && !string.IsNullOrEmpty(kindData.kindDefName) && !kindGearDataDict.ContainsKey(kindData.kindDefName))
                {
                    kindGearDataDict.Add(kindData.kindDefName, kindData);
                }
            }
        }

        public KindGearData GetOrCreateKindData(string kindDefName)
        {
            if (kindGearDataDict == null)
            {
                InitializeDictionary();
            }
            
            if (kindGearDataDict.TryGetValue(kindDefName, out var data))
            {
                return data;
            }
            
            data = new KindGearData(kindDefName);
            kindGearData.Add(data);
            kindGearDataDict.Add(kindDefName, data);
            return data;
        }

        public void ResetToDefault()
        {
            kindGearData.Clear();
            if (kindGearDataDict != null)
            {
                kindGearDataDict.Clear();
            }
            
            isModified = false;
            Label = null;
            Description = null;
            IconPath = null;
            Color = null;
            XenotypeChances?.Clear();
            groupMakers?.Clear();
            PlayerRelationOverride = null;
        }
        
        // 添加或更新兵种数据
        public void AddOrUpdateKindData(KindGearData data)
        {
            if (data == null) return;

            var existing = kindGearData.FirstOrDefault(k => k.kindDefName == data.kindDefName);
            if (existing != null)
            {
                kindGearData.Remove(existing);
            }
            kindGearData.Add(data);
            
            // 同步更新字典索引
            if (kindGearDataDict == null)
            {
                InitializeDictionary();
            }
            
            if (kindGearDataDict.ContainsKey(data.kindDefName))
            {
                kindGearDataDict[data.kindDefName] = data;
            }
            else
            {
                kindGearDataDict.Add(data.kindDefName, data);
            }
        }
        
        // 获取指定兵种的数据
        public KindGearData GetKindData(string kindDefName)
        {
            return kindGearData.FirstOrDefault(k => k.kindDefName == kindDefName);
        }

        public FactionGearData DeepCopy()
        {
            var copy = new FactionGearData(factionDefName);
            
            copy.isModified = this.isModified;
            copy.Label = this.Label;
            copy.Description = this.Description;
            copy.IconPath = this.IconPath;
            copy.Color = this.Color;
            copy.PlayerRelationOverride = this.PlayerRelationOverride;
            
            if (this.XenotypeChances != null)
            {
                foreach (var kvp in this.XenotypeChances)
                {
                    copy.XenotypeChances[kvp.Key] = kvp.Value;
                }
            }

            if (this.groupMakers != null)
            {
                foreach (var group in this.groupMakers)
                {
                    copy.groupMakers.Add(group.DeepCopy());
                }
            }

            foreach (var kind in kindGearData)
            {
                if (kind != null)
                    copy.kindGearData.Add(kind.DeepCopy());
            }
            copy.InitializeDictionary();
            return copy;
        }
    }
}