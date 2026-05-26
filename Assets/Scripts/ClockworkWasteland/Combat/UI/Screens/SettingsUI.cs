using System;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class SettingsUI : CombatUIScreen
    {
        [SerializeField] public Image settingsBg;
        [SerializeField] public Image sliderBg;

        [Header("Optional Explicit References")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text volumeLabelText;

        public override void BuildLayout()
        {
            // Prefab-driven screen. Runtime must not rebuild layout or override authored positions.
        }

        public void Show(Action onBack, Action onSaveGame)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("SettingsUI prefab layout is incomplete. Runtime layout rebuilding is disabled; repair the prefab instead.", this);
                return;
            }

            if (titleText != null)
            {
                titleText.text = "设置";
            }

            if (volumeLabelText != null)
            {
                volumeLabelText.text = "主音量";
            }

            if (bodyText != null)
            {
                bodyText.text = "其他设置项预留";
            }

            BindButton(saveButton, onSaveGame);
            BindButton(backButton, onBack);
            BindSlider(masterVolumeSlider, AudioListener.volume, value => AudioListener.volume = value);
        }

        private bool TryBindExistingLayout()
        {
            if (settingsBg == null)
            {
                var panel = transform.Find("SettingsPanel");
                if (panel != null)
                {
                    settingsBg = panel.GetComponent<Image>();
                }
            }

            if (titleText == null)
            {
                titleText = FindTextByName("Title");
            }

            if (bodyText == null)
            {
                bodyText = FindTextByName("Body");
            }

            if (volumeLabelText == null)
            {
                volumeLabelText = FindTextByName("VolumeLabel");
            }

            if (masterVolumeSlider == null)
            {
                masterVolumeSlider = FindSliderByName("MasterVolume");
                if (masterVolumeSlider == null)
                {
                    masterVolumeSlider = GetComponentInChildren<Slider>(true);
                }
            }

            if (sliderBg == null && masterVolumeSlider != null)
            {
                sliderBg = masterVolumeSlider.GetComponentInChildren<Image>(true);
            }

            saveButton = saveButton != null ? saveButton : FindButtonByLabel("保存游戏");
            backButton = backButton != null ? backButton : FindButtonByLabel("返回");

            return settingsBg != null;
        }

        private Button FindButtonByLabel(string label)
        {
            var root = settingsBg != null ? settingsBg.transform : transform;
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
            var texts = (settingsBg != null ? settingsBg.transform : transform).GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (string.Equals(texts[i].name, objectName, StringComparison.Ordinal))
                {
                    return texts[i];
                }
            }

            return null;
        }

        private Slider FindSliderByName(string objectName)
        {
            var sliders = (settingsBg != null ? settingsBg.transform : transform).GetComponentsInChildren<Slider>(true);
            for (var i = 0; i < sliders.Length; i++)
            {
                if (string.Equals(sliders[i].name, objectName, StringComparison.Ordinal))
                {
                    return sliders[i];
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

        private static void BindSlider(Slider slider, float currentValue, Action<float> onChanged)
        {
            if (slider == null)
            {
                return;
            }

            slider.onValueChanged.RemoveAllListeners();
            slider.SetValueWithoutNotify(currentValue);
            if (onChanged != null)
            {
                slider.onValueChanged.AddListener(value => onChanged.Invoke(value));
            }
        }
    }
}
