using UnityEngine;

namespace ClockworkWasteland.Combat
{
    public readonly struct HeroProgressionResult
    {
        public HeroProgressionResult(int experienceGained, int levelsGained, int levelBefore, int levelAfter, int healthRestoredFromGrowth)
        {
            ExperienceGained = experienceGained;
            LevelsGained = levelsGained;
            LevelBefore = levelBefore;
            LevelAfter = levelAfter;
            HealthRestoredFromGrowth = healthRestoredFromGrowth;
        }

        public int ExperienceGained { get; }
        public int LevelsGained { get; }
        public int LevelBefore { get; }
        public int LevelAfter { get; }
        public int HealthRestoredFromGrowth { get; }
    }

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
        public CombatantView unitPrefab;
        public string unitPrefabPath;
        public Color tint = Color.white;
        public float visualScale = 1f;
        public int occupiedSlotCount = 1;
        public int corpseHealth = 3;
        public CombatArchetype archetype = CombatArchetype.Undefined;
        public CombatRowPreference preferredRow = CombatRowPreference.Flexible;

        [Header("Recruitment")]
        public bool isUnlocked = true;
        public int recruitPrice = 500;

        [Header("Stats")]
        public int maxHealth = 32;
        public int speed = 5;
        public int attack = 8;
        public int defense = 2;

        [Header("Passive")]
        public HeroPassive passive = HeroPassive.None;

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
        public string ArchetypeDisplayName => GetArchetypeDisplayName(archetype);
        public string PreferredRowDisplayName => GetPreferredRowDisplayName(preferredRow);
        public string ArchetypeSummary => GetArchetypeSummary(archetype);

        public bool PrefersFrontRows => preferredRow == CombatRowPreference.Front;
        public bool PrefersBackRows => preferredRow == CombatRowPreference.Back;

        private void OnValidate()
        {
            characterId = string.IsNullOrWhiteSpace(characterId) ? "character" : characterId.Trim();
            displayName = string.IsNullOrWhiteSpace(displayName) ? "Combatant" : displayName.Trim();
            visualScale = Mathf.Max(0.1f, visualScale);
            occupiedSlotCount = Mathf.Max(1, occupiedSlotCount);
            corpseHealth = Mathf.Max(0, corpseHealth);
            recruitPrice = Mathf.Max(0, recruitPrice);
            maxHealth = Mathf.Max(1, maxHealth);
            speed = Mathf.Max(0, speed);
            attack = Mathf.Max(0, attack);
            defense = Mathf.Max(0, defense);
            currentLevel = Mathf.Max(1, currentLevel);
            currentExperience = Mathf.Max(0, currentExperience);
            if (!isHero)
            {
                currentHealth = -1;
            }
        }

        private static string GetArchetypeDisplayName(CombatArchetype value)
        {
            switch (value)
            {
                case CombatArchetype.Bulwark:
                    return "\u5b88\u536b\u8005";
                case CombatArchetype.Executioner:
                    return "\u5904\u5211\u8005";
                case CombatArchetype.Artificer:
                    return "\u6280\u5e08";
                case CombatArchetype.Physician:
                    return "\u533b\u7597\u5e08";
                default:
                    return "\u672a\u5b9a\u4e49";
            }
        }

        private static string GetPreferredRowDisplayName(CombatRowPreference value)
        {
            switch (value)
            {
                case CombatRowPreference.Front:
                    return "\u524d\u6392";
                case CombatRowPreference.Mid:
                    return "\u4e2d\u6392";
                case CombatRowPreference.Back:
                    return "\u540e\u6392";
                default:
                    return "\u7075\u6d3b";
            }
        }

        private static string GetArchetypeSummary(CombatArchetype value)
        {
            switch (value)
            {
                case CombatArchetype.Bulwark:
                    return "前排防御更稳，压制前排目标。";
                case CombatArchetype.Executioner:
                    return "专注终结残血目标，单体伤害更强。";
                case CombatArchetype.Artificer:
                    return "擅长控场与后排打击，多目标收益更高。";
                case CombatArchetype.Physician:
                    return "治疗和维持队伍节奏更强，输出相对保守。";
                default:
                    return "尚未定义战斗原型。";
            }
        }

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
            return GrantExperienceReward(amount).LevelsGained;
        }

        public HeroProgressionResult GrantExperienceReward(int amount)
        {
            if (!isHero || amount <= 0)
            {
                return new HeroProgressionResult(0, 0, Level, Level, 0);
            }

            EnsureRuntimeHealth();
            var levelBefore = Level;
            var healthBefore = CurrentHealth;
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

            return new HeroProgressionResult(amount, levelsGained, levelBefore, Level, Mathf.Max(0, CurrentHealth - healthBefore));
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
