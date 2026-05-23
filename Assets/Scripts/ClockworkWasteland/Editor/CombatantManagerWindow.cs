using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClockworkWasteland.Combat;
using UnityEditor;
using UnityEngine;

namespace ClockworkWasteland.EditorTools
{
    public sealed class CombatantManagerWindow : EditorWindow
    {
        private const string Root = "Assets/ClockworkWastelandDemo";
        private const string CombatantsPath = Root + "/Data/Combatants";
        private const string SkillsPath = Root + "/Data/Skills";
        private const string GrowthsPath = Root + "/Data/Growth";
        private const string UnitPrefabsPath = Root + "/Prefabs/CombatUnits";
        private const string CharacterArtRoot = "Assets/Art/Characters";
        private const string CharacterSpecificVfxRoot = "Assets/Art/VFX/Combat/CharacterSpecific";

        private readonly List<Sprite> createIdleFrames = new List<Sprite>();
        private readonly List<SkillData> createSkills = new List<SkillData>();
        private readonly List<Sprite> editIdleFrames = new List<Sprite>();

        private CombatantDefinition[] combatants = Array.Empty<CombatantDefinition>();
        private SkillData[] skills = Array.Empty<SkillData>();
        private CombatantDefinition selectedCombatant;
        private SkillData selectedSkill;
        private UnityEditor.Editor selectedCombatantEditor;
        private UnityEditor.Editor selectedSkillEditor;
        private Vector2 listScroll;
        private Vector2 detailsScroll;
        private Vector2 createScroll;
        private Vector2 skillListScroll;
        private Vector2 skillDetailsScroll;
        private Vector2 passiveListScroll;
        private Vector2 passiveDetailsScroll;
        private int tabIndex;
        private string skillSearch = string.Empty;
        private string createSkillId = "new_skill";
        private string createSkillName = "新技能";
        private string createSkillDescription = "一个战斗动作。";
        private SkillDataType createSkillType = SkillDataType.伤害;
        private SkillDataTargetType createSkillTargetType = SkillDataTargetType.单敌;
        private int createSkillBaseValue = 8;
        private float createSkillPowerMultiplier = 1f;
        private int createSkillManaCost;
        private int createSkillCooldown;

        private string createCharacterId = "new_unit";
        private string createDisplayName = "新单位";
        private bool createIsHero = true;
        private bool createStartUnlocked = true;
        private int createRecruitPrice = 500;
        private int createMaxHealth = 32;
        private int createSpeed = 5;
        private int createAttack = 8;
        private int createDefense = 2;
        private int createCorpseHealth = 3;
        private int createOccupiedSlotCount = 1;
        private float createVisualScale = 1f;
        private Color createTint = Color.white;
        private CombatArchetype createArchetype = CombatArchetype.Undefined;
        private CombatRowPreference createPreferredRow = CombatRowPreference.Flexible;
        private CombatSpecialization createSpecialization = CombatSpecialization.None;
        private HeroGrowthData createGrowthData;
        private UnityEngine.Object createIdleSourceAsset;
        private Sprite createAttackSprite;
        private Sprite createHitSprite;

        private UnityEngine.Object editIdleSourceAsset;
        private Sprite editAttackSprite;
        private Sprite editHitSprite;

