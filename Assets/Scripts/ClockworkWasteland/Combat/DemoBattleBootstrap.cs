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
            var strike = CreateSkill("iron_cut", "\u94c1\u5203\u65a9", "\u7a33\u5b9a\u7684\u5355\u4f53\u4f24\u5bb3\u3002", SkillEffectType.Damage, SkillTargetType.SingleEnemy, 8, 95, null, 0, 0, new[] { 1, 2 }, new[] { 1, 2 }, true);
            var burn = CreateSkill("ember_rend", "\u4f59\u70ec\u5272\u88c2", "\u9020\u6210\u4f24\u5bb3\u5e76\u7559\u4e0b\u707c\u70e7\u3002", SkillEffectType.Damage, SkillTargetType.SingleEnemy, 5, 90, "\u707c\u70e7", 3, 2, new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, true);
            var heal = CreateSkill("field_stitch", "\u6218\u5730\u7f1d\u5408", "\u6cbb\u7597\u4e00\u540d\u53cb\u65b9\u3002", SkillEffectType.Heal, SkillTargetType.SingleAlly, 7, 100, null, 0, 0, new[] { 2, 3, 4 }, new[] { 1, 2, 3, 4 });
            var volley = CreateSkill("scrap_volley", "\u5e9f\u94c1\u9f50\u5c04", "\u653b\u51fb\u654c\u65b9\u5168\u4f53\u3002", SkillEffectType.Damage, SkillTargetType.AllEnemies, 4, 85);
            var guardBreak = CreateSkill("guard_break", "\u7834\u9632\u4e00\u51fb", "\u5bf9\u5355\u4e2a\u654c\u4eba\u9020\u6210\u8f83\u9ad8\u4f24\u5bb3\u3002", SkillEffectType.Damage, SkillTargetType.SingleEnemy, 10, 85, null, 0, 0, new[] { 1, 2 }, new[] { 1 }, true);

            return new[]
            {
                CreateHero("hero_01", "\u82f1\u96c4\u4e00", 38, 4, "Assets/Art/hero_01_idle.png", strike, burn),
                CreateHero("hero_02", "\u82f1\u96c4\u4e8c", 28, 6, "Assets/Art/hero_02_idle.png", strike, heal),
                CreateHero("hero_03", "\u82f1\u96c4\u4e09", 30, 8, "Assets/Art/hero_03_idle.png", strike, volley),
                CreateHero("hero_04", "\u82f1\u96c4\u56db", 34, 5, "Assets/Art/hero_04_idle.png", strike, guardBreak)
            };
        }

        public static CombatantDefinition[] CreateHeroPool()
        {
            var strike = CreateSkill("iron_cut", "\u94c1\u5203\u65a9", "\u7a33\u5b9a\u7684\u5355\u4f53\u4f24\u5bb3\u3002", SkillEffectType.Damage, SkillTargetType.SingleEnemy, 8, 95, null, 0, 0, new[] { 1, 2 }, new[] { 1, 2 }, true);
            var burn = CreateSkill("ember_rend", "\u4f59\u70ec\u5272\u88c2", "\u9020\u6210\u4f24\u5bb3\u5e76\u7559\u4e0b\u707c\u70e7\u3002", SkillEffectType.Damage, SkillTargetType.SingleEnemy, 5, 90, "\u707c\u70e7", 3, 2, new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, true);
            var heal = CreateSkill("field_stitch", "\u6218\u5730\u7f1d\u5408", "\u6cbb\u7597\u4e00\u540d\u53cb\u65b9\u3002", SkillEffectType.Heal, SkillTargetType.SingleAlly, 7, 100, null, 0, 0, new[] { 2, 3, 4 }, new[] { 1, 2, 3, 4 });
            var volley = CreateSkill("scrap_volley", "\u5e9f\u94c1\u9f50\u5c04", "\u653b\u51fb\u654c\u65b9\u5168\u4f53\u3002", SkillEffectType.Damage, SkillTargetType.AllEnemies, 4, 85);
            var guardBreak = CreateSkill("guard_break", "\u7834\u9632\u4e00\u51fb", "\u5bf9\u5355\u4e2a\u654c\u4eba\u9020\u6210\u8f83\u9ad8\u4f24\u5bb3\u3002", SkillEffectType.Damage, SkillTargetType.SingleEnemy, 10, 85, null, 0, 0, new[] { 1, 2 }, new[] { 1 }, true);
            var backstab = CreateSkill("gear_sting", "\u9f7f\u8f6e\u523a", "\u540e\u6392\u51fa\u624b\u7684\u7cbe\u51c6\u523a\u51fb\u3002", SkillEffectType.Damage, SkillTargetType.SingleEnemy, 7, 92, null, 0, 0, new[] { 3, 4 }, new[] { 2, 3, 4 }, true);
            var purge = CreateSkill("steam_purge", "\u84b8\u6c7d\u51c0\u5316", "\u6cbb\u7597\u5e76\u7a33\u5b9a\u961f\u4f0d\u3002", SkillEffectType.Heal, SkillTargetType.SingleAlly, 5, 100, null, 0, 0, new[] { 3, 4 }, new[] { 1, 2, 3, 4 });

            return new[]
            {
                CreateHero("hero_01", "\u82f1\u96c4\u4e00", 38, 4, "Assets/Art/hero_01_idle.png", strike, burn),
                CreateHero("hero_02", "\u82f1\u96c4\u4e8c", 28, 6, "Assets/Art/hero_02_idle.png", strike, heal),
                CreateHero("hero_03", "\u82f1\u96c4\u4e09", 30, 8, "Assets/Art/hero_03_idle.png", strike, volley),
                CreateHero("hero_04", "\u82f1\u96c4\u56db", 34, 5, "Assets/Art/hero_04_idle.png", strike, guardBreak),
                CreateHero("hero_05", "\u94f6\u9f7f\u6e38\u4fa0", 26, 9, "Assets/Art/hero_01_idle.png", backstab, strike),
                CreateHero("hero_06", "\u949f\u8868\u533b\u5e08", 24, 7, "Assets/Art/hero_02_idle.png", heal, purge)
            };
        }

        public static CombatantDefinition[] CreateDefaultEnemies()
        {
            var claw = CreateSkill("claw", "\u722a\u51fb", "\u7c97\u66b4\u7684\u8fd1\u8eab\u653b\u51fb\u3002", SkillEffectType.Damage, SkillTargetType.SingleEnemy, 6, 88, null, 0, 0, new[] { 1, 2 }, new[] { 1, 2 }, true);
            var cinder = CreateSkill("cinder_bite", "\u7070\u70ec\u6495\u54ac", "\u5e26\u6765\u6301\u7eed\u707c\u70e7\u7684\u6495\u54ac\u3002", SkillEffectType.Damage, SkillTargetType.SingleEnemy, 4, 86, "\u707c\u70e7", 2, 2, new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, true);

            return new[]
            {
                CreateEnemy("enemy_01", "\u9508\u8680\u6b8b\u5175", 24, 5, "Assets/Art/hero_01_idle.png", claw),
                CreateEnemy("enemy_02", "\u7070\u70ec\u4fe1\u5f92", 22, 7, "Assets/Art/hero_02_idle.png", cinder),
                CreateEnemy("enemy_03", "\u9aa8\u8f6e\u866b", 18, 9, "Assets/Art/hero_03_idle.png", claw)
            };
        }

        private static CombatantDefinition CreateHero(string characterId, string displayName, int maxHealth, int speed, string idleSpriteSheetPath, params SkillDefinition[] skills)
        {
            var combatant = CreateCombatant(displayName, true, maxHealth, speed, Color.white, skills);
            combatant.characterId = characterId;
            combatant.idleAnimationFrames = EditorSpriteSheetLoader.LoadSprites(idleSpriteSheetPath);
            combatant.visualScale = 1f;
            return combatant;
        }

        private static CombatantDefinition CreateEnemy(string characterId, string displayName, int maxHealth, int speed, string idleSpriteSheetPath, params SkillDefinition[] skills)
        {
            var combatant = CreateCombatant(displayName, false, maxHealth, speed, Color.white, skills);
            combatant.characterId = characterId;
            combatant.idleAnimationFrames = EditorSpriteSheetLoader.LoadSprites(idleSpriteSheetPath);
            combatant.visualScale = 1f;
            return combatant;
        }

        private static SkillDefinition CreateSkill(string skillId, string displayName, string description, SkillEffectType effectType, SkillTargetType targetType, int power, int accuracy, string statusName = null, int statusDuration = 0, int statusTickDamage = 0, int[] casterPositions = null, int[] targetPositions = null, bool canTargetDead = false)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            skill.skillId = skillId;
            skill.displayName = displayName;
            skill.description = description;
            skill.effectType = effectType;
            skill.targetType = targetType;
            skill.power = power;
            skill.accuracy = accuracy;
            skill.casterAllowedPositions = casterPositions ?? new[] { 1, 2, 3, 4 };
            skill.targetAllowedPositions = targetPositions ?? new[] { 1, 2, 3, 4 };
            skill.canTargetDead = canTargetDead;
            skill.statusName = statusName;
            skill.statusDuration = statusDuration;
            skill.statusTickDamage = statusTickDamage;
            return skill;
        }

        private static CombatantDefinition CreateCombatant(string displayName, bool isHero, int maxHealth, int speed, Color tint, params SkillDefinition[] skills)
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
