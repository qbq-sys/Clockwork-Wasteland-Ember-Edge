using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class HeroCodexUI : CombatUIScreen
    {
        [SerializeField] public Image heroCodexBg;
        [SerializeField] public Image cardBg;
        [SerializeField] public Image portraitBg;

        private RectTransform heroListRoot;
        private Button heroButtonTemplate;
        private Text titleText;
        private Text detailHeaderText;
        private Text unlockStatusText;
        private Text detailLevelText;
        private Text archetypeText;
        private Text recoveryText;
        private Text statsLabelText;
        private Text statsDataText;
        private Text growthLabelText;
        private Text growthText;
        private Text portraitText;
        private Button backButton;

        private CombatantDefinition selectedHero;
        private IReadOnlyList<CombatantDefinition> allHeroes;
        private Action onBackAction;

        public override void BuildLayout()
        {
            // HeroCodexUI runtime is prefab-driven. Do not clear and rebuild the layout at runtime.
        }

        public void Show(IReadOnlyList<CombatantDefinition> heroPool, Action onBack)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("HeroCodexUI prefab layout is incomplete. Runtime layout rebuilding is disabled; repair the prefab instead.", this);
                return;
            }

            allHeroes = heroPool == null ? Array.Empty<CombatantDefinition>() : heroPool.Where(hero => hero != null && hero.isHero).ToArray();
            onBackAction = onBack;
            selectedHero = allHeroes.FirstOrDefault(hero => hero == selectedHero) ?? allHeroes.FirstOrDefault();

            titleText.text = "英雄图鉴";
            EnsureHeroListLayout();
            BindBackButton();
            RenderHeroList();
            RenderHeroDetail();
        }

        private void RenderHeroList()
        {
            if (heroListRoot == null || heroButtonTemplate == null)
            {
                return;
            }

            ClearHeroListInstances();
            if (allHeroes.Count == 0)
            {
                ConfigureHeroButton(heroButtonTemplate, null, "暂无英雄", false, false);
                heroButtonTemplate.gameObject.SetActive(true);
                return;
            }

            for (var i = 0; i < allHeroes.Count; i++)
            {
                var hero = allHeroes[i];
                var button = i == 0 ? heroButtonTemplate : Instantiate(heroButtonTemplate, heroListRoot);
                button.gameObject.name = $"HeroBtn_{i}";
                button.gameObject.SetActive(true);
                ConfigureHeroButton(button, hero, BuildHeroButtonText(hero), true, hero == selectedHero);
            }
        }

        private void RenderHeroDetail()
        {
            if (detailHeaderText == null || unlockStatusText == null || detailLevelText == null || archetypeText == null || statsDataText == null || growthText == null)
            {
                return;
            }

            if (selectedHero == null)
            {
                detailHeaderText.text = "请从左侧列表选择英雄";
                unlockStatusText.text = string.Empty;
                detailLevelText.text = string.Empty;
                archetypeText.text = string.Empty;
                if (recoveryText != null)
                {
                    recoveryText.text = string.Empty;
                }

                statsLabelText.text = "▎基础属性";
                statsDataText.text = "暂无数据";
                growthLabelText.text = "▎成长与被动";
                growthText.text = "暂无成长信息。";
                if (portraitBg != null)
                {
                    portraitBg.sprite = null;
                    portraitBg.color = new Color(0.08f, 0.07f, 0.06f, 0.9f);
                }

                if (portraitText != null)
                {
                    portraitText.text = "[立绘]";
                }

                return;
            }

            var speedDesc = selectedHero.SpeedWithArchetype >= 7 ? "极快" : selectedHero.SpeedWithArchetype >= 5 ? "较快" : "普通";
            detailHeaderText.text = selectedHero.displayName;
            CombatUIScreenUtility.SetTextStyle(detailHeaderText, new Color(0.98f, 0.85f, 0.42f), true);

            unlockStatusText.text = selectedHero.isUnlocked ? "已解锁" : "未解锁";
            CombatUIScreenUtility.SetTextStyle(unlockStatusText, selectedHero.isUnlocked ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.8f, 0.4f, 0.4f), false);

            detailLevelText.text = $"等级 {selectedHero.Level}   经验 {selectedHero.Experience}/{selectedHero.ExperienceToNextLevel}";
            CombatUIScreenUtility.SetTextStyle(detailLevelText, new Color(0.72f, 0.68f, 0.55f), false);

            archetypeText.text = $"职能 {selectedHero.ArchetypeDisplayName}   专精 {selectedHero.SpecializationDisplayName}   偏好站位 {selectedHero.PreferredRowDisplayName}";
            CombatUIScreenUtility.SetTextStyle(archetypeText, new Color(0.78f, 0.73f, 0.6f), false);

            if (recoveryText != null)
            {
                recoveryText.text = $"当前状态：{selectedHero.RecoveryDisplayName}";
                CombatUIScreenUtility.SetTextStyle(recoveryText, selectedHero.IsRecovering ? new Color(0.93f, 0.68f, 0.45f) : new Color(0.55f, 0.88f, 0.58f), false);
            }

            statsLabelText.text = "▎基础属性";
            statsDataText.text =
                $"生命值：{selectedHero.MaxHealthWithGrowth}    (+{selectedHero.GrowthMaxHealthPerLevel}/级)\n" +
                $"攻击力：{selectedHero.AttackWithGrowth}    (+{selectedHero.GrowthAttackPerLevel}/级)\n" +
                $"防御力：{selectedHero.DefenseWithGrowth}    (+{selectedHero.GrowthDefensePerLevel}/级)\n" +
                $"速度：{selectedHero.SpeedWithArchetype} （{speedDesc}）\n" +
                $"职能定位：{selectedHero.ArchetypeDisplayName}\n" +
                $"原型特征：{selectedHero.ArchetypeSummary}\n" +
                $"专精分支：{selectedHero.SpecializationDisplayName}\n" +
                $"专精特征：{selectedHero.SpecializationSummary}\n" +
                $"偏好站位：{selectedHero.PreferredRowDisplayName}\n" +
                $"招募价格：{selectedHero.recruitPrice} 金币";
            CombatUIScreenUtility.SetTextStyle(statsDataText, new Color(0.82f, 0.78f, 0.66f), false);

            growthLabelText.text = "▎成长与被动";
            growthText.text = HeroProgressionDescriptions.BuildGrowthOverview(selectedHero);
            CombatUIScreenUtility.SetTextStyle(growthText, new Color(0.78f, 0.72f, 0.6f), false);

            if (portraitBg != null)
            {
                portraitBg.sprite = selectedHero.portrait != null ? selectedHero.portrait : selectedHero.battleSprite;
                if (portraitBg.sprite == null && selectedHero.idleAnimationFrames != null && selectedHero.idleAnimationFrames.Length > 0)
                {
                    portraitBg.sprite = selectedHero.idleAnimationFrames[0];
                }

                portraitBg.color = portraitBg.sprite != null ? Color.white : new Color(0.08f, 0.07f, 0.06f, 0.9f);
                portraitBg.preserveAspect = true;
            }

            if (portraitText != null)
            {
                portraitText.text = portraitBg != null && portraitBg.sprite != null ? string.Empty : "[立绘]";
                CombatUIScreenUtility.SetTextStyle(portraitText, new Color(0.5f, 0.45f, 0.35f), false);
            }
        }

        public void RebuildLayoutFromCode(Sprite panelSprite)
        {
            var rootStyle = CombatUIImageStyle.Capture(heroCodexBg);
            var cardStyle = CombatUIImageStyle.Capture(cardBg);
            var portraitStyle = CombatUIImageStyle.Capture(portraitBg);

            CombatUIScreenUtility.ClearChildren(transform);
            var root = PrepareRoot();

            heroCodexBg = CombatUIScreenUtility.CreatePanel("HeroCodexPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080f, 720f), new Color(0.032f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
            rootStyle.ApplyTo(heroCodexBg);
            heroCodexBg.sprite = heroCodexBg.sprite != null ? heroCodexBg.sprite : panelSprite;
            heroCodexBg.type = heroCodexBg.sprite != null ? Image.Type.Sliced : heroCodexBg.type;

            titleText = CombatUIScreenUtility.CreateText("Title", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 28, TextAnchor.MiddleCenter);
            titleText.rectTransform.offsetMin = new Vector2(100f, -78f);
            titleText.rectTransform.offsetMax = new Vector2(-100f, -20f);
            titleText.text = "英雄图鉴";
            CombatUIScreenUtility.SetTextStyle(titleText, new Color(0.96f, 0.82f, 0.48f), true);

            var listPanelImage = CombatUIScreenUtility.CreatePanel("HeroList", heroCodexBg.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, new Color(0.05f, 0.04f, 0.035f, 0.85f)).GetComponent<Image>();
            cardStyle.ApplyTo(listPanelImage);
            listPanelImage.sprite = listPanelImage.sprite != null ? listPanelImage.sprite : panelSprite;
            listPanelImage.type = listPanelImage.sprite != null ? Image.Type.Sliced : listPanelImage.type;
            heroListRoot = listPanelImage.rectTransform;
            heroListRoot.offsetMin = new Vector2(16f, 70f);
            heroListRoot.offsetMax = new Vector2(224f, -72f);

            var heroListLayout = heroListRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            heroListLayout.spacing = 8f;
            heroListLayout.padding = new RectOffset(8, 8, 8, 8);
            heroListLayout.childAlignment = TextAnchor.UpperCenter;
            heroListLayout.childControlWidth = true;
            heroListLayout.childControlHeight = false;
            heroListLayout.childForceExpandWidth = true;
            heroListLayout.childForceExpandHeight = false;
            heroListRoot.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var heroButtonImage = CombatUIScreenUtility.CreatePanel("HeroBtn_0", heroListRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 62f), new Color(0.06f, 0.05f, 0.04f, 0.8f)).GetComponent<Image>();
            cardBg = heroButtonImage;
            cardStyle.ApplyTo(heroButtonImage);
            heroButtonImage.sprite = heroButtonImage.sprite != null ? heroButtonImage.sprite : panelSprite;
            heroButtonImage.type = heroButtonImage.sprite != null ? Image.Type.Sliced : heroButtonImage.type;
            var heroButtonLayout = heroButtonImage.gameObject.AddComponent<LayoutElement>();
            heroButtonLayout.minHeight = 62f;
            heroButtonLayout.preferredHeight = 62f;
            heroButtonTemplate = heroButtonImage.gameObject.AddComponent<Button>();
            heroButtonTemplate.targetGraphic = heroButtonImage;
            var colors = heroButtonTemplate.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor = new Color(0.95f, 0.95f, 0.95f, 0.92f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.55f);
            heroButtonTemplate.colors = colors;

            var heroButtonLabel = CombatUIScreenUtility.CreateText("Name_0", heroButtonImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleLeft);
            heroButtonLabel.rectTransform.offsetMin = new Vector2(12f, 10f);
            heroButtonLabel.rectTransform.offsetMax = new Vector2(-12f, -10f);
            heroButtonLabel.text = "英雄列表模板";
            CombatUIScreenUtility.SetTextStyle(heroButtonLabel, new Color(0.78f, 0.72f, 0.6f), false);

            portraitBg = CombatUIScreenUtility.CreatePanel("Portrait", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(248f, -72f), new Vector2(148f, 176f), new Color(0.08f, 0.07f, 0.06f, 0.9f)).GetComponent<Image>();
            portraitStyle.ApplyTo(portraitBg);
            portraitBg.sprite = portraitBg.sprite != null ? portraitBg.sprite : panelSprite;
            portraitBg.type = portraitBg.sprite != null ? Image.Type.Sliced : portraitBg.type;
            portraitText = CombatUIScreenUtility.CreateText("PortraitText", portraitBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleCenter);
            portraitText.text = "[立绘]";
            CombatUIScreenUtility.SetTextStyle(portraitText, new Color(0.5f, 0.45f, 0.35f), false);

            detailHeaderText = CombatUIScreenUtility.CreateText("DetailHeader", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 24, TextAnchor.MiddleLeft);
            detailHeaderText.rectTransform.offsetMin = new Vector2(416f, -110f);
            detailHeaderText.rectTransform.offsetMax = new Vector2(-44f, -70f);

            unlockStatusText = CombatUIScreenUtility.CreateText("UnlockStatus", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleLeft);
            unlockStatusText.rectTransform.offsetMin = new Vector2(416f, -134f);
            unlockStatusText.rectTransform.offsetMax = new Vector2(-44f, -108f);

            detailLevelText = CombatUIScreenUtility.CreateText("DetailLevel", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleLeft);
            detailLevelText.rectTransform.offsetMin = new Vector2(416f, -154f);
            detailLevelText.rectTransform.offsetMax = new Vector2(-44f, -128f);

            archetypeText = CombatUIScreenUtility.CreateText("Archetype", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleLeft);
            archetypeText.rectTransform.offsetMin = new Vector2(416f, -174f);
            archetypeText.rectTransform.offsetMax = new Vector2(-44f, -148f);

            recoveryText = CombatUIScreenUtility.CreateText("Recovery", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleLeft);
            recoveryText.rectTransform.offsetMin = new Vector2(416f, -194f);
            recoveryText.rectTransform.offsetMax = new Vector2(-44f, -168f);
            CombatUIScreenUtility.SetTextStyle(detailHeaderText, new Color(0.98f, 0.85f, 0.42f), true);
            CombatUIScreenUtility.SetTextStyle(unlockStatusText, new Color(0.72f, 0.68f, 0.55f), false);
            CombatUIScreenUtility.SetTextStyle(detailLevelText, new Color(0.72f, 0.68f, 0.55f), false);
            CombatUIScreenUtility.SetTextStyle(archetypeText, new Color(0.78f, 0.73f, 0.6f), false);
            CombatUIScreenUtility.SetTextStyle(recoveryText, new Color(0.72f, 0.68f, 0.55f), false);

            var divider = CombatUIScreenUtility.CreatePanel("Divider", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0.3f, 0.25f, 0.15f, 0.8f));
            divider.offsetMin = new Vector2(248f, -258f);
            divider.offsetMax = new Vector2(-44f, -256f);

            statsLabelText = CombatUIScreenUtility.CreateText("StatsLabel", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(248f, -272f), new Vector2(140f, 24f), 16, TextAnchor.MiddleLeft);
            statsLabelText.text = "▎基础属性";
            CombatUIScreenUtility.SetTextStyle(statsLabelText, new Color(0.96f, 0.82f, 0.48f), true);

            statsDataText = CombatUIScreenUtility.CreateText("StatsData", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 14, TextAnchor.UpperLeft);
            statsDataText.rectTransform.offsetMin = new Vector2(248f, -446f);
            statsDataText.rectTransform.offsetMax = new Vector2(-44f, -300f);
            CombatUIScreenUtility.SetTextStyle(statsDataText, new Color(0.82f, 0.78f, 0.66f), false);

            growthLabelText = CombatUIScreenUtility.CreateText("GrowthLabel", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(248f, -458f), new Vector2(180f, 24f), 16, TextAnchor.MiddleLeft);
            growthLabelText.text = "▎成长与被动";
            CombatUIScreenUtility.SetTextStyle(growthLabelText, new Color(0.96f, 0.82f, 0.48f), true);

            growthText = CombatUIScreenUtility.CreateText("GrowthText", heroCodexBg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 12, TextAnchor.UpperLeft);
            growthText.rectTransform.offsetMin = new Vector2(248f, 28f);
            growthText.rectTransform.offsetMax = new Vector2(-44f, -492f);
            CombatUIScreenUtility.SetTextStyle(growthText, new Color(0.78f, 0.72f, 0.6f), false);

            backButton = CombatUIScreenUtility.CreateButton(heroCodexBg.rectTransform, "返回大厅", new Vector2(540f, -662f), null, true);
            TryBindExistingLayout();
        }

        private bool TryBindExistingLayout()
        {
            if (heroCodexBg != null && heroListRoot != null && heroButtonTemplate != null && titleText != null && detailHeaderText != null &&
                unlockStatusText != null && detailLevelText != null && archetypeText != null && statsLabelText != null &&
                statsDataText != null && growthLabelText != null && growthText != null && portraitBg != null && portraitText != null && backButton != null)
            {
                return true;
            }

            heroCodexBg = transform.Find("HeroCodexPanel")?.GetComponent<Image>();
            var root = heroCodexBg != null ? heroCodexBg.rectTransform : null;
            heroListRoot = root != null ? root.Find("HeroList") as RectTransform : null;
            heroButtonTemplate = heroListRoot != null ? heroListRoot.Find("HeroBtn_0")?.GetComponent<Button>() : null;
            titleText = root != null ? root.Find("Title")?.GetComponent<Text>() : null;
            detailHeaderText = root != null ? root.Find("DetailHeader")?.GetComponent<Text>() : null;
            unlockStatusText = root != null ? root.Find("UnlockStatus")?.GetComponent<Text>() : null;
            detailLevelText = root != null ? root.Find("DetailLevel")?.GetComponent<Text>() : null;
            archetypeText = root != null ? root.Find("Archetype")?.GetComponent<Text>() : null;
            recoveryText = root != null ? root.Find("Recovery")?.GetComponent<Text>() : null;
            statsLabelText = root != null ? root.Find("StatsLabel")?.GetComponent<Text>() : null;
            statsDataText = root != null ? root.Find("StatsData")?.GetComponent<Text>() : null;
            growthLabelText = FindTextByPreferredNames(root, "GrowthLabel", "PassiveLabel");
            growthText = FindTextByPreferredNames(root, "GrowthText", "NoPassive", "PassiveDesc");
            portraitBg = root != null ? root.Find("Portrait")?.GetComponent<Image>() : null;
            portraitText = portraitBg != null ? portraitBg.transform.Find("PortraitText")?.GetComponent<Text>() : null;
            backButton = FindButtonByPreferredNames(root, "BackButton", "RuntimeButton_返回大厅");

            if (statsDataText != null)
            {
                statsDataText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (growthText != null)
            {
                growthText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            return heroCodexBg != null && heroListRoot != null && heroButtonTemplate != null && titleText != null && detailHeaderText != null &&
                   unlockStatusText != null && detailLevelText != null && archetypeText != null && statsLabelText != null &&
                   statsDataText != null && growthLabelText != null && growthText != null && portraitBg != null && portraitText != null && backButton != null;
        }

        private void BindBackButton()
        {
            if (backButton == null)
            {
                return;
            }

            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() =>
            {
                CombatAudio.Instance.PlayUiClick();
                onBackAction?.Invoke();
            });
        }

        private void EnsureHeroListLayout()
        {
            if (heroListRoot == null || heroButtonTemplate == null)
            {
                return;
            }

            var layout = heroListRoot.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = heroListRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = heroListRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = heroListRoot.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layoutElement = heroButtonTemplate.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = heroButtonTemplate.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 62f;
            layoutElement.preferredHeight = 62f;
        }

        private void ClearHeroListInstances()
        {
            if (heroListRoot == null || heroButtonTemplate == null)
            {
                return;
            }

            for (var i = heroListRoot.childCount - 1; i >= 0; i--)
            {
                var child = heroListRoot.GetChild(i);
                if (child == heroButtonTemplate.transform)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }

        private void ConfigureHeroButton(Button button, CombatantDefinition hero, string labelText, bool interactable, bool isSelected)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            var label = FindTextByPreferredNames(button.transform, "Name", "Name_0", "Label");
            if (label != null)
            {
                label.text = labelText;
                CombatUIScreenUtility.SetTextStyle(label, isSelected ? new Color(1f, 0.9f, 0.5f) : new Color(0.78f, 0.72f, 0.6f), isSelected);
            }

            if (image != null)
            {
                image.color = isSelected ? new Color(0.15f, 0.12f, 0.08f, 0.95f) : new Color(0.06f, 0.05f, 0.04f, 0.8f);
            }

            button.onClick.RemoveAllListeners();
            button.interactable = interactable;
            if (interactable && hero != null)
            {
                button.onClick.AddListener(() =>
                {
                    CombatAudio.Instance.PlayUiClick();
                    selectedHero = hero;
                    RenderHeroList();
                    RenderHeroDetail();
                });
            }
        }

        private static string BuildHeroButtonText(CombatantDefinition hero)
        {
            if (hero == null)
            {
                return string.Empty;
            }

            var statusLabel = hero.isUnlocked ? "[已解锁]" : "[未解锁]";
            return $"{statusLabel}\n{hero.displayName}  Lv.{hero.Level}  {hero.ArchetypeDisplayName}";
        }

        private static Text FindTextByPreferredNames(Transform root, params string[] names)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var name in names)
            {
                var candidate = root.Find(name)?.GetComponent<Text>();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Button FindButtonByPreferredNames(Transform root, params string[] names)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var name in names)
            {
                var candidate = root.Find(name)?.GetComponent<Button>();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
