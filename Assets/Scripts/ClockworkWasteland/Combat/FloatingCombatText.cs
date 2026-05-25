using UnityEngine;

namespace ClockworkWasteland.Combat
{
    public sealed class FloatingCombatText : MonoBehaviour
    {
        private TextMesh label;
        private Color baseColor;
        private Vector3 startPosition;
        private float elapsed;
        private float duration = 0.7f;
        private float riseDistance = 0.65f;
        private float startDelay;

        public void Initialize(string text, Color color, float lifetime = 0.7f, float rise = 0.65f)
        {
            duration = Mathf.Max(0.1f, lifetime);
            riseDistance = Mathf.Max(0.05f, rise);
            startPosition = transform.position;
            baseColor = color;

            label = gameObject.AddComponent<TextMesh>();
            label.text = text;
            label.font = ChineseFontProvider.LegacyFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.characterSize = 0.28f;
            label.fontSize = 48;
            label.fontStyle = FontStyle.Bold;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;

            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 600;
                if (label.font != null)
                {
                    renderer.material = label.font.material;
                }
            }
        }

        public void SetStartDelay(float delay)
        {
            startDelay = Mathf.Max(0f, delay);
            if (label != null && startDelay > 0f)
            {
                label.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
            }
        }

        private void Update()
        {
            if (startDelay > 0f)
            {
                startDelay -= Time.deltaTime;
                if (label != null)
                {
                    label.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
                }

                return;
            }

            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            transform.position = startPosition + new Vector3(0f, t * riseDistance, 0f);

            if (label != null)
            {
                label.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
