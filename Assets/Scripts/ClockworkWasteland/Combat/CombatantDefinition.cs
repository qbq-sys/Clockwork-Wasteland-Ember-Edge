using UnityEngine;

namespace ClockworkWasteland.Combat
{
    public enum HeroRecoveryState
    {
        Ready,
        Wounded
    }

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
        public CombatSpecialization specialization = CombatSpecialization.None;

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
        public HeroRecoveryState recoveryState = HeroRecoveryState.Ready;
        [Min(0)] public int recoveryBattlesRemaining;

        [Header("Actions")]
        public SkillData[] skills;

        public int Level => isHero ? Mathf.Max(1, currentLevel) : 1;
        public int Experience => isHero ? Mathf.Max(0, currentExperience) : 0;
        public int ExperienceToNextLevel => Mathf.Max(1, growthData != null ? growthData.experiencePerLevel : DefaultExperiencePerLevel);
        public int GrowthMaxHealthPerLevel => growthData != null ? growthData.maxHealthPerLevel : DefaultMaxHealthPerLevel;
        public int GrowthAttackPerLevel => growthData != null ? growthData.attackPerLevel : DefaultAttackPerLevel;
        public int GrowthDefensePerLevel => growthData != null ? growthData.defensePerLevel : DefaultDefensePerLevel;
        public int ArchetypeMaxHealthBonus => GetArchetypeMaxHealthBonus(archetype);
        public int ArchetypeAttackBonus => GetArchetypeAttackBonus(archetype);
        public int ArchetypeDefenseBonus => GetArchetypeDefenseBonus(archetype);
        public int ArchetypeSpeedBonus => GetArchetypeSpeedBonus(archetype);
        public int SpecializationMaxHealthBonus => GetSpecializationMaxHealthBonus(specialization);
        public int SpecializationAttackBonus => GetSpecializationAttackBonus(specialization);
        public int SpecializationDefenseBonus => GetSpecializationDefenseBonus(specialization);
        public int SpecializationSpeedBonus => GetSpecializationSpeedBonus(specialization);
        public int MaxHealthWithGrowth => maxHealth + (Level - 1) * GrowthMaxHealthPerLevel + ArchetypeMaxHealthBonus + SpecializationMaxHealthBonus;
        public int AttackWithGrowth => attack + (Level - 1) * GrowthAttackPerLevel + ArchetypeAttackBonus + SpecializationAttackBonus;
        public int DefenseWithGrowth => defense + (Level - 1) * GrowthDefensePerLevel + ArchetypeDefenseBonus + SpecializationDefenseBonus;
        public int SpeedWithArchetype => Mathf.Max(0, speed + ArchetypeSpeedBonus + SpecializationSpeedBonus);
        public int CurrentHealth => isHero ? Mathf.Clamp(currentHealth < 0 ? MaxHealthWithGrowth : currentHealth, 0, MaxHealthWithGrowth) : MaxHealthWithGrowth;
        public bool IsDead => isHero && CurrentHealth <= 0;
        public bool IsRecovering => isHero && recoveryState != HeroRecoveryState.Ready;
        public bool CanDeploy => isHero && isUnlocked && !IsDead && !IsRecovering;
        public string ArchetypeDisplayName => GetArchetypeDisplayName(archetype);
        public string PreferredRowDisplayName => GetPreferredRowDisplayName(preferredRow);
        public string ArchetypeSummary => GetArchetypeSummary(archetype);
        public string SpecializationDisplayName => GetSpecializationDisplayName(specialization);
        public string SpecializationSummary => GetSpecializationSummary(specialization);
        public string PassiveDisplayName => GetPassiveDisplayName(passive);
        public string RecoveryDisplayName => GetRecoveryDisplayName(recoveryState, recoveryBattlesRemaining);

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
            specialization = NormalizeSpecialization(archetype, specialization);
            recoveryBattlesRemaining = Mathf.Max(0, recoveryBattlesRemaining);
            if (recoveryState == HeroRecoveryState.Ready)
            {
                recoveryBattlesRemaining = 0;
            }
            if (!isHero)
            {
                currentHealth = -1;
                recoveryState = HeroRecoveryState.Ready;
                recoveryBattlesRemaining = 0;
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

        private static string GetPassiveDisplayName(HeroPassive value)
        {
            switch (value)
            {
                case HeroPassive.Berserker: return "狂战士";
                case HeroPassive.Executioner: return "处决者";
                case HeroPassive.ChainReaction: return "连锁反应";
                case HeroPassive.Backstab: return "背刺";
                case HeroPassive.GlassCannon: return "玻璃大炮";
                case HeroPassive.IronWill: return "铁意志";
                case HeroPassive.Regenerator: return "再生";
                case HeroPassive.ThornArmor: return "荆棘护甲";
                case HeroPassive.Bodyguard: return "保镖";
                case HeroPassive.Fortress: return "堡垒";
                case HeroPassive.Tactician: return "战术家";
                case HeroPassive.Scavenger: return "回收者";
                case HeroPassive.Vanguard: return "先锋";
                case HeroPassive.Reaper: return "收割者";
                case HeroPassive.Inspirer: return "鼓舞者";
                default: return "无";
            }
        }

        public static string GetSpecializationDisplayName(CombatSpecialization value)
        {
            switch (value)
            {
                case CombatSpecialization.Bastion: return "壁垒";
                case CombatSpecialization.Sentinel: return "哨卫";
                case CombatSpecialization.Slayer: return "斩杀号手";
                case CombatSpecialization.Breaker: return "破阵者";
                case CombatSpecialization.Bombardier: return "爆破手";
                case CombatSpecialization.Controller: return "控场师";
                case CombatSpecialization.Surgeon: return "外科医";
                case CombatSpecialization.Stimulator: return "激励师";
                default: return "未专精";
            }
        }

        public static string GetSpecializationSummary(CombatSpecialization value)
        {
            switch (value)
            {
                case CombatSpecialization.Bastion: return "更偏纯防御与前排硬顶。";
                case CombatSpecialization.Sentinel: return "更偏护卫、牵制与节奏维持。";
                case CombatSpecialization.Slayer: return "更偏残血处决与爆发收尾。";
                case CombatSpecialization.Breaker: return "更偏前排破阵与开口能力。";
                case CombatSpecialization.Bombardier: return "更偏高压群攻与后排爆破。";
                case CombatSpecialization.Controller: return "更偏控制、打断与节奏压制。";
                case CombatSpecialization.Surgeon: return "更偏抢救、净化与单体治疗。";
                case CombatSpecialization.Stimulator: return "更偏冷却支援、资源与队友加速。";
                default: return "尚未选择专精分支。";
            }
        }

        private static int GetArchetypeMaxHealthBonus(CombatArchetype value)
        {
            switch (value)
            {
                case CombatArchetype.Bulwark:
                    return 10;
                case CombatArchetype.Physician:
                    return 4;
                default:
                    return 0;
            }
        }

        private static int GetArchetypeAttackBonus(CombatArchetype value)
        {
            switch (value)
            {
                case CombatArchetype.Executioner:
                    return 2;
                case CombatArchetype.Artificer:
                    return 1;
                default:
                    return 0;
            }
        }

        private static int GetArchetypeDefenseBonus(CombatArchetype value)
        {
            switch (value)
            {
                case CombatArchetype.Bulwark:
                    return 2;
                case CombatArchetype.Physician:
                    return 1;
                default:
                    return 0;
            }
        }

        private static int GetArchetypeSpeedBonus(CombatArchetype value)
        {
            switch (value)
            {
                case CombatArchetype.Executioner:
                    return 1;
                case CombatArchetype.Artificer:
                    return 2;
                case CombatArchetype.Bulwark:
                    return -1;
                default:
                    return 0;
            }
        }

        private static int GetSpecializationMaxHealthBonus(CombatSpecialization value)
        {
            switch (value)
            {
                case CombatSpecialization.Bastion:
                    return 4;
                case CombatSpecialization.Surgeon:
                    return 2;
                default:
                    return 0;
            }
        }

        private static int GetSpecializationAttackBonus(CombatSpecialization value)
        {
            switch (value)
            {
                case CombatSpecialization.Slayer:
                case CombatSpecialization.Bombardier:
                    return 1;
                default:
                    return 0;
            }
        }

        private static int GetSpecializationDefenseBonus(CombatSpecialization value)
        {
            switch (value)
            {
                case CombatSpecialization.Bastion:
                    return 2;
                case CombatSpecialization.Sentinel:
                    return 1;
                default:
                    return 0;
            }
        }

        private static int GetSpecializationSpeedBonus(CombatSpecialization value)
        {
            switch (value)
            {
                case CombatSpecialization.Controller:
                case CombatSpecialization.Stimulator:
                    return 1;
                default:
                    return 0;
            }
        }

        private static CombatSpecialization NormalizeSpecialization(CombatArchetype archetype, CombatSpecialization specialization)
        {
            switch (archetype)
            {
                case CombatArchetype.Bulwark:
                    return specialization == CombatSpecialization.Bastion || specialization == CombatSpecialization.Sentinel ? specialization : CombatSpecialization.None;
                case CombatArchetype.Executioner:
                    return specialization == CombatSpecialization.Slayer || specialization == CombatSpecialization.Breaker ? specialization : CombatSpecialization.None;
                case CombatArchetype.Artificer:
                    return specialization == CombatSpecialization.Bombardier || specialization == CombatSpecialization.Controller ? specialization : CombatSpecialization.None;
                case CombatArchetype.Physician:
                    return specialization == CombatSpecialization.Surgeon || specialization == CombatSpecialization.Stimulator ? specialization : CombatSpecialization.None;
                default:
                    return CombatSpecialization.None;
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

        public void MarkWounded(int recoveryBattleCount, float stabilizedHealthPercent = 0.01f)
        {
            if (!isHero)
            {
                return;
            }

            recoveryState = HeroRecoveryState.Wounded;
            recoveryBattlesRemaining = Mathf.Max(1, recoveryBattleCount);
            currentHealth = Mathf.Clamp(
                Mathf.CeilToInt(MaxHealthWithGrowth * Mathf.Clamp(stabilizedHealthPercent, 0.01f, 1f)),
                1,
                MaxHealthWithGrowth);
        }

        public bool AdvanceRecovery(int completedBattles = 1)
        {
            if (!isHero || recoveryState == HeroRecoveryState.Ready || completedBattles <= 0)
            {
                return false;
            }

            recoveryBattlesRemaining = Mathf.Max(0, recoveryBattlesRemaining - completedBattles);
            if (recoveryBattlesRemaining > 0)
            {
                return false;
            }

            recoveryState = HeroRecoveryState.Ready;
            recoveryBattlesRemaining = 0;
            currentHealth = Mathf.Clamp(currentHealth, 1, MaxHealthWithGrowth);
            return true;
        }

        public int RecoverImmediately(float healthPercent)
        {
            if (!isHero || recoveryState == HeroRecoveryState.Ready)
            {
                return 0;
            }

            recoveryState = HeroRecoveryState.Ready;
            recoveryBattlesRemaining = 0;
            currentHealth = Mathf.Clamp(
                Mathf.CeilToInt(MaxHealthWithGrowth * Mathf.Clamp(healthPercent, 0.01f, 1f)),
                1,
                MaxHealthWithGrowth);
            return currentHealth;
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

            recoveryState = HeroRecoveryState.Ready;
            recoveryBattlesRemaining = 0;
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

        private static string GetRecoveryDisplayName(HeroRecoveryState state, int battlesRemaining)
        {
            switch (state)
            {
                case HeroRecoveryState.Wounded:
                    return battlesRemaining > 0 ? $"重伤休养（剩余 {battlesRemaining} 战）" : "重伤休养";
                case HeroRecoveryState.Ready:
                default:
                    return "可出战";
            }
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
