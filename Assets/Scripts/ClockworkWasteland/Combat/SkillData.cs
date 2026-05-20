using UnityEngine;

namespace ClockworkWasteland.Combat
{
    [CreateAssetMenu(menuName = "Clockwork Wasteland/Combat/Skill Data")]
    public sealed class SkillData : ScriptableObject
    {
        public string skillId = "skill";
        public string skillName = "Skill";
        [TextArea]
        public string description = "A combat action.";
        public Sprite icon;
        public SkillDataType skillType = SkillDataType.伤害;
        public int baseValue = 8;
        public float powerMultiplier = 1f;
        public SkillDataTargetType targetType = SkillDataTargetType.单敌;
        public int[] casterAllowedPositions = { 1, 2, 3, 4 };
        public int[] targetAllowedPositions = { 1, 2, 3, 4 };
        public int manaCost;
        public int cooldown;
        public BuffData applyBuff;

        [Header("Presentation")]
        public Sprite attackSprite;
        public Sprite hitSprite;
        [Range(0.05f, 0.5f)]
        public float overlayDuration = 0.2f;

        [HideInInspector]
        public bool isSwapSkill;
    }
}
