using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed class TurnIndicatorView : MonoBehaviour
    {
        private const float MinLocalY = -0.07f;
        private const float MaxLocalY = 0.155f;

        [SerializeField] private RectTransform animatedRoot;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text labelText;
        [SerializeField] private Text arrowText;
        [SerializeField] private float floatFrequency = 3.6f;
        [SerializeField] private float pulseAmplitude = 0.06f;
        [SerializeField] private float pulseFrequency = 4.2f;

        private Vector3 baseLocalPosition;
        private Vector3 baseLocalScale = Vector3.one;
        private bool isActive;

        private void Awake()
        {
            if (animatedRoot == null)
            {
                animatedRoot = transform as RectTransform;
            }

            if (animatedRoot != null)
            {
                baseLocalPosition = animatedRoot.localPosition;
                baseLocalScale = animatedRoot.localScale;
            }

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!isActive || animatedRoot == null)
            {
                return;
            }

            var normalized = (Mathf.Sin(Time.unscaledTime * floatFrequency) + 1f) * 0.5f;
            var targetY = Mathf.Lerp(MinLocalY, MaxLocalY, normalized);
            var pulseScale = 1f + Mathf.Sin(Time.unscaledTime * pulseFrequency) * pulseAmplitude;

            animatedRoot.localPosition = new Vector3(baseLocalPosition.x, targetY, baseLocalPosition.z);
            animatedRoot.localScale = baseLocalScale * pulseScale;
        }

        public void SetState(bool active, string label, Color color)
        {
            isActive = active;
            gameObject.SetActive(active);
            if (!active)
            {
                return;
            }

            if (animatedRoot != null)
            {
                animatedRoot.localPosition = new Vector3(baseLocalPosition.x, MinLocalY, baseLocalPosition.z);
                animatedRoot.localScale = baseLocalScale;
            }

            if (labelText != null)
            {
                labelText.text = string.IsNullOrWhiteSpace(label) ? "行动中" : label;
                labelText.color = color;
            }

            if (arrowText != null)
            {
                arrowText.color = color;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(color.r * 0.32f, color.g * 0.32f, color.b * 0.32f, 0.9f);
            }
        }
    }
}
