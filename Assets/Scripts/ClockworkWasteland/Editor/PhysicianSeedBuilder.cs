using System.Linq;
using ClockworkWasteland.Combat;
using UnityEditor;
using UnityEngine;

namespace ClockworkWasteland.Editor
{
    public static class PhysicianSeedBuilder
    {
        private const string CombatantsRoot = "Assets/ClockworkWastelandDemo/Data/Combatants";
        private const string SkillsRoot = "Assets/ClockworkWastelandDemo/Data/Skills";
        private const string BuffsRoot = "Assets/Resources/Buffs";
        private const string TargetCombatantPath = CombatantsRoot + "/瑟蕾辛·鸦骨.asset";

        private const string StunName = "眩晕";
        private const string BoneDartName = "巫骨针";
        private const string FieldStitchName = "战地缝合";
        private const string SteamPurgeName = "蒸汽净除";
        private const string StunChainName = "震荡骨链";

        [MenuItem("Clockwork Wasteland/Build/Seed Physician - 瑟蕾辛·鸦骨")]
        public static void BuildSelesinPhysician()
        {
            var stunBuff = AssetDatabase.LoadAssetAtPath<BuffData>($"{BuffsRoot}/{StunName}.asset");
            if (stunBuff == null)
            {
                Debug.LogError($"Physician seed requires stun buff at {BuffsRoot}/{StunName}.asset");
                return;
            }

            var boneDart = CreateOrUpdateSkill(
                BoneDartName,
                "hero_02_bone_dart",
                "投出巫骨针，为后续净化和压制争取时间。",
                SkillDataType.伤害,
                SkillDataTargetType.单敌,
                6,
                0.95f,
                1,
                0,
                null,
                1,
                new[] { 3, 4 },
                new[] { 1, 2, 3, 4 });

            var fieldStitch = CreateOrUpdateSkill(
                FieldStitchName,
                "hero_02_field_stitch",
                "稳定治疗一名友方，并帮助其更快恢复技能节奏。",
                SkillDataType.治疗,
                SkillDataTargetType.单友,
                7,
                1f,
                1,
                1,
                null,
                1,
                new[] { 3, 4 },
                new[] { 1, 2, 3, 4 });

            var steamPurge = CreateOrUpdateSkill(
                SteamPurgeName,
                "hero_02_steam_purge",
                "净化友方负面状态，适合处理灼烧和眩晕。",
                SkillDataType.控制,
                SkillDataTargetType.单友,
                0,
                0f,
                1,
                1,
                null,
                1,
                new[] { 3, 4 },
                new[] { 1, 2, 3, 4 });

            var stunChain = CreateOrUpdateSkill(
                StunChainName,
                "hero_02_stun_chain",
                "用震荡骨链压制敌方后排，命中后排时额外回收资源。",
                SkillDataType.控制,
                SkillDataTargetType.单敌,
                4,
                0.8f,
                2,
                2,
                stunBuff,
                1,
                new[] { 3, 4 },
                new[] { 2, 3, 4 });

            var combatant = AssetDatabase.LoadAssetAtPath<CombatantDefinition>(TargetCombatantPath);
            if (combatant == null)
            {
                Debug.LogError($"Physician target combatant not found: {TargetCombatantPath}");
                return;
            }

            combatant.archetype = CombatArchetype.Physician;
            combatant.preferredRow = CombatRowPreference.Back;
            combatant.specialization = CombatSpecialization.None;
            combatant.passive = HeroPassive.Regenerator;
            combatant.maxHealth = 30;
            combatant.attack = 7;
            combatant.defense = 2;
            combatant.speed = 5;
            combatant.recruitPrice = 650;
            combatant.skills = new[]
            {
                boneDart,
                fieldStitch,
                steamPurge,
                stunChain
            };

            EditorUtility.SetDirty(combatant);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Physician first-pass assets updated for 瑟蕾辛·鸦骨");
        }

        public static void ExecuteBuildSelesinPhysician()
        {
            BuildSelesinPhysician();
        }

        private static SkillData CreateOrUpdateSkill(
            string fileName,
            string skillId,
            string description,
            SkillDataType skillType,
            SkillDataTargetType targetType,
            int baseValue,
            float powerMultiplier,
            int manaCost,
            int cooldown,
            BuffData applyBuff,
            int applyBuffDuration,
            int[] casterAllowedPositions,
            int[] targetAllowedPositions)
        {
            var assetPath = $"{SkillsRoot}/{fileName}.asset";
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>(assetPath);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillData>();
                AssetDatabase.CreateAsset(skill, assetPath);
            }

            skill.skillId = skillId;
            skill.skillName = fileName;
            skill.description = description;
            skill.skillType = skillType;
            skill.targetType = targetType;
            skill.baseValue = baseValue;
            skill.powerMultiplier = powerMultiplier;
            skill.manaCost = manaCost;
            skill.cooldown = cooldown;
            skill.applyBuff = applyBuff;
            skill.applyBuffDuration = applyBuffDuration;
            skill.casterAllowedPositions = casterAllowedPositions ?? new[] { 1, 2, 3, 4 };
            skill.targetAllowedPositions = targetAllowedPositions ?? new[] { 1, 2, 3, 4 };
            skill.overlayDuration = 0.22f;
            skill.skillSfx = FindSkillClip(fileName, skillId, "针", "治", "净", "链");
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static AudioClip FindSkillClip(params string[] keywords)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    continue;
                }

                var searchText = $"{clip.name}|{path}";
                if (keywords.Any(keyword => !string.IsNullOrWhiteSpace(keyword) && searchText.Contains(keyword)))
                {
                    return clip;
                }
            }

            return null;
        }
    }
}
