using ClockworkWasteland.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.EditorTools
{
    public sealed class DebuffStatusBarFeaturePatch : CombatUnitFeaturePatch
    {
        private const float CanvasScale = 0.01f;

        public override string FeatureId => "debuff_status_bar";
        public override string TemplatePrefabPath => "Assets/ClockworkWastelandDemo/Prefabs/FeatureTemplates/DebuffStatusBar_Template.prefab";
        public override string FeatureRootName => "DebuffStatusBar";
        public override int Version => 4;
        protected override System.Collections.Generic.IEnumerable<string> LegacyFeatureRootNames => new[] { "DebuffStatusBar_Template" };

        protected override GameObject CreateTemplateRoot()
        {
            var root = new GameObject("DebuffStatusBar", typeof(RectTransform), typeof(Canvas), typeof(DebuffStatusBarFeature));
            root.transform.localPosition = new Vector3(0f, 2.15f, -0.2f);
            root.transform.localScale = Vector3.one * CanvasScale;

            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(120f, 18f);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 18;

            var iconRootObject = new GameObject("IconRoot", typeof(RectTransform));
            iconRootObject.transform.SetParent(root.transform, false);
            var iconRootRect = iconRootObject.GetComponent<RectTransform>();
            iconRootRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRootRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRootRect.pivot = new Vector2(0.5f, 0.5f);
            iconRootRect.anchoredPosition = Vector2.zero;
            iconRootRect.sizeDelta = new Vector2(96f, 18f);

            var iconTemplateObject = new GameObject("IconTemplate", typeof(RectTransform), typeof(Image));
            iconTemplateObject.transform.SetParent(iconRootObject.transform, false);
            var templateRect = iconTemplateObject.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0.5f);
            templateRect.anchorMax = new Vector2(0f, 0.5f);
            templateRect.pivot = new Vector2(0.5f, 0.5f);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(16f, 16f);

            var image = iconTemplateObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.enabled = false;
            var templateLayout = iconTemplateObject.AddComponent<LayoutElement>();
            templateLayout.ignoreLayout = true;

            CreateTooltip(root.transform);
            return root;
        }

        protected override bool UpgradeTemplatePrefab(GameObject templateRoot)
        {
            if (templateRoot == null)
            {
                return false;
            }

            var changed = false;
            var tooltip = templateRoot.transform.Find("Tooltip");
            if (tooltip == null)
            {
                CreateTooltip(templateRoot.transform);
                changed = true;
            }

            var iconTemplate = templateRoot.transform.Find("IconRoot/IconTemplate");
            if (iconTemplate != null)
            {
                var layout = iconTemplate.GetComponent<LayoutElement>();
                if (layout == null)
                {
                    layout = iconTemplate.gameObject.AddComponent<LayoutElement>();
                    changed = true;
                }

                if (!layout.ignoreLayout)
                {
                    layout.ignoreLayout = true;
                    changed = true;
                }
            }

            return changed;
        }

        private static void CreateTooltip(Transform parent)
        {
            var tooltipObject = new GameObject("Tooltip", typeof(RectTransform), typeof(Image));
            tooltipObject.transform.SetParent(parent, false);
            var tooltipRect = tooltipObject.GetComponent<RectTransform>();
            tooltipRect.anchorMin = new Vector2(0f, 0.5f);
            tooltipRect.anchorMax = new Vector2(0f, 0.5f);
            tooltipRect.pivot = new Vector2(0f, 0.5f);
            tooltipRect.anchoredPosition = new Vector2(26f, 20f);
            tooltipRect.sizeDelta = new Vector2(176f, 54f);

            var tooltipBackground = tooltipObject.GetComponent<Image>();
            tooltipBackground.color = new Color(0.05f, 0.05f, 0.05f, 0.92f);
            tooltipBackground.raycastTarget = false;

            var nameObject = new GameObject("NameText", typeof(RectTransform), typeof(Text));
            nameObject.transform.SetParent(tooltipObject.transform, false);
            var nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.offsetMin = new Vector2(8f, -24f);
            nameRect.offsetMax = new Vector2(-8f, -6f);
            var nameText = nameObject.GetComponent<Text>();
            nameText.font = ChineseFontProvider.LegacyFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = 15;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.UpperLeft;
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.verticalOverflow = VerticalWrapMode.Truncate;
            nameText.raycastTarget = false;

            var bodyObject = new GameObject("BodyText", typeof(RectTransform), typeof(Text));
            bodyObject.transform.SetParent(tooltipObject.transform, false);
            var bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(8f, 6f);
            bodyRect.offsetMax = new Vector2(-8f, -26f);
            var bodyText = bodyObject.GetComponent<Text>();
            bodyText.font = ChineseFontProvider.LegacyFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            bodyText.fontSize = 13;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.raycastTarget = false;

            tooltipObject.SetActive(false);
        }
    }
}
