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

        [Header("Recruitment")]
        public bool isUnlocked = true;
        public int recruitPrice = 500;

        [Header("Stats")]
        public int maxHealth = 32;
        public int speed = 5;
        public int attack = 8;
        public int defense = 2;

        [Header("Growth")]
        public HeroGrowthData growthData;
        [Min(1)] public int currentLevel = 1;
        [Min(0)] public int currentExperience;
        public int currentHealth = -1;

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
        public int CurrentHealth => isHero ? Mathf.Clamp(currentHealth < 0 ? MaxHealthWithGrowth : currentHealth, 0, MaxHealthWithGrowth) : MaxHealthWithGrowth;
        public bool IsDead => isHero && CurrentHealth <= 0;

        public void EnsureRuntimeHealth()
        {
            if (!isHero || currentHealth < 0)
            {
                currentHealth = MaxHealthWithGrowth;
            }
            else
            {
                currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealthWithGrowth);
            }
        }

        public void SetRuntimeHealth(int amount)
        {
            if (isHero)
            {
                currentHealth = Mathf.Clamp(amount, 0, MaxHealthWithGrowth);
            }
        }

        public int HealOutsideBattle(int amount)
        {
            if (!isHero || amount <= 0 || IsDead)
            {
                return 0;
            }

            var before = CurrentHealth;
            currentHealth = Mathf.Min(MaxHealthWithGrowth, before + amount);
            return currentHealth - before;
        }

        public int ReviveOutsideBattle(float healthPercent)
        {
            if (!isHero || !IsDead)
            {
                return 0;
            }

            currentHealth = Mathf.Clamp(Mathf.CeilToInt(MaxHealthWithGrowth * Mathf.Clamp01(healthPercent)), 1, MaxHealthWithGrowth);
            return currentHealth;
        }

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
                currentHealth = Mathf.Clamp(currentHealth + GrowthMaxHealthPerLevel, 0, MaxHealthWithGrowth);
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

    public enum InventoryItemEffectType
    {
        Heal,
        Revive
    }

    [CreateAssetMenu(menuName = "Clockwork Wasteland/Combat/Inventory Item Data")]
    public sealed class InventoryItemData : ScriptableObject
    {
        public string itemId = "item";
        public string itemName = "Item";
        [TextArea]
        public string description = "A usable item.";
        public Sprite icon;
        public int price = 100;
        public InventoryItemEffectType effectType = InventoryItemEffectType.Heal;
        public int healAmount = 20;
        [Range(0.01f, 1f)]
        public float reviveHealthPercent = 0.3f;
    }
}
