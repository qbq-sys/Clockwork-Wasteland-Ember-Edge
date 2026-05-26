using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class AdventureMapUI : CombatUIScreen
    {
        [Serializable]
        private sealed class MapCardBinding
        {
            public RectTransform root;
            public Image background;
            public RectTransform previewRoot;
            public Image previewImage;
            public Text previewText;
            public Text nameText;
            public Text descriptionText;
            public Button selectButton;
            public Text selectButtonText;
        }

        [SerializeField] public Image adventureBg;
        [SerializeField] public Image mapCardBg;

        [Header("Optional Explicit References")]
        [SerializeField] private Text titleText;
        [SerializeField] private Button backButton;
        [SerializeField] private List<MapCardBinding> mapCards = new List<MapCardBinding>();

        public override void BuildLayout()
        {
            // Prefab-driven screen. Runtime must not rebuild layout or override authored positions.
        }

        public void Show(IReadOnlyList<AdventureMapOption> maps, Action<AdventureMapOption> onSelect, Action onBack)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("AdventureMapUI prefab layout is incomplete. Runtime layout rebuilding is disabled; repair the prefab instead.", this);
                return;
            }

            if (titleText != null)
            {
                titleText.text = "冒险";
            }

            BindButton(backButton, onBack, "返回大厅", true);

            var mapList = maps ?? Array.Empty<AdventureMapOption>();
            for (var i = 0; i < mapCards.Count; i++)
            {
                var card = mapCards[i];
                if (card?.root == null)
                {
                    continue;
                }

                if (i >= mapList.Count)
                {
                    card.root.gameObject.SetActive(false);
                    continue;
                }

                card.root.gameObject.SetActive(true);
                BindMapCard(card, mapList[i], onSelect);
            }
        }

        private bool TryBindExistingLayout()
        {
            if (adventureBg == null)
            {
                var panel = transform.Find("AdventurePanel");
                if (panel != null)
                {
                    adventureBg = panel.GetComponent<Image>();
                }
            }

            if (titleText == null)
            {
                titleText = FindTextByName("Title");
            }

            if (backButton == null)
            {
                backButton = FindButtonByPreferredName("RuntimeButton_返回大厅") ?? FindButtonByLabel("返回大厅");
            }

            if (mapCards == null)
            {
                mapCards = new List<MapCardBinding>();
            }

            if (mapCards.Count == 0)
            {
                BindNamedMapCards();
            }

            return adventureBg != null && mapCards.Count > 0;
        }

        private void BindNamedMapCards()
        {
            for (var i = 0; i < 3; i++)
            {
                var root = FindRectTransform($"AdventureMap_{i}");
                if (root == null)
                {
                    continue;
                }

                var previewRoot = FindRectTransform("Preview", root);
                var selectButton = FindButtonByPreferredName("RuntimeButton_选择", root);
                var binding = new MapCardBinding
                {
                    root = root,
                    background = root.GetComponent<Image>(),
                    previewRoot = previewRoot,
                    previewImage = FindPreviewImage(previewRoot),
                    previewText = FindTextByName("PreviewText", root),
                    nameText = FindTextByName("Name", root),
                    descriptionText = FindTextByName("Description", root),
                    selectButton = selectButton,
                    selectButtonText = selectButton != null ? selectButton.GetComponentInChildren<Text>(true) : null
                };

                mapCards.Add(binding);
            }

            if (mapCards.Count > 0 && mapCardBg == null)
            {
                mapCardBg = mapCards[0].background;
            }
        }

        private void BindMapCard(MapCardBinding card, AdventureMapOption map, Action<AdventureMapOption> onSelect)
        {
            if (card.nameText != null)
            {
                card.nameText.text = string.IsNullOrWhiteSpace(map.DisplayName) ? "地图" : map.DisplayName;
            }

            if (card.descriptionText != null)
            {
                var statusLine = map.IsUnlocked ? "状态：已解锁" : $"解锁：{map.UnlockSummary}";
                card.descriptionText.text = $"{map.Description}\n战斗数：{map.BattleCount}\n{statusLine}";
            }

            if (card.previewImage != null)
            {
                card.previewImage.sprite = map.PreviewSprite;
                card.previewImage.enabled = map.PreviewSprite != null;
            }

            if (card.previewText != null)
            {
                card.previewText.gameObject.SetActive(map.PreviewSprite == null);
                if (map.PreviewSprite == null)
                {
                    card.previewText.text = "地图图片预留";
                }
            }

            var buttonLabel = map.IsUnlocked ? "选择" : "未解锁";
            var canSelect = map.IsUnlocked && onSelect != null;
            BindButton(card.selectButton, canSelect ? () => onSelect.Invoke(map) : null, buttonLabel, true);
        }

        private RectTransform FindRectTransform(string objectName, Transform scope = null)
        {
            var transforms = (scope != null ? scope : (adventureBg != null ? adventureBg.transform : transform)).GetComponentsInChildren<Transform>(true);
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
            var texts = (scope != null ? scope : (adventureBg != null ? adventureBg.transform : transform)).GetComponentsInChildren<Text>(true);
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

        private Button FindButtonByLabel(string label)
        {
            var root = adventureBg != null ? adventureBg.transform : transform;
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

        private static Image FindPreviewImage(Transform previewRoot)
        {
            if (previewRoot == null)
            {
                return null;
            }

            var images = previewRoot.GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                if (images[i].transform != previewRoot)
                {
                    return images[i];
                }
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
