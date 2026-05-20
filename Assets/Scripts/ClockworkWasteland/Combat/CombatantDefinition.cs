using UnityEngine;

namespace ClockworkWasteland.Combat
{
    [CreateAssetMenu(menuName = "Clockwork Wasteland/Combat/Combatant Definition")]
    public sealed class CombatantDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string characterId = "character";
        public string displayName = "Combatant";
        public bool isHero = true;
        public Sprite portrait;
        public Sprite battleSprite;
        public Sprite[] idleAnimationFrames;
        public Color tint = Color.white;
        public float visualScale = 1f;
        public int occupiedSlotCount = 1;
        public int corpseHealth = 3;

        [Header("Stats")]
        public int maxHealth = 32;
        public int speed = 5;
        public int attack = 8;
        public int defense = 2;

        [Header("Actions")]
        public SkillDefinition[] skills;
    }
}
