using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    [Obsolete("Use BattleHudController for new references. BattleUI remains as a compatibility wrapper for existing assets and scenes.")]
    public class BattleUI : MonoBehaviour
    {
        [Header("Runtime Slots")]
        [SerializeField] private Image activePortraitImage;
        [SerializeField] private Text activeNameText;
        [SerializeField] private Text activeMetaText;
        [SerializeField] private Text activeStatsText;
        [SerializeField] private RectTransform skillPanel;
        [SerializeField] private RectTransform skillListContent;
        [SerializeField] private Button skillButtonTemplate;
        [SerializeField] private RectTransform targetPanel;
        [SerializeField] private Text targetNameText;
        [SerializeField] private Text targetMetaText;
        [SerializeField] private Text roundText;
        [SerializeField] private Text turnText;
        [SerializeField] private Text logText;
        [SerializeField] private Text infoText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text skillDescriptionText;
        [SerializeField] private ScrollRect logScrollRect;
        [SerializeField] private RectTransform skillDescriptionPanel;
        [SerializeField] private RectTransform overlayPanel;
        [SerializeField] private Text overlayText;

        private readonly List<string> logLines = new List<string>();
        private Sprite panelSprite;
        private Sprite buttonSprite;
        private Sprite descriptionSprite;
        private UIManager runtimeUiManager;
        private BattleHudUI battleHudUI;
        private SkillDescriptionUI skillDescriptionUI;

        public void Build()
        {
            EnsureEventSystem();
            EnsureCanvas();
            EnsureRuntimePrefabUi();
            SetBattleHudVisible(false);

            if (skillPanel == null || targetPanel == null || roundText == null || turnText == null || logText == null || infoText == null || logScrollRect == null || skillDescriptionPanel == null || skillDescriptionText == null)
            {
                Debug.LogError("BattleUI failed to bind BattleHudUI / SkillDescriptionUI runtime prefabs. HUD layout generation fallback is disabled.", this);
            }
        }

        [Obsolete("Legacy authoring-only layout builder. Runtime battle HUD is provided by BattleHudUI.")]
        public void BuildDefaultLayout()
        {
            RebuildLegacyLayoutFromCode();
        }

        public void RebuildLegacyLayoutFromCode()
        {
            EnsureCanvas();
            EnsureSkinSprites();
            ClearChildren(transform);

            var root = GetComponent<RectTransform>();
            Stretch(root);

            turnText = CreateText("TurnText", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(760f, 42f), 22, TextAnchor.MiddleCenter);
            SetTextStyle(turnText, new Color(0.96f, 0.86f, 0.62f), true);

            goldText = CreateText("GoldText", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-145f, -34f), new Vector2(260f, 38f), 18, TextAnchor.MiddleRight);
            SetTextStyle(goldText, new Color(1f, 0.82f, 0.36f), true);
            SetGold(0);

            roundText = CreateText("RoundText", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(360f, 34f), 18, TextAnchor.MiddleCenter);
            SetTextStyle(roundText, new Color(0.82f, 0.72f, 0.54f), true);

            var bottomBar = CreatePanel("BottomBar", root, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 210f), new Color(0.025f, 0.024f, 0.026f, 0.96f));
            bottomBar.GetComponent<Image>().sprite = panelSprite;
            bottomBar.GetComponent<Image>().type = Image.Type.Sliced;
            bottomBar.pivot = new Vector2(0.5f, 0f);
            bottomBar.offsetMin = new Vector2(0f, 18f);
            bottomBar.offsetMax = new Vector2(0f, 228f);

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
            skillDescriptionPanel.gameObject.SetActive(false);
        }

        private void EnsureRuntimePrefabUi()
        {
            var battleRoot = transform as RectTransform;
            if (battleRoot == null)
            {
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            var canvasRoot = canvas != null ? canvas.transform as RectTransform : battleRoot;
            runtimeUiManager = UIManager.Ensure(canvasRoot);
            battleHudUI = runtimeUiManager.GetBattleHud();
            skillDescriptionUI = runtimeUiManager.GetSkillDescription();

            if (battleHudUI != null)
            {
                battleHudUI.Build();
                skillPanel = battleHudUI.SkillPanel;
                skillListContent = battleHudUI.SkillListContent;
                skillButtonTemplate = battleHudUI.SkillButtonTemplate;
                targetPanel = battleHudUI.TargetPanel;
                activePortraitImage = battleHudUI.ActivePortraitImage;
                activeNameText = battleHudUI.ActiveNameText;
                activeMetaText = battleHudUI.ActiveMetaText;
                activeStatsText = battleHudUI.ActiveStatsText;
                targetNameText = battleHudUI.TargetNameText;
                targetMetaText = battleHudUI.TargetMetaText;
                roundText = battleHudUI.RoundText;
                turnText = battleHudUI.TurnText;
                logText = battleHudUI.LogText;
                infoText = battleHudUI.InfoText;
                goldText = battleHudUI.GoldText;
                logScrollRect = battleHudUI.LogScrollRect;
                battleHudUI.gameObject.SetActive(false);
            }

            if (skillDescriptionUI != null)
            {
                skillDescriptionUI.Build();
                skillDescriptionText = skillDescriptionUI.SkillDescriptionText;
                skillDescriptionPanel = skillDescriptionUI.skillDescriptionBg != null ? skillDescriptionUI.skillDescriptionBg.rectTransform : null;
            }
        }

        private void SetBattleHudVisible(bool visible)
        {
            if (battleHudUI != null)
            {
                battleHudUI.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                HideSkillDescription();
            }
        }

        public void HideBattleHud()
        {
            SetBattleHudVisible(false);
        }

        public void RenderPlayerTurn(BattleUnit actor, IReadOnlyList<SkillUseState> skillStates, Action<SkillData> onSkillSelected)
        {
            SetBattleHudVisible(true);
            SetTurn($"{actor.DisplayName} \u884c\u52a8");
            ClearRuntimeButtons(targetPanel);
            AddLog($"\u8bf7\u9009\u62e9 {actor.DisplayName} \u7684\u6280\u80fd\u3002");
            RenderUnitPanel(actor, actor, skillStates, onSkillSelected);
        }

        public void RenderTargets(SkillData skill, IReadOnlyList<BattleUnit> targets, Action<BattleUnit> onTargetSelected)
        {
            SetBattleHudVisible(true);
            ClearRuntimeButtons(targetPanel);
            AddLog($"\u8bf7\u70b9\u51fb\u573a\u666f\u4e2d\u7684\u76ee\u6807\u6765\u4f7f\u7528 {skill.skillName}\u3002");
            AppendInfoHint($"\n\n\u5df2\u9009\u6280\u80fd\uff1a{skill.skillName}\n\u8bf7\u70b9\u51fb\u573a\u666f\u4e2d\u7684\u53ef\u9009\u76ee\u6807\u3002");
        }

        public void RenderUnitPanel(BattleUnit selectedUnit, BattleUnit activeActor, IReadOnlyList<SkillUseState> skillStates, Action<SkillData> onSkillSelected)
        {
            SetBattleHudVisible(true);
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
                if (skillListContent != null && skillButtonTemplate != null)
                {
                    CreateSkillTemplateButton(skillListContent, skillButtonTemplate, state, () => onSkillSelected(skill), canUseSkills && state.CanUse);
                }
                else
                {
                    var column = i % 2;
                    var row = i / 2;
                    CreateButton(skillPanel, state.ButtonLabel, new Vector2(92f + column * 178f, -62f - row * 54f), () => onSkillSelected(skill), canUseSkills && state.CanUse, skill);
                }
            }
        }

        private void ShowSkillDescription(SkillData skill, RectTransform source = null)
        {
            if (skillDescriptionText == null || skill == null)
            {
                return;
            }

            var casterRequirement = FormatPositionRequirement(skill.casterAllowedPositions, "\u9700\u8981\u7ad9\u5728");
            var description =
                $"{skill.skillName}\n" +
                $"{BuildEffectSummary(skill)}\n" +
                $"资源消耗：{skill.manaCost}    冷却：{skill.cooldown}\n" +
                $"\u65bd\u6cd5\u7ad9\u4f4d\uff1a{casterRequirement}\n" +
                $"\u76ee\u6807\u7ad9\u4f4d\uff1a{BuildTargetRequirement(skill)}\n" +
                $"{BuildTacticalHint(skill)}" +
                $"{skill.description}";

            if (skillDescriptionUI != null && source != null)
            {
                var tooltipRoot = runtimeUiManager != null ? runtimeUiManager.GetHudRoot() : transform as RectTransform;
                skillDescriptionUI.ShowNear(source, tooltipRoot, description);
                return;
            }

            skillDescriptionText.text = description;
            if (skillDescriptionPanel != null)
            {
                skillDescriptionPanel.gameObject.SetActive(true);
            }
        }

        private void HideSkillDescription()
        {
            if (skillDescriptionUI != null)
            {
                skillDescriptionUI.Hide();
                return;
            }

            if (skillDescriptionPanel != null)
            {
                skillDescriptionPanel.gameObject.SetActive(false);
            }
        }

        private static string BuildTacticalHint(SkillData skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            switch (skill.skillId)
            {
                case "hero_01_gear_sting":
                    return "战术提示：后排点杀工具，命中敌方后排时还能回资源。\n";
                case "hero_01_scrap_volley":
                    return "战术提示：压低全体血线，命中三人以上可回资源，但会有少量反震。\n";
                case "hero_01_lock_pin":
                    return "战术提示：先给目标挂压制，再用其他技能扩大收益。\n";
                case "hero_01_overload_spark":
                    return "战术提示：优先打已被压制的目标，伤害会更高。\n";
                case "hero_03_shadow_cut":
                    return "战术提示：优先打带负面状态目标，作为稳定起手和补刀都合适。\n";
                case "hero_03_vein_rend":
                    return "战术提示：先挂流血，再为后续处决和追猎铺路。\n";
                case "hero_03_crescent_lunge":
                    return "战术提示：专打敌方后排，命中后排时还能回资源。\n";
                case "hero_03_wild_hunt":
                    return "战术提示：锁定残血目标，完成击杀时会大量回收资源。\n";
                case "hero_01_iron_cut":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u524d\u6392\u5bf9\u649e\u65f6\u4f1a\u989d\u5916\u56de\u8d44\u6e90\u3002\n";
                case "hero_01_ember_rend":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u5bf9\u5df2\u707c\u70e7\u76ee\u6807\u4f24\u5bb3\u66f4\u9ad8\u3002\n";
                case "hero_02_iron_cut":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u53d7\u4f24\u65f6\u53ef\u7528\u6765\u8fd1\u8eab\u81ea\u7a33\u3002\n";
                case "hero_02_field_stitch":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u62a2\u6551\u534a\u8840\u4ee5\u4e0b\u53cb\u519b\u6536\u76ca\u66f4\u9ad8\u3002\n";
                case "hero_02_steam_purge":
                    return "战术提示：净化灼烧和眩晕时收益最高，适合稳住濒危友军。\n";
                case "hero_02_stun_chain":
                    return "战术提示：优先压制敌方后排，命中后排时会额外回资源。\n";
                case "hero_02_bone_dart":
                    return "战术提示：作为低消耗补刀和过渡出手，方便把资源留给治疗与控制。\n";
                case "hero_03_scrap_volley":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u547d\u4e2d\u591a\u4eba\u65f6\u53ef\u56de\u8d44\u6e90\uff0c\u66f4\u504f\u7a33\u5b9a\u538b\u5236\u3002\n";
                case "hero_04_iron_cut":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u66f4\u9002\u5408\u5148\u538b\u5065\u5eb7\u76ee\u6807\uff0c\u4e3a\u540e\u7eed\u5904\u51b3\u94fa\u8def\u3002\n";
                case "hero_04_guard_break":
                case "hero_07_guard_break":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u6253\u524d\u6392\u76ee\u6807\u65f6\u6536\u76ca\u66f4\u9ad8\u3002\n";
                case "hero_05_gear_sting":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u4ece\u540e\u6392\u70b9\u6740\u654c\u65b9\u540e\u6392\u65f6\u4f1a\u56de\u8d44\u6e90\u3002\n";
                case "hero_05_iron_cut":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u66f4\u9002\u5408\u6536\u5272\u534a\u8840\u4ee5\u4e0b\u76ee\u6807\u3002\n";
                case "hero_06_field_stitch":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u6cbb\u7597\u540e\u4f1a\u7f29\u77ed\u76ee\u6807\u4e00\u4e2a\u6280\u80fd\u51b7\u5374\u3002\n";
                case "hero_06_steam_purge":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u53ef\u51c0\u5316\u8d1f\u9762\u72b6\u6001\uff0c\u5904\u7406\u707c\u70e7\u548c\u7729\u6655\u65f6\u6cbb\u7597\u66f4\u5f3a\u3002\n";
                case "hero_06_stun_chain":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u538b\u5236\u654c\u65b9\u540e\u6392\u65f6\u4f1a\u989d\u5916\u56de\u8d44\u6e90\u3002\n";
                case "hero_07_iron_cut":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u524d\u6392\u4f7f\u7528\u53ef\u5c0f\u5e45\u56de\u590d\u81ea\u8eab\u751f\u547d\u3002\n";
                case "hero_08_scrap_volley":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u5bf9\u540e\u6392\u66f4\u5f3a\uff0c\u4f46\u547d\u4e2d\u591a\u4eba\u4f1a\u627f\u53d7\u66f4\u9ad8\u53cd\u9707\u3002\n";
                case "hero_08_ember_rend":
                    return "\u6218\u672f\u63d0\u793a\uff1a\u538b\u4e2d\u654c\u65b9\u540e\u6392\u4f1a\u56de\u8d44\u6e90\uff0c\u5bf9\u5df2\u707c\u70e7\u76ee\u6807\u589e\u4f24\u3002\n";
                default:
                    return string.Empty;
            }
        }

        private void RenderInfo(BattleUnit selectedUnit, BattleUnit activeActor)
        {
            if (infoText == null)
            {
                return;
            }

            RenderActiveUnitInfo(activeActor);

            if (selectedUnit == null)
            {
                if (targetNameText != null)
                {
                    targetNameText.text = "未选中目标";
                }

                if (targetMetaText != null)
                {
                    targetMetaText.text = "点击战场中的单位查看详细信息";
                }

                infoText.text = "\u70b9\u51fb\u89d2\u8272\u67e5\u770b\u5c5e\u6027";
                return;
            }

            var side = selectedUnit.IsHero ? "\u6211\u65b9" : "\u654c\u65b9";
            var active = selectedUnit == activeActor ? "\n\u5f53\u524d\u884c\u52a8\u89d2\u8272" : string.Empty;
            var status = selectedUnit.Statuses.Count == 0
                ? "\u65e0"
                : string.Join("\uff0c", selectedUnit.Statuses.Select(item => $"{item.DisplayName}({item.TurnsRemaining})"));

            if (targetNameText != null)
            {
                targetNameText.text = selectedUnit.DisplayName;
            }

            if (targetMetaText != null)
            {
                targetMetaText.text = $"{side}  {selectedUnit.Definition.ArchetypeDisplayName}  {selectedUnit.Definition.SpecializationDisplayName}";
            }

            var passiveLine = selectedUnit.Definition.growthPassive != HeroPassive.None
                ? $"{selectedUnit.Definition.PassiveDisplayName} / {selectedUnit.Definition.GrowthPassiveDisplayName}"
                : selectedUnit.Definition.PassiveDisplayName;

            infoText.text =
                $"{side}{active}\n\n" +
                $"\u804c\u80fd\uff1a{selectedUnit.Definition.ArchetypeDisplayName}\n" +
                $"\u5b9a\u4f4d\uff1a{selectedUnit.Definition.ArchetypeSummary}\n" +
                $"\u4e13\u7cbe\uff1a{selectedUnit.Definition.SpecializationDisplayName}\n" +
                $"\u4e13\u7cbe\u7279\u5f81\uff1a{selectedUnit.Definition.SpecializationSummary}\n" +
                $"\u88ab\u52a8\uff1a{passiveLine}\n" +
                $"\u504f\u597d\u7ad9\u4f4d\uff1a{selectedUnit.Definition.PreferredRowDisplayName}\n" +
                $"\u7b49\u7ea7\uff1a{selectedUnit.Level}\n" +
                $"\u751f\u547d\uff1a{selectedUnit.Health}/{selectedUnit.MaxHealth}\n" +
                $"\u8d44\u6e90\uff1a{selectedUnit.Resource}/{selectedUnit.MaxResource}\n" +
                $"\u653b\u51fb\uff1a{selectedUnit.Attack}\n" +
                $"\u9632\u5fa1\uff1a{selectedUnit.Defense}\n" +
                $"\u7ad9\u4f4d\uff1a{selectedUnit.CurrentPosition}\n" +
                $"\u901f\u5ea6\uff1a{selectedUnit.Speed}\n" +
                $"\u72b6\u6001\uff1a{status}";
        }

        private void RenderActiveUnitInfo(BattleUnit activeActor)
        {
            if (activeNameText == null || activeMetaText == null || activeStatsText == null)
            {
                return;
            }

            if (activeActor == null)
            {
                activeNameText.text = "当前行动者";
                activeMetaText.text = "等待进入战斗";
                activeStatsText.text = "尚未进入战斗回合。";
                if (activePortraitImage != null)
                {
                    activePortraitImage.sprite = null;
                    activePortraitImage.color = new Color(0.16f, 0.16f, 0.17f, 1f);
                }

                return;
            }

            activeNameText.text = activeActor.DisplayName;
            activeMetaText.text = $"{activeActor.Definition.ArchetypeDisplayName}  /  {activeActor.Definition.SpecializationDisplayName}  /  {activeActor.Definition.PreferredRowDisplayName}";
            activeStatsText.text =
                $"生命 {activeActor.Health}/{activeActor.MaxHealth}    资源 {activeActor.Resource}/{activeActor.MaxResource}\n" +
                $"攻击 {activeActor.Attack}    防御 {activeActor.Defense}    速度 {activeActor.Speed}    站位 {activeActor.CurrentPosition}";

            if (activePortraitImage != null)
            {
                activePortraitImage.sprite = activeActor.Definition.portrait;
                activePortraitImage.color = activeActor.Definition.portrait != null ? Color.white : new Color(0.16f, 0.16f, 0.17f, 1f);
                activePortraitImage.preserveAspect = true;
            }
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
            SetBattleHudVisible(true);
            if (roundText != null)
            {
                roundText.text = $"\u7b2c {round} \u56de\u5408";
            }
        }

        public void SetTurn(string text)
        {
            SetBattleHudVisible(true);
            if (turnText != null)
            {
                turnText.text = text;
            }
        }

        public void SetGold(int amount)
        {
            SetBattleHudVisible(true);
            if (goldText != null)
            {
                goldText.text = $"\u91d1\u5e01\uff1a{Mathf.Max(0, amount)}";
            }
        }

        public void AddLog(string message)
        {
            SetBattleHudVisible(true);
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
            overlayPanel.SetAsLastSibling();
            overlayText.text = message;
            var buttonParent = overlayPanel.Find("MessagePanel") as RectTransform ?? overlayPanel;
            ClearRuntimeButtons(buttonParent);
            CreateButton(buttonParent, buttonLabel, new Vector2(310f, -166f), () =>
            {
                HideOverlay();
                onContinue?.Invoke();
            }, true, null);
        }

        public void ShowChoicePrompt(string message, string leftLabel, Action onLeft, string rightLabel, Action onRight)
        {
            EnsureRuntimePrefabUi();
            if (runtimeUiManager != null)
            {
                runtimeUiManager.ShowChoicePopup(message, leftLabel, onLeft, rightLabel, onRight);
                return;
            }

            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            overlayPanel.SetAsLastSibling();
            overlayText.text = message;
            var buttonParent = overlayPanel.Find("MessagePanel") as RectTransform ?? overlayPanel;
            ClearRuntimeButtons(buttonParent);
            CreateButton(buttonParent, leftLabel, new Vector2(190f, -166f), () =>
            {
                HideOverlay();
                onLeft?.Invoke();
            }, true, null);
            CreateButton(buttonParent, rightLabel, new Vector2(430f, -166f), () =>
            {
                HideOverlay();
                onRight?.Invoke();
            }, true, null);
        }

        public void ShowEndScreen(string message)
        {
            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            overlayPanel.SetAsLastSibling();
            overlayText.text = message;
            var buttonParent = overlayPanel.Find("MessagePanel") as RectTransform ?? overlayPanel;
            ClearRuntimeButtons(buttonParent);
        }

        public void ShowTitleScreen(bool showContinue, Action onStartGame, Action onContinueGame, Action onOpenSettings, Action onQuit, Action onBack)
        {
            EnsureRuntimePrefabUi();
            SetBattleHudVisible(false);
            runtimeUiManager?.ShowStartMenu(showContinue, onStartGame, onContinueGame, onOpenSettings, onQuit, onBack);
        }

        public void ShowSettingsScreen(Action onBack, Action onSaveGame)
        {
            SetBattleHudVisible(false);
            if (runtimeUiManager != null)
            {
                runtimeUiManager.ShowSettings(onBack, onSaveGame);
                return;
            }

            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            overlayPanel.SetAsLastSibling();
            ClearChildren(overlayPanel);

            var panel = CreatePanel("SettingsPanel", overlayPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 360f), new Color(0.032f, 0.026f, 0.024f, 0.97f));
            panel.GetComponent<Image>().sprite = descriptionSprite;
            panel.GetComponent<Image>().type = Image.Type.Sliced;

            var title = CreateText("Title", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), new Vector2(-70f, 58f), 28, TextAnchor.MiddleCenter);
            title.text = "\u8bbe\u7f6e";
            SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var body = CreateText("Body", panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18, TextAnchor.MiddleCenter);
            body.rectTransform.offsetMin = new Vector2(70f, 112f);
            body.rectTransform.offsetMax = new Vector2(-70f, -128f);
            body.text = "\u97f3\u6548\uff1a\u5f00\u542f";
            SetTextStyle(body, new Color(0.86f, 0.78f, 0.64f), false);

            CreateButton(panel, "\u8fd4\u56de", new Vector2(310f, -304f), () => onBack?.Invoke(), true, null);
        }

        public void ShowRewardScreen(int goldGained, int totalGold, IReadOnlyList<BattleRewardResult> results, Action onContinue)
        {
            SetBattleHudVisible(false);
            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            overlayPanel.SetAsLastSibling();
            ClearChildren(overlayPanel);

            var panel = CreatePanel("RewardPanel", overlayPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(720f, 470f), new Color(0.035f, 0.026f, 0.024f, 0.97f));
            panel.GetComponent<Image>().sprite = descriptionSprite;
            panel.GetComponent<Image>().type = Image.Type.Sliced;

            var title = CreateText("Title", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-56f, 58f), 30, TextAnchor.MiddleCenter);
            title.text = "\u6218\u540e\u5956\u52b1";
            SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var goldLine = CreateText("Gold", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -102f), new Vector2(-80f, 36f), 20, TextAnchor.MiddleCenter);
            goldLine.text = $"\u83b7\u5f97\u91d1\u5e01\uff1a{goldGained}    \u5f53\u524d\u91d1\u5e01\uff1a{totalGold}";
            SetTextStyle(goldLine, new Color(1f, 0.78f, 0.34f), true);

            var rewardLines = results != null && results.Count > 0
                ? results.Select(result =>
                {
                    var levelText = result.LevelsGained > 0 ? $"  \u5347\u7ea7\uff01\u5f53\u524d {result.Hero.Level} \u7ea7" : $"  {result.Hero.Experience}/{result.Hero.ExperienceToNextLevel}";
                    return $"{result.Hero.displayName}  +{result.ExperienceGained} EXP{levelText}";
                })
                : new[] { "\u6ca1\u6709\u5b58\u6d3b\u82f1\u96c4\u83b7\u5f97\u7ecf\u9a8c\u3002" };

            var rewards = CreateText("Rewards", panel, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 18, TextAnchor.UpperLeft);
            rewards.rectTransform.offsetMin = new Vector2(80f, 92f);
            rewards.rectTransform.offsetMax = new Vector2(-80f, -132f);
            rewards.text = string.Join("\n", rewardLines);
            SetTextStyle(rewards, new Color(0.88f, 0.8f, 0.66f), false);

            CreateButton(panel, "\u7ee7\u7eed", new Vector2(360f, -414f), () =>
            {
                HideOverlay();
                onContinue?.Invoke();
            }, true, null);
        }

        public void ShowTeamSelection(
            IReadOnlyList<CombatantDefinition> heroPool,
            IReadOnlyList<CombatantDefinition> selectedHeroes,
            Action<CombatantDefinition> onToggleHero,
            Action onStartBattle,
            Action onOpenShop,
            Action onOpenInventory,
            Action onOpenTavern)
        {
            SetBattleHudVisible(false);
            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            overlayPanel.SetAsLastSibling();
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

            CreateButton(rootPanel, "\u5546\u5e97", new Vector2(1000f, -70f), () => onOpenShop?.Invoke(), true, null);
            CreateButton(rootPanel, "\u80cc\u5305", new Vector2(1000f, -122f), () => onOpenInventory?.Invoke(), true, null);
            CreateButton(rootPanel, "\u9152\u9986", new Vector2(1000f, -174f), () => onOpenTavern?.Invoke(), true, null);

            for (var i = 0; i < heroPool.Count; i++)
            {
                var hero = heroPool[i];
                var selected = selectedHeroes.Contains(hero);
                var row = i / 4;
                var column = i % 4;
                var card = CreatePanel($"HeroCard_{i}", rootPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(155f + column * 288f, -220f - row * 224f), new Vector2(250f, 176f), selected ? new Color(0.16f, 0.105f, 0.055f, 0.96f) : new Color(0.055f, 0.048f, 0.047f, 0.94f));
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
                portraitRect.anchoredPosition = new Vector2(48f, -8f);
                portraitRect.sizeDelta = new Vector2(68f, 108f);
                var portraitImage = portraitObject.GetComponent<Image>();
                portraitImage.sprite = ResolveHeroPortrait(hero);
                portraitImage.color = portraitImage.sprite != null ? Color.white : new Color(0.18f, 0.14f, 0.12f, 1f);
                portraitImage.preserveAspect = true;

                var stats = CreateText("Stats", card, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft);
                stats.rectTransform.offsetMin = new Vector2(92f, 36f);
                stats.rectTransform.offsetMax = new Vector2(-20f, -48f);
                stats.text = $"\u804c\u80fd {hero.ArchetypeDisplayName}  \u4e13\u7cbe {hero.SpecializationDisplayName}\n\u504f\u597d {hero.PreferredRowDisplayName}\n\u7b49\u7ea7 {hero.Level}  EXP {hero.Experience}/{hero.ExperienceToNextLevel}\n\u751f\u547d {hero.CurrentHealth}/{hero.MaxHealthWithGrowth}\n\u653b\u51fb {hero.AttackWithGrowth}\n\u9632\u5fa1 {hero.DefenseWithGrowth}\n\u901f\u5ea6 {hero.SpeedWithArchetype}";
                SetTextStyle(stats, new Color(0.84f, 0.78f, 0.66f), false);

                CreateButton(card, selected ? "\u53d6\u6d88" : "\u9009\u62e9", new Vector2(125f, -148f), () => onToggleHero?.Invoke(hero), true, null);
            }

            var lineup = selectedHeroes.Count == 0
                ? "\u5f53\u524d\u961f\u4f0d\uff1a\u672a\u9009\u62e9"
                : "\u5f53\u524d\u961f\u4f0d\uff1a" + string.Join(" / ", selectedHeroes.Select(hero => hero.displayName));
            var lineupText = CreateText("Lineup", rootPanel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 94f), new Vector2(-96f, 40f), 17, TextAnchor.MiddleCenter);
            lineupText.text = lineup;
            SetTextStyle(lineupText, new Color(0.9f, 0.78f, 0.58f), false);

            CreateButton(rootPanel, "\u5f00\u59cb\u6218\u6597", new Vector2(590f, -708f), onStartBattle.Invoke, selectedHeroes.Count > 0, null);
        }

        public void ShowLobby(int currentGold, Action onOpenTavern, Action onOpenAdventure, Action onOpenRecoveryWard, Action onOpenHeroCodex, Action onOpenSettings, Action onBackToStartMenu)
        {
            EnsureRuntimePrefabUi();
            SetBattleHudVisible(false);
            runtimeUiManager?.ShowLobby(currentGold, onOpenTavern, onOpenAdventure, onOpenRecoveryWard, onOpenHeroCodex, onOpenSettings, onBackToStartMenu);
        }

        public void ShowSaveSlots(string title, IReadOnlyList<SaveSlotSummary> slots, bool allowEmptySelection, Action<int> onSelect, Action<int> onDelete, Action onBack)
        {
            EnsureRuntimePrefabUi();
            SetBattleHudVisible(false);
            runtimeUiManager?.ShowSaveSlots(title, slots, allowEmptySelection, onSelect, onDelete, onBack);
        }

        public void ShowLevelUpSelection(LevelUpPresentation presentation, Action<LevelUpOptionData> onSelect)
        {
            EnsureRuntimePrefabUi();
            SetBattleHudVisible(false);
            if (runtimeUiManager != null)
            {
                runtimeUiManager.ShowLevelUp(presentation, onSelect);
                return;
            }

            if (presentation.Options != null && presentation.Options.Count >= 2)
            {
                var left = presentation.Options[0];
                var right = presentation.Options[1];
                ShowChoicePrompt(
                    $"{presentation.Title}\n\n{left.Title}\n{left.Summary}\n\n{right.Title}\n{right.Summary}",
                    left.Title,
                    () => onSelect?.Invoke(left),
                    right.Title,
                    () => onSelect?.Invoke(right));
                return;
            }

            ShowContinuePrompt(presentation.Title, "\u786e\u5b9a", () =>
            {
                if (presentation.Options != null && presentation.Options.Count > 0)
                {
                    onSelect?.Invoke(presentation.Options[0]);
                }
            });
        }

        public void ShowAdventureMap(IReadOnlyList<AdventureMapOption> maps, Action<AdventureMapOption> onSelect, Action onBack)
        {
            EnsureRuntimePrefabUi();
            SetBattleHudVisible(false);
            runtimeUiManager?.ShowAdventureMap(maps, onSelect, onBack);
        }

        public void ShowHeroCodex(IReadOnlyList<CombatantDefinition> heroPool, Action onBack)
        {
            EnsureRuntimePrefabUi();
            SetBattleHudVisible(false);
            runtimeUiManager?.ShowHeroCodex(heroPool, onBack);
        }

        public void ShowTeamSelection(IReadOnlyList<CombatantDefinition> heroPool, IReadOnlyList<CombatantDefinition> selectedHeroes, Action<CombatantDefinition> onToggleHero, Action onStartBattle, Action onBack)
        {
            EnsureRuntimePrefabUi();
            SetBattleHudVisible(false);
            if (runtimeUiManager != null)
            {
                runtimeUiManager.ShowTeamSelection(heroPool, selectedHeroes, onToggleHero, onStartBattle, onBack);
                return;
            }

            ShowTeamSelection(heroPool, selectedHeroes, onToggleHero, onStartBattle, onBack, null, null);
        }

        public void ShowShop(
            IReadOnlyList<InventoryItemData> shopItems,
            int currentGold,
            IReadOnlyList<InventoryItemStack> inventory,
            Action<InventoryItemData> onBuy,
            Action onBack)
        {
            SetBattleHudVisible(false);
            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            overlayPanel.SetAsLastSibling();
            ClearChildren(overlayPanel);

            var panel = CreatePanel("ShopPanel", overlayPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 620f), new Color(0.032f, 0.026f, 0.024f, 0.97f));
            panel.GetComponent<Image>().sprite = descriptionSprite;
            panel.GetComponent<Image>().type = Image.Type.Sliced;

            var title = CreateText("Title", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            title.text = "\u5546\u5e97";
            SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var goldLine = CreateText("Gold", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -96f), new Vector2(-110f, 34f), 18, TextAnchor.MiddleCenter);
            goldLine.text = $"\u5f53\u524d\u91d1\u5e01\uff1a{currentGold}";
            SetTextStyle(goldLine, new Color(1f, 0.78f, 0.34f), true);

            var items = shopItems ?? Array.Empty<InventoryItemData>();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var y = -168f - i * 118f;
                var row = CreatePanel($"ShopItem_{i}", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(-100f, 94f), new Color(0.055f, 0.048f, 0.047f, 0.94f));
                row.offsetMin = new Vector2(60f, row.offsetMin.y);
                row.offsetMax = new Vector2(-60f, row.offsetMax.y);
                row.GetComponent<Image>().sprite = panelSprite;
                row.GetComponent<Image>().type = Image.Type.Sliced;

                var count = inventory != null ? inventory.FirstOrDefault(stack => stack.Item == item).Count : 0;
                var itemText = CreateText("ItemText", row, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 16, TextAnchor.UpperLeft);
                itemText.rectTransform.offsetMin = new Vector2(18f, 10f);
                itemText.rectTransform.offsetMax = new Vector2(-170f, -10f);
                itemText.text = $"{item.itemName}  {item.price}\u91d1\u5e01  \u5df2\u6709 {count}\n{item.description}";
                SetTextStyle(itemText, new Color(0.88f, 0.8f, 0.66f), false);

                CreateButton(row, "\u8d2d\u4e70", new Vector2(650f, -48f), () => onBuy?.Invoke(item), currentGold >= item.price, null);
            }

            CreateButton(panel, "\u8fd4\u56de", new Vector2(430f, -562f), () => onBack?.Invoke(), true, null);
        }

        public void ShowTavern(
            IReadOnlyList<CombatantDefinition> recruitableHeroes,
            int currentGold,
            Action<CombatantDefinition> onRecruit,
            Action onBack)
        {
            EnsureRuntimePrefabUi();
            SetBattleHudVisible(false);
            if (runtimeUiManager != null)
            {
                runtimeUiManager.ShowTavern(recruitableHeroes, currentGold, onRecruit, onBack);
                return;
            }

            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            overlayPanel.SetAsLastSibling();
            ClearChildren(overlayPanel);

            var panel = CreatePanel("TavernPanel", overlayPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 660f), new Color(0.032f, 0.026f, 0.024f, 0.97f));
            panel.GetComponent<Image>().sprite = descriptionSprite;
            panel.GetComponent<Image>().type = Image.Type.Sliced;

            var title = CreateText("Title", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            title.text = "\u9152\u9986";
            SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var goldLine = CreateText("Gold", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -96f), new Vector2(-110f, 34f), 18, TextAnchor.MiddleCenter);
            goldLine.text = $"\u5f53\u524d\u91d1\u5e01\uff1a{currentGold}";
            SetTextStyle(goldLine, new Color(1f, 0.78f, 0.34f), true);

            var heroes = recruitableHeroes ?? Array.Empty<CombatantDefinition>();
            if (heroes.Count == 0)
            {
                var empty = CreateText("Empty", panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 20, TextAnchor.MiddleCenter);
                empty.text = "\u6240\u6709\u82f1\u96c4\u90fd\u5df2\u52a0\u5165\u961f\u4f0d\u3002";
                SetTextStyle(empty, new Color(0.82f, 0.72f, 0.54f), false);
            }
            else
            {
                for (var i = 0; i < heroes.Count; i++)
                {
                    var hero = heroes[i];
                    var card = CreatePanel($"RecruitHero_{i}", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(190f + i * 300f, -330f), new Vector2(260f, 350f), new Color(0.055f, 0.048f, 0.047f, 0.94f));
                    card.GetComponent<Image>().sprite = panelSprite;
                    card.GetComponent<Image>().type = Image.Type.Sliced;

                    var nameText = CreateText("Name", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), new Vector2(-26f, 50f), 19, TextAnchor.MiddleCenter);
                    nameText.text = hero.displayName;
                    SetTextStyle(nameText, new Color(1f, 0.84f, 0.44f), true);

                    var portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                    portraitObject.transform.SetParent(card, false);
                    var portraitRect = portraitObject.GetComponent<RectTransform>();
                    portraitRect.anchorMin = new Vector2(0.5f, 1f);
                    portraitRect.anchorMax = new Vector2(0.5f, 1f);
                    portraitRect.pivot = new Vector2(0.5f, 0.5f);
                    portraitRect.anchoredPosition = new Vector2(0f, -112f);
                    portraitRect.sizeDelta = new Vector2(96f, 122f);
                    var portraitImage = portraitObject.GetComponent<Image>();
                    portraitImage.sprite = ResolveHeroPortrait(hero);
                    portraitImage.color = portraitImage.sprite != null ? Color.white : new Color(0.18f, 0.14f, 0.12f, 1f);
                    portraitImage.preserveAspect = true;

                    var stats = CreateText("Stats", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft);
                    stats.rectTransform.offsetMin = new Vector2(24f, 74f);
                    stats.rectTransform.offsetMax = new Vector2(-24f, -196f);
                    stats.text = $"\u804c\u80fd {hero.ArchetypeDisplayName}\n\u4e13\u7cbe {hero.SpecializationDisplayName}\n\u504f\u597d {hero.PreferredRowDisplayName}\n\u751f\u547d {hero.MaxHealthWithGrowth}\n\u653b\u51fb {hero.AttackWithGrowth}\n\u9632\u5fa1 {hero.DefenseWithGrowth}\n\u901f\u5ea6 {hero.SpeedWithArchetype}\n\u4ef7\u683c {hero.recruitPrice}\u91d1\u5e01";
                    SetTextStyle(stats, new Color(0.88f, 0.8f, 0.66f), false);

                    CreateButton(card, "\u62db\u52df", new Vector2(130f, -306f), () => onRecruit?.Invoke(hero), currentGold >= hero.recruitPrice, null);
                }
            }

            CreateButton(panel, "\u8fd4\u56de", new Vector2(490f, -602f), () => onBack?.Invoke(), true, null);
        }

        public void ShowInventory(
            IReadOnlyList<InventoryItemStack> inventory,
            IReadOnlyList<CombatantDefinition> heroes,
            Action<InventoryItemData, CombatantDefinition> onUse,
            Action onBack)
        {
            SetBattleHudVisible(false);
            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            overlayPanel.SetAsLastSibling();
            ClearChildren(overlayPanel);

            var panel = CreatePanel("InventoryPanel", overlayPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 690f), new Color(0.032f, 0.026f, 0.024f, 0.97f));
            panel.GetComponent<Image>().sprite = descriptionSprite;
            panel.GetComponent<Image>().type = Image.Type.Sliced;

            var title = CreateText("Title", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            title.text = "\u80cc\u5305";
            SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var stacks = inventory ?? Array.Empty<InventoryItemStack>();
            if (stacks.Count == 0)
            {
                var empty = CreateText("Empty", panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 20, TextAnchor.MiddleCenter);
                empty.text = "\u80cc\u5305\u662f\u7a7a\u7684\u3002";
                SetTextStyle(empty, new Color(0.82f, 0.72f, 0.54f), false);
            }
            else
            {
                for (var i = 0; i < stacks.Count; i++)
                {
                    var stack = stacks[i];
                    var x = i % 2 == 0 ? 255f : 725f;
                    var y = -150f - (i / 2) * 250f;
                    var itemPanel = CreatePanel($"InventoryItem_{i}", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(420f, 220f), new Color(0.055f, 0.048f, 0.047f, 0.94f));
                    itemPanel.GetComponent<Image>().sprite = panelSprite;
                    itemPanel.GetComponent<Image>().type = Image.Type.Sliced;

                    var itemText = CreateText("ItemText", itemPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -42f), new Vector2(-32f, 56f), 17, TextAnchor.MiddleCenter);
                    itemText.text = $"{stack.Item.itemName} x{stack.Count}";
                    SetTextStyle(itemText, new Color(0.96f, 0.82f, 0.48f), true);

                    var heroList = heroes ?? Array.Empty<CombatantDefinition>();
                    for (var h = 0; h < heroList.Count && h < 4; h++)
                    {
                        var hero = heroList[h];
                        var usable = IsItemUsableOnHero(stack.Item, hero);
                        var label = $"{hero.displayName} {hero.CurrentHealth}/{hero.MaxHealthWithGrowth}";
                        CreateButton(itemPanel, label, new Vector2(210f, -86f - h * 34f), () => onUse?.Invoke(stack.Item, hero), usable, null);
                    }
                }
            }

            CreateButton(panel, "\u8fd4\u56de", new Vector2(490f, -632f), () => onBack?.Invoke(), true, null);
        }

        public void ShowRecoveryWard(
            IReadOnlyList<CombatantDefinition> heroes,
            int currentGold,
            int treatmentCost,
            Action<CombatantDefinition> onTreat,
            Action onBack)
        {
            SetBattleHudVisible(false);
            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            overlayPanel.SetAsLastSibling();
            ClearChildren(overlayPanel);

            var panel = CreatePanel("RecoveryWardPanel", overlayPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 620f), new Color(0.032f, 0.026f, 0.024f, 0.97f));
            panel.GetComponent<Image>().sprite = descriptionSprite;
            panel.GetComponent<Image>().type = Image.Type.Sliced;

            var title = CreateText("Title", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            title.text = "\u4f24\u5458\u4f11\u6574";
            SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var goldText = CreateText("Gold", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -94f), new Vector2(-120f, 34f), 18, TextAnchor.MiddleCenter);
            goldText.text = $"\u5f53\u524d\u91d1\u5e01\uff1a{currentGold}";
            SetTextStyle(goldText, new Color(1f, 0.78f, 0.34f), true);

            var heroList = (heroes ?? Array.Empty<CombatantDefinition>())
                .Where(hero => hero != null && hero.isHero && hero.isUnlocked)
                .OrderBy(hero => hero.IsRecovering ? 0 : 1)
                .ThenBy(hero => hero.displayName)
                .ToArray();

            if (heroList.Length == 0)
            {
                var empty = CreateText("Empty", panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18, TextAnchor.MiddleCenter);
                empty.text = "\u6682\u65e0\u53ef\u7ba1\u7406\u7684\u82f1\u96c4\u3002";
                SetTextStyle(empty, new Color(0.82f, 0.72f, 0.54f), false);
            }
            else
            {
                for (var i = 0; i < heroList.Length && i < 6; i++)
                {
                    var hero = heroList[i];
                    var y = -160f - i * 68f;
                    var row = CreatePanel($"RecoveryHero_{i}", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(-120f, 52f), new Color(0.055f, 0.048f, 0.047f, 0.94f));
                    row.offsetMin = new Vector2(80f, row.offsetMin.y);
                    row.offsetMax = new Vector2(-80f, row.offsetMax.y);
                    row.GetComponent<Image>().sprite = panelSprite;
                    row.GetComponent<Image>().type = Image.Type.Sliced;

                    var statusText = hero.IsRecovering ? hero.RecoveryDisplayName : "\u53ef\u51fa\u6218";
                    var text = CreateText("Hero", row, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleLeft);
                    text.rectTransform.offsetMin = new Vector2(18f, 0f);
                    text.rectTransform.offsetMax = new Vector2(-260f, 0f);
                    text.text = $"{hero.displayName}    {statusText}    \u751f\u547d {hero.CurrentHealth}/{hero.MaxHealthWithGrowth}";
                    SetTextStyle(text, hero.IsRecovering ? new Color(0.95f, 0.74f, 0.52f) : new Color(0.78f, 0.84f, 0.72f), false);

                    var canTreat = hero.IsRecovering && currentGold >= treatmentCost;
                    var actionLabel = hero.IsRecovering ? $"\u6025\u6551\u6062\u590d {treatmentCost}\u91d1" : "\u5df2\u7a33\u5b9a";
                    CreateButton(row, actionLabel, new Vector2(640f, -26f), () => onTreat?.Invoke(hero), canTreat, null);
                }
            }

            CreateButton(panel, "\u8fd4\u56de\u5927\u5385", new Vector2(490f, -572f), () => onBack?.Invoke(), true, null);
        }

        public void ShowMap(int step, int totalSteps, IReadOnlyList<MapNodeOption> options, Action<MapNodeOption> onSelect)
        {
            SetBattleHudVisible(false);
            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            overlayPanel.SetAsLastSibling();
            ClearChildren(overlayPanel);

            var panel = CreatePanel("MapPanel", overlayPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 560f), new Color(0.032f, 0.026f, 0.024f, 0.97f));
            panel.GetComponent<Image>().sprite = descriptionSprite;
            panel.GetComponent<Image>().type = Image.Type.Sliced;

            var title = CreateText("Title", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            title.text = "\u8def\u7ebf\u9009\u62e9";
            SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var route = CreateText("Route", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -106f), new Vector2(-120f, 40f), 18, TextAnchor.MiddleCenter);
            route.text = $"\u8d77\u70b9  >  \u8282\u70b9 {step}/{totalSteps}  >  \u6700\u7ec8 Boss";
            SetTextStyle(route, new Color(0.82f, 0.72f, 0.54f), false);

            var nodeOptions = options ?? Array.Empty<MapNodeOption>();
            for (var i = 0; i < nodeOptions.Count; i++)
            {
                var option = nodeOptions[i];
                var cardX = nodeOptions.Count == 2 ? 310f + i * 360f : 190f + i * 300f;
                var card = CreatePanel($"MapNode_{i}", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(cardX, -286f), new Vector2(260f, 240f), GetMapNodeColor(option.NodeType));
                card.GetComponent<Image>().sprite = panelSprite;
                card.GetComponent<Image>().type = Image.Type.Sliced;

                var name = CreateText("Name", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -42f), new Vector2(-32f, 52f), 21, TextAnchor.MiddleCenter);
                name.text = option.DisplayName;
                SetTextStyle(name, new Color(1f, 0.84f, 0.44f), true);

                var description = CreateText("Description", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 16, TextAnchor.UpperLeft);
                description.rectTransform.offsetMin = new Vector2(24f, 72f);
                description.rectTransform.offsetMax = new Vector2(-24f, -86f);
                description.text = option.Description;
                SetTextStyle(description, new Color(0.88f, 0.8f, 0.66f), false);

                CreateButton(card, "\u524d\u5f80", new Vector2(130f, -198f), () => onSelect?.Invoke(option), true, null);
            }
        }

        public void ShowRestNode(IReadOnlyList<CombatantDefinition> heroes, Action<CombatantDefinition> onSelectHero)
        {
            SetBattleHudVisible(false);
            EnsureOverlay();
            overlayPanel.gameObject.SetActive(true);
            overlayPanel.SetAsLastSibling();
            ClearChildren(overlayPanel);

            var panel = CreatePanel("RestPanel", overlayPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 560f), new Color(0.032f, 0.026f, 0.024f, 0.97f));
            panel.GetComponent<Image>().sprite = descriptionSprite;
            panel.GetComponent<Image>().type = Image.Type.Sliced;

            var title = CreateText("Title", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            title.text = "\u4f11\u606f\u8282\u70b9";
            SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var subtitle = CreateText("Subtitle", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -98f), new Vector2(-110f, 36f), 18, TextAnchor.MiddleCenter);
            subtitle.text = "\u9009\u62e9\u4e00\u540d\u82f1\u96c4\u6062\u590d 20 \u751f\u547d";
            SetTextStyle(subtitle, new Color(0.82f, 0.72f, 0.54f), false);

            var heroList = heroes ?? Array.Empty<CombatantDefinition>();
            var hasUsableHero = heroList.Any(hero => hero != null && hero.isHero && !hero.IsDead && hero.CurrentHealth < hero.MaxHealthWithGrowth);
            for (var i = 0; i < heroList.Count && i < 4; i++)
            {
                var hero = heroList[i];
                if (hero == null)
                {
                    continue;
                }

                var y = -172f - i * 74f;
                var row = CreatePanel($"RestHero_{i}", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(-120f, 54f), new Color(0.055f, 0.048f, 0.047f, 0.94f));
                row.offsetMin = new Vector2(80f, row.offsetMin.y);
                row.offsetMax = new Vector2(-80f, row.offsetMax.y);
                row.GetComponent<Image>().sprite = panelSprite;
                row.GetComponent<Image>().type = Image.Type.Sliced;

                var status = hero.IsDead ? "\u5df2\u6b7b\u4ea1" : $"{hero.CurrentHealth}/{hero.MaxHealthWithGrowth}";
                var text = CreateText("Hero", row, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 17, TextAnchor.MiddleLeft);
                text.rectTransform.offsetMin = new Vector2(18f, 0f);
                text.rectTransform.offsetMax = new Vector2(-210f, 0f);
                text.text = $"{hero.displayName}    \u751f\u547d {status}";
                SetTextStyle(text, new Color(0.88f, 0.8f, 0.66f), false);

                var usable = hero != null && !hero.IsDead && hero.CurrentHealth < hero.MaxHealthWithGrowth;
                CreateButton(row, "\u4f11\u606f", new Vector2(610f, -27f), () => onSelectHero?.Invoke(hero), usable, null);
            }

            if (!hasUsableHero)
            {
                CreateButton(panel, "\u7ee7\u7eed", new Vector2(420f, -496f), () => onSelectHero?.Invoke(null), true, null);
            }
        }

        public void HideOverlay()
        {
            runtimeUiManager?.HideAll();

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

            var root = runtimeUiManager != null ? runtimeUiManager.GetOverlayRoot() : GetComponent<RectTransform>();
            if (root == null)
            {
                return;
            }

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

        private static bool IsItemUsableOnHero(InventoryItemData item, CombatantDefinition hero)
        {
            if (item == null || hero == null || !hero.isHero)
            {
                return false;
            }

            switch (item.effectType)
            {
                case InventoryItemEffectType.Revive:
                    return hero.IsDead;
                case InventoryItemEffectType.Heal:
                default:
                    return !hero.IsDead && !hero.IsRecovering && hero.CurrentHealth < hero.MaxHealthWithGrowth;
            }
        }

        private static Color GetMapNodeColor(MapNodeType nodeType)
        {
            switch (nodeType)
            {
                case MapNodeType.Rest:
                    return new Color(0.045f, 0.075f, 0.06f, 0.96f);
                case MapNodeType.Chest:
                    return new Color(0.105f, 0.072f, 0.034f, 0.96f);
                case MapNodeType.Battle:
                default:
                    return new Color(0.075f, 0.044f, 0.04f, 0.96f);
            }
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
            if (image.sprite == null)
            {
                EnsureSkinSprites();
                image.sprite = buttonSprite;
                if (image.sprite != null)
                {
                    image.type = Image.Type.Sliced;
                }
            }

            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                CombatAudio.Instance.PlayUiClick();

                if (skill != null)
                {
                    HideSkillDescription();
                }

                action?.Invoke();
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
                    ShowSkillDescription(skill, rect);
                }
            });
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                if (skill != null)
                {
                    HideSkillDescription();
                }
            });
            trigger.triggers.Add(exit);

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ =>
            {
                if (skill != null)
                {
                    ShowSkillDescription(skill, rect);
                }
            });
            trigger.triggers.Add(down);
        }

        private void CreateSkillTemplateButton(RectTransform parent, Button template, SkillUseState state, UnityEngine.Events.UnityAction action, bool interactable)
        {
            if (parent == null || template == null || state.Skill == null)
            {
                return;
            }

            var buttonObject = Instantiate(template.gameObject, parent, false);
            buttonObject.name = $"RuntimeSkillButton_{state.Skill.skillId}";
            buttonObject.SetActive(true);

            var image = buttonObject.GetComponent<Image>();
            if (image != null && image.sprite == null)
            {
                EnsureSkinSprites();
                image.sprite = buttonSprite;
                if (image.sprite != null)
                {
                    image.type = Image.Type.Sliced;
                }
            }

            var button = buttonObject.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                CombatAudio.Instance.PlayUiClick();
                HideSkillDescription();
                action?.Invoke();
            });
            button.interactable = interactable;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.72f);
            button.colors = colors;

            var nameText = buttonObject.transform.Find("SkillNameText")?.GetComponent<Text>();
            var metaText = buttonObject.transform.Find("SkillMetaText")?.GetComponent<Text>();
            var hintText = buttonObject.transform.Find("SkillHintText")?.GetComponent<Text>();

            if (nameText != null)
            {
                nameText.text = state.Skill.skillName;
                nameText.color = interactable ? new Color(0.96f, 0.88f, 0.68f) : new Color(0.62f, 0.62f, 0.62f);
            }

            if (metaText != null)
            {
                metaText.text = BuildSkillMetaText(state.Skill);
                metaText.color = interactable ? new Color(0.82f, 0.74f, 0.6f) : new Color(0.5f, 0.5f, 0.5f);
            }

            if (hintText != null)
            {
                hintText.text = interactable ? BuildSkillRowHint(state.Skill) : state.DisabledReason;
                hintText.color = interactable ? new Color(0.72f, 0.66f, 0.58f) : new Color(0.78f, 0.48f, 0.42f);
            }

            var trigger = buttonObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = buttonObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();
            var rect = buttonObject.GetComponent<RectTransform>();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowSkillDescription(state.Skill, rect));
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => HideSkillDescription());
            trigger.triggers.Add(exit);

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => ShowSkillDescription(state.Skill, rect));
            trigger.triggers.Add(down);
        }

        private static string BuildSkillMetaText(SkillData skill)
        {
            var targetLabel = skill.targetType switch
            {
                SkillDataTargetType.单敌 => "单敌",
                SkillDataTargetType.单友 => "单友",
                SkillDataTargetType.前排两敌 => "前排两敌",
                SkillDataTargetType.全体敌 => "全体敌",
                SkillDataTargetType.自己 => "自己",
                _ => "未知"
            };

            return $"消耗 {skill.manaCost}    冷却 {skill.cooldown}    {targetLabel}";
        }

        private static string BuildSkillRowHint(SkillData skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            if (skill.skillType == SkillDataType.治疗)
            {
                return "恢复 / 稳定 / 净化";
            }

            if (skill.skillType == SkillDataType.控制)
            {
                return "护卫 / 压制 / 站位控制";
            }

            return skill.targetType == SkillDataTargetType.前排两敌
                ? "前排压制 / 多目标"
                : "单体打击 / 节奏推进";
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

            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true)
                         .Where(child => child != parent &&
                                         (child.name.StartsWith("RuntimeButton_", StringComparison.Ordinal) ||
                                          child.name.StartsWith("RuntimeSkillButton_", StringComparison.Ordinal)))
                         .ToArray())
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


