using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class BattleHudUI : CombatUIScreen
    {
        [Header("Runtime Slots")]
        [SerializeField] private RectTransform activeUnitPanel;
        [SerializeField] private RectTransform skillPanel;
        [SerializeField] private RectTransform skillListContent;
        [SerializeField] private Button skillButtonTemplate;
        [SerializeField] private RectTransform targetPanel;
        [SerializeField] private RectTransform logFrame;
        [SerializeField] private Image activePortraitImage;
        [SerializeField] private Text activeNameText;
        [SerializeField] private Text activeMetaText;
        [SerializeField] private Text activeStatsText;
        [SerializeField] private Text targetNameText;
        [SerializeField] private Text targetMetaText;
        [SerializeField] private Text roundText;
        [SerializeField] private Text turnText;
        [SerializeField] private Text logText;
        [SerializeField] private Text infoText;
        [SerializeField] private Text goldText;
        [SerializeField] private ScrollRect logScrollRect;
        [SerializeField] private Text logTitleText;
        [SerializeField] private Text skillTitleText;
        [SerializeField] private Text targetTitleText;

        [Header("Replaceable UI Art Slots")]
        public Image battleHudBg;
        public Image logFrameBg;
        public Image skillPanelBg;
        public Image targetPanelBg;

        public RectTransform SkillPanel => skillPanel;
        public RectTransform SkillListContent => skillListContent;
        public Button SkillButtonTemplate => skillButtonTemplate;
        public RectTransform TargetPanel => targetPanel;
        public Text RoundText => roundText;
        public Text TurnText => turnText;
        public Text LogText => logText;
        public Text InfoText => infoText;
        public Text GoldText => goldText;
        public ScrollRect LogScrollRect => logScrollRect;
        public Image ActivePortraitImage => activePortraitImage;
        public Text ActiveNameText => activeNameText;
        public Text ActiveMetaText => activeMetaText;
        public Text ActiveStatsText => activeStatsText;
        public Text TargetNameText => targetNameText;
        public Text TargetMetaText => targetMetaText;

        public override void BuildLayout()
        {
            // Prefab-driven HUD. Runtime must not rebuild layout or override authored positions.
        }

        public void Build()
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("BattleHudUI prefab layout is incomplete. Runtime layout rebuilding is disabled; repair the prefab instead.", this);
            }
        }

        public void RebuildLayoutFromCode(Sprite panelSprite)
        {
            var rootStyle = CombatUIImageStyle.Capture(battleHudBg);
            var logStyle = CombatUIImageStyle.Capture(logFrameBg);
            var skillStyle = CombatUIImageStyle.Capture(skillPanelBg);
            var targetStyle = CombatUIImageStyle.Capture(targetPanelBg);
            CombatUIScreenUtility.ClearChildren(transform);
            var root = PrepareRoot();

            turnText = CombatUIScreenUtility.CreateText("TurnText", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(760f, 42f), 22, TextAnchor.MiddleCenter);
            CombatUIScreenUtility.SetTextStyle(turnText, new Color(0.96f, 0.86f, 0.62f), true);

            goldText = CombatUIScreenUtility.CreateText("GoldText", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-145f, -34f), new Vector2(260f, 38f), 18, TextAnchor.MiddleRight);
            CombatUIScreenUtility.SetTextStyle(goldText, new Color(1f, 0.82f, 0.36f), true);

            roundText = CombatUIScreenUtility.CreateText("RoundText", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(360f, 34f), 18, TextAnchor.MiddleCenter);
            CombatUIScreenUtility.SetTextStyle(roundText, new Color(0.82f, 0.72f, 0.54f), true);

            battleHudBg = CombatUIScreenUtility.CreatePanel("BottomBar", root, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 210f), new Color(0.025f, 0.024f, 0.026f, 0.96f)).GetComponent<Image>();
            rootStyle.ApplyTo(battleHudBg);
            battleHudBg.sprite = battleHudBg.sprite != null ? battleHudBg.sprite : panelSprite;
            battleHudBg.type = Image.Type.Sliced;
            var bottomBar = battleHudBg.rectTransform;
            bottomBar.pivot = new Vector2(0.5f, 0f);
            bottomBar.offsetMin = new Vector2(0f, 18f);
            bottomBar.offsetMax = new Vector2(0f, 228f);

            logFrameBg = CombatUIScreenUtility.CreatePanel("LogFrame", bottomBar, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(190f, 0f), new Vector2(360f, -28f), new Color(0.055f, 0.052f, 0.054f, 1f)).GetComponent<Image>();
            logStyle.ApplyTo(logFrameBg);
            logFrameBg.sprite = logFrameBg.sprite != null ? logFrameBg.sprite : panelSprite;
            logFrameBg.type = Image.Type.Sliced;
            logFrame = logFrameBg.rectTransform;
            logFrame.pivot = new Vector2(0f, 0.5f);
            logFrame.offsetMin = new Vector2(18f, 18f);
            logFrame.offsetMax = new Vector2(378f, -18f);

            logTitleText = CombatUIScreenUtility.CreateText("LogTitle", logFrame, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -18f), new Vector2(-24f, 24f), 16, TextAnchor.MiddleLeft);
            logTitleText.text = "战斗记录";
            CombatUIScreenUtility.SetTextStyle(logTitleText, new Color(0.9f, 0.65f, 0.38f), true);
            logTitleText.rectTransform.offsetMin = new Vector2(14f, -34f);
            logTitleText.rectTransform.offsetMax = new Vector2(-14f, -10f);

            logScrollRect = CreateLogScroll(logFrame);

            var centerColumnBg = CombatUIScreenUtility.CreatePanel("CenterColumn", bottomBar, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0.03f, 0.03f, 0.035f, 0f)).GetComponent<Image>();
            var centerColumn = centerColumnBg.rectTransform;
            centerColumn.offsetMin = new Vector2(396f, 18f);
            centerColumn.offsetMax = new Vector2(-396f, -18f);

            var activeUnitPanelBg = CombatUIScreenUtility.CreatePanel("ActiveUnitPanel", centerColumn, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 94f), new Color(0.06f, 0.05f, 0.045f, 1f)).GetComponent<Image>();
            activeUnitPanelBg.sprite = panelSprite;
            activeUnitPanelBg.type = Image.Type.Sliced;
            activeUnitPanel = activeUnitPanelBg.rectTransform;
            activeUnitPanel.offsetMin = new Vector2(0f, -94f);
            activeUnitPanel.offsetMax = Vector2.zero;

            var portraitFrame = CombatUIScreenUtility.CreatePanel("ActivePortraitFrame", activeUnitPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(52f, 0f), new Vector2(72f, 72f), new Color(0.12f, 0.09f, 0.07f, 1f)).GetComponent<Image>();
            portraitFrame.sprite = panelSprite;
            portraitFrame.type = Image.Type.Sliced;
            activePortraitImage = CombatUIScreenUtility.CreatePanel("ActivePortrait", portraitFrame.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.16f, 0.16f, 0.17f, 1f)).GetComponent<Image>();
            activePortraitImage.rectTransform.offsetMin = new Vector2(6f, 6f);
            activePortraitImage.rectTransform.offsetMax = new Vector2(-6f, -6f);

            activeNameText = CombatUIScreenUtility.CreateText("ActiveNameText", activeUnitPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 22, TextAnchor.UpperLeft);
            activeNameText.rectTransform.offsetMin = new Vector2(102f, -38f);
            activeNameText.rectTransform.offsetMax = new Vector2(-16f, -8f);
            CombatUIScreenUtility.SetTextStyle(activeNameText, new Color(0.96f, 0.86f, 0.62f), true);

            activeMetaText = CombatUIScreenUtility.CreateText("ActiveMetaText", activeUnitPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 14, TextAnchor.UpperLeft);
            activeMetaText.rectTransform.offsetMin = new Vector2(102f, -62f);
            activeMetaText.rectTransform.offsetMax = new Vector2(-16f, -36f);
            CombatUIScreenUtility.SetTextStyle(activeMetaText, new Color(0.82f, 0.74f, 0.6f), false);

            activeStatsText = CombatUIScreenUtility.CreateText("ActiveStatsText", activeUnitPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 14, TextAnchor.UpperLeft);
            activeStatsText.rectTransform.offsetMin = new Vector2(102f, 14f);
            activeStatsText.rectTransform.offsetMax = new Vector2(-16f, -64f);
            CombatUIScreenUtility.SetTextStyle(activeStatsText, new Color(0.88f, 0.84f, 0.76f), false);

            skillPanelBg = CombatUIScreenUtility.CreatePanel("SkillPanel", centerColumn, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.035f, 0.032f, 1f)).GetComponent<Image>();
            skillStyle.ApplyTo(skillPanelBg);
            skillPanelBg.sprite = skillPanelBg.sprite != null ? skillPanelBg.sprite : panelSprite;
            skillPanelBg.type = Image.Type.Sliced;
            skillPanel = skillPanelBg.rectTransform;
            skillPanel.offsetMin = new Vector2(0f, 0f);
            skillPanel.offsetMax = new Vector2(0f, -102f);

            skillTitleText = CombatUIScreenUtility.CreateText("SkillTitle", skillPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleLeft);
            skillTitleText.text = "技能";
            CombatUIScreenUtility.SetTextStyle(skillTitleText, new Color(0.96f, 0.82f, 0.48f), true);
            skillTitleText.rectTransform.offsetMin = new Vector2(16f, -34f);
            skillTitleText.rectTransform.offsetMax = new Vector2(-16f, -10f);

            skillListContent = CreateSkillListScroll(skillPanel);
            skillButtonTemplate = CreateSkillButtonTemplate(skillListContent, panelSprite);

            targetPanelBg = CombatUIScreenUtility.CreatePanel("TargetPanel", bottomBar, new Vector2(1f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0.045f, 0.045f, 0.052f, 1f)).GetComponent<Image>();
            targetStyle.ApplyTo(targetPanelBg);
            targetPanelBg.sprite = targetPanelBg.sprite != null ? targetPanelBg.sprite : panelSprite;
            targetPanelBg.type = Image.Type.Sliced;
            targetPanel = targetPanelBg.rectTransform;
            targetPanel.pivot = new Vector2(1f, 0.5f);
            targetPanel.offsetMin = new Vector2(-378f, 18f);
            targetPanel.offsetMax = new Vector2(-18f, -18f);

            targetTitleText = CombatUIScreenUtility.CreateText("TargetTitle", targetPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleLeft);
            targetTitleText.text = "角色信息";
            CombatUIScreenUtility.SetTextStyle(targetTitleText, new Color(0.96f, 0.82f, 0.48f), true);
            targetTitleText.rectTransform.offsetMin = new Vector2(16f, -34f);
            targetTitleText.rectTransform.offsetMax = new Vector2(-16f, -10f);

            targetNameText = CombatUIScreenUtility.CreateText("TargetNameText", targetPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 20, TextAnchor.UpperLeft);
            targetNameText.rectTransform.offsetMin = new Vector2(16f, -64f);
            targetNameText.rectTransform.offsetMax = new Vector2(-16f, -38f);
            CombatUIScreenUtility.SetTextStyle(targetNameText, new Color(0.96f, 0.88f, 0.72f), true);

            targetMetaText = CombatUIScreenUtility.CreateText("TargetMetaText", targetPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 14, TextAnchor.UpperLeft);
            targetMetaText.rectTransform.offsetMin = new Vector2(16f, -88f);
            targetMetaText.rectTransform.offsetMax = new Vector2(-16f, -64f);
            CombatUIScreenUtility.SetTextStyle(targetMetaText, new Color(0.82f, 0.74f, 0.6f), false);

            infoText = CombatUIScreenUtility.CreateText("InfoText", targetPanel, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft);
            infoText.rectTransform.offsetMin = new Vector2(16f, 18f);
            infoText.rectTransform.offsetMax = new Vector2(-16f, -98f);
            infoText.text = "点击角色查看属性";
        }

        private bool TryBindExistingLayout()
        {
            if (skillPanel != null && targetPanel != null && logFrame != null && roundText != null && turnText != null && logText != null && infoText != null && goldText != null && logScrollRect != null)
            {
                return true;
            }

            turnText = transform.Find("TurnText")?.GetComponent<Text>();
            goldText = transform.Find("GoldText")?.GetComponent<Text>();
            roundText = transform.Find("RoundText")?.GetComponent<Text>();
            battleHudBg = transform.Find("BottomBar")?.GetComponent<Image>();
            var bottomBar = battleHudBg != null ? battleHudBg.rectTransform : null;
            logFrameBg = bottomBar != null ? bottomBar.Find("LogFrame")?.GetComponent<Image>() : null;
            logFrame = logFrameBg != null ? logFrameBg.rectTransform : null;
            logTitleText = logFrame != null ? logFrame.Find("LogTitle")?.GetComponent<Text>() : null;
            logScrollRect = logFrame != null ? logFrame.Find("LogScroll")?.GetComponent<ScrollRect>() : null;
            logText = logScrollRect != null ? logScrollRect.transform.Find("Viewport/Content/LogText")?.GetComponent<Text>() : null;
            activeUnitPanel = bottomBar != null ? bottomBar.Find("CenterColumn/ActiveUnitPanel") as RectTransform : null;
            activePortraitImage = activeUnitPanel != null ? activeUnitPanel.Find("ActivePortraitFrame/ActivePortrait")?.GetComponent<Image>() : null;
            activeNameText = activeUnitPanel != null ? activeUnitPanel.Find("ActiveNameText")?.GetComponent<Text>() : null;
            activeMetaText = activeUnitPanel != null ? activeUnitPanel.Find("ActiveMetaText")?.GetComponent<Text>() : null;
            activeStatsText = activeUnitPanel != null ? activeUnitPanel.Find("ActiveStatsText")?.GetComponent<Text>() : null;
            skillPanelBg = bottomBar != null ? bottomBar.Find("SkillPanel")?.GetComponent<Image>() : null;
            if (skillPanelBg == null && bottomBar != null)
            {
                skillPanelBg = bottomBar.Find("CenterColumn/SkillPanel")?.GetComponent<Image>();
            }
            skillPanel = skillPanelBg != null ? skillPanelBg.rectTransform : null;
            skillTitleText = skillPanel != null ? skillPanel.Find("SkillTitle")?.GetComponent<Text>() : null;
            skillListContent = skillPanel != null ? skillPanel.Find("SkillListScroll/Viewport/Content") as RectTransform : null;
            skillButtonTemplate = skillListContent != null ? skillListContent.Find("SkillButtonTemplate")?.GetComponent<Button>() : null;
            targetPanelBg = bottomBar != null ? bottomBar.Find("TargetPanel")?.GetComponent<Image>() : null;
            targetPanel = targetPanelBg != null ? targetPanelBg.rectTransform : null;
            targetTitleText = targetPanel != null ? targetPanel.Find("TargetTitle")?.GetComponent<Text>() : null;
            targetNameText = targetPanel != null ? targetPanel.Find("TargetNameText")?.GetComponent<Text>() : null;
            targetMetaText = targetPanel != null ? targetPanel.Find("TargetMetaText")?.GetComponent<Text>() : null;
            infoText = targetPanel != null ? targetPanel.Find("InfoText")?.GetComponent<Text>() : null;

            return skillPanel != null && targetPanel != null && logFrame != null && roundText != null && turnText != null && logText != null && infoText != null && goldText != null && logScrollRect != null;
        }

        private RectTransform CreateSkillListScroll(RectTransform parent)
        {
            var scrollObject = new GameObject("SkillListScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(parent, false);
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(12f, 12f);
            scrollRectTransform.offsetMax = new Vector2(-12f, -42f);
            scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObject.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-14f, 0f);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(scrollObject.transform, false);
            var scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(10f, 0f);
            scrollbarObject.GetComponent<Image>().color = new Color(0.09f, 0.075f, 0.065f, 1f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(scrollbarObject.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            handle.GetComponent<Image>().color = new Color(0.72f, 0.35f, 0.18f, 1f);

            var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;

            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.vertical = true;
            scroll.horizontal = false;
            scroll.verticalScrollbar = scrollbar;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            return contentRect;
        }

        private Button CreateSkillButtonTemplate(RectTransform parent, Sprite fallbackPanelSprite)
        {
            var buttonObject = new GameObject("SkillButtonTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.SetActive(false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 72f);

            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 72f;
            layout.minHeight = 72f;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.12f, 0.1f, 1f);
            image.sprite = fallbackPanelSprite;
            image.type = Image.Type.Sliced;

            var nameText = CombatUIScreenUtility.CreateText("SkillNameText", buttonObject.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 18, TextAnchor.UpperLeft);
            nameText.rectTransform.offsetMin = new Vector2(16f, -30f);
            nameText.rectTransform.offsetMax = new Vector2(-16f, -8f);
            CombatUIScreenUtility.SetTextStyle(nameText, new Color(0.96f, 0.88f, 0.68f), true);

            var metaText = CombatUIScreenUtility.CreateText("SkillMetaText", buttonObject.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 13, TextAnchor.UpperLeft);
            metaText.rectTransform.offsetMin = new Vector2(16f, -50f);
            metaText.rectTransform.offsetMax = new Vector2(-16f, -28f);
            CombatUIScreenUtility.SetTextStyle(metaText, new Color(0.82f, 0.74f, 0.6f), false);

            var hintText = CombatUIScreenUtility.CreateText("SkillHintText", buttonObject.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, Vector2.zero, 12, TextAnchor.LowerLeft);
            hintText.rectTransform.offsetMin = new Vector2(16f, 8f);
            hintText.rectTransform.offsetMax = new Vector2(-16f, 26f);
            CombatUIScreenUtility.SetTextStyle(hintText, new Color(0.72f, 0.66f, 0.58f), false);

            return buttonObject.GetComponent<Button>();
        }

        private ScrollRect CreateLogScroll(RectTransform parent)
        {
            var scrollObject = new GameObject("LogScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(parent, false);
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(12f, 12f);
            scrollRectTransform.offsetMax = new Vector2(-12f, -42f);
            scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObject.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-16f, 0f);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.05f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            logText = CombatUIScreenUtility.CreateText("LogText", content.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft);
            logText.verticalOverflow = VerticalWrapMode.Overflow;
            logText.rectTransform.pivot = new Vector2(0.5f, 1f);
            logText.rectTransform.sizeDelta = new Vector2(0f, 0f);

            var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(scrollObject.transform, false);
            var scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(12f, 0f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarObject.GetComponent<Image>().color = new Color(0.09f, 0.075f, 0.065f, 1f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(scrollbarObject.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            handle.GetComponent<Image>().color = new Color(0.72f, 0.35f, 0.18f, 1f);

            var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;

            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.vertical = true;
            scroll.horizontal = false;
            scroll.verticalScrollbar = scrollbar;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            return scroll;
        }
    }
}
