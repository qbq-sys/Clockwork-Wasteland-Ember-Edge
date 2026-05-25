using System;
using System.Collections.Generic;

namespace ClockworkWasteland.Combat
{
    public enum LevelUpOptionType
    {
        Specialization,
        Passive,
        SkillUpgrade
    }

    public sealed class LevelUpOptionData
    {
        public LevelUpOptionData(
            string optionId,
            LevelUpOptionType optionType,
            string title,
            string subtitle,
            string summary,
            string detail,
            IReadOnlyList<string> tags,
            CombatSpecialization specialization = CombatSpecialization.None,
            HeroPassive passive = HeroPassive.None,
            string skillId = null)
        {
            OptionId = optionId ?? string.Empty;
            OptionType = optionType;
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
            Summary = summary ?? string.Empty;
            Detail = detail ?? string.Empty;
            Tags = tags ?? Array.Empty<string>();
            Specialization = specialization;
            Passive = passive;
            SkillId = skillId ?? string.Empty;
        }

        public string OptionId { get; }
        public LevelUpOptionType OptionType { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string Summary { get; }
        public string Detail { get; }
        public IReadOnlyList<string> Tags { get; }
        public CombatSpecialization Specialization { get; }
        public HeroPassive Passive { get; }
        public string SkillId { get; }
    }

    public readonly struct LevelUpPresentation
    {
        public LevelUpPresentation(
            CombatantDefinition hero,
            string title,
            string subtitle,
            IReadOnlyList<LevelUpOptionData> options)
        {
            Hero = hero;
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
            Options = options ?? Array.Empty<LevelUpOptionData>();
        }

        public CombatantDefinition Hero { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public IReadOnlyList<LevelUpOptionData> Options { get; }
    }

    public sealed partial class LevelUpUI : CombatUIScreen
    {
    }
}
