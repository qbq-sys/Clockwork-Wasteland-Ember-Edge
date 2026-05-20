using UnityEngine;

namespace ClockworkWasteland.Combat
{
    [CreateAssetMenu(menuName = "Clockwork Wasteland/Combat/Combatant Definition")]
    public sealed class CombatantDefinition : ScriptableObject
    {
        private const int DefaultExperiencePerLevel = 100;
        private const int DefaultMaxHealthPerLevel = 5;
        private const int DefaultAttackPerLevel = 2;
        private const int DefaultDefensePerLevel = 1;

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

        [Header("Growth")]
        public HeroGrowthData growthData;
        [Min(1)] public int currentLevel = 1;
        [Min(0)] public int currentExperience;

        [Header("Actions")]
        public SkillData[] skills;

        public int Level => isHero ? Mathf.Max(1, currentLevel) : 1;
        public int Experience => isHero ? Mathf.Max(0, currentExperience) : 0;
        public int ExperienceToNextLevel => Mathf.Max(1, growthData != null ? growthData.experiencePerLevel : DefaultExperiencePerLevel);
        public int GrowthMaxHealthPerLevel => growthData != null ? growthData.maxHealthPerLevel : DefaultMaxHealthPerLevel;
        public int GrowthAttackPerLevel => growthData != null ? growthData.attackPerLevel : DefaultAttackPerLevel;
        public int GrowthDefensePerLevel => growthData != null ? growthData.defensePerLevel : DefaultDefensePerLevel;
        public int MaxHealthWithGrowth => maxHealth + (Level - 1) * GrowthMaxHealthPerLevel;
        public int AttackWithGrowth => attack + (Level - 1) * GrowthAttackPerLevel;
        public int DefenseWithGrowth => defense + (Level - 1) * GrowthDefensePerLevel;

        public int AddExperience(int amount)
        {
            if (!isHero || amount <= 0)
            {
                return 0;
            }

            currentExperience += amount;
            var levelsGained = 0;
            var requiredExperience = ExperienceToNextLevel;
            while (currentExperience >= requiredExperience)
            {
                currentExperience -= requiredExperience;
                currentLevel = Mathf.Max(1, currentLevel + 1);
                levelsGained++;
                requiredExperience = ExperienceToNextLevel;
            }

            return levelsGained;
        }
    }

    [CreateAssetMenu(menuName = "Clockwork Wasteland/Combat/Hero Growth Data")]
    public sealed class HeroGrowthData : ScriptableObject
    {
        public int experiencePerLevel = 100;
        public int maxHealthPerLevel = 5;
        public int attackPerLevel = 2;
        public int defensePerLevel = 1;
    }
}
