using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed class BattleUI : MonoBehaviour
    {
        [Header("Runtime Slots")]
        [SerializeField] private RectTransform skillPanel;
        [SerializeField] private RectTransform targetPanel;
        [SerializeField] private Text roundText;
        [SerializeField] private Text turnText;
        [SerializeField] private Text logText;
        [SerializeField] private Text infoText;
        [SerializeField] private Text skillDescriptionText;
        [SerializeField] private ScrollRect logScrollRect;
        [SerializeField] private RectTransform skillDescriptionPanel;
        [SerializeField] private RectTransform overlayPanel;
        [SerializeField] private Text overlayText;

        private readonly List<string> logLines = new List<string>();
        private Sprite panelSprite;
        private Sprite buttonSprite;
        private Sprite descriptionSprite;

        public void Build()
        {
            EnsureEventSystem();
            EnsureCanvas();
            EnsureSkinSprites();

            if (skillPanel == null || targetPanel == null || roundText == null || turnText == null || logText == null || infoText == null || logScrollRect == null || skillDescriptionPanel == null || skillDescriptionText == null)
            {
                BuildDefaultLayout();
            }
        }

        public void BuildDefaultLayout()
        {
            EnsureCanvas();
            EnsureSkinSprites();
            ClearChildren(transform);

            var root = GetComponent<RectTransform>();
            Stretch(root);

            turnText = CreateText("TurnText", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(760f, 42f), 22, TextAnchor.MiddleCenter);
            SetTextStyle(turnText, new Color(0.96f, 0.86f, 0.62f), true);

            roundText = CreateText("RoundText", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(360f, 34f), 18, TextAnchor.MiddleCenter);
            SetTextStyle(roundText, new Color(0.82f, 0.72f, 0.54f), true);

            var bottomBar = CreatePanel("BottomBar", root, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 210f), new Color(0.025f, 0.024f, 0.026f, 0.96f));
            bottomBar.GetComponent<Image>().sprite = panelSprite;
            bottomBar.GetComponent<Image>().type = Image.Type.Sliced;
            bottomBar.pivot = new Vector2(0.5f, 0f);
            bottomBar.offsetMin = new Vector2(18f, 18f);
            bottomBar.offsetMax = new Vector2(-18f, 228f);

            var logFrame = CreatePanel("LogFrame", bottomBar, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(190f, 0f), new Vector2(360f, -28f), new Color(0.055f, 0.052f, 0.054f, 1f));
            logFrame.GetComponent<Image>().sprite = panelSprite;
            logFrame.GetComponent<Image>().type = Image.Type.Sliced;
            logFrame.pivot = new Vector2(0f, 0.5f);
            logFrame.offsetMin = new Vector2(18f, 18f);
            logFrame.offsetMax = new Vector2(378f, -18f);

            var logTitle = CreateText("LogTitle", logFrame, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -18f), new Vector2(-24f, 24f), 16, TextAnchor.MiddleLeft);
            logTitle.text = "\u6218\u6597\u8bb0\u5f55";
            SetTextStyle(logTitle, new Color(0.9f, 0.65f, 0.38f), true);
            logTitle.rectTransform.offsetMin = new Vector2(14f, -34f);
            logTitle.rectTransform.offsetMax = new Vector2(-14f, -10f);

            logScrollRect = CreateLogScroll(logFrame);

            skillPanel = CreatePanel("SkillPanel", bottomBar, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.035f, 0.032f, 1f));
            skillPanel.GetComponent<Image>().sprite = panelSprite;
            skillPanel.GetComponent<Image>().type = Image.Type.Sliced;
            skillPanel.offsetMin = new Vector2(398f, 18f);
            skillPanel.offsetMax = new Vector2(-398f, -18f);

            var skillTitle = CreateText("SkillTitle", skillPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleLeft);
            skillTitle.text = "\u6280\u80fd";
            SetTextStyle(skillTitle, new Color(0.96f, 0.82f, 0.48f), true);
            skillTitle.rectTransform.offsetMin = new Vector2(16f, -34f);
            skillTitle.rectTransform.offsetMax = new Vector2(-16f, -10f);

            targetPanel = CreatePanel("TargetPanel", bottomBar, new Vector2(1f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0.045f, 0.045f, 0.052f, 1f));
            targetPanel.GetComponent<Image>().sprite = panelSprite;
            targetPanel.GetComponent<Image>().type = Image.Type.Sliced;
            targetPanel.pivot = new Vector2(1f, 0.5f);
            targetPanel.offsetMin = new Vector2(-378f, 18f);
            targetPanel.offsetMax = new Vector2(-18f, -18f);

            var targetTitle = CreateText("TargetTitle", targetPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleLeft);
            targetTitle.text = "\u89d2\u8272\u4fe1\u606f";
            SetTextStyle(targetTitle, new Color(0.96f, 0.82f, 0.48f), true);
            targetTitle.rectTransform.offsetMin = new Vector2(16f, -34f);
            targetTitle.rectTransform.offsetMax = new Vector2(-16f, -10f);

            infoText = CreateText("InfoText", targetPanel, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft);
            infoText.rectTransform.offsetMin = new Vector2(16f, 18f);
            infoText.rectTransform.offsetMax = new Vector2(-16f, -44f);
            infoText.text = "\u70b9\u51fb\u89d2\u8272\u67e5\u770b\u5c5e\u6027";

            skillDescriptionPanel = CreatePanel("SkillDescriptionPanel", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 252f), new Vector2(560f, 142f), new Color(0.04f, 0.029f, 0.025f, 0.95f));
            skillDescriptionPanel.GetComponent<Image>().sprite = descriptionSprite;
            skillDescriptionPanel.GetComponent<Image>().type = Image.Type.Sliced;

            skillDescriptionText = CreateText("SkillDescriptionText", skillDescriptionPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft);
            skillDescriptionText.rectTransform.offsetMin = new Vector2(24f, 18f);
            skillDescriptionText.rectTransform.offsetMax = new Vector2(-24f, -18f);
            skillDescriptionText.text = "\u6280\u80fd\u8bf4\u660e";
            SetTextStyle(skillDescriptionText, new Color(0.94f, 0.84f, 0.65f), false);
        }

        public void RenderPlayerTurn(BattleUnit actor, IReadOnlyList<SkillUseState> skillStates, Action<SkillData> onSkillSelected)
        {
            SetTurn($"{actor.DisplayName} \u884c\u52a8");
            ClearRuntimeButtons(targetPanel);
            AddLog($"\u8bf7\u9009\u62e9 {actor.DisplayName} \u7684\u6280\u80fd\u3002");
            RenderUnitPanel(actor, actor, skillStates, onSkillSelected);
        }

        public void RenderTargets(SkillData skill, IReadOnlyList<BattleUnit> targets, Action<BattleUnit> onTargetSelected)
        {
            ClearRuntimeButtons(targetPanel);
            AddLog($"\u8bf7\u70b9\u51fb\u573a\u666f\u4e2d\u7684\u76ee\u6807\u6765\u4f7f\u7528 {skill.skillName}\u3002");
            AppendInfoHint($"\n\n\u5df2\u9009\u6280\u80fd\uff1a{skill.skillName}\n\u8bf7\u70b9\u51fb\u573a\u666f\u4e2d\u7684\u53ef\u9009\u76ee\u6807\u3002");
        }

        public void RenderUnitPanel(BattleUnit selectedUnit, BattleUnit activeActor, IReadOnlyList<SkillUseState> skillStates, Action<SkillData> onSkillSelected)
        {
            ClearRuntimeButtons(skillPanel);
            ClearRuntimeButtons(targetPanel);
            RenderInfo(selectedUnit, activeActor);

            if (selectedUnit == null || !selectedUnit.IsHero)
            {
                return;
            }

            var canUseSkills = activeActor != null && selectedUnit == activeActor && selectedUnit.CanAct;
            var states = skillStates ?? Array.Empty<SkillUseState>();
            for (var i = 0; i < states.Count; i++)
            {
                var state = states[i];
                var skill = state.Skill;
                var column = i % 2;
                var row = i / 2;
                CreateButton(skillPanel, state.ButtonLabel, new Vector2(92f + column * 178f, -62f - row * 54f), () => onSkillSelected(skill), canUseSkills && state.CanUse, skill);
            }
        }

        private void ShowSkillDescription(SkillData skill)
        {
            if (skillDescriptionText == null || skill == null)
            {
                return;
            }

            var casterRequirement = FormatPositionRequirement(skill.casterAllowedPositions, "\u9700\u8981\u7ad9\u5728");
            skillDescriptionText.text =
                $"{skill.skillName}\n" +
                $"{BuildEffectSummary(skill)}\n" +
                $"\u65bd\u6cd5\u7ad9\u4f4d\uff1a{casterRequirement}\n" +
                $"\u76ee\u6807\u7ad9\u4f4d\uff1a{BuildTargetRequirement(skill)}\n" +
                $"{skill.description}";
        }

        private void RenderInfo(BattleUnit selectedUnit, BattleUnit activeActor)
        {
            if (infoText == null)
            {
                return;
            }

            if (selectedUnit == null)
            {
                infoText.text = "\u70b9\u51fb\u89d2\u8272\u67e5\u770b\u5c5e\u6027";
                return;
            }

            var side = selectedUnit.IsHero ? "\u6211\u65b9" : "\u654c\u65b9";
            var active = selectedUnit == activeActor ? "\n\u5f53\u524d\u884c\u52a8\u89d2\u8272" : string.Empty;
            var status = selectedUnit.Statuses.Count == 0
                ? "\u65e0"
                : string.Join("\uff0c", selectedUnit.Statuses.Select(item => $"{item.DisplayName}({item.TurnsRemaining})"));

            infoText.text =
                $"{selectedUnit.DisplayName}\n" +
                $"{side}{active}\n\n" +
                $"\u751f\u547d\uff1a{selectedUnit.Health}/{selectedUnit.MaxHealth}\n" +
                $"\u7ad9\u4f4d\uff1a{selectedUnit.CurrentPosition}\n" +
                $"\u901f\u5ea6\uff1a{selectedUnit.Speed}\n" +
                $"\u72b6\u6001\uff1a{status}";
        }

        private void AppendInfoHint(string hint)
        {
            if (infoText != null && !string.IsNullOrWhiteSpace(hint))
            {
                infoText.text += hint;
            }
        }

        public void SetRound(int round)
        {
            if (roundText != null)
            {
                roundText.text = $"\u7b2c {round} \u56de\u5408";
            }
        }

        public void SetTurn(string text)
        {
            if (turnText != null)
            {
                turnText.text = text;
            }
        }

        public void AddLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || logText == null)
            {
                return;
            }

            logLines.Add(message);

            logText.text = string.Join("\n", logLines);
            Canvas.ForceUpdateCanvases();
            if (logScrollRect != null)
            {
                logScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        public void ClearActionPanels()
        {
            ClearRuntimeButtons(skillPanel);
        }

        public void ShowContinuePrompt(string message, string buttonLabel, Action onContinue)
        {
            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            overlayText.text = message;
            var buttonParent = overlayPanel.Find("MessagePanel") as RectTransform ?? overlayPanel;
            ClearRuntimeButtons(buttonParent);
            CreateButton(buttonParent, buttonLabel, new Vector2(310f, -166f), () =>
            {
                HideOverlay();
                onContinue?.Invoke();
            }, true, null);
        }

        public void ShowEndScreen(string message)
        {
            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            overlayText.text = message;
            var buttonParent = overlayPanel.Find("MessagePanel") as RectTransform ?? overlayPanel;
            ClearRuntimeButtons(buttonParent);
        }

        public void ShowTeamSelection(
            IReadOnlyList<CombatantDefinition> heroPool,
            IReadOnlyList<CombatantDefinition> selectedHeroes,
            Action<CombatantDefinition> onToggleHero,
            Action onStartBattle)
        {
            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            ClearChildren(overlayPanel);

            var rootPanel = CreatePanel("TeamSelectionPanel", overlayPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 760f), new Color(0.032f, 0.026f, 0.024f, 0.97f));
            rootPanel.GetComponent<Image>().sprite = descriptionSprite;
            rootPanel.GetComponent<Image>().type = Image.Type.Sliced;

            var title = CreateText("Title", rootPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            title.text = "\u961f\u4f0d\u914d\u7f6e";
            SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var subtitle = CreateText("Subtitle", rootPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -94f), new Vector2(-120f, 36f), 17, TextAnchor.MiddleCenter);
            subtitle.text = $"\u9009\u62e9\u6700\u591a 4 \u540d\u82f1\u96c4\u51fa\u6218\uff08\u5df2\u9009 {selectedHeroes.Count}/4\uff09";
            SetTextStyle(subtitle, new Color(0.82f, 0.72f, 0.54f), false);

            for (var i = 0; i < heroPool.Count; i++)
            {
                var hero = heroPool[i];
                var selected = selectedHeroes.Contains(hero);
                var row = i / 3;
                var column = i % 3;
                var card = CreatePanel($"HeroCard_{i}", rootPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(215f + column * 380f, -218f - row * 220f), new Vector2(330f, 176f), selected ? new Color(0.16f, 0.105f, 0.055f, 0.96f) : new Color(0.055f, 0.048f, 0.047f, 0.94f));
                card.GetComponent<Image>().sprite = panelSprite;
                card.GetComponent<Image>().type = Image.Type.Sliced;

                var nameText = CreateText("Name", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -30f), new Vector2(-28f, 32f), 18, TextAnchor.MiddleCenter);
                nameText.text = selected ? $"[\u5df2\u9009] {hero.displayName}" : hero.displayName;
                SetTextStyle(nameText, selected ? new Color(1f, 0.82f, 0.38f) : new Color(0.95f, 0.84f, 0.65f), true);

                var portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                portraitObject.transform.SetParent(card, false);
                var portraitRect = portraitObject.GetComponent<RectTransform>();
                portraitRect.anchorMin = new Vector2(0f, 0f);
                portraitRect.anchorMax = new Vector2(0f, 1f);
                portraitRect.pivot = new Vector2(0.5f, 0.5f);
                portraitRect.anchoredPosition = new Vector2(58f, -8f);
                portraitRect.sizeDelta = new Vector2(78f, 116f);
                var portraitImage = portraitObject.GetComponent<Image>();
                portraitImage.sprite = ResolveHeroPortrait(hero);
                portraitImage.color = portraitImage.sprite != null ? Color.white : new Color(0.18f, 0.14f, 0.12f, 1f);
                portraitImage.preserveAspect = true;

                var stats = CreateText("Stats", card, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft);
                stats.rectTransform.offsetMin = new Vector2(112f, 46f);
                stats.rectTransform.offsetMax = new Vector2(-20f, -58f);
                stats.text = $"\u751f\u547d {hero.maxHealth}\n\u653b\u51fb {hero.attack}\n\u9632\u5fa1 {hero.defense}\n\u901f\u5ea6 {hero.speed}";
                SetTextStyle(stats, new Color(0.84f, 0.78f, 0.66f), false);

                CreateButton(card, selected ? "\u53d6\u6d88" : "\u9009\u62e9", new Vector2(165f, -148f), () => onToggleHero?.Invoke(hero), true, null);
            }

            var lineup = selectedHeroes.Count == 0
                ? "\u5f53\u524d\u961f\u4f0d\uff1a\u672a\u9009\u62e9"
                : "\u5f53\u524d\u961f\u4f0d\uff1a" + string.Join(" / ", selectedHeroes.Select(hero => hero.displayName));
            var lineupText = CreateText("Lineup", rootPanel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 94f), new Vector2(-96f, 40f), 17, TextAnchor.MiddleCenter);
            lineupText.text = lineup;
            SetTextStyle(lineupText, new Color(0.9f, 0.78f, 0.58f), false);

            CreateButton(rootPanel, "\u5f00\u59cb\u6218\u6597", new Vector2(590f, -708f), onStartBattle.Invoke, selectedHeroes.Count > 0, null);
        }

        public void HideOverlay()
        {
            if (overlayPanel != null)
            {
                overlayPanel.gameObject.SetActive(false);
            }
        }

        private void EnsureCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void EnsureOverlay()
        {
            if (overlayPanel != null && overlayText != null)
            {
                return;
            }

            var root = GetComponent<RectTransform>();
            if (overlayPanel == null)
            {
                overlayPanel = CreatePanel("SequenceOverlay", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.62f));
                overlayPanel.offsetMin = Vector2.zero;
                overlayPanel.offsetMax = Vector2.zero;
            }
            else
            {
                ClearChildren(overlayPanel);
                overlayPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);
            }

            var messagePanel = CreatePanel("MessagePanel", overlayPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(620f, 220f), new Color(0.035f, 0.026f, 0.024f, 0.96f));
            messagePanel.GetComponent<Image>().sprite = descriptionSprite;
            messagePanel.GetComponent<Image>().type = Image.Type.Sliced;

            overlayText = CreateText("OverlayText", messagePanel, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, -30f), new Vector2(-56f, 92f), 28, TextAnchor.MiddleCenter);
            overlayText.rectTransform.offsetMin = new Vector2(28f, -8f);
            overlayText.rectTransform.offsetMax = new Vector2(-28f, -24f);
            SetTextStyle(overlayText, new Color(0.96f, 0.82f, 0.48f), true);

            overlayPanel.gameObject.SetActive(false);
        }

        private static Sprite ResolveHeroPortrait(CombatantDefinition hero)
        {
            if (hero.portrait != null)
            {
                return hero.portrait;
            }

            if (hero.battleSprite != null)
            {
                return hero.battleSprite;
            }

            return hero.idleAnimationFrames != null && hero.idleAnimationFrames.Length > 0
                ? hero.idleAnimationFrames[0]
                : null;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private static RectTransform CreatePanel(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            var panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            panel.GetComponent<Image>().color = color;
            return rect;
        }

        private static Text CreateText(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var text = textObject.AddComponent<Text>();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.font = ChineseFontProvider.LegacyFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            return text;
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

            logText = CreateText("LogText", content.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft);
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

        private void CreateButton(RectTransform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action, bool interactable, SkillData skill)
        {
            var buttonObject = new GameObject($"RuntimeButton_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(162f, 42f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.12f, 0.1f, 1f);
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;

            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                if (skill != null)
                {
                    ShowSkillDescription(skill);
                }

                action();
            });
            button.interactable = interactable;

            var colors = button.colors;
            colors.normalColor = new Color(0.16f, 0.12f, 0.1f, 1f);
            colors.highlightedColor = new Color(0.38f, 0.2f, 0.12f, 1f);
            colors.pressedColor = new Color(0.55f, 0.16f, 0.12f, 1f);
            colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.75f);
            button.colors = colors;

            var text = CreateText("Label", buttonObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 15, TextAnchor.MiddleCenter);
            text.text = label;
            text.raycastTarget = false;
            SetTextStyle(text, interactable ? new Color(0.96f, 0.88f, 0.68f) : new Color(0.5f, 0.5f, 0.5f), false);

            var trigger = buttonObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                if (skill != null)
                {
                    ShowSkillDescription(skill);
                }
            });
            trigger.triggers.Add(enter);

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ =>
            {
                if (skill != null)
                {
                    ShowSkillDescription(skill);
                }
            });
            trigger.triggers.Add(down);
        }

        private void EnsureSkinSprites()
        {
            panelSprite = panelSprite != null
                ? panelSprite
                : CombatUiAssetLoader.LoadSprite("Assets/Art/UI/Combat/ui_skill_description_panel.png", new Vector4(80f, 80f, 80f, 80f))
                  ?? CreateUiSprite("GothicPanel", new Color(0.028f, 0.024f, 0.023f, 1f), new Color(0.52f, 0.31f, 0.16f, 1f), new Color(0.12f, 0.095f, 0.075f, 1f));

            buttonSprite = buttonSprite != null
                ? buttonSprite
                : CombatUiAssetLoader.LoadSprite("Assets/Art/UI/Combat/ui_skill_button_frame.png", new Vector4(60f, 60f, 60f, 60f))
                  ?? CreateUiSprite("GothicButton", new Color(0.12f, 0.07f, 0.055f, 1f), new Color(0.8f, 0.48f, 0.18f, 1f), new Color(0.24f, 0.11f, 0.075f, 1f));

            descriptionSprite = descriptionSprite != null
                ? descriptionSprite
                : CombatUiAssetLoader.LoadSprite("Assets/Art/UI/Combat/ui_skill_description_panel.png", new Vector4(80f, 80f, 80f, 80f))
                  ?? CreateUiSprite("GothicDescription", new Color(0.038f, 0.029f, 0.027f, 1f), new Color(0.62f, 0.39f, 0.2f, 1f), new Color(0.1f, 0.075f, 0.055f, 1f));
        }

        private static Sprite CreateUiSprite(string name, Color fill, Color border, Color inner)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.filterMode = FilterMode.Bilinear;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                    var gearLine = (x + y) % 17 == 0 || Mathf.Abs(x - y) % 23 == 0;
                    var color = fill;
                    if (edge < 4)
                    {
                        color = border;
                    }
                    else if (edge < 8)
                    {
                        color = inner;
                    }
                    else if (gearLine)
                    {
                        color = Color.Lerp(fill, border, 0.18f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(10f, 10f, 10f, 10f));
        }

        private static string BuildEffectSummary(SkillData skill)
        {
            if (skill.isSwapSkill)
            {
                return "\u6548\u679c\uff1a\u548c\u4e00\u540d\u961f\u53cb\u4ea4\u6362\u7ad9\u4f4d\u3002";
            }

            if (skill.skillType == SkillDataType.治疗)
            {
                return $"\u6548\u679c\uff1a\u6cbb\u7597\u76ee\u6807\uff0c\u57fa\u7840\u5f3a\u5ea6 {skill.baseValue}\u3002";
            }

            if (skill.skillType == SkillDataType.控制)
            {
                return $"\u6548\u679c\uff1a\u63a7\u5236\u76ee\u6807\uff0c\u57fa\u7840\u6570\u503c {skill.baseValue}\uff0c\u500d\u7387 {skill.powerMultiplier:0.##}\u3002";
            }

            return $"\u6548\u679c\uff1a\u9020\u6210\u4f24\u5bb3\uff0c\u57fa\u7840\u6570\u503c {skill.baseValue}\uff0c\u500d\u7387 {skill.powerMultiplier:0.##}\u3002";
        }

        private static string BuildTargetRequirement(SkillData skill)
        {
            switch (skill.targetType)
            {
                case SkillDataTargetType.自己:
                    return "\u81ea\u8eab";
                case SkillDataTargetType.全体敌:
                    return "\u654c\u65b9\u5168\u4f53";
                case SkillDataTargetType.单友:
                    return FormatPositionRequirement(skill.targetAllowedPositions, "\u53ef\u9009\u53cb\u65b9");
                case SkillDataTargetType.前排两敌:
                    return "\u654c\u65b9\u524d\u6392\u4e24\u4eba";
                case SkillDataTargetType.单敌:
                default:
                    return FormatPositionRequirement(skill.targetAllowedPositions, "\u53ef\u6253\u51fb\u654c\u65b9");
            }
        }

        private static string FormatPositionRequirement(int[] positions, string prefix)
        {
            if (positions == null || positions.Length == 0 || positions.Length >= 4)
            {
                return $"{prefix}1-4\u53f7\u4f4d";
            }

            var sorted = positions.Distinct().OrderBy(item => item).ToArray();
            if (sorted.Length > 1 && sorted.Zip(sorted.Skip(1), (a, b) => b - a).All(delta => delta == 1))
            {
                return $"{prefix}{sorted.First()}-{sorted.Last()}\u53f7\u4f4d";
            }

            return $"{prefix}{string.Join("\u3001", sorted)}\u53f7\u4f4d";
        }

        private static void SetTextStyle(Text text, Color color, bool bold)
        {
            text.color = color;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ClearRuntimeButtons(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            foreach (Transform child in parent.Cast<Transform>().Where(child => child.name.StartsWith("RuntimeButton_", StringComparison.Ordinal)).ToArray())
            {
                Destroy(child.gameObject);
            }
        }

        private static void ClearChildren(Transform parent)
        {
            foreach (Transform child in parent.Cast<Transform>().ToArray())
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}


