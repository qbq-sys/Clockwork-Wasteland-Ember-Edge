using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ClockworkWasteland.Combat
{
    public readonly struct BattleRewardResult
    {
        public BattleRewardResult(CombatantDefinition hero, int experienceGained, int levelsGained)
        {
            Hero = hero;
            ExperienceGained = experienceGained;
            LevelsGained = levelsGained;
        }

        public CombatantDefinition Hero { get; }
        public int ExperienceGained { get; }
        public int LevelsGained { get; }
    }

    public readonly struct InventoryItemStack
    {
        public InventoryItemStack(InventoryItemData item, int count)
        {
            Item = item;
            Count = count;
        }

        public InventoryItemData Item { get; }
        public int Count { get; }
    }

    public readonly struct BlurredRendererState
    {
        public BlurredRendererState(SpriteRenderer renderer, Material material)
        {
            Renderer = renderer;
            Material = material;
        }

        public SpriteRenderer Renderer { get; }
        public Material Material { get; }
    }

    public readonly struct AttackLungeState
    {
        public AttackLungeState(CombatantView actorView, Vector3 actorStart, Vector3 actorEnd, Vector3 actorRecoil, CombatantView[] targetViews, Vector3[] targetStarts, Vector3[] targetEnds, Vector3[] targetRecoils)
        {
            ActorView = actorView;
            ActorStart = actorStart;
            ActorEnd = actorEnd;
            ActorRecoil = actorRecoil;
            TargetViews = targetViews;
            TargetStarts = targetStarts;
            TargetEnds = targetEnds;
            TargetRecoils = targetRecoils;
        }

        public CombatantView ActorView { get; }
        public Vector3 ActorStart { get; }
        public Vector3 ActorEnd { get; }
        public Vector3 ActorRecoil { get; }
        public CombatantView[] TargetViews { get; }
        public Vector3[] TargetStarts { get; }
        public Vector3[] TargetEnds { get; }
        public Vector3[] TargetRecoils { get; }
        public bool IsValid => ActorView != null && TargetViews != null && TargetViews.Length > 0;
    }

    public enum MapNodeType
    {
        Battle,
        Rest,
        Chest
    }

    public readonly struct MapNodeOption
    {
        public MapNodeOption(MapNodeType nodeType, string displayName, string description)
        {
            NodeType = nodeType;
            DisplayName = displayName;
            Description = description;
        }

        public MapNodeType NodeType { get; }
        public string DisplayName { get; }
        public string Description { get; }
    }

    public readonly struct AdventureMapOption
    {
        public AdventureMapOption(AdventureMapData data, bool isUnlocked)
        {
            Data = data;
            MapId = data != null ? data.mapId : string.Empty;
            DisplayName = data != null ? data.displayName : "δ������ͼ";
            Description = data != null ? data.description : string.Empty;
            PreviewSprite = data != null ? data.backgroundSprite : null;
            BackgroundOffset = data != null ? data.backgroundOffset : Vector2.zero;
            BackgroundScale = data != null ? Mathf.Max(0.1f, data.backgroundScale) : 1f;
            BattleCount = data != null ? Mathf.Max(1, data.BattleCount) : 1;
            IsUnlocked = isUnlocked;
            UnlockSummary = data != null ? data.GetUnlockSummary() : string.Empty;
        }

        public AdventureMapData Data { get; }
        public string MapId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite PreviewSprite { get; }
        public Vector2 BackgroundOffset { get; }
        public float BackgroundScale { get; }
        public int BattleCount { get; }
        public bool IsUnlocked { get; }
        public string UnlockSummary { get; }
    }

    public readonly struct SaveSlotSummary
    {
        public SaveSlotSummary(int slotIndex, bool hasSave, string title, string detail)
        {
            SlotIndex = slotIndex;
            HasSave = hasSave;
            Title = title;
            Detail = detail;
        }

        public int SlotIndex { get; }
        public bool HasSave { get; }
        public string Title { get; }
        public string Detail { get; }
    }

    [System.Serializable]
    public sealed class GameSaveData
    {
        public int gold;
        public string savedAt;
        public List<HeroSaveData> heroes = new List<HeroSaveData>();
        public List<string> clearedMapIds = new List<string>();
    }

    [System.Serializable]
    public sealed class HeroSaveData
    {
        public string characterId;
        public bool unlocked;
        public int level;
        public int experience;
        public int health;
        public int specialization;
        public int growthPassive;
        public int recoveryState;
        public int recoveryBattlesRemaining;
    }

    public sealed class BattleController : MonoBehaviour
    {
        private const string GuardStatusName = "护卫";
        private const string SuppressedStatusName = "压制";
        private const int WoundedRecoveryBattleCount = 1;
        private const int RecoveryTreatmentCost = 150;
        private const float RecoveryTreatmentHealthPercent = 0.5f;
        private const float WoundedStabilizedHealthPercent = 0.01f;

        private const int MaxFormationSlots = 4;
        private const int MapNodeCount = 3;
        private const int InitialGold = 1200;
        private const float HeroFrontSlotX = -0.8f;
        private const float EnemyFrontSlotX = 1.3f;
        private const float BaseFormationSlotSpacing = 1.1f;
        private const float ReferenceFormationVisualScale = 0.8f;
        private const int AttackFocusTargetSortingOrder = 300;
        private const int AttackFocusActorSortingOrder = 301;
        private const int AttackFocusTargetOverlaySortingOrder = 302;
        private const int AttackFocusActorOverlaySortingOrder = 303;
        private const int SaveSlotCount = 3;
        private const string LegacySaveKey = "ClockworkWasteland.Save.v1";
        private const string SaveSlotKeyPrefix = "ClockworkWasteland.SaveSlot.v2.";
        private const float MinCombatOverlayDuration = 0.56f;
        private const float BulletTimeDuration = 0.62f;
        private const float BulletTimeMinScale = 0.08f;
        private static readonly int[] AnyPosition = { 1, 2, 3, 4 };

        [SerializeField] private CombatantDefinition[] heroParty;
        [SerializeField] private CombatantDefinition[] enemyParty;
        [SerializeField] private CombatantDefinition[] heroPoolConfig;
        [SerializeField] private AdventureMapCatalog adventureMapCatalog;

        [Header("Formation")]
        [SerializeField] private float combatantY = 0.2f;
        [SerializeField] private float nameplatePositionY = -1.726f;

        [Header("Presentation")]
        [FormerlySerializedAs("battleUIPrefab")]
        [SerializeField] private BattleHudController battleHudControllerPrefab;
        [SerializeField] private CombatantView defaultUnitPrefab;
        [SerializeField] private CombatNameplate nameplatePrefab;
        [SerializeField] private TurnIndicatorView turnIndicatorPrefab;
        [SerializeField] private Sprite[] battleBackgrounds;
        [SerializeField] private int battleBackgroundIndex = 0;
        [SerializeField] private Vector3 floatingTextBaseOffset = new Vector3(0f, 1.2f, -0.6f);
        [SerializeField] private float floatingTextBaseScale = 1f;
        [SerializeField] private float floatingTextBurstWindow = 0.18f;
        [SerializeField] private float floatingTextQueueDelay = 0.08f;
        [SerializeField] private float floatingTextHorizontalSpacing = 0.18f;
        [SerializeField] private float floatingTextVerticalSpacing = 0.26f;
        [SerializeField] private float floatingTextAdditionalLiftPerText = 0.05f;

        [Header("Attack Focus")]
        [SerializeField] private Material attackFocusBlurMaterial;
        [SerializeField] private float attackFocusZoomRatio = 0.62f;
        [SerializeField] private Vector2 attackFocusCameraOffset = new Vector2(0f, -0.05f);
        [SerializeField] private float attackFocusScale = 1.2f;
        [SerializeField] private float attackFocusBlurStrength = 0.45f;
        [SerializeField] private Color attackFocusBlurTint = new Color(0.68f, 0.68f, 0.72f, 1f);
        [SerializeField] private float attackFocusLungeDuration = 0.09f;
        [SerializeField] private float attackFocusHitPause = 0.05f;
        [SerializeField] private float attackFocusScaleRestoreDuration = 0.2f;
        [SerializeField, Range(0f, 1f)] private float attackFocusRecoilRatio = 0.22f;
        [SerializeField] private float attackFocusReturnDuration = 0.07f;
        [SerializeField, Range(0f, 0.9f)] private float attackFocusSingleLungeRatio = 0.4f;
        [SerializeField] private float attackFocusSingleMinDistance = 0.8f;
        [SerializeField] private float attackFocusSingleActorOffset = 1.15f;
        [SerializeField] private float attackFocusSingleTargetOffset = 1.15f;
        [SerializeField] private float attackFocusFriendlyAdvanceOffset = 1.15f;
        [SerializeField] private float attackFocusAoeActorOffset = 1.15f;
        [SerializeField] private float attackFocusAoeTargetOffset = 1.35f;
        [SerializeField] private float attackFocusAoeTargetSpacing = 1.35f;

        [Header("Testing")]
        [SerializeField] private bool debugTestingEnabled;
        [SerializeField, Min(0.1f)] private float debugExperienceRewardMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float debugGoldRewardMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float debugChestGoldMultiplier = 1f;
        [SerializeField] private bool debugUnlockAllMaps;
        [SerializeField] private bool debugUnlockAllHeroesOnNewGame = true;
        [SerializeField, Min(1)] private int debugNewGameHeroLevel = 1;
        [SerializeField] private int debugStartingGoldOverride = -1;
        [SerializeField, Min(1)] private int debugManualExperienceGrant = 100;

        private readonly List<BattleUnit> heroes = new List<BattleUnit>();
        private readonly List<BattleUnit> enemies = new List<BattleUnit>();
        private readonly BattleUnit[] heroSlots = new BattleUnit[MaxFormationSlots];
        private readonly BattleUnit[] enemySlots = new BattleUnit[MaxFormationSlots];
        private readonly Dictionary<BattleUnit, CombatantView> views = new Dictionary<BattleUnit, CombatantView>();
        private readonly List<BattleUnit> turnQueue = new List<BattleUnit>();
        private Dictionary<string, BuffData> buffLookup;

        private BattleHudController hudController;
        private Sprite fallbackSprite;
        private bool waitingForPlayer;
        private BattleUnit currentActor;
        private BattleUnit selectedUnit;
        private SkillData selectedSkill;
        private BattleUnit[] validSelectedTargets = new BattleUnit[0];
        private bool resolvingPlayerAction;
        private int round;
        private Vector3 defaultCameraPosition;
        private float defaultCameraSize;
        private SkillData swapSkill;
        private readonly HashSet<string> clearedMapIds = new HashSet<string>();
        private AdventureBattleConfig currentAdventureBattle;
        private SpriteRenderer battleBackdropRenderer;
        private SkillData passTurnSkill;
        private int currentBattleNumber;
        private int gold;
        private InventoryItemData[] shopItems = new InventoryItemData[0];
        private readonly Dictionary<InventoryItemData, int> inventory = new Dictionary<InventoryItemData, int>();
        private CombatantDefinition[] totalHeroPool = new CombatantDefinition[0];
        private CombatantDefinition[] availableHeroPool = new CombatantDefinition[0];
        private readonly List<CombatantDefinition> selectedHeroDefinitions = new List<CombatantDefinition>();
        private readonly List<CombatantDefinition> tavernOffers = new List<CombatantDefinition>();
        private readonly List<BlurredRendererState> blurredRendererStates = new List<BlurredRendererState>();
        private readonly HashSet<BattleUnit> ironWillUsedThisBattle = new HashSet<BattleUnit>();
        private AdventureMapOption selectedAdventureMap;
        private Material runtimeFocusBlurMaterial;
        private int activeSaveSlotIndex = -1;
        private Action settingsBackAction;

        public void Configure(CombatantDefinition[] heroesToUse, CombatantDefinition[] enemiesToUse, BattleHudController hudControllerPrefabToUse = null, CombatantView unitPrefabToUse = null, CombatNameplate nameplatePrefabToUse = null, TurnIndicatorView turnIndicatorPrefabToUse = null)
        {
            heroParty = heroesToUse;
            enemyParty = enemiesToUse;
            battleHudControllerPrefab = hudControllerPrefabToUse;
            defaultUnitPrefab = unitPrefabToUse;
            nameplatePrefab = nameplatePrefabToUse;
            turnIndicatorPrefab = turnIndicatorPrefabToUse;
        }

        private void Start()
        {
            fallbackSprite = CreateFallbackSprite();
            swapSkill = CreateSwapSkill();
            passTurnSkill = CreatePassTurnSkill();
            SetupScene();
            CombatAudio.Ensure();
            CacheDefaultCamera();
            LoadDefaultPresentationPrefabs();
            shopItems = LoadShopItems();
            LoadOrCreateGameState();
            ShowTitleScreen();
        }

        public void RunAttackFlowByIndices(int attackerIndex, int skillIndex, int targetIndex)
        {
            var attacker = heroes.Concat(enemies).Where(unit => unit.CanAct).ElementAtOrDefault(attackerIndex);
            var skills = attacker != null ? GetSkillList(attacker).ToArray() : new SkillData[0];
            if (attacker == null || skillIndex < 0 || skillIndex >= skills.Length)
            {
                hudController?.AddLog("\u653b\u51fb\u6d41\u7a0b\u53c2\u6570\u65e0\u6548\u3002");
                return;
            }

            var skill = skills[skillIndex];
            var state = GetSkillUseState(attacker, skill);
            if (!state.CanUse)
            {
                hudController?.AddLog($"\u6280\u80fd\u4e0d\u53ef\u7528\uff1a{state.DisabledReason}\u3002");
                return;
            }

            var candidates = GetValidTargets(attacker, skill).ToArray();
            if (targetIndex < 0 || targetIndex >= candidates.Length)
            {
                hudController?.AddLog("\u76ee\u6807\u7d22\u5f15\u65e0\u6548\u3002");
                return;
            }

            StartCoroutine(ExecuteSkill(attacker, skill, candidates[targetIndex]));
        }

        private void ShowTitleScreen()
        {
            PrepareNonCombatScreen("��ʼ����");
            hudController.ShowTitleScreen(HasAnySaveSlots(), StartNewGame, ShowContinueGameSlots, ShowSettingsFromTitle, QuitGame, ShowLobby);
        }

        private void PrepareNonCombatScreen(string turnLabel, bool stopMusic = true)
        {
            if (stopMusic)
            {
                CombatAudio.Instance.StopMusic();
            }

            ClearAllUnits();
            hudController.HideTransientUi();
            hudController.ClearActionPanels();
            hudController.SetRound(0);
            hudController.SetTurn(turnLabel);
            hudController.SetGold(gold);
            hudController.HideBattleHud();
        }

        private void ResetCombatSelectionState()
        {
            waitingForPlayer = false;
            resolvingPlayerAction = false;
            ClearCurrentActorIndicator();
            currentActor = null;
            selectedUnit = null;
            selectedSkill = null;
            validSelectedTargets = new BattleUnit[0];
            SetTargetHighlights(validSelectedTargets);
        }

        private void ResetCombatRuntimeState(bool clearTurnQueue)
        {
            ResetCombatSelectionState();
            if (clearTurnQueue)
            {
                turnQueue.Clear();
            }
        }

        private bool TryValidateSelectedHeroesForBattle()
        {
            if (selectedHeroDefinitions.Count == 0)
            {
                hudController.AddLog("\u8bf7\u81f3\u5c11\u9009\u62e9 1 \u540d\u82f1\u96c4\u3002");
                return false;
            }

            var deadHero = selectedHeroDefinitions.FirstOrDefault(hero => hero != null && hero.IsDead);
            if (deadHero != null)
            {
                hudController.AddLog($"{deadHero.displayName} \u5df2\u6b7b\u4ea1\uff0c\u9700\u8981\u5148\u590d\u6d3b\u624d\u80fd\u51fa\u6218\u3002");
                return false;
            }

            var recoveringHero = selectedHeroDefinitions.FirstOrDefault(hero => hero != null && hero.IsRecovering);
            if (recoveringHero != null)
            {
                hudController.AddLog($"{recoveringHero.displayName} \u6b63\u5728\u4f11\u517b\uff0c\u9700\u8981\u5148\u6062\u590d\u624d\u80fd\u51fa\u6218\u3002");
                return false;
            }

            return true;
        }

        private void BeginSelectedAdventure()
        {
            ClearAllUnits();
            hudController.HideTransientUi();
            hudController.ClearActionPanels();
            CombatAudio.Instance.PlayStartExpedition();
            CommitSelectedPartyToHeroParty();
            SetupHeroUnits();
            StartCoroutine(SelectedAdventureLoop());
        }

        private void ShowLobby()
        {
            PrepareNonCombatScreen("\u5927\u5385");
            hudController.ShowLobby(gold, ShowTavern, ShowAdventureMap, ShowRecoveryWard, ShowHeroCodex, ShowShopFromLobby, ShowInventoryFromLobby, ShowSettingsFromLobby, ShowTitleScreen);
        }

        private void StartNewGame()
        {
            var emptySlot = FindFirstEmptySaveSlot();
            if (emptySlot >= 0)
            {
                CreateNewGameInSlot(emptySlot);
                ShowLobby();
                return;
            }

            hudController.ShowChoicePrompt(
                "\u4e09\u4e2a\u5b58\u6863\u4f4d\u90fd\u5df2\u6709\u5b58\u6863\u3002\u7ee7\u7eed\u65b0\u6e38\u620f\u5c06\u8986\u76d6 1 \u53f7\u5b58\u6863\u3002",
                "\u53d6\u6d88",
                ShowLobby,
                "\u8986\u76d6\u5f00\u59cb",
                () =>
                {
                    CreateNewGameInSlot(0);
                    ShowLobby();
                });
        }

        private void ResetGameState()
        {
            StopAllCoroutines();
            ClearAllUnits();
            gold = GetInitialGoldForCurrentMode();
            inventory.Clear();
            totalHeroPool = BuildHeroPool();
            NormalizeHeroPool(totalHeroPool);
            ApplyDebugNewGameOverrides(totalHeroPool);
            availableHeroPool = GetUnlockedHeroPool();
            SyncSelectedHeroDefinitions(fillToMax: true);
            currentBattleNumber = 0;
            CommitSelectedPartyToHeroParty();
            RebuildDerivedGameState();
        }

        private void ShowSettings()
        {
            ShowSettings(settingsBackAction ?? ShowTitleScreen);
        }

        private void ShowSettings(Action onBack)
        {
            PrepareNonCombatScreen("\u8bbe\u7f6e", false);
            settingsBackAction = onBack ?? ShowTitleScreen;
            hudController.ShowSettingsScreen(settingsBackAction, ShowManualSaveSlots);
        }

        private void ShowSettingsFromTitle()
        {
            ShowSettings(ShowTitleScreen);
        }

        private void ShowSettingsFromLobby()
        {
            ShowSettings(ShowLobby);
        }

        private void QuitGame()
        {
            hudController.AddLog("\u9000\u51fa\u6e38\u620f\u3002");
            Application.Quit();
        }

        private void ShowHeroCodex()
        {
            PrepareNonCombatScreen("\u82f1\u96c4\u56fe\u9274");
            hudController.ShowHeroCodex(totalHeroPool, ShowLobby);
        }

        private void ShowRecoveryWard()
        {
            PrepareNonCombatScreen("\u4f24\u5458\u4f11\u6574", false);
            hudController.ShowRecoveryWard(totalHeroPool, gold, RecoveryTreatmentCost, TreatRecoveringHero, ShowLobby);
        }

        private void ShowAdventureMap()
        {
            PrepareNonCombatScreen("\u5192\u9669");
            hudController.ShowAdventureMap(GetAdventureMaps(), SelectAdventureMap, ShowLobby);
        }

        private void SelectAdventureMap(AdventureMapOption map)
        {
            if (!map.IsUnlocked)
            {
                hudController.AddLog($"\u5730\u56fe\u5c1a\u672a\u89e3\u9501\uff1a{map.UnlockSummary}");
                ShowAdventureMap();
                return;
            }

            selectedAdventureMap = map;
            hudController.AddLog($"\u9009\u62e9\u5730\u56fe\uff1a{map.DisplayName}\u3002");
            ShowTeamSelection();
        }

        private void ShowTeamSelection()
        {
            PrepareNonCombatScreen("\u961f\u4f0d\u914d\u7f6e");
            SyncSelectedHeroDefinitions(fillToMax: false);
            hudController.ShowTeamSelection(GetDeployableHeroPool(), selectedHeroDefinitions, ToggleHeroSelection, StartSelectedBattleSequence, ShowAdventureMap);
        }

        private void ToggleHeroSelection(CombatantDefinition hero)
        {
            if (hero == null)
            {
                return;
            }

            if (selectedHeroDefinitions.Contains(hero))
            {
                selectedHeroDefinitions.Remove(hero);
            }
            else if (selectedHeroDefinitions.Count < MaxFormationSlots)
            {
                selectedHeroDefinitions.Add(hero);
            }
            else
            {
                hudController.AddLog("\u6700\u591a\u53ea\u80fd\u9009\u62e9 4 \u540d\u82f1\u96c4\u3002");
            }

            SortSelectedHeroesInPlace();
            hudController.ShowTeamSelection(GetDeployableHeroPool(), selectedHeroDefinitions, ToggleHeroSelection, StartSelectedBattleSequence, ShowAdventureMap);
        }

        private void StartSelectedBattleSequence()
        {
            if (!TryValidateSelectedHeroesForBattle())
            {
                return;
            }

            BeginSelectedAdventure();
        }

        private void ShowShop()
        {
            hudController.ShowShop(shopItems, gold, GetInventoryStacks(), BuyItem, ShowTeamSelection);
        }

        private void ShowShopFromLobby()
        {
            PrepareNonCombatScreen("\u5546\u5e97", false);
            hudController.ShowShop(shopItems, gold, GetInventoryStacks(), BuyItem, ShowLobby);
        }

        private void ShowInventory()
        {
            hudController.ShowInventory(GetInventoryStacks(), availableHeroPool, UseItemOnHero, ShowTeamSelection);
        }

        private void ShowInventoryFromLobby()
        {
            PrepareNonCombatScreen("\u80cc\u5305", false);
            hudController.ShowInventory(GetInventoryStacks(), availableHeroPool, UseItemOnHero, ShowLobby);
        }

        private void ShowTavern()
        {
            if (tavernOffers.Count == 0)
            {
                RefreshTavernOffers();
            }

            hudController.ShowTavern(tavernOffers, gold, RecruitHero, ShowLobby);
        }

        private void RecruitHero(CombatantDefinition hero)
        {
            if (hero == null)
            {
                return;
            }

            if (hero.isUnlocked)
            {
                hudController.AddLog($"{hero.displayName} \u5df2\u7ecf\u52a0\u5165\u82f1\u96c4\u6c60\u3002");
                ShowTavern();
                return;
            }

            if (gold < hero.recruitPrice)
            {
                hudController.AddLog("\u91d1\u5e01\u4e0d\u8db3\uff0c\u65e0\u6cd5\u62db\u52df\u8be5\u82f1\u96c4\u3002");
                ShowTavern();
                return;
            }

            gold -= hero.recruitPrice;
            hero.isUnlocked = true;
            hero.EnsureRuntimeHealth();
            availableHeroPool = GetUnlockedHeroPool();
            selectedHeroDefinitions.RemoveAll(selected => selected == null || !selected.isUnlocked);
            hudController.SetGold(gold);
            SaveGameState();
            RefreshTavernOffers();
            hudController.AddLog($"\u62db\u52df\u4e86 {hero.displayName}\uff0c\u4ed6\u5df2\u52a0\u5165\u82f1\u96c4\u6c60\u3002");
            hudController.ShowContinuePrompt($"\u62db\u52df\u6210\u529f\uff1a{hero.displayName}", "\u7ee7\u7eed", ShowTavern);
        }

        private void BuyItem(InventoryItemData item)
        {
            if (item == null)
            {
                return;
            }

            if (gold < item.price)
            {
                hudController.AddLog("\u91d1\u5e01\u4e0d\u8db3\u3002");
                ShowShop();
                return;
            }

            gold -= item.price;
            inventory[item] = inventory.TryGetValue(item, out var count) ? count + 1 : 1;
            hudController.SetGold(gold);
            SaveGameState();
            hudController.AddLog($"\u8d2d\u4e70\u4e86 {item.itemName}\u3002");
            ShowShop();
        }

        private void UseItemOnHero(InventoryItemData item, CombatantDefinition hero)
        {
            if (item == null || hero == null || !inventory.TryGetValue(item, out var count) || count <= 0)
            {
                return;
            }

            if (hero.IsRecovering && item.effectType != InventoryItemEffectType.Revive)
            {
                hudController.AddLog($"{hero.displayName} 正在休养中，不能直接使用治疗道具。");
                ShowInventory();
                return;
            }

            var used = false;
            switch (item.effectType)
            {
                case InventoryItemEffectType.Revive:
                    var revivedHealth = hero.ReviveOutsideBattle(item.reviveHealthPercent);
                    used = revivedHealth > 0;
                    hudController.AddLog(used
                        ? $"{hero.displayName} \u88ab\u590d\u6d3b\uff0c\u751f\u547d\u6062\u590d\u5230 {revivedHealth}/{hero.MaxHealthWithGrowth}\u3002"
                        : $"{item.itemName} \u53ea\u80fd\u5bf9\u6b7b\u4ea1\u82f1\u96c4\u4f7f\u7528\u3002");
                    break;
                case InventoryItemEffectType.Heal:
                default:
                    var healed = hero.HealOutsideBattle(item.healAmount);
                    used = healed > 0;
                    hudController.AddLog(used
                        ? $"{hero.displayName} \u6062\u590d\u4e86 {healed} \u70b9\u751f\u547d\u3002"
                        : $"{item.itemName} \u65e0\u6cd5\u5bf9\u8be5\u82f1\u96c4\u751f\u6548\u3002");
                    break;
            }

            if (used)
            {
                count--;
                if (count <= 0)
                {
                    inventory.Remove(item);
                }
                else
                {
                    inventory[item] = count;
                }

                SaveGameState();
            }

            ShowInventory();
        }

        private IReadOnlyList<InventoryItemStack> GetInventoryStacks()
        {
            return inventory
                .Where(pair => pair.Key != null && pair.Value > 0)
                .Select(pair => new InventoryItemStack(pair.Key, pair.Value))
                .OrderBy(stack => stack.Item.price)
                .ToArray();
        }

        private CombatantDefinition[] GetUnlockedHeroPool()
        {
            return totalHeroPool
                .Where(hero => hero != null && hero.isHero && hero.isUnlocked)
                .OrderBy(GetPreferredRowSortOrder)
                .ThenBy(GetArchetypeSortOrder)
                .ThenBy(hero => hero.displayName)
                .ToArray();
        }

        private void TreatRecoveringHero(CombatantDefinition hero)
        {
            if (hero == null || !hero.isHero)
            {
                ShowRecoveryWard();
                return;
            }

            if (!hero.IsRecovering)
            {
                hudController.AddLog($"{hero.displayName} 当前无需休整。");
                ShowRecoveryWard();
                return;
            }

            if (gold < RecoveryTreatmentCost)
            {
                hudController.AddLog($"金币不足，急救恢复需要 {RecoveryTreatmentCost} 金币。");
                ShowRecoveryWard();
                return;
            }

            gold -= RecoveryTreatmentCost;
            hero.RecoverImmediately(RecoveryTreatmentHealthPercent);
            RebuildDerivedGameState();
            SaveGameState();
            hudController.AddLog($"{hero.displayName} 完成急救恢复，重新回到可出战状态。");
            ShowRecoveryWard();
        }

        private CombatantDefinition[] GetDeployableHeroPool()
        {
            return availableHeroPool
                .Where(hero => hero != null && hero.CanDeploy)
                .OrderBy(GetPreferredRowSortOrder)
                .ThenBy(GetArchetypeSortOrder)
                .ThenBy(hero => hero.displayName)
                .ToArray();
        }

        private IReadOnlyList<CombatantDefinition> GetTavernOffers()
        {
            return totalHeroPool
                .Where(hero => hero != null && hero.isHero && !hero.isUnlocked)
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(3)
                .ToArray();
        }

        private void RefreshTavernOffers()
        {
            tavernOffers.Clear();
            tavernOffers.AddRange(GetTavernOffers());
        }

        private IReadOnlyList<AdventureMapOption> GetAdventureMaps()
        {
            var maps = LoadAdventureMaps();
            if (maps.Length == 0)
            {
                return CreateFallbackAdventureMaps();
            }

            return maps
                .Select(map => new AdventureMapOption(map, map != null && (debugTestingEnabled && debugUnlockAllMaps || map.IsUnlocked(clearedMapIds))))
                .ToArray();
        }

        private void LoadOrCreateGameState()
        {
            totalHeroPool = BuildHeroPool();
            NormalizeHeroPool(totalHeroPool);
            MigrateLegacySaveIfNeeded();

            var startupSlot = FindFirstOccupiedSaveSlot();
            if (startupSlot >= 0)
            {
                LoadGameFromSlot(startupSlot);
                return;
            }

            activeSaveSlotIndex = -1;
            ResetGameState();
        }

        private void ApplySaveData(GameSaveData save)
        {
            clearedMapIds.Clear();
            if (save == null)
            {
                gold = InitialGold;
                return;
            }

            gold = Mathf.Max(0, save.gold);
            var heroStates = save.heroes != null
                ? save.heroes.Where(hero => !string.IsNullOrWhiteSpace(hero.characterId)).ToDictionary(hero => hero.characterId, hero => hero)
                : new Dictionary<string, HeroSaveData>();

            foreach (var hero in totalHeroPool.Where(hero => hero != null && hero.isHero))
            {
                if (!heroStates.TryGetValue(hero.characterId, out var state))
                {
                    hero.recoveryState = HeroRecoveryState.Ready;
                    hero.recoveryBattlesRemaining = 0;
                    hero.EnsureRuntimeHealth();
                    continue;
                }

                hero.isUnlocked = state.unlocked;
                hero.currentLevel = Mathf.Max(1, state.level);
                hero.currentExperience = Mathf.Max(0, state.experience);
                hero.currentHealth = Mathf.Clamp(state.health, 0, hero.MaxHealthWithGrowth);
                hero.specialization = System.Enum.IsDefined(typeof(CombatSpecialization), state.specialization)
                    ? (CombatSpecialization)state.specialization
                    : CombatSpecialization.None;
                hero.growthPassive = System.Enum.IsDefined(typeof(HeroPassive), state.growthPassive)
                    ? (HeroPassive)state.growthPassive
                    : HeroPassive.None;
                hero.recoveryState = System.Enum.IsDefined(typeof(HeroRecoveryState), state.recoveryState)
                    ? (HeroRecoveryState)state.recoveryState
                    : HeroRecoveryState.Ready;
                hero.recoveryBattlesRemaining = Mathf.Max(0, state.recoveryBattlesRemaining);
                if (hero.IsRecovering && hero.currentHealth <= 0)
                {
                    hero.currentHealth = 1;
                }
            }

            if (save.clearedMapIds != null)
            {
                foreach (var mapId in save.clearedMapIds.Where(mapId => !string.IsNullOrWhiteSpace(mapId)))
                {
                    clearedMapIds.Add(mapId.Trim());
                }
            }
        }

        private CombatantDefinition[] BuildHeroPool()
        {
            var configuredPool = heroPoolConfig != null
                ? heroPoolConfig.Where(hero => hero != null).Select(Instantiate).ToArray()
                : System.Array.Empty<CombatantDefinition>();

            return configuredPool.Length > 0
                ? configuredPool
                : LoadHeroPool();
        }

        private void NormalizeHeroPool(IEnumerable<CombatantDefinition> heroesToNormalize)
        {
            if (heroesToNormalize == null)
            {
                return;
            }

            foreach (var hero in heroesToNormalize.Where(hero => hero != null && hero.isHero))
            {
                hero.EnsureRuntimeHealth();
                if (hero.archetype != CombatArchetype.Undefined && hero.preferredRow == CombatRowPreference.Flexible)
                {
                    hero.preferredRow = GetDefaultRowForArchetype(hero.archetype);
                }
            }
        }

        private void SyncSelectedHeroDefinitions(bool fillToMax)
        {
            var deployable = GetDeployableHeroPool();
            selectedHeroDefinitions.RemoveAll(hero => hero == null || !deployable.Contains(hero));

            SortSelectedHeroesInPlace();

            if (!fillToMax || selectedHeroDefinitions.Count >= MaxFormationSlots)
            {
                return;
            }

            foreach (var hero in deployable)
            {
                if (selectedHeroDefinitions.Contains(hero))
                {
                    continue;
                }

                selectedHeroDefinitions.Add(hero);
                if (selectedHeroDefinitions.Count >= MaxFormationSlots)
                {
                    break;
                }
            }

            SortSelectedHeroesInPlace();
        }

        private void SortSelectedHeroesInPlace()
        {
            var ordered = selectedHeroDefinitions
                .Where(hero => hero != null)
                .Distinct()
                .OrderBy(GetPreferredRowSortOrder)
                .ThenBy(GetArchetypeSortOrder)
                .ThenBy(hero => hero.displayName)
                .Take(MaxFormationSlots)
                .ToArray();

            selectedHeroDefinitions.Clear();
            selectedHeroDefinitions.AddRange(ordered);
        }

        private void CommitSelectedPartyToHeroParty()
        {
            SortSelectedHeroesInPlace();
            heroParty = selectedHeroDefinitions.Take(MaxFormationSlots).ToArray();
        }

        private static int GetPreferredRowSortOrder(CombatantDefinition hero)
        {
            if (hero == null)
            {
                return int.MaxValue;
            }

            switch (hero.preferredRow)
            {
                case CombatRowPreference.Front:
                    return 0;
                case CombatRowPreference.Mid:
                    return 1;
                case CombatRowPreference.Flexible:
                    return 2;
                case CombatRowPreference.Back:
                    return 3;
                default:
                    return 4;
            }
        }

        private static int GetArchetypeSortOrder(CombatantDefinition hero)
        {
            if (hero == null)
            {
                return int.MaxValue;
            }

            switch (hero.archetype)
            {
                case CombatArchetype.Bulwark:
                    return 0;
                case CombatArchetype.Executioner:
                    return 1;
                case CombatArchetype.Artificer:
                    return 2;
                case CombatArchetype.Physician:
                    return 3;
                default:
                    return 4;
            }
        }

        private static CombatRowPreference GetDefaultRowForArchetype(CombatArchetype archetype)
        {
            switch (archetype)
            {
                case CombatArchetype.Bulwark:
                    return CombatRowPreference.Front;
                case CombatArchetype.Executioner:
                    return CombatRowPreference.Mid;
                case CombatArchetype.Artificer:
                case CombatArchetype.Physician:
                    return CombatRowPreference.Back;
                default:
                    return CombatRowPreference.Flexible;
            }
        }

        private void SaveGameState()
        {
            if (totalHeroPool == null || totalHeroPool.Length == 0)
            {
                return;
            }

            var slotIndex = ResolveActiveSaveSlotIndex();
            if (slotIndex < 0)
            {
                return;
            }

            var save = new GameSaveData
            {
                gold = Mathf.Max(0, gold),
                savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                clearedMapIds = clearedMapIds.OrderBy(mapId => mapId).ToList(),
                heroes = totalHeroPool
                    .Where(hero => hero != null && hero.isHero)
                    .Select(hero => new HeroSaveData
                    {
                        characterId = hero.characterId,
                        unlocked = hero.isUnlocked,
                        level = hero.Level,
                        experience = hero.Experience,
                        health = hero.CurrentHealth,
                        specialization = (int)hero.specialization,
                        growthPassive = (int)hero.growthPassive,
                        recoveryState = (int)hero.recoveryState,
                        recoveryBattlesRemaining = hero.recoveryBattlesRemaining
                    })
                    .ToList()
            };

            PlayerPrefs.SetString(GetSaveKey(slotIndex), JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        private void ShowContinueGameSlots()
        {
            PrepareNonCombatScreen("\u7ee7\u7eed\u6e38\u620f");
            hudController.ShowSaveSlots("\u9009\u62e9\u8981\u7ee7\u7eed\u7684\u5b58\u6863", GetSaveSlotSummaries(), false, LoadSelectedSaveSlot, ConfirmDeleteFromContinueSlots, ShowTitleScreen);
        }

        private void ShowManualSaveSlots()
        {
            PrepareNonCombatScreen("\u4fdd\u5b58\u6e38\u620f", false);
            hudController.ShowSaveSlots("\u9009\u62e9\u8981\u8986\u76d6\u7684\u5b58\u6863", GetSaveSlotSummaries(), true, ConfirmSaveToSlot, ConfirmDeleteFromManualSlots, ShowSettings);
        }

        private void ConfirmDeleteFromContinueSlots(int slotIndex)
        {
            ConfirmDeleteSlot(slotIndex, ShowContinueGameSlots);
        }

        private void ConfirmDeleteFromManualSlots(int slotIndex)
        {
            ConfirmDeleteSlot(slotIndex, ShowManualSaveSlots);
        }

        private void ConfirmDeleteSlot(int slotIndex, System.Action onReturn)
        {
            var summary = GetSaveSlotSummary(slotIndex);
            if (!summary.HasSave)
            {
                onReturn?.Invoke();
                return;
            }

            hudController.ShowChoicePrompt(
                $"\u786e\u5b9a\u5220\u9664{summary.Title}\u5417\uff1f\n{summary.Detail}",
                "\u53d6\u6d88",
                onReturn,
                "\u786e\u8ba4\u5220\u9664",
                () =>
                {
                    DeleteSaveSlot(slotIndex);
                    onReturn?.Invoke();
                });
        }

        private void DeleteSaveSlot(int slotIndex)
        {
            var key = GetSaveKey(slotIndex);
            if (!PlayerPrefs.HasKey(key))
            {
                return;
            }

            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            if (activeSaveSlotIndex == slotIndex)
            {
                activeSaveSlotIndex = -1;
            }
        }

        private void LoadSelectedSaveSlot(int slotIndex)
        {
            if (!TryReadSaveSlot(slotIndex, out _))
            {
                hudController.ShowContinuePrompt("\u8be5\u5b58\u6863\u4f4d\u4e3a\u7a7a\u3002", "\u8fd4\u56de", ShowContinueGameSlots);
                return;
            }

            LoadGameFromSlot(slotIndex);
            ShowLobby();
        }

        private void ConfirmSaveToSlot(int slotIndex)
        {
            var summary = GetSaveSlotSummary(slotIndex);
            if (!summary.HasSave)
            {
                SaveToSlotAndReturn(slotIndex);
                return;
            }

            hudController.ShowChoicePrompt(
                $"\u786e\u5b9a\u8986\u76d6{summary.Title}\u5417\uff1f\n{summary.Detail}",
                "\u53d6\u6d88",
                ShowManualSaveSlots,
                "\u786e\u8ba4\u8986\u76d6",
                () => SaveToSlotAndReturn(slotIndex));
        }

        private void SaveToSlotAndReturn(int slotIndex)
        {
            activeSaveSlotIndex = Mathf.Clamp(slotIndex, 0, SaveSlotCount - 1);
            SaveGameState();
            ShowSettings();
        }

        private void CreateNewGameInSlot(int slotIndex)
        {
            activeSaveSlotIndex = Mathf.Clamp(slotIndex, 0, SaveSlotCount - 1);
            ResetGameState();
            SaveGameState();
        }

        private void LoadGameFromSlot(int slotIndex)
        {
            totalHeroPool = BuildHeroPool();
            NormalizeHeroPool(totalHeroPool);

            if (TryReadSaveSlot(slotIndex, out var save))
            {
                ApplySaveData(save);
            }
            else
            {
                gold = InitialGold;
            }

            activeSaveSlotIndex = Mathf.Clamp(slotIndex, 0, SaveSlotCount - 1);
            RebuildDerivedGameState();
        }

        private void RebuildDerivedGameState()
        {
            availableHeroPool = GetUnlockedHeroPool();
            SyncSelectedHeroDefinitions(fillToMax: true);
            CommitSelectedPartyToHeroParty();
            RefreshTavernOffers();
            hudController.SetGold(gold);
        }

        private bool HasAnySaveSlots()
        {
            return FindFirstOccupiedSaveSlot() >= 0;
        }

        private int FindFirstOccupiedSaveSlot()
        {
            for (var i = 0; i < SaveSlotCount; i++)
            {
                if (PlayerPrefs.HasKey(GetSaveKey(i)))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindFirstEmptySaveSlot()
        {
            for (var i = 0; i < SaveSlotCount; i++)
            {
                if (!PlayerPrefs.HasKey(GetSaveKey(i)))
                {
                    return i;
                }
            }

            return -1;
        }

        private int ResolveActiveSaveSlotIndex()
        {
            if (activeSaveSlotIndex >= 0 && activeSaveSlotIndex < SaveSlotCount)
            {
                return activeSaveSlotIndex;
            }

            var emptySlot = FindFirstEmptySaveSlot();
            activeSaveSlotIndex = emptySlot >= 0 ? emptySlot : 0;
            return activeSaveSlotIndex;
        }

        private SaveSlotSummary[] GetSaveSlotSummaries()
        {
            return Enumerable.Range(0, SaveSlotCount)
                .Select(GetSaveSlotSummary)
                .ToArray();
        }

        private SaveSlotSummary GetSaveSlotSummary(int slotIndex)
        {
            var title = $"\u5b58\u6863 {slotIndex + 1}";
            if (!TryReadSaveSlot(slotIndex, out var save))
            {
                return new SaveSlotSummary(slotIndex, false, title, "\u7a7a");
            }

            var unlockedCount = save.heroes != null ? save.heroes.Count(hero => hero != null && hero.unlocked) : 0;
            var detail = $"\u91d1\u5e01 {Mathf.Max(0, save.gold)}  \u5df2\u89e3\u9501\u82f1\u96c4 {unlockedCount}";
            if (!string.IsNullOrWhiteSpace(save.savedAt))
            {
                detail = $"{detail}\n\u4fdd\u5b58\u65f6\u95f4 {save.savedAt}";
            }

            return new SaveSlotSummary(slotIndex, true, title, detail);
        }

        private bool TryReadSaveSlot(int slotIndex, out GameSaveData save)
        {
            save = null;
            var key = GetSaveKey(slotIndex);
            if (!PlayerPrefs.HasKey(key))
            {
                return false;
            }

            try
            {
                save = JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString(key));
                return save != null;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Failed to read save slot {slotIndex + 1}. {exception.Message}");
                return false;
            }
        }

        private void MigrateLegacySaveIfNeeded()
        {
            if (!PlayerPrefs.HasKey(LegacySaveKey) || HasAnySaveSlots())
            {
                return;
            }

            PlayerPrefs.SetString(GetSaveKey(0), PlayerPrefs.GetString(LegacySaveKey));
            PlayerPrefs.DeleteKey(LegacySaveKey);
            PlayerPrefs.Save();
        }

        private static string GetSaveKey(int slotIndex)
        {
            return $"{SaveSlotKeyPrefix}{slotIndex}";
        }

        private static InventoryItemData[] LoadShopItems()
        {
            var configuredItems = Resources.LoadAll<InventoryItemData>("Items")
                .Where(item => item != null)
                .OrderBy(item => item.price)
                .ToArray();

            return configuredItems.Length > 0
                ? configuredItems
                : CreateDefaultShopItems();
        }

        private static InventoryItemData[] CreateDefaultShopItems()
        {
            return new[]
            {
                CreateRuntimeItem("small_potion", "\u5c0f\u836f\u6c34", "\u6218\u6597\u5916\u4f7f\u7528\uff0c\u4e3a\u4e00\u540d\u82f1\u96c4\u6062\u590d 20 \u751f\u547d\u3002", 100, InventoryItemEffectType.Heal, 20, 0.3f),
                CreateRuntimeItem("large_potion", "\u5927\u836f\u6c34", "\u6218\u6597\u5916\u4f7f\u7528\uff0c\u4e3a\u4e00\u540d\u82f1\u96c4\u6062\u590d 50 \u751f\u547d\u3002", 250, InventoryItemEffectType.Heal, 50, 0.3f),
                CreateRuntimeItem("revive_potion", "\u590d\u6d3b\u836f\u6c34", "\u6218\u6597\u5916\u4f7f\u7528\uff0c\u590d\u6d3b\u4e00\u540d\u6b7b\u4ea1\u82f1\u96c4\uff0c\u751f\u547d\u6062\u590d\u5230 30%\u3002", 500, InventoryItemEffectType.Revive, 0, 0.3f)
            };
        }

        private static InventoryItemData CreateRuntimeItem(string itemId, string itemName, string description, int price, InventoryItemEffectType effectType, int healAmount, float reviveHealthPercent)
        {
            var item = ScriptableObject.CreateInstance<InventoryItemData>();
            item.itemId = itemId;
            item.itemName = itemName;
            item.description = description;
            item.price = price;
            item.effectType = effectType;
            item.healAmount = healAmount;
            item.reviveHealthPercent = reviveHealthPercent;
            return item;
        }

        private IEnumerator SelectedAdventureLoop()
        {
            var mapData = selectedAdventureMap.Data;
            var battles = mapData != null ? mapData.GetBattles() : System.Array.Empty<AdventureBattleConfig>();
            if (battles.Count == 0)
            {
                currentBattleNumber = 1;
                hudController.AddLog($"\u5192\u9669\u5f00\u59cb\uff1a{selectedAdventureMap.DisplayName}\u3002");
                currentAdventureBattle = null;
                yield return StartCoroutine(RunCombatEncounter(false));
            }
            else
            {
                hudController.AddLog($"\u5192\u9669\u5f00\u59cb\uff1a{selectedAdventureMap.DisplayName}\u3002");
                for (var battleIndex = 0; battleIndex < battles.Count; battleIndex++)
                {
                    currentBattleNumber = battleIndex + 1;
                    currentAdventureBattle = battles[battleIndex];
                    yield return StartCoroutine(RunCombatEncounter(currentAdventureBattle != null && currentAdventureBattle.isBossBattle));

                    if (!heroes.Any(hero => hero.CanAct))
                    {
                        yield return StartCoroutine(ShowDefeatAndReturn());
                        yield break;
                    }
                }
            }

            if (!heroes.Any(hero => hero.CanAct))
            {
                yield return StartCoroutine(ShowDefeatAndReturn());
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(selectedAdventureMap.MapId))
            {
                clearedMapIds.Add(selectedAdventureMap.MapId);
            }

            currentAdventureBattle = null;
            SaveGameState();
            ShowLobby();
        }

        private IEnumerator MapExpeditionLoop()
        {
            currentBattleNumber = 0;
            hudController.AddLog("\u8fdc\u5f81\u5f00\u59cb\u3002");

            for (var mapStep = 1; mapStep <= MapNodeCount; mapStep++)
            {
                var nodeSelected = false;
                var selectedNode = default(MapNodeOption);
                hudController.ClearActionPanels();
                hudController.SetRound(0);
                hudController.SetTurn($"\u5730\u56fe {mapStep}/{MapNodeCount}");
                hudController.ShowMap(mapStep, MapNodeCount, GenerateMapOptions(mapStep), node =>
                {
                    selectedNode = node;
                    nodeSelected = true;
                });

                while (!nodeSelected)
                {
                    yield return null;
                }

                hudController.HideTransientUi();

                if (selectedNode.NodeType == MapNodeType.Battle)
                {
                    currentBattleNumber++;
                    yield return StartCoroutine(RunCombatEncounter(false));

                    if (!heroes.Any(hero => hero.CanAct))
                    {
                        yield return StartCoroutine(ShowDefeatAndReturn());
                        yield break;
                    }
                }
                else if (selectedNode.NodeType == MapNodeType.Rest)
                {
                    yield return StartCoroutine(RunRestNode());
                }
                else if (selectedNode.NodeType == MapNodeType.Chest)
                {
                    yield return StartCoroutine(RunChestNode());
                }
            }

            currentBattleNumber++;
            hudController.AddLog("\u8fdc\u5f81\u8d70\u5230\u5c3d\u5934\uff0cBoss \u51fa\u73b0\u4e86\u3002");
            yield return StartCoroutine(RunCombatEncounter(true));

            if (!heroes.Any(hero => hero.CanAct))
            {
                yield return StartCoroutine(ShowDefeatAndReturn());
                yield break;
            }

            hudController.ClearActionPanels();
            hudController.SetTurn("\u606d\u559c\u901a\u5173");
            hudController.AddLog("Boss \u5df2\u88ab\u51fb\u8d25\uff0c\u8fdc\u5f81\u901a\u5173\u3002");
            var requestedReturn = false;
            hudController.ShowContinuePrompt("\u606d\u559c\u901a\u5173", "\u8fd4\u56de\u5927\u5385", () => requestedReturn = true);
            while (!requestedReturn)
            {
                yield return null;
            }

            SaveGameState();
            ShowLobby();
        }

        private IEnumerator RunCombatEncounter(bool bossBattle)
        {
            ironWillUsedThisBattle.Clear();
            CombatAudio.Instance.PlayBossMusic();
            PrepareBattle(currentBattleNumber, bossBattle);
            yield return StartCoroutine(BattleLoop());
            CombatAudio.Instance.StopMusic();

            if (!heroes.Any(hero => hero.CanAct))
            {
                yield break;
            }

            var rewardResults = GrantVictoryRewards(out var goldGained);
            var continueRequested = false;
            hudController.SetTurn(bossBattle ? "Boss \u6218\u80dc\u5229" : "\u6218\u6597\u80dc\u5229");
            hudController.ShowRewardScreen(goldGained, gold, rewardResults, () => continueRequested = true);
            hudController.AddLog(bossBattle ? "Boss \u6218\u80dc\u5229\u3002" : "\u6218\u6597\u80dc\u5229\uff0c\u961f\u4f0d\u72b6\u6001\u5df2\u4fdd\u7559\u3002");

            while (!continueRequested)
            {
                yield return null;
            }

            yield return StartCoroutine(ResolvePendingLevelUpChoices(rewardResults));
        }

        private IEnumerator RunRestNode()
        {
            var selected = false;
            hudController.SetTurn("\u4f11\u606f");
            hudController.ShowRestNode(selectedHeroDefinitions, hero =>
            {
                if (hero != null)
                {
                    var healed = hero.HealOutsideBattle(20);
                    if (healed > 0)
                    {
                        CombatAudio.Instance.PlayHeal();
                    }

                    hudController.AddLog(healed > 0
                        ? $"{hero.displayName} \u5728\u4f11\u606f\u4e2d\u6062\u590d\u4e86 {healed} \u70b9\u751f\u547d\u3002"
                        : $"{hero.displayName} \u73b0\u5728\u65e0\u9700\u4f11\u606f\u3002");
                    SaveGameState();
                }

                selected = true;
            });

            while (!selected)
            {
                yield return null;
            }

            hudController.HideTransientUi();
        }

        private static IReadOnlyList<MapNodeOption> GenerateMapOptions(int mapStep)
        {
            var options = new List<MapNodeOption>
            {
                new MapNodeOption(MapNodeType.Battle, "\u6218\u6597\u8282\u70b9", "\u906d\u9047\u4e00\u652f\u968f\u673a\u654c\u4eba\u961f\u4f0d\u3002")
            };

            if (mapStep % 2 == 0)
            {
                options.Add(new MapNodeOption(MapNodeType.Chest, "\u5b9d\u7bb1\u8282\u70b9", "\u83b7\u5f97 50-150 \u91d1\u5e01\u3002"));
                options.Add(new MapNodeOption(MapNodeType.Rest, "\u4f11\u606f\u8282\u70b9", "\u9009\u62e9\u4e00\u540d\u82f1\u96c4\u6062\u590d 20 \u751f\u547d\u3002"));
            }
            else
            {
                options.Add(new MapNodeOption(MapNodeType.Rest, "\u4f11\u606f\u8282\u70b9", "\u9009\u62e9\u4e00\u540d\u82f1\u96c4\u6062\u590d 20 \u751f\u547d\u3002"));
                if (UnityEngine.Random.value >= 0.45f)
                {
                    options.Add(new MapNodeOption(MapNodeType.Chest, "\u5b9d\u7bb1\u8282\u70b9", "\u83b7\u5f97 50-150 \u91d1\u5e01\u3002"));
                }
            }

            return options;
        }

        private IEnumerator RunChestNode()
        {
            var gained = ApplyDebugGoldMultiplier(UnityEngine.Random.Range(50, 151), debugChestGoldMultiplier);
            gold += gained;
            hudController.SetGold(gold);
            hudController.AddLog($"\u6253\u5f00\u5b9d\u7bb1\uff0c\u83b7\u5f97\u91d1\u5e01 {gained}\u3002");
            SaveGameState();

            var continueRequested = false;
            hudController.ShowContinuePrompt($"\u5b9d\u7bb1\uff1a\u83b7\u5f97 {gained} \u91d1\u5e01", "\u7ee7\u7eed", () => continueRequested = true);
            while (!continueRequested)
            {
                yield return null;
            }
        }

        private IEnumerator ShowDefeatAndReturn()
        {
            FinalizeHeroCasualtiesFromCurrentBattle();
            RebuildDerivedGameState();
            hudController.ClearActionPanels();
            hudController.SetTurn("\u6218\u8d25");
            hudController.AddLog("\u8fdc\u5f81\u961f\u5012\u4e0b\u4e86\uff0c\u8fdc\u5f81\u7ec8\u6b62\u3002");
            var returnRequested = false;
            hudController.ShowContinuePrompt("\u6218\u8d25", "\u8fd4\u56de\u5927\u5385", () => returnRequested = true);
            while (!returnRequested)
            {
                yield return null;
            }

            SaveGameState();
            ShowLobby();
        }

        private IReadOnlyList<BattleRewardResult> GrantVictoryRewards(out int goldGained)
        {
            AdvanceRecoveryProgress(1);
            FinalizeHeroCasualtiesFromCurrentBattle();
            var rewardMin = currentAdventureBattle != null ? currentAdventureBattle.goldRewardMin : 50;
            var rewardMax = currentAdventureBattle != null ? currentAdventureBattle.goldRewardMax : 150;
            goldGained = ApplyDebugGoldMultiplier(UnityEngine.Random.Range(Mathf.Min(rewardMin, rewardMax), Mathf.Max(rewardMin, rewardMax) + 1), debugGoldRewardMultiplier);
            gold += goldGained;
            hudController.SetGold(gold);
            hudController.AddLog($"\u83b7\u5f97\u91d1\u5e01 {goldGained}\u3002");

            var results = new List<BattleRewardResult>();
            foreach (var hero in heroes.Where(unit => unit.IsHero && unit.IsAlive && !unit.IsCorpse))
            {
                var expMin = currentAdventureBattle != null ? currentAdventureBattle.experienceRewardMin : 10;
                var expMax = currentAdventureBattle != null ? currentAdventureBattle.experienceRewardMax : 20;
                var grantedExperience = ApplyDebugExperienceMultiplier(UnityEngine.Random.Range(Mathf.Min(expMin, expMax), Mathf.Max(expMin, expMax) + 1));
                var progression = hero.Definition.GrantExperienceReward(grantedExperience);
                results.Add(new BattleRewardResult(hero.Definition, progression.ExperienceGained, progression.LevelsGained));
                hudController.AddLog($"{hero.Definition.displayName} \u83b7\u5f97 {progression.ExperienceGained} \u7ecf\u9a8c\u3002");

                if (progression.HealthRestoredFromGrowth > 0)
                {
                    hero.RestoreForMaxHealthGain(progression.HealthRestoredFromGrowth);
                }

                if (progression.LevelsGained > 0)
                {
                    for (var level = progression.LevelBefore + 1; level <= progression.LevelAfter; level++)
                    {
                        hudController.AddLog($"{hero.Definition.displayName} \u5347\u5230\u4e86 {level} \u7ea7\u3002");
                    }
                }
            }

            RefreshViews();
            RebuildDerivedGameState();
            SaveGameState();
            return results;
        }

        private void FinalizeHeroCasualtiesFromCurrentBattle()
        {
            foreach (var hero in heroes.Where(unit => unit != null && unit.IsHero))
            {
                if (!hero.IsAlive)
                {
                    hero.Definition.MarkWounded(WoundedRecoveryBattleCount, WoundedStabilizedHealthPercent);
                }
            }
        }

        private void AdvanceRecoveryProgress(int completedBattles)
        {
            if (completedBattles <= 0 || totalHeroPool == null)
            {
                return;
            }

            foreach (var hero in totalHeroPool.Where(hero => hero != null && hero.isHero && hero.IsRecovering))
            {
                if (hero.AdvanceRecovery(completedBattles))
                {
                    hudController.AddLog($"{hero.displayName} 完成休养，重新可出战。");
                }
            }
        }

        private IEnumerator ResolvePendingLevelUpChoices(IReadOnlyList<BattleRewardResult> rewardResults)
        {
            if (rewardResults == null || rewardResults.Count == 0)
            {
                yield break;
            }

            foreach (var result in rewardResults)
            {
                var hero = result.Hero;
                if (hero == null || result.LevelsGained <= 0)
                {
                    continue;
                }

                if (hero.specialization == CombatSpecialization.None && hero.Level >= 2)
                {
                    if (TryGetSpecializationChoices(hero.archetype, out var left, out var right))
                    {
                        var resolved = false;
                        var selected = CombatSpecialization.None;
                        var presentation = CreateSpecializationLevelUpPresentation(hero, left, right);

                        hudController.ShowLevelUpSelection(
                            presentation,
                            option =>
                            {
                                selected = option != null ? option.Specialization : CombatSpecialization.None;
                                resolved = selected != CombatSpecialization.None;
                            });

                        while (!resolved)
                        {
                            yield return null;
                        }

                        hero.specialization = selected;
                        hudController.AddLog($"{hero.displayName} 选择了专精：{hero.SpecializationDisplayName}");
                        SaveGameState();
                    }
                }

                if (hero.growthPassive == HeroPassive.None && hero.Level >= 3)
                {
                    var passiveChoices = HeroProgressionDescriptions.GetLevelThreePassiveChoices(hero);
                    if (passiveChoices != null && passiveChoices.Length > 0)
                    {
                        var resolved = false;
                        var selected = HeroPassive.None;
                        var presentation = CreatePassiveLevelUpPresentation(hero, passiveChoices);

                        hudController.ShowLevelUpSelection(
                            presentation,
                            option =>
                            {
                                selected = option != null ? option.Passive : HeroPassive.None;
                                resolved = selected != HeroPassive.None;
                            });

                        while (!resolved)
                        {
                            yield return null;
                        }

                        hero.growthPassive = selected;
                        hudController.AddLog($"{hero.displayName} 获得了成长被动：{HeroProgressionDescriptions.GetPassiveDisplayName(selected)}");
                        SaveGameState();
                    }
                }
            }
        }

        private static bool TryGetSpecializationChoices(CombatArchetype archetype, out CombatSpecialization left, out CombatSpecialization right)
        {
            switch (archetype)
            {
                case CombatArchetype.Bulwark:
                    left = CombatSpecialization.Bastion;
                    right = CombatSpecialization.Sentinel;
                    return true;
                case CombatArchetype.Executioner:
                    left = CombatSpecialization.Slayer;
                    right = CombatSpecialization.Breaker;
                    return true;
                case CombatArchetype.Artificer:
                    left = CombatSpecialization.Bombardier;
                    right = CombatSpecialization.Controller;
                    return true;
                case CombatArchetype.Physician:
                    left = CombatSpecialization.Surgeon;
                    right = CombatSpecialization.Stimulator;
                    return true;
                default:
                    left = CombatSpecialization.None;
                    right = CombatSpecialization.None;
                    return false;
            }
        }

        private static LevelUpPresentation CreateSpecializationLevelUpPresentation(CombatantDefinition hero, CombatSpecialization left, CombatSpecialization right)
        {
            var options = new[]
            {
                CreateSpecializationLevelUpOption(left),
                CreateSpecializationLevelUpOption(right)
            };

            return new LevelUpPresentation(
                hero,
                $"{hero.displayName} \u5347\u7ea7\u6210\u957f",
                $"\u5f53\u524d\u7b49\u7ea7 Lv.{hero.Level}  \u8bf7\u9009\u62e9\u672c\u6b21\u7684\u4e13\u7cbe\u5206\u652f",
                options);
        }

        private static LevelUpOptionData CreateSpecializationLevelUpOption(CombatSpecialization specialization)
        {
            return new LevelUpOptionData(
                specialization.ToString(),
                LevelUpOptionType.Specialization,
                CombatantDefinition.GetSpecializationDisplayName(specialization),
                HeroProgressionDescriptions.GetSpecializationTrackLabel(specialization),
                CombatantDefinition.GetSpecializationSummary(specialization),
                HeroProgressionDescriptions.GetSpecializationLongDescription(specialization),
                HeroProgressionDescriptions.GetSpecializationTags(specialization),
                specialization: specialization);
        }

        private static LevelUpPresentation CreatePassiveLevelUpPresentation(CombatantDefinition hero, IReadOnlyList<HeroPassive> choices)
        {
            var options = choices
                .Where(choice => choice != HeroPassive.None)
                .Select(CreatePassiveLevelUpOption)
                .ToArray();

            return new LevelUpPresentation(
                hero,
                "3级成长选择",
                "选择一个成长被动，决定这名角色的中期方向。",
                options);
        }

        private static LevelUpOptionData CreatePassiveLevelUpOption(HeroPassive passive)
        {
            return new LevelUpOptionData(
                passive.ToString(),
                LevelUpOptionType.Passive,
                HeroProgressionDescriptions.GetPassiveDisplayName(passive),
                "3级成长 / 被动选择",
                HeroProgressionDescriptions.GetPassiveDescription(passive),
                HeroProgressionDescriptions.GetPassiveDesignNote(passive),
                HeroProgressionDescriptions.GetPassiveTags(passive),
                passive: passive);
        }

        public void DebugResetCurrentRunWithTesting()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Debug reset only works in Play Mode.", this);
                return;
            }

            ResetGameState();
            SaveGameState();
            ShowLobby();
            hudController?.AddLog("\u8C03\u8BD5\uFF1A\u5DF2\u91CD\u7F6E\u8FD0\u884C\u72B6\u6001\u5E76\u5E94\u7528\u6D4B\u8BD5\u53C2\u6570\u3002");
        }

        public void DebugGrantExperienceToCurrentHeroes()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Debug experience grant only works in Play Mode.", this);
                return;
            }

            if (heroes.Count > 0 || enemies.Count > 0)
            {
                Debug.LogWarning("Debug experience grant is only supported outside active combat.", this);
                return;
            }

            StartCoroutine(DebugGrantExperienceRoutine());
        }

        private IEnumerator DebugGrantExperienceRoutine()
        {
            var targetHeroes = (selectedHeroDefinitions.Count > 0 ? selectedHeroDefinitions : GetDeployableHeroPool().ToList())
                .Where(hero => hero != null && hero.isHero && hero.isUnlocked)
                .Distinct()
                .ToArray();

            if (targetHeroes.Length == 0)
            {
                hudController?.AddLog("\u8C03\u8BD5\uFF1A\u5F53\u524D\u6CA1\u6709\u53EF\u53D1\u7ECF\u9A8C\u7684\u82F1\u96C4\u3002");
                yield break;
            }

            var amount = Mathf.Max(1, debugManualExperienceGrant);
            var results = new List<BattleRewardResult>();
            foreach (var hero in targetHeroes)
            {
                var progression = hero.GrantExperienceReward(amount);
                results.Add(new BattleRewardResult(hero, progression.ExperienceGained, progression.LevelsGained));
                hudController?.AddLog($"{hero.displayName} \u83B7\u5F97\u8C03\u8BD5\u7ECF\u9A8C {progression.ExperienceGained}\u3002");
            }

            SaveGameState();
            yield return StartCoroutine(ResolvePendingLevelUpChoices(results));
            ShowLobby();
        }

        private int GetInitialGoldForCurrentMode()
        {
            if (!debugTestingEnabled || debugStartingGoldOverride < 0)
            {
                return InitialGold;
            }

            return Mathf.Max(0, debugStartingGoldOverride);
        }

        private void ApplyDebugNewGameOverrides(IEnumerable<CombatantDefinition> heroesToConfigure)
        {
            if (!debugTestingEnabled || heroesToConfigure == null)
            {
                return;
            }

            foreach (var hero in heroesToConfigure.Where(hero => hero != null && hero.isHero))
            {
                if (debugUnlockAllHeroesOnNewGame)
                {
                    hero.isUnlocked = true;
                }

                hero.currentLevel = Mathf.Max(1, debugNewGameHeroLevel);
                hero.currentExperience = 0;
                hero.currentHealth = hero.MaxHealthWithGrowth;
                hero.specialization = CombatSpecialization.None;
                hero.growthPassive = HeroPassive.None;
                hero.recoveryState = HeroRecoveryState.Ready;
                hero.recoveryBattlesRemaining = 0;
            }
        }

        private int ApplyDebugExperienceMultiplier(int baseValue)
        {
            return ApplyRewardMultiplier(baseValue, debugExperienceRewardMultiplier);
        }

        private int ApplyDebugGoldMultiplier(int baseValue, float multiplier)
        {
            return ApplyRewardMultiplier(baseValue, multiplier);
        }

        private int ApplyRewardMultiplier(int baseValue, float configuredMultiplier)
        {
            if (!debugTestingEnabled)
            {
                return baseValue;
            }

            var multiplier = Mathf.Max(0.1f, configuredMultiplier);
            return Mathf.Max(0, Mathf.RoundToInt(baseValue * multiplier));
        }

        private IEnumerator BattleLoop()
        {
            hudController.AddLog($"\u7b2c {currentBattleNumber} \u573a\u6218\u6597\u5f00\u59cb\u3002");

            while (!IsBattleOver())
            {
                round++;
                hudController.SetRound(round);
                RebuildTurnQueue();

                foreach (var actor in turnQueue.ToArray())
                {
                    if (!actor.CanAct || IsBattleOver())
                    {
                        continue;
                    }

                    currentActor = actor;
                    UpdateCurrentActorIndicator(actor, false);
                    actor.ResetActionPoints();
                    yield return StartCoroutine(RunTurn(actor));
                }
            }

            ResetCombatRuntimeState(clearTurnQueue: true);
            hudController.ClearActionPanels();
            hudController.SetTurn(heroes.Any(hero => hero.CanAct) ? "\u80dc\u5229" : "\u5931\u8d25");
            hudController.AddLog(heroes.Any(hero => hero.CanAct) ? "\u5c0f\u961f\u6491\u8fc7\u4e86\u8fd9\u573a\u906d\u9047\u3002" : "\u8fdc\u5f81\u961f\u5012\u4e0b\u4e86\u3002");
        }

        private IEnumerator RunTurn(BattleUnit actor)
        {
            var stunnedAtTurnStart = actor.IsStunned;
            var statusTicks = actor.TickStatuses();
            var statusDamage = statusTicks.Sum(result => result.Damage);
            if (!actor.IsAlive)
            {
                HandleDefeatedUnit(actor, null);
            }

            RefreshViews();

            if (statusDamage > 0)
            {
                if (views.TryGetValue(actor, out var actorView))
                {
                    foreach (var tick in statusTicks)
                    {
                        if (tick.Status != null)
                        {
                            actorView.ShowFloatingText(tick.Status.DisplayName, tick.Status.NameTextColor, 0.8f);
                        }

                        actorView.ShowFloatingText($"-{tick.Damage}", tick.Status != null ? tick.Status.DamageTextColor : Color.red, 0.95f);
                    }
                }

                hudController.AddLog($"{actor.DisplayName} \u53d7\u5230 {statusDamage} \u70b9\u72b6\u6001\u4f24\u5bb3\u3002");
                yield return new WaitForSeconds(0.45f);
            }

            if (stunnedAtTurnStart || !actor.CanAct)
            {
                if (stunnedAtTurnStart)
                {
                    hudController.AddLog($"{actor.DisplayName} \u88ab\u7729\u6655\uff0c\u65e0\u6cd5\u884c\u52a8\u3002");
                }

                actor.TickSkillCooldowns();
                ClearCurrentActorIndicator();
                yield break;
            }

            actor.TickSkillCooldowns();
            ApplyPassiveOnTurnStart(actor);
            ApplyArchetypeTurnStart(actor);

            if (!actor.IsAlive || !actor.CanAct)
            {
                ClearCurrentActorIndicator();
                RefreshViews();
                yield break;
            }

            if (actor.IsHero)
            {
                UpdateCurrentActorIndicator(actor, true);
                waitingForPlayer = true;
                selectedUnit = actor;
                selectedSkill = null;
                validSelectedTargets = new BattleUnit[0];
                SetTargetHighlights(validSelectedTargets);
                hudController.RenderPlayerTurn(actor, GetSkillUseStates(actor), SelectSkill);

                while (waitingForPlayer)
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(0.45f);
                var skill = SelectEnemySkill(actor);
                var target = skill != null ? SelectEnemyTarget(actor, skill) : null;
                if (skill != null && target != null)
                {
                    yield return StartCoroutine(ExecuteSkill(actor, skill, target));
                }
            }

            ClearCurrentActorIndicator();
        }

        private void SelectSkill(SkillData skill)
        {
            if (!waitingForPlayer || resolvingPlayerAction || currentActor == null || selectedUnit != currentActor)
            {
                return;
            }

            var state = GetSkillUseState(currentActor, skill);
            if (!state.CanUse)
            {
                hudController.AddLog($"\u65e0\u6cd5\u4f7f\u7528 {skill.skillName}\uff1a{state.DisabledReason}\u3002");
                hudController.RenderUnitPanel(selectedUnit, currentActor, GetSkillUseStates(selectedUnit), SelectSkill);
                return;
            }

            selectedSkill = skill;
            validSelectedTargets = GetValidTargets(currentActor, skill).ToArray();
            SetTargetHighlights(validSelectedTargets);

            if (validSelectedTargets.Length == 1 && skill.targetType == SkillDataTargetType.自己)
            {
                SelectTarget(validSelectedTargets[0]);
                return;
            }

            hudController.RenderTargets(skill, validSelectedTargets, SelectTarget);
        }

        private void HandleUnitClicked(BattleUnit unit)
        {
            if (resolvingPlayerAction || unit == null || !unit.IsAlive)
            {
                return;
            }

            if (waitingForPlayer && selectedSkill != null && validSelectedTargets.Contains(unit))
            {
                SelectTarget(unit);
                return;
            }

            if (waitingForPlayer && selectedSkill != null)
            {
                hudController.AddLog("\u8be5\u6280\u80fd\u65e0\u6cd5\u653b\u51fb\u8fd9\u4e2a\u76ee\u6807\u3002");
                SetTargetHighlights(validSelectedTargets);
                return;
            }

            selectedUnit = unit;
            selectedSkill = null;
            validSelectedTargets = new BattleUnit[0];
            SetTargetHighlights(validSelectedTargets);
            hudController.RenderUnitPanel(selectedUnit, waitingForPlayer ? currentActor : null, GetSkillUseStates(selectedUnit), SelectSkill);
        }

        private void SelectTarget(BattleUnit target)
        {
            if (!waitingForPlayer || resolvingPlayerAction || currentActor == null || selectedSkill == null)
            {
                return;
            }

            if (!IsValidTarget(currentActor, selectedSkill, target))
            {
                hudController.AddLog("\u8be5\u76ee\u6807\u5df2\u4e0d\u518d\u5408\u6cd5\uff0c\u8bf7\u91cd\u65b0\u9009\u62e9\u3002");
                validSelectedTargets = GetValidTargets(currentActor, selectedSkill).ToArray();
                SetTargetHighlights(validSelectedTargets);
                return;
            }

            StartCoroutine(ResolvePlayerAction(target));
        }

        private IEnumerator ResolvePlayerAction(BattleUnit target)
        {
            resolvingPlayerAction = true;
            hudController.ClearActionPanels();
            SetTargetHighlights(new BattleUnit[0]);
            yield return StartCoroutine(ExecuteSkill(currentActor, selectedSkill, target));
            validSelectedTargets = new BattleUnit[0];
            selectedSkill = null;
            resolvingPlayerAction = false;
            waitingForPlayer = false;
        }

        private IEnumerator ExecuteSkill(BattleUnit actor, SkillData skill, BattleUnit primaryTarget)
        {
            if (!actor.CanAct || !GetSkillUseState(actor, skill).CanUse || !IsValidTarget(actor, skill, primaryTarget))
            {
                hudController.AddLog("\u884c\u52a8\u5df2\u5931\u6548\uff0c\u8bf7\u91cd\u65b0\u9009\u62e9\u3002");
                yield break;
            }

            if (skill.isSwapSkill)
            {
                yield return StartCoroutine(ExecuteSwap(actor, primaryTarget, skill));
                yield break;
            }

            if (skill.isPassSkill)
            {
                hudController.AddLog($"{actor.DisplayName} ѡ���������غϡ�");
                yield break;
            }

            var targets = ResolveTargets(actor, skill, primaryTarget).Where(target => IsValidTarget(actor, skill, target)).ToArray();
            if (targets.Length == 0)
            {
                yield break;
            }

            if (!views.TryGetValue(actor, out var actorView))
            {
                yield break;
            }

            actor.SpendResourcesFor(skill);
            var mainTarget = targets[0];
            var useImpactPresentation = IsImpactPresentationSkill(skill);
            var useFriendlyAdvancePresentation = ShouldUseFriendlyAdvancePresentation(actor, skill, mainTarget);
            var useAdvancePresentation = useImpactPresentation || useFriendlyAdvancePresentation;
            views.TryGetValue(mainTarget, out var mainTargetView);
            var targetViews = targets
                .Select(target => views.TryGetValue(target, out var view) ? view : null)
                .Where(view => view != null)
                .ToArray();
            SetTargetHighlights(new[] { mainTarget });

            var lungeState = default(AttackLungeState);
            if (useAdvancePresentation)
            {
                yield return StartCoroutine(BeginAttackFocus(actorView, mainTargetView, targetViews));
                lungeState = CreateAttackLungeState(actorView, targetViews, useImpactPresentation);
                yield return StartCoroutine(MoveAttackFocusLunge(lungeState, true));
                yield return new WaitForSecondsRealtime(attackFocusHitPause);
            }

            var overlayDuration = Mathf.Max(MinCombatOverlayDuration, skill.overlayDuration);
            Coroutine presentationCoroutine = null;
            if (useImpactPresentation && mainTargetView != null)
            {
                CombatAudio.Instance.PlayAttack(skill);
                var attackSprite = ResolveAttackSprite(actor, skill);
                var hitSprites = targets
                    .Select(target => ResolveHitSprite(target, skill))
                    .ToArray();
                presentationCoroutine = StartCoroutine(PlayAttackAndHitOverlays(actorView, targetViews, attackSprite, hitSprites, overlayDuration, lungeState));
            }
            else if (useImpactPresentation)
            {
                CombatAudio.Instance.PlayAttack(skill);
                presentationCoroutine = StartCoroutine(actorView.PlayOverlay(ResolveAttackSprite(actor, skill), overlayDuration));
            }
            else if (useFriendlyAdvancePresentation)
            {
                CombatAudio.Instance.PlayAttack(skill);
                presentationCoroutine = StartCoroutine(actorView.PlayOverlay(ResolveAttackSprite(actor, skill), overlayDuration));
            }

            Coroutine shakeCoroutine = null;
            if (useImpactPresentation)
            {
                shakeCoroutine = StartCoroutine(ScreenShake(GetShakeStrength(skill), Mathf.Clamp(skill.baseValue * 0.015f, 0.1f, 0.2f)));
            }

            foreach (var target in targets)
            {
                if (skill.skillType == SkillDataType.伤害 || skill.skillType == SkillDataType.控制)
                {
                    if (skill.baseValue > 0)
                    {
                        var damage = CalculateDamage(actor, skill, target);
                        var protector = FindBodyguardProtector(actor, target);
                        var protectorDamage = 0;
                        var targetDamage = damage.Amount;
                        if (protector != null)
                        {
                            var protectionRatio = protector.Specialization == CombatSpecialization.Bastion ? 0.5f : 0.3f;
                            protectorDamage = Mathf.Max(1, Mathf.RoundToInt(damage.Amount * protectionRatio));
                            targetDamage = Mathf.Max(1, damage.Amount - protectorDamage);
                            hudController.AddLog($"{protector.DisplayName} ͦ����� {target.DisplayName}���ֵ��� {protectorDamage} ���˺���");
                            ApplyRawDamage(protector, protectorDamage, actor);
                        }

                        var ironWillTriggered = ApplyPassiveBeforeDeath(target, targetDamage);
                        if (!ironWillTriggered)
                        {
                            ApplyRawDamage(target, targetDamage, actor);
                        }

                        hudController.AddLog(damage.IsCritical
                            ? $"{actor.DisplayName} \u4f7f\u7528 {skill.skillName}\u66b4\u51fb\uff01{target.DisplayName} \u53d7\u5230 {targetDamage} \u70b9\u4f24\u5bb3\u3002"
                            : $"{actor.DisplayName} \u4f7f\u7528 {skill.skillName}\uff0c{target.DisplayName} \u53d7\u5230 {targetDamage} \u70b9\u4f24\u5bb3\u3002");

                        if (views.TryGetValue(target, out var targetView))
                        {
                            targetView.ShowFloatingText(damage.IsCritical ? $"-{targetDamage}!" : $"-{targetDamage}", Color.red, damage.IsCritical ? 1.55f : 1f);
                            if (!target.IsAlive)
                            {
                                yield return StartCoroutine(targetView.PlayDeathCue());
                                ApplyPassiveOnKill(actor, target);
                                HandleDefeatedUnit(target, skill);
                            }
                        }

                        if (protector != null && views.TryGetValue(protector, out var protectorView))
                        {
                            protectorView.ShowFloatingText($"-{protectorDamage}", new Color(0.95f, 0.45f, 0.12f));
                            if (!protector.IsAlive)
                            {
                                yield return StartCoroutine(protectorView.PlayDeathCue());
                                ApplyPassiveOnKill(actor, protector);
                                HandleDefeatedUnit(protector, skill);
                            }
                        }
                    }
                }
                else
                {
                    var amount = CalculateHealingAmount(actor, skill, target);
                    target.Heal(amount);
                    CombatAudio.Instance.PlayAttack(skill);
                    hudController.AddLog($"{actor.DisplayName} \u4f7f\u7528 {skill.skillName}\uff0c{target.DisplayName} \u6062\u590d {amount} \u70b9\u751f\u547d\u3002");

                    if (views.TryGetValue(target, out var targetView))
                    {
                        targetView.ShowFloatingText("+" + amount, GetDamageColor(skill.skillType));
                    }
                }

                if (skill.applyBuff != null && target.CanAct)
                {
                    target.AddOrRefreshBuff(skill.applyBuff, Mathf.Max(1, skill.applyBuffDuration));
                    if (views.TryGetValue(target, out var buffTargetView))
                    {
                        buffTargetView.Refresh();
                        buffTargetView.ShowFloatingText(skill.applyBuff.buffName, skill.applyBuff.nameTextColor, 0.85f);
                    }

                    hudController.AddLog($"{target.DisplayName} \u83b7\u5f97\u72b6\u6001\uff1a{skill.applyBuff.buffName}\u3002");
                }

                ApplySkillSpecificPostEffect(actor, skill, target);
            }

            if (presentationCoroutine != null)
            {
                yield return presentationCoroutine;
            }

            if (shakeCoroutine != null)
            {
                yield return shakeCoroutine;
            }

            ApplySkillSpecificActionReward(actor, skill, targets);
            ApplySkillSpecificActionRisk(actor, skill, targets);
            ApplyArchetypeActionResource(actor, skill, targets);

            if (useAdvancePresentation)
            {
                yield return StartCoroutine(MoveAttackFocusLungeBack(lungeState));
            }

            if (useAdvancePresentation)
            {
                yield return StartCoroutine(EndAttackFocus(actorView, targetViews));
            }

            RefreshViews();
            LayoutFormation(true);
            LayoutFormation(false);

            SetTargetHighlights(new BattleUnit[0]);
            yield return new WaitForSeconds(0.25f);
        }

        private IEnumerator ExecuteSwap(BattleUnit actor, BattleUnit target, SkillData skill)
        {
            if (actor == target)
            {
                hudController.AddLog("\u4e0d\u80fd\u548c\u81ea\u5df1\u6362\u4f4d\u3002");
                yield break;
            }

            actor.SpendResourcesFor(skill);
            var slots = actor.IsHero ? heroSlots : enemySlots;
            var actorIndex = actor.CurrentPosition - 1;
            var targetIndex = target.CurrentPosition - 1;
            var actorTargetPosition = target.CurrentPosition;
            var targetTargetPosition = actor.CurrentPosition;
            slots[actorIndex] = target;
            slots[targetIndex] = actor;

            actor.CurrentPosition = actorTargetPosition;
            target.CurrentPosition = targetTargetPosition;
            yield return StartCoroutine(AnimateFormation(actor.IsHero));

            RefreshViews();
            hudController.AddLog($"{actor.DisplayName} \u548c {target.DisplayName} \u4ea4\u6362\u4e86\u7ad9\u4f4d\u3002");
        }

        private IEnumerator AnimateFormation(bool isHero)
        {
            const float duration = 0.28f;
            var slots = isHero ? heroSlots : enemySlots;
            var moves = new List<Coroutine>();

            for (var i = 0; i < slots.Length; i++)
            {
                var unit = slots[i];
                if (unit == null || !views.TryGetValue(unit, out var view) || !view.gameObject.activeSelf)
                {
                    continue;
                }

                unit.CurrentPosition = i + 1;
                var targetX = GetFormationSlotX(isHero, slots, unit.CurrentPosition);
                moves.Add(view.StartCoroutine(view.MoveToFormation(targetX, combatantY, duration)));
            }

            foreach (var move in moves)
            {
                yield return move;
            }
        }

        private SkillUseState GetSkillUseState(BattleUnit actor, SkillData skill)
        {
            if (actor == null || skill == null || !actor.CanAct)
            {
                return new SkillUseState(skill, false, "\u65e0\u6cd5\u884c\u52a8");
            }

            if (!IsPositionAllowed(actor.CurrentPosition, skill.casterAllowedPositions))
            {
                return new SkillUseState(skill, false, "\u7ad9\u4f4d\u9519\u8bef");
            }

            var cooldown = actor.GetCooldownRemaining(skill);
            if (cooldown > 0)
            {
                return new SkillUseState(skill, false, $"{cooldown}\u56de\u5408");
            }

            if (!GetValidTargets(actor, skill).Any())
            {
                return new SkillUseState(skill, false, "\u65e0\u6709\u6548\u76ee\u6807");
            }

            return new SkillUseState(skill, true, string.Empty);
        }

        private IReadOnlyList<SkillUseState> GetSkillUseStates(BattleUnit unit)
        {
            if (unit == null || !unit.IsHero)
            {
                return new SkillUseState[0];
            }

            return GetSkillList(unit).Select(skill => GetSkillUseState(unit, skill)).ToArray();
        }

        private IEnumerable<SkillData> GetSkillList(BattleUnit unit)
        {
            var definedSkills = unit?.Definition.skills != null
                ? unit.Definition.skills.Where(skill => skill != null)
                : Enumerable.Empty<SkillData>();

            if (unit == null)
            {
                return definedSkills;
            }

            return unit.IsHero
                ? definedSkills.Concat(new[] { swapSkill, passTurnSkill })
                : definedSkills.Concat(new[] { passTurnSkill });
        }

        private SkillData SelectEnemySkill(BattleUnit actor)
        {
            var usableSkills = GetSkillList(actor)
                .Where(skill => skill != null && GetSkillUseState(actor, skill).CanUse)
                .ToArray();
            if (usableSkills.Length == 0)
            {
                return null;
            }

            return usableSkills
                .OrderByDescending(skill => ScoreEnemySkill(actor, skill))
                .ThenBy(skill => skill.skillName)
                .FirstOrDefault();
        }

        private BattleUnit SelectEnemyTarget(BattleUnit actor, SkillData skill)
        {
            var validTargets = GetValidTargets(actor, skill).ToArray();
            if (validTargets.Length == 0)
            {
                return null;
            }

            return validTargets
                .OrderByDescending(target => ScoreEnemyTarget(actor, skill, target))
                .ThenBy(target => target.CurrentPosition)
                .FirstOrDefault();
        }

        private float ScoreEnemySkill(BattleUnit actor, SkillData skill)
        {
            if (skill != null && skill.isPassSkill)
            {
                return -10000f;
            }

            var validTargets = GetValidTargets(actor, skill).ToArray();
            if (validTargets.Length == 0)
            {
                return float.MinValue;
            }

            var score = 0f;
            var allyTargets = validTargets.Where(unit => unit.IsHero == actor.IsHero).ToArray();
            var enemyTargets = validTargets.Where(unit => unit.IsHero != actor.IsHero).ToArray();
            var injuredAllies = allyTargets.Where(unit => unit.HealthRatio < 0.8f).ToArray();

            if (skill.skillType == SkillDataType.治疗)
            {
                score += 80f;
                score += injuredAllies.Sum(unit => (1f - unit.HealthRatio) * 40f);
                if (skill.targetType == SkillDataTargetType.自己 && actor.HealthRatio < 0.6f)
                {
                    score += 12f;
                }
            }
            else
            {
                score += skill.baseValue;
                score += enemyTargets.Length * 4f;
                if (skill.skillType == SkillDataType.控制)
                {
                    score += 10f;
                }

                if (skill.targetType == SkillDataTargetType.全体敌)
                {
                    score += enemyTargets.Length >= 2 ? 22f : 6f;
                }
                else if (skill.targetType == SkillDataTargetType.前排两敌)
                {
                    score += enemyTargets.Length >= 2 ? 16f : 4f;
                }
            }

            switch (actor.Archetype)
            {
                case CombatArchetype.Physician:
                    score += skill.skillType == SkillDataType.治疗 ? 40f : 0f;
                    score += skill.targetType == SkillDataTargetType.单友 || skill.targetType == SkillDataTargetType.自己 ? 10f : 0f;
                    break;
                case CombatArchetype.Artificer:
                    score += skill.targetType == SkillDataTargetType.全体敌 ? 24f : 0f;
                    score += skill.targetType == SkillDataTargetType.前排两敌 ? 10f : 0f;
                    score += skill.skillType == SkillDataType.控制 ? 14f : 0f;
                    break;
                case CombatArchetype.Bulwark:
                    score += skill.targetType == SkillDataTargetType.自己 ? 12f : 0f;
                    score += skill.targetType == SkillDataTargetType.单敌 ? 8f : 0f;
                    score += enemyTargets.Any(target => target.CurrentPosition <= 2) ? 8f : 0f;
                    break;
                case CombatArchetype.Executioner:
                    score += skill.skillType == SkillDataType.伤害 ? 18f : 0f;
                    score += enemyTargets.Any(target => target.HealthRatio <= 0.35f) ? 14f : 0f;
                    break;
            }

            switch (skill.skillId)
            {
                case "hero_03_scrap_volley":
                    score += enemyTargets.Length >= 3 ? 10f : -4f;
                    break;
                case "hero_04_guard_break":
                case "hero_07_guard_break":
                    score += enemyTargets.Any(target => target.IsFrontline) ? 10f : -3f;
                    break;
                case "hero_05_gear_sting":
                case "hero_08_ember_rend":
                    score += enemyTargets.Any(target => target.IsBackline) ? 12f : -4f;
                    break;
                case "hero_02_field_stitch":
                case "hero_06_field_stitch":
                    score += injuredAllies.Any(unit => unit.HasCooldowns) ? 10f : 0f;
                    break;
                case "hero_02_steam_purge":
                case "hero_06_steam_purge":
                    score += injuredAllies.Any(unit => unit.HasStatus("\u707c\u70e7") || unit.HasStatus("\u7729\u6655")) ? 14f : 0f;
                    break;
                case "hero_02_stun_chain":
                case "hero_06_stun_chain":
                    score += enemyTargets.Any(target => target.IsBackline) ? 10f : 0f;
                    break;
            }

            return score;
        }

        private float ScoreEnemyTarget(BattleUnit actor, SkillData skill, BattleUnit target)
        {
            var score = 0f;
            var missingHealthRatio = 1f - target.HealthRatio;

            if (target.IsHero == actor.IsHero)
            {
                score += skill.skillType == SkillDataType.治疗 ? missingHealthRatio * 100f : 10f;
                score += target.CurrentPosition <= 2 ? 3f : 0f;
                return score;
            }

            score += missingHealthRatio * 35f;
            score += target.Speed * 0.35f;

            switch (actor.Archetype)
            {
                case CombatArchetype.Bulwark:
                    score += target.CurrentPosition <= 2 ? 20f : 2f;
                    break;
                case CombatArchetype.Executioner:
                    score += missingHealthRatio * 55f;
                    score += target.Health <= Mathf.Max(10, actor.Attack) ? 12f : 0f;
                    break;
                case CombatArchetype.Artificer:
                    score += target.CurrentPosition >= 3 ? 18f : 4f;
                    break;
                case CombatArchetype.Physician:
                    score += missingHealthRatio * 8f;
                    break;
            }

            if (skill.skillType == SkillDataType.控制)
            {
                score += target.Speed * 0.8f;
            }

            return score;
        }

        private IEnumerable<BattleUnit> GetValidTargets(BattleUnit actor, SkillData skill)
        {
            return GetCandidateTargets(actor, skill).Where(target => IsValidTarget(actor, skill, target));
        }

        private IEnumerable<BattleUnit> GetCandidateTargets(BattleUnit actor, SkillData skill)
        {
            switch (skill.targetType)
            {
                case SkillDataTargetType.单友:
                    return GetOccupiedSlots(actor.IsHero);
                case SkillDataTargetType.自己:
                    return new[] { actor };
                case SkillDataTargetType.前排两敌:
                    return GetOccupiedSlots(!actor.IsHero).Where(unit => unit.CurrentPosition <= 2);
                case SkillDataTargetType.全体敌:
                case SkillDataTargetType.单敌:
                default:
                    return GetOccupiedSlots(!actor.IsHero);
            }
        }

        private bool IsValidTarget(BattleUnit actor, SkillData skill, BattleUnit target)
        {
            if (actor == null || skill == null || target == null || !target.IsAlive)
            {
                return false;
            }

            if (target.IsCorpse && !CanSkillTargetCorpse(skill))
            {
                return false;
            }

            switch (skill.targetType)
            {
                case SkillDataTargetType.自己:
                    return target == actor;
                case SkillDataTargetType.单友:
                    return target.IsHero == actor.IsHero && IsPositionAllowed(target.CurrentPosition, skill.targetAllowedPositions);
                case SkillDataTargetType.单敌:
                    return target.IsHero != actor.IsHero && IsPositionAllowed(target.CurrentPosition, skill.targetAllowedPositions);
                case SkillDataTargetType.前排两敌:
                    return target.IsHero != actor.IsHero && target.CurrentPosition <= 2 && IsPositionAllowed(target.CurrentPosition, skill.targetAllowedPositions);
                case SkillDataTargetType.全体敌:
                    return target.IsHero != actor.IsHero;
                default:
                    return false;
            }
        }

        private static bool CanSkillTargetCorpse(SkillData skill)
        {
            return skill.targetType == SkillDataTargetType.单敌 && skill.skillType == SkillDataType.伤害;
        }

        private IEnumerable<BattleUnit> ResolveTargets(BattleUnit actor, SkillData skill, BattleUnit primaryTarget)
        {
            switch (skill.targetType)
            {
                case SkillDataTargetType.全体敌:
                    return GetValidTargets(actor, skill);
                case SkillDataTargetType.前排两敌:
                    return GetValidTargets(actor, skill).OrderBy(unit => unit.CurrentPosition).Take(2);
                case SkillDataTargetType.自己:
                    return new[] { actor };
                default:
                    return new[] { primaryTarget };
            }
        }

        private void HandleDefeatedUnit(BattleUnit unit, SkillData killingSkill)
        {
            if (unit == null)
            {
                return;
            }

            if (unit.IsHero && !unit.IsDowned)
            {
                unit.EnterDownedState();
                hudController.AddLog($"{unit.Definition.displayName} \u6fd2\u5371\u5012\u4e0b\uff0c\u9000\u51fa\u4e86\u9635\u7ebf\u3002");
                ReleaseUnitSlot(unit);
                return;
            }

            if (!unit.IsCorpse)
            {
                unit.ConvertToCorpse();
                hudController.AddLog($"{unit.Definition.displayName} \u5012\u4e0b\uff0c\u7559\u4e0b\u4e86\u5c38\u4f53\u3002");
                RefreshViews();
                return;
            }

            RemoveUnitFromFormation(unit);
            hudController.AddLog($"{unit.DisplayName} \u88ab\u6e05\u9664\u4e86\u3002");
        }

        private void ReleaseUnitSlot(BattleUnit unit)
        {
            var slots = unit.IsHero ? heroSlots : enemySlots;
            var index = unit.CurrentPosition - 1;
            if (index >= 0 && index < slots.Length && slots[index] == unit)
            {
                slots[index] = null;
            }

            if (views.TryGetValue(unit, out var view))
            {
                StartCoroutine(HideViewAfterDownedCue(view));
            }

            CompactFormation(unit.IsHero);
        }

        private void RemoveUnitFromFormation(BattleUnit unit)
        {
            var slots = unit.IsHero ? heroSlots : enemySlots;
            var index = unit.CurrentPosition - 1;
            if (index >= 0 && index < slots.Length && slots[index] == unit)
            {
                slots[index] = null;
            }

            if (views.TryGetValue(unit, out var view))
            {
                view.gameObject.SetActive(false);
            }

            CompactFormation(unit.IsHero);
        }

        private static IEnumerator HideViewAfterDownedCue(CombatantView view)
        {
            if (view == null)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.05f);
            view.gameObject.SetActive(false);
        }

        private void CompactFormation(bool isHero)
        {
            var slots = isHero ? heroSlots : enemySlots;
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    continue;
                }

                for (var j = i + 1; j < slots.Length; j++)
                {
                    if (slots[j] == null)
                    {
                        continue;
                    }

                    slots[i] = slots[j];
                    slots[j] = null;
                    slots[i].CurrentPosition = i + 1;
                    break;
                }
            }

            LayoutFormation(isHero);
        }

        private static bool IsPositionAllowed(int position, int[] allowedPositions)
        {
            return allowedPositions == null || allowedPositions.Length == 0 || allowedPositions.Contains(position);
        }

        private IEnumerable<BattleUnit> GetLivingAllies(BattleUnit unit, bool includeSelf = false)
        {
            if (unit == null)
            {
                return Enumerable.Empty<BattleUnit>();
            }

            return GetOccupiedSlots(unit.IsHero)
                .Where(candidate => includeSelf || candidate != unit);
        }

        private IEnumerable<BattleUnit> GetLivingOpponents(BattleUnit unit)
        {
            return unit == null ? Enumerable.Empty<BattleUnit>() : GetOccupiedSlots(!unit.IsHero);
        }

        private IEnumerable<BattleUnit> GetOccupiedSlots(bool isHero)
        {
            return (isHero ? heroSlots : enemySlots).Where(unit => unit != null && unit.IsAlive);
        }

        private static IEnumerator PlayAttackAndHitOverlays(CombatantView actorView, IReadOnlyList<CombatantView> targetViews, Sprite attackSprite, IReadOnlyList<Sprite> hitSprites, float duration, AttackLungeState lungeState)
        {
            var validTargetViews = (targetViews ?? new CombatantView[0]).Where(view => view != null).ToArray();
            actorView.SetCombatEmphasis(true, 60);
            for (var i = 0; i < validTargetViews.Length; i++)
            {
                validTargetViews[i].SetCombatEmphasis(true, 61);
            }

            var bulletTime = actorView.StartCoroutine(PlayBulletTime());
            var recoil = lungeState.IsValid ? actorView.StartCoroutine(PlayAttackFocusRecoil(lungeState)) : null;
            var attack = actorView.StartCoroutine(actorView.PlayOverlay(attackSprite, duration, 0.1f, AttackFocusActorOverlaySortingOrder));
            var hits = validTargetViews
                .Select((targetView, index) =>
                {
                    var hitSprite = hitSprites != null && index < hitSprites.Count ? hitSprites[index] : null;
                    return targetView.StartCoroutine(targetView.PlayHitOverlay(hitSprite, duration, AttackFocusTargetOverlaySortingOrder));
                })
                .ToArray();
            yield return attack;
            foreach (var hit in hits)
            {
                yield return hit;
            }

            yield return bulletTime;
            if (recoil != null)
            {
                yield return recoil;
            }

            actorView.SetCombatEmphasis(false, 0);
            for (var i = 0; i < validTargetViews.Length; i++)
            {
                validTargetViews[i].SetCombatEmphasis(false, 0);
            }
        }

        private static IEnumerator PlayBulletTime()
        {
            var originalScale = Time.timeScale;
            var originalFixedDeltaTime = Time.fixedDeltaTime;
            var elapsed = 0f;

            while (elapsed < BulletTimeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsed / BulletTimeDuration);
                var slowHold = BulletTimeMinScale + Mathf.Sin(normalized * Mathf.PI * 2f) * 0.012f;
                var scale = normalized < 0.28f
                    ? Mathf.Lerp(originalScale, BulletTimeMinScale, Smooth01(normalized / 0.28f))
                    : normalized > 0.7f
                        ? Mathf.Lerp(slowHold, originalScale, Smooth01((normalized - 0.7f) / 0.3f))
                        : slowHold;

                Time.timeScale = Mathf.Clamp(scale, 0.08f, originalScale);
                Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;
                yield return null;
            }

            Time.timeScale = originalScale;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float EaseOutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            var inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseInCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value;
        }

        private static float EaseInExpo(float value)
        {
            value = Mathf.Clamp01(value);
            return value <= 0f ? 0f : Mathf.Pow(2f, 10f * (value - 1f));
        }

        private static bool IsImpactPresentationSkill(SkillData skill)
        {
            return skill != null && (skill.skillType == SkillDataType.伤害 || skill.skillType == SkillDataType.控制);
        }

        private static bool ShouldUseFriendlyAdvancePresentation(BattleUnit actor, SkillData skill, BattleUnit primaryTarget)
        {
            return actor != null &&
                   skill != null &&
                   primaryTarget != null &&
                   actor != primaryTarget &&
                   actor.IsHero == primaryTarget.IsHero &&
                   skill.targetType == SkillDataTargetType.单友;
        }

        private static Sprite ResolveAttackSprite(BattleUnit actor, SkillData skill)
        {
            var characterId = ResolveId(actor.Definition.characterId, actor.Definition.name, actor.DisplayName);
            var skillId = ResolveId(skill.skillId, skill.name, skill.skillName);
            var characterSpecificPath = $"Assets/Art/VFX/Combat/CharacterSpecific/{characterId}/{skillId}_attack.png";
            var characterDefaultPath = $"Assets/Art/VFX/Combat/CharacterSpecific/{characterId}/default_attack.png";
            var visualFallbackPath = $"Assets/Art/VFX/Combat/CharacterSpecific/{ResolveVisualFallbackId(characterId)}/default_attack.png";
            var genericPath = $"Assets/Art/VFX/Combat/AttackSprites/{skillId}_attack.png";
            return LoadCombatVfxSprite(characterSpecificPath)
                ?? LoadCombatVfxSprite(characterDefaultPath)
                ?? LoadCombatVfxSprite(visualFallbackPath)
                ?? LoadCombatVfxSprite(genericPath)
                ?? skill.attackSprite;
        }

        private static Sprite ResolveHitSprite(BattleUnit target, SkillData skill)
        {
            var characterId = ResolveId(target.Definition.characterId, target.Definition.name, target.DisplayName);
            var skillId = ResolveId(skill.skillId, skill.name, skill.skillName);
            var characterSpecificPath = $"Assets/Art/VFX/Combat/CharacterSpecific/{characterId}/{skillId}_hit.png";
            var characterDefaultPath = $"Assets/Art/VFX/Combat/CharacterSpecific/{characterId}/default_hit.png";
            var visualFallbackPath = $"Assets/Art/VFX/Combat/CharacterSpecific/{ResolveVisualFallbackId(characterId)}/default_hit.png";
            var genericPath = $"Assets/Art/VFX/Combat/HitSprites/{skillId}_hit.png";
            return LoadCombatVfxSprite(characterSpecificPath)
                ?? LoadCombatVfxSprite(characterDefaultPath)
                ?? LoadCombatVfxSprite(visualFallbackPath)
                ?? LoadCombatVfxSprite(genericPath)
                ?? skill.hitSprite;
        }

        private static Sprite LoadCombatVfxSprite(string assetPath)
        {
            return EditorSpriteSheetLoader.LoadSprite(assetPath)
                ?? CombatUiAssetLoader.LoadSprite(assetPath, Vector4.zero);
        }

        private static string ResolveVisualFallbackId(string characterId)
        {
            switch (characterId)
            {
                case "enemy_01":
                    return "hero_01";
                case "enemy_02":
                    return "hero_02";
                case "enemy_03":
                    return "hero_03";
                case "boss_clockwork_warden":
                    return "hero_04";
                default:
                    return characterId;
            }
        }

        private static string ResolveId(string explicitId, string assetName, string displayName)
        {
            var source = !string.IsNullOrWhiteSpace(explicitId) && explicitId != "skill" && explicitId != "character"
                ? explicitId
                : !string.IsNullOrWhiteSpace(assetName)
                    ? assetName
                    : displayName;

            return SanitizeId(source);
        }

        private static string SanitizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unnamed";
            }

            var chars = value.Trim().ToLowerInvariant().Select(character =>
                char.IsLetterOrDigit(character) ? character : '_').ToArray();
            return new string(chars).Trim('_');
        }

        private DamageResult CalculateDamage(BattleUnit actor, SkillData skill, BattleUnit target)
        {
            var randomOffset = UnityEngine.Random.Range(-2, 3);
            var attack = GetEffectiveAttack(actor, target);
            var defense = GetEffectiveDefense(target);
            var baseDamage = Mathf.Max(1, skill.baseValue + attack - defense + randomOffset);
            var amount = Mathf.Max(1, Mathf.RoundToInt(baseDamage * skill.powerMultiplier));

            amount = ApplyPassiveToDamage(actor, target, amount);
            amount = ApplyArchetypeToDamage(actor, skill, target, amount);
            amount = ApplySpecializationToDamage(actor, skill, target, amount);
            amount = ApplySkillSpecificDamageModifier(actor, skill, target, amount);

            if (actor.HasStatus(SuppressedStatusName))
            {
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * 0.8f));
            }

            var critical = UnityEngine.Random.value < 0.1f;
            if (critical)
            {
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * 1.5f));
            }

            return new DamageResult(amount, critical);
        }

        private int GetEffectiveAttack(BattleUnit unit, BattleUnit target)
        {
            var attack = unit.Attack;
            if (unit.HasPassive(HeroPassive.Berserker))
            {
                var hpPercent = (float)unit.Health / unit.MaxHealth;
                if (hpPercent < 0.5f)
                {
                    attack = Mathf.RoundToInt(attack * 1.3f);
                }
            }

            if (unit.HasPassive(HeroPassive.GlassCannon))
            {
                attack = Mathf.RoundToInt(attack * 1.25f);
            }

            attack += GetTeamAttackAuraBonus(unit);
            return attack;
        }

        private int GetTeamAttackAuraBonus(BattleUnit unit)
        {
            if (unit == null || !unit.IsAlive)
            {
                return 0;
            }

            var allies = GetLivingAllies(unit)
                .Where(candidate => candidate.HasPassive(HeroPassive.Vanguard) && candidate.IsFrontline);

            return allies.Count() * 2;
        }

        private int GetEffectiveDefense(BattleUnit unit)
        {
            var defense = unit.Defense;
            if (unit.HasPassive(HeroPassive.GlassCannon))
            {
                defense = Mathf.RoundToInt(defense * 0.5f);
            }

            if (unit.HasPassive(HeroPassive.Fortress) && unit.IsFrontline)
            {
                defense += 4;
            }

            if (unit.Archetype == CombatArchetype.Bulwark)
            {
                defense += unit.IsFrontline ? 3 : 1;
            }

            return defense;
        }

        private int ApplyPassiveToDamage(BattleUnit actor, BattleUnit target, int amount)
        {
            if ((!actor.IsHero && !target.IsHero) || amount <= 0)
            {
                return amount;
            }

            if (actor.HasPassive(HeroPassive.Executioner))
            {
                if (target.HealthRatio < 0.3f)
                {
                    amount = Mathf.RoundToInt(amount * 1.5f);
                }
            }

            if (actor.HasPassive(HeroPassive.Backstab))
            {
                if (actor.IsBackline && target.IsFrontline)
                {
                    amount = Mathf.RoundToInt(amount * 1.25f);
                }
            }

            if (actor.HasPassive(HeroPassive.Reaper))
            {
                var opposingTeam = actor.IsHero ? enemies : heroes;
                var deadCount = opposingTeam.Count(candidate => candidate != null && (!candidate.IsAlive || candidate.IsCorpse));
                if (deadCount > 0)
                {
                    amount = Mathf.RoundToInt(amount * (1f + deadCount * 0.1f));
                }
            }

            return amount;
        }

        private int ApplyArchetypeToDamage(BattleUnit actor, SkillData skill, BattleUnit target, int amount)
        {
            if (actor == null || skill == null || target == null || amount <= 0)
            {
                return amount;
            }

            switch (actor.Archetype)
            {
                case CombatArchetype.Bulwark:
                    if (actor.IsFrontline && target.IsFrontline)
                    {
                        amount = Mathf.RoundToInt(amount * 1.1f);
                    }
                    break;
                case CombatArchetype.Executioner:
                    amount = Mathf.RoundToInt(amount * 1.1f);
                    if (target.HealthRatio <= 0.5f)
                    {
                        amount = Mathf.RoundToInt(amount * 1.2f);
                    }
                    break;
                case CombatArchetype.Artificer:
                    if (skill.skillType == SkillDataType.控制)
                    {
                        amount = Mathf.RoundToInt(amount * 1.15f);
                    }

                    if (skill.targetType == SkillDataTargetType.全体敌 || skill.targetType == SkillDataTargetType.前排两敌)
                    {
                        amount = Mathf.RoundToInt(amount * 1.15f);
                    }

                    if (target.IsBackline)
                    {
                        amount = Mathf.RoundToInt(amount * 1.15f);
                    }
                    break;
                case CombatArchetype.Physician:
                    amount = Mathf.RoundToInt(amount * 0.9f);
                    break;
            }

            return Mathf.Max(1, amount);
        }

        private int ApplySpecializationToDamage(BattleUnit actor, SkillData skill, BattleUnit target, int amount)
        {
            if (actor == null || skill == null || target == null || amount <= 0)
            {
                return amount;
            }

            switch (actor.Specialization)
            {
                case CombatSpecialization.Slayer:
                    if (target.HealthRatio <= 0.5f)
                    {
                        amount = Mathf.RoundToInt(amount * 1.12f);
                    }
                    break;
                case CombatSpecialization.Breaker:
                    if (target.IsFrontline || target.HealthRatio >= 0.7f)
                    {
                        amount = Mathf.RoundToInt(amount * 1.1f);
                    }
                    break;
                case CombatSpecialization.Bombardier:
                    if (skill.targetType == SkillDataTargetType.全体敌 || target.IsBackline)
                    {
                        amount = Mathf.RoundToInt(amount * 1.1f);
                    }
                    break;
                case CombatSpecialization.Controller:
                    if (skill.skillType == SkillDataType.控制)
                    {
                        amount = Mathf.RoundToInt(amount * 1.1f);
                    }
                    break;
            }

            return Mathf.Max(1, amount);
        }

        private int ApplySkillSpecificDamageModifier(BattleUnit actor, SkillData skill, BattleUnit target, int amount)
        {
            if (actor == null || skill == null || target == null || amount <= 0)
            {
                return amount;
            }

            switch (skill.skillId)
            {
                case "hero_01_gear_sting":
                case "hero_05_gear_sting":
                    if (target.IsBackline)
                    {
                        amount = Mathf.RoundToInt(amount * 1.3f);
                    }
                    break;
                case "hero_01_overload_spark":
                    if (target.HasStatus(SuppressedStatusName))
                    {
                        amount = Mathf.RoundToInt(amount * 1.25f);
                    }
                    break;
                case "hero_03_shadow_cut":
                    if (target.Statuses.Count > 0)
                    {
                        amount = Mathf.RoundToInt(amount * 1.1f);
                    }
                    break;
                case "hero_03_crescent_lunge":
                    if (target.IsBackline)
                    {
                        amount = Mathf.RoundToInt(amount * 1.2f);
                    }
                    break;
                case "hero_03_wild_hunt":
                    if (target.HealthRatio <= 0.4f)
                    {
                        amount = Mathf.RoundToInt(amount * 1.25f);
                    }
                    break;
                case "hero_01_ember_rend":
                case "hero_08_ember_rend":
                    if (target.HasStatus("\u707c\u70e7"))
                    {
                        amount = Mathf.RoundToInt(amount * 1.25f);
                    }
                    break;
                case "hero_04_guard_break":
                case "hero_07_guard_break":
                    if (target.IsFrontline)
                    {
                        amount = Mathf.RoundToInt(amount * 1.2f);
                    }
                    break;
                case "hero_04_iron_cut":
                    if (target.HealthRatio >= 0.7f)
                    {
                        amount = Mathf.RoundToInt(amount * 1.15f);
                    }
                    break;
                case "hero_08_scrap_volley":
                    if (target.IsBackline)
                    {
                        amount = Mathf.RoundToInt(amount * 1.15f);
                    }
                    break;
                case "hero_05_iron_cut":
                    if (target.HealthRatio <= 0.5f)
                    {
                        amount = Mathf.RoundToInt(amount * 1.15f);
                    }
                    break;
                case "hero_03_earthbreak_ram":
                case "hero_05_earthbreak_ram":
                    if (target.IsFrontline)
                    {
                        amount = Mathf.RoundToInt(amount * 1.15f);
                    }
                    break;
            }

            return Mathf.Max(1, amount);
        }

        private void ApplySkillSpecificPostEffect(BattleUnit actor, SkillData skill, BattleUnit target)
        {
            if (actor == null || skill == null || target == null || !target.IsAlive)
            {
                return;
            }

            switch (skill.skillId)
            {
                case "hero_02_iron_cut":
                    if (actor.HealthRatio < 0.85f)
                    {
                        var selfHeal = Mathf.Max(1, Mathf.RoundToInt(actor.MaxHealth * 0.08f));
                        actor.Heal(selfHeal);
                        hudController.AddLog($"{actor.DisplayName} ����������ס��ţ��ָ��� {selfHeal} ��������");
                        if (views.TryGetValue(actor, out var medicActorView))
                        {
                            medicActorView.ShowFloatingText("+" + selfHeal, new Color(0.48f, 1f, 0.48f), 0.85f);
                        }
                    }
                    break;
                case "hero_02_field_stitch":
                case "hero_06_field_stitch":
                    var cooledSkill = target.ReduceRandomCooldown(1);
                    if (cooledSkill != null)
                    {
                        hudController.AddLog($"{target.DisplayName} \u7684 {cooledSkill.skillName} \u51b7\u5374\u7f29\u77ed\u4e86 1 \u56de\u5408\u3002");
                        if (views.TryGetValue(target, out var fieldStitchTargetView))
                        {
                            fieldStitchTargetView.ShowFloatingText("\u51b7\u5374-1", new Color(0.76f, 0.9f, 1f), 0.85f);
                        }
                    }
                    break;
                case "hero_07_iron_cut":
                    if (actor.IsFrontline)
                    {
                        var selfHeal = Mathf.Max(1, Mathf.RoundToInt(actor.MaxHealth * 0.06f));
                        actor.Heal(selfHeal);
                        hudController.AddLog($"{actor.DisplayName} �����ش����סǰ�ߣ��ָ��� {selfHeal} ��������");
                        if (views.TryGetValue(actor, out var bodyguardActorView))
                        {
                            bodyguardActorView.ShowFloatingText("+" + selfHeal, new Color(0.55f, 0.95f, 0.7f), 0.85f);
                        }
                    }
                    break;
                case "hero_02_steam_purge":
                case "hero_06_steam_purge":
                    var removed = target.ClearNegativeStatuses();
                    if (removed > 0)
                    {
                        hudController.AddLog($"{target.DisplayName} \u7684\u8d1f\u9762\u72b6\u6001\u88ab\u84b8\u6c7d\u51c0\u5316\u6e05\u9664\u4e86\u3002");
                        if (views.TryGetValue(target, out var purgeTargetView))
                        {
                            purgeTargetView.ShowFloatingText("\u51c0\u5316", new Color(0.58f, 0.94f, 0.95f), 0.9f);
                        }
                    }
                    break;
                case "hero_03_earthbreak_ram":
                case "hero_05_earthbreak_ram":
                    if (TryPushUnitBackward(target))
                    {
                        RefreshViews();
                        LayoutFormation(target.IsHero);
                        hudController.AddLog($"{target.DisplayName} 被裂地撞击顶退了一格。");
                        if (views.TryGetValue(target, out var pushedTargetView))
                        {
                            pushedTargetView.ShowFloatingText("击退", new Color(0.95f, 0.78f, 0.32f), 0.9f);
                        }
                    }
                    break;
                case "hero_03_watcher_sweep":
                case "hero_05_watcher_sweep":
                    if (actor.Specialization == CombatSpecialization.Sentinel)
                    {
                        var suppressBuff = FindBuffByName(SuppressedStatusName);
                        if (suppressBuff != null)
                        {
                            target.AddOrRefreshBuff(suppressBuff, 1);
                            if (views.TryGetValue(target, out var sentinelTargetView))
                            {
                                sentinelTargetView.Refresh();
                                sentinelTargetView.ShowFloatingText(suppressBuff.buffName, suppressBuff.nameTextColor, 0.85f);
                            }

                            hudController.AddLog($"{target.DisplayName} 被哨卫横扫压制，下一次攻击会变弱。");
                        }
                    }
                    break;
                case "hero_03_stand_guard":
                case "hero_05_stand_guard":
                    if (actor.Specialization == CombatSpecialization.Bastion && actor.HealthRatio < 0.7f)
                    {
                        var selfHeal = Mathf.Max(1, Mathf.RoundToInt(actor.MaxHealth * 0.08f));
                        actor.Heal(selfHeal);
                        hudController.AddLog($"{actor.DisplayName} 的壁垒专精在护卫时稳住了阵线，恢复了 {selfHeal} 点生命。");
                        if (views.TryGetValue(actor, out var bastionActorView))
                        {
                            bastionActorView.ShowFloatingText("+" + selfHeal, new Color(0.55f, 0.95f, 0.7f), 0.85f);
                        }
                    }
                    break;
            }
        }

        private BuffData FindBuffByName(string buffName)
        {
            if (string.IsNullOrWhiteSpace(buffName))
            {
                return null;
            }

            if (buffLookup == null)
            {
                buffLookup = Resources.LoadAll<BuffData>("Buffs")
                    .Where(buff => buff != null && !string.IsNullOrWhiteSpace(buff.buffName))
                    .GroupBy(buff => buff.buffName)
                    .ToDictionary(group => group.Key, group => group.First());
            }

            return buffLookup.TryGetValue(buffName, out var buffData) ? buffData : null;
        }

        private void ApplySkillSpecificActionReward(BattleUnit actor, SkillData skill, BattleUnit[] targets)
        {
            return;
        }

        private void ApplySkillSpecificActionRisk(BattleUnit actor, SkillData skill, BattleUnit[] targets)
        {
            if (actor == null || skill == null || targets == null || targets.Length == 0 || !actor.IsAlive)
            {
                return;
            }

            var livingTargets = targets.Count(target => target != null && target.IsAlive);
            if (livingTargets < 3)
            {
                return;
            }

            var recoilDamage = 0;
            switch (skill.skillId)
            {
                case "hero_01_scrap_volley":
                    recoilDamage = 2;
                    break;
                case "hero_03_scrap_volley":
                    recoilDamage = 2;
                    break;
                case "hero_08_scrap_volley":
                    recoilDamage = 4;
                    break;
            }

            if (recoilDamage <= 0)
            {
                return;
            }

            ApplyRawDamage(actor, recoilDamage, null);
            hudController.AddLog($"{actor.DisplayName} \u627f\u53d7\u4e86\u9f50\u5c04\u53cd\u9707\u7684 {recoilDamage} \u70b9\u4f24\u5bb3\u3002");
            if (views.TryGetValue(actor, out var actorView))
            {
                actorView.ShowFloatingText($"-{recoilDamage}", new Color(1f, 0.68f, 0.22f), 0.95f);
            }

            if (!actor.IsAlive)
            {
                if (views.TryGetValue(actor, out var deadActorView))
                {
                    StartCoroutine(deadActorView.PlayDeathCue());
                }

                HandleDefeatedUnit(actor, skill);
            }
        }

        private void ApplyPassiveOnTurnStart(BattleUnit unit)
        {
            if (!unit.IsHero || (unit.Definition.passive == HeroPassive.None && unit.Definition.growthPassive == HeroPassive.None))
            {
                return;
            }

            if (unit.HasPassive(HeroPassive.Regenerator))
            {
                var regenAmount = Mathf.Max(1, Mathf.RoundToInt(unit.MaxHealth * 0.05f));
                unit.Heal(regenAmount);
                hudController.AddLog($"{unit.DisplayName} 的再生恢复了 {regenAmount} 点生命。");
            }

            if (unit.HasPassive(HeroPassive.Tactician))
            {
                var allies = GetLivingAllies(unit).Where(candidate => candidate.HasCooldowns).ToArray();
                if (allies.Length > 0)
                {
                    var ally = allies[UnityEngine.Random.Range(0, allies.Length)];
                    var reducedSkill = ally.ReduceRandomCooldown(2);
                    if (reducedSkill != null)
                    {
                        hudController.AddLog($"{unit.DisplayName} 的战术家被动为 {ally.DisplayName} 缩短了 {reducedSkill.skillName} 的冷却。");
                    }
                }
            }

            if (unit.HasPassive(HeroPassive.Vanguard) && unit.IsFrontline)
            {
                hudController.AddLog($"{unit.DisplayName} 的先锋光环正在强化前线队友。");
            }

            if (unit.HasPassive(HeroPassive.Inspirer))
            {
                foreach (var ally in GetLivingAllies(unit, includeSelf: true))
                {
                    var healAmount = Mathf.Max(1, Mathf.RoundToInt(ally.MaxHealth * 0.1f));
                    ally.Heal(healAmount);
                }
                hudController.AddLog($"{unit.DisplayName} 的鼓舞恢复了全队生命。");
            }
        }

        private void ApplyPassiveOnKill(BattleUnit killer, BattleUnit victim)
        {
            if (!killer.IsHero || (killer.Definition.passive == HeroPassive.None && killer.Definition.growthPassive == HeroPassive.None))
            {
                return;
            }

            if (killer.HasPassive(HeroPassive.Scavenger))
            {
                var healAmount = Mathf.Max(1, Mathf.RoundToInt(killer.MaxHealth * 0.2f));
                killer.Heal(healAmount);
                hudController.AddLog($"{killer.DisplayName} 的回收者被动恢复了 {healAmount} 点生命。");
            }

            if (killer.HasPassive(HeroPassive.ChainReaction))
            {
                var aliveEnemies = GetLivingOpponents(killer).Where(candidate => candidate != victim).ToArray();
                if (aliveEnemies.Length > 0)
                {
                    var target = aliveEnemies[UnityEngine.Random.Range(0, aliveEnemies.Length)];
                    var splashDamage = Mathf.Max(1, Mathf.RoundToInt(victim.MaxHealth * 0.25f));
                    target.TakeDamage(splashDamage);
                    hudController.AddLog($"{killer.DisplayName} 的连锁反应对 {target.DisplayName} 造成了 {splashDamage} 点溅射伤害。");
                    if (!target.IsAlive && views.TryGetValue(target, out var targetView))
                    {
                        HandleDefeatedUnit(target, null);
                    }
                }
            }
        }

        private void ApplyRawDamage(BattleUnit target, int amount, BattleUnit attacker)
        {
            if (target == null || amount <= 0)
            {
                return;
            }

            target.TakeDamage(amount);

            if (target.HasPassive(HeroPassive.ThornArmor) && attacker != null && attacker.IsAlive)
            {
                var reflectDamage = Mathf.Max(1, Mathf.RoundToInt(amount * 0.2f));
                attacker.TakeDamage(reflectDamage);
                hudController.AddLog($"{target.DisplayName} �ľ������׷����� {reflectDamage} ���˺��� {attacker.DisplayName}��");

                if (views.TryGetValue(attacker, out var attackerView))
                {
                    attackerView.ShowFloatingText($"-{reflectDamage}", new Color(0.85f, 0.25f, 0.95f));
                }

                if (!attacker.IsAlive)
                {
                    ApplyPassiveOnKill(target, attacker);
                    HandleDefeatedUnit(attacker, null);
                }
            }
        }

        private BattleUnit FindBodyguardProtector(BattleUnit attacker, BattleUnit protectedUnit)
        {
            if (protectedUnit == null || !protectedUnit.IsHero || protectedUnit.IsCorpse)
            {
                return null;
            }

            return GetLivingAllies(protectedUnit).FirstOrDefault(candidate =>
                candidate != protectedUnit &&
                (candidate.HasPassive(HeroPassive.Bodyguard) || candidate.HasStatus(GuardStatusName)) &&
                Mathf.Abs(candidate.CurrentPosition - protectedUnit.CurrentPosition) == 1);
        }

        private bool TryPushUnitBackward(BattleUnit target)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            var slots = target.IsHero ? heroSlots : enemySlots;
            var targetIndex = target.CurrentPosition - 1;
            if (targetIndex < 0 || targetIndex >= slots.Length - 1)
            {
                return false;
            }

            var emptyIndex = -1;
            for (var i = targetIndex + 1; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    emptyIndex = i;
                    break;
                }
            }

            if (emptyIndex < 0)
            {
                return false;
            }

            for (var i = emptyIndex; i > targetIndex; i--)
            {
                slots[i] = slots[i - 1];
            }

            slots[targetIndex] = null;
            return true;
        }

        private bool ApplyPassiveBeforeDeath(BattleUnit unit, int incomingDamage)
        {
            if (!unit.HasPassive(HeroPassive.IronWill))
            {
                return false;
            }

            if (unit.Health - incomingDamage <= 0)
            {
                if (!ironWillUsedThisBattle.Contains(unit))
                {
                    ironWillUsedThisBattle.Add(unit);
                    unit.TakeDamage(Mathf.Max(0, unit.Health - 1));
                    hudController.AddLog($"{unit.DisplayName} ������־�����ˣ����� 1 ��������");
                    return true;
                }
            }

            return false;
        }

        private int CalculateHealingAmount(BattleUnit actor, SkillData skill, BattleUnit target)
        {
            var amount = Mathf.Max(1, Mathf.RoundToInt((skill.baseValue + actor.Attack * 0.5f) * skill.powerMultiplier));
            if (actor != null && actor.Archetype == CombatArchetype.Physician)
            {
                amount = Mathf.RoundToInt(amount * 1.25f);
            }

            amount = ApplySpecializationToHealing(actor, skill, target, amount);
            amount = ApplySkillSpecificHealingModifier(actor, skill, target, amount);
            return Mathf.Max(1, amount);
        }

        private int ApplySpecializationToHealing(BattleUnit actor, SkillData skill, BattleUnit target, int amount)
        {
            if (actor == null || skill == null || target == null || amount <= 0)
            {
                return amount;
            }

            switch (actor.Specialization)
            {
                case CombatSpecialization.Surgeon:
                    if (target.HealthRatio <= 0.5f)
                    {
                        amount = Mathf.RoundToInt(amount * 1.12f);
                    }
                    break;
                case CombatSpecialization.Stimulator:
                    if (target.HasCooldowns)
                    {
                        amount = Mathf.RoundToInt(amount * 1.08f);
                    }
                    break;
            }

            return Mathf.Max(1, amount);
        }

        private int ApplySkillSpecificHealingModifier(BattleUnit actor, SkillData skill, BattleUnit target, int amount)
        {
            if (actor == null || skill == null || target == null || amount <= 0)
            {
                return amount;
            }

            switch (skill.skillId)
            {
                case "hero_02_field_stitch":
                    if (target.HealthRatio <= 0.5f)
                    {
                        amount = Mathf.RoundToInt(amount * 1.25f);
                    }
                    break;
                case "hero_02_steam_purge":
                case "hero_06_steam_purge":
                    if (target.HasStatus("\u707c\u70e7") || target.HasStatus("\u7729\u6655"))
                    {
                        amount = Mathf.RoundToInt(amount * 1.15f);
                    }
                    break;
            }

            return Mathf.Max(1, amount);
        }

        private void ApplyArchetypeTurnStart(BattleUnit actor)
        {
            return;
        }

        private void ApplySpecializationTurnStart(BattleUnit actor)
        {
            return;
        }

        private void ApplyArchetypeActionResource(BattleUnit actor, SkillData skill, BattleUnit[] targets)
        {
            return;
        }

        private static Color GetDamageColor(SkillDataType skillType)
        {
            if (skillType == SkillDataType.治疗)
            {
                return new Color(0.25f, 0.95f, 0.38f);
            }

            switch (skillType)
            {
                case SkillDataType.控制:
                    return new Color(0.72f, 0.22f, 0.95f);
                case SkillDataType.伤害:
                default:
                    return new Color(0.95f, 0.12f, 0.08f);
            }
        }

        private static float GetShakeStrength(SkillData skill)
        {
            return Mathf.Clamp(0.035f + skill.baseValue * 0.004f, 0.04f, 0.12f);
        }

        private void CacheDefaultCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            defaultCameraPosition = camera.transform.position;
            defaultCameraSize = camera.orthographicSize;
        }

        private IEnumerator FocusCamera(Vector3 focusPosition)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                yield break;
            }

            var startPosition = camera.transform.position;
            var startSize = camera.orthographicSize;
            var targetPosition = new Vector3(
                focusPosition.x + attackFocusCameraOffset.x,
                Mathf.Clamp(focusPosition.y + 0.4f + attackFocusCameraOffset.y, -1.4f, 1.2f),
                startPosition.z);
            var targetSize = Mathf.Max(2.65f, defaultCameraSize * 0.66f);
            const float duration = 0.3f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = EaseOutCubic(elapsed / duration);
                camera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                camera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
                yield return null;
            }
        }

        private IEnumerator BeginAttackFocus(CombatantView actorView, CombatantView targetView, IReadOnlyCollection<CombatantView> focusTargetViews)
        {
            actorView?.SetFocusLayer(true, AttackFocusActorSortingOrder);
            actorView?.SetFocusScale(attackFocusScale);
            foreach (var focusTargetView in focusTargetViews ?? new CombatantView[0])
            {
                focusTargetView?.SetFocusLayer(true, AttackFocusTargetSortingOrder);
                focusTargetView?.SetFocusScale(attackFocusScale);
            }

            ApplyFocusBlur(actorView, focusTargetViews);

            if (targetView == null)
            {
                yield return StartCoroutine(FocusCamera(actorView.transform.position));
                yield break;
            }

            yield return StartCoroutine(FocusCameraBetween(actorView.transform.position, targetView.transform.position));
        }

        private IEnumerator EndAttackFocus(CombatantView actorView, IReadOnlyCollection<CombatantView> focusTargetViews)
        {
            var focusViews = new List<CombatantView>();
            if (actorView != null)
            {
                focusViews.Add(actorView);
            }

            if (focusTargetViews != null)
            {
                foreach (var focusTargetView in focusTargetViews)
                {
                    if (focusTargetView != null && !focusViews.Contains(focusTargetView))
                    {
                        focusViews.Add(focusTargetView);
                    }
                }
            }

            yield return StartCoroutine(AnimateFocusScaleRestore(focusViews));

            foreach (var view in focusViews)
            {
                view.SetFocusLayer(false, 0);
            }

            ClearFocusBlur();
            yield return StartCoroutine(RestoreCamera());
        }

        private IEnumerator AnimateFocusScaleRestore(IReadOnlyList<CombatantView> focusViews)
        {
            if (focusViews == null || focusViews.Count == 0)
            {
                yield break;
            }

            var duration = Mathf.Max(0f, attackFocusScaleRestoreDuration);
            if (duration <= 0.001f)
            {
                for (var i = 0; i < focusViews.Count; i++)
                {
                    focusViews[i]?.SetFocusScale(1f);
                }

                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var scale = Mathf.Lerp(attackFocusScale, 1f, Smooth01(t));
                for (var i = 0; i < focusViews.Count; i++)
                {
                    focusViews[i]?.SetFocusScale(scale);
                }

                yield return null;
            }

            for (var i = 0; i < focusViews.Count; i++)
            {
                focusViews[i]?.SetFocusScale(1f);
            }
        }

        private AttackLungeState CreateAttackLungeState(CombatantView actorView, IReadOnlyList<CombatantView> targetViews, bool moveTargets)
        {
            if (actorView == null || targetViews == null || targetViews.Count == 0)
            {
                return default;
            }

            var actorStart = actorView.transform.position;
            var orderedTargets = targetViews
                .Where(view => view != null)
                .OrderBy(view => view.transform.position.x)
                .ToArray();
            if (orderedTargets.Length == 0)
            {
                return default;
            }

            var targetStarts = orderedTargets.Select(view => view.transform.position).ToArray();
            var actorEnd = actorStart;
            var targetEnds = new Vector3[targetStarts.Length];
            var actorRecoil = actorStart;
            var targetRecoils = new Vector3[targetStarts.Length];

            if (!moveTargets)
            {
                var targetCenter = targetStarts.Aggregate(Vector3.zero, (sum, position) => sum + position) / targetStarts.Length;
                var focusCenter = (actorStart + targetCenter) * 0.5f;
                actorEnd = new Vector3(focusCenter.x - attackFocusFriendlyAdvanceOffset, actorStart.y, actorStart.z);
                for (var i = 0; i < targetEnds.Length; i++)
                {
                    targetEnds[i] = targetStarts[i];
                }
            }
            else if (orderedTargets.Length == 1)
            {
                var targetStart = targetStarts[0];
                var focusCenter = (actorStart + targetStart) * 0.5f;
                var direction = actorStart.x <= targetStart.x ? 1f : -1f;
                actorEnd = new Vector3(focusCenter.x - direction * attackFocusSingleActorOffset, actorStart.y, actorStart.z);
                targetEnds[0] = new Vector3(focusCenter.x + direction * attackFocusSingleTargetOffset, targetStart.y, targetStart.z);
            }
            else
            {
                var targetCenter = targetStarts.Aggregate(Vector3.zero, (sum, position) => sum + position) / targetStarts.Length;
                var focusCenter = (actorStart + targetCenter) * 0.5f;
                var direction = actorStart.x <= targetCenter.x ? 1f : -1f;
                actorEnd = new Vector3(focusCenter.x - direction * attackFocusAoeActorOffset, actorStart.y, actorStart.z);
                var targetLineCenter = new Vector3(focusCenter.x + direction * attackFocusAoeTargetOffset, targetCenter.y, targetCenter.z);
                var startOffset = -(orderedTargets.Length - 1) * attackFocusAoeTargetSpacing * 0.5f;

                for (var i = 0; i < targetEnds.Length; i++)
                {
                    targetEnds[i] = targetLineCenter + new Vector3(startOffset + i * attackFocusAoeTargetSpacing, 0f, 0f);
                }
            }

            actorRecoil = Vector3.Lerp(actorEnd, actorStart, Mathf.Clamp01(attackFocusRecoilRatio));
            for (var i = 0; i < targetEnds.Length; i++)
            {
                targetRecoils[i] = Vector3.Lerp(targetEnds[i], targetStarts[i], Mathf.Clamp01(attackFocusRecoilRatio));
            }

            return new AttackLungeState(actorView, actorStart, actorEnd, actorRecoil, orderedTargets, targetStarts, targetEnds, targetRecoils);
        }

        private IEnumerator MoveAttackFocusLunge(AttackLungeState state, bool moveIn)
        {
            if (!state.IsValid)
            {
                yield break;
            }

            var actorStart = moveIn ? state.ActorStart : state.ActorEnd;
            var actorEnd = moveIn ? state.ActorEnd : state.ActorStart;
            var elapsed = 0f;

            while (elapsed < attackFocusLungeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / attackFocusLungeDuration);
                var eased = moveIn ? EaseOutCubic(t) : EaseInCubic(t);
                if (state.ActorView != null)
                {
                    state.ActorView.transform.position = Vector3.LerpUnclamped(actorStart, actorEnd, eased);
                }

                for (var i = 0; i < state.TargetViews.Length; i++)
                {
                    var targetView = state.TargetViews[i];
                    if (targetView == null)
                    {
                        continue;
                    }

                    var targetStart = moveIn ? state.TargetStarts[i] : state.TargetEnds[i];
                    var targetEnd = moveIn ? state.TargetEnds[i] : state.TargetStarts[i];
                    targetView.transform.position = Vector3.LerpUnclamped(targetStart, targetEnd, eased);
                }

                yield return null;
            }

            if (state.ActorView != null)
            {
                state.ActorView.transform.position = actorEnd;
            }

            for (var i = 0; i < state.TargetViews.Length; i++)
            {
                if (state.TargetViews[i] != null)
                {
                    state.TargetViews[i].transform.position = moveIn ? state.TargetEnds[i] : state.TargetStarts[i];
                }
            }
        }

        private IEnumerator FocusCameraBetween(Vector3 actorPosition, Vector3 targetPosition)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                yield break;
            }

            var startPosition = camera.transform.position;
            var startSize = camera.orthographicSize;
            var center = (actorPosition + targetPosition) * 0.5f;
            var cameraTargetPosition = new Vector3(
                center.x + attackFocusCameraOffset.x,
                Mathf.Clamp(center.y + 0.35f + attackFocusCameraOffset.y, -1.4f, 1.25f),
                startPosition.z);
            var targetSize = Mathf.Max(2.35f, defaultCameraSize * attackFocusZoomRatio);
            const float duration = 0.24f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = EaseOutCubic(elapsed / duration);
                camera.transform.position = Vector3.Lerp(startPosition, cameraTargetPosition, t);
                camera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
                yield return null;
            }

            camera.transform.position = cameraTargetPosition;
            camera.orthographicSize = targetSize;
        }

        private IEnumerator RestoreCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                yield break;
            }

            var startPosition = camera.transform.position;
            var startSize = camera.orthographicSize;
            const float duration = 0.2f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                camera.transform.position = Vector3.Lerp(startPosition, defaultCameraPosition, t);
                camera.orthographicSize = Mathf.Lerp(startSize, defaultCameraSize, t);
                yield return null;
            }

            camera.transform.position = defaultCameraPosition;
            camera.orthographicSize = defaultCameraSize;
        }

        private void ApplyFocusBlur(CombatantView actorView, IReadOnlyCollection<CombatantView> targetViews)
        {
            ClearFocusBlur();
            var material = GetFocusBlurMaterial();
            if (material == null)
            {
                return;
            }

            var actorTransform = actorView != null ? actorView.transform : null;
            var targetTransforms = (targetViews ?? new CombatantView[0])
                .Where(view => view != null)
                .Select(view => view.transform)
                .ToArray();
            foreach (var renderer in FindObjectsOfType<SpriteRenderer>())
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                var rendererTransform = renderer.transform;
                if (actorTransform != null && rendererTransform.IsChildOf(actorTransform) ||
                    targetTransforms.Any(targetTransform => rendererTransform.IsChildOf(targetTransform)))
                {
                    continue;
                }

                blurredRendererStates.Add(new BlurredRendererState(renderer, renderer.sharedMaterial));
                renderer.material = material;
            }

            Debug.Log($"Attack focus blur applied to {blurredRendererStates.Count} SpriteRenderer(s).", this);
        }

        private void ClearFocusBlur()
        {
            foreach (var state in blurredRendererStates)
            {
                if (state.Renderer != null)
                {
                    state.Renderer.sharedMaterial = state.Material;
                }
            }

            blurredRendererStates.Clear();
        }

        private Material GetFocusBlurMaterial()
        {
            if (attackFocusBlurMaterial != null)
            {
                attackFocusBlurMaterial.SetFloat("_BlurStrength", attackFocusBlurStrength);
                attackFocusBlurMaterial.SetColor("_Color", attackFocusBlurTint);
                return attackFocusBlurMaterial;
            }

            if (runtimeFocusBlurMaterial != null)
            {
                return runtimeFocusBlurMaterial;
            }

            var shader = Shader.Find("Clockwork Wasteland/Sprite Focus Blur");
            if (shader == null)
            {
                Debug.LogWarning("Focus blur shader was not found. Attack focus will continue without background blur.", this);
                return null;
            }

            runtimeFocusBlurMaterial = new Material(shader)
            {
                name = "Runtime Focus Blur"
            };
            runtimeFocusBlurMaterial.SetFloat("_BlurStrength", attackFocusBlurStrength);
            runtimeFocusBlurMaterial.SetColor("_Color", attackFocusBlurTint);
            return runtimeFocusBlurMaterial;
        }

        private IEnumerator ScreenShake(float strength, float duration)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                yield break;
            }

            var basePosition = camera.transform.position;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                camera.transform.position = basePosition + new Vector3(
                    UnityEngine.Random.Range(-strength, strength),
                    UnityEngine.Random.Range(-strength, strength),
                    0f);
                yield return null;
            }

            camera.transform.position = basePosition;
        }

        private void SetupScene()
        {
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("Main Camera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.orthographic = true;
                camera.orthographicSize = 4.4f;
                camera.backgroundColor = new Color(0.06f, 0.065f, 0.07f);
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            }

            EnsureAudioListener();
            UpdateBackdrop(ResolveBattleBackground(), Vector2.zero, 1f);

            hudController = battleHudControllerPrefab != null
                ? Instantiate(battleHudControllerPrefab)
                : new GameObject("Battle HUD Controller", typeof(RectTransform)).AddComponent<BattleHudController>();

            hudController.name = "Battle HUD Controller";
            hudController.Build();
        }

        private static void EnsureAudioListener()
        {
            if (Object.FindObjectOfType<AudioListener>() != null)
            {
                return;
            }

            var camera = Camera.main;
            if (camera != null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }
        }

        private void LoadDefaultPresentationPrefabs()
        {
#if UNITY_EDITOR
            if (defaultUnitPrefab == null)
            {
                defaultUnitPrefab = AssetDatabase.LoadAssetAtPath<CombatantView>("Assets/ClockworkWastelandDemo/Prefabs/CombatUnit.prefab");
            }

            if (nameplatePrefab == null)
            {
                nameplatePrefab = AssetDatabase.LoadAssetAtPath<CombatNameplate>("Assets/ClockworkWastelandDemo/Prefabs/CombatNameplate.prefab");
            }

            if (turnIndicatorPrefab == null)
            {
                turnIndicatorPrefab = AssetDatabase.LoadAssetAtPath<TurnIndicatorView>("Assets/ClockworkWastelandDemo/Prefabs/CombatTurnIndicator.prefab");
            }
#endif
        }

        private void SetupHeroUnits()
        {
            var heroDefinitions = heroParty != null && heroParty.Length > 0
                ? heroParty
                : selectedHeroDefinitions.Where(hero => hero != null).Take(MaxFormationSlots).ToArray();

            if (heroDefinitions == null || heroDefinitions.Length == 0)
            {
                heroDefinitions = GetDeployableHeroPool().Take(MaxFormationSlots).ToArray();
            }

            if (heroDefinitions == null || heroDefinitions.Length == 0)
            {
                heroDefinitions = LoadHeroPool().Take(MaxFormationSlots).ToArray();
            }

            if (heroDefinitions == null || heroDefinitions.Length == 0)
            {
                heroDefinitions = DemoBattleBootstrap.CreateDefaultHeroes();
            }

            SpawnTeam(heroDefinitions, heroes, heroSlots, true);
            LayoutFormation(true);
        }

        private void PrepareBattle(int battleNumber, bool bossBattle)
        {
            ResetCombatRuntimeState(clearTurnQueue: true);
            round = 0;
            battleBackgroundIndex = battleBackgrounds != null && battleBackgrounds.Length > 0
                ? Mathf.Clamp(battleNumber - 1, 0, battleBackgrounds.Length - 1)
                : battleBackgroundIndex;

            var mapBackground = selectedAdventureMap.Data != null ? selectedAdventureMap.Data.backgroundSprite : null;
            var mapOffset = selectedAdventureMap.Data != null ? selectedAdventureMap.Data.backgroundOffset : Vector2.zero;
            var mapScale = selectedAdventureMap.Data != null ? selectedAdventureMap.Data.backgroundScale : 1f;
            var battleBackground = currentAdventureBattle != null && currentAdventureBattle.backgroundOverride != null
                ? currentAdventureBattle.backgroundOverride
                : mapBackground;
            var battleOffset = currentAdventureBattle != null && currentAdventureBattle.backgroundOverride != null
                ? currentAdventureBattle.backgroundOffset
                : mapOffset;
            var battleScale = currentAdventureBattle != null && currentAdventureBattle.backgroundOverride != null
                ? currentAdventureBattle.backgroundScale
                : mapScale;
            UpdateBackdrop(battleBackground != null ? battleBackground : ResolveBattleBackground(), battleOffset, battleScale);

            ClearEnemyUnits();

            var configuredPlacements = currentAdventureBattle != null
                ? currentAdventureBattle.GetOrderedPlacements()
                : System.Array.Empty<AdventureEnemyPlacement>();

            if (configuredPlacements.Count > 0)
            {
                SpawnConfiguredEnemyTeam(configuredPlacements);
            }
            else
            {
                var enemyDefinitions = bossBattle
                    ? DemoBattleBootstrap.CreateBossEnemies()
                    : SelectRandomEnemyEncounter();
                SpawnTeam(enemyDefinitions, enemies, enemySlots, false);
            }

            LayoutFormation(true);
            LayoutFormation(false);
            RefreshViews();
            hudController.SetRound(1);
            var turnLabel = currentAdventureBattle != null && !string.IsNullOrWhiteSpace(currentAdventureBattle.displayName)
                ? currentAdventureBattle.displayName
                : (bossBattle ? "\u6700\u7ec8 Boss \u6218" : $"\u7b2c {battleNumber} \u573a\u6218\u6597");
            hudController.SetTurn(turnLabel);
        }

        private CombatantDefinition[] SelectRandomEnemyEncounter()
        {
            var sceneEnemies = enemyParty == null
                ? Enumerable.Empty<CombatantDefinition>()
                : enemyParty.Where(enemy => enemy != null);
            var pool = sceneEnemies
                .Concat(LoadEnemyPool())
                .Where(enemy => enemy != null)
                .GroupBy(enemy => string.IsNullOrWhiteSpace(enemy.characterId) ? enemy.name : enemy.characterId)
                .Select(group => group.First())
                .ToArray();

            if (pool.Length == 0)
            {
                return DemoBattleBootstrap.CreateDefaultEnemies();
            }

            var maxCount = Mathf.Min(MaxFormationSlots, pool.Length);
            var count = UnityEngine.Random.Range(1, maxCount + 1);
            return pool
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(count)
                .ToArray();
        }

        private static CombatantDefinition[] LoadEnemyPool()
        {
#if UNITY_EDITOR
            var assetEnemies = EnumerateCombatantAssets()
                .Select(AssetDatabase.LoadAssetAtPath<CombatantDefinition>)
                .Where(enemy => enemy != null && !enemy.isHero)
                .OrderBy(enemy => enemy.characterId)
                .ToArray();

            if (assetEnemies.Length > 0)
            {
                return assetEnemies;
            }
#endif
            return DemoBattleBootstrap.CreateDefaultEnemies();
        }

        private static CombatantDefinition[] LoadHeroPool()
        {
#if UNITY_EDITOR
            var assetHeroes = EnumerateCombatantAssets()
                .Select(AssetDatabase.LoadAssetAtPath<CombatantDefinition>)
                .Where(hero => hero != null && hero.isHero)
                .OrderBy(hero => hero.characterId)
                .ToArray();

            if (assetHeroes.Length > 0)
            {
                return assetHeroes.Select(Instantiate).ToArray();
            }
#endif
            return DemoBattleBootstrap.CreateHeroPool();
        }

        private AdventureMapData[] LoadAdventureMaps()
        {
            var catalog = adventureMapCatalog != null ? adventureMapCatalog : Resources.Load<AdventureMapCatalog>("Adventure/AdventureMapCatalog");
#if UNITY_EDITOR
            if (catalog == null)
            {
                catalog = AssetDatabase.LoadAssetAtPath<AdventureMapCatalog>("Assets/Resources/Adventure/AdventureMapCatalog.asset");
            }
#endif
            return catalog != null
                ? catalog.GetOrderedMaps().Where(map => map != null).ToArray()
                : System.Array.Empty<AdventureMapData>();
        }

        private static AdventureMapOption[] CreateFallbackAdventureMaps()
        {
            var fallback = ScriptableObject.CreateInstance<AdventureMapData>();
            fallback.mapId = "rust_wastes";
            fallback.displayName = "\u9508\u94c1\u8352\u539f";
            fallback.description = "\u6807\u51c6\u5192\u9669\u8def\u7ebf\uff0c\u9002\u5408\u6d4b\u8bd5\u961f\u4f0d\u3002";
            fallback.unlockType = AdventureMapUnlockType.Default;
            fallback.unlockDescription = "\u9ed8\u8ba4\u89e3\u9501";
            fallback.backgroundScale = 1f;
            fallback.battles = new List<AdventureBattleConfig>
            {
                new AdventureBattleConfig
                {
                    battleId = "battle_01",
                    displayName = "\u7b2c 1 \u573a\u6218\u6597",
                    description = "\u9ed8\u8ba4\u793a\u4f8b\u6218\u6597\u3002"
                }
            };
            return new[] { new AdventureMapOption(fallback, true) };
        }

        private void SpawnConfiguredEnemyTeam(IReadOnlyList<AdventureEnemyPlacement> placements)
        {
            if (placements == null || placements.Count == 0)
            {
                SpawnTeam(SelectRandomEnemyEncounter(), enemies, enemySlots, false);
                return;
            }

            for (var index = 0; index < placements.Count; index++)
            {
                var placement = placements[index];
                if (placement == null || placement.combatant == null)
                {
                    continue;
                }

                var slot = Mathf.Clamp(placement.slot, 1, MaxFormationSlots);
                if (enemySlots[slot - 1] != null)
                {
                    continue;
                }

                var definition = CloneEnemyDefinitionForBattle(placement);
                definition.isHero = false;
                definition.occupiedSlotCount = Mathf.Max(1, definition.occupiedSlotCount);
                var unit = new BattleUnit(definition, slot);
                enemies.Add(unit);
                enemySlots[slot - 1] = unit;

                var view = CreateCombatantView(unit);
                view.transform.position = new Vector3(GetBaseSlotX(false, unit.CurrentPosition), combatantY, 0f);
                view.Initialize(unit, fallbackSprite, HandleUnitClicked, nameplatePrefab, turnIndicatorPrefab, nameplatePositionY);
                view.ResetVisualOffset();
                views[unit] = view;
            }

            if (enemies.Count == 0)
            {
                SpawnTeam(SelectRandomEnemyEncounter(), enemies, enemySlots, false);
            }
        }

        private static CombatantDefinition CloneEnemyDefinitionForBattle(AdventureEnemyPlacement placement)
        {
            var source = placement.combatant;
            var clone = Instantiate(source);
            clone.isHero = false;
            clone.currentLevel = 1;
            clone.currentExperience = 0;
            clone.currentHealth = -1;

            var level = Mathf.Max(1, placement.level);
            var extraLevels = Mathf.Max(0, level - 1);
            clone.maxHealth += extraLevels * Mathf.Max(2, source.GrowthMaxHealthPerLevel);
            clone.attack += extraLevels * Mathf.Max(1, source.GrowthAttackPerLevel);
            clone.defense += extraLevels * Mathf.Max(1, source.GrowthDefensePerLevel);
            clone.speed += Mathf.FloorToInt(extraLevels * 0.5f);
            clone.name = $"{source.name}_Lv{level}";
            return clone;
        }

