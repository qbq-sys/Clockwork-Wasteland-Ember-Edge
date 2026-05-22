using System.Linq;
using UnityEngine;

namespace ClockworkWasteland.Combat
{
    public static class DemoBattleBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureDemoBattleExists()
        {
            if (Object.FindObjectOfType<BattleController>() != null)
            {
                return;
            }

            var controllerObject = new GameObject("Demo Battle Controller");
            controllerObject.AddComponent<BattleController>();
        }

        public static CombatantDefinition[] CreateDefaultHeroes()
        {
            var strike = CreateSkill("iron_cut", "\u94c1\u5203\u65a9", "\u7a33\u5b9a\u7684\u5355\u4f53\u4f24\u5bb3\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 8, null, new[] { 1, 2 }, new[] { 1, 2 });
            var burn = CreateSkill("ember_rend", "\u4f59\u70ec\u5272\u88c2", "\u9020\u6210\u4f24\u5bb3\u5e76\u7559\u4e0b\u707c\u70e7\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 5, CreateBuff("\u707c\u70e7", 3, false, 2), new[] { 1, 2, 3 }, new[] { 1, 2, 3 });
            var heal = CreateSkill("field_stitch", "\u6218\u5730\u7f1d\u5408", "\u6cbb\u7597\u4e00\u540d\u53cb\u65b9\u3002", SkillDataType.治疗, SkillDataTargetType.单友, 7, null, new[] { 2, 3, 4 }, new[] { 1, 2, 3, 4 });
            var volley = CreateSkill("scrap_volley", "\u5e9f\u94c1\u9f50\u5c04", "\u653b\u51fb\u654c\u65b9\u5168\u4f53\u3002", SkillDataType.伤害, SkillDataTargetType.全体敌, 4);
            var guardBreak = CreateSkill("guard_break", "\u7834\u9632\u4e00\u51fb", "\u5bf9\u5355\u4e2a\u654c\u4eba\u9020\u6210\u8f83\u9ad8\u4f24\u5bb3\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 10, null, new[] { 1, 2 }, new[] { 1 });

            return new[]
            {
                ConfigureCombatIdentity(CreateHero("hero_01", "\u82f1\u96c4\u4e00", 38, 4, "Assets/Art/hero_01_idle.png", strike, burn), CombatArchetype.Bulwark, CombatRowPreference.Front),
                ConfigureCombatIdentity(CreateHero("hero_02", "\u82f1\u96c4\u4e8c", 28, 6, "Assets/Art/hero_02_idle.png", strike, heal), CombatArchetype.Physician, CombatRowPreference.Back),
                ConfigureCombatIdentity(CreateHero("hero_03", "\u82f1\u96c4\u4e09", 30, 8, "Assets/Art/hero_03_idle.png", strike, volley), CombatArchetype.Artificer, CombatRowPreference.Back),
                ConfigureCombatIdentity(CreateHero("hero_04", "\u82f1\u96c4\u56db", 34, 5, "Assets/Art/hero_04_idle.png", strike, guardBreak), CombatArchetype.Executioner, CombatRowPreference.Mid)
            };
        }

        public static CombatantDefinition[] CreateHeroPool()
        {
            var strike = CreateSkill("iron_cut", "\u94c1\u5203\u65a9", "\u7a33\u5b9a\u7684\u5355\u4f53\u4f24\u5bb3\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 8, null, new[] { 1, 2 }, new[] { 1, 2 });
            var burn = CreateSkill("ember_rend", "\u4f59\u70ec\u5272\u88c2", "\u9020\u6210\u4f24\u5bb3\u5e76\u7559\u4e0b\u707c\u70e7\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 5, CreateBuff("\u707c\u70e7", 3, false, 2), new[] { 1, 2, 3 }, new[] { 1, 2, 3 });
            var heal = CreateSkill("field_stitch", "\u6218\u5730\u7f1d\u5408", "\u6cbb\u7597\u4e00\u540d\u53cb\u65b9\u3002", SkillDataType.治疗, SkillDataTargetType.单友, 7, null, new[] { 2, 3, 4 }, new[] { 1, 2, 3, 4 });
            var volley = CreateSkill("scrap_volley", "\u5e9f\u94c1\u9f50\u5c04", "\u653b\u51fb\u654c\u65b9\u5168\u4f53\u3002", SkillDataType.伤害, SkillDataTargetType.全体敌, 4);
            var guardBreak = CreateSkill("guard_break", "\u7834\u9632\u4e00\u51fb", "\u5bf9\u5355\u4e2a\u654c\u4eba\u9020\u6210\u8f83\u9ad8\u4f24\u5bb3\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 10, null, new[] { 1, 2 }, new[] { 1 });
            var backstab = CreateSkill("gear_sting", "\u9f7f\u8f6e\u523a", "\u540e\u6392\u51fa\u624b\u7684\u7cbe\u51c6\u523a\u51fb\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 7, null, new[] { 3, 4 }, new[] { 2, 3, 4 });
            var purge = CreateSkill("steam_purge", "\u84b8\u6c7d\u51c0\u5316", "\u6cbb\u7597\u5e76\u7a33\u5b9a\u961f\u4f0d\u3002", SkillDataType.治疗, SkillDataTargetType.单友, 5, null, new[] { 3, 4 }, new[] { 1, 2, 3, 4 });
            var stun = CreateSkill("example_stun", "震荡锁链", "眩晕一名敌人。", SkillDataType.控制, SkillDataTargetType.单敌, 0, CreateBuff("眩晕", 1, true, 0), new[] { 2, 3, 4 }, new[] { 1, 2, 3 });

            return new[]
            {
                ConfigureRecruitment(ConfigureCombatIdentity(CreateHero("hero_01", "\u82f1\u96c4\u4e00", 38, 4, "Assets/Art/hero_01_idle.png", strike, burn), CombatArchetype.Bulwark, CombatRowPreference.Front), true, 0, 8, 2),
                ConfigureRecruitment(ConfigureCombatIdentity(CreateHero("hero_02", "\u82f1\u96c4\u4e8c", 28, 6, "Assets/Art/hero_02_idle.png", strike, heal), CombatArchetype.Physician, CombatRowPreference.Back), true, 0, 8, 2),
                ConfigureRecruitment(ConfigureCombatIdentity(CreateHero("hero_03", "\u82f1\u96c4\u4e09", 30, 8, "Assets/Art/hero_03_idle.png", strike, volley), CombatArchetype.Artificer, CombatRowPreference.Back), true, 0, 8, 2),
                ConfigureRecruitment(ConfigureCombatIdentity(CreateHero("hero_04", "\u82f1\u96c4\u56db", 34, 5, "Assets/Art/hero_04_idle.png", strike, guardBreak), CombatArchetype.Executioner, CombatRowPreference.Mid), true, 0, 9, 3),
                ConfigureRecruitment(ConfigureCombatIdentity(CreateHero("hero_05", "\u94f6\u9f7f\u6e38\u4fa0", 26, 9, "Assets/Art/hero_05_idle.png", backstab, strike), CombatArchetype.Executioner, CombatRowPreference.Mid), false, 500, 11, 2),
                ConfigureRecruitment(ConfigureCombatIdentity(CreateHero("hero_06", "\u949f\u8868\u533b\u5e08", 30, 7, "Assets/Art/hero_02_idle.png", heal, purge, stun), CombatArchetype.Physician, CombatRowPreference.Back), false, 550, 7, 3),
                ConfigureRecruitment(ConfigureCombatIdentity(CreateHero("hero_07", "\u94c1\u80ba\u76fe\u536b", 48, 3, "Assets/Art/hero_03_idle.png", strike, guardBreak), CombatArchetype.Bulwark, CombatRowPreference.Front), false, 650, 8, 6),
                ConfigureRecruitment(ConfigureCombatIdentity(CreateHero("hero_08", "\u7834\u7247\u5de5\u7a0b\u5e08", 32, 6, "Assets/Art/hero_04_idle.png", volley, burn), CombatArchetype.Artificer, CombatRowPreference.Back), false, 700, 10, 3)
            };
        }

        public static CombatantDefinition[] CreateDefaultEnemies()
        {
            var claw = CreateSkill("claw", "\u722a\u51fb", "\u7c97\u66b4\u7684\u8fd1\u8eab\u653b\u51fb\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 6, null, new[] { 1, 2 }, new[] { 1, 2 });
            var cinder = CreateSkill("cinder_bite", "\u7070\u70ec\u6495\u54ac", "\u5e26\u6765\u6301\u7eed\u707c\u70e7\u7684\u6495\u54ac\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 4, CreateBuff("灼烧", 2, false, 2), new[] { 1, 2, 3 }, new[] { 1, 2, 3 });

            return new[]
            {
                CreateEnemy("enemy_01", "\u9508\u8680\u6b8b\u5175", 24, 5, "Assets/Art/hero_01_idle.png", claw),
                CreateEnemy("enemy_02", "\u7070\u70ec\u4fe1\u5f92", 22, 7, "Assets/Art/hero_02_idle.png", cinder),
                CreateEnemy("enemy_03", "\u9aa8\u8f6e\u866b", 18, 9, "Assets/Art/hero_03_idle.png", claw)
            };
        }

        public static CombatantDefinition[] CreateBossEnemies()
        {
            var defaultEnemies = CreateDefaultEnemies();
            var bossSkills = defaultEnemies
                .SelectMany(enemy => enemy.skills ?? new SkillData[0])
                .Where(skill => skill != null)
                .Distinct()
                .Take(2)
                .ToArray();

            var boss = CreateEnemy("boss_clockwork_warden", "\u53d1\u6761\u76d1\u5de5", 64, 6, "Assets/Art/hero_04_idle.png", bossSkills);
            boss.attack = 12;
            boss.defense = 5;
            boss.visualScale = 1.18f;
            boss.tint = new Color(1f, 0.78f, 0.55f, 1f);
            boss.corpseHealth = 8;

            return new[]
            {
                boss,
                defaultEnemies.ElementAtOrDefault(0),
                defaultEnemies.ElementAtOrDefault(1)
            }.Where(enemy => enemy != null).ToArray();
        }

        private static CombatantDefinition CreateHero(string characterId, string displayName, int maxHealth, int speed, string idleSpriteSheetPath, params SkillData[] skills)
        {
            var combatant = CreateCombatant(displayName, true, maxHealth, speed, Color.white, skills);
            combatant.characterId = characterId;
            combatant.idleAnimationFrames = EditorSpriteSheetLoader.LoadSprites(idleSpriteSheetPath);
            combatant.unitPrefabPath = $"Assets/ClockworkWastelandDemo/Prefabs/CombatUnits/{characterId}_Unit.prefab";
            combatant.visualScale = 1f;
            return combatant;
        }

        private static CombatantDefinition CreateEnemy(string characterId, string displayName, int maxHealth, int speed, string idleSpriteSheetPath, params SkillData[] skills)
        {
            var combatant = CreateCombatant(displayName, false, maxHealth, speed, Color.white, skills);
            combatant.characterId = characterId;
            combatant.idleAnimationFrames = EditorSpriteSheetLoader.LoadSprites(idleSpriteSheetPath);
            combatant.unitPrefabPath = $"Assets/ClockworkWastelandDemo/Prefabs/CombatUnits/{characterId}_Unit.prefab";
            combatant.visualScale = 1f;
            return combatant;
        }

        private static CombatantDefinition ConfigureRecruitment(CombatantDefinition hero, bool isUnlocked, int recruitPrice, int attack, int defense, HeroPassive passive = HeroPassive.None)
        {
            hero.isUnlocked = isUnlocked;
            hero.recruitPrice = recruitPrice;
            hero.attack = attack;
            hero.defense = defense;
            hero.passive = passive;
            return hero;
        }

        private static CombatantDefinition ConfigureCombatIdentity(CombatantDefinition combatant, CombatArchetype archetype, CombatRowPreference preferredRow)
        {
            combatant.archetype = archetype;
            combatant.preferredRow = preferredRow;
            return combatant;
        }

        private static SkillData CreateSkill(string skillId, string displayName, string description, SkillDataType skillType, SkillDataTargetType targetType, int baseValue, BuffData applyBuff = null, int[] casterPositions = null, int[] targetPositions = null)
        {
            var configuredSkill = SkillLibrary.Instance.Get(skillId);
            if (configuredSkill != null)
            {
                return configuredSkill;
            }

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = skillId;
            skill.skillName = displayName;
            skill.description = description;
            skill.skillType = skillType;
            skill.targetType = targetType;
            skill.baseValue = baseValue;
            skill.casterAllowedPositions = casterPositions ?? new[] { 1, 2, 3, 4 };
            skill.targetAllowedPositions = targetPositions ?? new[] { 1, 2, 3, 4 };
            skill.applyBuff = applyBuff;
            return skill;
        }

        private static BuffData CreateBuff(string displayName, int duration, bool stun, int tickDamage)
        {
            var buff = ScriptableObject.CreateInstance<BuffData>();
            buff.buffId = displayName;
            buff.buffName = displayName;
            buff.duration = duration;
            buff.stun = stun;
            buff.tickDamage = tickDamage;
            return buff;
        }

        private static CombatantDefinition CreateCombatant(string displayName, bool isHero, int maxHealth, int speed, Color tint, params SkillData[] skills)
        {
            var combatant = ScriptableObject.CreateInstance<CombatantDefinition>();
            combatant.displayName = displayName;
            combatant.isHero = isHero;
            combatant.maxHealth = maxHealth;
            combatant.speed = speed;
            combatant.tint = tint;
            combatant.visualScale = 1f;
            combatant.corpseHealth = 3;
            combatant.skills = skills.Where(skill => skill != null).ToArray();
            return combatant;
        }
    }
}
