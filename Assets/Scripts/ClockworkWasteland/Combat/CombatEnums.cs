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
}