        [MenuItem("Clockwork Wasteland/Tools/Combat Content Manager")]
        public static void Open()
        {
            var window = GetWindow<CombatantManagerWindow>("战斗内容管理器");
            window.minSize = new Vector2(1180f, 700f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshCombatants();
            RefreshSkills();
            createGrowthData = LoadOrCreateDefaultGrowthData();
        }

        private void OnDisable()
        {
            DestroyCachedEditor();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(6f);

            switch (tabIndex)
            {
                case 0:
                    DrawExistingUnitsTab();
                    break;
                case 1:
                    DrawCreateUnitTab();
                    break;
                case 2:
                    DrawSkillsTab();
                    break;
                default:
                    DrawPassivesTab();
                    break;
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                tabIndex = GUILayout.Toolbar(tabIndex, new[] { "单位", "创建单位", "技能", "被动" }, EditorStyles.toolbarButton, GUILayout.Width(360f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                {
                    RefreshCombatants();
                    RefreshSkills();
                }
            }
        }

        private void DrawExistingUnitsTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawCombatantListPane();
                DrawCombatantDetailsPane();
            }
        }

        private void DrawCombatantListPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(320f)))
            {
                EditorGUILayout.LabelField("单位列表", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("选择一个已有单位，查看定义、定位预制体、重建战斗预制体，或替换立绘与攻击/受击资源。", MessageType.None);

                listScroll = EditorGUILayout.BeginScrollView(listScroll, "box");
                foreach (var combatant in combatants)
                {
                    if (combatant == null)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var selected = selectedCombatant == combatant;
                        if (GUILayout.Toggle(selected, $"{combatant.displayName}  [{combatant.characterId}]", "Button"))
                        {
                            SelectCombatant(combatant);
                        }

                        GUILayout.Label(combatant.isHero ? "英雄" : "敌人", GUILayout.Width(48f));
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawCombatantDetailsPane()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (selectedCombatant == null)
                {
                    EditorGUILayout.LabelField("详情", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("请先在左侧选择一个单位。", MessageType.Info);
                    return;
                }

                EditorGUILayout.LabelField(selectedCombatant.displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("角色 ID", selectedCombatant.characterId);
                EditorGUILayout.LabelField("预制体路径", GetUnitPrefabPath(selectedCombatant));
                EditorGUILayout.LabelField("特效资源目录", GetCharacterSpecificVfxFolder(selectedCombatant.characterId));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("定位定义", GUILayout.Width(120f)))
                    {
                        EditorGUIUtility.PingObject(selectedCombatant);
                        Selection.activeObject = selectedCombatant;
                    }

                    if (GUILayout.Button("定位预制体", GUILayout.Width(120f)))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<CombatantView>(GetUnitPrefabPath(selectedCombatant));
                        if (prefab != null)
                        {
                            EditorGUIUtility.PingObject(prefab);
                            Selection.activeObject = prefab;
                        }
                    }

                    if (GUILayout.Button("重建预制体", GUILayout.Width(120f)))
                    {
                        RunEditorAction("重建预制体", () => RebuildCombatantPrefab(selectedCombatant));
                    }
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("替换美术资源", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Idle 序列帧会更新头像、战斗立绘和预制体预览。攻击图/受击图会复制到角色专属特效目录，作为默认 Overlay。", MessageType.None);

                editIdleSourceAsset = EditorGUILayout.ObjectField("待机整图/图集", editIdleSourceAsset, typeof(UnityEngine.Object), false);
                DrawSpriteList("待机帧", editIdleFrames, allowCollectSelection: true);
                editAttackSprite = (Sprite)EditorGUILayout.ObjectField("默认攻击图", editAttackSprite, typeof(Sprite), false);
                editHitSprite = (Sprite)EditorGUILayout.ObjectField("默认受击图", editHitSprite, typeof(Sprite), false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("从待机整图导入切片", GUILayout.Width(220f)))
                    {
                        ReplaceSpriteListFromAsset(editIdleFrames, editIdleSourceAsset);
                    }

                    if (GUILayout.Button("从当前选择收集图片", GUILayout.Width(220f)))
                    {
                        ReplaceSpriteListFromSelection(editIdleFrames);
                    }

                    if (GUILayout.Button("应用到当前单位", GUILayout.Width(220f)))
                    {
                        RunEditorAction("应用单位美术", () => ApplyVisualsToExistingCombatant(selectedCombatant));
                    }
                }

                EditorGUILayout.Space(8f);
                detailsScroll = EditorGUILayout.BeginScrollView(detailsScroll);
                EnsureSelectedEditor();
                selectedCombatantEditor?.OnInspectorGUI();
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawCreateUnitTab()
        {
            createScroll = EditorGUILayout.BeginScrollView(createScroll);
            EditorGUILayout.LabelField("创建新单位", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("待机帧、默认攻击图、默认受击图可以从任意目录直接拖入。工具会自动复制到角色规范目录、重命名，并创建 CombatantDefinition、战斗预制体和角色专属特效资源。", MessageType.None);

            createDisplayName = EditorGUILayout.TextField("显示名称", createDisplayName);
            createCharacterId = EditorGUILayout.TextField("角色 ID", createCharacterId);
            createIsHero = EditorGUILayout.Toggle("是否英雄", createIsHero);
            createStartUnlocked = EditorGUILayout.Toggle("初始解锁", createStartUnlocked);
            createRecruitPrice = EditorGUILayout.IntField("招募价格", createRecruitPrice);
            createMaxHealth = EditorGUILayout.IntField("最大生命", createMaxHealth);
            createSpeed = EditorGUILayout.IntField("速度", createSpeed);
            createAttack = EditorGUILayout.IntField("攻击", createAttack);
            createDefense = EditorGUILayout.IntField("防御", createDefense);
            createCorpseHealth = EditorGUILayout.IntField("尸体生命", createCorpseHealth);
            createOccupiedSlotCount = EditorGUILayout.IntField("占位格数", createOccupiedSlotCount);
            createVisualScale = EditorGUILayout.FloatField("显示缩放", createVisualScale);
            createTint = EditorGUILayout.ColorField("染色", createTint);
            createArchetype = (CombatArchetype)EditorGUILayout.EnumPopup("职业原型", createArchetype);
            createPreferredRow = (CombatRowPreference)EditorGUILayout.EnumPopup("偏好站位", createPreferredRow);
            createSpecialization = (CombatSpecialization)EditorGUILayout.EnumPopup("专精分支", createSpecialization);
            createGrowthData = (HeroGrowthData)EditorGUILayout.ObjectField("成长数据", createGrowthData, typeof(HeroGrowthData), false);

            EditorGUILayout.Space(6f);
            DrawSkillList();
            EditorGUILayout.Space(6f);
            createIdleSourceAsset = EditorGUILayout.ObjectField("待机整图/图集", createIdleSourceAsset, typeof(UnityEngine.Object), false);
            DrawSpriteList("待机帧", createIdleFrames, allowCollectSelection: true);

            createAttackSprite = (Sprite)EditorGUILayout.ObjectField("默认攻击图", createAttackSprite, typeof(Sprite), false);
            createHitSprite = (Sprite)EditorGUILayout.ObjectField("默认受击图", createHitSprite, typeof(Sprite), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("从待机整图导入切片", GUILayout.Width(220f)))
                {
                    ReplaceSpriteListFromAsset(createIdleFrames, createIdleSourceAsset);
                }

                if (GUILayout.Button("从当前选择收集待机帧", GUILayout.Width(220f)))
                {
                    ReplaceSpriteListFromSelection(createIdleFrames);
                }

                if (GUILayout.Button("添加当前选中技能", GUILayout.Width(180f)))
                {
                    AddSelectedSkills(createSkills);
                }
            }

            EditorGUILayout.Space(10f);
            if (GUILayout.Button("创建 / 更新单位", GUILayout.Height(34f)))
            {
                RunEditorAction("创建 / 更新单位", CreateOrUpdateCombatant);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSkillsTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSkillListPane();
                DrawSkillDetailsPane();
            }
        }

        private void DrawPassivesTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawPassiveListPane();
                DrawPassiveDetailsPane();
            }
        }

        private void DrawSkillListPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(340f)))
            {
                EditorGUILayout.LabelField("技能列表", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("创建新技能、查看已有技能资产，并追踪当前哪些单位正在使用这些技能。", MessageType.None);
                skillSearch = EditorGUILayout.TextField("搜索", skillSearch);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("创建技能", GUILayout.Width(140f)))
                    {
                        CreateSkillAssetFromDraft();
                    }

                    if (GUILayout.Button("复制当前技能", GUILayout.Width(160f)))
                    {
                        DuplicateSelectedSkill();
                    }
                }

                skillListScroll = EditorGUILayout.BeginScrollView(skillListScroll, "box");
                foreach (var skill in skills.Where(DoesSkillMatchSearch))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var selected = selectedSkill == skill;
                        if (GUILayout.Toggle(selected, $"{skill.skillName}  [{skill.skillId}]", "Button"))
                        {
                            SelectSkill(skill);
                        }

                        GUILayout.Label(skill.skillType.ToString(), GUILayout.Width(52f));
                    }
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("新技能草稿", EditorStyles.boldLabel);
                createSkillId = EditorGUILayout.TextField("技能 ID", createSkillId);
                createSkillName = EditorGUILayout.TextField("显示名称", createSkillName);
                createSkillDescription = EditorGUILayout.TextField("描述", createSkillDescription);
                createSkillType = (SkillDataType)EditorGUILayout.EnumPopup("类型", createSkillType);
                createSkillTargetType = (SkillDataTargetType)EditorGUILayout.EnumPopup("目标类型", createSkillTargetType);
                createSkillBaseValue = EditorGUILayout.IntField("基础数值", createSkillBaseValue);
                createSkillPowerMultiplier = EditorGUILayout.FloatField("倍率", createSkillPowerMultiplier);
                createSkillManaCost = EditorGUILayout.IntField("消耗资源", createSkillManaCost);
                createSkillCooldown = EditorGUILayout.IntField("冷却回合", createSkillCooldown);
            }
        }

        private void DrawSkillDetailsPane()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (selectedSkill == null)
                {
                    EditorGUILayout.LabelField("技能详情", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("请先在左侧选择一个技能。", MessageType.Info);
                    return;
                }

                EditorGUILayout.LabelField(selectedSkill.skillName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("技能 ID", selectedSkill.skillId);
                EditorGUILayout.LabelField("资源路径", AssetDatabase.GetAssetPath(selectedSkill));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("定位技能", GUILayout.Width(120f)))
                    {
                        EditorGUIUtility.PingObject(selectedSkill);
                        Selection.activeObject = selectedSkill;
                    }

                    if (GUILayout.Button("挂到当前单位", GUILayout.Width(180f)))
                    {
                        AssignSelectedSkillToSelectedCombatant();
                    }
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("当前使用者", EditorStyles.boldLabel);
                foreach (var combatant in GetCombatantsUsingSkill(selectedSkill))
                {
                    if (GUILayout.Button($"{combatant.displayName}  [{combatant.characterId}]", GUILayout.Width(280f)))
                    {
                        tabIndex = 0;
                        SelectCombatant(combatant);
                    }
                }

                if (!GetCombatantsUsingSkill(selectedSkill).Any())
                {
                    EditorGUILayout.HelpBox("当前没有单位使用这个技能。", MessageType.None);
                }

                EditorGUILayout.Space(8f);
                skillDetailsScroll = EditorGUILayout.BeginScrollView(skillDetailsScroll);
                EnsureSelectedSkillEditor();
                selectedSkillEditor?.OnInspectorGUI();
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawPassiveListPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(320f)))
            {
                EditorGUILayout.LabelField("被动列表", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("查看当前被动规则，并追踪哪些英雄挂载了这些被动。", MessageType.None);
                passiveListScroll = EditorGUILayout.BeginScrollView(passiveListScroll, "box");

                foreach (HeroPassive passive in Enum.GetValues(typeof(HeroPassive)))
                {
                    if (passive == HeroPassive.None)
                    {
                        continue;
                    }

                    var isSelected = selectedPassive == passive;
                    if (GUILayout.Toggle(isSelected, GetPassiveDisplayName(passive), "Button"))
                    {
                        selectedPassive = passive;
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private HeroPassive selectedPassive = HeroPassive.Executioner;

        private void DrawPassiveDetailsPane()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(GetPassiveDisplayName(selectedPassive), EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(GetPassiveRuleSummary(selectedPassive), MessageType.None);

                EditorGUILayout.LabelField("已挂载英雄", EditorStyles.boldLabel);
                passiveDetailsScroll = EditorGUILayout.BeginScrollView(passiveDetailsScroll);
                var users = combatants.Where(combatant => combatant != null && combatant.isHero && combatant.passive == selectedPassive).ToArray();
                foreach (var combatant in users)
                {
                    if (GUILayout.Button($"{combatant.displayName}  [{combatant.characterId}]  {combatant.ArchetypeDisplayName}/{combatant.SpecializationDisplayName}", GUILayout.Width(420f)))
                    {
                        tabIndex = 0;
                        SelectCombatant(combatant);
                    }
                }

                if (users.Length == 0)
                {
                    EditorGUILayout.HelpBox("当前没有英雄挂载这个被动。", MessageType.None);
                }

                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("设计备注", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(GetPassiveDesignNote(selectedPassive), MessageType.None);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSkillList()
        {
            EditorGUILayout.LabelField("技能槽", EditorStyles.boldLabel);
            for (var i = 0; i < createSkills.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    createSkills[i] = (SkillData)EditorGUILayout.ObjectField($"Skill {i + 1}", createSkills[i], typeof(SkillData), false);
                    if (GUILayout.Button("-", GUILayout.Width(28f)))
                    {
                        createSkills.RemoveAt(i);
                        i--;
                    }
                }
            }

            if (GUILayout.Button("添加技能槽", GUILayout.Width(120f)))
            {
                createSkills.Add(null);
            }
        }

        private void DrawSpriteList(string label, List<Sprite> sprites, bool allowCollectSelection)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            for (var i = 0; i < sprites.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    sprites[i] = (Sprite)EditorGUILayout.ObjectField($"{label} {i + 1}", sprites[i], typeof(Sprite), false);
                    if (GUILayout.Button("-", GUILayout.Width(28f)))
                    {
                        sprites.RemoveAt(i);
                        i--;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加图片槽", GUILayout.Width(120f)))
                {
                    sprites.Add(null);
                }

                if (allowCollectSelection && GUILayout.Button("从当前选择替换", GUILayout.Width(160f)))
                {
                    ReplaceSpriteListFromSelection(sprites);
                }
            }
        }

        private void CreateOrUpdateCombatant()
        {
            EnsureCoreFolders();

            var characterId = SanitizeId(createCharacterId);
            if (string.IsNullOrWhiteSpace(characterId))
            {
                EditorUtility.DisplayDialog("创建单位", "角色 ID 不能为空。", "确定");
                return;
            }

            if (string.IsNullOrWhiteSpace(createDisplayName))
            {
                EditorUtility.DisplayDialog("创建单位", "显示名称不能为空。", "确定");
                return;
            }

            var idleFrames = PrepareManagedIdleFrames(characterId, ResolveIdleSourceSprites(createIdleFrames, createIdleSourceAsset));
            if (idleFrames.Length == 0)
            {
                EditorUtility.DisplayDialog("创建单位", "至少需要一张待机帧图片。", "确定");
                return;
            }

            var combatant = LoadOrCreateCombatant(characterId, createDisplayName);
            combatant.characterId = characterId;
            combatant.displayName = createDisplayName.Trim();
            combatant.isHero = createIsHero;
            combatant.isUnlocked = createIsHero && createStartUnlocked;
            combatant.recruitPrice = Mathf.Max(0, createRecruitPrice);
            combatant.maxHealth = Mathf.Max(1, createMaxHealth);
            combatant.speed = Mathf.Max(0, createSpeed);
            combatant.attack = Mathf.Max(0, createAttack);
            combatant.defense = Mathf.Max(0, createDefense);
            combatant.corpseHealth = Mathf.Max(0, createCorpseHealth);
            combatant.occupiedSlotCount = Mathf.Max(1, createOccupiedSlotCount);
            combatant.visualScale = Mathf.Max(0.1f, createVisualScale);
            combatant.tint = createTint;
            combatant.archetype = createArchetype;
            combatant.preferredRow = createPreferredRow;
            combatant.specialization = createSpecialization;
            combatant.growthData = createIsHero ? (createGrowthData != null ? createGrowthData : LoadOrCreateDefaultGrowthData()) : null;
            combatant.idleAnimationFrames = idleFrames;
            combatant.battleSprite = idleFrames[0];
            combatant.portrait = idleFrames[0];
            combatant.skills = ResolveSkillSelection(createSkills);
            combatant.currentLevel = Mathf.Max(1, combatant.currentLevel);
            combatant.currentExperience = Mathf.Max(0, combatant.currentExperience);
            combatant.currentHealth = -1;

            var managedAttackSprite = PrepareManagedOverlaySprite(characterId, createAttackSprite, "Attack", "attack");
            var managedHitSprite = PrepareManagedOverlaySprite(characterId, createHitSprite, "Hit", "hit");
            CopyDefaultCharacterSpecificVfx(characterId, managedAttackSprite, managedHitSprite);
            combatant.unitPrefabPath = GetUnitPrefabPath(combatant);
            combatant.unitPrefab = CreateOrUpdateCombatUnitPrefab(combatant);

            EditorUtility.SetDirty(combatant);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshCombatants();
            SelectCombatant(combatant);
            EditorUtility.DisplayDialog("创建单位", $"已创建/更新：{combatant.displayName}", "确定");
        }

        private void ApplyVisualsToExistingCombatant(CombatantDefinition combatant)
        {
            if (combatant == null)
            {
                return;
            }

            var idleFrames = PrepareManagedIdleFrames(combatant.characterId, ResolveIdleSourceSprites(editIdleFrames, editIdleSourceAsset));
            if (idleFrames.Length > 0)
            {
                combatant.idleAnimationFrames = idleFrames;
                combatant.battleSprite = idleFrames[0];
                combatant.portrait = idleFrames[0];
            }

            var managedAttackSprite = PrepareManagedOverlaySprite(combatant.characterId, editAttackSprite, "Attack", "attack");
            var managedHitSprite = PrepareManagedOverlaySprite(combatant.characterId, editHitSprite, "Hit", "hit");
            CopyDefaultCharacterSpecificVfx(combatant.characterId, managedAttackSprite, managedHitSprite);
            combatant.unitPrefabPath = GetUnitPrefabPath(combatant);
            combatant.unitPrefab = CreateOrUpdateCombatUnitPrefab(combatant);
            EditorUtility.SetDirty(combatant);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshCombatants();
            SelectCombatant(combatant);
        }

        private void RebuildCombatantPrefab(CombatantDefinition combatant)
        {
            if (combatant == null)
            {
                return;
            }

            combatant.unitPrefabPath = GetUnitPrefabPath(combatant);
            combatant.unitPrefab = CreateOrUpdateCombatUnitPrefab(combatant);
            EditorUtility.SetDirty(combatant);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshCombatants();
            SelectCombatant(combatant);
        }

        private static CombatantDefinition LoadOrCreateCombatant(string characterId, string displayName)
        {
            var existing = FindCombatantById(characterId);
            if (existing != null)
            {
                return existing;
            }

            var assetPath = $"{CombatantsPath}/{ToAssetName(displayName)}.asset";
            var combatant = AssetDatabase.LoadAssetAtPath<CombatantDefinition>(assetPath);
            if (combatant == null)
            {
                combatant = ScriptableObject.CreateInstance<CombatantDefinition>();
                AssetDatabase.CreateAsset(combatant, assetPath);
            }

            return combatant;
        }

        private static CombatantDefinition FindCombatantById(string characterId)
        {
            var sanitized = SanitizeId(characterId);
            var guids = AssetDatabase.FindAssets("t:CombatantDefinition", new[] { CombatantsPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var combatant = AssetDatabase.LoadAssetAtPath<CombatantDefinition>(path);
                if (combatant != null && string.Equals(SanitizeId(combatant.characterId), sanitized, StringComparison.Ordinal))
                {
                    return combatant;
                }
            }

            return null;
        }

        private static SkillData[] ResolveSkillSelection(List<SkillData> skills)
        {
            var result = skills.Where(skill => skill != null).Distinct().ToArray();
            if (result.Length > 0)
            {
                return result;
            }

            var fallback = AssetDatabase.FindAssets("t:SkillData", new[] { Root + "/Data/Skills" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SkillData>)
                .FirstOrDefault(skill => skill != null);
            return fallback != null ? new[] { fallback } : Array.Empty<SkillData>();
        }

        private static void CopyDefaultCharacterSpecificVfx(string characterId, Sprite attackSprite, Sprite hitSprite)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            EnsureFolderPath(CharacterSpecificVfxRoot);
            var folder = GetCharacterSpecificVfxFolder(characterId);
            EnsureFolderPath(folder);

            if (attackSprite != null)
            {
                CopySpriteAsset(attackSprite, $"{folder}/default_attack{Path.GetExtension(AssetDatabase.GetAssetPath(attackSprite))}");
            }

            if (hitSprite != null)
            {
                CopySpriteAsset(hitSprite, $"{folder}/default_hit{Path.GetExtension(AssetDatabase.GetAssetPath(hitSprite))}");
            }
        }

        private static Sprite[] PrepareManagedIdleFrames(string characterId, Sprite[] sourceSprites)
        {
            if (string.IsNullOrWhiteSpace(characterId) || sourceSprites == null || sourceSprites.Length == 0)
            {
                return Array.Empty<Sprite>();
            }

            var idleFolder = $"{GetCharacterArtFolder(characterId)}/Idle";
            EnsureFolderPath(idleFolder);
            var distinctSourcePaths = sourceSprites
                .Where(sprite => sprite != null)
                .Select(AssetDatabase.GetAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct()
                .ToArray();

            if (distinctSourcePaths.Length == 1)
            {
                var extension = Path.GetExtension(distinctSourcePaths[0]);
                var targetPath = $"{idleFolder}/{SanitizeId(characterId)}_idle_sheet{extension}";
                CopyAssetToManagedPath(distinctSourcePaths[0], targetPath);
                return AssetDatabase.LoadAllAssetsAtPath(targetPath)
                    .OfType<Sprite>()
                    .OrderByDescending(sprite => sprite.rect.y)
                    .ThenBy(sprite => sprite.rect.x)
                    .ThenBy(sprite => sprite.name)
                    .ToArray();
            }

            var orderedSources = sourceSprites
                .Where(sprite => sprite != null)
                .OrderBy(sprite => sprite.name)
                .ToArray();
            var managedSprites = new List<Sprite>(orderedSources.Length);
            for (var i = 0; i < orderedSources.Length; i++)
            {
                var sourcePath = AssetDatabase.GetAssetPath(orderedSources[i]);
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    continue;
                }

                var extension = Path.GetExtension(sourcePath);
                var targetPath = $"{idleFolder}/{SanitizeId(characterId)}_idle_{i + 1:000}{extension}";
                CopyAssetToManagedPath(sourcePath, targetPath);
                var copiedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(targetPath);
                if (copiedSprite != null)
                {
                    managedSprites.Add(copiedSprite);
                }
            }

            return managedSprites.ToArray();
        }

        private static Sprite PrepareManagedOverlaySprite(string characterId, Sprite sprite, string categoryFolder, string canonicalName)
        {
            if (string.IsNullOrWhiteSpace(characterId) || sprite == null)
            {
                return null;
            }

            var sourcePath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return null;
            }

            var targetFolder = $"{GetCharacterArtFolder(characterId)}/{categoryFolder}";
            EnsureFolderPath(targetFolder);
            var extension = Path.GetExtension(sourcePath);
            var targetPath = $"{targetFolder}/{SanitizeId(characterId)}_{canonicalName}{extension}";
            CopyAssetToManagedPath(sourcePath, targetPath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(targetPath);
        }

        private static string GetCharacterArtFolder(string characterId)
        {
            return $"{CharacterArtRoot}/{SanitizeId(characterId)}";
        }

        private static void CopySpriteAsset(Sprite sprite, string targetPath)
        {
            var sourcePath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return;
            }

            CopyAssetToManagedPath(sourcePath, targetPath);
        }

        private static void CopyAssetToManagedPath(string sourcePath, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(targetPath))
            {
                return;
            }

            EnsureFolderPath(Path.GetDirectoryName(targetPath)?.Replace('\\', '/') ?? "Assets");
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetPath) != null)
            {
                AssetDatabase.DeleteAsset(targetPath);
            }

            AssetDatabase.CopyAsset(sourcePath, targetPath);
        }

        private static CombatantView CreateOrUpdateCombatUnitPrefab(CombatantDefinition combatant)
        {
            EnsureFolderPath(UnitPrefabsPath);
            var prefabPath = GetUnitPrefabPath(combatant);
            var prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
            var prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(prefabPath)
                : new GameObject(GetUnitPrefabName(combatant), typeof(CombatantView));

            try
            {
                var view = prefabRoot.GetComponent<CombatantView>() ?? prefabRoot.AddComponent<CombatantView>();
                var visualRoot = EnsureChild(prefabRoot.transform, "VisualRoot");
                var bodySpriteTransform = EnsureChild(visualRoot, "BodySprite");
                var bodyRenderer = EnsureSpriteRenderer(bodySpriteTransform, 2);
                bodyRenderer.sprite = combatant.idleAnimationFrames != null && combatant.idleAnimationFrames.Length > 0 ? combatant.idleAnimationFrames[0] : combatant.battleSprite;
                bodyRenderer.color = combatant.tint;

                var actionOverlayTransform = EnsureChild(visualRoot, "ActionOverlay");
                var actionOverlay = EnsureSpriteRenderer(actionOverlayTransform, 80);
                actionOverlay.enabled = false;
                if (!prefabExists || actionOverlayTransform.localPosition == Vector3.zero)
                {
                    actionOverlayTransform.localPosition = GetDefaultOverlayPosition(combatant);
                }
                if (!prefabExists || IsZeroScale(actionOverlayTransform.localScale))
                {
                    actionOverlayTransform.localScale = GetDefaultOverlayScale(combatant);
                }

                var hitOverlayTransform = EnsureChild(visualRoot, "HitOverlay");
                var hitOverlay = EnsureSpriteRenderer(hitOverlayTransform, 81);
                hitOverlay.enabled = false;
                if (!prefabExists || hitOverlayTransform.localPosition == Vector3.zero)
                {
                    hitOverlayTransform.localPosition = actionOverlayTransform.localPosition;
                }
                if (!prefabExists || IsZeroScale(hitOverlayTransform.localScale))
                {
                    hitOverlayTransform.localScale = actionOverlayTransform.localScale;
                }

                var nameplatePosition = EnsureChild(prefabRoot.transform, "NameplatePosition");
                if (nameplatePosition.localPosition == Vector3.zero)
                {
                    nameplatePosition.localPosition = new Vector3(0f, -1.826f, 0f);
                }

                var colliderTransform = EnsureChild(prefabRoot.transform, "Collider");
                var clickCollider = colliderTransform.GetComponent<BoxCollider2D>() ?? colliderTransform.gameObject.AddComponent<BoxCollider2D>();
                if (colliderTransform.GetComponent<CombatantClickProxy>() == null)
                {
                    colliderTransform.gameObject.AddComponent<CombatantClickProxy>();
                }

                BindCombatantViewReferences(view, visualRoot, bodyRenderer, actionOverlay, hitOverlay, nameplatePosition, clickCollider);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                if (prefabExists)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
                else
                {
                    DestroyImmediate(prefabRoot);
                }
            }

            return AssetDatabase.LoadAssetAtPath<CombatantView>(prefabPath);
        }

        private static SpriteRenderer EnsureSpriteRenderer(Transform transform, int defaultSortingOrder)
        {
            if (transform == null)
            {
                throw new InvalidOperationException("SpriteRenderer 目标节点不存在。");
            }

            if (!transform.TryGetComponent<SpriteRenderer>(out var renderer) || renderer == null)
            {
                renderer = transform.gameObject.AddComponent<SpriteRenderer>();
            }

            if (renderer == null)
            {
                throw new InvalidOperationException($"无法在节点 {transform.name} 上创建 SpriteRenderer。");
            }

            if (renderer.sortingOrder == 0)
            {
                renderer.sortingOrder = defaultSortingOrder;
            }

            return renderer;
        }

        private static void RunEditorAction(string title, Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog(title, $"执行失败：{ex.Message}", "确定");
                GUIUtility.ExitGUI();
            }
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            var childObject = new GameObject(childName);
            child = childObject.transform;
            child.SetParent(parent, false);
            return child;
        }

        private static bool IsZeroScale(Vector3 scale)
        {
            return Mathf.Approximately(scale.x, 0f) && Mathf.Approximately(scale.y, 0f) && Mathf.Approximately(scale.z, 0f);
        }

        private static void BindCombatantViewReferences(CombatantView view, Transform visualRoot, SpriteRenderer bodyRenderer, SpriteRenderer actionOverlay, SpriteRenderer hitOverlay, Transform nameplatePosition, BoxCollider2D clickCollider)
        {
            var serializedView = new SerializedObject(view);
            SetObjectReferenceIfPropertyExists(serializedView, "visualRoot", visualRoot);
            SetObjectReferenceIfPropertyExists(serializedView, "spriteRenderer", bodyRenderer);
            SetObjectReferenceIfPropertyExists(serializedView, "overlayRenderer", actionOverlay);
            SetObjectReferenceIfPropertyExists(serializedView, "hitOverlayRenderer", hitOverlay);
            SetObjectReferenceIfPropertyExists(serializedView, "nameplatePosition", nameplatePosition);
            SetObjectReferenceIfPropertyExists(serializedView, "clickCollider", clickCollider);
            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectReferenceIfPropertyExists(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static HeroGrowthData LoadOrCreateDefaultGrowthData()
        {
            EnsureFolderPath(GrowthsPath);
            const string assetPath = GrowthsPath + "/DefaultHeroGrowth.asset";
            var growth = AssetDatabase.LoadAssetAtPath<HeroGrowthData>(assetPath);
            if (growth == null)
            {
                growth = ScriptableObject.CreateInstance<HeroGrowthData>();
                growth.experiencePerLevel = 100;
                growth.maxHealthPerLevel = 5;
                growth.attackPerLevel = 2;
                growth.defensePerLevel = 1;
                AssetDatabase.CreateAsset(growth, assetPath);
            }

            return growth;
        }

        private void RefreshCombatants()
        {
            combatants = AssetDatabase.FindAssets("t:CombatantDefinition", new[] { CombatantsPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CombatantDefinition>)
                .Where(combatant => combatant != null)
                .OrderBy(combatant => combatant.isHero ? 0 : 1)
                .ThenBy(combatant => combatant.displayName)
                .ToArray();

            if (selectedCombatant != null)
            {
                selectedCombatant = combatants.FirstOrDefault(combatant => combatant == selectedCombatant || combatant.characterId == selectedCombatant.characterId);
            }
        }

        private void RefreshSkills()
        {
            skills = AssetDatabase.FindAssets("t:SkillData", new[] { SkillsPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SkillData>)
                .Where(skill => skill != null)
                .OrderBy(skill => skill.skillName)
                .ThenBy(skill => skill.skillId)
                .ToArray();

            if (selectedSkill != null)
            {
                selectedSkill = skills.FirstOrDefault(skill => skill == selectedSkill || skill.skillId == selectedSkill.skillId);
            }
        }

        private void SelectCombatant(CombatantDefinition combatant)
        {
            if (selectedCombatant == combatant)
            {
                return;
            }

            selectedCombatant = combatant;
            editIdleFrames.Clear();
            editAttackSprite = null;
            editHitSprite = null;
            DestroyCachedEditor();
        }

        private void EnsureSelectedEditor()
        {
            if (selectedCombatant == null)
            {
                DestroyCachedEditor();
                return;
            }

            if (selectedCombatantEditor == null || selectedCombatantEditor.target != selectedCombatant)
            {
                DestroyCachedEditor();
                selectedCombatantEditor = UnityEditor.Editor.CreateEditor(selectedCombatant);
            }
        }

        private void DestroyCachedEditor()
        {
            if (selectedCombatantEditor != null)
            {
                DestroyImmediate(selectedCombatantEditor);
                selectedCombatantEditor = null;
            }

            if (selectedSkillEditor != null)
            {
                DestroyImmediate(selectedSkillEditor);
                selectedSkillEditor = null;
            }
        }

        private void SelectSkill(SkillData skill)
        {
            if (selectedSkill == skill)
            {
                return;
            }

            selectedSkill = skill;
            if (selectedSkillEditor != null)
            {
                DestroyImmediate(selectedSkillEditor);
                selectedSkillEditor = null;
            }
        }

        private void EnsureSelectedSkillEditor()
        {
            if (selectedSkill == null)
            {
                if (selectedSkillEditor != null)
                {
                    DestroyImmediate(selectedSkillEditor);
                    selectedSkillEditor = null;
                }

                return;
            }

            if (selectedSkillEditor == null || selectedSkillEditor.target != selectedSkill)
            {
                if (selectedSkillEditor != null)
                {
                    DestroyImmediate(selectedSkillEditor);
                }

                selectedSkillEditor = UnityEditor.Editor.CreateEditor(selectedSkill);
            }
        }

        private void CreateSkillAssetFromDraft()
        {
            EnsureFolderPath(SkillsPath);
            var sanitizedId = SanitizeId(createSkillId);
            if (string.IsNullOrWhiteSpace(sanitizedId))
            {
                EditorUtility.DisplayDialog("创建技能", "技能 ID 不能为空。", "确定");
                return;
            }

            var assetPath = $"{SkillsPath}/{ToAssetName(createSkillName)}.asset";
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>(assetPath);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillData>();
                AssetDatabase.CreateAsset(skill, assetPath);
            }

            skill.skillId = sanitizedId;
            skill.skillName = string.IsNullOrWhiteSpace(createSkillName) ? sanitizedId : createSkillName.Trim();
            skill.description = createSkillDescription ?? string.Empty;
            skill.skillType = createSkillType;
            skill.targetType = createSkillTargetType;
            skill.baseValue = Mathf.Max(0, createSkillBaseValue);
            skill.powerMultiplier = Mathf.Max(0.1f, createSkillPowerMultiplier);
            skill.manaCost = Mathf.Max(0, createSkillManaCost);
            skill.cooldown = Mathf.Max(0, createSkillCooldown);
            skill.casterAllowedPositions = new[] { 1, 2, 3, 4 };
            skill.targetAllowedPositions = new[] { 1, 2, 3, 4 };
            EditorUtility.SetDirty(skill);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshSkills();
            SelectSkill(skill);
        }

        private void DuplicateSelectedSkill()
        {
            if (selectedSkill == null)
            {
                return;
            }

            EnsureFolderPath(SkillsPath);
            var path = AssetDatabase.GetAssetPath(selectedSkill);
            var duplicatedPath = AssetDatabase.GenerateUniqueAssetPath($"{SkillsPath}/{ToAssetName(selectedSkill.skillName)}_Copy.asset");
            if (!AssetDatabase.CopyAsset(path, duplicatedPath))
            {
                EditorUtility.DisplayDialog("复制技能", "复制技能资源失败。", "确定");
                return;
            }

            var duplicate = AssetDatabase.LoadAssetAtPath<SkillData>(duplicatedPath);
            if (duplicate != null)
            {
                duplicate.skillId = $"{SanitizeId(selectedSkill.skillId)}_copy";
                duplicate.skillName = $"{selectedSkill.skillName} Copy";
                EditorUtility.SetDirty(duplicate);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshSkills();
            SelectSkill(duplicate);
        }

        private void AssignSelectedSkillToSelectedCombatant()
        {
            if (selectedSkill == null || selectedCombatant == null)
            {
                return;
            }

            var skillList = (selectedCombatant.skills ?? Array.Empty<SkillData>()).Where(skill => skill != null).ToList();
            if (!skillList.Contains(selectedSkill))
            {
                skillList.Add(selectedSkill);
                selectedCombatant.skills = skillList.ToArray();
                EditorUtility.SetDirty(selectedCombatant);
                AssetDatabase.SaveAssets();
                RefreshCombatants();
            }
        }

        private IEnumerable<CombatantDefinition> GetCombatantsUsingSkill(SkillData skill)
        {
            return combatants.Where(combatant => combatant != null && combatant.skills != null && combatant.skills.Contains(skill));
        }

        private bool DoesSkillMatchSearch(SkillData skill)
        {
            if (skill == null || string.IsNullOrWhiteSpace(skillSearch))
            {
                return skill != null;
            }

            return skill.skillName.IndexOf(skillSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   skill.skillId.IndexOf(skillSearch, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetPassiveDisplayName(HeroPassive passive)
        {
            return passive switch
            {
                HeroPassive.Berserker => "狂战士",
                HeroPassive.Executioner => "处决者",
                HeroPassive.ChainReaction => "连锁反应",
                HeroPassive.Backstab => "背刺",
                HeroPassive.GlassCannon => "玻璃大炮",
                HeroPassive.IronWill => "铁意志",
                HeroPassive.Regenerator => "再生",
                HeroPassive.ThornArmor => "荆棘护甲",
                HeroPassive.Bodyguard => "保镖",
                HeroPassive.Fortress => "堡垒",
                HeroPassive.Tactician => "战术家",
                HeroPassive.Scavenger => "回收者",
                HeroPassive.Vanguard => "先锋",
                HeroPassive.Reaper => "收割者",
                HeroPassive.Inspirer => "鼓舞者",
                _ => "无"
            };
        }

        private static string GetPassiveRuleSummary(HeroPassive passive)
        {
            return passive switch
            {
                HeroPassive.Berserker => "血量低于 50% 时攻击提升。",
                HeroPassive.Executioner => "攻击残血目标时伤害显著提升。",
                HeroPassive.ChainReaction => "击杀后对另一敌人造成溅射伤害。",
                HeroPassive.Backstab => "从后排压前排时伤害提高。",
                HeroPassive.GlassCannon => "高攻击，低防御。",
                HeroPassive.IronWill => "每场战斗首次致死时保留 1 点生命。",
                HeroPassive.Regenerator => "回合开始时恢复生命。",
                HeroPassive.ThornArmor => "受击时反弹部分伤害。",
                HeroPassive.Bodyguard => "相邻队友受击时分担伤害。",
                HeroPassive.Fortress => "位于前排时防御更高。",
                HeroPassive.Tactician => "回合开始时帮助队友转冷。",
                HeroPassive.Scavenger => "击杀敌人时自我恢复。",
                HeroPassive.Vanguard => "位于前排时为全队提供攻击光环。",
                HeroPassive.Reaper => "敌人死亡越多，自身伤害越高。",
                HeroPassive.Inspirer => "回合开始时恢复全队生命。",
                _ => "未定义。"
            };
        }

        private static string GetPassiveDesignNote(HeroPassive passive)
        {
            return passive switch
            {
                HeroPassive.Executioner => "适合和 Slayer / Breaker 类专精配合，强化收尾节奏。",
                HeroPassive.Backstab => "适合与后排点杀技能组合，形成高机动爆发。",
                HeroPassive.Bodyguard => "更适合 Bulwark 的护卫路线，强化队友保护。",
                HeroPassive.Tactician => "更适合 Stimulator 路线，围绕冷却和资源支援。",
                HeroPassive.ChainReaction => "更适合 Bombardier 路线，强化 AOE 和击杀扩散。",
                _ => "当前仍是 enum 规则；后续如需更深成长，可以升级成 PassiveData 资产。"
            };
        }

        private static void ReplaceSpriteListFromSelection(List<Sprite> target)
        {
            target.Clear();
            var selectedSprites = Selection.objects.OfType<Sprite>().ToArray();
            var distinctPaths = selectedSprites
                .Select(AssetDatabase.GetAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct()
                .ToArray();

            if (distinctPaths.Length == 1)
            {
                target.AddRange(selectedSprites
                    .OrderByDescending(sprite => sprite.rect.y)
                    .ThenBy(sprite => sprite.rect.x)
                    .ThenBy(sprite => sprite.name));
                return;
            }

            target.AddRange(selectedSprites.OrderBy(sprite => sprite.name));
        }

        private static void ReplaceSpriteListFromAsset(List<Sprite> target, UnityEngine.Object sourceAsset)
        {
            target.Clear();
            target.AddRange(LoadSpritesFromAsset(sourceAsset));
        }

        private static Sprite[] ResolveIdleSourceSprites(List<Sprite> explicitSprites, UnityEngine.Object sourceAsset)
        {
            var sprites = explicitSprites.Where(sprite => sprite != null).ToArray();
            if (sprites.Length > 0)
            {
                return sprites;
            }

            return LoadSpritesFromAsset(sourceAsset);
        }

        private static Sprite[] LoadSpritesFromAsset(UnityEngine.Object sourceAsset)
        {
            if (sourceAsset == null)
            {
                return Array.Empty<Sprite>();
            }

            var assetPath = AssetDatabase.GetAssetPath(sourceAsset);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return Array.Empty<Sprite>();
            }

            var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderByDescending(sprite => sprite.rect.y)
                .ThenBy(sprite => sprite.rect.x)
                .ThenBy(sprite => sprite.name)
                .ToArray();

            if (sprites.Length == 0 && sourceAsset is Sprite singleSprite)
            {
                return new[] { singleSprite };
            }

            return sprites;
        }

        private static void AddSelectedSkills(List<SkillData> target)
        {
            foreach (var skill in Selection.objects.OfType<SkillData>())
            {
                if (!target.Contains(skill))
                {
                    target.Add(skill);
                }
            }
        }

        private static string GetCharacterSpecificVfxFolder(string characterId)
        {
            return $"{CharacterSpecificVfxRoot}/{SanitizeId(characterId)}";
        }

        private static string GetUnitPrefabPath(CombatantDefinition combatant)
        {
            return $"{UnitPrefabsPath}/{GetUnitPrefabName(combatant)}.prefab";
        }

        private static string GetUnitPrefabName(CombatantDefinition combatant)
        {
            return $"{SanitizeId(combatant.characterId)}_Unit";
        }

        private static string ToAssetName(string displayName)
        {
            var sanitized = displayName;
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }

            return sanitized.Replace(' ', '_');
        }

        private static string SanitizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unnamed";
            }

            var chars = value.Trim().ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray();
            return new string(chars).Trim('_');
        }

        private static void EnsureCoreFolders()
        {
            EnsureFolderPath(CombatantsPath);
            EnsureFolderPath(GrowthsPath);
            EnsureFolderPath(UnitPrefabsPath);
            EnsureFolderPath(CharacterSpecificVfxRoot);
        }

        private static void EnsureFolderPath(string assetPath)
        {
            var segments = assetPath.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private static Vector3 GetDefaultOverlayPosition(CombatantDefinition combatant)
        {
            return combatant.characterId switch
            {
                "hero_02" => new Vector3(0f, 0.05f, -0.6f),
                "hero_03" => new Vector3(0f, 0.12f, -0.6f),
                "hero_04" => new Vector3(0f, 0.08f, -0.6f),
                "hero_05" => new Vector3(0f, 0.08f, -0.6f),
                "enemy_02" => new Vector3(0f, 0.05f, -0.6f),
                "enemy_03" => new Vector3(0f, 0.12f, -0.6f),
                _ => new Vector3(0f, 0f, -0.6f)
            };
        }

        private static Vector3 GetDefaultOverlayScale(CombatantDefinition combatant)
        {
            return combatant.characterId switch
            {
                "hero_03" => new Vector3(1.12f, 1.12f, 1f),
                "hero_04" => new Vector3(1.06f, 1.06f, 1f),
                "hero_05" => new Vector3(1.06f, 1.06f, 1f),
                "enemy_03" => new Vector3(1.12f, 1.12f, 1f),
                _ => Vector3.one
            };
        }
    }
}
