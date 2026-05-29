using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    [DisallowMultipleComponent]
    public sealed class UIButtonHoverFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Targets")]
        [SerializeField] private RectTransform scaleTarget;
        [SerializeField] private Graphic primaryGraphic;
        [SerializeField] private Graphic[] extraGraphics = Array.Empty<Graphic>();

        [Header("Scale")]
        [SerializeField] private bool enableScale = true;
        [SerializeField] private Vector3 normalScale = Vector3.one;
        [SerializeField] private Vector3 hoverScale = new Vector3(1.08f, 1.08f, 1f);

        [Header("Color")]
        [SerializeField] private bool enableColorTint = true;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 0.85f, 0.55f, 1f);

        [Header("Animation")]
        [SerializeField] private float transitionDuration = 0.12f;
        [SerializeField] private bool useUnscaledTime = true;

        private bool isHovered;
        private float transitionProgress = 1f;
        private Color[] extraGraphicNormalColors = Array.Empty<Color>();

        private void Reset()
        {
            scaleTarget = transform as RectTransform;
            primaryGraphic = GetComponent<Graphic>();
            if (scaleTarget != null)
            {
                normalScale = scaleTarget.localScale;
            }
        }

        private void Awake()
        {
            if (scaleTarget == null)
            {
                scaleTarget = transform as RectTransform;
            }

            if (primaryGraphic == null)
            {
                primaryGraphic = GetComponent<Graphic>();
            }

            if (scaleTarget != null && normalScale == Vector3.zero)
            {
                normalScale = scaleTarget.localScale;
            }

            if (primaryGraphic != null && normalColor == default)
            {
                normalColor = primaryGraphic.color;
            }

            CaptureExtraGraphicColors();
            ApplyStateImmediate(false);
        }

        private void OnEnable()
        {
            ApplyStateImmediate(isHovered);
        }

        private void OnDisable()
        {
            isHovered = false;
            ApplyStateImmediate(false);
        }

        private void Update()
        {
            if (transitionDuration <= 0f)
            {
                ApplyStateImmediate(isHovered);
                return;
            }

            var target = isHovered ? 1f : 0f;
            if (Mathf.Approximately(transitionProgress, target))
            {
                return;
            }

            var delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            transitionProgress = Mathf.MoveTowards(transitionProgress, target, delta / transitionDuration);
            ApplyVisuals(transitionProgress);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            if (transitionDuration <= 0f)
            {
                ApplyStateImmediate(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            if (transitionDuration <= 0f)
            {
                ApplyStateImmediate(false);
            }
        }

        private void CaptureExtraGraphicColors()
        {
            if (extraGraphics == null || extraGraphics.Length == 0)
            {
                extraGraphicNormalColors = Array.Empty<Color>();
                return;
            }

            extraGraphicNormalColors = new Color[extraGraphics.Length];
            for (var i = 0; i < extraGraphics.Length; i++)
            {
                extraGraphicNormalColors[i] = extraGraphics[i] != null ? extraGraphics[i].color : Color.white;
            }
        }

        private void ApplyStateImmediate(bool hovered)
        {
            transitionProgress = hovered ? 1f : 0f;
            ApplyVisuals(transitionProgress);
        }

        private void ApplyVisuals(float t)
        {
            if (enableScale && scaleTarget != null)
            {
                scaleTarget.localScale = Vector3.LerpUnclamped(normalScale, hoverScale, t);
            }

            if (enableColorTint)
            {
                if (primaryGraphic != null)
                {
                    primaryGraphic.color = Color.LerpUnclamped(normalColor, hoverColor, t);
                }

                if (extraGraphics != null)
                {
                    for (var i = 0; i < extraGraphics.Length; i++)
                    {
                        var graphic = extraGraphics[i];
                        if (graphic == null)
                        {
                            continue;
                        }

                        var baseColor = i < extraGraphicNormalColors.Length ? extraGraphicNormalColors[i] : graphic.color;
                        graphic.color = Color.LerpUnclamped(baseColor, hoverColor, t);
                    }
                }
            }
        }
    }
}
