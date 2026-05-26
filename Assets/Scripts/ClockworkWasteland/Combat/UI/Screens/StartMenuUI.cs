using System;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class StartMenuUI : CombatUIScreen
    {
        [SerializeField] public Image startMenuBg;
        [SerializeField] public Image heroShowcaseBg;

        [Header("Optional Explicit References")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button backButton;

        public override void BuildLayout()
        {
            // Prefab-driven screen. Runtime must not rebuild layout or override authored positions.
        }

        public void Show(bool showContinue, Action onStartNewGame, Action onContinueGame, Action onOpenSettings, Action onQuit, Action onBack)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("StartMenuUI prefab layout is incomplete. Runtime layout rebuilding is disabled; repair the prefab instead.", this);
                return;
            }

            HideLobbyOnlyElements();
            BindButton(newGameButton, onStartNewGame);
            BindButton(continueButton, onContinueGame);
            BindButton(settingsButton, onOpenSettings);
            BindButton(quitButton, onQuit);
            BindButton(backButton, onBack);

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(showContinue);
            }

            if (backButton != null)
            {
                backButton.gameObject.SetActive(onBack != null);
            }
        }

        private bool TryBindExistingLayout()
        {
            if (startMenuBg == null)
            {
                var startPanel = transform.Find("StartMenuPanel");
                if (startPanel != null)
                {
                    startMenuBg = startPanel.GetComponent<Image>();
                }
            }

            if (heroShowcaseBg == null && startMenuBg != null)
            {
                var showcase = startMenuBg.transform.Find("HeroShowcasePlaceholder");
                if (showcase != null)
                {
                    heroShowcaseBg = showcase.GetComponent<Image>();
                }
            }

            newGameButton = newGameButton != null ? newGameButton : FindButtonByLabel("新游戏");
            continueButton = continueButton != null ? continueButton : FindButtonByLabel("继续游戏");
            settingsButton = settingsButton != null ? settingsButton : FindButtonByLabel("设置");
            quitButton = quitButton != null ? quitButton : FindButtonByLabel("退出");
            backButton = backButton != null ? backButton : FindButtonByLabel("返回大厅");

            return startMenuBg != null && newGameButton != null && settingsButton != null && quitButton != null;
        }

        private Button FindButtonByLabel(string label)
        {
            var root = startMenuBg != null ? startMenuBg.transform : transform;
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

        private static void BindButton(Button button, Action callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.interactable = callback != null;
            if (callback != null)
            {
                button.onClick.AddListener(() => callback.Invoke());
            }
        }

        private void HideLobbyOnlyElements()
        {
            SetButtonVisibleByLabel("酒馆", false);
            SetButtonVisibleByLabel("冒险", false);
            SetButtonVisibleByLabel("伤员休整", false);
            SetButtonVisibleByLabel("英雄图鉴", false);
            SetButtonVisibleByLabel("主菜单", false);

            var gold = FindTextByName("Gold");
            if (gold != null)
            {
                gold.gameObject.SetActive(false);
            }
        }

        private void SetButtonVisibleByLabel(string label, bool visible)
        {
            var button = FindButtonByLabel(label);
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }

        private Text FindTextByName(string objectName)
        {
            var texts = (startMenuBg != null ? startMenuBg.transform : transform).GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (string.Equals(texts[i].name, objectName, StringComparison.Ordinal))
                {
                    return texts[i];
                }
            }

            return null;
        }
    }
}
