using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ClockworkWasteland.Combat
{
    public sealed class DebuffStatusBarFeature : MonoBehaviour, ICombatantViewFeature
    {
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private RectTransform iconRoot;
        [SerializeField] private Image iconTemplate;
        [SerializeField] private RectTransform tooltipRoot;
        [SerializeField] private Image tooltipBackground;
        [SerializeField] private Text tooltipNameText;
        [SerializeField] private Text tooltipBodyText;
        [SerializeField] private Vector2 iconSize = new Vector2(16f, 16f);
        [SerializeField] private float iconSpacing = 18f;

        private readonly List<Image> activeIcons = new List<Image>();
        private readonly List<StatusInstance> activeStatuses = new List<StatusInstance>();
        private CombatantView ownerView;
        private BattleUnit boundUnit;

        public void Bind(CombatantView view, BattleUnit unit)
        {
            ownerView = view;
            boundUnit = unit;
            EnsureHierarchy();
            Refresh(unit);
        }

        public void Refresh(BattleUnit unit)
        {
            boundUnit = unit;
            EnsureHierarchy();
            if (unit == null || iconRoot == null)
            {
                SetAllInactive();
                return;
            }

            var visibleIndex = 0;
            foreach (var status in unit.Statuses)
            {
                if (status == null || status.TurnsRemaining < 0 || status.Icon == null)
                {
                    continue;
                }

                var icon = GetOrCreateIcon(visibleIndex++);
                icon.sprite = status.Icon;
                icon.color = Color.white;
                icon.enabled = true;
                activeStatuses[visibleIndex - 1] = status;
            }

            for (var i = visibleIndex; i < activeIcons.Count; i++)
            {
                activeIcons[i].enabled = false;
                activeStatuses[i] = null;
            }

            HideTooltip();
        }

        private void Update()
        {
            if (boundUnit == null || tooltipRoot == null || iconRoot == null)
            {
                return;
            }

            var cameraToUse = worldCanvas != null && worldCanvas.worldCamera != null ? worldCanvas.worldCamera : Camera.main;
            StatusInstance hoveredStatus = null;
            RectTransform hoveredRect = null;

            for (var i = 0; i < activeIcons.Count; i++)
            {
                var icon = activeIcons[i];
                var status = i < activeStatuses.Count ? activeStatuses[i] : null;
                if (icon == null || !icon.enabled || status == null)
                {
                    continue;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(icon.rectTransform, Input.mousePosition, cameraToUse))
                {
                    hoveredStatus = status;
                    hoveredRect = icon.rectTransform;
                    break;
                }
            }

            if (hoveredStatus == null || hoveredRect == null)
            {
                HideTooltip();
                return;
            }

            ShowTooltip(hoveredStatus, hoveredRect);
        }

        private void EnsureHierarchy()
        {
            if (worldCanvas == null)
            {
                worldCanvas = GetComponent<Canvas>();
            }

            if (worldCanvas != null)
            {
                worldCanvas.renderMode = RenderMode.WorldSpace;
            }

            if (iconRoot == null)
            {
                var existing = transform.Find("IconRoot");
                if (existing != null)
                {
                    iconRoot = existing as RectTransform;
                }
            }

            if (iconRoot == null)
            {
                var iconRootObject = new GameObject("IconRoot", typeof(RectTransform));
                iconRootObject.transform.SetParent(transform, false);
                iconRoot = iconRootObject.GetComponent<RectTransform>();
                iconRoot.anchorMin = new Vector2(0.5f, 0.5f);
                iconRoot.anchorMax = new Vector2(0.5f, 0.5f);
                iconRoot.pivot = new Vector2(0.5f, 0.5f);
                iconRoot.anchoredPosition = Vector2.zero;
                iconRoot.sizeDelta = new Vector2(96f, 18f);
            }

            if (iconTemplate == null)
            {
                var templateTransform = iconRoot.Find("IconTemplate");
                iconTemplate = templateTransform != null ? templateTransform.GetComponent<Image>() : null;
            }

            if (iconTemplate == null)
            {
                var iconTemplateObject = new GameObject("IconTemplate", typeof(RectTransform), typeof(Image));
                iconTemplateObject.transform.SetParent(iconRoot, false);
                var rect = iconTemplateObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = iconSize;
                iconTemplate = iconTemplateObject.GetComponent<Image>();
                iconTemplate.preserveAspect = true;
                iconTemplate.enabled = false;
            }

            var templateLayout = iconTemplate.GetComponent<LayoutElement>();
            if (templateLayout == null)
            {
                templateLayout = iconTemplate.gameObject.AddComponent<LayoutElement>();
            }

            templateLayout.ignoreLayout = true;

            if (tooltipRoot == null)
            {
                var tooltipTransform = transform.Find("Tooltip");
                tooltipRoot = tooltipTransform as RectTransform;
            }

            if (tooltipRoot == null)
            {
                var tooltipObject = new GameObject("Tooltip", typeof(RectTransform), typeof(Image));
                tooltipObject.transform.SetParent(transform, false);
                tooltipRoot = tooltipObject.GetComponent<RectTransform>();
                tooltipRoot.anchorMin = new Vector2(0f, 0.5f);
                tooltipRoot.anchorMax = new Vector2(0f, 0.5f);
                tooltipRoot.pivot = new Vector2(0f, 0.5f);
                tooltipRoot.anchoredPosition = new Vector2(26f, 20f);
                tooltipRoot.sizeDelta = new Vector2(176f, 54f);
            }

            if (tooltipBackground == null)
            {
                tooltipBackground = tooltipRoot.GetComponent<Image>();
            }

            if (tooltipBackground == null)
            {
                tooltipBackground = tooltipRoot.gameObject.AddComponent<Image>();
            }

            tooltipBackground.color = new Color(0.05f, 0.05f, 0.05f, 0.92f);
            tooltipBackground.raycastTarget = false;

            if (tooltipNameText == null)
            {
                var nameTransform = tooltipRoot.Find("NameText");
                tooltipNameText = nameTransform != null ? nameTransform.GetComponent<Text>() : null;
            }

            if (tooltipNameText == null)
            {
                var nameObject = new GameObject("NameText", typeof(RectTransform), typeof(Text));
                nameObject.transform.SetParent(tooltipRoot, false);
                var nameRect = nameObject.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0f, 1f);
                nameRect.anchorMax = new Vector2(1f, 1f);
                nameRect.pivot = new Vector2(0f, 1f);
                nameRect.offsetMin = new Vector2(8f, -24f);
                nameRect.offsetMax = new Vector2(-8f, -6f);
                tooltipNameText = nameObject.GetComponent<Text>();
            }

            tooltipNameText.font = ChineseFontProvider.LegacyFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            tooltipNameText.fontSize = 15;
            tooltipNameText.fontStyle = FontStyle.Bold;
            tooltipNameText.alignment = TextAnchor.UpperLeft;
            tooltipNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tooltipNameText.verticalOverflow = VerticalWrapMode.Truncate;
            tooltipNameText.raycastTarget = false;

            if (tooltipBodyText == null)
            {
                var bodyTransform = tooltipRoot.Find("BodyText");
                tooltipBodyText = bodyTransform != null ? bodyTransform.GetComponent<Text>() : null;
            }

            if (tooltipBodyText == null)
            {
                var bodyObject = new GameObject("BodyText", typeof(RectTransform), typeof(Text));
                bodyObject.transform.SetParent(tooltipRoot, false);
                var bodyRect = bodyObject.GetComponent<RectTransform>();
                bodyRect.anchorMin = new Vector2(0f, 0f);
                bodyRect.anchorMax = new Vector2(1f, 1f);
                bodyRect.offsetMin = new Vector2(8f, 6f);
                bodyRect.offsetMax = new Vector2(-8f, -26f);
                tooltipBodyText = bodyObject.GetComponent<Text>();
            }

            tooltipBodyText.font = ChineseFontProvider.LegacyFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            tooltipBodyText.fontSize = 13;
            tooltipBodyText.alignment = TextAnchor.UpperLeft;
            tooltipBodyText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            tooltipBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tooltipBodyText.verticalOverflow = VerticalWrapMode.Overflow;
            tooltipBodyText.raycastTarget = false;

            HideTooltip();
        }

        private Image GetOrCreateIcon(int index)
        {
            while (activeIcons.Count <= index)
            {
                var icon = Instantiate(iconTemplate, iconRoot);
                icon.name = $"Icon_{activeIcons.Count}";
                icon.enabled = false;
                var rect = icon.rectTransform;
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = iconSize;
                var layout = icon.GetComponent<LayoutElement>();
                if (layout == null)
                {
                    layout = icon.gameObject.AddComponent<LayoutElement>();
                }

                layout.ignoreLayout = false;
                activeIcons.Add(icon);
                activeStatuses.Add(null);
            }

            var targetIcon = activeIcons[index];
            if (iconRoot.GetComponent<GridLayoutGroup>() == null &&
                iconRoot.GetComponent<HorizontalLayoutGroup>() == null &&
                iconRoot.GetComponent<VerticalLayoutGroup>() == null)
            {
                targetIcon.rectTransform.anchoredPosition = new Vector2(index * iconSpacing, 0f);
            }
            return targetIcon;
        }

        private void SetAllInactive()
        {
            for (var i = 0; i < activeIcons.Count; i++)
            {
                var icon = activeIcons[i];
                if (icon != null)
                {
                    icon.enabled = false;
                }

                if (i < activeStatuses.Count)
                {
                    activeStatuses[i] = null;
                }
            }

            HideTooltip();
        }

        private void ShowTooltip(StatusInstance status, RectTransform iconRect)
        {
            if (status == null || tooltipRoot == null || tooltipNameText == null || tooltipBodyText == null)
            {
                return;
            }

            tooltipNameText.text = status.DisplayName;
            tooltipNameText.color = status.NameTextColor;
            tooltipBodyText.text = $"状态持续{Mathf.Max(0, status.TurnsRemaining)}回合";
            tooltipRoot.anchoredPosition = iconRect.anchoredPosition + new Vector2(22f, 24f);
            if (!tooltipRoot.gameObject.activeSelf)
            {
                tooltipRoot.gameObject.SetActive(true);
            }
        }

        private void HideTooltip()
        {
            if (tooltipRoot != null && tooltipRoot.gameObject.activeSelf)
            {
                tooltipRoot.gameObject.SetActive(false);
            }
        }
    }
}
