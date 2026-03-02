using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionGearCustomizer.Validation
{
    public static class HediffPartMappingManager
    {
        private static Dictionary<string, List<string>> _hediffToPartMappings;
        private static Dictionary<string, int> _partMaxImplantCount;
        private static bool _initialized = false;

        // 关键词到身体部位的映射配置（用于自动推断）
        private static List<(string[] Keywords, string[] Parts)> BodyPartInferenceRules;

        static HediffPartMappingManager()
        {
            InitializeMappings();
        }

        private static void InitializeMappings()
        {
            _hediffToPartMappings = new Dictionary<string, List<string>>();
            _partMaxImplantCount = new Dictionary<string, int>();

            InitializeDefaultPartLimits();
            InitializeDefaultHediffMappings();
            BodyPartInferenceRules = InitializeInferenceRules();

            _initialized = true;
        }

        private static void InitializeDefaultPartLimits()
        {
            // 头部
            _partMaxImplantCount["Brain"] = 2;
            _partMaxImplantCount["Head"] = 3;
            _partMaxImplantCount["Neck"] = 1;
            
            // 躯干
            _partMaxImplantCount["Torso"] = 4;
            _partMaxImplantCount["Spine"] = 1;
            _partMaxImplantCount["Ribcage"] = 2;
            _partMaxImplantCount["Sternum"] = 2;
            _partMaxImplantCount["Pelvis"] = 2;
            
            // 内脏器官
            _partMaxImplantCount["Stomach"] = 1;
            _partMaxImplantCount["Heart"] = 1;
            _partMaxImplantCount["Lung"] = 2;  // Lung def 包含左右肺，最多2个植入物
            _partMaxImplantCount["Liver"] = 1;
            _partMaxImplantCount["Kidney"] = 2;  // Kidney def 包含左右肾，最多2个植入物
            
            // 上肢
            _partMaxImplantCount["Shoulder"] = 2;  // Shoulder def 包含左右肩
            _partMaxImplantCount["Clavicle"] = 2;  // Clavicle def 包含左右锁骨
            _partMaxImplantCount["Arm"] = 2;  // Arm def 包含左右臂
            _partMaxImplantCount["Humerus"] = 2;  // Humerus def 包含左右肱骨
            _partMaxImplantCount["Radius"] = 2;  // Radius def 包含左右桡骨
            _partMaxImplantCount["Hand"] = 2;  // Hand def 包含左右手
            _partMaxImplantCount["Finger"] = 10;  // Finger def 包含10个手指
            
            // 下肢
            _partMaxImplantCount["Waist"] = 2;
            _partMaxImplantCount["Hip"] = 2;  // Hip def 包含左右臀
            _partMaxImplantCount["Leg"] = 2;  // Leg def 包含左右腿
            _partMaxImplantCount["Femur"] = 2;  // Femur def 包含左右股骨
            _partMaxImplantCount["Tibia"] = 2;  // Tibia def 包含左右胫骨
            _partMaxImplantCount["Foot"] = 2;  // Foot def 包含左右足
            _partMaxImplantCount["Toe"] = 10;  // Toe def 包含10个脚趾
            
            // 感觉器官
            _partMaxImplantCount["Eye"] = 2;  // Eye def 包含左右眼
            _partMaxImplantCount["Ear"] = 2;  // Ear def 包含左右耳
            _partMaxImplantCount["Nose"] = 1;
            _partMaxImplantCount["Jaw"] = 1;
            _partMaxImplantCount["Tongue"] = 1;
            _partMaxImplantCount["Teeth"] = 1;
            _partMaxImplantCount["Skull"] = 1;
        }

        private static void InitializeDefaultHediffMappings()
        {
            // 眼部植入物 - Eye def 包含左右眼两个实例
            AddHediffMapping("BionicEye", new List<string> { "Eye" });
            AddHediffMapping("ArchotechEye", new List<string> { "Eye" });
            AddHediffMapping("SimpleEye", new List<string> { "Eye" });
            
            // 耳部植入物 - Ear def 包含左右耳两个实例
            AddHediffMapping("BionicEar", new List<string> { "Ear" });
            AddHediffMapping("ArchotechEar", new List<string> { "Ear" });
            AddHediffMapping("CochlearImplant", new List<string> { "Ear" });
            
            // 鼻部植入物
            AddHediffMapping("BionicNose", new List<string> { "Nose" });
            AddHediffMapping("ReinforcedNose", new List<string> { "Nose" });
            
            // 下颚/舌头植入物
            AddHediffMapping("BionicJaw", new List<string> { "Jaw" });
            AddHediffMapping("BionicTongue", new List<string> { "Tongue" });
            AddHediffMapping("SyntheticTongue", new List<string> { "Tongue" });
            AddHediffMapping("VenomFangs", new List<string> { "Teeth" });
            
            // 手臂植入物 - Arm def 包含左右臂两个实例
            AddHediffMapping("BionicArm", new List<string> { "Arm" });
            AddHediffMapping("ArchotechArm", new List<string> { "Arm" });
            AddHediffMapping("SimpleBionicArm", new List<string> { "Arm" });
            AddHediffMapping("PowerClaw", new List<string> { "Hand" });
            AddHediffMapping("ElbowBlade", new List<string> { "Arm" });
            AddHediffMapping("HandTalon", new List<string> { "Hand" });
            AddHediffMapping("FingerClaw", new List<string> { "Finger" });
            
            // 腿部植入物 - Leg def 包含左右腿两个实例
            AddHediffMapping("BionicLeg", new List<string> { "Leg" });
            AddHediffMapping("ArchotechLeg", new List<string> { "Leg" });
            AddHediffMapping("SimpleBionicLeg", new List<string> { "Leg" });
            AddHediffMapping("SimpleProstheticLeg", new List<string> { "Leg" });
            AddHediffMapping("FootClaw", new List<string> { "Foot" });
            
            // 脊柱/骨骼植入物
            AddHediffMapping("BionicSpine", new List<string> { "Spine" });
            AddHediffMapping("ArchotechSpine", new List<string> { "Spine" });
            AddHediffMapping("ReinforcedSpine", new List<string> { "Spine" });
            AddHediffMapping("RibcageImplant", new List<string> { "Ribcage" });
            AddHediffMapping("SternumPlate", new List<string> { "Sternum" });
            AddHediffMapping("PelvisPlate", new List<string> { "Pelvis" });
            AddHediffMapping("SkullPlate", new List<string> { "Skull" });
            
            // 内脏器官植入物
            AddHediffMapping("BionicHeart", new List<string> { "Heart" });
            AddHediffMapping("ArchotechHeart", new List<string> { "Heart" });
            AddHediffMapping("StrongHeart", new List<string> { "Heart" });
            AddHediffMapping("BionicStomach", new List<string> { "Stomach" });
            AddHediffMapping("ArchotechStomach", new List<string> { "Stomach" });
            AddHediffMapping("NuclearStomach", new List<string> { "Stomach" });
            AddHediffMapping("BionicLiver", new List<string> { "Liver" });
            AddHediffMapping("ArchotechLiver", new List<string> { "Liver" });
            // Kidney def 包含左右肾两个实例
            AddHediffMapping("BionicKidney", new List<string> { "Kidney" });
            AddHediffMapping("ArchotechKidney", new List<string> { "Kidney" });
            // Lung def 包含左右肺两个实例
            AddHediffMapping("BionicLung", new List<string> { "Lung" });
            AddHediffMapping("ArchotechLung", new List<string> { "Lung" });
            AddHediffMapping("ReinforcedLung", new List<string> { "Lung" });
            AddHediffMapping("DetoxifierLung", new List<string> { "Lung" });
            AddHediffMapping("DeathAcidifier", new List<string> { "Torso" });
            
            // 大脑/神经植入物
            AddHediffMapping("Joywire", new List<string> { "Brain" });
            AddHediffMapping("Painstopper", new List<string> { "Brain" });
            AddHediffMapping("Neurotrainer", new List<string> { "Brain" });
            AddHediffMapping("LearningAssistant", new List<string> { "Brain" });
            AddHediffMapping("PsychicAmplifier", new List<string> { "Brain" });
            AddHediffMapping("PsychicHarmonizer", new List<string> { "Brain" });
            AddHediffMapping("AIChip", new List<string> { "Brain" });
            AddHediffMapping("CircadianAssistant", new List<string> { "Brain" });
            AddHediffMapping("DeadmansSwitch", new List<string> { "Brain" });
            AddHediffMapping("AgeReversal", new List<string> { "Brain" });
            AddHediffMapping("AgeReversal_Backup", new List<string> { "Brain" });
            AddHediffMapping("ArchotechCortex", new List<string> { "Brain" });
            AddHediffMapping("SubcoreEjector", new List<string> { "Brain" });
            AddHediffMapping("NeuralSupercomputer", new List<string> { "Brain" });
            AddHediffMapping("BrainWiring", new List<string> { "Brain" });
            
            // 躯干/通用植入物
            AddHediffMapping("FirefoamPopper", new List<string> { "Torso" });
            AddHediffMapping("LoveEnhancer", new List<string> { "Torso" });
            AddHediffMapping("MedicalPod", new List<string> { "Torso" });
            AddHediffMapping("Immunocytoinjection", new List<string> { "Torso" });
            AddHediffMapping("Mechlink", new List<string> { "LeftHand", "RightHand", "Hand" });
            AddHediffMapping("WasteProcessingSystem", new List<string> { "Torso" });
            AddHediffMapping("ArmorskinGland", new List<string> { "Torso" });
            AddHediffMapping("LimbReplacement", new List<string> { "Torso" });
            
            // 肩部/锁骨植入物
            AddHediffMapping("ShoulderMount", new List<string> { "LeftShoulder", "RightShoulder", "Shoulder" });
            AddHediffMapping("ClaviclePlate", new List<string> { "Clavicle" });
            
            // 骨骼植入物
            AddHediffMapping("HumerusPlate", new List<string> { "Humerus" });
            AddHediffMapping("RadiusPlate", new List<string> { "Radius" });
            AddHediffMapping("FemurPlate", new List<string> { "Femur" });
            AddHediffMapping("TibiaPlate", new List<string> { "Tibia" });
            
            // 颈部/头部植入物
            AddHediffMapping("NeckImplant", new List<string> { "Neck" });
            AddHediffMapping("HeadImplant", new List<string> { "Head" });
            
            // 腰部/臀部植入物
            AddHediffMapping("WaistImplant", new List<string> { "Waist" });
            AddHediffMapping("HipImplant", new List<string> { "Hip", "LeftHip", "RightHip" });
        }

        /// <summary>
        /// 初始化身体部位推断规则 - 使用正确的 BodyPartDef defName
        /// 注意：RimWorld 中成对的器官（眼、耳、臂、腿、手、足、肾、肺）使用同一个 BodyPartDef
        /// 例如：Eye def 包含左右两个眼睛实例，而不是 LeftEye 和 RightEye 两个 def
        /// </summary>
        private static List<(string[] Keywords, string[] Parts)> InitializeInferenceRules()
        {
            return new List<(string[] Keywords, string[] Parts)>
            {
                // 眼部 - Eye def 包含左右眼两个实例
                (new[] { "eye" }, new[] { "Eye" }),
                
                // 耳部 - Ear def 包含左右耳两个实例
                (new[] { "ear" }, new[] { "Ear" }),
                
                // 手臂 - Arm def 包含左右臂两个实例
                (new[] { "arm" }, new[] { "Arm" }),
                
                // 腿部 - Leg def 包含左右腿两个实例
                (new[] { "leg" }, new[] { "Leg" }),
                
                // 手部 - Hand def 包含左右手两个实例
                (new[] { "hand" }, new[] { "Hand" }),
                
                // 足部 - Foot def 包含左右足两个实例
                (new[] { "foot" }, new[] { "Foot" }),
                
                // 手指 - Finger def 包含多个手指实例
                (new[] { "finger" }, new[] { "Finger" }),
                
                // 脚趾 - Toe def 包含多个脚趾实例
                (new[] { "toe" }, new[] { "Toe" }),
                
                // 心脏
                (new[] { "heart" }, new[] { "Heart" }),
                
                // 胃
                (new[] { "stomach" }, new[] { "Stomach" }),
                
                // 肝脏
                (new[] { "liver" }, new[] { "Liver" }),
                
                // 肾脏 - Kidney def 包含左右肾两个实例
                (new[] { "kidney" }, new[] { "Kidney" }),
                
                // 肺 - Lung def 包含左右肺两个实例
                (new[] { "lung" }, new[] { "Lung" }),
                
                // 大脑/神经（多个关键词）
                (new[] { "brain", "neural", "psychic", "joywire", "painstopper", "cortex" }, new[] { "Brain" }),
                
                // 脊柱
                (new[] { "spine" }, new[] { "Spine" }),
                
                // 鼻
                (new[] { "nose" }, new[] { "Nose" }),
                
                // 下颚
                (new[] { "jaw" }, new[] { "Jaw" }),
                
                // 舌头
                (new[] { "tongue" }, new[] { "Tongue" }),
                
                // 牙齿/毒牙
                (new[] { "fang", "tooth" }, new[] { "Teeth" }),
                
                // 爪/钳
                (new[] { "claw" }, new[] { "Hand" }),
                
                // 肋骨
                (new[] { "rib" }, new[] { "Ribcage" }),
                
                // 胸骨
                (new[] { "stern" }, new[] { "Sternum" }),
                
                // 骨盆
                (new[] { "pelv" }, new[] { "Pelvis" }),
                
                // 头骨
                (new[] { "skull" }, new[] { "Skull" }),
                
                // 颈部
                (new[] { "neck" }, new[] { "Neck" }),
                
                // 头部
                (new[] { "head" }, new[] { "Head" }),
                
                // 肩部 - Shoulder def 包含左右肩两个实例
                (new[] { "shoulder" }, new[] { "Shoulder" }),
                
                // 锁骨
                (new[] { "clavicle" }, new[] { "Clavicle" }),
                
                // 肱骨
                (new[] { "humerus" }, new[] { "Humerus" }),
                
                // 桡骨
                (new[] { "radius" }, new[] { "Radius" }),
                
                // 腰部
                (new[] { "waist" }, new[] { "Waist" }),
                
                // 臀部 - Hip def 包含左右臀两个实例
                (new[] { "hip" }, new[] { "Hip" }),
                
                // 股骨
                (new[] { "femur" }, new[] { "Femur" }),
                
                // 胫骨
                (new[] { "tibia" }, new[] { "Tibia" }),
                
                // 躯干
                (new[] { "torso" }, new[] { "Torso" })
            };
        }

        public static void AddHediffMapping(string hediffDefName, List<string> partDefNames)
        {
            if (string.IsNullOrEmpty(hediffDefName)) return;
            if (partDefNames == null || partDefNames.Count == 0) return;

            _hediffToPartMappings[hediffDefName] = new List<string>(partDefNames);
        }

        public static void SetPartMaxImplantCount(string partDefName, int maxCount)
        {
            if (string.IsNullOrEmpty(partDefName)) return;
            _partMaxImplantCount[partDefName] = Math.Max(1, maxCount);
        }

        public static List<string> GetRecommendedPartsForHediff(HediffDef hediffDef)
        {
            if (hediffDef == null) return new List<string>();

            if (_hediffToPartMappings.TryGetValue(hediffDef.defName, out var parts))
            {
                return new List<string>(parts);
            }

            var inferredParts = InferPartsFromHediffDef(hediffDef);
            if (inferredParts.Count > 0)
            {
                AddHediffMapping(hediffDef.defName, inferredParts);
            }

            return inferredParts;
        }

        /// <summary>
        /// 根据 HediffDef 自动推断适用的身体部位
        /// </summary>
        /// <param name="hediffDef">Hediff 定义</param>
        /// <returns>推断出的身体部位列表</returns>
        private static List<string> InferPartsFromHediffDef(HediffDef hediffDef)
        {
            var parts = new List<string>();

            if (hediffDef.label == null) return parts;

            string labelLower = hediffDef.label.ToLower();
            string defNameLower = hediffDef.defName.ToLower();

            // 遍历所有推断规则
            foreach (var (keywords, targetParts) in BodyPartInferenceRules)
            {
                foreach (var keyword in keywords)
                {
                    if (labelLower.Contains(keyword) || defNameLower.Contains(keyword))
                    {
                        parts.AddRange(targetParts);
                        break; // 匹配到当前规则后，跳出关键词循环，继续下一条规则
                    }
                }
            }

            return parts;
        }

        public static int GetMaxImplantCountForPart(BodyPartDef partDef)
        {
            if (partDef == null) return 1;

            if (_partMaxImplantCount.TryGetValue(partDef.defName, out var maxCount))
            {
                return maxCount;
            }

            return 1;
        }

        public static int GetMaxImplantCountForPart(string partDefName)
        {
            if (string.IsNullOrEmpty(partDefName)) return 1;

            if (_partMaxImplantCount.TryGetValue(partDefName, out var maxCount))
            {
                return maxCount;
            }

            return 1;
        }

        public static bool HasMappingForHediff(HediffDef hediffDef)
        {
            if (hediffDef == null) return false;
            return _hediffToPartMappings.ContainsKey(hediffDef.defName);
        }

        public static void ClearAllMappings()
        {
            _hediffToPartMappings.Clear();
            _partMaxImplantCount.Clear();
            InitializeDefaultPartLimits();
            InitializeDefaultHediffMappings();
        }

        public static Dictionary<string, List<string>> GetAllMappings()
        {
            return new Dictionary<string, List<string>>(_hediffToPartMappings);
        }

        public static Dictionary<string, int> GetAllPartLimits()
        {
            return new Dictionary<string, int>(_partMaxImplantCount);
        }
    }
}
