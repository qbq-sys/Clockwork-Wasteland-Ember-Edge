using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class RecoveryWardUI : CombatUIScreen
    {
        [Serializable]
        private sealed class RecoveryHeroRowBinding
        {
            public RectTransform root;
            public Image background;
            public Text heroText;
            public Button actionButton;
            public Text actionButtonText;
        }

        public Image recoveryWardBg;
        public Image rowBg;

        [Header("Optional Explicit References")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text goldText;
        [SerializeField] private Button backButton;
        [SerializeField] private List<RecoveryHeroRowBinding> heroRows = new List<RecoveryHeroRowBinding>();

        public override void BuildLayout()
        {
            // Prefab-driven screen. Runtime must not rebuild layout or override authored positions.
        }

        public void Show(IReadOnlyList<CombatantDefinition> heroes, int currentGold, int treatmentCost, Action<CombatantDefinition> onTreat, Action onBack)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("RecoveryWardUI prefab layout is incomplete. Repair the prefab instead of rebuilding it at runtime.", this);
                return;
            }

            if (titleText != null)
            {
                titleText.text = "伤员休整";
            }

            if (goldText != null)
            {
                goldText.text = $"当前金币：{currentGold}";
            }

            BindButton(backButton, onBack, "返回大厅");

            var heroList = (heroes ?? Array.Empty<CombatantDefinition>())
                .Where(hero => hero != null && hero.isHero && hero.isUnlocked)
                .OrderBy(hero => hero.IsRecovering ? 0 : 1)
                .ThenBy(hero => hero.displayName)
                .ToArray();

            for (var i = 0; i < heroRows.Count; i++)
            {
                var row = heroRows[i];
                if (row?.root == null)
                {
                    continue;
                }

                if (i >= heroList.Length)
                {
                    row.root.gameObject.SetActive(false);
                    continue;
                }

                row.root.gameObject.SetActive(true);
                BindHeroRow(row, heroList[i], currentGold, treatmentCost, onTreat);
            }
        }

        public void RebuildLayoutFromCode(Sprite panelSprite)
        {
            CombatUIScreenUtility.ClearChildren(transform);
            var root = PrepareRoot();

            var panel = CombatUIScreenUtility.CreatePanel("RecoveryWardPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 620f), new Color(0.032f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
            panel.sprite = panelSprite;
            panel.type = Image.Type.Sliced;
            recoveryWardBg = panel;

            titleText = CombatUIScreenUtility.CreateText("Title", panel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            titleText.text = "伤员休整";
            CombatUIScreenUtility.SetTextStyle(titleText, new Color(0.96f, 0.82f, 0.48f), true);

            goldText = CombatUIScreenUtility.CreateText("Gold", panel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -94f), new Vector2(-120f, 34f), 18, TextAnchor.MiddleCenter);
            goldText.text = "当前金币：1200";
            CombatUIScreenUtility.SetTextStyle(goldText, new Color(1f, 0.78f, 0.34f), true);

            heroRows.Clear();
            for (var i = 0; i < 6; i++)
            {
                var y = -160f - i * 68f;
                var row = CombatUIScreenUtility.CreatePanel($"RecoveryHero_{i}", panel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(-120f, 52f), new Color(0.055f, 0.048f, 0.047f, 0.94f));
                row.offsetMin = new Vector2(80f, row.offsetMin.y);
                row.offsetMax = new Vector2(-80f, row.offsetMax.y);
                var rowImage = row.GetComponent<Image>();
                rowImage.sprite = panelSprite;
                rowImage.type = Image.Type.Sliced;

                var heroText = CombatUIScreenUtility.CreateText("Hero", row, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleLeft);
                heroText.rectTransform.offsetMin = new Vector2(18f, 0f);
                heroText.rectTransform.offsetMax = new Vector2(-260f, 0f);
                heroText.text = "英雄示例    重伤休整中    生命 12/32";
                CombatUIScreenUtility.SetTextStyle(heroText, new Color(0.95f, 0.74f, 0.52f), false);

                var actionButton = CombatUIScreenUtility.CreateButton(row, "急救恢复 120金", new Vector2(640f, -26f), null, true);
                var binding = new RecoveryHeroRowBinding
                {
                    root = row,
                    background = rowImage,
                    heroText = heroText,
                    actionButton = actionButton,
                    actionButtonText = actionButton != null ? actionButton.GetComponentInChildren<Text>(true) : null
                };
                heroRows.Add(binding);
            }

            backButton = CombatUIScreenUtility.CreateButton(panel.rectTransform, "返回大厅", new Vector2(490f, -572f), null, true);
        }

        private bool TryBindExistingLayout()
        {
            if (recoveryWardBg == null)
            {
                recoveryWardBg = transform.Find("RecoveryWardPanel")?.GetComponent<Image>();
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
                backButton = FindButtonByPreferredName("RuntimeButton_返回大厅") ?? FindButtonByLabel("返回大厅");
            }

            if (heroRows == null)
            {
                heroRows = new List<RecoveryHeroRowBinding>();
            }

            if (heroRows.Count == 0)
            {
                BindNamedRows();
            }

            return recoveryWardBg != null && heroRows.Count > 0;
        }

        private void BindNamedRows()
        {
            for (var i = 0; i < 6; i++)
            {
                var root = FindRectTransform($"RecoveryHero_{i}");
                if (root == null)
                {
                    continue;
                }

                var buttons = root.GetComponentsInChildren<Button>(true);
                var actionButton = buttons.FirstOrDefault();
                var binding = new RecoveryHeroRowBinding
                {
                    root = root,
                    background = root.GetComponent<Image>(),
                    heroText = FindTextByName("Hero", root),
                    actionButton = actionButton,
                    actionButtonText = actionButton != null ? actionButton.GetComponentInChildren<Text>(true) : null
                };
                heroRows.Add(binding);
            }

            if (heroRows.Count > 0 && rowBg == null)
            {
                rowBg = heroRows[0].background;
            }
        }

        private void BindHeroRow(RecoveryHeroRowBinding row, CombatantDefinition hero, int currentGold, int treatmentCost, Action<CombatantDefinition> onTreat)
        {
            if (row.heroText != null)
            {
                var statusText = hero.IsRecovering ? hero.RecoveryDisplayName : "可出战";
                row.heroText.text = $"{hero.displayName}    {statusText}    生命 {hero.CurrentHealth}/{hero.MaxHealthWithGrowth}";
                CombatUIScreenUtility.SetTextStyle(row.heroText, hero.IsRecovering ? new Color(0.95f, 0.74f, 0.52f) : new Color(0.78f, 0.84f, 0.72f), false);
            }

            var canTreat = hero.IsRecovering && currentGold >= treatmentCost;
            var actionLabel = hero.IsRecovering ? $"急救恢复 {treatmentCost}金" : "已稳定";
            BindButton(row.actionButton, canTreat ? () => onTreat?.Invoke(hero) : null, actionLabel);
        }

        private RectTransform FindRectTransform(string objectName)
        {
            var transforms = (recoveryWardBg != null ? recoveryWardBg.transform : transform).GetComponentsInChildren<Transform>(true);
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
            var texts = (scope != null ? scope : (recoveryWardBg != null ? recoveryWardBg.transform : transform)).GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (string.Equals(texts[i].name, objectName, StringComparison.Ordinal))
                {
                    return texts[i];
                }
            }

            return null;
        }

        private Button FindButtonByPreferredName(string objectName)
        {
            var buttons = (recoveryWardBg != null ? recoveryWardBg.transform : transform).GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                if (string.Equals(buttons[i].name, objectName, StringComparison.Ordinal))
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private Button FindButtonByLabel(string label)
        {
            var buttons = (recoveryWardBg != null ? recoveryWardBg.transform : transform).GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                var text = buttons[i].GetComponentInChildren<Text>(true);
                if (text != null && string.Equals(text.text, label, StringComparison.Ordinal))
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private static void BindButton(Button button, Action callback, string label)
        {
            if (button == null)
            {
                return;
            }

            var text = button.GetComponentInChildren<Text>(true);
            if (text != null && !string.IsNullOrWhiteSpace(label))
            {
                text.text = label;
            }

            button.onClick.RemoveAllListeners();
            button.interactable = callback != null;
            if (callback != null)
            {
                button.onClick.AddListener(() => callback.Invoke());
            }
        }
    }
}
