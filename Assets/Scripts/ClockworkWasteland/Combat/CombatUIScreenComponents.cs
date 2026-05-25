using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ClockworkWasteland.Combat
{
    internal static class CombatUIPaths
    {
        public const string PrefabRootPath = "Assets/UI/Prefabs";
        public const string ScreenPrefabRootPath = PrefabRootPath + "/Screens";
        public const string HudPrefabRootPath = PrefabRootPath + "/HUD";
        public const string PopupPrefabRootPath = PrefabRootPath + "/Popups";

        public const string LobbyPrefabPath = ScreenPrefabRootPath + "/LobbyUI.prefab";
        public const string StartMenuPrefabPath = ScreenPrefabRootPath + "/StartMenuUI.prefab";
        public const string TavernPrefabPath = ScreenPrefabRootPath + "/TavernUI.prefab";
        public const string AdventurePrefabPath = ScreenPrefabRootPath + "/AdventureMapUI.prefab";
        public const string TeamSelectionPrefabPath = ScreenPrefabRootPath + "/TeamSelectionUI.prefab";
        public const string HeroCodexPrefabPath = ScreenPrefabRootPath + "/HeroCodexUI.prefab";
        public const string SettingsPrefabPath = ScreenPrefabRootPath + "/SettingsUI.prefab";
        public const string SaveSlotPrefabPath = ScreenPrefabRootPath + "/SaveSlotUI.prefab";
        public const string LevelUpPrefabPath = ScreenPrefabRootPath + "/LevelUpUI.prefab";
        public const string PopupPrefabPath = PopupPrefabRootPath + "/PopupUI.prefab";
        public const string BattleHudPrefabPath = HudPrefabRootPath + "/BattleHudUI.prefab";
        public const string SkillDescriptionPrefabPath = HudPrefabRootPath + "/SkillDescriptionUI.prefab";
        public const string SkillDescriptionPanelPrefabPath = HudPrefabRootPath + "/SkillDescriptionPanelUI.prefab";
    }

    public sealed partial class UIManager
    {
        private const string UIRootName = "UIRoot";
        private const string ScreenRootName = "ScreenRoot";
        private const string HudRootName = "HudRoot";
        private const string PopupRootName = "PopupRoot";
        private const string OverlayRootName = "OverlayRoot";

        [SerializeField] private StartMenuUI startMenuPrefab;
        [SerializeField] private LobbyUI lobbyPrefab;
        [SerializeField] private TavernUI tavernPrefab;
        [SerializeField] private AdventureMapUI adventureMapPrefab;
        [SerializeField] private TeamSelectionUI teamSelectionPrefab;
        [SerializeField] private HeroCodexUI heroCodexPrefab;
        [SerializeField] private SettingsUI settingsPrefab;
        [SerializeField] private SaveSlotUI saveSlotPrefab;
        [SerializeField] private LevelUpUI levelUpPrefab;
        [SerializeField] private PopupUI popupPrefab;
        [SerializeField] private BattleHudUI battleHudPrefab;
        [SerializeField] private SkillDescriptionUI skillDescriptionPrefab;

        private readonly Dictionary<Type, CombatUIScreen> cachedScreens = new Dictionary<Type, CombatUIScreen>();
        private PopupUI popupInstance;
        private RectTransform uiRoot;
        private RectTransform screenRoot;
        private RectTransform hudRoot;
        private RectTransform popupRoot;
        private RectTransform overlayRoot;

        public static UIManager Instance { get; private set; }

        public static UIManager Ensure(RectTransform canvasRoot)
        {
            if (Instance == null)
            {
                var managerObject = new GameObject(UIRootName, typeof(RectTransform), typeof(UIManager));
                Instance = managerObject.GetComponent<UIManager>();
            }

            Instance.Initialize(canvasRoot);
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            uiRoot = transform as RectTransform;
        }

        public void Initialize(RectTransform canvasRoot)
        {
            uiRoot = transform as RectTransform;
            if (uiRoot == null)
            {
                return;
            }

            if (canvasRoot != null && transform.parent != canvasRoot)
            {
                transform.SetParent(canvasRoot, false);
            }

            CombatUIScreenUtility.Stretch(uiRoot);
            screenRoot = EnsureContainer(screenRoot, ScreenRootName);
            hudRoot = EnsureContainer(hudRoot, HudRootName);
            popupRoot = EnsureContainer(popupRoot, PopupRootName);
            overlayRoot = EnsureContainer(overlayRoot, OverlayRootName);
        }

        public void HideAll()
        {
            foreach (var screen in cachedScreens.Values)
            {
                if (screen != null && !(screen is BattleHudUI) && !(screen is SkillDescriptionUI))
                {
                    screen.gameObject.SetActive(false);
                }
            }

            if (popupInstance != null)
            {
                popupInstance.gameObject.SetActive(false);
            }
        }

        public void ShowStartMenu(bool showContinue, Action onStartNewGame, Action onContinueGame, Action onOpenSettings, Action onQuit, Action onBack)
        {
            var screen = ShowScreen(startMenuPrefab, CombatUIPaths.StartMenuPrefabPath);
            screen.Show(showContinue, onStartNewGame, onContinueGame, onOpenSettings, onQuit, onBack);
        }

        public void ShowLobby(int currentGold, Action onOpenTavern, Action onOpenAdventure, Action onOpenRecoveryWard, Action onOpenHeroCodex, Action onOpenMenu)
        {
            var screen = ShowScreen(lobbyPrefab, CombatUIPaths.LobbyPrefabPath);
            screen.Show(currentGold, onOpenTavern, onOpenAdventure, onOpenRecoveryWard, onOpenHeroCodex, onOpenMenu);
        }

        public void ShowTavern(IReadOnlyList<CombatantDefinition> recruitableHeroes, int currentGold, Action<CombatantDefinition> onRecruit, Action onBack)
        {
            var screen = ShowScreen(tavernPrefab, CombatUIPaths.TavernPrefabPath);
            screen.Show(recruitableHeroes, currentGold, onRecruit, onBack);
        }

        public void ShowAdventureMap(IReadOnlyList<AdventureMapOption> maps, Action<AdventureMapOption> onSelect, Action onBack)
        {
            var screen = ShowScreen(adventureMapPrefab, CombatUIPaths.AdventurePrefabPath);
            screen.Show(maps, onSelect, onBack);
        }

        public void ShowTeamSelection(IReadOnlyList<CombatantDefinition> heroPool, IReadOnlyList<CombatantDefinition> selectedHeroes, Action<CombatantDefinition> onToggleHero, Action onStartBattle, Action onBack)
        {
            var screen = ShowScreen(teamSelectionPrefab, CombatUIPaths.TeamSelectionPrefabPath);
            screen.Show(heroPool, selectedHeroes, onToggleHero, onStartBattle, onBack);
        }

        public void ShowHeroCodex(IReadOnlyList<CombatantDefinition> heroPool, Action onBack)
        {
            var screen = ShowScreen(heroCodexPrefab, CombatUIPaths.HeroCodexPrefabPath);
            screen.Show(heroPool, onBack);
        }

        public void ShowSettings(Action onBack, Action onSaveGame)
        {
            var screen = ShowScreen(settingsPrefab, CombatUIPaths.SettingsPrefabPath);
            screen.Show(onBack, onSaveGame);
        }

        public void ShowSaveSlots(string title, IReadOnlyList<SaveSlotSummary> slots, bool allowEmptySelection, Action<int> onSelect, Action<int> onDelete, Action onBack)
        {
            var screen = ShowScreen(saveSlotPrefab, CombatUIPaths.SaveSlotPrefabPath);
            screen.Show(title, slots, allowEmptySelection, onSelect, onDelete, onBack);
        }

        public void ShowLevelUp(LevelUpPresentation presentation, Action<LevelUpOptionData> onSelect)
        {
            var screen = ShowScreen(levelUpPrefab, CombatUIPaths.LevelUpPrefabPath);
            screen.Show(presentation, onSelect);
        }

        public void ShowPopup(string message, string buttonLabel, Action onContinue)
        {
            popupInstance = GetOrCreatePopup();
            popupInstance.gameObject.SetActive(true);
            popupInstance.transform.SetAsLastSibling();
            popupInstance.Show(message, buttonLabel, onContinue);
        }

        public void ShowChoicePopup(string message, string leftLabel, Action onLeft, string rightLabel, Action onRight)
        {
            popupInstance = GetOrCreatePopup();
            popupInstance.gameObject.SetActive(true);
            popupInstance.transform.SetAsLastSibling();
            popupInstance.ShowChoice(message, leftLabel, onLeft, rightLabel, onRight);
        }

        public BattleHudUI GetBattleHud()
        {
            return GetOrCreateScreen(battleHudPrefab, CombatUIPaths.BattleHudPrefabPath);
        }

        public SkillDescriptionUI GetSkillDescription()
        {
            return GetOrCreateScreen(skillDescriptionPrefab, CombatUIPaths.SkillDescriptionPrefabPath);
        }

        public RectTransform GetHudRoot()
        {
            return hudRoot != null ? hudRoot : screenRoot;
        }

        public RectTransform GetOverlayRoot()
        {
            return overlayRoot != null ? overlayRoot : popupRoot != null ? popupRoot : screenRoot;
        }

        private T ShowScreen<T>(T prefab, string assetPath) where T : CombatUIScreen
        {
            foreach (var screen in cachedScreens.Values)
            {
                if (screen != null && !(screen is BattleHudUI) && !(screen is SkillDescriptionUI))
                {
                    screen.gameObject.SetActive(false);
                }
            }

            var instance = GetOrCreateScreen(prefab, assetPath);
            instance.gameObject.SetActive(true);
            instance.transform.SetAsLastSibling();
            return instance;
        }

        private T GetOrCreateScreen<T>(T prefab, string assetPath) where T : CombatUIScreen
        {
            var type = typeof(T);
            if (cachedScreens.TryGetValue(type, out var cached) && cached != null)
            {
                return (T)cached;
            }

            var source = prefab != null ? prefab : LoadPrefab<T>(assetPath);
            T instance;
            var parentRoot = GetContainerFor(type);
            if (source != null)
            {
                instance = Instantiate(source, parentRoot);
            }
            else
            {
                var screenObject = new GameObject(type.Name, typeof(RectTransform), type);
                screenObject.transform.SetParent(parentRoot, false);
                instance = screenObject.GetComponent<T>();
            }

            CombatUIScreenUtility.Stretch(instance.transform as RectTransform);
            CombatUIScreenUtility.ApplyPreferredFonts(instance.transform);
            instance.gameObject.SetActive(false);
            cachedScreens[type] = instance;
            return instance;
        }

        private PopupUI GetOrCreatePopup()
        {
            if (popupInstance != null)
            {
                return popupInstance;
            }

            popupInstance = GetOrCreateScreen(popupPrefab, CombatUIPaths.PopupPrefabPath);
            return popupInstance;
        }

        private static T LoadPrefab<T>(string assetPath) where T : CombatUIScreen
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
            return null;
#endif
        }

        private RectTransform EnsureContainer(RectTransform current, string containerName)
        {
            var existing = current != null ? current : uiRoot.Find(containerName) as RectTransform;
            if (existing == null)
            {
                var child = new GameObject(containerName, typeof(RectTransform));
                existing = child.GetComponent<RectTransform>();
                existing.SetParent(uiRoot, false);
            }

            CombatUIScreenUtility.Stretch(existing);
            return existing;
        }

        private RectTransform GetContainerFor(Type screenType)
        {
            if (screenType == typeof(BattleHudUI) || screenType == typeof(SkillDescriptionUI))
            {
                return hudRoot != null ? hudRoot : screenRoot;
            }

            if (screenType == typeof(PopupUI))
            {
                return popupRoot != null ? popupRoot : screenRoot;
            }

            return screenRoot != null ? screenRoot : uiRoot;
        }
    }

    public abstract partial class CombatUIScreen
    {
        public virtual void BuildLayout()
        {
            CombatUIScreenUtility.ClearChildren(transform);
        }

        protected RectTransform PrepareRoot()
        {
            var rect = transform as RectTransform;
            if (rect != null)
            {
                CombatUIScreenUtility.Stretch(rect);
            }

            return rect;
        }
    }

    public sealed partial class StartMenuUI
    {
        public Image startMenuBg;
        public Image heroShowcaseBg;

        public void Show(bool showContinue, Action onStartNewGame, Action onContinueGame, Action onOpenSettings, Action onQuit, Action onBack)
        {
            var panelStyle = CombatUIImageStyle.Capture(startMenuBg);
            var showcaseStyle = CombatUIImageStyle.Capture(heroShowcaseBg);
            BuildLayout();
            var root = PrepareRoot();
            startMenuBg = CombatUIScreenUtility.CreatePanel("StartMenuPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.026f, 0.024f, 0.026f, 0.98f)).GetComponent<Image>();
            panelStyle.ApplyTo(startMenuBg);

            var title = CombatUIScreenUtility.CreateText("Title", startMenuBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -108f), new Vector2(-120f, 90f), 36, TextAnchor.MiddleCenter);
            title.text = "Clockwork Wasteland\nEmber Edge";
            CombatUIScreenUtility.SetTextStyle(title, new Color(0.98f, 0.78f, 0.38f), true);

            heroShowcaseBg = CombatUIScreenUtility.CreatePanel("StartMenuShowcase", startMenuBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), new Vector2(760f, 420f), new Color(0.06f, 0.052f, 0.05f, 0.72f)).GetComponent<Image>();
            showcaseStyle.ApplyTo(heroShowcaseBg);
            var centerText = CombatUIScreenUtility.CreateText("CenterText", heroShowcaseBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 22, TextAnchor.MiddleCenter);
            centerText.text = "\u5f00\u59cb\u754c\u9762\u80cc\u666f / \u7acb\u7ed8\u9884\u7559";
            CombatUIScreenUtility.SetTextStyle(centerText, new Color(0.72f, 0.66f, 0.58f), false);

            CombatUIScreenUtility.CreateButton(startMenuBg.rectTransform, "\u65b0\u6e38\u620f", new Vector2(560f, -860f), () => onStartNewGame?.Invoke(), true);
            if (showContinue)
            {
                CombatUIScreenUtility.CreateButton(startMenuBg.rectTransform, "\u7ee7\u7eed\u6e38\u620f", new Vector2(760f, -860f), () => onContinueGame?.Invoke(), true);
            }

            CombatUIScreenUtility.CreateButton(startMenuBg.rectTransform, "\u8bbe\u7f6e", new Vector2(960f, -860f), () => onOpenSettings?.Invoke(), true);
            CombatUIScreenUtility.CreateButton(startMenuBg.rectTransform, "\u9000\u51fa", new Vector2(1160f, -860f), () => onQuit?.Invoke(), true);
            if (onBack != null)
            {
                CombatUIScreenUtility.CreateButton(startMenuBg.rectTransform, "\u8fd4\u56de\u5927\u5385", new Vector2(160f, -860f), () => onBack?.Invoke(), true);
            }
        }
    }

    public sealed partial class LobbyUI
    {
        public Image lobbyBg;
        public Image heroShowcaseBg;
        public Image cardBg;

        public void Show(int currentGold, Action onOpenTavern, Action onOpenAdventure, Action onOpenRecoveryWard, Action onOpenHeroCodex, Action onOpenMenu)
        {
            var lobbyStyle = CombatUIImageStyle.Capture(lobbyBg);
            var showcaseStyle = CombatUIImageStyle.Capture(heroShowcaseBg);
            BuildLayout();
            var root = PrepareRoot();
            lobbyBg = CombatUIScreenUtility.CreatePanel("LobbyPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.026f, 0.024f, 0.026f, 0.98f)).GetComponent<Image>();
            lobbyStyle.ApplyTo(lobbyBg);

            var title = CombatUIScreenUtility.CreateText("Title", lobbyBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -108f), new Vector2(-120f, 90f), 36, TextAnchor.MiddleCenter);
            title.text = "Clockwork Wasteland\nEmber Edge";
            CombatUIScreenUtility.SetTextStyle(title, new Color(0.98f, 0.78f, 0.38f), true);

            heroShowcaseBg = CombatUIScreenUtility.CreatePanel("HeroShowcasePlaceholder", lobbyBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), new Vector2(760f, 420f), new Color(0.06f, 0.052f, 0.05f, 0.72f)).GetComponent<Image>();
            showcaseStyle.ApplyTo(heroShowcaseBg);
            cardBg = heroShowcaseBg;
            var centerText = CombatUIScreenUtility.CreateText("CenterText", heroShowcaseBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 22, TextAnchor.MiddleCenter);
            centerText.text = "\u5927\u5385\u80cc\u666f / \u82f1\u96c4\u7acb\u7ed8\u9884\u7559";
            CombatUIScreenUtility.SetTextStyle(centerText, new Color(0.72f, 0.66f, 0.58f), false);

            var gold = CombatUIScreenUtility.CreateText("Gold", lobbyBg.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-160f, -40f), new Vector2(260f, 42f), 20, TextAnchor.MiddleRight);
            gold.text = $"\u91d1\u5e01\uff1a{Mathf.Max(0, currentGold)}";
            CombatUIScreenUtility.SetTextStyle(gold, new Color(1f, 0.82f, 0.36f), true);

            CombatUIScreenUtility.CreateButton(lobbyBg.rectTransform, "\u9152\u9986", new Vector2(380f, -928f), () => onOpenTavern?.Invoke(), true);
            CombatUIScreenUtility.CreateButton(lobbyBg.rectTransform, "\u5192\u9669", new Vector2(560f, -928f), () => onOpenAdventure?.Invoke(), true);
            CombatUIScreenUtility.CreateButton(lobbyBg.rectTransform, "\u4f24\u5458\u4f11\u6574", new Vector2(740f, -928f), () => onOpenRecoveryWard?.Invoke(), true);
            CombatUIScreenUtility.CreateButton(lobbyBg.rectTransform, "\u82f1\u96c4\u56fe\u9274", new Vector2(920f, -928f), () => onOpenHeroCodex?.Invoke(), true);
            CombatUIScreenUtility.CreateButton(lobbyBg.rectTransform, "\u4e3b\u83dc\u5355", new Vector2(1100f, -928f), () => onOpenMenu?.Invoke(), true);
        }
    }

    public sealed partial class TavernUI
    {
        public Image tavernBg;
        public Image cardBg;

        public void Show(IReadOnlyList<CombatantDefinition> recruitableHeroes, int currentGold, Action<CombatantDefinition> onRecruit, Action onBack)
        {
            var tavernStyle = CombatUIImageStyle.Capture(tavernBg);
            var cardStyle = CombatUIImageStyle.Capture(cardBg);
            BuildLayout();
            var root = PrepareRoot();
            tavernBg = CombatUIScreenUtility.CreatePanel("TavernPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 660f), new Color(0.032f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
            tavernStyle.ApplyTo(tavernBg);

            var title = CombatUIScreenUtility.CreateText("Title", tavernBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            title.text = "\u9152\u9986";
            CombatUIScreenUtility.SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var goldLine = CombatUIScreenUtility.CreateText("Gold", tavernBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -96f), new Vector2(-110f, 34f), 18, TextAnchor.MiddleCenter);
            goldLine.text = $"\u5f53\u524d\u91d1\u5e01\uff1a{currentGold}";
            CombatUIScreenUtility.SetTextStyle(goldLine, new Color(1f, 0.78f, 0.34f), true);

            var heroes = recruitableHeroes ?? Array.Empty<CombatantDefinition>();
            if (heroes.Count == 0)
            {
                var empty = CombatUIScreenUtility.CreateText("Empty", tavernBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 20, TextAnchor.MiddleCenter);
                empty.text = "\u6240\u6709\u82f1\u96c4\u90fd\u5df2\u52a0\u5165\u961f\u4f0d\u3002";
                CombatUIScreenUtility.SetTextStyle(empty, new Color(0.82f, 0.72f, 0.54f), false);
            }
            else
            {
                for (var i = 0; i < heroes.Count; i++)
                {
                    var hero = heroes[i];
                    var card = CombatUIScreenUtility.CreatePanel($"RecruitHero_{i}", tavernBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(190f + i * 300f, -330f), new Vector2(260f, 350f), new Color(0.055f, 0.048f, 0.047f, 0.94f)).GetComponent<Image>();
                    cardStyle.ApplyTo(card);
                    cardBg = card;

                    var nameText = CombatUIScreenUtility.CreateText("Name", card.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), new Vector2(-26f, 50f), 19, TextAnchor.MiddleCenter);
                    nameText.text = hero.displayName;
                    CombatUIScreenUtility.SetTextStyle(nameText, new Color(1f, 0.84f, 0.44f), true);
                    CombatUIScreenUtility.CreatePortrait(card.rectTransform, hero, new Vector2(0f, -112f), new Vector2(96f, 122f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

                    var stats = CombatUIScreenUtility.CreateText("Stats", card.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft);
                    stats.rectTransform.offsetMin = new Vector2(24f, 74f);
                    stats.rectTransform.offsetMax = new Vector2(-24f, -196f);
                    stats.text = $"\u804c\u80fd {hero.ArchetypeDisplayName}\n\u4e13\u7cbe {hero.SpecializationDisplayName}\n\u504f\u597d {hero.PreferredRowDisplayName}\n\u751f\u547d {hero.MaxHealthWithGrowth}\n\u653b\u51fb {hero.AttackWithGrowth}\n\u9632\u5fa1 {hero.DefenseWithGrowth}\n\u901f\u5ea6 {hero.SpeedWithArchetype}\n\u4ef7\u683c {hero.recruitPrice}\u91d1\u5e01";
                    CombatUIScreenUtility.SetTextStyle(stats, new Color(0.88f, 0.8f, 0.66f), false);

                    CombatUIScreenUtility.CreateButton(card.rectTransform, "\u62db\u52df", new Vector2(130f, -306f), () => onRecruit?.Invoke(hero), currentGold >= hero.recruitPrice);
                }
            }

            CombatUIScreenUtility.CreateButton(tavernBg.rectTransform, "\u8fd4\u56de\u5927\u5385", new Vector2(490f, -602f), () => onBack?.Invoke(), true);
        }
    }

    public sealed partial class AdventureMapUI
    {
        public Image adventureBg;
        public Image mapCardBg;

        public void Show(IReadOnlyList<AdventureMapOption> maps, Action<AdventureMapOption> onSelect, Action onBack)
        {
            var adventureStyle = CombatUIImageStyle.Capture(adventureBg);
            var cardStyle = CombatUIImageStyle.Capture(mapCardBg);
            BuildLayout();
            var root = PrepareRoot();
            adventureBg = CombatUIScreenUtility.CreatePanel("AdventurePanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 660f), new Color(0.032f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
            adventureStyle.ApplyTo(adventureBg);

            var title = CombatUIScreenUtility.CreateText("Title", adventureBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -54f), new Vector2(-80f, 62f), 30, TextAnchor.MiddleCenter);
            title.text = "\u5192\u9669";
            CombatUIScreenUtility.SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var mapList = maps ?? Array.Empty<AdventureMapOption>();
            for (var i = 0; i < mapList.Count; i++)
            {
                var map = mapList[i];
                var card = CombatUIScreenUtility.CreatePanel($"AdventureMap_{i}", adventureBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(210f + i * 310f, -330f), new Vector2(280f, 360f), new Color(0.055f, 0.048f, 0.047f, 0.94f)).GetComponent<Image>();
                cardStyle.ApplyTo(card);
                mapCardBg = card;

                var preview = CombatUIScreenUtility.CreatePanel("Preview", card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(210f, 112f), new Color(0.12f, 0.1f, 0.09f, 1f));
                if (map.PreviewSprite != null)
                {
                    var previewImageObject = new GameObject("PreviewImage", typeof(RectTransform), typeof(Image));
                    previewImageObject.transform.SetParent(preview, false);
                    var previewRect = previewImageObject.GetComponent<RectTransform>();
                    previewRect.anchorMin = Vector2.zero;
                    previewRect.anchorMax = Vector2.one;
                    previewRect.offsetMin = new Vector2(6f, 6f);
                    previewRect.offsetMax = new Vector2(-6f, -6f);
                    var previewImage = previewImageObject.GetComponent<Image>();
                    previewImage.sprite = map.PreviewSprite;
                    previewImage.preserveAspect = true;
                    previewImage.color = Color.white;
                }
                else
                {
                    var previewText = CombatUIScreenUtility.CreateText("PreviewText", preview, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleCenter);
                    previewText.text = "\u5730\u56fe\u56fe\u7247\u9884\u7559";
                    CombatUIScreenUtility.SetTextStyle(previewText, new Color(0.72f, 0.66f, 0.58f), false);
                }

                var name = CombatUIScreenUtility.CreateText("Name", card.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -174f), new Vector2(-30f, 42f), 21, TextAnchor.MiddleCenter);
                name.text = map.DisplayName;
                CombatUIScreenUtility.SetTextStyle(name, new Color(1f, 0.84f, 0.44f), true);

                var description = CombatUIScreenUtility.CreateText("Description", card.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 16, TextAnchor.UpperLeft);
                description.rectTransform.offsetMin = new Vector2(28f, 76f);
                description.rectTransform.offsetMax = new Vector2(-28f, -214f);
                description.text = $"{map.Description}\n\u6218\u6597\u6570\uff1a{map.BattleCount}\n{(map.IsUnlocked ? "\u72b6\u6001\uff1a\u5df2\u89e3\u9501" : $"\u89e3\u9501\uff1a{map.UnlockSummary}")}";
                CombatUIScreenUtility.SetTextStyle(description, new Color(0.88f, 0.8f, 0.66f), false);

                CombatUIScreenUtility.CreateButton(card.rectTransform, map.IsUnlocked ? "\u9009\u62e9" : "\u672a\u89e3\u9501", new Vector2(140f, -316f), () => onSelect?.Invoke(map), map.IsUnlocked);
            }

            CombatUIScreenUtility.CreateButton(adventureBg.rectTransform, "\u8fd4\u56de\u5927\u5385", new Vector2(520f, -604f), () => onBack?.Invoke(), true);
        }
    }

    public sealed partial class TeamSelectionUI
    {
        public Image teamSelectionBg;
        public Image heroCardBg;

        public void Show(IReadOnlyList<CombatantDefinition> heroPool, IReadOnlyList<CombatantDefinition> selectedHeroes, Action<CombatantDefinition> onToggleHero, Action onStartBattle, Action onBack)
        {
            var teamStyle = CombatUIImageStyle.Capture(teamSelectionBg);
            var cardStyle = CombatUIImageStyle.Capture(heroCardBg);
            BuildLayout();
            var root = PrepareRoot();
            var selectedList = selectedHeroes ?? Array.Empty<CombatantDefinition>();
            teamSelectionBg = CombatUIScreenUtility.CreatePanel("TeamSelectionPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 760f), new Color(0.032f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
            teamStyle.ApplyTo(teamSelectionBg);

            var title = CombatUIScreenUtility.CreateText("Title", teamSelectionBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            title.text = "\u961f\u4f0d\u914d\u7f6e";
            CombatUIScreenUtility.SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var subtitle = CombatUIScreenUtility.CreateText("Subtitle", teamSelectionBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -94f), new Vector2(-120f, 36f), 17, TextAnchor.MiddleCenter);
            subtitle.text = $"\u9009\u62e9\u6700\u591a 4 \u540d\u82f1\u96c4\u51fa\u6218\uff08\u5df2\u9009 {selectedList.Count}/4\uff09";
            CombatUIScreenUtility.SetTextStyle(subtitle, new Color(0.82f, 0.72f, 0.54f), false);
            CombatUIScreenUtility.CreateButton(teamSelectionBg.rectTransform, "\u8fd4\u56de\u5730\u56fe", new Vector2(1000f, -70f), () => onBack?.Invoke(), true);

            var pool = heroPool ?? Array.Empty<CombatantDefinition>();
            for (var i = 0; i < pool.Count; i++)
            {
                var hero = pool[i];
                var selected = selectedList.Contains(hero);
                var row = i / 4;
                var column = i % 4;
                var card = CombatUIScreenUtility.CreatePanel($"HeroCard_{i}", teamSelectionBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(155f + column * 288f, -220f - row * 224f), new Vector2(250f, 176f), selected ? new Color(0.16f, 0.105f, 0.055f, 0.96f) : new Color(0.055f, 0.048f, 0.047f, 0.94f)).GetComponent<Image>();
                cardStyle.ApplyTo(card);
                heroCardBg = card;

                var nameText = CombatUIScreenUtility.CreateText("Name", card.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -30f), new Vector2(-28f, 32f), 18, TextAnchor.MiddleCenter);
                nameText.text = selected ? $"[\u5df2\u9009] {hero.displayName}" : hero.displayName;
                CombatUIScreenUtility.SetTextStyle(nameText, selected ? new Color(1f, 0.82f, 0.38f) : new Color(0.95f, 0.84f, 0.65f), true);
                CombatUIScreenUtility.CreatePortrait(card.rectTransform, hero, new Vector2(48f, -8f), new Vector2(68f, 108f), new Vector2(0f, 0f), new Vector2(0f, 1f));

                var stats = CombatUIScreenUtility.CreateText("Stats", card.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft);
                stats.rectTransform.offsetMin = new Vector2(92f, 36f);
                stats.rectTransform.offsetMax = new Vector2(-20f, -48f);
                stats.text = $"\u804c\u80fd {hero.ArchetypeDisplayName}  \u4e13\u7cbe {hero.SpecializationDisplayName}\n\u72b6\u6001 {hero.RecoveryDisplayName}\n\u504f\u597d {hero.PreferredRowDisplayName}\n\u7b49\u7ea7 {hero.Level}  EXP {hero.Experience}/{hero.ExperienceToNextLevel}\n\u751f\u547d {hero.CurrentHealth}/{hero.MaxHealthWithGrowth}\n\u653b\u51fb {hero.AttackWithGrowth}\n\u9632\u5fa1 {hero.DefenseWithGrowth}\n\u901f\u5ea6 {hero.SpeedWithArchetype}";
                CombatUIScreenUtility.SetTextStyle(stats, new Color(0.84f, 0.78f, 0.66f), false);
                CombatUIScreenUtility.CreateButton(card.rectTransform, selected ? "\u53d6\u6d88" : "\u9009\u62e9", new Vector2(125f, -148f), () => onToggleHero?.Invoke(hero), true);
            }

            var lineup = selectedList.Count == 0 ? "\u5f53\u524d\u961f\u4f0d\uff1a\u672a\u9009\u62e9" : "\u5f53\u524d\u961f\u4f0d\uff1a" + string.Join(" / ", selectedList.Select(hero => hero.displayName));
            var lineupText = CombatUIScreenUtility.CreateText("Lineup", teamSelectionBg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 94f), new Vector2(-96f, 40f), 17, TextAnchor.MiddleCenter);
            lineupText.text = lineup;
            CombatUIScreenUtility.SetTextStyle(lineupText, new Color(0.9f, 0.78f, 0.58f), false);
            CombatUIScreenUtility.CreateButton(teamSelectionBg.rectTransform, "\u5f00\u59cb\u6218\u6597", new Vector2(590f, -708f), () => onStartBattle?.Invoke(), selectedList.Count > 0);
        }
    }

    public sealed partial class HeroCodexUI
    {
        public Image heroCodexBg;
        public Image cardBg;
        public Image portraitBg;

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

    public sealed partial class SettingsUI
    {
        public Image settingsBg;
        public Image sliderBg;

        public void Show(Action onBack, Action onSaveGame)
        {
            var settingsStyle = CombatUIImageStyle.Capture(settingsBg);
            var sliderStyle = CombatUIImageStyle.Capture(sliderBg);
            BuildLayout();
            var root = PrepareRoot();
            settingsBg = CombatUIScreenUtility.CreatePanel("SettingsPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 360f), new Color(0.032f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
            settingsStyle.ApplyTo(settingsBg);

            var title = CombatUIScreenUtility.CreateText("Title", settingsBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), new Vector2(-70f, 58f), 28, TextAnchor.MiddleCenter);
            title.text = "\u8bbe\u7f6e";
            CombatUIScreenUtility.SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var volumeLabel = CombatUIScreenUtility.CreateText("VolumeLabel", settingsBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -142f), new Vector2(160f, 34f), 18, TextAnchor.MiddleLeft);
            volumeLabel.text = "\u4e3b\u97f3\u91cf";
            CombatUIScreenUtility.SetTextStyle(volumeLabel, new Color(0.96f, 0.82f, 0.48f), true);
            sliderBg = CombatUIScreenUtility.CreateSlider(settingsBg.rectTransform, "MasterVolume", new Vector2(360f, -142f), AudioListener.volume, value => AudioListener.volume = value).GetComponentInChildren<Image>();
            sliderStyle.ApplyTo(sliderBg);

            var body = CombatUIScreenUtility.CreateText("Body", settingsBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18, TextAnchor.MiddleCenter);
            body.rectTransform.offsetMin = new Vector2(70f, 170f);
            body.rectTransform.offsetMax = new Vector2(-70f, -100f);
            body.text = "\u5176\u4ed6\u8bbe\u7f6e\u9879\u9884\u7559";
            CombatUIScreenUtility.SetTextStyle(body, new Color(0.86f, 0.78f, 0.64f), false);
            CombatUIScreenUtility.CreateButton(settingsBg.rectTransform, "\u4fdd\u5b58\u6e38\u620f", new Vector2(310f, -248f), () => onSaveGame?.Invoke(), true);
            CombatUIScreenUtility.CreateButton(settingsBg.rectTransform, "\u8fd4\u56de", new Vector2(310f, -304f), () => onBack?.Invoke(), true);
        }
    }

    public sealed partial class SaveSlotUI
    {
        public Image saveSlotBg;
        public Image slotCardBg;

        public void Show(string titleText, IReadOnlyList<SaveSlotSummary> slots, bool allowEmptySelection, Action<int> onSelect, Action<int> onDelete, Action onBack)
        {
            var screenStyle = CombatUIImageStyle.Capture(saveSlotBg);
            var cardStyle = CombatUIImageStyle.Capture(slotCardBg);
            BuildLayout();
            var root = PrepareRoot();
            saveSlotBg = CombatUIScreenUtility.CreatePanel("SaveSlotPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 620f), new Color(0.032f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
            screenStyle.ApplyTo(saveSlotBg);

            var title = CombatUIScreenUtility.CreateText("Title", saveSlotBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -54f), new Vector2(-80f, 62f), 30, TextAnchor.MiddleCenter);
            title.text = string.IsNullOrWhiteSpace(titleText) ? "\u5b58\u6863" : titleText;
            CombatUIScreenUtility.SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            var slotList = slots ?? Array.Empty<SaveSlotSummary>();
            for (var i = 0; i < slotList.Count; i++)
            {
                var slot = slotList[i];
                var card = CombatUIScreenUtility.CreatePanel($"SaveSlot_{i}", saveSlotBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f + i * 280f, -332f), new Vector2(240f, 360f), new Color(0.055f, 0.048f, 0.047f, 0.94f)).GetComponent<Image>();
                cardStyle.ApplyTo(card);
                slotCardBg = card;

                var name = CombatUIScreenUtility.CreateText("Name", card.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -42f), new Vector2(-24f, 44f), 20, TextAnchor.MiddleCenter);
                name.text = slot.Title;
                CombatUIScreenUtility.SetTextStyle(name, new Color(1f, 0.84f, 0.44f), true);

                var detail = CombatUIScreenUtility.CreateText("Detail", card.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 16, TextAnchor.UpperLeft);
                detail.rectTransform.offsetMin = new Vector2(20f, 94f);
                detail.rectTransform.offsetMax = new Vector2(-20f, -82f);
                detail.text = slot.HasSave ? slot.Detail : "\u7a7a\u5b58\u6863\u4f4d";
                CombatUIScreenUtility.SetTextStyle(detail, new Color(0.88f, 0.8f, 0.66f), false);

                var buttonLabel = slot.HasSave ? (allowEmptySelection ? "\u8986\u76d6" : "\u8f7d\u5165") : (allowEmptySelection ? "\u4fdd\u5b58\u5230\u6b64" : "\u7a7a");
                var canSelect = slot.HasSave || allowEmptySelection;
                CombatUIScreenUtility.CreateButton(card.rectTransform, buttonLabel, new Vector2(120f, -310f), () => onSelect?.Invoke(slot.SlotIndex), canSelect);
                if (slot.HasSave && onDelete != null)
                {
                    CombatUIScreenUtility.CreateButton(card.rectTransform, "\u5220\u9664", new Vector2(120f, -258f), () => onDelete?.Invoke(slot.SlotIndex), true);
                }
            }

            CombatUIScreenUtility.CreateButton(saveSlotBg.rectTransform, "\u8fd4\u56de", new Vector2(490f, -566f), () => onBack?.Invoke(), true);
        }
    }

    public sealed partial class LevelUpUI
    {
        public Image levelUpBg;
        public Image heroCardBg;
        public Image optionCardBg;

        public void Show(LevelUpPresentation presentation, Action<LevelUpOptionData> onSelect)
        {
            var screenStyle = CombatUIImageStyle.Capture(levelUpBg);
            var heroCardStyle = CombatUIImageStyle.Capture(heroCardBg);
            var optionCardStyle = CombatUIImageStyle.Capture(optionCardBg);
            BuildLayout();
            var root = PrepareRoot();

            var overlay = CombatUIScreenUtility.CreatePanel("LevelUpOverlay", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.72f));
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;

            levelUpBg = CombatUIScreenUtility.CreatePanel("LevelUpPanel", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1260f, 700f), new Color(0.028f, 0.024f, 0.022f, 0.98f)).GetComponent<Image>();
            screenStyle.ApplyTo(levelUpBg);
            var rootPanel = levelUpBg.rectTransform;

            var title = CombatUIScreenUtility.CreateText("Title", rootPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -50f), new Vector2(-90f, 62f), 34, TextAnchor.MiddleCenter);
            title.text = string.IsNullOrWhiteSpace(presentation.Title) ? "\u5347\u7ea7\u9009\u62e9" : presentation.Title;
            CombatUIScreenUtility.SetTextStyle(title, new Color(0.97f, 0.83f, 0.46f), true);

            var subtitle = CombatUIScreenUtility.CreateText("Subtitle", rootPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -98f), new Vector2(-120f, 34f), 18, TextAnchor.MiddleCenter);
            subtitle.text = string.IsNullOrWhiteSpace(presentation.Subtitle) ? "\u9009\u62e9\u4e00\u9879\u6210\u957f\u65b9\u5411" : presentation.Subtitle;
            CombatUIScreenUtility.SetTextStyle(subtitle, new Color(0.82f, 0.72f, 0.54f), false);

            heroCardBg = CombatUIScreenUtility.CreatePanel("HeroCard", rootPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(170f, -390f), new Vector2(280f, 520f), new Color(0.055f, 0.048f, 0.047f, 0.96f)).GetComponent<Image>();
            heroCardStyle.ApplyTo(heroCardBg);
            var heroCard = heroCardBg.rectTransform;

            var heroPortraitFrame = CombatUIScreenUtility.CreatePanel("PortraitFrame", heroCard, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -106f), new Vector2(160f, 190f), new Color(0.1f, 0.08f, 0.07f, 1f)).GetComponent<Image>();
            heroPortraitFrame.sprite = optionCardBg != null ? optionCardBg.sprite : null;
            heroPortraitFrame.type = Image.Type.Sliced;
            var portrait = CombatUIScreenUtility.CreatePortrait(heroPortraitFrame.rectTransform, presentation.Hero, Vector2.zero, new Vector2(136f, 166f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            var heroName = CombatUIScreenUtility.CreateText("HeroName", heroCard, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -228f), new Vector2(-30f, 42f), 24, TextAnchor.MiddleCenter);
            heroName.text = presentation.Hero != null ? presentation.Hero.displayName : "\u89d2\u8272";
            CombatUIScreenUtility.SetTextStyle(heroName, new Color(0.96f, 0.86f, 0.62f), true);

            var heroMeta = CombatUIScreenUtility.CreateText("HeroMeta", heroCard, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -264f), new Vector2(-36f, 56f), 15, TextAnchor.MiddleCenter);
            heroMeta.text = BuildHeroMetaLine(presentation.Hero);
            CombatUIScreenUtility.SetTextStyle(heroMeta, new Color(0.82f, 0.74f, 0.6f), false);

            var heroStats = CombatUIScreenUtility.CreateText("HeroStats", heroCard, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft);
            heroStats.rectTransform.offsetMin = new Vector2(26f, 26f);
            heroStats.rectTransform.offsetMax = new Vector2(-26f, -324f);
            heroStats.text = BuildHeroStatsBlock(presentation.Hero);
            CombatUIScreenUtility.SetTextStyle(heroStats, new Color(0.88f, 0.82f, 0.74f), false);

            var sectionTitle = CombatUIScreenUtility.CreateText("OptionsTitle", rootPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(176f, -158f), new Vector2(-120f, 34f), 18, TextAnchor.MiddleLeft);
            sectionTitle.text = "\u672c\u6b21\u53ef\u9009\u6210\u957f";
            CombatUIScreenUtility.SetTextStyle(sectionTitle, new Color(0.96f, 0.82f, 0.48f), true);

            var options = presentation.Options ?? Array.Empty<LevelUpOptionData>();
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var card = CombatUIScreenUtility.CreatePanel($"OptionCard_{i}", rootPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f + i * 258f, -410f), new Vector2(232f, 516f), new Color(0.058f, 0.047f, 0.04f, 0.97f)).GetComponent<Image>();
                optionCardStyle.ApplyTo(card);
                optionCardBg = card;
                BuildOptionCard(card.rectTransform, option, onSelect);
            }
        }

        private void BuildOptionCard(RectTransform card, LevelUpOptionData option, Action<LevelUpOptionData> onSelect)
        {
            var badge = CombatUIScreenUtility.CreatePanel("TypeBadge", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(152f, 34f), GetOptionBadgeColor(option.OptionType)).GetComponent<Image>();
            badge.type = Image.Type.Sliced;

            var badgeText = CombatUIScreenUtility.CreateText("TypeLabel", badge.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleCenter);
            badgeText.text = GetOptionTypeLabel(option.OptionType);
            CombatUIScreenUtility.SetTextStyle(badgeText, new Color(0.98f, 0.93f, 0.82f), true);

            var title = CombatUIScreenUtility.CreateText("OptionTitle", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -88f), new Vector2(-26f, 42f), 22, TextAnchor.MiddleCenter);
            title.text = option.Title;
            CombatUIScreenUtility.SetTextStyle(title, new Color(0.96f, 0.86f, 0.62f), true);

            var subtitle = CombatUIScreenUtility.CreateText("OptionSubtitle", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -122f), new Vector2(-32f, 34f), 14, TextAnchor.MiddleCenter);
            subtitle.text = option.Subtitle;
            CombatUIScreenUtility.SetTextStyle(subtitle, new Color(0.82f, 0.74f, 0.6f), false);

            var summary = CombatUIScreenUtility.CreateText("OptionSummary", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -188f), new Vector2(-36f, 96f), 16, TextAnchor.UpperLeft);
            summary.rectTransform.offsetMin = new Vector2(20f, -236f);
            summary.rectTransform.offsetMax = new Vector2(-20f, -146f);
            summary.text = option.Summary;
            CombatUIScreenUtility.SetTextStyle(summary, new Color(0.9f, 0.84f, 0.76f), false);

            var tags = CombatUIScreenUtility.CreateText("OptionTags", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -276f), new Vector2(-36f, 72f), 13, TextAnchor.UpperLeft);
            tags.rectTransform.offsetMin = new Vector2(20f, -320f);
            tags.rectTransform.offsetMax = new Vector2(-20f, -246f);
            tags.text = option.Tags != null && option.Tags.Count > 0
                ? string.Join("  /  ", option.Tags)
                : "\u6807\u7b7e\u9884\u7559";
            CombatUIScreenUtility.SetTextStyle(tags, new Color(0.72f, 0.68f, 0.6f), false);

            var detail = CombatUIScreenUtility.CreateText("OptionDetail", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 14, TextAnchor.UpperLeft);
            detail.rectTransform.offsetMin = new Vector2(20f, 88f);
            detail.rectTransform.offsetMax = new Vector2(-20f, -334f);
            detail.text = string.IsNullOrWhiteSpace(option.Detail) ? "\u6548\u679c\u9884\u7559" : option.Detail;
            CombatUIScreenUtility.SetTextStyle(detail, new Color(0.86f, 0.8f, 0.7f), false);

            CombatUIScreenUtility.CreateButton(card, "\u9009\u62e9", new Vector2(116f, -470f), () =>
            {
                gameObject.SetActive(false);
                onSelect?.Invoke(option);
            }, true);
        }

        private static string BuildHeroMetaLine(CombatantDefinition hero)
        {
            if (hero == null)
            {
                return "\u804c\u80fd \u672A\u5B9A\u4E49  /  \u7B49\u7EA7 1";
            }

            return $"{hero.ArchetypeDisplayName}  /  {hero.SpecializationDisplayName}  /  Lv.{hero.Level}";
        }

        private static string BuildHeroStatsBlock(CombatantDefinition hero)
        {
            if (hero == null)
            {
                return "\u751F\u547D --\n\u653B\u51FB --\n\u9632\u5FA1 --\n\u901F\u5EA6 --";
            }

            return
                $"\u504F\u597D\u7AD9\u4F4D\uff1A{hero.PreferredRowDisplayName}\n" +
                $"\u751F\u547D\uff1A{hero.CurrentHealth}/{hero.MaxHealthWithGrowth}\n" +
                $"\u653B\u51FB\uff1A{hero.AttackWithGrowth}\n" +
                $"\u9632\u5FA1\uff1A{hero.DefenseWithGrowth}\n" +
                $"\u901F\u5EA6\uff1A{hero.SpeedWithArchetype}\n" +
                $"\u7ECF\u9A8C\uff1A{hero.Experience}/{hero.ExperienceToNextLevel}";
        }

        private static string GetOptionTypeLabel(LevelUpOptionType optionType)
        {
            switch (optionType)
            {
                case LevelUpOptionType.Specialization:
                    return "\u4E13\u7CBE\u5206\u652F";
                case LevelUpOptionType.Passive:
                    return "\u88AB\u52A8\u6210\u957F";
                case LevelUpOptionType.SkillUpgrade:
                    return "\u6280\u80FD\u5F3A\u5316";
                default:
                    return "\u6210\u957F\u9009\u9879";
            }
        }

        private static Color GetOptionBadgeColor(LevelUpOptionType optionType)
        {
            switch (optionType)
            {
                case LevelUpOptionType.Specialization:
                    return new Color(0.42f, 0.18f, 0.12f, 1f);
                case LevelUpOptionType.Passive:
                    return new Color(0.18f, 0.28f, 0.18f, 1f);
                case LevelUpOptionType.SkillUpgrade:
                    return new Color(0.16f, 0.2f, 0.34f, 1f);
                default:
                    return new Color(0.24f, 0.2f, 0.16f, 1f);
            }
        }
    }

    public sealed partial class PopupUI
    {
        public Image popupBg;

        public void Show(string message, string buttonLabel, Action onContinue)
        {
            var popupStyle = CombatUIImageStyle.Capture(popupBg);
            BuildLayout();
            var root = PrepareRoot();
            var overlay = CombatUIScreenUtility.CreatePanel("PopupOverlay", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.62f));
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            popupBg = CombatUIScreenUtility.CreatePanel("MessagePanel", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(620f, 220f), new Color(0.035f, 0.026f, 0.024f, 0.96f)).GetComponent<Image>();
            popupStyle.ApplyTo(popupBg);

            var text = CombatUIScreenUtility.CreateText("OverlayText", popupBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, -30f), new Vector2(-56f, 92f), 28, TextAnchor.MiddleCenter);
            text.rectTransform.offsetMin = new Vector2(28f, -8f);
            text.rectTransform.offsetMax = new Vector2(-28f, -24f);
            text.text = message;
            CombatUIScreenUtility.SetTextStyle(text, new Color(0.96f, 0.82f, 0.48f), true);

            CombatUIScreenUtility.CreateButton(popupBg.rectTransform, buttonLabel, new Vector2(310f, -166f), () =>
            {
                gameObject.SetActive(false);
                onContinue?.Invoke();
            }, true);
        }

        public void ShowChoice(string message, string leftLabel, Action onLeft, string rightLabel, Action onRight)
        {
            var popupStyle = CombatUIImageStyle.Capture(popupBg);
            BuildLayout();
            var root = PrepareRoot();
            var overlay = CombatUIScreenUtility.CreatePanel("PopupOverlay", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.62f));
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            popupBg = CombatUIScreenUtility.CreatePanel("ChoicePanel", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(760f, 360f), new Color(0.035f, 0.026f, 0.024f, 0.96f)).GetComponent<Image>();
            popupStyle.ApplyTo(popupBg);

            var text = CombatUIScreenUtility.CreateText("ChoiceText", popupBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, -30f), new Vector2(-56f, 208f), 24, TextAnchor.UpperLeft);
            text.rectTransform.offsetMin = new Vector2(32f, 90f);
            text.rectTransform.offsetMax = new Vector2(-32f, -28f);
            text.text = message;
            CombatUIScreenUtility.SetTextStyle(text, new Color(0.96f, 0.82f, 0.48f), false);

            CombatUIScreenUtility.CreateButton(popupBg.rectTransform, leftLabel, new Vector2(210f, -300f), () =>
            {
                gameObject.SetActive(false);
                onLeft?.Invoke();
            }, true);

            CombatUIScreenUtility.CreateButton(popupBg.rectTransform, rightLabel, new Vector2(550f, -300f), () =>
            {
                gameObject.SetActive(false);
                onRight?.Invoke();
            }, true);
        }
    }

    public sealed partial class BattleHudUI
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

        public void Build()
        {
            if (!TryBindExistingLayout())
            {
                Debug.LogError("BattleHudUI prefab layout is incomplete. Runtime layout rebuilding is disabled; repair the prefab instead.", this);
                return;
            }
        }

        public void RebuildLayoutFromCode(Sprite panelSprite)
        {
            var rootStyle = CombatUIImageStyle.Capture(battleHudBg);
            var logStyle = CombatUIImageStyle.Capture(logFrameBg);
            var skillStyle = CombatUIImageStyle.Capture(skillPanelBg);
            var targetStyle = CombatUIImageStyle.Capture(targetPanelBg);
            BuildLayout();
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
            logTitleText.text = "\u6218\u6597\u8bb0\u5f55";
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
            skillTitleText.text = "\u6280\u80fd";
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
            targetTitleText.text = "\u89d2\u8272\u4fe1\u606f";
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
            infoText.text = "\u70b9\u51fb\u89d2\u8272\u67e5\u770b\u5c5e\u6027";
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

    public sealed partial class SkillDescriptionUI
    {
        private const string SkillDescriptionPanelPrefabPath = CombatUIPaths.SkillDescriptionPanelPrefabPath;
        private static readonly Vector2 TooltipSize = new Vector2(460f, 126f);
        private static readonly Vector2 TooltipOffset = new Vector2(0f, 88f);

        [SerializeField] private SkillDescriptionPanelUI panelPrefab;
        [SerializeField] private SkillDescriptionPanelUI panelInstance;
        [SerializeField] private Text skillDescriptionText;

        public Image skillDescriptionBg;
        public Text SkillDescriptionText => skillDescriptionText;

        public void Build()
        {
            BuildLayout();
            var root = PrepareRoot();
            ConfigureTooltipRect();
            CreateOrBindPanel(root);
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

    internal readonly struct CombatUIImageStyle
    {
        private readonly bool hasImage;
        private readonly Sprite sprite;
        private readonly Color color;
        private readonly Material material;
        private readonly Image.Type type;
        private readonly bool preserveAspect;

        private CombatUIImageStyle(Image image)
        {
            hasImage = image != null;
            sprite = image != null ? image.sprite : null;
            color = image != null ? image.color : Color.white;
            material = image != null ? image.material : null;
            type = image != null ? image.type : Image.Type.Simple;
            preserveAspect = image != null && image.preserveAspect;
        }

        public static CombatUIImageStyle Capture(Image image)
        {
            return new CombatUIImageStyle(image);
        }

        public void ApplyTo(Image image)
        {
            if (!hasImage || image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.color = color;
            image.material = material;
            image.type = type;
            image.preserveAspect = preserveAspect;
        }
    }

    internal static class CombatUIScreenUtility
    {
        public static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        public static void Stretch(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static RectTransform CreatePanel(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
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

        public static Text CreateText(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, int fontSize, TextAnchor alignment)
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

        public static void ApplyPreferredFonts(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var preferredFont = ChineseFontProvider.LegacyFont;
            if (preferredFont == null)
            {
                return;
            }

            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                if (text == null)
                {
                    continue;
                }

                text.font = preferredFont;
            }
        }

        public static Button CreateButton(RectTransform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action, bool interactable)
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

            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                CombatAudio.Instance.PlayUiClick();
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
            return button;
        }

        public static Slider CreateSlider(RectTransform parent, string name, Vector2 anchoredPosition, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            var rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(320f, 28f);

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(sliderObject.transform, false);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(0f, 8f);
            background.GetComponent<Image>().color = new Color(0.12f, 0.095f, 0.075f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(6f, 0f);
            fillAreaRect.offsetMax = new Vector2(-6f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(1f, 0.5f);
            fillRect.sizeDelta = new Vector2(0f, 8f);
            fill.GetComponent<Image>().color = new Color(0.78f, 0.42f, 0.18f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObject.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(8f, 0f);
            handleAreaRect.offsetMax = new Vector2(-8f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(22f, 22f);
            handle.GetComponent<Image>().color = new Color(0.96f, 0.82f, 0.48f, 1f);

            var slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(value);
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        public static Image CreatePortrait(RectTransform parent, CombatantDefinition hero, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 anchorMin, Vector2 anchorMax)
        {
            var portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitObject.transform.SetParent(parent, false);
            var portraitRect = portraitObject.GetComponent<RectTransform>();
            portraitRect.anchorMin = anchorMin;
            portraitRect.anchorMax = anchorMax;
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = anchoredPosition;
            portraitRect.sizeDelta = sizeDelta;

            var portraitImage = portraitObject.GetComponent<Image>();
            portraitImage.sprite = ResolveHeroPortrait(hero);
            portraitImage.color = portraitImage.sprite != null ? Color.white : new Color(0.18f, 0.14f, 0.12f, 1f);
            portraitImage.preserveAspect = true;
            return portraitImage;
        }

        public static void SetTextStyle(Text text, Color color, bool bold)
        {
            text.color = color;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        }

        private static Sprite ResolveHeroPortrait(CombatantDefinition hero)
        {
            if (hero == null)
            {
                return null;
            }

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
    }

#if UNITY_EDITOR
    public static class CombatUIScreenPrefabBuilder
    {
        [InitializeOnLoadMethod]
        private static void AutoCreateMissingScreenUIPrefabs()
        {
            EditorApplication.delayCall += () =>
            {
                if (!AssetDatabase.IsValidFolder("Assets/UI/Prefabs") ||
                    !AssetDatabase.LoadAssetAtPath<StartMenuUI>(CombatUIPaths.StartMenuPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<LobbyUI>(CombatUIPaths.LobbyPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<TavernUI>(CombatUIPaths.TavernPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<AdventureMapUI>(CombatUIPaths.AdventurePrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<TeamSelectionUI>(CombatUIPaths.TeamSelectionPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<HeroCodexUI>(CombatUIPaths.HeroCodexPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<SettingsUI>(CombatUIPaths.SettingsPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<SaveSlotUI>(CombatUIPaths.SaveSlotPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<PopupUI>(CombatUIPaths.PopupPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<BattleHudUI>(CombatUIPaths.BattleHudPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<SkillDescriptionUI>(CombatUIPaths.SkillDescriptionPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<SkillDescriptionPanelUI>(CombatUIPaths.SkillDescriptionPanelPrefabPath))
                {
                    CreateMissingScreenUIPrefabs();
                }
            };
        }

        [MenuItem("Clockwork Wasteland/Build/Create Missing Screen UI Prefabs")]
        public static void CreateMissingScreenUIPrefabs()
        {
            EnsureFolder("Assets", "UI");
            EnsureFolder("Assets/UI", "Prefabs");
            EnsureFolder(CombatUIPaths.PrefabRootPath, "Screens");
            EnsureFolder(CombatUIPaths.PrefabRootPath, "HUD");
            EnsureFolder(CombatUIPaths.PrefabRootPath, "Popups");
            CreatePrefab<StartMenuUI>(CombatUIPaths.StartMenuPrefabPath, "StartMenuUI", overwriteExisting: false);
            CreatePrefab<LobbyUI>(CombatUIPaths.LobbyPrefabPath, "LobbyUI", overwriteExisting: false);
            CreatePrefab<TavernUI>(CombatUIPaths.TavernPrefabPath, "TavernUI", overwriteExisting: false);
            CreatePrefab<AdventureMapUI>(CombatUIPaths.AdventurePrefabPath, "AdventureMapUI", overwriteExisting: false);
            CreatePrefab<TeamSelectionUI>(CombatUIPaths.TeamSelectionPrefabPath, "TeamSelectionUI", overwriteExisting: false);
            CreatePrefab<HeroCodexUI>(CombatUIPaths.HeroCodexPrefabPath, "HeroCodexUI", overwriteExisting: false);
            CreatePrefab<SettingsUI>(CombatUIPaths.SettingsPrefabPath, "SettingsUI", overwriteExisting: false);
            CreatePrefab<SaveSlotUI>(CombatUIPaths.SaveSlotPrefabPath, "SaveSlotUI", overwriteExisting: false);
            CreatePrefab<LevelUpUI>(CombatUIPaths.LevelUpPrefabPath, "LevelUpUI", overwriteExisting: false);
            CreatePrefab<PopupUI>(CombatUIPaths.PopupPrefabPath, "PopupUI", overwriteExisting: false);
            CreatePrefab<BattleHudUI>(CombatUIPaths.BattleHudPrefabPath, "BattleHudUI", overwriteExisting: false);
            CreateSkillDescriptionPanelPrefab(overwriteExisting: false);
            CreatePrefab<SkillDescriptionUI>(CombatUIPaths.SkillDescriptionPrefabPath, "SkillDescriptionUI", overwriteExisting: false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Clockwork Wasteland/Build/Create Screen UI Prefabs")]
        public static void CreateScreenUIPrefabs()
        {
            EnsureFolder("Assets", "UI");
            EnsureFolder("Assets/UI", "Prefabs");
            EnsureFolder(CombatUIPaths.PrefabRootPath, "Screens");
            EnsureFolder(CombatUIPaths.PrefabRootPath, "HUD");
            EnsureFolder(CombatUIPaths.PrefabRootPath, "Popups");
            CreatePrefab<StartMenuUI>(CombatUIPaths.StartMenuPrefabPath, "StartMenuUI", overwriteExisting: true);
            CreatePrefab<LobbyUI>(CombatUIPaths.LobbyPrefabPath, "LobbyUI", overwriteExisting: true);
            CreatePrefab<TavernUI>(CombatUIPaths.TavernPrefabPath, "TavernUI", overwriteExisting: true);
            CreatePrefab<AdventureMapUI>(CombatUIPaths.AdventurePrefabPath, "AdventureMapUI", overwriteExisting: true);
            CreatePrefab<TeamSelectionUI>(CombatUIPaths.TeamSelectionPrefabPath, "TeamSelectionUI", overwriteExisting: true);
            CreatePrefab<HeroCodexUI>(CombatUIPaths.HeroCodexPrefabPath, "HeroCodexUI", overwriteExisting: true);
            CreatePrefab<SettingsUI>(CombatUIPaths.SettingsPrefabPath, "SettingsUI", overwriteExisting: true);
            CreatePrefab<SaveSlotUI>(CombatUIPaths.SaveSlotPrefabPath, "SaveSlotUI", overwriteExisting: true);
            CreatePrefab<LevelUpUI>(CombatUIPaths.LevelUpPrefabPath, "LevelUpUI", overwriteExisting: true);
            CreatePrefab<PopupUI>(CombatUIPaths.PopupPrefabPath, "PopupUI", overwriteExisting: true);
            CreatePrefab<BattleHudUI>(CombatUIPaths.BattleHudPrefabPath, "BattleHudUI", overwriteExisting: true);
            CreateSkillDescriptionPanelPrefab(overwriteExisting: true);
            CreatePrefab<SkillDescriptionUI>(CombatUIPaths.SkillDescriptionPrefabPath, "SkillDescriptionUI", overwriteExisting: true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Clockwork Wasteland/Build/Rebuild Battle HUD Prefab")]
        public static void RebuildBattleHudPrefab()
        {
            EnsureFolder("Assets", "UI");
            EnsureFolder("Assets/UI", "Prefabs");
            EnsureFolder(CombatUIPaths.PrefabRootPath, "HUD");
            CreatePrefab<BattleHudUI>(CombatUIPaths.BattleHudPrefabPath, "BattleHudUI", overwriteExisting: true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Clockwork Wasteland/Build/Rebuild Level Up Prefab")]
        public static void RebuildLevelUpPrefab()
        {
            EnsureFolder("Assets", "UI");
            EnsureFolder("Assets/UI", "Prefabs");
            EnsureFolder(CombatUIPaths.PrefabRootPath, "Screens");
            CreatePrefab<LevelUpUI>(CombatUIPaths.LevelUpPrefabPath, "LevelUpUI", overwriteExisting: true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Clockwork Wasteland/Build/Rebuild Hero Codex Prefab")]
        public static void RebuildHeroCodexPrefab()
        {
            EnsureFolder("Assets", "UI");
            EnsureFolder("Assets/UI", "Prefabs");
            EnsureFolder(CombatUIPaths.PrefabRootPath, "Screens");
            CreatePrefab<HeroCodexUI>(CombatUIPaths.HeroCodexPrefabPath, "HeroCodexUI", overwriteExisting: true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreatePrefab<T>(string path, string prefabName, bool overwriteExisting) where T : CombatUIScreen
        {
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                return;
            }

            var root = new GameObject(prefabName, typeof(RectTransform), typeof(T));
            try
            {
                CombatUIScreenUtility.Stretch(root.transform as RectTransform);
                BuildPreviewLayout(root.GetComponent<T>());
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateSkillDescriptionPanelPrefab(bool overwriteExisting)
        {
            var path = CombatUIPaths.SkillDescriptionPanelPrefabPath;
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<SkillDescriptionPanelUI>(path) != null)
            {
                return;
            }

            var root = new GameObject("SkillDescriptionPanelUI", typeof(RectTransform), typeof(SkillDescriptionPanelUI));
            try
            {
                var rect = root.transform as RectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                root.GetComponent<SkillDescriptionPanelUI>().Build();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildPreviewLayout(CombatUIScreen screen)
        {
            if (screen is StartMenuUI startMenu)
            {
                startMenu.Show(true, null, null, null, null, null);
                return;
            }

            if (screen is LobbyUI lobby)
            {
                lobby.Show(1200, null, null, null, null, null);
                return;
            }

            if (screen is TavernUI tavern)
            {
                var hero = CreatePreviewHero();
                try
                {
                    tavern.Show(new[] { hero, hero, hero }, 1200, null, null);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(hero);
                }

                return;
            }

            if (screen is AdventureMapUI adventureMap)
            {
                var previewMaps = new[]
                {
                    CreatePreviewAdventureMap("preview_1", "\u5e9f\u571f\u8fb9\u5883", "\u5730\u56fe\u63cf\u8ff0\u9884\u7559"),
                    CreatePreviewAdventureMap("preview_2", "\u94a2\u94c1\u5de5\u574a", "\u5730\u56fe\u63cf\u8ff0\u9884\u7559"),
                    CreatePreviewAdventureMap("preview_3", "\u7070\u70ec\u5730\u7a9f", "\u5730\u56fe\u63cf\u8ff0\u9884\u7559")
                };
                adventureMap.Show(new[]
                {
                    new AdventureMapOption(previewMaps[0], true),
                    new AdventureMapOption(previewMaps[1], true),
                    new AdventureMapOption(previewMaps[2], false)
                }, null, null);
                return;
            }

            if (screen is TeamSelectionUI teamSelection)
            {
                var hero = CreatePreviewHero();
                try
                {
                    teamSelection.Show(new[] { hero, hero, hero, hero }, new[] { hero }, null, null, null);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(hero);
                }

                return;
            }

            if (screen is HeroCodexUI heroCodex)
            {
                heroCodex.RebuildLayoutFromCode(null);
                return;
            }

            if (screen is SettingsUI settings)
            {
                settings.Show(null, null);
                return;
            }

            if (screen is SaveSlotUI saveSlot)
            {
                saveSlot.Show("\u5b58\u6863\u9884\u89c8", new[]
                {
                    new SaveSlotSummary(0, true, "\u5b58\u6863 1", "\u91d1\u5e01 1200  \u5df2\u89e3\u9501\u82f1\u96c4 4\n\u4fdd\u5b58\u65f6\u95f4 2026-05-23 12:00:00"),
                    new SaveSlotSummary(1, false, "\u5b58\u6863 2", "\u7a7a"),
                    new SaveSlotSummary(2, false, "\u5b58\u6863 3", "\u7a7a")
                }, true, null, null, null);
                return;
            }

            if (screen is LevelUpUI levelUp)
            {
                var hero = CreatePreviewHero();
                try
                {
                    levelUp.Show(
                        new LevelUpPresentation(
                            hero,
                            "\u5347\u7ea7\u9009\u62e9",
                            "\u5f53\u524d\u7b49\u7ea7 Lv.2  \u8bf7\u9009\u62e9\u672c\u6b21\u7684\u6210\u957f\u65b9\u5411",
                            new[]
                            {
                                new LevelUpOptionData("preview_specialization_1", LevelUpOptionType.Specialization, "\u58C1\u5792", "\u7A33\u9635 / \u627F\u4F24 / \u62A4\u536B", "\u7ACB\u8DB3\u524D\u6392\uff0c\u4E13\u6CE8\u627F\u62C5\u538B\u529B\u5E76\u4FDD\u62A4\u961F\u53CB\u3002", "\u62A4\u536B\u76F8\u90BB\u5355\u4F4D\uff0C\u88AB\u653B\u51FB\u65F6\u53EF\u83B7\u5F97\u989D\u5916\u6536\u76CA\u3002", new[] { "\u524D\u6392", "\u62A4\u536B", "\u627F\u4F24" }, specialization: CombatSpecialization.Bastion),
                                new LevelUpOptionData("preview_specialization_2", LevelUpOptionType.Specialization, "\u54E8\u5175", "\u538B\u5236 / \u7834\u9635 / \u53CD\u5236", "\u901A\u8FC7\u649E\u51FB\u3001\u538B\u5236\u548C\u53CD\u51FB\u6253\u4E71\u654C\u9635\u3002", "\u5BF9\u591A\u76EE\u6807\u7684\u538B\u5236\u4E0E\u9635\u578B\u6270\u4E71\u80FD\u529B\u66F4\u5F3A\u3002", new[] { "\u9635\u578B", "\u538B\u5236", "\u53CD\u5236" }, specialization: CombatSpecialization.Sentinel),
                                new LevelUpOptionData("preview_passive", LevelUpOptionType.Passive, "\u9884\u7559\u88AB\u52A8", "\u88AB\u52A8 / \u63A5\u53E3", "\u7B2C\u4E00\u7248\u5148\u63A5\u901A\u4E13\u7CBE\u5206\u652F\uff0C\u88AB\u52A8\u5361\u4F4D\u9884\u7559\u5728\u8FD9\u91CC\u3002", "\u540E\u7EED 3/5/7/9 \u7EA7\u7684\u88AB\u52A8\u6210\u957F\u3001\u6280\u80FD\u5F3A\u5316\u90FD\u8D70\u540C\u4E00\u5957\u754C\u9762\u3002", new[] { "\u63A5\u53E3", "\u88AB\u52A8", "\u540E\u7EED" })
                            }),
                        null);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(hero);
                }

                return;
            }

            if (screen is PopupUI popup)
            {
                popup.Show("\u901a\u7528\u5f39\u7a97\u9884\u89c8", "\u786e\u5b9a", null);
                return;
            }

            if (screen is BattleHudUI battleHud)
            {
                battleHud.RebuildLayoutFromCode(null);
                return;
            }

            if (screen is SkillDescriptionUI skillDescription)
            {
                skillDescription.Build();
            }
        }

        private static CombatantDefinition CreatePreviewHero()
        {
            var hero = ScriptableObject.CreateInstance<CombatantDefinition>();
            hero.hideFlags = HideFlags.HideAndDontSave;
            hero.characterId = "preview_hero";
            hero.displayName = "\u82f1\u96c4\u5360\u4f4d";
            hero.isHero = true;
            hero.isUnlocked = true;
            hero.recruitPrice = 500;
            hero.maxHealth = 32;
            hero.attack = 8;
            hero.defense = 2;
            hero.speed = 5;
            hero.currentLevel = 1;
            hero.currentExperience = 0;
            hero.currentHealth = 32;
            return hero;
        }

        private static AdventureMapData CreatePreviewAdventureMap(string mapId, string displayName, string description)
        {
            var map = ScriptableObject.CreateInstance<AdventureMapData>();
            map.hideFlags = HideFlags.HideAndDontSave;
            map.mapId = mapId;
            map.displayName = displayName;
            map.description = description;
            map.unlockType = AdventureMapUnlockType.Default;
            map.unlockDescription = "\u9884\u89c8\u7528\u5730\u56fe";
            map.battles = new List<AdventureBattleConfig>
            {
                new AdventureBattleConfig
                {
                    battleId = "preview_battle_01",
                    displayName = "\u9884\u89c8\u6218\u6597",
                    description = "\u9884\u89c8\u7528"
                }
            };
            return map;
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
#endif
}
