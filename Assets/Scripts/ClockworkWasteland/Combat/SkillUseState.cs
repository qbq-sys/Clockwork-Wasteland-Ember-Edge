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
            ? BuildEnabledLabel()
            : $"{BuildEnabledLabel()}\n{DisabledReason}";

        private string BuildEnabledLabel()
        {
            if (Skill == null)
            {
                return string.Empty;
            }

            var suffix = Skill.cooldown > 0 ? $" [冷{Skill.cooldown}]" : string.Empty;
            return Skill.skillName + suffix;
        }
    }
}
