using System.Linq;
using ClockworkWasteland.Combat;
using UnityEditor;
using UnityEngine;

namespace ClockworkWasteland.Editor
{
    public static class BulwarkSeedBuilder
    {
        private const string CombatantsRoot = "Assets/ClockworkWastelandDemo/Data/Combatants";
        private const string SkillsRoot = "Assets/ClockworkWastelandDemo/Data/Skills";
        private const string BuffsRoot = "Assets/Resources/Buffs";
        private const string TargetCombatantPath = CombatantsRoot + "/\u5DF4\u5C14\u30FB\u788E\u65A9.asset";

        private const string GuardName = "\u62A4\u536B";
        private const string SuppressedName = "\u538B\u5236";
        private const string HoldTheLineName = "\u5B88\u7EBF\u65A9";
        private const string EarthbreakRamName = "\u88C2\u5730\u649E\u51FB";
        private const string WatcherSweepName = "\u5B88\u671B\u6A2A\u626B";
        private const string StandGuardName = "\u633A\u8EAB\u62A4\u536B";

        [MenuItem("Clockwork Wasteland/Build/Seed Bulwark - \u5DF4\u5C14\u30FB\u788E\u65A9")]
        public static void BuildBaalBulwark()
        {
            EnsureFolder("Assets/Resources", "Buffs");

            var guardBuff = CreateOrUpdateBuff(
                GuardName,
                "guard",
                "\u633A\u8EAB\u62A4\u5728\u90BB\u8FD1\u961F\u53CB\u8EAB\u524D\uFF0C\u5206\u62C5\u672C\u5E94\u843D\u5411\u4ED6\u4EEC\u7684\u4F24\u5BB3\u3002",
                2,
                false,
                0,
                new Color(0.42f, 0.88f, 0.82f, 1f),
                new Color(0.42f, 0.88f, 0.82f, 1f));

            var suppressBuff = CreateOrUpdateBuff(
                SuppressedName,
                "suppressed",
                "\u53D7\u5230\u538B\u5236\u65F6\uFF0C\u4E0B\u4E00\u6B21\u9020\u6210\u7684\u4F24\u5BB3\u4F1A\u660E\u663E\u4E0B\u964D\u3002",
                2,
                false,
                0,
                new Color(0.96f, 0.72f, 0.26f, 1f),
                new Color(0.96f, 0.72f, 0.26f, 1f));

            var holdTheLine = CreateOrUpdateSkill(
                HoldTheLineName,
                "hero_05_hold_the_line",
                "\u7A33\u4F4F\u524D\u6392\u8282\u594F\u7684\u57FA\u7840\u65A9\u51FB\u3002",
                SkillDataType.伤害,
                SkillDataTargetType.单敌,
                8,
                1f,
                0,
                0,
                null,
                1,
                new[] { 1, 2 },
                new[] { 1, 2 });

            var earthbreakRam = CreateOrUpdateSkill(
                EarthbreakRamName,
                "hero_05_earthbreak_ram",
                "\u5411\u524D\u731B\u649E\u76EE\u6807\uFF0C\u65BD\u52A0\u538B\u5236\u5E76\u5C1D\u8BD5\u5C06\u5176\u9876\u9000\u3002",
                SkillDataType.伤害,
                SkillDataTargetType.单敌,
                7,
                1.15f,
                2,
                1,
                suppressBuff,
                2,
                new[] { 1, 2 },
                new[] { 1, 2 });

            var watcherSweep = CreateOrUpdateSkill(
                WatcherSweepName,
                "hero_05_watcher_sweep",
                "\u6A2A\u626B\u654C\u65B9\u524D\u6392\u4E24\u540D\u76EE\u6807\uFF0C\u538B\u4F4F\u5BF9\u65B9\u9635\u7EBF\u3002",
                SkillDataType.伤害,
                SkillDataTargetType.前排两敌,
                5,
                0.95f,
                1,
                1,
                null,
                1,
                new[] { 1, 2 },
                new[] { 1, 2 });

            var standGuard = CreateOrUpdateSkill(
                StandGuardName,
                "hero_05_stand_guard",
                "\u8FDB\u5165\u62A4\u536B\u59FF\u6001\uFF0C\u4FDD\u62A4\u76F8\u90BB\u961F\u53CB\u3002",
                SkillDataType.控制,
                SkillDataTargetType.自己,
                0,
                0f,
                1,
                1,
                guardBuff,
                2,
                new[] { 1, 2 },
                new[] { 1, 2, 3, 4 });

            var combatant = AssetDatabase.LoadAssetAtPath<CombatantDefinition>(TargetCombatantPath);
            if (combatant == null)
            {
                Debug.LogError($"Bulwark target combatant not found: {TargetCombatantPath}");
                return;
            }

            combatant.archetype = CombatArchetype.Bulwark;
            combatant.preferredRow = CombatRowPreference.Front;
            combatant.specialization = CombatSpecialization.None;
            combatant.passive = HeroPassive.Fortress;
            combatant.maxHealth = 42;
            combatant.attack = 7;
            combatant.defense = 4;
            combatant.speed = 3;
            combatant.recruitPrice = 600;
            combatant.skills = new[]
            {
                holdTheLine,
                earthbreakRam,
                watcherSweep,
                standGuard
            };

            EditorUtility.SetDirty(combatant);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Bulwark first-pass assets updated for \u5DF4\u5C14\u30FB\u788E\u65A9");
        }

        public static void ExecuteBuildBaalBulwark()
        {
            BuildBaalBulwark();
        }

        private static BuffData CreateOrUpdateBuff(
            string fileName,
            string buffId,
            string description,
            int duration,
            bool stun,
            int tickDamage,
            Color nameTextColor,
            Color damageTextColor)
        {
            var assetPath = $"{BuffsRoot}/{fileName}.asset";
            var buff = AssetDatabase.LoadAssetAtPath<BuffData>(assetPath);
            if (buff == null)
            {
                buff = ScriptableObject.CreateInstance<BuffData>();
                AssetDatabase.CreateAsset(buff, assetPath);
            }

            buff.buffId = buffId;
            buff.buffName = fileName;
            buff.description = description;
            buff.duration = duration;
            buff.stun = stun;
            buff.tickDamage = tickDamage;
            buff.nameTextColor = nameTextColor;
            buff.damageTextColor = damageTextColor;
            EditorUtility.SetDirty(buff);
            return buff;
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
            skill.skillSfx = FindSkillClip(fileName, skillId, "\u65A9", "\u91CD\u51FB", "\u62A4\u536B");
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

        private static void EnsureFolder(string parent, string name)
        {
            var path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
