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
    public sealed partial class UIManager
    {
        private const string LobbyPrefabPath = "Assets/UI/Prefabs/LobbyUI.prefab";
        private const string TavernPrefabPath = "Assets/UI/Prefabs/TavernUI.prefab";
        private const string AdventurePrefabPath = "Assets/UI/Prefabs/AdventureMapUI.prefab";
        private const string TeamSelectionPrefabPath = "Assets/UI/Prefabs/TeamSelectionUI.prefab";
        private const string HeroCodexPrefabPath = "Assets/UI/Prefabs/HeroCodexUI.prefab";
        private const string SettingsPrefabPath = "Assets/UI/Prefabs/SettingsUI.prefab";
        private const string PopupPrefabPath = "Assets/UI/Prefabs/PopupUI.prefab";
        private const string BattleHudPrefabPath = "Assets/UI/Prefabs/BattleHudUI.prefab";
        private const string SkillDescriptionPrefabPath = "Assets/UI/Prefabs/SkillDescriptionUI.prefab";

        [SerializeField] private LobbyUI lobbyPrefab;
        [SerializeField] private TavernUI tavernPrefab;
        [SerializeField] private AdventureMapUI adventureMapPrefab;
        [SerializeField] private TeamSelectionUI teamSelectionPrefab;
        [SerializeField] private HeroCodexUI heroCodexPrefab;
        [SerializeField] private SettingsUI settingsPrefab;
        [SerializeField] private PopupUI popupPrefab;
        [SerializeField] private BattleHudUI battleHudPrefab;
        [SerializeField] private SkillDescriptionUI skillDescriptionPrefab;

        private readonly Dictionary<Type, CombatUIScreen> cachedScreens = new Dictionary<Type, CombatUIScreen>();
        private PopupUI popupInstance;
        private RectTransform screenRoot;

        public static UIManager Instance { get; private set; }

        public static UIManager Ensure(RectTransform parent)
        {
            if (Instance == null)
            {
                var managerObject = new GameObject("UIManager", typeof(RectTransform), typeof(UIManager));
                Instance = managerObject.GetComponent<UIManager>();
            }

            Instance.Initialize(parent);
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
            screenRoot = transform as RectTransform;
        }

        public void Initialize(RectTransform parent)
        {
            screenRoot = transform as RectTransform;
            if (screenRoot == null)
            {
                return;
            }

            if (parent != null && transform.parent != parent)
            {
                transform.SetParent(parent, false);
            }

            CombatUIScreenUtility.Stretch(screenRoot);
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

        public void ShowLobby(int currentGold, Action onOpenTavern, Action onOpenAdventure, Action onOpenHeroCodex, Action onOpenSettings, Action onQuit)
        {
            var screen = ShowScreen(lobbyPrefab, LobbyPrefabPath);
            screen.Show(currentGold, onOpenTavern, onOpenAdventure, onOpenHeroCodex, onOpenSettings, onQuit);
        }

        public void ShowTavern(IReadOnlyList<CombatantDefinition> recruitableHeroes, int currentGold, Action<CombatantDefinition> onRecruit, Action onBack)
        {
            var screen = ShowScreen(tavernPrefab, TavernPrefabPath);
            screen.Show(recruitableHeroes, currentGold, onRecruit, onBack);
        }

        public void ShowAdventureMap(IReadOnlyList<AdventureMapOption> maps, Action<AdventureMapOption> onSelect, Action onBack)
        {
            var screen = ShowScreen(adventureMapPrefab, AdventurePrefabPath);
            screen.Show(maps, onSelect, onBack);
        }

        public void ShowTeamSelection(IReadOnlyList<CombatantDefinition> heroPool, IReadOnlyList<CombatantDefinition> selectedHeroes, Action<CombatantDefinition> onToggleHero, Action onStartBattle, Action onBack)
        {
            var screen = ShowScreen(teamSelectionPrefab, TeamSelectionPrefabPath);
            screen.Show(heroPool, selectedHeroes, onToggleHero, onStartBattle, onBack);
        }

        public void ShowHeroCodex(IReadOnlyList<CombatantDefinition> heroPool, Action onBack)
        {
            var screen = ShowScreen(heroCodexPrefab, HeroCodexPrefabPath);
            screen.Show(heroPool, onBack);
        }

        public void ShowSettings(Action onBack)
        {
            var screen = ShowScreen(settingsPrefab, SettingsPrefabPath);
            screen.Show(onBack);
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
            return GetOrCreateScreen(battleHudPrefab, BattleHudPrefabPath);
        }

        public SkillDescriptionUI GetSkillDescription()
        {
            return GetOrCreateScreen(skillDescriptionPrefab, SkillDescriptionPrefabPath);
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
            if (source != null)
            {
                instance = Instantiate(source, screenRoot);
            }
            else
            {
                var screenObject = new GameObject(type.Name, typeof(RectTransform), type);
                screenObject.transform.SetParent(screenRoot, false);
                instance = screenObject.GetComponent<T>();
            }

            CombatUIScreenUtility.Stretch(instance.transform as RectTransform);
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

            popupInstance = GetOrCreateScreen(popupPrefab, PopupPrefabPath);
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

    public sealed partial class LobbyUI
    {
        public Image lobbyBg;
        public Image heroShowcaseBg;
        public Image cardBg;

        public void Show(int currentGold, Action onOpenTavern, Action onOpenAdventure, Action onOpenHeroCodex, Action onOpenSettings, Action onQuit)
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

            CombatUIScreenUtility.CreateButton(lobbyBg.rectTransform, "\u9152\u9986", new Vector2(470f, -928f), () => onOpenTavern?.Invoke(), true);
            CombatUIScreenUtility.CreateButton(lobbyBg.rectTransform, "\u5192\u9669", new Vector2(650f, -928f), () => onOpenAdventure?.Invoke(), true);
            CombatUIScreenUtility.CreateButton(lobbyBg.rectTransform, "\u82f1\u96c4\u56fe\u9274", new Vector2(830f, -928f), () => onOpenHeroCodex?.Invoke(), true);
            CombatUIScreenUtility.CreateButton(lobbyBg.rectTransform, "\u8bbe\u7f6e", new Vector2(1010f, -928f), () => onOpenSettings?.Invoke(), true);
            CombatUIScreenUtility.CreateButton(lobbyBg.rectTransform, "\u9000\u51fa", new Vector2(1190f, -928f), () => onQuit?.Invoke(), true);
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
                var previewText = CombatUIScreenUtility.CreateText("PreviewText", preview, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleCenter);
                previewText.text = "\u5730\u56fe\u56fe\u7247\u9884\u7559";
                CombatUIScreenUtility.SetTextStyle(previewText, new Color(0.72f, 0.66f, 0.58f), false);

                var name = CombatUIScreenUtility.CreateText("Name", card.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -174f), new Vector2(-30f, 42f), 21, TextAnchor.MiddleCenter);
                name.text = map.DisplayName;
                CombatUIScreenUtility.SetTextStyle(name, new Color(1f, 0.84f, 0.44f), true);

                var description = CombatUIScreenUtility.CreateText("Description", card.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 16, TextAnchor.UpperLeft);
                description.rectTransform.offsetMin = new Vector2(28f, 76f);
                description.rectTransform.offsetMax = new Vector2(-28f, -214f);
                description.text = $"{map.Description}\n\u96be\u5ea6\uff1a{map.Difficulty}";
                CombatUIScreenUtility.SetTextStyle(description, new Color(0.88f, 0.8f, 0.66f), false);

                CombatUIScreenUtility.CreateButton(card.rectTransform, "\u9009\u62e9", new Vector2(140f, -316f), () => onSelect?.Invoke(map), true);
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
                stats.text = $"\u804c\u80fd {hero.ArchetypeDisplayName}  \u4e13\u7cbe {hero.SpecializationDisplayName}\n\u504f\u597d {hero.PreferredRowDisplayName}\n\u7b49\u7ea7 {hero.Level}  EXP {hero.Experience}/{hero.ExperienceToNextLevel}\n\u751f\u547d {hero.CurrentHealth}/{hero.MaxHealthWithGrowth}\n\u653b\u51fb {hero.AttackWithGrowth}\n\u9632\u5fa1 {hero.DefenseWithGrowth}\n\u901f\u5ea6 {hero.SpeedWithArchetype}";
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
        private CombatantDefinition selectedHero;
        private IReadOnlyList<CombatantDefinition> allHeroes;
        private Action onBackAction;

        public void Show(IReadOnlyList<CombatantDefinition> heroPool, Action onBack)
        {
            var codexStyle = CombatUIImageStyle.Capture(heroCodexBg);
            var cardStyle = CombatUIImageStyle.Capture(cardBg);
            BuildLayout();
            var root = PrepareRoot();
            allHeroes = heroPool == null ? Array.Empty<CombatantDefinition>() : heroPool.Where(hero => hero != null && hero.isHero).ToArray();
            onBackAction = onBack;

            heroCodexBg = CombatUIScreenUtility.CreatePanel("HeroCodexPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1020f, 680f), new Color(0.032f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
            codexStyle.ApplyTo(heroCodexBg);

            var title = CombatUIScreenUtility.CreateText("Title", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 58f), 28, TextAnchor.MiddleCenter);
            title.text = "英雄图鉴";
            CombatUIScreenUtility.SetTextStyle(title, new Color(0.96f, 0.82f, 0.48f), true);

            if (allHeroes.Count > 0)
            {
                selectedHero = allHeroes[0];
            }

            RenderHeroList();
            RenderHeroDetail();
            CombatUIScreenUtility.CreateButton(heroCodexBg.rectTransform, "返回大厅", new Vector2(510f, -630f), () => onBack?.Invoke(), true);
        }

        private void RenderHeroList()
        {
            if (allHeroes.Count == 0)
            {
                var emptyText = CombatUIScreenUtility.CreateText("EmptyList", heroCodexBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.2f, 0.5f), new Vector2(14f, 0f), new Vector2(176f, -200f), 16, TextAnchor.MiddleCenter);
                emptyText.text = "暂无英雄";
                CombatUIScreenUtility.SetTextStyle(emptyText, new Color(0.6f, 0.55f, 0.45f), false);
                return;
            }

            var listHeight = allHeroes.Count * 68f;
            var listPanel = CombatUIScreenUtility.CreatePanel("HeroList", heroCodexBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -40f), new Vector2(204f, -600f), new Color(0.05f, 0.04f, 0.035f, 0.85f));

            for (var i = 0; i < allHeroes.Count; i++)
            {
                var hero = allHeroes[i];
                var yOffset = -20f - i * 68f;
                var isSelected = hero == selectedHero;
                var bgColor = isSelected ? new Color(0.15f, 0.12f, 0.08f, 0.95f) : new Color(0.06f, 0.05f, 0.04f, 0.8f);
                var button = CombatUIScreenUtility.CreatePanel($"HeroBtn_{i}", listPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(4f, yOffset), new Vector2(-4f, yOffset + 62f), bgColor);
                var capturedHero = hero;
                button.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
                {
                    selectedHero = capturedHero;
                    BuildLayout();
                    var root2 = PrepareRoot();
                    var freshBg = CombatUIScreenUtility.CreatePanel("HeroCodexPanel", root2, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1020f, 680f), new Color(0.032f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
                    heroCodexBg = freshBg;

                    var title2 = CombatUIScreenUtility.CreateText("Title", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 58f), 28, TextAnchor.MiddleCenter);
                    title2.text = "英雄图鉴";
                    CombatUIScreenUtility.SetTextStyle(title2, new Color(0.96f, 0.82f, 0.48f), true);
                    allHeroes = allHeroes;
                    selectedHero = capturedHero;
                    RenderHeroList();
                    RenderHeroDetail();
                    CombatUIScreenUtility.CreateButton(heroCodexBg.rectTransform, "返回大厅", new Vector2(510f, -630f), () => onBackAction?.Invoke(), true);
                });

                var statusLabel = hero.isUnlocked ? "[已解锁]" : "[未解锁]";
                var statusColor = hero.isUnlocked ? new Color(0.38f, 0.9f, 0.38f) : new Color(0.7f, 0.3f, 0.3f);
                var nameText = CombatUIScreenUtility.CreateText($"Name_{i}", button, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(12f, 14f), new Vector2(-12f, -12f), 14, TextAnchor.MiddleLeft);
                nameText.text = $"{statusLabel}\n{hero.displayName}  Lv.{hero.Level}  {hero.ArchetypeDisplayName}";
                CombatUIScreenUtility.SetTextStyle(nameText, isSelected ? new Color(1f, 0.9f, 0.5f) : new Color(0.78f, 0.72f, 0.6f), isSelected);
            }
        }

        private void RenderHeroDetail()
        {
            if (selectedHero == null)
            {
                var emptyText = CombatUIScreenUtility.CreateText("DetailEmpty", heroCodexBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180f, -20f), new Vector2(-40f, -600f), 16, TextAnchor.MiddleCenter);
                emptyText.text = "请从左侧列表选择一个英雄";
                CombatUIScreenUtility.SetTextStyle(emptyText, new Color(0.6f, 0.55f, 0.45f), false);
                return;
            }

            var detailX = 240f;
            var detailWidth = 740f;

            // Portrait area
            portraitBg = CombatUIScreenUtility.CreatePanel("Portrait", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(detailX, -72f), new Vector2(detailX + 148f, -248f), new Color(0.08f, 0.07f, 0.06f, 0.9f)).GetComponent<Image>();
            var portraitText = CombatUIScreenUtility.CreateText("PortraitText", portraitBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleCenter);
            portraitText.text = selectedHero.portrait != null ? "" : "[立绘]";
            CombatUIScreenUtility.SetTextStyle(portraitText, new Color(0.5f, 0.45f, 0.35f), false);

            // Hero name & level
            var headerText = CombatUIScreenUtility.CreateText("DetailHeader", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(detailX + 160f, -78f), new Vector2(-40f, -140f), 24, TextAnchor.MiddleLeft);
            headerText.text = $"{selectedHero.displayName}";
            CombatUIScreenUtility.SetTextStyle(headerText, new Color(0.98f, 0.85f, 0.42f), true);

            var unlockText = CombatUIScreenUtility.CreateText("UnlockStatus", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(detailX + 160f, -100f), new Vector2(-40f, -122f), 14, TextAnchor.MiddleLeft);
            unlockText.text = selectedHero.isUnlocked ? "已解锁" : "未解锁";
            CombatUIScreenUtility.SetTextStyle(unlockText, selectedHero.isUnlocked ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.8f, 0.4f, 0.4f), false);

            var levelText = CombatUIScreenUtility.CreateText("DetailLevel", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(detailX + 160f, -118f), new Vector2(-40f, -140f), 14, TextAnchor.MiddleLeft);
            levelText.text = $"等级 {selectedHero.Level}   经验 {selectedHero.Experience}/{selectedHero.ExperienceToNextLevel}";
            CombatUIScreenUtility.SetTextStyle(levelText, new Color(0.72f, 0.68f, 0.55f), false);

            var archetypeText = CombatUIScreenUtility.CreateText("Archetype", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(detailX + 160f, -136f), new Vector2(-40f, -158f), 14, TextAnchor.MiddleLeft);
            archetypeText.text = $"职能 {selectedHero.ArchetypeDisplayName}   专精 {selectedHero.SpecializationDisplayName}   偏好站位 {selectedHero.PreferredRowDisplayName}";
            CombatUIScreenUtility.SetTextStyle(archetypeText, new Color(0.78f, 0.73f, 0.6f), false);

            // Divider
            var divider = CombatUIScreenUtility.CreatePanel("Divider", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(detailX, -255f), new Vector2(-40f, -257f), new Color(0.3f, 0.25f, 0.15f, 0.8f));

            // Stats section
            var statsLabel = CombatUIScreenUtility.CreateText("StatsLabel", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(detailX, -268f), new Vector2(detailX + 140f, -294f), 16, TextAnchor.MiddleLeft);
            statsLabel.text = "▎基础属性";
            CombatUIScreenUtility.SetTextStyle(statsLabel, new Color(0.96f, 0.82f, 0.48f), true);

            var statsData = CombatUIScreenUtility.CreateText("StatsData", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(detailX, -300f), new Vector2(-40f, -440f), 14, TextAnchor.UpperLeft);
            var speedDesc = selectedHero.SpeedWithArchetype >= 7 ? "极快" : selectedHero.SpeedWithArchetype >= 5 ? "较快" : "普通";
            statsData.text = $"生命值：{selectedHero.MaxHealthWithGrowth}    (+{selectedHero.GrowthMaxHealthPerLevel}/级)\n" +
                             $"攻击力：{selectedHero.AttackWithGrowth}    (+{selectedHero.GrowthAttackPerLevel}/级)\n" +
                             $"防御力：{selectedHero.DefenseWithGrowth}    (+{selectedHero.GrowthDefensePerLevel}/级)\n" +
                             $"速度：{selectedHero.SpeedWithArchetype} （{speedDesc}）\n" +
                             $"职能定位：{selectedHero.ArchetypeDisplayName}\n" +
                             $"原型特征：{selectedHero.ArchetypeSummary}\n" +
                             $"专精分支：{selectedHero.SpecializationDisplayName}\n" +
                             $"专精特征：{selectedHero.SpecializationSummary}\n" +
                             $"偏好站位：{selectedHero.PreferredRowDisplayName}\n" +
                             $"招募价格：{selectedHero.recruitPrice} 金币";
            CombatUIScreenUtility.SetTextStyle(statsData, new Color(0.82f, 0.78f, 0.66f), false);

            // Passive section
            var passiveLabel = CombatUIScreenUtility.CreateText("PassiveLabel", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(detailX, -452f), new Vector2(detailX + 200f, -478f), 16, TextAnchor.MiddleLeft);
            passiveLabel.text = "▎被动能力";
            CombatUIScreenUtility.SetTextStyle(passiveLabel, new Color(0.96f, 0.82f, 0.48f), true);

            if (selectedHero.passive != HeroPassive.None)
            {
                var passiveName = CombatUIScreenUtility.CreateText("PassiveName", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(detailX, -486f), new Vector2(detailX + 200f, -506f), 15, TextAnchor.MiddleLeft);
                passiveName.text = GetPassiveDisplayName(selectedHero.passive);
                CombatUIScreenUtility.SetTextStyle(passiveName, new Color(1f, 0.75f, 0.35f), true);

                var passiveDesc = CombatUIScreenUtility.CreateText("PassiveDesc", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(detailX, -512f), new Vector2(-40f, -550f), 12, TextAnchor.UpperLeft);
                passiveDesc.text = GetPassiveDescription(selectedHero.passive);
                CombatUIScreenUtility.SetTextStyle(passiveDesc, new Color(0.7f, 0.66f, 0.55f), false);
            }
            else
            {
                var noPassive = CombatUIScreenUtility.CreateText("NoPassive", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(detailX, -488f), new Vector2(-40f, -525f), 12, TextAnchor.UpperLeft);
                noPassive.text = "该英雄尚未觉醒被动能力。";
                CombatUIScreenUtility.SetTextStyle(noPassive, new Color(0.55f, 0.5f, 0.4f), false);
            }

            // Skills section
            if (selectedHero.skills != null && selectedHero.skills.Length > 0)
            {
                var skillsLabel = CombatUIScreenUtility.CreateText("SkillsLabel", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(detailX, -562f), new Vector2(detailX + 200f, -588f), 16, TextAnchor.MiddleLeft);
                skillsLabel.text = "▎技能";
                CombatUIScreenUtility.SetTextStyle(skillsLabel, new Color(0.96f, 0.82f, 0.48f), true);

                var skillsText = CombatUIScreenUtility.CreateText("SkillsText", heroCodexBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(detailX, -596f), new Vector2(-40f, -628f), 12, TextAnchor.UpperLeft);
                var skillLines = new System.Text.StringBuilder();
                foreach (var skill in selectedHero.skills)
                {
                    if (skill == null) continue;
                    var typeLabel = skill.skillType == SkillDataType.伤害 ? "⚔" : skill.skillType == SkillDataType.治疗 ? "♥" : "✦";
                    skillLines.AppendLine($"{typeLabel} {skill.skillName} - {skill.description}");
                }
                skillsText.text = skillLines.ToString().TrimEnd();
                CombatUIScreenUtility.SetTextStyle(skillsText, new Color(0.78f, 0.72f, 0.6f), false);
            }
        }

        private static string GetPassiveDisplayName(HeroPassive passive)
        {
            switch (passive)
            {
                case HeroPassive.Berserker: return "狂战士";
                case HeroPassive.Executioner: return "处决者";
                case HeroPassive.ChainReaction: return "连锁反应";
                case HeroPassive.Backstab: return "背刺";
                case HeroPassive.GlassCannon: return "玻璃大炮";
                case HeroPassive.IronWill: return "铁意志";
                case HeroPassive.Regenerator: return "再生";
                case HeroPassive.ThornArmor: return "荆棘护甲";
                case HeroPassive.Bodyguard: return "保镖";
                case HeroPassive.Fortress: return "堡垒";
                case HeroPassive.Tactician: return "战术家";
                case HeroPassive.Scavenger: return "回收者";
                case HeroPassive.Vanguard: return "先锋";
                case HeroPassive.Reaper: return "收割者";
                case HeroPassive.Inspirer: return "鼓舞者";
                default: return "无";
            }
        }

        private static string GetPassiveDescription(HeroPassive passive)
        {
            switch (passive)
            {
                case HeroPassive.Berserker: return "血量低于50%时，攻击力提升30%。残血时越战越勇。";
                case HeroPassive.Executioner: return "攻击血量低于30%的敌人时，伤害提升50%。善于收割残血目标。";
                case HeroPassive.ChainReaction: return "击杀敌人时，对随机另一敌人造成目标最大生命值25%的溅射伤害。";
                case HeroPassive.Backstab: return "从后排攻击前排敌人时，伤害提升25%。适合放在后排输出。";
                case HeroPassive.GlassCannon: return "攻击力提升25%，但防御力降低50%。高攻低防的极端输出者。";
                case HeroPassive.IronWill: return "每场战斗首次受到致命伤害时，保留1点生命值。绝境中屹立不倒。";
                case HeroPassive.Regenerator: return "回合开始时，恢复最大生命值的5%。持久战的生存专家。";
                case HeroPassive.ThornArmor: return "受到伤害时，反弹20%伤害给攻击者。以牙还牙。";
                case HeroPassive.Bodyguard: return "相邻队友受到攻击时，承担30%伤害。守护身边的战友。";
                case HeroPassive.Fortress: return "位于前排时，防御力+4。前排最坚实的壁垒。";
                case HeroPassive.Tactician: return "回合开始时，随机减少1名队友的技能冷却2回合。团队的战术大脑。";
                case HeroPassive.Scavenger: return "击杀敌人时，恢复自身最大生命值的20%。从战场中汲取力量。";
                case HeroPassive.Vanguard: return "位于前排时，全体队友攻击力+2。冲锋在前的战斗领袖。";
                case HeroPassive.Reaper: return "场上每有一个敌人死亡，攻击力提升10%（可叠加）。越战越强。";
                case HeroPassive.Inspirer: return "回合开始时，恢复全体队友最大生命值的10%。团队的灵魂支柱。";
                default: return "";
            }
        }
    }

    public sealed partial class SettingsUI
    {
        public Image settingsBg;
        public Image sliderBg;

        public void Show(Action onBack)
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
            CombatUIScreenUtility.CreateButton(settingsBg.rectTransform, "\u8fd4\u56de", new Vector2(310f, -304f), () => onBack?.Invoke(), true);
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
        [SerializeField] private RectTransform skillPanel;
        [SerializeField] private RectTransform targetPanel;
        [SerializeField] private RectTransform logFrame;
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
        public RectTransform TargetPanel => targetPanel;
        public Text RoundText => roundText;
        public Text TurnText => turnText;
        public Text LogText => logText;
        public Text InfoText => infoText;
        public Text GoldText => goldText;
        public ScrollRect LogScrollRect => logScrollRect;

        public void Build(Sprite panelSprite)
        {
            if (TryBindExistingLayout())
            {
                ApplyExistingStyles(panelSprite);
                return;
            }

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
            bottomBar.offsetMin = new Vector2(18f, 18f);
            bottomBar.offsetMax = new Vector2(-18f, 228f);

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

            skillPanelBg = CombatUIScreenUtility.CreatePanel("SkillPanel", bottomBar, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.035f, 0.032f, 1f)).GetComponent<Image>();
            skillStyle.ApplyTo(skillPanelBg);
            skillPanelBg.sprite = skillPanelBg.sprite != null ? skillPanelBg.sprite : panelSprite;
            skillPanelBg.type = Image.Type.Sliced;
            skillPanel = skillPanelBg.rectTransform;
            skillPanel.offsetMin = new Vector2(398f, 18f);
            skillPanel.offsetMax = new Vector2(-398f, -18f);

            skillTitleText = CombatUIScreenUtility.CreateText("SkillTitle", skillPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleLeft);
            skillTitleText.text = "\u6280\u80fd";
            CombatUIScreenUtility.SetTextStyle(skillTitleText, new Color(0.96f, 0.82f, 0.48f), true);
            skillTitleText.rectTransform.offsetMin = new Vector2(16f, -34f);
            skillTitleText.rectTransform.offsetMax = new Vector2(-16f, -10f);

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

            infoText = CombatUIScreenUtility.CreateText("InfoText", targetPanel, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft);
            infoText.rectTransform.offsetMin = new Vector2(16f, 18f);
            infoText.rectTransform.offsetMax = new Vector2(-16f, -44f);
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
            skillPanelBg = bottomBar != null ? bottomBar.Find("SkillPanel")?.GetComponent<Image>() : null;
            skillPanel = skillPanelBg != null ? skillPanelBg.rectTransform : null;
            skillTitleText = skillPanel != null ? skillPanel.Find("SkillTitle")?.GetComponent<Text>() : null;
            targetPanelBg = bottomBar != null ? bottomBar.Find("TargetPanel")?.GetComponent<Image>() : null;
            targetPanel = targetPanelBg != null ? targetPanelBg.rectTransform : null;
            targetTitleText = targetPanel != null ? targetPanel.Find("TargetTitle")?.GetComponent<Text>() : null;
            infoText = targetPanel != null ? targetPanel.Find("InfoText")?.GetComponent<Text>() : null;

            return skillPanel != null && targetPanel != null && logFrame != null && roundText != null && turnText != null && logText != null && infoText != null && goldText != null && logScrollRect != null;
        }

        private void ApplyExistingStyles(Sprite panelSprite)
        {
            if (battleHudBg != null)
            {
                battleHudBg.sprite = battleHudBg.sprite != null ? battleHudBg.sprite : panelSprite;
                battleHudBg.type = Image.Type.Sliced;
            }

            if (logFrameBg != null)
            {
                logFrameBg.sprite = logFrameBg.sprite != null ? logFrameBg.sprite : panelSprite;
                logFrameBg.type = Image.Type.Sliced;
            }

            if (skillPanelBg != null)
            {
                skillPanelBg.sprite = skillPanelBg.sprite != null ? skillPanelBg.sprite : panelSprite;
                skillPanelBg.type = Image.Type.Sliced;
            }

            if (targetPanelBg != null)
            {
                targetPanelBg.sprite = targetPanelBg.sprite != null ? targetPanelBg.sprite : panelSprite;
                targetPanelBg.type = Image.Type.Sliced;
            }
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
        private const string SkillDescriptionPanelPrefabPath = "Assets/UI/Prefabs/SkillDescriptionPanelUI.prefab";
        private static readonly Vector2 TooltipSize = new Vector2(460f, 126f);
        private static readonly Vector2 TooltipOffset = new Vector2(0f, 88f);

        [SerializeField] private SkillDescriptionPanelUI panelPrefab;
        [SerializeField] private SkillDescriptionPanelUI panelInstance;
        [SerializeField] private Text skillDescriptionText;

        public Image skillDescriptionBg;
        public Text SkillDescriptionText => skillDescriptionText;

        public void Build(Sprite descriptionSprite)
        {
            BuildLayout();
            var root = PrepareRoot();
            ConfigureTooltipRect();
            CreateOrBindPanel(root, descriptionSprite);
            Hide();
        }

        private void CreateOrBindPanel(RectTransform root, Sprite descriptionSprite)
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
            panelInstance.Build(descriptionSprite);
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

        private void ApplyExistingStyles(Sprite descriptionSprite)
        {
            if (skillDescriptionBg != null)
            {
                skillDescriptionBg.sprite = skillDescriptionBg.sprite != null ? skillDescriptionBg.sprite : descriptionSprite;
                skillDescriptionBg.type = Image.Type.Sliced;
            }
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
                    !AssetDatabase.LoadAssetAtPath<LobbyUI>("Assets/UI/Prefabs/LobbyUI.prefab") ||
                    !AssetDatabase.LoadAssetAtPath<TavernUI>("Assets/UI/Prefabs/TavernUI.prefab") ||
                    !AssetDatabase.LoadAssetAtPath<AdventureMapUI>("Assets/UI/Prefabs/AdventureMapUI.prefab") ||
                    !AssetDatabase.LoadAssetAtPath<TeamSelectionUI>("Assets/UI/Prefabs/TeamSelectionUI.prefab") ||
                    !AssetDatabase.LoadAssetAtPath<HeroCodexUI>("Assets/UI/Prefabs/HeroCodexUI.prefab") ||
                    !AssetDatabase.LoadAssetAtPath<SettingsUI>("Assets/UI/Prefabs/SettingsUI.prefab") ||
                    !AssetDatabase.LoadAssetAtPath<PopupUI>("Assets/UI/Prefabs/PopupUI.prefab") ||
                    !AssetDatabase.LoadAssetAtPath<BattleHudUI>("Assets/UI/Prefabs/BattleHudUI.prefab") ||
                    !AssetDatabase.LoadAssetAtPath<SkillDescriptionUI>("Assets/UI/Prefabs/SkillDescriptionUI.prefab") ||
                    !AssetDatabase.LoadAssetAtPath<SkillDescriptionPanelUI>("Assets/UI/Prefabs/SkillDescriptionPanelUI.prefab"))
                {
                    CreateScreenUIPrefabs();
                }
            };
        }

        [MenuItem("Clockwork Wasteland/Create Screen UI Prefabs")]
        public static void CreateScreenUIPrefabs()
        {
            EnsureFolder("Assets", "UI");
            EnsureFolder("Assets/UI", "Prefabs");
            CreatePrefab<LobbyUI>("LobbyUI");
            CreatePrefab<TavernUI>("TavernUI");
            CreatePrefab<AdventureMapUI>("AdventureMapUI");
            CreatePrefab<TeamSelectionUI>("TeamSelectionUI");
            CreatePrefab<HeroCodexUI>("HeroCodexUI");
            CreatePrefab<SettingsUI>("SettingsUI");
            CreatePrefab<PopupUI>("PopupUI");
            CreatePrefab<BattleHudUI>("BattleHudUI");
            CreateSkillDescriptionPanelPrefab();
            CreatePrefab<SkillDescriptionUI>("SkillDescriptionUI");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreatePrefab<T>(string prefabName) where T : CombatUIScreen
        {
            var path = $"Assets/UI/Prefabs/{prefabName}.prefab";
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

        private static void CreateSkillDescriptionPanelPrefab()
        {
            var path = "Assets/UI/Prefabs/SkillDescriptionPanelUI.prefab";
            var root = new GameObject("SkillDescriptionPanelUI", typeof(RectTransform), typeof(SkillDescriptionPanelUI));
            try
            {
                var rect = root.transform as RectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                root.GetComponent<SkillDescriptionPanelUI>().Build(null);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildPreviewLayout(CombatUIScreen screen)
        {
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
                adventureMap.Show(new[]
                {
                    new AdventureMapOption("preview_1", "\u5e9f\u571f\u8fb9\u5883", "\u5730\u56fe\u63cf\u8ff0\u9884\u7559", 1),
                    new AdventureMapOption("preview_2", "\u94a2\u94c1\u5de5\u574a", "\u5730\u56fe\u63cf\u8ff0\u9884\u7559", 2),
                    new AdventureMapOption("preview_3", "\u7070\u70ec\u5730\u7a9f", "\u5730\u56fe\u63cf\u8ff0\u9884\u7559", 3)
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
                var hero = CreatePreviewHero();
                try
                {
                    heroCodex.Show(new[] { hero }, null);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(hero);
                }

                return;
            }

            if (screen is SettingsUI settings)
            {
                settings.Show(null);
                return;
            }

            if (screen is PopupUI popup)
            {
                popup.Show("\u901a\u7528\u5f39\u7a97\u9884\u89c8", "\u786e\u5b9a", null);
                return;
            }

            if (screen is BattleHudUI battleHud)
            {
                battleHud.Build(null);
                return;
            }

            if (screen is SkillDescriptionUI skillDescription)
            {
                skillDescription.Build(null);
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
