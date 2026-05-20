namespace ClockworkWasteland.Combat
{
    public readonly struct SkillUseState
    {
        public SkillUseState(SkillData skill, bool canUse, string disabledReason)
        {
            Skill = skill;
            CanUse = canUse;
            DisabledReason = disabledReason;
        }

        public SkillData Skill { get; }
        public bool CanUse { get; }
        public string DisabledReason { get; }

        public string ButtonLabel => CanUse || string.IsNullOrWhiteSpace(DisabledReason)
            ? Skill.skillName
            : $"{Skill.skillName}\n{DisabledReason}";
    }
}
