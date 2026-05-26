using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        public Image levelUpBg;
        public Image heroCardBg;
        public Image optionCardBg;

        private RectTransform rootPanel;
        private Text titleText;
        private Text subtitleText;
        private Text heroNameText;
        private Text heroMetaText;
        private Text heroStatsText;
        private Text optionsTitleText;
        private Image portraitImage;
        private readonly List<RectTransform> optionCards = new List<RectTransform>();

        public override void BuildLayout()
        {
            // Prefab-driven screen. Runtime must not rebuild layout or override authored positions.
        }

        public void Show(LevelUpPresentation presentation, Action<LevelUpOptionData> onSelect)
        {
            EnsureBindings();
            gameObject.SetActive(true);

            titleText.text = string.IsNullOrWhiteSpace(presentation.Title) ? "升级选择" : presentation.Title;
            subtitleText.text = string.IsNullOrWhiteSpace(presentation.Subtitle) ? "选择一项成长方向" : presentation.Subtitle;
            heroNameText.text = presentation.Hero != null ? presentation.Hero.displayName : "角色";
            heroMetaText.text = BuildHeroMetaLine(presentation.Hero);
            heroStatsText.text = BuildHeroStatsBlock(presentation.Hero);
            optionsTitleText.text = "本次可选成长";

            if (portraitImage != null)
            {
                portraitImage.sprite = presentation.Hero != null && presentation.Hero.portrait != null
                    ? presentation.Hero.portrait
                    : presentation.Hero != null ? presentation.Hero.battleSprite : null;
                portraitImage.enabled = portraitImage.sprite != null;
            }

            var options = presentation.Options ?? Array.Empty<LevelUpOptionData>();
            for (var i = 0; i < optionCards.Count; i++)
            {
                var active = i < options.Count;
                optionCards[i].gameObject.SetActive(active);
                if (active)
                {
                    BindOptionCard(optionCards[i], options[i], onSelect);
                }
            }
        }

        private void EnsureBindings()
        {
            if (rootPanel != null)
            {
                return;
            }

            rootPanel = FindRequiredRectTransform("LevelUpPanel");
            levelUpBg = levelUpBg != null ? levelUpBg : rootPanel.GetComponent<Image>();
            heroCardBg = heroCardBg != null ? heroCardBg : FindRequiredImage("HeroCard");
            optionCardBg = optionCardBg != null ? optionCardBg : FindRequiredImage("OptionCard_0");
            titleText = FindRequiredText("Title");
            subtitleText = FindRequiredText("Subtitle");
            heroNameText = FindRequiredText("HeroName");
            heroMetaText = FindRequiredText("HeroMeta");
            heroStatsText = FindRequiredText("HeroStats");
            optionsTitleText = FindRequiredText("OptionsTitle");
            portraitImage = FindRequiredImage("Portrait");

            optionCards.Clear();
            var slotIndex = 0;
            while (true)
            {
                var card = FindRectTransform($"OptionCard_{slotIndex}");
                if (card == null)
                {
                    break;
                }

                optionCards.Add(card);
                slotIndex++;
            }
        }

        private void BindOptionCard(RectTransform card, LevelUpOptionData option, Action<LevelUpOptionData> onSelect)
        {
            var badge = FindRequiredImage("TypeBadge", card);
            var badgeText = FindRequiredText("TypeLabel", card);
            var title = FindRequiredText("OptionTitle", card);
            var subtitle = FindRequiredText("OptionSubtitle", card);
            var summary = FindRequiredText("OptionSummary", card);
            var tags = FindRequiredText("OptionTags", card);
            var detail = FindRequiredText("OptionDetail", card);
            var button = FindRequiredButton("RuntimeButton_选择", card);

            badge.color = GetOptionBadgeColor(option.OptionType);
            badgeText.text = GetOptionTypeLabel(option.OptionType);
            title.text = option.Title;
            subtitle.text = option.Subtitle;
            summary.text = string.IsNullOrWhiteSpace(option.Summary) ? "效果摘要待补充" : option.Summary;
            tags.text = option.Tags != null && option.Tags.Count > 0
                ? string.Join("  /  ", option.Tags)
                : "成长";
            detail.text = string.IsNullOrWhiteSpace(option.Detail) ? "详细说明待补充" : option.Detail;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                onSelect?.Invoke(option);
            });
        }

        private static string BuildHeroMetaLine(CombatantDefinition hero)
        {
            if (hero == null)
            {
                return "职能 未定义  /  等级 1";
            }

            return $"{hero.ArchetypeDisplayName}  /  {hero.SpecializationDisplayName}  /  Lv.{hero.Level}";
        }

        private static string BuildHeroStatsBlock(CombatantDefinition hero)
        {
            if (hero == null)
            {
                return "生命 --\n攻击 --\n防御 --\n速度 --";
            }

            return
                $"偏好站位：{hero.PreferredRowDisplayName}\n" +
                $"生命：{hero.CurrentHealth}/{hero.MaxHealthWithGrowth}\n" +
                $"攻击：{hero.AttackWithGrowth}\n" +
                $"防御：{hero.DefenseWithGrowth}\n" +
                $"速度：{hero.SpeedWithArchetype}\n" +
                $"经验：{hero.Experience}/{hero.ExperienceToNextLevel}";
        }

        private static string GetOptionTypeLabel(LevelUpOptionType optionType)
        {
            switch (optionType)
            {
                case LevelUpOptionType.Specialization:
                    return "专精分支";
                case LevelUpOptionType.Passive:
                    return "被动成长";
                case LevelUpOptionType.SkillUpgrade:
                    return "技能强化";
                default:
                    return "成长选项";
            }
        }

        private static Color GetOptionBadgeColor(LevelUpOptionType optionType)
        {
            switch (optionType)
            {
                case LevelUpOptionType.Specialization:
                    return new Color(0.42f, 0.18f, 0.12f, 1f);
                case LevelUpOptionType.Passive:
                    return new Color(0.18f, 0.28f, 0.18f, 1f);
                case LevelUpOptionType.SkillUpgrade:
                    return new Color(0.16f, 0.2f, 0.34f, 1f);
                default:
                    return new Color(0.24f, 0.2f, 0.16f, 1f);
            }
        }

        private RectTransform FindRequiredRectTransform(string name)
        {
            var result = FindRectTransform(name);
            if (result == null)
            {
                throw new MissingReferenceException($"LevelUpUI prefab 缺少节点：{name}");
            }

            return result;
        }

        private RectTransform FindRectTransform(string name, Transform scope = null)
        {
            var transforms = scope != null ? scope.GetComponentsInChildren<Transform>(true) : GetComponentsInChildren<Transform>(true);
            foreach (var current in transforms)
            {
                if (current.name == name)
                {
                    return current as RectTransform;
                }
            }

            return null;
        }

        private Image FindRequiredImage(string name, Transform scope = null)
        {
            var transform = FindRectTransform(name, scope);
            var image = transform != null ? transform.GetComponent<Image>() : null;
            if (image == null)
            {
                throw new MissingReferenceException($"LevelUpUI prefab 缺少 Image 节点：{name}");
            }

            return image;
        }

        private Text FindRequiredText(string name, Transform scope = null)
        {
            var transform = FindRectTransform(name, scope);
            var text = transform != null ? transform.GetComponent<Text>() : null;
            if (text == null)
            {
                throw new MissingReferenceException($"LevelUpUI prefab 缺少 Text 节点：{name}");
            }

            return text;
        }

        private Button FindRequiredButton(string name, Transform scope = null)
        {
            var transform = FindRectTransform(name, scope);
            var button = transform != null ? transform.GetComponent<Button>() : null;
            if (button == null)
            {
                throw new MissingReferenceException($"LevelUpUI prefab 缺少 Button 节点：{name}");
            }

            return button;
        }
    }
}