#if UNITY_EDITOR
        private static IEnumerable<string> EnumerateCombatantAssets()
        {
            const string combatantsFolder = "Assets/ClockworkWastelandDemo/Data/Combatants";
            var projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/');
            var absoluteFolder = Path.Combine(Directory.GetCurrentDirectory(), combatantsFolder.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(absoluteFolder))
            {
                return Enumerable.Empty<string>();
            }

            return Directory
                .GetFiles(absoluteFolder, "*.asset", SearchOption.TopDirectoryOnly)
                .Select(path => path.Replace('\\', '/'))
                .Select(path => path.Replace(projectRoot + "/", string.Empty));
        }
#endif

        private void ClearEnemyUnits()
        {
            foreach (var enemy in enemies.ToArray())
            {
                if (views.TryGetValue(enemy, out var view))
                {
                    Destroy(view.gameObject);
                    views.Remove(enemy);
                }
            }

            enemies.Clear();
            for (var i = 0; i < enemySlots.Length; i++)
            {
                enemySlots[i] = null;
            }
        }

        private void ClearAllUnits()
        {
            ResetCombatRuntimeState(clearTurnQueue: true);

            foreach (var view in views.Values.ToArray())
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            views.Clear();
            heroes.Clear();
            enemies.Clear();
            for (var i = 0; i < MaxFormationSlots; i++)
            {
                heroSlots[i] = null;
                enemySlots[i] = null;
            }
        }

        private void SpawnTeam(IReadOnlyList<CombatantDefinition> definitions, ICollection<BattleUnit> destination, BattleUnit[] slots, bool isHero)
        {
            for (var i = 0; i < definitions.Count && i < MaxFormationSlots; i++)
            {
                definitions[i].isHero = isHero;
                definitions[i].occupiedSlotCount = Mathf.Max(1, definitions[i].occupiedSlotCount);
                var unit = new BattleUnit(definitions[i], i + 1);
                destination.Add(unit);
                slots[i] = unit;

                var view = CreateCombatantView(unit);
                view.transform.position = new Vector3(GetBaseSlotX(isHero, unit.CurrentPosition), combatantY, 0f);
                view.Initialize(unit, fallbackSprite, HandleUnitClicked, nameplatePrefab, turnIndicatorPrefab, nameplatePositionY);
                view.ResetVisualOffset();
                views[unit] = view;
            }
        }

        private static IEnumerator PlayAttackFocusRecoil(AttackLungeState state)
        {
            if (!state.IsValid)
            {
                yield break;
            }

            var duration = Mathf.Max(0.01f, BulletTimeDuration);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseInExpo(t);

                if (state.ActorView != null)
                {
                    state.ActorView.transform.position = Vector3.LerpUnclamped(state.ActorEnd, state.ActorRecoil, eased);
                }

                for (var i = 0; i < state.TargetViews.Length; i++)
                {
                    var targetView = state.TargetViews[i];
                    if (targetView == null)
                    {
                        continue;
                    }

                    targetView.transform.position = Vector3.LerpUnclamped(state.TargetEnds[i], state.TargetRecoils[i], eased);
                }

                yield return null;
            }

            if (state.ActorView != null)
            {
                state.ActorView.transform.position = state.ActorRecoil;
            }

            for (var i = 0; i < state.TargetViews.Length; i++)
            {
                if (state.TargetViews[i] != null)
                {
                    state.TargetViews[i].transform.position = state.TargetRecoils[i];
                }
            }
        }

        private IEnumerator MoveAttackFocusLungeBack(AttackLungeState state)
        {
            if (!state.IsValid)
            {
                yield break;
            }

            var duration = Mathf.Max(0.01f, attackFocusReturnDuration);
            var actorCurrent = state.ActorView != null ? state.ActorView.transform.position : state.ActorRecoil;
            var targetCurrents = state.TargetViews.Select(view => view != null ? view.transform.position : Vector3.zero).ToArray();
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseInCubic(t);

                if (state.ActorView != null)
                {
                    state.ActorView.transform.position = Vector3.LerpUnclamped(actorCurrent, state.ActorStart, eased);
                }

                for (var i = 0; i < state.TargetViews.Length; i++)
                {
                    var targetView = state.TargetViews[i];
                    if (targetView == null)
                    {
                        continue;
                    }

                    targetView.transform.position = Vector3.LerpUnclamped(targetCurrents[i], state.TargetStarts[i], eased);
                }

                yield return null;
            }

            if (state.ActorView != null)
            {
                state.ActorView.transform.position = state.ActorStart;
            }

            for (var i = 0; i < state.TargetViews.Length; i++)
            {
                if (state.TargetViews[i] != null)
                {
                    state.TargetViews[i].transform.position = state.TargetStarts[i];
                }
            }
        }

        private CombatantView CreateCombatantView(BattleUnit unit)
        {
            var prefab = unit.Definition.unitPrefab;
            var prefabSource = prefab != null ? prefab.name : string.Empty;
#if UNITY_EDITOR
            if (prefab == null && !string.IsNullOrWhiteSpace(unit.Definition.unitPrefabPath))
            {
                prefab = AssetDatabase.LoadAssetAtPath<CombatantView>(unit.Definition.unitPrefabPath);
                prefabSource = unit.Definition.unitPrefabPath;
            }
#endif
            if (prefab == null)
            {
                prefab = defaultUnitPrefab;
                prefabSource = prefab != null ? prefab.name : string.Empty;
            }

            if (prefab != null)
            {
                var view = Instantiate(prefab);
                view.name = unit.DisplayName;
                view.ConfigureFloatingText(
                    floatingTextBaseOffset,
                    floatingTextBaseScale,
                    floatingTextBurstWindow,
                    floatingTextQueueDelay,
                    floatingTextHorizontalSpacing,
                    floatingTextVerticalSpacing,
                    floatingTextAdditionalLiftPerText);
#if UNITY_EDITOR
                Debug.Log($"Spawned {unit.DisplayName} using unit prefab: {prefabSource}", view);
#endif
                return view;
            }

            var unitObject = new GameObject(unit.DisplayName);
            var fallbackView = unitObject.AddComponent<CombatantView>();
            fallbackView.ConfigureFloatingText(
                floatingTextBaseOffset,
                floatingTextBaseScale,
                floatingTextBurstWindow,
                floatingTextQueueDelay,
                floatingTextHorizontalSpacing,
                floatingTextVerticalSpacing,
                floatingTextAdditionalLiftPerText);
            return fallbackView;
        }

        private void LayoutFormation(bool isHero)
        {
            var slots = isHero ? heroSlots : enemySlots;
            for (var i = 0; i < slots.Length; i++)
            {
                var unit = slots[i];
                if (unit == null || !views.TryGetValue(unit, out var view) || !view.gameObject.activeSelf)
                {
                    continue;
                }

                unit.CurrentPosition = i + 1;
                view.transform.position = new Vector3(GetFormationSlotX(isHero, slots, unit.CurrentPosition), combatantY, 0f);
                view.ResetVisualOffset();
            }
        }

        private static float GetBaseSlotX(bool isHero, int position)
        {
            return isHero
                ? HeroFrontSlotX - (position - 1) * BaseFormationSlotSpacing
                : EnemyFrontSlotX + (position - 1) * BaseFormationSlotSpacing;
        }

        private float GetFormationSlotX(bool isHero, BattleUnit[] slots, int position)
        {
            var x = isHero ? HeroFrontSlotX : EnemyFrontSlotX;
            var direction = isHero ? -1f : 1f;
            var targetIndex = Mathf.Clamp(position - 1, 0, MaxFormationSlots - 1);

            for (var i = 1; i <= targetIndex; i++)
            {
                x += direction * GetFormationSpacing(slots.ElementAtOrDefault(i - 1), slots.ElementAtOrDefault(i));
            }

            return x;
        }

        private float GetFormationSpacing(BattleUnit leftUnit, BattleUnit rightUnit)
        {
            var leftScale = GetFormationSpacingScale(leftUnit);
            var rightScale = GetFormationSpacingScale(rightUnit);
            var largestScale = Mathf.Max(ReferenceFormationVisualScale, leftScale, rightScale);
            return BaseFormationSlotSpacing * largestScale / ReferenceFormationVisualScale;
        }

        private float GetFormationSpacingScale(BattleUnit unit)
        {
            return unit != null && views.TryGetValue(unit, out var view)
                ? view.FormationSpacingScale
                : ReferenceFormationVisualScale;
        }

        private void SetTargetHighlights(IReadOnlyCollection<BattleUnit> targets)
        {
            foreach (var pair in views)
            {
                pair.Value.SetHighlighted(targets != null && targets.Contains(pair.Key));
            }
        }

        private void RebuildTurnQueue()
        {
            turnQueue.Clear();
            turnQueue.AddRange(heroes.Concat(enemies).Where(unit => unit.CanAct).OrderByDescending(unit => unit.Speed));
        }

        private bool IsBattleOver()
        {
            return heroes.All(hero => !hero.CanAct) || enemies.All(enemy => !enemy.CanAct);
        }

        private void RefreshViews()
        {
            foreach (var view in views.Values)
            {
                view.Refresh();
            }
        }

        private void UpdateCurrentActorIndicator(BattleUnit actor, bool waitingForPlayerAction)
        {
            foreach (var pair in views)
            {
                var view = pair.Value;
                if (view == null)
                {
                    continue;
                }

                if (pair.Key == actor)
                {
                    var label = waitingForPlayerAction && actor != null && actor.IsHero ? "请行动" : "行动中";
                    var color = waitingForPlayerAction && actor != null && actor.IsHero
                        ? new Color(0.98f, 0.88f, 0.3f, 1f)
                        : new Color(0.9f, 0.36f, 0.3f, 1f);
                    view.SetTurnIndicator(true, label, color);
                }
                else
                {
                    view.SetTurnIndicator(false, string.Empty, Color.white);
                }
            }
        }

        private void ClearCurrentActorIndicator()
        {
            foreach (var view in views.Values)
            {
                view?.SetTurnIndicator(false, string.Empty, Color.white);
            }
        }

        private static SkillData CreateSwapSkill()
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "swap_position";
            skill.skillName = "\u8c03\u6574\u7ad9\u4f4d";
            skill.description = "\u548c\u4e00\u540d\u961f\u53cb\u4ea4\u6362\u7ad9\u4f4d\u3002";
            skill.skillType = SkillDataType.控制;
            skill.targetType = SkillDataTargetType.单友;
            skill.casterAllowedPositions = AnyPosition;
            skill.targetAllowedPositions = AnyPosition;
            skill.baseValue = 0;
            skill.isSwapSkill = true;
            return skill;
        }

        private static SkillData CreatePassTurnSkill()
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "pass_turn";
            skill.skillName = "跳过回合";
            skill.description = "跳过回合且不执行技能，直接结束行动。";
            skill.skillType = SkillDataType.控制;
            skill.targetType = SkillDataTargetType.自己;
            skill.baseValue = 0;
            skill.powerMultiplier = 1f;
            skill.casterAllowedPositions = new[] { 1, 2, 3, 4 };
            skill.targetAllowedPositions = new[] { 1, 2, 3, 4 };
            skill.manaCost = 0;
            skill.cooldown = 0;
            skill.overlayDuration = 0.05f;
            skill.isPassSkill = true;
            return skill;
        }

        private static Sprite CreateFallbackSprite()
        {
            var texture = new Texture2D(32, 48);
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var edge = x < 2 || y < 2 || x > texture.width - 3 || y > texture.height - 3;
                    texture.SetPixel(x, y, edge ? new Color(0.05f, 0.05f, 0.05f) : Color.white);
                }
            }

            texture.filterMode = FilterMode.Point;
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 24f);
        }

        private Sprite ResolveBattleBackground()
        {
            if (battleBackgrounds == null || battleBackgrounds.Length == 0)
            {
                battleBackgrounds = new[]
                {
                    EditorSpriteSheetLoader.LoadSprite("Assets/Art/Environments/Backgrounds/battle_bg_01.png"),
                    EditorSpriteSheetLoader.LoadSprite("Assets/Art/Environments/Backgrounds/battle_bg_02.png"),
                    EditorSpriteSheetLoader.LoadSprite("Assets/Art/Environments/Backgrounds/battle_bg_03.png"),
                    EditorSpriteSheetLoader.LoadSprite("Assets/Art/Environments/Backgrounds/battle_bg_04.png"),
                    EditorSpriteSheetLoader.LoadSprite("Assets/Art/Environments/Backgrounds/battle_bg_05.png"),
                    EditorSpriteSheetLoader.LoadSprite("Assets/Art/Environments/Backgrounds/battle_bg_06.png")
                }.Where(sprite => sprite != null).ToArray();
            }

            if (battleBackgrounds == null || battleBackgrounds.Length == 0)
            {
                return null;
            }

            var index = Mathf.Clamp(battleBackgroundIndex, 0, battleBackgrounds.Length - 1);
            return battleBackgrounds[index];
        }

        private void UpdateBackdrop(Sprite backgroundSprite, Vector2 offset, float scaleMultiplier)
        {
            if (battleBackdropRenderer == null)
            {
                var background = new GameObject("Battle Background");
                battleBackdropRenderer = background.AddComponent<SpriteRenderer>();
                battleBackdropRenderer.sortingOrder = -20;
                battleBackdropRenderer.color = Color.white;
                background.transform.position = new Vector3(0f, 0f, 4f);
            }

            if (battleBackdropRenderer == null)
            {
                return;
            }

            battleBackdropRenderer.sprite = backgroundSprite;
            var backgroundTransform = battleBackdropRenderer.transform;
            backgroundTransform.position = new Vector3(offset.x, offset.y, 4f);

            if (backgroundSprite != null)
            {
                var spriteSize = backgroundSprite.bounds.size;
                if (spriteSize.x > 0f && spriteSize.y > 0f)
                {
                    var scale = Mathf.Max(0.01f, scaleMultiplier);
                    backgroundTransform.localScale = new Vector3(scale, scale, 1f);
                }
            }
            else
            {
                backgroundTransform.localScale = Vector3.one;
            }
        }

        private readonly struct DamageResult
        {
            public DamageResult(int amount, bool isCritical)
            {
                Amount = amount;
                IsCritical = isCritical;
            }

            public int Amount { get; }
            public bool IsCritical { get; }
        }
    }
}





