using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        public AttackLungeState(CombatantView actorView, Vector3 actorStart, Vector3 actorEnd, CombatantView[] targetViews, Vector3[] targetStarts, Vector3[] targetEnds)
        {
            ActorView = actorView;
            ActorStart = actorStart;
            ActorEnd = actorEnd;
            TargetViews = targetViews;
            TargetStarts = targetStarts;
            TargetEnds = targetEnds;
        }

        public CombatantView ActorView { get; }
        public Vector3 ActorStart { get; }
        public Vector3 ActorEnd { get; }
        public CombatantView[] TargetViews { get; }
        public Vector3[] TargetStarts { get; }
        public Vector3[] TargetEnds { get; }
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
        public AdventureMapOption(string mapId, string displayName, string description, int difficulty)
        {
            MapId = mapId;
            DisplayName = displayName;
            Description = description;
            Difficulty = difficulty;
        }

        public string MapId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int Difficulty { get; }
    }

    [System.Serializable]
    public sealed class GameSaveData
    {
        public int gold;
        public List<HeroSaveData> heroes = new List<HeroSaveData>();
    }

    [System.Serializable]
    public sealed class HeroSaveData
    {
        public string characterId;
        public bool unlocked;
        public int level;
        public int experience;
        public int health;
    }

    public sealed class BattleController : MonoBehaviour
    {
        private const int MaxFormationSlots = 4;
        private const int MapNodeCount = 3;
        private const int InitialGold = 1200;
        private const float FormationFeetY = -1.8f;
        private const float HeroFrontSlotX = -0.8f;
        private const float EnemyFrontSlotX = 1.3f;
        private const float BaseFormationSlotSpacing = 1.1f;
        private const float ReferenceFormationVisualScale = 0.8f;
        private const int AttackFocusActorSortingOrder = 120;
        private const int AttackFocusTargetSortingOrder = 121;
        private const string SaveKey = "ClockworkWasteland.Save.v1";
        private const float MinCombatOverlayDuration = 0.56f;
        private const float BulletTimeDuration = 0.62f;
        private const float BulletTimeMinScale = 0.08f;
        private static readonly int[] AnyPosition = { 1, 2, 3, 4 };

        [SerializeField] private CombatantDefinition[] heroParty;
        [SerializeField] private CombatantDefinition[] enemyParty;
        [SerializeField] private CombatantDefinition[] heroPoolConfig;

        [Header("Presentation")]
        [SerializeField] private BattleUI battleUIPrefab;
        [SerializeField] private CombatantView defaultUnitPrefab;
        [SerializeField] private CombatNameplate nameplatePrefab;
        [SerializeField] private float heroVisualScale = 0.8f;
        [SerializeField] private Sprite[] battleBackgrounds;
        [SerializeField] private int battleBackgroundIndex = 0;

        [Header("Attack Focus")]
        [SerializeField] private Material attackFocusBlurMaterial;
        [SerializeField] private float attackFocusZoomRatio = 0.62f;
        [SerializeField] private float attackFocusScale = 1.2f;
        [SerializeField] private float attackFocusBlurStrength = 0.45f;
        [SerializeField] private Color attackFocusBlurTint = new Color(0.68f, 0.68f, 0.72f, 1f);
        [SerializeField] private float attackFocusLungeDuration = 0.09f;
        [SerializeField] private float attackFocusHitPause = 0.05f;
        [SerializeField, Range(0f, 0.9f)] private float attackFocusSingleLungeRatio = 0.4f;
        [SerializeField] private float attackFocusSingleMinDistance = 0.8f;
        [SerializeField] private float attackFocusAoeActorOffset = 1.15f;
        [SerializeField] private float attackFocusAoeTargetOffset = 1.35f;
        [SerializeField] private float attackFocusAoeTargetSpacing = 1.35f;

        private readonly List<BattleUnit> heroes = new List<BattleUnit>();
        private readonly List<BattleUnit> enemies = new List<BattleUnit>();
        private readonly BattleUnit[] heroSlots = new BattleUnit[MaxFormationSlots];
        private readonly BattleUnit[] enemySlots = new BattleUnit[MaxFormationSlots];
        private readonly Dictionary<BattleUnit, CombatantView> views = new Dictionary<BattleUnit, CombatantView>();
        private readonly List<BattleUnit> turnQueue = new List<BattleUnit>();

        private BattleUI ui;
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

        public void Configure(CombatantDefinition[] heroesToUse, CombatantDefinition[] enemiesToUse, BattleUI uiPrefabToUse = null, CombatantView unitPrefabToUse = null, CombatNameplate nameplatePrefabToUse = null)
        {
            heroParty = heroesToUse;
            enemyParty = enemiesToUse;
            battleUIPrefab = uiPrefabToUse;
            defaultUnitPrefab = unitPrefabToUse;
            nameplatePrefab = nameplatePrefabToUse;
        }

        private void Start()
        {
            fallbackSprite = CreateFallbackSprite();
            swapSkill = CreateSwapSkill();
            SetupScene();
            CombatAudio.Ensure();
            CacheDefaultCamera();
            LoadDefaultPresentationPrefabs();
            shopItems = LoadShopItems();
            LoadOrCreateGameState();
            ShowLobby();
        }

        public void RunAttackFlowByIndices(int attackerIndex, int skillIndex, int targetIndex)
        {
            var attacker = heroes.Concat(enemies).Where(unit => unit.CanAct).ElementAtOrDefault(attackerIndex);
            var skills = attacker != null ? GetSkillList(attacker).ToArray() : new SkillData[0];
            if (attacker == null || skillIndex < 0 || skillIndex >= skills.Length)
            {
                ui?.AddLog("\u653b\u51fb\u6d41\u7a0b\u53c2\u6570\u65e0\u6548\u3002");
                return;
            }

            var skill = skills[skillIndex];
            var state = GetSkillUseState(attacker, skill);
            if (!state.CanUse)
            {
                ui?.AddLog($"\u6280\u80fd\u4e0d\u53ef\u7528\uff1a{state.DisabledReason}\u3002");
                return;
            }

            var candidates = GetValidTargets(attacker, skill).ToArray();
            if (targetIndex < 0 || targetIndex >= candidates.Length)
            {
                ui?.AddLog("\u76ee\u6807\u7d22\u5f15\u65e0\u6548\u3002");
                return;
            }

            StartCoroutine(ExecuteSkill(attacker, skill, candidates[targetIndex]));
        }

        private void ShowTitleScreen()
        {
            ShowLobby();
        }

        private void PrepareNonCombatScreen(string turnLabel, bool stopMusic = true)
        {
            if (stopMusic)
            {
                CombatAudio.Instance.StopMusic();
            }

            ClearAllUnits();
            ui.HideOverlay();
            ui.ClearActionPanels();
            ui.SetRound(0);
            ui.SetTurn(turnLabel);
            ui.SetGold(gold);
        }

        private void ResetCombatSelectionState()
        {
            waitingForPlayer = false;
            resolvingPlayerAction = false;
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
                ui.AddLog("\u8bf7\u81f3\u5c11\u9009\u62e9 1 \u540d\u82f1\u96c4\u3002");
                return false;
            }

            var deadHero = selectedHeroDefinitions.FirstOrDefault(hero => hero != null && hero.IsDead);
            if (deadHero != null)
            {
                ui.AddLog($"{deadHero.displayName} \u5df2\u6b7b\u4ea1\uff0c\u9700\u8981\u5148\u590d\u6d3b\u624d\u80fd\u51fa\u6218\u3002");
                return false;
            }

            return true;
        }

        private void BeginSelectedAdventure()
        {
            ClearAllUnits();
            ui.HideOverlay();
            ui.ClearActionPanels();
            CombatAudio.Instance.PlayStartExpedition();
            CommitSelectedPartyToHeroParty();
            SetupHeroUnits();
            StartCoroutine(SelectedAdventureLoop());
        }

        private void ShowLobby()
        {
            PrepareNonCombatScreen("\u5927\u5385");
            ui.ShowLobby(gold, ShowTavern, ShowAdventureMap, ShowHeroCodex, ShowSettings, QuitGame);
        }

        private void StartNewGame()
        {
            ResetGameState();
            SaveGameState();
            ShowLobby();
        }

        private void ResetGameState()
        {
            StopAllCoroutines();
            ClearAllUnits();
            gold = InitialGold;
            inventory.Clear();
            totalHeroPool = BuildHeroPool();
            NormalizeHeroPool(totalHeroPool);
            availableHeroPool = GetUnlockedHeroPool();
            SyncSelectedHeroDefinitions(fillToMax: true);
            currentBattleNumber = 0;
            CommitSelectedPartyToHeroParty();
            ui.SetGold(gold);
            RefreshTavernOffers();
        }

        private void ShowSettings()
        {
            PrepareNonCombatScreen("\u8bbe\u7f6e", false);
            ui.ShowSettingsScreen(ShowLobby);
        }

        private void QuitGame()
        {
            ui.AddLog("\u9000\u51fa\u6e38\u620f\u3002");
            Application.Quit();
        }

        private void ShowHeroCodex()
        {
            PrepareNonCombatScreen("\u82f1\u96c4\u56fe\u9274");
            ui.ShowHeroCodex(totalHeroPool, ShowLobby);
        }

        private void ShowAdventureMap()
        {
            PrepareNonCombatScreen("\u5192\u9669");
            ui.ShowAdventureMap(GetAdventureMaps(), SelectAdventureMap, ShowLobby);
        }

        private void SelectAdventureMap(AdventureMapOption map)
        {
            selectedAdventureMap = map;
            ui.AddLog($"\u9009\u62e9\u5730\u56fe\uff1a{map.DisplayName}\u3002");
            ShowTeamSelection();
        }

        private void ShowTeamSelection()
        {
            PrepareNonCombatScreen("\u961f\u4f0d\u914d\u7f6e");
            SyncSelectedHeroDefinitions(fillToMax: false);
            ui.ShowTeamSelection(GetDeployableHeroPool(), selectedHeroDefinitions, ToggleHeroSelection, StartSelectedBattleSequence, ShowAdventureMap);
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
                ui.AddLog("\u6700\u591a\u53ea\u80fd\u9009\u62e9 4 \u540d\u82f1\u96c4\u3002");
            }

            SortSelectedHeroesInPlace();
            ui.ShowTeamSelection(GetDeployableHeroPool(), selectedHeroDefinitions, ToggleHeroSelection, StartSelectedBattleSequence, ShowAdventureMap);
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
            ui.ShowShop(shopItems, gold, GetInventoryStacks(), BuyItem, ShowTeamSelection);
        }

        private void ShowInventory()
        {
            ui.ShowInventory(GetInventoryStacks(), availableHeroPool, UseItemOnHero, ShowTeamSelection);
        }

        private void ShowTavern()
        {
            if (tavernOffers.Count == 0)
            {
                RefreshTavernOffers();
            }

            ui.ShowTavern(tavernOffers, gold, RecruitHero, ShowLobby);
        }

        private void RecruitHero(CombatantDefinition hero)
        {
            if (hero == null)
            {
                return;
            }

            if (hero.isUnlocked)
            {
                ui.AddLog($"{hero.displayName} \u5df2\u7ecf\u52a0\u5165\u82f1\u96c4\u6c60\u3002");
                ShowTavern();
                return;
            }

            if (gold < hero.recruitPrice)
            {
                ui.AddLog("\u91d1\u5e01\u4e0d\u8db3\uff0c\u65e0\u6cd5\u62db\u52df\u8be5\u82f1\u96c4\u3002");
                ShowTavern();
                return;
            }

            gold -= hero.recruitPrice;
            hero.isUnlocked = true;
            hero.EnsureRuntimeHealth();
            availableHeroPool = GetUnlockedHeroPool();
            selectedHeroDefinitions.RemoveAll(selected => selected == null || !selected.isUnlocked);
            ui.SetGold(gold);
            SaveGameState();
            RefreshTavernOffers();
            ui.AddLog($"\u62db\u52df\u4e86 {hero.displayName}\uff0c\u4ed6\u5df2\u52a0\u5165\u82f1\u96c4\u6c60\u3002");
            ui.ShowContinuePrompt($"\u62db\u52df\u6210\u529f\uff1a{hero.displayName}", "\u7ee7\u7eed", ShowTavern);
        }

        private void BuyItem(InventoryItemData item)
        {
            if (item == null)
            {
                return;
            }

            if (gold < item.price)
            {
                ui.AddLog("\u91d1\u5e01\u4e0d\u8db3\u3002");
                ShowShop();
                return;
            }

            gold -= item.price;
            inventory[item] = inventory.TryGetValue(item, out var count) ? count + 1 : 1;
            ui.SetGold(gold);
            SaveGameState();
            ui.AddLog($"\u8d2d\u4e70\u4e86 {item.itemName}\u3002");
            ShowShop();
        }

        private void UseItemOnHero(InventoryItemData item, CombatantDefinition hero)
        {
            if (item == null || hero == null || !inventory.TryGetValue(item, out var count) || count <= 0)
            {
                return;
            }

            var used = false;
            switch (item.effectType)
            {
                case InventoryItemEffectType.Revive:
                    var revivedHealth = hero.ReviveOutsideBattle(item.reviveHealthPercent);
                    used = revivedHealth > 0;
                    ui.AddLog(used
                        ? $"{hero.displayName} \u88ab\u590d\u6d3b\uff0c\u751f\u547d\u6062\u590d\u5230 {revivedHealth}/{hero.MaxHealthWithGrowth}\u3002"
                        : $"{item.itemName} \u53ea\u80fd\u5bf9\u6b7b\u4ea1\u82f1\u96c4\u4f7f\u7528\u3002");
                    break;
                case InventoryItemEffectType.Heal:
                default:
                    var healed = hero.HealOutsideBattle(item.healAmount);
                    used = healed > 0;
                    ui.AddLog(used
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

        private CombatantDefinition[] GetDeployableHeroPool()
        {
            return availableHeroPool
                .Where(hero => hero != null && hero.isHero && hero.isUnlocked && !hero.IsDead)
                .OrderBy(GetPreferredRowSortOrder)
                .ThenBy(GetArchetypeSortOrder)
                .ThenBy(hero => hero.displayName)
                .ToArray();
        }

        private IReadOnlyList<CombatantDefinition> GetTavernOffers()
        {
            return totalHeroPool
                .Where(hero => hero != null && hero.isHero && !hero.isUnlocked)
                .OrderBy(_ => Random.value)
                .Take(3)
                .ToArray();
        }

        private void RefreshTavernOffers()
        {
            tavernOffers.Clear();
            tavernOffers.AddRange(GetTavernOffers());
        }

        private static IReadOnlyList<AdventureMapOption> GetAdventureMaps()
        {
            return new[]
            {
                new AdventureMapOption("rust_wastes", "\u9508\u94c1\u8352\u539f", "\u6807\u51c6\u5192\u9669\u8def\u7ebf\uff0c\u9002\u5408\u6d4b\u8bd5\u961f\u4f0d\u3002", 1),
                new AdventureMapOption("ember_foundry", "\u4f59\u70ec\u94f8\u5382", "\u66f4\u591a\u6218\u6597\u8282\u70b9\uff0c\u5956\u52b1\u66f4\u9ad8\u3002", 2),
                new AdventureMapOption("clockwork_depths", "\u53d1\u6761\u6df1\u5904", "\u9ad8\u5371\u9669\u8def\u7ebf\uff0c\u540e\u7eed\u53ef\u63a5 Boss \u548c\u7279\u6b8a\u4e8b\u4ef6\u3002", 3)
            };
        }

        private void LoadOrCreateGameState()
        {
            totalHeroPool = BuildHeroPool();
            NormalizeHeroPool(totalHeroPool);

            if (PlayerPrefs.HasKey(SaveKey))
            {
                try
                {
                    var save = JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString(SaveKey));
                    ApplySaveData(save);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning($"Failed to load combat save data. Starting a new state. {exception.Message}");
                    gold = InitialGold;
                }
            }
            else
            {
                gold = InitialGold;
            }

            availableHeroPool = GetUnlockedHeroPool();
            SyncSelectedHeroDefinitions(fillToMax: true);
            CommitSelectedPartyToHeroParty();
            RefreshTavernOffers();
            ui.SetGold(gold);
        }

        private void ApplySaveData(GameSaveData save)
        {
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
                    hero.EnsureRuntimeHealth();
                    continue;
                }

                hero.isUnlocked = state.unlocked;
                hero.currentLevel = Mathf.Max(1, state.level);
                hero.currentExperience = Mathf.Max(0, state.experience);
                hero.currentHealth = Mathf.Clamp(state.health, 0, hero.MaxHealthWithGrowth);
            }
        }

        private CombatantDefinition[] BuildHeroPool()
        {
            var configuredPool = heroPoolConfig != null
                ? heroPoolConfig.Where(hero => hero != null).Select(Instantiate).ToArray()
                : System.Array.Empty<CombatantDefinition>();

            return configuredPool.Length > 0
                ? configuredPool
                : DemoBattleBootstrap.CreateHeroPool();
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

            var save = new GameSaveData
            {
                gold = Mathf.Max(0, gold),
                heroes = totalHeroPool
                    .Where(hero => hero != null && hero.isHero)
                    .Select(hero => new HeroSaveData
                    {
                        characterId = hero.characterId,
                        unlocked = hero.isUnlocked,
                        level = hero.Level,
                        experience = hero.Experience,
                        health = hero.CurrentHealth
                    })
                    .ToList()
            };

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
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
            currentBattleNumber = Mathf.Max(1, selectedAdventureMap.Difficulty);
            ui.AddLog($"\u5192\u9669\u5f00\u59cb\uff1a{selectedAdventureMap.DisplayName}\u3002");
            yield return StartCoroutine(RunCombatEncounter(selectedAdventureMap.Difficulty >= 3));

            if (!heroes.Any(hero => hero.CanAct))
            {
                yield return StartCoroutine(ShowDefeatAndReturn());
                yield break;
            }

            SaveGameState();
            ShowLobby();
        }

        private IEnumerator MapExpeditionLoop()
        {
            currentBattleNumber = 0;
            ui.AddLog("\u8fdc\u5f81\u5f00\u59cb\u3002");

            for (var mapStep = 1; mapStep <= MapNodeCount; mapStep++)
            {
                var nodeSelected = false;
                var selectedNode = default(MapNodeOption);
                ui.ClearActionPanels();
                ui.SetRound(0);
                ui.SetTurn($"\u5730\u56fe {mapStep}/{MapNodeCount}");
                ui.ShowMap(mapStep, MapNodeCount, GenerateMapOptions(mapStep), node =>
                {
                    selectedNode = node;
                    nodeSelected = true;
                });

                while (!nodeSelected)
                {
                    yield return null;
                }

                ui.HideOverlay();

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
            ui.AddLog("\u8fdc\u5f81\u8d70\u5230\u5c3d\u5934\uff0cBoss \u51fa\u73b0\u4e86\u3002");
            yield return StartCoroutine(RunCombatEncounter(true));

            if (!heroes.Any(hero => hero.CanAct))
            {
                yield return StartCoroutine(ShowDefeatAndReturn());
                yield break;
            }

            ui.ClearActionPanels();
            ui.SetTurn("\u606d\u559c\u901a\u5173");
            ui.AddLog("Boss \u5df2\u88ab\u51fb\u8d25\uff0c\u8fdc\u5f81\u901a\u5173\u3002");
            var requestedReturn = false;
            ui.ShowContinuePrompt("\u606d\u559c\u901a\u5173", "\u8fd4\u56de\u5927\u5385", () => requestedReturn = true);
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
            ui.SetTurn(bossBattle ? "Boss \u6218\u80dc\u5229" : "\u6218\u6597\u80dc\u5229");
            ui.ShowRewardScreen(goldGained, gold, rewardResults, () => continueRequested = true);
            ui.AddLog(bossBattle ? "Boss \u6218\u80dc\u5229\u3002" : "\u6218\u6597\u80dc\u5229\uff0c\u961f\u4f0d\u72b6\u6001\u5df2\u4fdd\u7559\u3002");

            while (!continueRequested)
            {
                yield return null;
            }
        }

        private IEnumerator RunRestNode()
        {
            var selected = false;
            ui.SetTurn("\u4f11\u606f");
            ui.ShowRestNode(selectedHeroDefinitions, hero =>
            {
                if (hero != null)
                {
                    var healed = hero.HealOutsideBattle(20);
                    if (healed > 0)
                    {
                        CombatAudio.Instance.PlayHeal();
                    }

                    ui.AddLog(healed > 0
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

            ui.HideOverlay();
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
                if (Random.value >= 0.45f)
                {
                    options.Add(new MapNodeOption(MapNodeType.Chest, "\u5b9d\u7bb1\u8282\u70b9", "\u83b7\u5f97 50-150 \u91d1\u5e01\u3002"));
                }
            }

            return options;
        }

        private IEnumerator RunChestNode()
        {
            var gained = Random.Range(50, 151);
            gold += gained;
            ui.SetGold(gold);
            ui.AddLog($"\u6253\u5f00\u5b9d\u7bb1\uff0c\u83b7\u5f97\u91d1\u5e01 {gained}\u3002");
            SaveGameState();

            var continueRequested = false;
            ui.ShowContinuePrompt($"\u5b9d\u7bb1\uff1a\u83b7\u5f97 {gained} \u91d1\u5e01", "\u7ee7\u7eed", () => continueRequested = true);
            while (!continueRequested)
            {
                yield return null;
            }
        }

        private IEnumerator ShowDefeatAndReturn()
        {
            ui.ClearActionPanels();
            ui.SetTurn("\u6218\u8d25");
            ui.AddLog("\u8fdc\u5f81\u961f\u5012\u4e0b\u4e86\uff0c\u8fdc\u5f81\u7ec8\u6b62\u3002");
            var returnRequested = false;
            ui.ShowContinuePrompt("\u6218\u8d25", "\u8fd4\u56de\u5927\u5385", () => returnRequested = true);
            while (!returnRequested)
            {
                yield return null;
            }

            SaveGameState();
            ShowLobby();
        }

        private IReadOnlyList<BattleRewardResult> GrantVictoryRewards(out int goldGained)
        {
            goldGained = Random.Range(50, 151);
            gold += goldGained;
            ui.SetGold(gold);
            ui.AddLog($"\u83b7\u5f97\u91d1\u5e01 {goldGained}\u3002");

            var results = new List<BattleRewardResult>();
            foreach (var hero in heroes.Where(unit => unit.IsHero && unit.IsAlive && !unit.IsCorpse))
            {
                var progression = hero.Definition.GrantExperienceReward(Random.Range(10, 21));
                results.Add(new BattleRewardResult(hero.Definition, progression.ExperienceGained, progression.LevelsGained));
                ui.AddLog($"{hero.Definition.displayName} \u83b7\u5f97 {progression.ExperienceGained} \u7ecf\u9a8c\u3002");

                if (progression.HealthRestoredFromGrowth > 0)
                {
                    hero.RestoreForMaxHealthGain(progression.HealthRestoredFromGrowth);
                }

                if (progression.LevelsGained > 0)
                {
                    for (var level = progression.LevelBefore + 1; level <= progression.LevelAfter; level++)
                    {
                        ui.AddLog($"{hero.Definition.displayName} \u5347\u5230\u4e86 {level} \u7ea7\u3002");
                    }
                }
            }

            RefreshViews();
            SaveGameState();
            return results;
        }

        private IEnumerator BattleLoop()
        {
            ui.AddLog($"\u7b2c {currentBattleNumber} \u573a\u6218\u6597\u5f00\u59cb\u3002");

            while (!IsBattleOver())
            {
                round++;
                ui.SetRound(round);
                RebuildTurnQueue();

                foreach (var actor in turnQueue.ToArray())
                {
                    if (!actor.CanAct || IsBattleOver())
                    {
                        continue;
                    }

                    currentActor = actor;
                    actor.ResetActionPoints();
                    yield return StartCoroutine(RunTurn(actor));
                }
            }

            ResetCombatRuntimeState(clearTurnQueue: true);
            ui.ClearActionPanels();
            ui.SetTurn(heroes.Any(hero => hero.CanAct) ? "\u80dc\u5229" : "\u5931\u8d25");
            ui.AddLog(heroes.Any(hero => hero.CanAct) ? "\u5c0f\u961f\u6491\u8fc7\u4e86\u8fd9\u573a\u906d\u9047\u3002" : "\u8fdc\u5f81\u961f\u5012\u4e0b\u4e86\u3002");
        }

        private IEnumerator RunTurn(BattleUnit actor)
        {
            var stunnedAtTurnStart = actor.IsStunned;
            var statusDamage = actor.TickStatuses();
            if (!actor.IsAlive)
            {
                HandleDefeatedUnit(actor, null);
            }

            RefreshViews();

            if (statusDamage > 0)
            {
                ui.AddLog($"{actor.DisplayName} \u53d7\u5230 {statusDamage} \u70b9\u72b6\u6001\u4f24\u5bb3\u3002");
                yield return new WaitForSeconds(0.45f);
            }

            if (stunnedAtTurnStart || !actor.CanAct)
            {
                if (stunnedAtTurnStart)
                {
                    ui.AddLog($"{actor.DisplayName} \u88ab\u7729\u6655\uff0c\u65e0\u6cd5\u884c\u52a8\u3002");
                }

                actor.TickSkillCooldowns();
                yield break;
            }

            actor.TickSkillCooldowns();
            ApplyPassiveOnTurnStart(actor);
            ApplyArchetypeTurnStart(actor);

            if (!actor.IsAlive || !actor.CanAct)
            {
                RefreshViews();
                yield break;
            }

            if (actor.IsHero)
            {
                waitingForPlayer = true;
                selectedUnit = actor;
                selectedSkill = null;
                validSelectedTargets = new BattleUnit[0];
                SetTargetHighlights(validSelectedTargets);
                ui.RenderPlayerTurn(actor, GetSkillUseStates(actor), SelectSkill);

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
                ui.AddLog($"\u65e0\u6cd5\u4f7f\u7528 {skill.skillName}\uff1a{state.DisabledReason}\u3002");
                ui.RenderUnitPanel(selectedUnit, currentActor, GetSkillUseStates(selectedUnit), SelectSkill);
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

            ui.RenderTargets(skill, validSelectedTargets, SelectTarget);
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
                ui.AddLog("\u8be5\u6280\u80fd\u65e0\u6cd5\u653b\u51fb\u8fd9\u4e2a\u76ee\u6807\u3002");
                SetTargetHighlights(validSelectedTargets);
                return;
            }

            selectedUnit = unit;
            selectedSkill = null;
            validSelectedTargets = new BattleUnit[0];
            SetTargetHighlights(validSelectedTargets);
            ui.RenderUnitPanel(selectedUnit, waitingForPlayer ? currentActor : null, GetSkillUseStates(selectedUnit), SelectSkill);
        }

        private void SelectTarget(BattleUnit target)
        {
            if (!waitingForPlayer || resolvingPlayerAction || currentActor == null || selectedSkill == null)
            {
                return;
            }

            if (!IsValidTarget(currentActor, selectedSkill, target))
            {
                ui.AddLog("\u8be5\u76ee\u6807\u5df2\u4e0d\u518d\u5408\u6cd5\uff0c\u8bf7\u91cd\u65b0\u9009\u62e9\u3002");
                validSelectedTargets = GetValidTargets(currentActor, selectedSkill).ToArray();
                SetTargetHighlights(validSelectedTargets);
                return;
            }

            StartCoroutine(ResolvePlayerAction(target));
        }

        private IEnumerator ResolvePlayerAction(BattleUnit target)
        {
            resolvingPlayerAction = true;
            ui.ClearActionPanels();
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
                ui.AddLog("\u884c\u52a8\u5df2\u5931\u6548\uff0c\u8bf7\u91cd\u65b0\u9009\u62e9\u3002");
                yield break;
            }

            if (skill.isSwapSkill)
            {
                yield return StartCoroutine(ExecuteSwap(actor, primaryTarget, skill));
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
            var useImpactPresentation = IsImpactPresentationSkill(skill);
            if (useImpactPresentation)
            {
                CombatAudio.Instance.PlayAttack(skill);
            }

            var mainTarget = targets[0];
            views.TryGetValue(mainTarget, out var mainTargetView);
            var targetViews = targets
                .Select(target => views.TryGetValue(target, out var view) ? view : null)
                .Where(view => view != null)
                .ToArray();
            SetTargetHighlights(new[] { mainTarget });

            yield return StartCoroutine(BeginAttackFocus(actorView, mainTargetView, targetViews));
            var lungeState = CreateAttackLungeState(actorView, targetViews);
            yield return StartCoroutine(MoveAttackFocusLunge(lungeState, true));
            yield return new WaitForSecondsRealtime(attackFocusHitPause);

            var overlayDuration = Mathf.Max(MinCombatOverlayDuration, skill.overlayDuration);
            if (useImpactPresentation && mainTargetView != null)
            {
                var attackSprite = ResolveAttackSprite(actor, skill);
                var hitSprite = ResolveHitSprite(mainTarget, skill);
                yield return StartCoroutine(PlayAttackAndHitOverlays(actorView, mainTargetView, attackSprite, hitSprite, overlayDuration));
            }
            else if (useImpactPresentation)
            {
                yield return StartCoroutine(actorView.PlayOverlay(ResolveAttackSprite(actor, skill), overlayDuration));
            }

            yield return StartCoroutine(ScreenShake(GetShakeStrength(skill), Mathf.Clamp(skill.baseValue * 0.015f, 0.1f, 0.2f)));

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
                            protectorDamage = Mathf.Max(1, Mathf.RoundToInt(damage.Amount * 0.3f));
                            targetDamage = Mathf.Max(1, damage.Amount - protectorDamage);
                            ui.AddLog($"{protector.DisplayName} 挺身保护 {target.DisplayName}，分担了 {protectorDamage} 点伤害。");
                            ApplyRawDamage(protector, protectorDamage, actor);
                        }

                        var ironWillTriggered = ApplyPassiveBeforeDeath(target, targetDamage);
                        if (!ironWillTriggered)
                        {
                            ApplyRawDamage(target, targetDamage, actor);
                        }

                        CombatAudio.Instance.PlayImpact();
                        ui.AddLog(damage.IsCritical
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
                    CombatAudio.Instance.PlayHeal();
                    ui.AddLog($"{actor.DisplayName} \u4f7f\u7528 {skill.skillName}\uff0c{target.DisplayName} \u6062\u590d {amount} \u70b9\u751f\u547d\u3002");

                    if (views.TryGetValue(target, out var targetView))
                    {
                        targetView.ShowFloatingText("+" + amount, GetDamageColor(skill.skillType));
                    }
                }

                if (skill.applyBuff != null && target.CanAct)
                {
                    target.AddOrRefreshBuff(skill.applyBuff);
                    ui.AddLog($"{target.DisplayName} \u83b7\u5f97\u72b6\u6001\uff1a{skill.applyBuff.buffName}\u3002");
                }

                ApplySkillSpecificPostEffect(actor, skill, target);
            }

            ApplySkillSpecificActionReward(actor, skill, targets);
            ApplySkillSpecificActionRisk(actor, skill, targets);
            ApplyArchetypeActionResource(actor, skill, targets);

            yield return StartCoroutine(MoveAttackFocusLunge(lungeState, false));
            RefreshViews();
            LayoutFormation(true);
            LayoutFormation(false);
            yield return StartCoroutine(EndAttackFocus(actorView, mainTargetView));
            SetTargetHighlights(new BattleUnit[0]);
            yield return new WaitForSeconds(0.25f);
        }

        private IEnumerator ExecuteSwap(BattleUnit actor, BattleUnit target, SkillData skill)
        {
            if (actor == target)
            {
                ui.AddLog("\u4e0d\u80fd\u548c\u81ea\u5df1\u6362\u4f4d\u3002");
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
            ui.AddLog($"{actor.DisplayName} \u548c {target.DisplayName} \u4ea4\u6362\u4e86\u7ad9\u4f4d\u3002");
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
                moves.Add(view.StartCoroutine(view.MoveToFormation(targetX, FormationFeetY, duration)));
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

            if (!actor.HasResourcesFor(skill))
            {
                return new SkillUseState(skill, false, "\u8d44\u6e90\u4e0d\u8db3");
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

            return unit != null && unit.IsHero ? definedSkills.Concat(new[] { swapSkill }) : definedSkills;
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
                .ThenBy(skill => skill.manaCost)
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
                    score += skill.targetType == SkillDataTargetType.单友 ? 8f : 0f;
                    score += enemyTargets.Any(target => target.CurrentPosition <= 2) ? 8f : 0f;
                    break;
                case CombatArchetype.Executioner:
                    score += skill.skillType == SkillDataType.伤害 ? 18f : 0f;
                    score += enemyTargets.Any(target => target.HealthRatio <= 0.35f) ? 14f : 0f;
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

            if (!unit.IsCorpse)
            {
                unit.ConvertToCorpse();
                ui.AddLog($"{unit.Definition.displayName} \u5012\u4e0b\uff0c\u7559\u4e0b\u4e86\u5c38\u4f53\u3002");
                RefreshViews();
                return;
            }

            RemoveUnitFromFormation(unit);
            ui.AddLog($"{unit.DisplayName} \u88ab\u6e05\u9664\u4e86\u3002");
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

        private static IEnumerator PlayAttackAndHitOverlays(CombatantView actorView, CombatantView targetView, Sprite attackSprite, Sprite hitSprite, float duration)
        {
            actorView.SetCombatEmphasis(true, 60);
            targetView.SetCombatEmphasis(true, 61);
            var bulletTime = actorView.StartCoroutine(PlayBulletTime());
            var attack = actorView.StartCoroutine(actorView.PlayOverlay(attackSprite, duration, 0.1f, 80));
            var hit = targetView.StartCoroutine(targetView.PlayHitOverlay(hitSprite, duration, 81));
            yield return attack;
            yield return hit;
            yield return bulletTime;
            actorView.SetCombatEmphasis(false, 0);
            targetView.SetCombatEmphasis(false, 0);
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

        private static bool IsImpactPresentationSkill(SkillData skill)
        {
            return skill != null && (skill.skillType == SkillDataType.伤害 || skill.skillType == SkillDataType.控制);
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
            var randomOffset = Random.Range(-2, 3);
            var attack = GetEffectiveAttack(actor, target);
            var defense = GetEffectiveDefense(target);
            var baseDamage = Mathf.Max(1, skill.baseValue + attack - defense + randomOffset);
            var amount = Mathf.Max(1, Mathf.RoundToInt(baseDamage * skill.powerMultiplier));

            amount = ApplyPassiveToDamage(actor, target, amount);
            amount = ApplyArchetypeToDamage(actor, skill, target, amount);
            amount = ApplySkillSpecificDamageModifier(actor, skill, target, amount);

            var critical = Random.value < 0.1f;
            if (critical)
            {
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * 1.5f));
            }

            return new DamageResult(amount, critical);
        }

        private int GetEffectiveAttack(BattleUnit unit, BattleUnit target)
        {
            var attack = unit.Attack;
            if (unit.IsHero && unit.Definition.passive == HeroPassive.Berserker)
            {
                var hpPercent = (float)unit.Health / unit.MaxHealth;
                if (hpPercent < 0.5f)
                {
                    attack = Mathf.RoundToInt(attack * 1.3f);
                }
            }

            if (unit.IsHero && unit.Definition.passive == HeroPassive.GlassCannon)
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

        private int ApplySkillSpecificDamageModifier(BattleUnit actor, SkillData skill, BattleUnit target, int amount)
        {
            if (actor == null || skill == null || target == null || amount <= 0)
            {
                return amount;
            }

            switch (skill.skillId)
            {
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
                case "hero_05_gear_sting":
                    if (target.IsBackline)
                    {
                        amount = Mathf.RoundToInt(amount * 1.3f);
                    }
                    break;
                case "hero_08_scrap_volley":
                    if (target.IsBackline)
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
                case "hero_06_field_stitch":
                    var cooledSkill = target.ReduceRandomCooldown(1);
                    if (cooledSkill != null)
                    {
                        ui.AddLog($"{target.DisplayName} \u7684 {cooledSkill.skillName} \u51b7\u5374\u7f29\u77ed\u4e86 1 \u56de\u5408\u3002");
                        if (views.TryGetValue(target, out var fieldStitchTargetView))
                        {
                            fieldStitchTargetView.ShowFloatingText("\u51b7\u5374-1", new Color(0.76f, 0.9f, 1f), 0.85f);
                        }
                    }
                    break;
                case "hero_06_steam_purge":
                    var removed = target.ClearNegativeStatuses();
                    if (removed > 0)
                    {
                        ui.AddLog($"{target.DisplayName} \u7684\u8d1f\u9762\u72b6\u6001\u88ab\u84b8\u6c7d\u51c0\u5316\u6e05\u9664\u4e86\u3002");
                        if (views.TryGetValue(target, out var purgeTargetView))
                        {
                            purgeTargetView.ShowFloatingText("\u51c0\u5316", new Color(0.58f, 0.94f, 0.95f), 0.9f);
                        }
                    }
                    break;
                case "hero_06_stun_chain":
                    if (target.IsBackline)
                    {
                        var gained = actor.GainResource(1);
                        if (gained > 0)
                        {
                            ui.AddLog($"{actor.DisplayName} \u7528\u9707\u8361\u9501\u94fe\u538b\u5236\u540e\u6392\uff0c\u989d\u5916\u83b7\u5f97\u4e86 {gained} \u70b9\u8d44\u6e90\u3002");
                        }
                    }
                    break;
            }
        }

        private void ApplySkillSpecificActionReward(BattleUnit actor, SkillData skill, BattleUnit[] targets)
        {
            if (actor == null || skill == null || targets == null || targets.Length == 0 || !actor.IsAlive)
            {
                return;
            }

            var livingTargets = targets.Count(target => target != null && target.IsAlive);
            if (skill.skillId == "hero_03_scrap_volley" && livingTargets >= 3)
            {
                var gained = actor.GainResource(1);
                if (gained > 0)
                {
                    ui.AddLog($"{actor.DisplayName} \u7684\u9f50\u5c04\u547d\u4e2d\u591a\u540d\u76ee\u6807\uff0c\u56de\u6536\u4e86 {gained} \u70b9\u8d44\u6e90\u3002");
                }
            }
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
            ui.AddLog($"{actor.DisplayName} \u627f\u53d7\u4e86\u9f50\u5c04\u53cd\u9707\u7684 {recoilDamage} \u70b9\u4f24\u5bb3\u3002");
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
            if (!unit.IsHero || unit.Definition.passive == HeroPassive.None)
            {
                return;
            }

            switch (unit.Definition.passive)
            {
                case HeroPassive.Regenerator:
                    var regenAmount = Mathf.Max(1, Mathf.RoundToInt(unit.MaxHealth * 0.05f));
                    unit.Heal(regenAmount);
                    ui.AddLog($"{unit.DisplayName} 的再生恢复了 {regenAmount} 点生命。");
                    break;

                case HeroPassive.Tactician:
                    var allies = GetLivingAllies(unit).Where(candidate => candidate.HasCooldowns).ToArray();
                    if (allies.Length > 0)
                    {
                        var ally = allies[Random.Range(0, allies.Length)];
                        var reducedSkill = ally.ReduceRandomCooldown(2);
                        if (reducedSkill != null)
                        {
                            ui.AddLog($"{unit.DisplayName} 的战术指挥缩短了 {ally.DisplayName} 的 {reducedSkill.skillName} 冷却。");
                        }
                    }
                    break;

                case HeroPassive.Vanguard:
                    if (unit.IsFrontline)
                    {
                        ui.AddLog($"{unit.DisplayName} 的先锋光环为全队提供了攻击加成。");
                    }
                    break;

                case HeroPassive.Inspirer:
                    foreach (var ally in GetLivingAllies(unit, includeSelf: true))
                    {
                        var healAmount = Mathf.Max(1, Mathf.RoundToInt(ally.MaxHealth * 0.1f));
                        ally.Heal(healAmount);
                    }
                    ui.AddLog($"{unit.DisplayName} 的鼓舞恢复了全队生命。");
                    break;
            }
        }

        private void ApplyPassiveOnKill(BattleUnit killer, BattleUnit victim)
        {
            if (!killer.IsHero || killer.Definition.passive == HeroPassive.None)
            {
                return;
            }

            switch (killer.Definition.passive)
            {
                case HeroPassive.Scavenger:
                    var healAmount = Mathf.Max(1, Mathf.RoundToInt(killer.MaxHealth * 0.2f));
                    killer.Heal(healAmount);
                    ui.AddLog($"{killer.DisplayName} 的回收者被动恢复了 {healAmount} 点生命。");
                    break;

                case HeroPassive.ChainReaction:
                    var aliveEnemies = GetLivingOpponents(killer).Where(candidate => candidate != victim).ToArray();
                    if (aliveEnemies.Length > 0)
                    {
                        var target = aliveEnemies[Random.Range(0, aliveEnemies.Length)];
                        var splashDamage = Mathf.Max(1, Mathf.RoundToInt(victim.MaxHealth * 0.25f));
                        target.TakeDamage(splashDamage);
                        ui.AddLog($"{killer.DisplayName} 的连锁反应对 {target.DisplayName} 造成 {splashDamage} 点溅射伤害。");
                        if (!target.IsAlive && views.TryGetValue(target, out var targetView))
                        {
                            HandleDefeatedUnit(target, null);
                        }
                }
                    break;
            }
        }

        private void ApplyRawDamage(BattleUnit target, int amount, BattleUnit attacker)
        {
            if (target == null || amount <= 0)
            {
                return;
            }

            target.TakeDamage(amount);

            if (target.IsHero && target.Definition.passive == HeroPassive.ThornArmor && attacker != null && attacker.IsAlive)
            {
                var reflectDamage = Mathf.Max(1, Mathf.RoundToInt(amount * 0.2f));
                attacker.TakeDamage(reflectDamage);
                ui.AddLog($"{target.DisplayName} 的荆棘护甲反弹了 {reflectDamage} 点伤害给 {attacker.DisplayName}。");

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
                candidate.HasPassive(HeroPassive.Bodyguard) &&
                Mathf.Abs(candidate.CurrentPosition - protectedUnit.CurrentPosition) == 1);
        }

        private bool ApplyPassiveBeforeDeath(BattleUnit unit, int incomingDamage)
        {
            if (!unit.IsHero || unit.Definition.passive != HeroPassive.IronWill)
            {
                return false;
            }

            if (unit.Health - incomingDamage <= 0)
            {
                if (!ironWillUsedThisBattle.Contains(unit))
                {
                    ironWillUsedThisBattle.Add(unit);
                    unit.TakeDamage(Mathf.Max(0, unit.Health - 1));
                    ui.AddLog($"{unit.DisplayName} 的铁意志触发了！保留 1 点生命。");
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

            amount = ApplySkillSpecificHealingModifier(actor, skill, target, amount);
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
            if (actor == null || !actor.CanAct)
            {
                return;
            }

            var gained = 0;
            switch (actor.Archetype)
            {
                case CombatArchetype.Bulwark:
                    gained = actor.IsFrontline ? actor.GainResource(1) : 0;
                    break;
                case CombatArchetype.Executioner:
                    gained = GetLivingOpponents(actor).Any(target => target.HealthRatio <= 0.6f) ? actor.GainResource(1) : 0;
                    break;
                case CombatArchetype.Artificer:
                    gained = actor.IsBackline ? actor.GainResource(1) : 0;
                    break;
                case CombatArchetype.Physician:
                    gained = GetLivingAllies(actor, includeSelf: true).Any(target => target.HealthRatio < 0.85f) ? actor.GainResource(1) : 0;
                    break;
            }

            if (gained > 0)
            {
                ui.AddLog($"{actor.DisplayName} 的{actor.Definition.ArchetypeDisplayName}节奏恢复了 {gained} 点资源。");
            }
        }

        private void ApplyArchetypeActionResource(BattleUnit actor, SkillData skill, BattleUnit[] targets)
        {
            if (actor == null || skill == null || targets == null || targets.Length == 0)
            {
                return;
            }

            var gained = 0;
            switch (actor.Archetype)
            {
                case CombatArchetype.Bulwark:
                    if (targets.Any(target => target.IsFrontline))
                    {
                        gained = actor.GainResource(1);
                    }
                    break;
                case CombatArchetype.Executioner:
                    if (targets.Any(target => !target.IsAlive || target.HealthRatio <= 0.5f))
                    {
                        gained = actor.GainResource(1);
                    }
                    break;
                case CombatArchetype.Artificer:
                    if (skill.skillType == SkillDataType.控制 || targets.Length >= 2 || targets.Any(target => target.IsBackline))
                    {
                        gained = actor.GainResource(1);
                    }
                    break;
                case CombatArchetype.Physician:
                    if (skill.skillType == SkillDataType.治疗)
                    {
                        gained = actor.GainResource(targets.Count(target => target.HealthRatio < 1f) >= 2 ? 2 : 1);
                    }
                    break;
            }

            if (gained > 0)
            {
                ui.AddLog($"{actor.DisplayName} 积累了 {gained} 点资源。");
            }
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
            var targetPosition = new Vector3(focusPosition.x, Mathf.Clamp(focusPosition.y + 0.4f, -0.4f, 1.2f), startPosition.z);
            var targetSize = Mathf.Max(2.65f, defaultCameraSize * 0.66f);
            const float duration = 0.3f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Smooth01(elapsed / duration);
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

        private IEnumerator EndAttackFocus(CombatantView actorView, CombatantView targetView)
        {
            foreach (var view in views.Values)
            {
                view?.SetFocusScale(1f);
                view?.SetFocusLayer(false, 0);
            }

            ClearFocusBlur();
            yield return StartCoroutine(RestoreCamera());
        }

        private AttackLungeState CreateAttackLungeState(CombatantView actorView, IReadOnlyList<CombatantView> targetViews)
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

            if (orderedTargets.Length == 1)
            {
                var targetStart = targetStarts[0];
                var delta = targetStart - actorStart;
                var startDistance = delta.magnitude;
                if (startDistance > 0.001f)
                {
                    var desiredDistance = Mathf.Max(attackFocusSingleMinDistance, startDistance * (1f - Mathf.Clamp01(attackFocusSingleLungeRatio)));
                    var moveDistancePerSide = Mathf.Max(0f, (startDistance - desiredDistance) * 0.5f);
                    var direction = delta / startDistance;
                    actorEnd = actorStart + direction * moveDistancePerSide;
                    targetEnds[0] = targetStart - direction * moveDistancePerSide;
                }
                else
                {
                    targetEnds[0] = targetStart;
                }
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

            return new AttackLungeState(actorView, actorStart, actorEnd, orderedTargets, targetStarts, targetEnds);
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
            var cameraTargetPosition = new Vector3(center.x, Mathf.Clamp(center.y + 0.35f, -0.55f, 1.25f), startPosition.z);
            var targetSize = Mathf.Max(2.35f, defaultCameraSize * attackFocusZoomRatio);
            const float duration = 0.24f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Smooth01(elapsed / duration);
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
                    Random.Range(-strength, strength),
                    Random.Range(-strength, strength),
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
            CreateBackdrop(ResolveBattleBackground());

            ui = battleUIPrefab != null
                ? Instantiate(battleUIPrefab)
                : new GameObject("Battle UI", typeof(RectTransform)).AddComponent<BattleUI>();

            ui.name = "Battle UI";
            ui.Build();
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
#endif
        }

        private void SetupHeroUnits()
        {
            var heroDefinitions = heroParty != null && heroParty.Length > 0 ? heroParty : DemoBattleBootstrap.CreateDefaultHeroes();
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

            ClearEnemyUnits();

            var enemyDefinitions = bossBattle
                ? DemoBattleBootstrap.CreateBossEnemies()
                : SelectRandomEnemyEncounter();
            SpawnTeam(enemyDefinitions, enemies, enemySlots, false);
            LayoutFormation(true);
            LayoutFormation(false);
            RefreshViews();
            ui.SetRound(1);
            ui.SetTurn(bossBattle ? "\u6700\u7ec8 Boss \u6218" : $"\u7b2c {battleNumber} \u573a\u6218\u6597");
        }

        private CombatantDefinition[] SelectRandomEnemyEncounter()
        {
            var pool = enemyParty != null && enemyParty.Length > 0
                ? enemyParty.Where(enemy => enemy != null).ToArray()
                : DemoBattleBootstrap.CreateDefaultEnemies();

            if (pool.Length == 0)
            {
                return DemoBattleBootstrap.CreateDefaultEnemies();
            }

            var count = Mathf.Clamp(Random.Range(2, 4), 1, Mathf.Min(MaxFormationSlots, pool.Length));
            return pool
                .OrderBy(_ => Random.value)
                .Take(count)
                .ToArray();
        }

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
                view.transform.position = new Vector3(GetBaseSlotX(isHero, unit.CurrentPosition), -0.35f, 0f);
                view.Initialize(unit, fallbackSprite, heroVisualScale, HandleUnitClicked, nameplatePrefab);
                view.AlignFeetTo(FormationFeetY);
                views[unit] = view;
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
#if UNITY_EDITOR
                Debug.Log($"Spawned {unit.DisplayName} using unit prefab: {prefabSource}", view);
#endif
                return view;
            }

            var unitObject = new GameObject(unit.DisplayName);
            return unitObject.AddComponent<CombatantView>();
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
                view.transform.position = new Vector3(GetFormationSlotX(isHero, slots, unit.CurrentPosition), -0.35f, 0f);
                view.AlignFeetTo(FormationFeetY);
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

        private static void CreateBackdrop(Sprite backgroundSprite)
        {
            if (backgroundSprite != null)
            {
                var background = new GameObject("Battle Background");
                var renderer = background.AddComponent<SpriteRenderer>();
                renderer.sprite = backgroundSprite;
                renderer.sortingOrder = -20;
                renderer.color = Color.white;
                background.transform.position = new Vector3(0f, 0f, 4f);

                var spriteSize = backgroundSprite.bounds.size;
                if (spriteSize.x > 0f && spriteSize.y > 0f)
                {
                    var camera = Camera.main;
                    var targetHeight = camera != null && camera.orthographic
                        ? camera.orthographicSize * 2f
                        : 8.8f;
                    var targetWidth = camera != null
                        ? targetHeight * camera.aspect
                        : 15.7f;
                    var scale = Mathf.Max(targetWidth / spriteSize.x, targetHeight / spriteSize.y);
                    background.transform.localScale = new Vector3(scale, scale, 1f);
                }
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

