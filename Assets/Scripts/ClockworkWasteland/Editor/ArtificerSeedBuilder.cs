using System.Linq;
using ClockworkWasteland.Combat;
using UnityEditor;
using UnityEngine;

namespace ClockworkWasteland.Editor
{
    public static class ArtificerSeedBuilder
    {
        private const string CombatantsRoot = "Assets/ClockworkWastelandDemo/Data/Combatants";
        private const string SkillsRoot = "Assets/ClockworkWastelandDemo/Data/Skills";
        private const string BuffsRoot = "Assets/Resources/Buffs";
        private const string TargetCombatantPath = CombatantsRoot + "/巴托洛缪·黑线.asset";

        private const string SuppressedName = "压制";
        private const string GearStingName = "齿轮刺针";
        private const string ScrapVolleyName = "废铁齐射";
        private const string LockPinName = "锁定射钉";
        private const string OverloadSparkName = "过载火花";

        [MenuItem("Clockwork Wasteland/Build/Seed Artificer - 巴托洛缪·黑线")]
        public static void BuildBartArtificer()
        {
            var suppressBuff = AssetDatabase.LoadAssetAtPath<BuffData>($"{BuffsRoot}/{SuppressedName}.asset");
            if (suppressBuff == null)
            {
                Debug.LogError($"Artificer seed requires suppressed buff at {BuffsRoot}/{SuppressedName}.asset");
                return;
            }

            var gearSting = CreateOrUpdateSkill(
                GearStingName,
                "hero_01_gear_sting",
                "发射带齿射钉，擅长精准打击敌方后排。",
                SkillDataType.伤害,
                SkillDataTargetType.单敌,
                7,
                1.05f,
                1,
                0,
                null,
                1,
                new[] { 3, 4 },
                new[] { 2, 3, 4 });

            var scrapVolley = CreateOrUpdateSkill(
                ScrapVolleyName,
                "hero_01_scrap_volley",
                "倾泻拼装火力，对敌方全体形成压制。",
                SkillDataType.伤害,
                SkillDataTargetType.全体敌,
                4,
                0.8f,
                2,
                1,
                null,
                1,
                new[] { 3, 4 },
                new[] { 1, 2, 3, 4 });

            var lockPin = CreateOrUpdateSkill(
                LockPinName,
                "hero_01_lock_pin",
                "射出锁定钉压制目标，削弱其下次攻击。",
                SkillDataType.控制,
                SkillDataTargetType.单敌,
                5,
                0.95f,
                1,
                1,
                suppressBuff,
                1,
                new[] { 3, 4 },
                new[] { 1, 2, 3, 4 });

            var overloadSpark = CreateOrUpdateSkill(
                OverloadSparkName,
                "hero_01_overload_spark",
                "引爆已被锁定的破绽，对受压制目标造成更高伤害。",
                SkillDataType.伤害,
                SkillDataTargetType.单敌,
                8,
                1.2f,
                2,
                2,
                null,
                1,
                new[] { 3, 4 },
                new[] { 1, 2, 3, 4 });

            var combatant = AssetDatabase.LoadAssetAtPath<CombatantDefinition>(TargetCombatantPath);
            if (combatant == null)
            {
                Debug.LogError($"Artificer target combatant not found: {TargetCombatantPath}");
                return;
            }

            combatant.archetype = CombatArchetype.Artificer;
            combatant.preferredRow = CombatRowPreference.Back;
            combatant.specialization = CombatSpecialization.None;
            combatant.passive = HeroPassive.Tactician;
            combatant.maxHealth = 30;
            combatant.attack = 8;
            combatant.defense = 2;
            combatant.speed = 6;
            combatant.recruitPrice = 650;
            combatant.skills = new[]
            {
                gearSting,
                scrapVolley,
                lockPin,
                overloadSpark
            };

            EditorUtility.SetDirty(combatant);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Artificer first-pass assets updated for 巴托洛缪·黑线");
        }

        public static void ExecuteBuildBartArtificer()
        {
            BuildBartArtificer();
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
            skill.skillSfx = FindSkillClip(fileName, skillId, "射", "火", "钉", "爆");
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
