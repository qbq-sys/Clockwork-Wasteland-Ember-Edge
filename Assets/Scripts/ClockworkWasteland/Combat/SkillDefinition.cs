using UnityEngine;

namespace ClockworkWasteland.Combat
{
    public sealed class SkillDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string skillId = "skill";
        public string displayName = "Skill";
        [TextArea]
        public string description = "A combat action.";

        [Header("Rules")]
        public SkillEffectType effectType = SkillEffectType.Damage;
        public SkillTargetType targetType = SkillTargetType.SingleEnemy;
        public int[] casterAllowedPositions = { 1, 2, 3, 4 };
        public int[] targetAllowedPositions = { 1, 2, 3, 4 };
        public bool canTargetDead;
        public DamageType damageType = DamageType.Physical;
        public int power = 8;
        public float powerMultiplier = 1f;
        [Range(0, 100)]
        public int accuracy = 90;
        public int actionPointCost = 1;
        public int resourceCost;
        public int cooldownTurns;
        public bool obliteratesTarget;
        public bool isSwapSkill;

        [Header("Presentation")]
        public Sprite attackSprite;
        public Sprite hitSprite;
        [Range(0.05f, 0.5f)]
        public float overlayDuration = 0.2f;

        [Header("Optional Status")]
        public string statusName;
        public int statusDuration;
        public int statusTickDamage;

        public bool AppliesStatus => !string.IsNullOrWhiteSpace(statusName) && statusDuration > 0;
    }
}
