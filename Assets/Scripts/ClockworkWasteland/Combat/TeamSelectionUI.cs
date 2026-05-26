using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class TeamSelectionUI : CombatUIScreen
    {
        [Serializable]
        private sealed class HeroCardBinding
        {
            public RectTransform root;
            public Image background;
            public Image portraitImage;
            public Text nameText;
            public Text statsText;
            public Button toggleButton;
            public Text toggleButtonText;
        }

        [SerializeField] public Image teamSelectionBg;
        [SerializeField] public Image heroCardBg;

        [Header("Optional Explicit References")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text lineupText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button startBattleButton;
        [SerializeField] private List<HeroCardBinding> heroCards = new List<HeroCardBinding>();

        public override void BuildLayout()
        {
            // Prefab-driven screen. Runtime must not rebuild layout or override authored positions.
        }

        public void Show(IReadOnlyList<CombatantDefinition> heroPool, IReadOnlyList<CombatantDefinition> selectedHeroes, Action<CombatantDefinition> onToggleHero, Action onStartBattle, Action onBack)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("TeamSelectionUI prefab layout is incomplete. Runtime layout rebuilding is disabled; repair the prefab instead.", this);
                return;
            }

            var selectedList = selectedHeroes ?? Array.Empty<CombatantDefinition>();
            var pool = heroPool ?? Array.Empty<CombatantDefinition>();

            if (titleText != null)
            {
                titleText.text = "队伍配置";
            }

            if (subtitleText != null)
            {
                subtitleText.text = $"选择最多 4 名英雄出战（已选 {selectedList.Count}/4）";
            }

            if (lineupText != null)
            {
                lineupText.text = selectedList.Count == 0
                    ? "当前队伍：未选择"
                    : "当前队伍：" + string.Join(" / ", selectedList.Select(hero => hero.displayName));
            }

            BindButton(backButton, onBack, "返回地图", true);
            BindButton(startBattleButton, selectedList.Count > 0 ? onStartBattle : null, "开始战斗", true);

            for (var i = 0; i < heroCards.Count; i++)
            {
                var card = heroCards[i];
                if (card?.root == null)
                {
                    continue;
                }

                if (i >= pool.Count)
                {
                    card.root.gameObject.SetActive(false);
                    continue;
                }

                card.root.gameObject.SetActive(true);
                BindHeroCard(card, pool[i], selectedList.Contains(pool[i]), onToggleHero);
            }
        }

        private bool TryBindExistingLayout()
        {
            if (teamSelectionBg == null)
            {
                var panel = transform.Find("TeamSelectionPanel");
                if (panel != null)
                {
                    teamSelectionBg = panel.GetComponent<Image>();
                }
            }

            if (titleText == null)
            {
                titleText = FindTextByName("Title");
            }

            if (subtitleText == null)
            {
                subtitleText = FindTextByName("Subtitle");
            }

            if (lineupText == null)
            {
                lineupText = FindTextByName("Lineup");
            }

            if (backButton == null)
            {
                backButton = FindButtonByPreferredName("RuntimeButton_返回地图") ?? FindButtonByLabel("返回地图");
            }

            if (startBattleButton == null)
            {
                startBattleButton = FindButtonByPreferredName("RuntimeButton_开始战斗") ?? FindButtonByLabel("开始战斗");
            }

            if (heroCards == null)
            {
                heroCards = new List<HeroCardBinding>();
            }

            if (heroCards.Count == 0)
            {
                BindNamedHeroCards();
            }

            return teamSelectionBg != null && heroCards.Count > 0;
        }

        private void BindNamedHeroCards()
        {
            for (var i = 0; i < 4; i++)
            {
                var root = FindRectTransform($"HeroCard_{i}");
                if (root == null)
                {
                    continue;
                }

                var toggleButton = FindButtonByPreferredName("RuntimeButton_取消", root);
                if (toggleButton == null)
                {
                    toggleButton = FindButtonByLabel("取消", root) ?? FindButtonByLabel("选择", root);
                }

                var binding = new HeroCardBinding
                {
                    root = root,
                    background = root.GetComponent<Image>(),
                    portraitImage = FindPortraitImage(root),
                    nameText = FindTextByName("Name", root),
                    statsText = FindTextByName("Stats", root),
                    toggleButton = toggleButton,
                    toggleButtonText = toggleButton != null ? toggleButton.GetComponentInChildren<Text>(true) : null
                };

                heroCards.Add(binding);
            }

            if (heroCards.Count > 0 && heroCardBg == null)
            {
                heroCardBg = heroCards[0].background;
            }
        }

        private void BindHeroCard(HeroCardBinding card, CombatantDefinition hero, bool selected, Action<CombatantDefinition> onToggleHero)
        {
            if (card.background != null)
            {
                card.background.color = selected
                    ? new Color(0.16f, 0.105f, 0.055f, 0.96f)
                    : new Color(0.055f, 0.048f, 0.047f, 0.94f);
            }

            if (card.nameText != null)
            {
                card.nameText.text = selected ? $"[已选] {hero.displayName}" : hero.displayName;
                card.nameText.color = selected ? new Color(1f, 0.82f, 0.38f) : new Color(0.95f, 0.84f, 0.65f);
            }

            if (card.statsText != null)
            {
                card.statsText.text =
                    $"职能 {hero.ArchetypeDisplayName}  专精 {hero.SpecializationDisplayName}\n" +
                    $"状态 {hero.RecoveryDisplayName}\n" +
                    $"偏好 {hero.PreferredRowDisplayName}\n" +
                    $"等级 {hero.Level}  EXP {hero.Experience}/{hero.ExperienceToNextLevel}\n" +
                    $"生命 {hero.CurrentHealth}/{hero.MaxHealthWithGrowth}\n" +
                    $"攻击 {hero.AttackWithGrowth}\n" +
                    $"防御 {hero.DefenseWithGrowth}\n" +
                    $"速度 {hero.SpeedWithArchetype}";
            }

            if (card.portraitImage != null)
            {
                card.portraitImage.sprite = hero.portrait != null ? hero.portrait : hero.battleSprite;
                card.portraitImage.enabled = card.portraitImage.sprite != null;
            }

            var buttonLabel = selected ? "取消" : "选择";
            BindButton(card.toggleButton, onToggleHero != null ? () => onToggleHero.Invoke(hero) : null, buttonLabel, true);
        }

        private RectTransform FindRectTransform(string objectName, Transform scope = null)
        {
            var transforms = (scope != null ? scope : (teamSelectionBg != null ? teamSelectionBg.transform : transform)).GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (string.Equals(transforms[i].name, objectName, StringComparison.Ordinal))
                {
                    return transforms[i] as RectTransform;
                }
            }

            return null;
        }

        private Text FindTextByName(string objectName, Transform scope = null)
        {
            var texts = (scope != null ? scope : (teamSelectionBg != null ? teamSelectionBg.transform : transform)).GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (string.Equals(texts[i].name, objectName, StringComparison.Ordinal))
                {
                    return texts[i];
                }
            }

            return null;
        }

        private Button FindButtonByPreferredName(string objectName, Transform scope = null)
        {
            var root = FindRectTransform(objectName, scope);
            return root != null ? root.GetComponent<Button>() : null;
        }

        private Button FindButtonByLabel(string label, Transform scope = null)
        {
            var root = scope ?? (teamSelectionBg != null ? teamSelectionBg.transform : transform);
            var buttons = root.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                var labelText = button.GetComponentInChildren<Text>(true);
                if (labelText != null && string.Equals(labelText.text, label, StringComparison.Ordinal))
                {
                    return button;
                }
            }

            return null;
        }

        private static Image FindPortraitImage(Transform root)
        {
            var images = root.GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image.transform == root)
                {
                    continue;
                }

                if (image.GetComponent<Button>() != null)
                {
                    continue;
                }

                var name = image.name;
                if (string.Equals(name, "Name", StringComparison.Ordinal) || string.Equals(name, "Stats", StringComparison.Ordinal))
                {
                    continue;
                }

                return image;
            }

            return null;
        }

        private static void BindButton(Button button, Action callback, string label, bool keepVisible)
        {
            if (button == null)
            {
                return;
            }

            if (keepVisible)
            {
                button.gameObject.SetActive(true);
            }

            button.onClick.RemoveAllListeners();
            button.interactable = callback != null;

            var labelText = button.GetComponentInChildren<Text>(true);
            if (labelText != null && !string.IsNullOrWhiteSpace(label))
            {
                labelText.text = label;
            }

            if (callback != null)
            {
                button.onClick.AddListener(() => callback.Invoke());
            }
        }
    }
}
