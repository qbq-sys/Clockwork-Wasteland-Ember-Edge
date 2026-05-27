using System.Linq;
using ClockworkWasteland.Combat;
using UnityEditor;
using UnityEngine;

namespace ClockworkWasteland.Editor
{
    public static class ExecutionerSeedBuilder
    {
        private const string CombatantsRoot = "Assets/ClockworkWastelandDemo/Data/Combatants";
        private const string SkillsRoot = "Assets/ClockworkWastelandDemo/Data/Skills";
        private const string BuffsRoot = "Assets/Resources/Buffs";
        private const string TargetCombatantPath = CombatantsRoot + "/洛恩·灰刃.asset";

        private const string BleedName = "流血";
        private const string ShadowCutName = "影切";
        private const string VeinRendName = "裂脉斩";
        private const string CrescentLungeName = "残月突进";
        private const string WildHuntName = "野性追猎";

        [MenuItem("Clockwork Wasteland/Build/Seed Executioner - 洛恩·灰刃")]
        public static void BuildLornExecutioner()
        {
            var bleedBuff = AssetDatabase.LoadAssetAtPath<BuffData>($"{BuffsRoot}/{BleedName}.asset");
            if (bleedBuff == null)
            {
                Debug.LogError($"Executioner seed requires bleed buff at {BuffsRoot}/{BleedName}.asset");
                return;
            }

            var shadowCut = CreateOrUpdateSkill(
                ShadowCutName,
                "hero_03_shadow_cut",
                "稳定切开目标破绽，对带负面状态目标更有效。",
                SkillDataType.伤害,
                SkillDataTargetType.单敌,
                8,
                1f,
                1,
                0,
                null,
                1,
                new[] { 2, 3 },
                new[] { 1, 2, 3, 4 });

            var veinRend = CreateOrUpdateSkill(
                VeinRendName,
                "hero_03_vein_rend",
                "斩开血脉，附加流血并为后续处决铺路。",
                SkillDataType.伤害,
                SkillDataTargetType.单敌,
                7,
                1.1f,
                2,
                1,
                bleedBuff,
                2,
                new[] { 2, 3 },
                new[] { 1, 2, 3, 4 });

            var crescentLunge = CreateOrUpdateSkill(
                CrescentLungeName,
                "hero_03_crescent_lunge",
                "突进斩击敌方后排，擅长主动打开缺口。",
                SkillDataType.伤害,
                SkillDataTargetType.单敌,
                9,
                1.2f,
                2,
                2,
                null,
                1,
                new[] { 2, 3 },
                new[] { 3, 4 });

            var wildHunt = CreateOrUpdateSkill(
                WildHuntName,
                "hero_03_wild_hunt",
                "锁定残血目标发起追猎，完成击杀时回收资源。",
                SkillDataType.伤害,
                SkillDataTargetType.单敌,
                10,
                1.35f,
                3,
                2,
                null,
                1,
                new[] { 2, 3 },
                new[] { 1, 2, 3, 4 });

            var combatant = AssetDatabase.LoadAssetAtPath<CombatantDefinition>(TargetCombatantPath);
            if (combatant == null)
            {
                Debug.LogError($"Executioner target combatant not found: {TargetCombatantPath}");
                return;
            }

            combatant.archetype = CombatArchetype.Executioner;
            combatant.preferredRow = CombatRowPreference.Mid;
            combatant.specialization = CombatSpecialization.None;
            combatant.passive = HeroPassive.Executioner;
            combatant.maxHealth = 34;
            combatant.attack = 9;
            combatant.defense = 2;
            combatant.speed = 6;
            combatant.recruitPrice = 650;
            combatant.skills = new[]
            {
                shadowCut,
                veinRend,
                crescentLunge,
                wildHunt
            };

            EditorUtility.SetDirty(combatant);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Executioner first-pass assets updated for 洛恩·灰刃");
        }

        public static void ExecuteBuildLornExecutioner()
        {
            BuildLornExecutioner();
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
            skill.skillSfx = FindSkillClip(fileName, skillId, "斩", "突进", "追猎", "刀");
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
