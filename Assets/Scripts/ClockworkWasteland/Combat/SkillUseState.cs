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

            var suffix = string.Empty;
            if (Skill.manaCost > 0 || Skill.cooldown > 0)
            {
                suffix = $" [{(Skill.manaCost > 0 ? $"费{Skill.manaCost}" : "费0")}/{(Skill.cooldown > 0 ? $"冷{Skill.cooldown}" : "冷0")}]";
            }

            return Skill.skillName + suffix;
        }
    }
}
