using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class TavernUI : CombatUIScreen
    {
        [Serializable]
        private sealed class RecruitCardBinding
        {
            public RectTransform root;
            public Image background;
            public Image portraitImage;
            public Text nameText;
            public Text statsText;
            public Button recruitButton;
            public Text recruitButtonText;
        }

        [SerializeField] public Image tavernBg;
        [SerializeField] public Image cardBg;

        [Header("Optional Explicit References")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text emptyText;
        [SerializeField] private Button backButton;
        [SerializeField] private List<RecruitCardBinding> recruitCards = new List<RecruitCardBinding>();

        public override void BuildLayout()
        {
            // Prefab-driven screen. Runtime must not rebuild layout or override authored positions.
        }

        public void Show(IReadOnlyList<CombatantDefinition> recruitableHeroes, int currentGold, Action<CombatantDefinition> onRecruit, Action onBack)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("TavernUI prefab layout is incomplete. Runtime layout rebuilding is disabled; repair the prefab instead.", this);
                return;
            }

            if (titleText != null)
            {
                titleText.text = "酒馆";
            }

            if (goldText != null)
            {
                goldText.text = $"当前金币：{Mathf.Max(0, currentGold)}";
            }

            BindButton(backButton, onBack, "返回大厅");

            var heroes = recruitableHeroes ?? Array.Empty<CombatantDefinition>();
            var hasHeroes = heroes.Count > 0;

            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(!hasHeroes);
                if (!hasHeroes)
                {
                    emptyText.text = "所有英雄都已加入队伍。";
                }
            }

            for (var i = 0; i < recruitCards.Count; i++)
            {
                var card = recruitCards[i];
                if (card?.root == null)
                {
                    continue;
                }

                if (i >= heroes.Count)
                {
                    card.root.gameObject.SetActive(false);
                    continue;
                }

                card.root.gameObject.SetActive(true);
                BindRecruitCard(card, heroes[i], currentGold, onRecruit);
            }
        }

        private bool TryBindExistingLayout()
        {
            if (tavernBg == null)
            {
                var panel = transform.Find("TavernPanel");
                if (panel != null)
                {
                    tavernBg = panel.GetComponent<Image>();
                }
            }

            if (titleText == null)
            {
                titleText = FindTextByName("Title");
            }

            if (goldText == null)
            {
                goldText = FindTextByName("Gold");
            }

            if (backButton == null)
            {
                backButton = FindButtonByLabel("返回大厅");
            }

            if (emptyText == null)
            {
                emptyText = FindTextByName("Empty");
            }

            if (recruitCards == null)
            {
                recruitCards = new List<RecruitCardBinding>();
            }

            if (recruitCards.Count == 0)
            {
                BindNamedRecruitCards();
            }

            return tavernBg != null && recruitCards.Count > 0;
        }

        private void BindNamedRecruitCards()
        {
            for (var i = 0; i < 3; i++)
            {
                var root = FindRectTransform($"RecruitHero_{i}");
                if (root == null)
                {
                    continue;
                }

                var binding = new RecruitCardBinding
                {
                    root = root,
                    background = root.GetComponent<Image>(),
                    portraitImage = FindPortraitImage(root),
                    nameText = FindTextByName("Name", root),
                    statsText = FindTextByName("Stats", root)
                };

                var buttons = root.GetComponentsInChildren<Button>(true);
                if (buttons.Length > 0)
                {
                    binding.recruitButton = buttons[0];
                    binding.recruitButtonText = binding.recruitButton.GetComponentInChildren<Text>(true);
                }

                recruitCards.Add(binding);
            }

            if (recruitCards.Count > 0 && cardBg == null)
            {
                cardBg = recruitCards[0].background;
            }
        }

        private void BindRecruitCard(RecruitCardBinding card, CombatantDefinition hero, int currentGold, Action<CombatantDefinition> onRecruit)
        {
            if (card.nameText != null)
            {
                card.nameText.text = hero != null ? hero.displayName : "英雄占位";
            }

            if (card.statsText != null && hero != null)
            {
                card.statsText.text =
                    $"职能 {hero.ArchetypeDisplayName}\n" +
                    $"专精 {hero.SpecializationDisplayName}\n" +
                    $"偏好 {hero.PreferredRowDisplayName}\n" +
                    $"生命 {hero.MaxHealthWithGrowth}\n" +
                    $"攻击 {hero.AttackWithGrowth}\n" +
                    $"防御 {hero.DefenseWithGrowth}\n" +
                    $"速度 {hero.SpeedWithArchetype}\n" +
                    $"价格 {hero.recruitPrice}金币";
            }

            if (card.portraitImage != null)
            {
                card.portraitImage.sprite = hero != null && hero.portrait != null
                    ? hero.portrait
                    : hero != null ? hero.battleSprite : null;
                card.portraitImage.enabled = card.portraitImage.sprite != null;
            }

            var canRecruit = hero != null && currentGold >= hero.recruitPrice && onRecruit != null;
            BindButton(card.recruitButton, canRecruit ? () => onRecruit.Invoke(hero) : null, "招募");
        }

        private RectTransform FindRectTransform(string objectName)
        {
            var transforms = (tavernBg != null ? tavernBg.transform : transform).GetComponentsInChildren<Transform>(true);
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
            var texts = (scope != null ? scope : (tavernBg != null ? tavernBg.transform : transform)).GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (string.Equals(texts[i].name, objectName, StringComparison.Ordinal))
                {
                    return texts[i];
                }
            }

            return null;
        }

        private Button FindButtonByLabel(string label)
        {
            var root = tavernBg != null ? tavernBg.transform : transform;
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

        private static void BindButton(Button button, Action callback, string label)
        {
            if (button == null)
            {
                return;
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
