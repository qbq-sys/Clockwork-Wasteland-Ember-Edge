using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class SaveSlotUI : CombatUIScreen
    {
        [Serializable]
        private sealed class SaveSlotCardBinding
        {
            public RectTransform root;
            public Image background;
            public Text nameText;
            public Text detailText;
            public Button primaryButton;
            public Text primaryButtonText;
            public Button deleteButton;
            public Text deleteButtonText;
        }

        [SerializeField] public Image saveSlotBg;
        [SerializeField] public Image slotCardBg;

        [Header("Optional Explicit References")]
        [SerializeField] private Text titleText;
        [SerializeField] private Button backButton;
        [SerializeField] private List<SaveSlotCardBinding> slotCards = new List<SaveSlotCardBinding>();

        public override void BuildLayout()
        {
            // Prefab-driven screen. Runtime must not rebuild layout or override authored positions.
        }

        public void Show(string title, IReadOnlyList<SaveSlotSummary> slots, bool allowEmptySelection, Action<int> onSelect, Action<int> onDelete, Action onBack)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("SaveSlotUI prefab layout is incomplete. Runtime layout rebuilding is disabled; repair the prefab instead.", this);
                return;
            }

            if (titleText != null)
            {
                titleText.text = string.IsNullOrWhiteSpace(title) ? "存档" : title;
            }

            BindButton(backButton, onBack, null);

            var slotList = slots ?? Array.Empty<SaveSlotSummary>();
            for (var i = 0; i < slotCards.Count; i++)
            {
                var card = slotCards[i];
                if (card?.root == null)
                {
                    continue;
                }

                if (i >= slotList.Count)
                {
                    card.root.gameObject.SetActive(false);
                    continue;
                }

                card.root.gameObject.SetActive(true);
                BindSlotCard(card, slotList[i], allowEmptySelection, onSelect, onDelete);
            }
        }

        private bool TryBindExistingLayout()
        {
            if (saveSlotBg == null)
            {
                var panel = transform.Find("SaveSlotPanel");
                if (panel != null)
                {
                    saveSlotBg = panel.GetComponent<Image>();
                }
            }

            if (titleText == null)
            {
                titleText = FindTextByName("Title");
            }

            if (backButton == null)
            {
                backButton = FindButtonByLabel("返回");
            }

            if (slotCards == null)
            {
                slotCards = new List<SaveSlotCardBinding>();
            }

            if (slotCards.Count == 0)
            {
                BindNamedSlotCards();
            }

            return saveSlotBg != null && slotCards.Count > 0;
        }

        private void BindNamedSlotCards()
        {
            for (var i = 0; i < 3; i++)
            {
                var root = FindRectTransform($"SaveSlot_{i}");
                if (root == null)
                {
                    continue;
                }

                var binding = new SaveSlotCardBinding
                {
                    root = root,
                    background = root.GetComponent<Image>(),
                    nameText = FindTextByName("Name", root),
                    detailText = FindTextByName("Detail", root)
                };

                var buttons = root.GetComponentsInChildren<Button>(true);
                if (buttons.Length > 0)
                {
                    binding.primaryButton = buttons[0];
                    binding.primaryButtonText = binding.primaryButton.GetComponentInChildren<Text>(true);
                }

                if (buttons.Length > 1)
                {
                    binding.deleteButton = buttons[1];
                    binding.deleteButtonText = binding.deleteButton.GetComponentInChildren<Text>(true);
                }

                slotCards.Add(binding);
            }

            if (slotCards.Count > 0 && slotCardBg == null)
            {
                slotCardBg = slotCards[0].background;
            }
        }

        private void BindSlotCard(SaveSlotCardBinding card, SaveSlotSummary slot, bool allowEmptySelection, Action<int> onSelect, Action<int> onDelete)
        {
            if (card.nameText != null)
            {
                card.nameText.text = string.IsNullOrWhiteSpace(slot.Title) ? $"存档 {slot.SlotIndex + 1}" : slot.Title;
            }

            if (card.detailText != null)
            {
                card.detailText.text = slot.HasSave ? slot.Detail : "空存档位";
            }

            var primaryLabel = slot.HasSave ? (allowEmptySelection ? "覆盖" : "载入") : (allowEmptySelection ? "保存到此" : "空");
            var canSelect = slot.HasSave || allowEmptySelection;
            BindButton(card.primaryButton, canSelect ? () => onSelect?.Invoke(slot.SlotIndex) : null, primaryLabel);

            if (card.deleteButton != null)
            {
                var canDelete = slot.HasSave && onDelete != null;
                card.deleteButton.gameObject.SetActive(canDelete);
                BindButton(card.deleteButton, canDelete ? () => onDelete?.Invoke(slot.SlotIndex) : null, "删除");
            }
        }

        private RectTransform FindRectTransform(string objectName)
        {
            var transforms = (saveSlotBg != null ? saveSlotBg.transform : transform).GetComponentsInChildren<Transform>(true);
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
            var texts = (scope != null ? scope : (saveSlotBg != null ? saveSlotBg.transform : transform)).GetComponentsInChildren<Text>(true);
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
            var root = saveSlotBg != null ? saveSlotBg.transform : transform;
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
