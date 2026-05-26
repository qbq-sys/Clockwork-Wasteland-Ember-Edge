using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ClockworkWasteland.Combat
{
    public sealed partial class SkillDescriptionUI : CombatUIScreen
    {
        private const string SkillDescriptionPanelPrefabPath = CombatUIPaths.SkillDescriptionPanelPrefabPath;
        private static readonly Vector2 TooltipSize = new Vector2(460f, 126f);
        private static readonly Vector2 TooltipOffset = new Vector2(0f, 88f);

        [SerializeField] private SkillDescriptionPanelUI panelPrefab;
        [SerializeField] private SkillDescriptionPanelUI panelInstance;
        [SerializeField] private Text skillDescriptionText;

        public Image skillDescriptionBg;
        public Text SkillDescriptionText => skillDescriptionText;

        public override void BuildLayout()
        {
            // Tooltip uses prefab-driven layout and a dedicated panel prefab.
        }

        public void Build()
        {
            PrepareRoot();
            ConfigureTooltipRect();
            CreateOrBindPanel(transform as RectTransform);
            Hide();
        }

        private void CreateOrBindPanel(RectTransform root)
        {
            if (panelInstance == null)
            {
                panelInstance = GetComponentInChildren<SkillDescriptionPanelUI>(true);
            }

            if (panelInstance == null)
            {
                var source = panelPrefab != null ? panelPrefab : LoadPanelPrefab();
                if (source != null)
                {
                    panelInstance = Instantiate(source, root);
                }
            }

            if (panelInstance == null)
            {
                var panelObject = new GameObject("SkillDescriptionPanelUI", typeof(RectTransform), typeof(SkillDescriptionPanelUI));
                panelObject.transform.SetParent(root, false);
                panelInstance = panelObject.GetComponent<SkillDescriptionPanelUI>();
            }

            CombatUIScreenUtility.Stretch(panelInstance.RectTransform);
            panelInstance.Build();
            skillDescriptionBg = panelInstance.panelBg;
            skillDescriptionText = panelInstance.descriptionText;
        }

        public void ShowNear(RectTransform target, RectTransform canvasRoot, string description)
        {
            if (skillDescriptionText == null || skillDescriptionBg == null)
            {
                return;
            }

            skillDescriptionText.text = description;
            ConfigureTooltipRect();
            PositionNear(target, canvasRoot);
            DisableTooltipRaycasts();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private bool TryBindExistingLayout()
        {
            if (skillDescriptionBg != null && skillDescriptionText != null)
            {
                return true;
            }

            skillDescriptionBg = transform.Find("SkillDescriptionPanel")?.GetComponent<Image>();
            skillDescriptionText = skillDescriptionBg != null ? skillDescriptionBg.transform.Find("SkillDescriptionText")?.GetComponent<Text>() : null;
            return skillDescriptionBg != null && skillDescriptionText != null;
        }

        private void ConfigureTooltipRect()
        {
            var root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = TooltipSize;

            if (skillDescriptionBg != null)
            {
                var panel = skillDescriptionBg.rectTransform;
                panel.anchorMin = Vector2.zero;
                panel.anchorMax = Vector2.one;
                panel.pivot = new Vector2(0.5f, 0.5f);
                panel.anchoredPosition = Vector2.zero;
                panel.offsetMin = Vector2.zero;
                panel.offsetMax = Vector2.zero;
            }
        }

        private static SkillDescriptionPanelUI LoadPanelPrefab()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<SkillDescriptionPanelUI>(SkillDescriptionPanelPrefabPath);
#else
            return null;
#endif
        }

        private void PositionNear(RectTransform target, RectTransform canvasRoot)
        {
            var root = transform as RectTransform;
            if (root == null || target == null || canvasRoot == null)
            {
                return;
            }

            var targetCorners = new Vector3[4];
            target.GetWorldCorners(targetCorners);
            var targetTopCenter = (targetCorners[1] + targetCorners[2]) * 0.5f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, targetTopCenter, null, out var localPoint);

            var desired = localPoint + TooltipOffset;
            var halfWidth = TooltipSize.x * 0.5f;
            var minX = canvasRoot.rect.xMin + halfWidth + 12f;
            var maxX = canvasRoot.rect.xMax - halfWidth - 12f;
            var minY = canvasRoot.rect.yMin + 12f;
            var maxY = canvasRoot.rect.yMax - TooltipSize.y - 12f;

            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            desired.y = Mathf.Clamp(desired.y, minY, maxY);
            root.anchoredPosition = desired;
        }

        private void DisableTooltipRaycasts()
        {
            foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }
    }
}
