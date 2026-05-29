using System;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class LobbyUI : CombatUIScreen
    {
        [SerializeField] public Image lobbyBg;
        [SerializeField] public Image heroShowcaseBg;
        [SerializeField] public Image cardBg;

        [Header("Optional Explicit References")]
        [SerializeField] private Button tavernButton;
        [SerializeField] private Button adventureButton;
        [SerializeField] private Button recoveryWardButton;
        [SerializeField] private Button heroCodexButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button settingsButton;

        [SerializeField] private Text goldText;

        public override void BuildLayout()
        {
            // Prefab-driven screen. Runtime must not rebuild layout or override authored positions.
        }

        public void Show(int currentGold, Action onOpenTavern, Action onOpenAdventure, Action onOpenRecoveryWard, Action onOpenHeroCodex, Action onOpenShop, Action onOpenInventory, Action onOpenSettings, Action onBackToStartMenu)
        {
            PrepareRoot();
            TryBindExistingLayout();
            if (lobbyBg == null)
            {
                Debug.LogError("LobbyUI prefab layout is incomplete: missing root background image.", this);
                return;
            }

            HideStartMenuOnlyElements();
            BindButton(tavernButton, onOpenTavern);
            BindButton(adventureButton, onOpenAdventure);
            BindButton(recoveryWardButton, onOpenRecoveryWard);
            BindButton(heroCodexButton, onOpenHeroCodex);
            BindButton(shopButton, onOpenShop);
            BindButton(inventoryButton, onOpenInventory);
            BindButton(menuButton, onBackToStartMenu);
            BindButton(settingsButton, onOpenSettings);

            if (goldText != null)
            {
                goldText.gameObject.SetActive(true);
                goldText.text = $"金币：{Mathf.Max(0, currentGold)}";
            }
        }

        private bool TryBindExistingLayout()
        {
            if (lobbyBg == null)
            {
                var panel = transform.Find("LobbyPanel");
                if (panel != null)
                {
                    lobbyBg = panel.GetComponent<Image>();
                }
            }

            if (heroShowcaseBg == null && lobbyBg != null)
            {
                var showcase = lobbyBg.transform.Find("HeroShowcasePlaceholder");
                if (showcase != null)
                {
                    heroShowcaseBg = showcase.GetComponent<Image>();
                    cardBg = heroShowcaseBg;
                }
            }

            tavernButton = tavernButton != null ? tavernButton : FindButtonByLabel("酒馆");
            adventureButton = adventureButton != null ? adventureButton : FindButtonByLabel("冒险");
            recoveryWardButton = recoveryWardButton != null ? recoveryWardButton : FindButtonByLabel("伤员休整");
            heroCodexButton = heroCodexButton != null ? heroCodexButton : FindButtonByLabel("英雄图鉴");
            shopButton = shopButton != null ? shopButton : FindButtonByLabel("商店");
            inventoryButton = inventoryButton != null ? inventoryButton : FindButtonByLabel("背包");
            menuButton = menuButton != null ? menuButton : FindButtonByLabel("主菜单");
            if (menuButton == null)
            {
                menuButton = FindButtonByLabel("返回主菜单");
            }
            settingsButton = settingsButton != null ? settingsButton : FindButtonByLabel("设置");

            if (goldText == null)
            {
                goldText = FindTextByName("Gold");
            }

            return lobbyBg != null;
        }

        private void HideStartMenuOnlyElements()
        {
            SetButtonVisibleByLabel("返回大厅", false);
        }

        private void SetButtonVisibleByLabel(string label, bool visible)
        {
            var button = FindButtonByLabel(label);
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }

        private Button FindButtonByLabel(string label)
        {
            var root = lobbyBg != null ? lobbyBg.transform : transform;
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

        private Text FindTextByName(string objectName)
        {
            var texts = (lobbyBg != null ? lobbyBg.transform : transform).GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (string.Equals(texts[i].name, objectName, StringComparison.Ordinal))
                {
                    return texts[i];
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
    }
}
