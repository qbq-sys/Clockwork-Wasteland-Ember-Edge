using System.IO;
using ClockworkWasteland.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClockworkWasteland.EditorTools
{
    public static class DemoContentBuilder
    {
        private const string Root = "Assets/ClockworkWastelandDemo";
        private const string ItemsPath = "Assets/Resources/Items";
        private const string SkillsPath = Root + "/Data/Skills";
        private const string BuffsPath = Root + "/Data/Buffs";
        private const string GrowthsPath = Root + "/Data/Growth";
        private const string CombatantsPath = Root + "/Data/Combatants";
        private const string PrefabsPath = Root + "/Prefabs";
        private const string BattleUIPrefabPath = PrefabsPath + "/BattleUI.prefab";
        private const string ScenePath = "Assets/Scenes/CombatDemo.unity";

        [MenuItem("Clockwork Wasteland/Create Battle UI Prefab")]
        public static BattleUI CreateBattleUIPrefab()
        {
            EnsureFolder("Assets", "ClockworkWastelandDemo");
            EnsureFolder(Root, "Prefabs");

            var prefabRoot = new GameObject("BattleUI", typeof(RectTransform));
            var ui = prefabRoot.AddComponent<BattleUI>();
            ui.BuildDefaultLayout();

            var prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, BattleUIPrefabPath);
            Object.DestroyImmediate(prefabRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<BattleUI>(BattleUIPrefabPath);
        }

        [MenuItem("Clockwork Wasteland/Create Combat Demo Assets")]
        public static void CreateCombatDemoAssets()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "Items");
            EnsureFolder("Assets", "ClockworkWastelandDemo");
            EnsureFolder(Root, "Data");
            EnsureFolder(Root + "/Data", "Skills");
            EnsureFolder(Root + "/Data", "Buffs");
            EnsureFolder(Root + "/Data", "Growth");
            EnsureFolder(Root + "/Data", "Combatants");
            EnsureFolder(Root, "Prefabs");

            var burn = CreateBuff("burn", "\u707c\u70e7", "\u6bcf\u56de\u5408\u53d7\u5230\u6301\u7eed\u4f24\u5bb3\u3002", 3, false, 2);
            CreateItem("small_potion", "\u5c0f\u836f\u6c34", "\u6218\u6597\u5916\u4f7f\u7528\uff0c\u4e3a\u4e00\u540d\u82f1\u96c4\u6062\u590d 20 \u751f\u547d\u3002", 100, InventoryItemEffectType.Heal, 20, 0.3f);
            CreateItem("large_potion", "\u5927\u836f\u6c34", "\u6218\u6597\u5916\u4f7f\u7528\uff0c\u4e3a\u4e00\u540d\u82f1\u96c4\u6062\u590d 50 \u751f\u547d\u3002", 250, InventoryItemEffectType.Heal, 50, 0.3f);
            CreateItem("revive_potion", "\u590d\u6d3b\u836f\u6c34", "\u6218\u6597\u5916\u4f7f\u7528\uff0c\u590d\u6d3b\u4e00\u540d\u6b7b\u4ea1\u82f1\u96c4\uff0c\u751f\u547d\u6062\u590d\u5230 30%\u3002", 500, InventoryItemEffectType.Revive, 0, 0.3f);
            var ironCut = CreateSkill("iron_cut", "\u94c1\u5203\u65a9", "\u7a33\u5b9a\u7684\u5355\u4f53\u4f24\u5bb3\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 8, 1f, null, new[] { 1, 2 }, new[] { 1, 2 });
            var emberNick = CreateSkill("ember_rend", "\u4f59\u70ec\u5272\u88c2", "\u9020\u6210\u4f24\u5bb3\u5e76\u7559\u4e0b\u707c\u70e7\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 5, 1f, burn, new[] { 1, 2, 3 }, new[] { 1, 2, 3 });
            var fieldStitch = CreateSkill("field_stitch", "\u6218\u5730\u7f1d\u5408", "\u6cbb\u7597\u4e00\u540d\u53cb\u65b9\u3002", SkillDataType.治疗, SkillDataTargetType.单友, 7, 1f, null, new[] { 2, 3, 4 }, new[] { 1, 2, 3, 4 });
            var scrapVolley = CreateSkill("scrap_volley", "\u5e9f\u94c1\u9f50\u5c04", "\u653b\u51fb\u654c\u65b9\u5168\u4f53\u3002", SkillDataType.伤害, SkillDataTargetType.全体敌, 4, 0.75f);
            var guardBreak = CreateSkill("guard_break", "\u7834\u9632\u4e00\u51fb", "\u5bf9\u5355\u4e2a\u654c\u4eba\u9020\u6210\u8f83\u9ad8\u4f24\u5bb3\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 10, 1.15f, null, new[] { 1, 2 }, new[] { 1 });
            var claw = CreateSkill("claw", "\u722a\u51fb", "\u7c97\u66b4\u7684\u8fd1\u8eab\u653b\u51fb\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 6, 1f, null, new[] { 1, 2 }, new[] { 1, 2 });
            var cinderBite = CreateSkill("cinder_bite", "\u7070\u70ec\u6495\u54ac", "\u5e26\u6765\u6301\u7eed\u707c\u70e7\u7684\u6495\u54ac\u3002", SkillDataType.伤害, SkillDataTargetType.单敌, 4, 1f, burn, new[] { 1, 2, 3 }, new[] { 1, 2, 3 });

            var heroes = new[]
            {
                CreateCombatant("hero_01", "\u82f1\u96c4\u4e00", true, 38, 4, Color.white, "Assets/Art/hero_01_idle.png", ironCut, emberNick),
                CreateCombatant("hero_02", "\u82f1\u96c4\u4e8c", true, 28, 6, Color.white, "Assets/Art/hero_02_idle.png", ironCut, fieldStitch),
                CreateCombatant("hero_03", "\u82f1\u96c4\u4e09", true, 30, 8, Color.white, "Assets/Art/hero_03_idle.png", ironCut, scrapVolley),
                CreateCombatant("hero_04", "\u82f1\u96c4\u56db", true, 34, 5, Color.white, "Assets/Art/hero_04_idle.png", ironCut, guardBreak)
            };

            var enemies = new[]
            {
                CreateCombatant("enemy_01", "\u9508\u8680\u6b8b\u5175", false, 24, 5, Color.white, "Assets/Art/hero_01_idle.png", claw),
                CreateCombatant("enemy_02", "\u7070\u70ec\u4fe1\u5f92", false, 22, 7, Color.white, "Assets/Art/hero_02_idle.png", cinderBite),
                CreateCombatant("enemy_03", "\u9aa8\u8f6e\u866b", false, 18, 9, Color.white, "Assets/Art/hero_03_idle.png", claw)
            };

            var uiPrefab = AssetDatabase.LoadAssetAtPath<BattleUI>(BattleUIPrefabPath);
            if (uiPrefab == null)
            {
                uiPrefab = CreateBattleUIPrefab();
            }

            CreateScene(heroes, enemies, uiPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Combat Demo Created", "Created demo data, BattleUI prefab, and Assets/Scenes/CombatDemo.unity.", "OK");
        }

        private static SkillData CreateSkill(string skillId, string displayName, string description, SkillDataType skillType, SkillDataTargetType targetType, int baseValue, float powerMultiplier, BuffData applyBuff = null, int[] casterPositions = null, int[] targetPositions = null)
        {
            var assetPath = $"{SkillsPath}/{ToAssetName(displayName)}.asset";
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>(assetPath);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillData>();
                AssetDatabase.CreateAsset(skill, assetPath);
            }

            skill.skillId = skillId;
            skill.skillName = displayName;
            skill.description = description;
            skill.skillType = skillType;
            skill.targetType = targetType;
            skill.baseValue = baseValue;
            skill.powerMultiplier = powerMultiplier;
            skill.casterAllowedPositions = casterPositions ?? new[] { 1, 2, 3, 4 };
            skill.targetAllowedPositions = targetPositions ?? new[] { 1, 2, 3, 4 };
            skill.applyBuff = applyBuff;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static BuffData CreateBuff(string buffId, string displayName, string description, int duration, bool stun, int tickDamage)
        {
            var assetPath = $"{BuffsPath}/{ToAssetName(displayName)}.asset";
            var buff = AssetDatabase.LoadAssetAtPath<BuffData>(assetPath);
            if (buff == null)
            {
                buff = ScriptableObject.CreateInstance<BuffData>();
                AssetDatabase.CreateAsset(buff, assetPath);
            }

            buff.buffId = buffId;
            buff.buffName = displayName;
            buff.description = description;
            buff.duration = duration;
            buff.stun = stun;
            buff.tickDamage = tickDamage;
            EditorUtility.SetDirty(buff);
            return buff;
        }

        private static InventoryItemData CreateItem(string itemId, string displayName, string description, int price, InventoryItemEffectType effectType, int healAmount, float reviveHealthPercent)
        {
            var assetPath = $"{ItemsPath}/{ToAssetName(displayName)}.asset";
            var item = AssetDatabase.LoadAssetAtPath<InventoryItemData>(assetPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<InventoryItemData>();
                AssetDatabase.CreateAsset(item, assetPath);
            }

            item.itemId = itemId;
            item.itemName = displayName;
            item.description = description;
            item.price = price;
            item.effectType = effectType;
            item.healAmount = healAmount;
            item.reviveHealthPercent = reviveHealthPercent;
            EditorUtility.SetDirty(item);
            return item;
        }

        private static CombatantDefinition CreateCombatant(string characterId, string displayName, bool isHero, int maxHealth, int speed, Color tint, string idleSpriteSheetPath, params SkillData[] skills)
        {
            var assetPath = $"{CombatantsPath}/{ToAssetName(displayName)}.asset";
            var combatant = AssetDatabase.LoadAssetAtPath<CombatantDefinition>(assetPath);
            if (combatant == null)
            {
                combatant = ScriptableObject.CreateInstance<CombatantDefinition>();
                AssetDatabase.CreateAsset(combatant, assetPath);
            }

            combatant.characterId = characterId;
            combatant.displayName = displayName;
            combatant.isHero = isHero;
            combatant.maxHealth = maxHealth;
            combatant.speed = speed;
            combatant.tint = tint;
            combatant.visualScale = 1f;
            combatant.corpseHealth = 3;
            combatant.growthData = isHero ? CreateDefaultGrowthData() : null;
            combatant.currentLevel = Mathf.Max(1, combatant.currentLevel);
            combatant.currentExperience = Mathf.Max(0, combatant.currentExperience);
            combatant.idleAnimationFrames = string.IsNullOrWhiteSpace(idleSpriteSheetPath) ? new Sprite[0] : EditorSpriteSheetLoader.LoadSprites(idleSpriteSheetPath);
            combatant.skills = skills;
            EditorUtility.SetDirty(combatant);
            return combatant;
        }

        private static HeroGrowthData CreateDefaultGrowthData()
        {
            const string assetPath = GrowthsPath + "/DefaultHeroGrowth.asset";
            var growth = AssetDatabase.LoadAssetAtPath<HeroGrowthData>(assetPath);
            if (growth == null)
            {
                growth = ScriptableObject.CreateInstance<HeroGrowthData>();
                AssetDatabase.CreateAsset(growth, assetPath);
            }

            growth.experiencePerLevel = 100;
            growth.maxHealthPerLevel = 5;
            growth.attackPerLevel = 2;
            growth.defensePerLevel = 1;
            EditorUtility.SetDirty(growth);
            return growth;
        }

        private static void CreateScene(CombatantDefinition[] heroes, CombatantDefinition[] enemies, BattleUI uiPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 4.4f;
            camera.backgroundColor = new Color(0.06f, 0.065f, 0.07f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var controllerObject = new GameObject("Battle Controller");
            var controller = controllerObject.AddComponent<BattleController>();
            controller.Configure(heroes, enemies, uiPrefab);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static string ToAssetName(string displayName)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                displayName = displayName.Replace(invalidChar, '_');
            }

            return displayName.Replace(' ', '_');
        }
    }
}
