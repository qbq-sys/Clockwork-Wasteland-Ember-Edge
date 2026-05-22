namespace ClockworkWasteland.Combat
{
    public enum SkillEffectType
    {
        Damage,
        Heal
    }

    public enum SkillTargetType
    {
        SingleEnemy = 0,
        SingleAlly = 1,
        AllEnemies = 3,
        AllAllies = 4,
        Self = 2,
    }

    public enum DamageType
    {
        Physical,
        Witchcraft,
        Mechanical,
        Healing
    }

    public enum SkillDataType
    {
        伤害,
        治疗,
        控制
    }

    public enum SkillDataTargetType
    {
        单敌,
        单友,
        前排两敌,
        全体敌,
        自己
    }

    public enum HeroPassive
    {
        None,

        // --- 进攻型 ---
        Berserker,       // 血量低于50%时，攻击力+30%
        Executioner,     // 攻击血量低于30%的敌人时，伤害+50%
        ChainReaction,   // 击杀敌人后，对随机另一敌人造成本次伤害50%的溅射
        Backstab,        // 从后排攻击前排敌人时，伤害+25%
        GlassCannon,     // 攻击力+25%，但防御力-50%

        // --- 防御型 ---
        IronWill,        // 每场战斗首次受到致命伤害时，保留1点HP
        Regenerator,     // 回合开始时，恢复最大生命值5%
        ThornArmor,      // 受到伤害时，反弹20%给攻击者
        Bodyguard,       // 相邻队友受到攻击时，承担30%伤害
        Fortress,        // 位于前排时，防御力+4

        // --- 辅助型 ---
        Tactician,       // 回合开始时，随机减少1个队友技能冷却1回合
        Scavenger,       // 击杀敌人时，恢复20%最大生命值
        Vanguard,        // 位于前排时，全体队友攻击力+2
        Reaper,          // 每有一个敌人死亡，攻击力+10%（可叠加）
        Inspirer,        // 回合开始时，恢复全体队友10%最大生命值
    }

    public enum CombatArchetype
    {
        Undefined,
        Bulwark,
        Executioner,
        Artificer,
        Physician
    }

    public enum CombatRowPreference
    {
        Flexible,
        Front,
        Mid,
        Back
    }
}
