using System;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class PopupUI : CombatUIScreen
    {
        [SerializeField] public Image popupBg;

        [Header("Optional Explicit References")]
        [SerializeField] private Image overlayBg;
        [SerializeField] private RectTransform popupOverlay;
        [SerializeField] private RectTransform messagePanel;
        [SerializeField] private Text messageText;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Text primaryButtonText;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private Text secondaryButtonText;

        private Vector2 defaultPanelAnchoredPosition;
        private Vector2 defaultPanelSize;
        private Vector2 defaultPrimaryButtonAnchoredPosition;
        private Vector2 defaultMessageOffsetMin;
        private Vector2 defaultMessageOffsetMax;
        private int defaultMessageFontSize;
        private TextAnchor defaultMessageAlignment;
        private bool layoutSnapshotCaptured;

        public override void BuildLayout()
        {
            // Popup uses prefab-driven layout. Runtime may only make minimal stateful adjustments
            // for the two-button variant because the prefab currently ships with a single-button template.
        }

        public void Show(string message, string buttonLabel, Action onContinue)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("PopupUI prefab layout is incomplete. Runtime layout rebuilding is disabled; repair the prefab instead.", this);
                return;
            }

            ApplyMessageLayout();
            SetMessage(message, 28, TextAnchor.MiddleCenter, new Vector2(28f, -8f), new Vector2(-28f, -24f));
            ConfigureButton(primaryButton, primaryButtonText, string.IsNullOrWhiteSpace(buttonLabel) ? "确定" : buttonLabel, () =>
            {
                gameObject.SetActive(false);
                onContinue?.Invoke();
            });

            if (secondaryButton != null)
            {
                secondaryButton.gameObject.SetActive(false);
                secondaryButton.onClick.RemoveAllListeners();
            }

            gameObject.SetActive(true);
        }

        public void ShowChoice(string message, string leftLabel, Action onLeft, string rightLabel, Action onRight)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("PopupUI prefab layout is incomplete. Runtime layout rebuilding is disabled; repair the prefab instead.", this);
                return;
            }

            EnsureSecondaryButton();
            ApplyChoiceLayout();
            SetMessage(message, 24, TextAnchor.UpperLeft, new Vector2(32f, 90f), new Vector2(-32f, -28f));

            ConfigureButton(primaryButton, primaryButtonText, string.IsNullOrWhiteSpace(leftLabel) ? "确认" : leftLabel, () =>
            {
                gameObject.SetActive(false);
                onLeft?.Invoke();
            });

            ConfigureButton(secondaryButton, secondaryButtonText, string.IsNullOrWhiteSpace(rightLabel) ? "返回" : rightLabel, () =>
            {
                gameObject.SetActive(false);
                onRight?.Invoke();
            });

            SetButtonPosition(primaryButton, new Vector2(210f, -300f));
            SetButtonPosition(secondaryButton, new Vector2(550f, -300f));
            gameObject.SetActive(true);
        }

        private bool TryBindExistingLayout()
        {
            if (popupOverlay == null)
            {
                popupOverlay = transform.Find("PopupOverlay") as RectTransform;
            }

            if (overlayBg == null && popupOverlay != null)
            {
                overlayBg = popupOverlay.GetComponent<Image>();
            }

            if (messagePanel == null)
            {
                messagePanel = transform.Find("PopupOverlay/MessagePanel") as RectTransform;
            }

            if (popupBg == null && messagePanel != null)
            {
                popupBg = messagePanel.GetComponent<Image>();
            }

            if (messageText == null)
            {
                messageText = FindTextByName("OverlayText", messagePanel);
            }

            if (primaryButton == null)
            {
                primaryButton = FindButtonByPreferredNames(messagePanel, "PrimaryButton", "RuntimeButton_确定");
            }

            if (primaryButtonText == null && primaryButton != null)
            {
                primaryButtonText = primaryButton.GetComponentInChildren<Text>(true);
            }

            CaptureLayoutSnapshot();
            return popupOverlay != null && messagePanel != null && popupBg != null && messageText != null && primaryButton != null;
        }

        private void CaptureLayoutSnapshot()
        {
            if (layoutSnapshotCaptured || messagePanel == null || messageText == null || primaryButton == null)
            {
                return;
            }

            defaultPanelAnchoredPosition = messagePanel.anchoredPosition;
            defaultPanelSize = messagePanel.sizeDelta;
            defaultPrimaryButtonAnchoredPosition = (primaryButton.transform as RectTransform)?.anchoredPosition ?? Vector2.zero;
            defaultMessageOffsetMin = messageText.rectTransform.offsetMin;
            defaultMessageOffsetMax = messageText.rectTransform.offsetMax;
            defaultMessageFontSize = messageText.fontSize;
            defaultMessageAlignment = messageText.alignment;
            layoutSnapshotCaptured = true;
        }

        private void ApplyMessageLayout()
        {
            if (messagePanel != null)
            {
                messagePanel.anchoredPosition = defaultPanelAnchoredPosition;
                messagePanel.sizeDelta = defaultPanelSize;
            }

            SetButtonPosition(primaryButton, defaultPrimaryButtonAnchoredPosition);
        }

        private void ApplyChoiceLayout()
        {
            if (messagePanel != null)
            {
                messagePanel.anchoredPosition = new Vector2(0f, 18f);
                messagePanel.sizeDelta = new Vector2(760f, 360f);
            }
        }

        private void SetMessage(string message, int fontSize, TextAnchor alignment, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (messageText == null)
            {
                return;
            }

            messageText.text = message;
            messageText.fontSize = fontSize;
            messageText.alignment = alignment;
            messageText.rectTransform.offsetMin = offsetMin;
            messageText.rectTransform.offsetMax = offsetMax;
            CombatUIScreenUtility.SetTextStyle(messageText, new Color(0.96f, 0.82f, 0.48f), alignment == TextAnchor.MiddleCenter);
        }

        private void EnsureSecondaryButton()
        {
            if (secondaryButton != null)
            {
                secondaryButton.gameObject.SetActive(true);
                return;
            }

            if (primaryButton == null || primaryButton.transform.parent == null)
            {
                return;
            }

            var clone = Instantiate(primaryButton.gameObject, primaryButton.transform.parent);
            clone.name = "SecondaryButton";
            secondaryButton = clone.GetComponent<Button>();
            secondaryButtonText = secondaryButton != null ? secondaryButton.GetComponentInChildren<Text>(true) : null;
        }

        private static void ConfigureButton(Button button, Text label, string caption, Action callback)
        {
            if (button == null)
            {
                return;
            }

            if (label != null)
            {
                label.text = caption;
            }

            button.onClick.RemoveAllListeners();
            button.interactable = callback != null;
            if (callback != null)
            {
                button.onClick.AddListener(() => callback.Invoke());
            }

            button.gameObject.SetActive(true);
        }

        private static void SetButtonPosition(Button button, Vector2 anchoredPosition)
        {
            var rect = button != null ? button.transform as RectTransform : null;
            if (rect != null)
            {
                rect.anchoredPosition = anchoredPosition;
            }
        }

        private static Text FindTextByName(string objectName, Transform scope)
        {
            if (scope == null)
            {
                return null;
            }

            var texts = scope.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (string.Equals(texts[i].name, objectName, StringComparison.Ordinal))
                {
                    return texts[i];
                }
            }

            return null;
        }

        private static Button FindButtonByPreferredNames(Transform scope, params string[] names)
        {
            if (scope == null)
            {
                return null;
            }

            var buttons = scope.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                for (var j = 0; j < names.Length; j++)
                {
                    if (string.Equals(button.name, names[j], StringComparison.Ordinal))
                    {
                        return button;
                    }
                }
            }

            return null;
        }
    }
}
