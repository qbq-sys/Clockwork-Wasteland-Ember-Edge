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

    public sealed class BattleController : MonoBehaviour
    {
        private const int MaxFormationSlots = 4;
        private const int MapNodeCount = 3;
        private const float FormationFeetY = -1.8f;
        private const float MinCombatOverlayDuration = 0.56f;
        private const float BulletTimeDuration = 0.62f;
        private const float BulletTimeMinScale = 0.08f;
        private static readonly int[] AnyPosition = { 1, 2, 3, 4 };

        [SerializeField] private CombatantDefinition[] heroParty;
        [SerializeField] private CombatantDefinition[] enemyParty;

        [Header("Presentation")]
        [SerializeField] private BattleUI battleUIPrefab;
        [SerializeField] private CombatantView defaultUnitPrefab;
        [SerializeField] private CombatNameplate nameplatePrefab;
        [SerializeField] private float heroVisualScale = 0.8f;
        [SerializeField] private Sprite[] battleBackgrounds;
        [SerializeField] private int battleBackgroundIndex = 0;

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
            CombatAudio.Ensure();
            SetupScene();
            CacheDefaultCamera();
            LoadDefaultPresentationPrefabs();
            shopItems = LoadShopItems();
            ShowTitleScreen();
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
            CombatAudio.Instance.StopMusic();
            ClearAllUnits();
            ui.ClearActionPanels();
            ui.SetRound(0);
            ui.SetTurn("\u6807\u9898\u754c\u9762");
            ui.SetGold(0);
            ui.ShowTitleScreen(StartNewGame, ShowSettings, QuitGame);
        }

        private void StartNewGame()
        {
            ResetGameState();
            ShowTeamSelection();
        }

        private void ResetGameState()
        {
            StopAllCoroutines();
            ClearAllUnits();
            gold = 0;
            inventory.Clear();
            totalHeroPool = DemoBattleBootstrap.CreateHeroPool();
            availableHeroPool = GetUnlockedHeroPool();
            selectedHeroDefinitions.Clear();
            selectedHeroDefinitions.AddRange(availableHeroPool.Take(MaxFormationSlots));
            currentBattleNumber = 0;
            heroParty = selectedHeroDefinitions.ToArray();
            ui.SetGold(gold);
        }

        private void ShowSettings()
        {
            ui.ShowSettingsScreen(ShowTitleScreen);
        }

        private void QuitGame()
        {
            ui.AddLog("\u9000\u51fa\u6e38\u620f\u3002");
            Application.Quit();
        }

        private void ShowTeamSelection()
        {
            CombatAudio.Instance.StopMusic();
            ClearAllUnits();
            ui.ClearActionPanels();
            ui.SetRound(0);
            ui.SetTurn("\u961f\u4f0d\u914d\u7f6e");
            ui.SetGold(gold);
            ui.ShowTeamSelection(availableHeroPool, selectedHeroDefinitions, ToggleHeroSelection, StartSelectedBattleSequence, ShowShop, ShowInventory, ShowTavern);
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

            ui.ShowTeamSelection(availableHeroPool, selectedHeroDefinitions, ToggleHeroSelection, StartSelectedBattleSequence, ShowShop, ShowInventory, ShowTavern);
        }

        private void StartSelectedBattleSequence()
        {
            if (selectedHeroDefinitions.Count == 0)
            {
                ui.AddLog("\u8bf7\u81f3\u5c11\u9009\u62e9 1 \u540d\u82f1\u96c4\u3002");
                return;
            }

            var deadHero = selectedHeroDefinitions.FirstOrDefault(hero => hero != null && hero.IsDead);
            if (deadHero != null)
            {
                ui.AddLog($"{deadHero.displayName} \u5df2\u6b7b\u4ea1\uff0c\u9700\u8981\u5148\u590d\u6d3b\u624d\u80fd\u51fa\u6218\u3002");
                return;
            }

            ClearAllUnits();
            ui.HideOverlay();
            CombatAudio.Instance.PlayStartExpedition();
            heroParty = selectedHeroDefinitions.Take(MaxFormationSlots).ToArray();
            SetupHeroUnits();
            StartCoroutine(MapExpeditionLoop());
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
            ui.ShowTavern(GetTavernOffers(), gold, RecruitHero, ShowTeamSelection);
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
            ui.AddLog($"\u62db\u52df\u4e86 {hero.displayName}\uff0c\u4ed6\u5df2\u52a0\u5165\u82f1\u96c4\u6c60\u3002");
            ShowTavern();
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
            ui.ShowContinuePrompt("\u606d\u559c\u901a\u5173", "\u8fd4\u56de\u6807\u9898", () => requestedReturn = true);
            while (!requestedReturn)
            {
                yield return null;
            }

            ShowTitleScreen();
        }

        private IEnumerator RunCombatEncounter(bool bossBattle)
        {
            if (bossBattle)
            {
                CombatAudio.Instance.PlayBossMusic();
            }

            PrepareBattle(currentBattleNumber, bossBattle);
            yield return StartCoroutine(BattleLoop());

            if (bossBattle)
            {
                CombatAudio.Instance.StopMusic();
            }

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
            ui.ShowContinuePrompt("\u6218\u8d25", "\u8fd4\u56de\u6807\u9898", () => returnRequested = true);
            while (!returnRequested)
            {
                yield return null;
            }

            ShowTitleScreen();
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
                var experienceGained = Random.Range(10, 21);
                var maxHealthBefore = hero.MaxHealth;
                var levelsGained = hero.Definition.AddExperience(experienceGained);
                var maxHealthGain = Mathf.Max(0, hero.MaxHealth - maxHealthBefore);
                hero.RestoreForMaxHealthGain(maxHealthGain);

                results.Add(new BattleRewardResult(hero.Definition, experienceGained, levelsGained));
                ui.AddLog($"{hero.Definition.displayName} \u83b7\u5f97 {experienceGained} \u7ecf\u9a8c\u3002");

                if (levelsGained > 0)
                {
                    var firstNewLevel = hero.Definition.Level - levelsGained + 1;
                    for (var level = firstNewLevel; level <= hero.Definition.Level; level++)
                    {
                        ui.AddLog($"{hero.Definition.displayName} \u5347\u5230\u4e86 {level} \u7ea7\u3002");
                    }
                }
            }

            RefreshViews();
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
                var skill = GetSkillList(actor).FirstOrDefault(item => item != null && GetSkillUseState(actor, item).CanUse);
                var target = skill != null ? GetValidTargets(actor, skill).OrderBy(unit => unit.Health).FirstOrDefault() : null;
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
            SetTargetHighlights(new[] { mainTarget });

            yield return StartCoroutine(FocusCamera(actorView.transform.position));

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
                        target.TakeDamage(damage.Amount);
                        CombatAudio.Instance.PlayImpact();
                        ui.AddLog(damage.IsCritical
                            ? $"{actor.DisplayName} \u4f7f\u7528 {skill.skillName}\u66b4\u51fb\uff01{target.DisplayName} \u53d7\u5230 {damage.Amount} \u70b9\u4f24\u5bb3\u3002"
                            : $"{actor.DisplayName} \u4f7f\u7528 {skill.skillName}\uff0c{target.DisplayName} \u53d7\u5230 {damage.Amount} \u70b9\u4f24\u5bb3\u3002");

                        if (views.TryGetValue(target, out var targetView))
                        {
                            targetView.ShowFloatingText(damage.IsCritical ? $"-{damage.Amount}!" : $"-{damage.Amount}", Color.red, damage.IsCritical ? 1.55f : 1f);
                            if (!target.IsAlive)
                            {
                                yield return StartCoroutine(targetView.PlayDeathCue());
                                HandleDefeatedUnit(target, skill);
                            }
                        }
                    }
                }
                else
                {
                    var amount = CalculateHealingAmount(actor, skill);
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
            }

            RefreshViews();
            LayoutFormation(true);
            LayoutFormation(false);
            yield return StartCoroutine(RestoreCamera());
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
            yield return StartCoroutine(AnimateSwap(actor, target));

            RefreshViews();
            ui.AddLog($"{actor.DisplayName} \u548c {target.DisplayName} \u4ea4\u6362\u4e86\u7ad9\u4f4d\u3002");
        }

        private IEnumerator AnimateSwap(BattleUnit actor, BattleUnit target)
        {
            if (!views.TryGetValue(actor, out var actorView) || !views.TryGetValue(target, out var targetView))
            {
                LayoutFormation(actor.IsHero);
                yield break;
            }

            const float duration = 0.28f;
            var actorMove = actorView.StartCoroutine(actorView.MoveToFormation(GetSlotX(actor.IsHero, actor.CurrentPosition), FormationFeetY, duration));
            var targetMove = targetView.StartCoroutine(targetView.MoveToFormation(GetSlotX(target.IsHero, target.CurrentPosition), FormationFeetY, duration));
            yield return actorMove;
            yield return targetMove;
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
            var hit = targetView.StartCoroutine(targetView.PlayOverlay(hitSprite, duration, 0.1f, 81));
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
            var baseDamage = Mathf.Max(1, skill.baseValue + actor.Attack - target.Defense + randomOffset);
            var amount = Mathf.Max(1, Mathf.RoundToInt(baseDamage * skill.powerMultiplier));
            var critical = Random.value < 0.1f;
            if (critical)
            {
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * 1.5f));
            }

            return new DamageResult(amount, critical);
        }

        private int CalculateHealingAmount(BattleUnit actor, SkillData skill)
        {
            return Mathf.Max(1, Mathf.RoundToInt((skill.baseValue + actor.Attack * 0.5f) * skill.powerMultiplier));
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
            currentActor = null;
            selectedUnit = null;
            selectedSkill = null;
            waitingForPlayer = false;
            resolvingPlayerAction = false;
            validSelectedTargets = new BattleUnit[0];
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
            waitingForPlayer = false;
            resolvingPlayerAction = false;
            currentActor = null;
            selectedUnit = null;
            selectedSkill = null;
            validSelectedTargets = new BattleUnit[0];
            turnQueue.Clear();

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
                view.transform.position = new Vector3(GetSlotX(isHero, unit.CurrentPosition), -0.35f, 0f);
                view.Initialize(unit, fallbackSprite, heroVisualScale, HandleUnitClicked, nameplatePrefab);
                view.AlignFeetTo(FormationFeetY);
                views[unit] = view;
            }
        }

        private CombatantView CreateCombatantView(BattleUnit unit)
        {
            var prefab = unit.Definition.unitPrefab != null ? unit.Definition.unitPrefab : defaultUnitPrefab;
#if UNITY_EDITOR
            if (prefab == null && !string.IsNullOrWhiteSpace(unit.Definition.unitPrefabPath))
            {
                prefab = AssetDatabase.LoadAssetAtPath<CombatantView>(unit.Definition.unitPrefabPath);
            }
#endif

            if (prefab != null)
            {
                var view = Instantiate(prefab);
                view.name = unit.DisplayName;
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
                view.transform.position = new Vector3(GetSlotX(isHero, unit.CurrentPosition), -0.35f, 0f);
                view.AlignFeetTo(FormationFeetY);
            }
        }

        private static float GetSlotX(bool isHero, int position)
        {
            return isHero
                ? -0.8f - (position - 1) * 1.1f
                : 1.3f + (position - 1) * 1.1f;
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

