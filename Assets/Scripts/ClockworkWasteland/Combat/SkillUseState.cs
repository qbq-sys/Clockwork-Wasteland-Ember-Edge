namespace ClockworkWasteland.Combat
{
    public readonly struct SkillUseState
    {
        public SkillUseState(SkillDefinition skill, bool canUse, string disabledReason)
        {
            Skill = skill;
            CanUse = canUse;
            DisabledReason = disabledReason;
        }

        public SkillDefinition Skill { get; }
        public bool CanUse { get; }
        public string DisabledReason { get; }

        public string ButtonLabel => CanUse || string.IsNullOrWhiteSpace(DisabledReason)
            ? Skill.displayName
            : $"{Skill.displayName}\n{DisabledReason}";
    }
}
