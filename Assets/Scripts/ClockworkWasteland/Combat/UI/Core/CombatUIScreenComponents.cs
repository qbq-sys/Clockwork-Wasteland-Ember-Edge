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
        public const string RecoveryWardPrefabPath = ScreenPrefabRootPath + "/RecoveryWardUI.prefab";
        public const string LevelUpPrefabPath = ScreenPrefabRootPath + "/LevelUpUI.prefab";
        public const string RewardScreenPrefabPath = ScreenPrefabRootPath + "/RewardScreenUI.prefab";
        public const string ShopPrefabPath = ScreenPrefabRootPath + "/ShopUI.prefab";
        public const string InventoryPrefabPath = ScreenPrefabRootPath + "/InventoryUI.prefab";
        public const string RouteMapPrefabPath = ScreenPrefabRootPath + "/RouteMapUI.prefab";
        public const string RestNodePrefabPath = ScreenPrefabRootPath + "/RestNodeUI.prefab";
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
        [SerializeField] private RecoveryWardUI recoveryWardPrefab;
        [SerializeField] private LevelUpUI levelUpPrefab;
        [SerializeField] private RewardScreenUI rewardScreenPrefab;
        [SerializeField] private ShopUI shopPrefab;
        [SerializeField] private InventoryUI inventoryPrefab;
        [SerializeField] private RouteMapUI routeMapPrefab;
        [SerializeField] private RestNodeUI restNodePrefab;
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

        public void ShowLobby(int currentGold, Action onOpenTavern, Action onOpenAdventure, Action onOpenRecoveryWard, Action onOpenHeroCodex, Action onOpenShop, Action onOpenInventory, Action onOpenSettings, Action onBackToStartMenu)
        {
            var screen = ShowScreen(lobbyPrefab, CombatUIPaths.LobbyPrefabPath);
            screen.Show(currentGold, onOpenTavern, onOpenAdventure, onOpenRecoveryWard, onOpenHeroCodex, onOpenShop, onOpenInventory, onOpenSettings, onBackToStartMenu);
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

        public void ShowRecoveryWard(IReadOnlyList<CombatantDefinition> heroes, int currentGold, int treatmentCost, Action<CombatantDefinition> onTreat, Action onBack)
        {
            var screen = ShowScreen(recoveryWardPrefab, CombatUIPaths.RecoveryWardPrefabPath);
            screen.Show(heroes, currentGold, treatmentCost, onTreat, onBack);
        }

        public void ShowLevelUp(LevelUpPresentation presentation, Action<LevelUpOptionData> onSelect)
        {
            var screen = ShowScreen(levelUpPrefab, CombatUIPaths.LevelUpPrefabPath);
            screen.Show(presentation, onSelect);
        }

        public void ShowRewardScreen(int goldGained, int totalGold, IReadOnlyList<BattleRewardResult> results, Action onContinue)
        {
            var screen = ShowScreen(rewardScreenPrefab, CombatUIPaths.RewardScreenPrefabPath);
            screen.Show(goldGained, totalGold, results, onContinue);
        }

        public void ShowShop(IReadOnlyList<InventoryItemData> shopItems, int currentGold, IReadOnlyList<InventoryItemStack> inventory, Action<InventoryItemData> onBuy, Action onBack)
        {
            var screen = ShowScreen(shopPrefab, CombatUIPaths.ShopPrefabPath);
            screen.Show(shopItems, currentGold, inventory, onBuy, onBack);
        }

        public void ShowInventory(IReadOnlyList<InventoryItemStack> inventory, IReadOnlyList<CombatantDefinition> heroes, Action<InventoryItemData, CombatantDefinition> onUse, Action onBack)
        {
            var screen = ShowScreen(inventoryPrefab, CombatUIPaths.InventoryPrefabPath);
            screen.Show(inventory, heroes, onUse, onBack);
        }

        public void ShowRouteMap(int step, int totalSteps, IReadOnlyList<MapNodeOption> options, Action<MapNodeOption> onSelect)
        {
            var screen = ShowScreen(routeMapPrefab, CombatUIPaths.RouteMapPrefabPath);
            screen.Show(step, totalSteps, options, onSelect);
        }

        public void ShowRestNode(IReadOnlyList<CombatantDefinition> heroes, Action<CombatantDefinition> onSelectHero)
        {
            var screen = ShowScreen(restNodePrefab, CombatUIPaths.RestNodePrefabPath);
            screen.Show(heroes, onSelectHero);
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
                    !AssetDatabase.LoadAssetAtPath<RecoveryWardUI>(CombatUIPaths.RecoveryWardPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<RewardScreenUI>(CombatUIPaths.RewardScreenPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<ShopUI>(CombatUIPaths.ShopPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<InventoryUI>(CombatUIPaths.InventoryPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<RouteMapUI>(CombatUIPaths.RouteMapPrefabPath) ||
                    !AssetDatabase.LoadAssetAtPath<RestNodeUI>(CombatUIPaths.RestNodePrefabPath) ||
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
            CreatePrefab<RecoveryWardUI>(CombatUIPaths.RecoveryWardPrefabPath, "RecoveryWardUI", overwriteExisting: false);
            CreatePrefab<LevelUpUI>(CombatUIPaths.LevelUpPrefabPath, "LevelUpUI", overwriteExisting: false);
            CreatePrefab<RewardScreenUI>(CombatUIPaths.RewardScreenPrefabPath, "RewardScreenUI", overwriteExisting: false);
            CreatePrefab<ShopUI>(CombatUIPaths.ShopPrefabPath, "ShopUI", overwriteExisting: false);
            CreatePrefab<InventoryUI>(CombatUIPaths.InventoryPrefabPath, "InventoryUI", overwriteExisting: false);
            CreatePrefab<RouteMapUI>(CombatUIPaths.RouteMapPrefabPath, "RouteMapUI", overwriteExisting: false);
            CreatePrefab<RestNodeUI>(CombatUIPaths.RestNodePrefabPath, "RestNodeUI", overwriteExisting: false);
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
            CreatePrefab<RecoveryWardUI>(CombatUIPaths.RecoveryWardPrefabPath, "RecoveryWardUI", overwriteExisting: true);
            CreatePrefab<LevelUpUI>(CombatUIPaths.LevelUpPrefabPath, "LevelUpUI", overwriteExisting: true);
            CreatePrefab<RewardScreenUI>(CombatUIPaths.RewardScreenPrefabPath, "RewardScreenUI", overwriteExisting: true);
            CreatePrefab<ShopUI>(CombatUIPaths.ShopPrefabPath, "ShopUI", overwriteExisting: true);
            CreatePrefab<InventoryUI>(CombatUIPaths.InventoryPrefabPath, "InventoryUI", overwriteExisting: true);
            CreatePrefab<RouteMapUI>(CombatUIPaths.RouteMapPrefabPath, "RouteMapUI", overwriteExisting: true);
            CreatePrefab<RestNodeUI>(CombatUIPaths.RestNodePrefabPath, "RestNodeUI", overwriteExisting: true);
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

        [MenuItem("Clockwork Wasteland/Build/Rebuild Recovery Ward Prefab")]
        public static void RebuildRecoveryWardPrefab()
        {
            EnsureFolder("Assets", "UI");
            EnsureFolder("Assets/UI", "Prefabs");
            EnsureFolder(CombatUIPaths.PrefabRootPath, "Screens");
            CreatePrefab<RecoveryWardUI>(CombatUIPaths.RecoveryWardPrefabPath, "RecoveryWardUI", overwriteExisting: true);
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
                lobby.Show(1200, null, null, null, null, null, null, null, null);
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

            if (screen is RecoveryWardUI recoveryWard)
            {
                var hero = CreatePreviewHero();
                hero.isUnlocked = true;
                hero.currentHealth = 12;
                hero.recoveryState = HeroRecoveryState.Wounded;
                hero.recoveryBattlesRemaining = 2;
                try
                {
                    recoveryWard.RebuildLayoutFromCode(null);
                    recoveryWard.Show(new[] { hero }, 1200, 150, null, null);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(hero);
                }

                return;
            }

            if (screen is RewardScreenUI rewardScreen)
            {
                rewardScreen.RebuildLayoutFromCode(null);
                return;
            }

            if (screen is ShopUI shop)
            {
                shop.RebuildLayoutFromCode(null);
                return;
            }

            if (screen is InventoryUI inventory)
            {
                inventory.RebuildLayoutFromCode(null);
                return;
            }

            if (screen is RouteMapUI routeMap)
            {
                routeMap.RebuildLayoutFromCode(null);
                return;
            }

            if (screen is RestNodeUI restNode)
            {
                restNode.RebuildLayoutFromCode(null);
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

