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
        private const string BuffsPath = "Assets/Resources/Buffs";
        private const string GrowthsPath = Root + "/Data/Growth";
        private const string UnitPrefabsPath = Root + "/Prefabs/CombatUnits";
        private const float VisualRootY = -1.4f;
        private const float NameplatePositionY = -1.726f;
        private const string CharacterArtRoot = "Assets/Art/Characters";
        private const string CharacterSpecificVfxRoot = "Assets/Art/VFX/Combat/CharacterSpecific";

        private readonly List<Sprite> createIdleFrames = new List<Sprite>();
        private readonly List<SkillData> createSkills = new List<SkillData>();
        private readonly List<Sprite> editIdleFrames = new List<Sprite>();

        private CombatantDefinition[] combatants = Array.Empty<CombatantDefinition>();
        private SkillData[] skills = Array.Empty<SkillData>();
        private BuffData[] buffs = Array.Empty<BuffData>();
        private CombatantDefinition selectedCombatant;
        private SkillData selectedSkill;
        private BuffData selectedBuff;
        private UnityEditor.Editor selectedCombatantEditor;
        private UnityEditor.Editor selectedSkillEditor;
        private UnityEditor.Editor selectedBuffEditor;
        private Vector2 listScroll;
        private Vector2 detailsScroll;
        private Vector2 createScroll;
        private Vector2 skillListScroll;
        private Vector2 skillDetailsScroll;
        private Vector2 passiveListScroll;
        private Vector2 passiveDetailsScroll;
        private Vector2 buffListScroll;
        private Vector2 buffDetailsScroll;
        private int tabIndex;
        private string skillSearch = string.Empty;
        private string buffSearch = string.Empty;
        private string createSkillId = "new_skill";
        private string createSkillName = "新技能";
        private string createSkillDescription = "一个战斗动作。";
        private SkillDataType createSkillType = SkillDataType.伤害;
        private SkillDataTargetType createSkillTargetType = SkillDataTargetType.单敌;
        private int createSkillBaseValue = 8;
        private float createSkillPowerMultiplier = 1f;
        private int createSkillManaCost;
        private int createSkillCooldown;
        private string createBuffId = "new_buff";
        private string createBuffName = "新Buff";
        private string createBuffDescription = "Buff效果描述";
        private bool createBuffStun;
        private int createBuffTickDamage;
        private Color createBuffNameColor = Color.white;
        private Color createBuffDamageColor = new Color(1f, 0.3f, 0.3f, 1f);
        private Sprite createBuffIcon;

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
        private HeroPassive createPassive = HeroPassive.None;
        private HeroPassive createGrowthPassive = HeroPassive.None;
        private HeroGrowthData createGrowthData;
        private UnityEngine.Object createIdleSourceAsset;
        private Sprite createAttackSprite;
        private Sprite createHitSprite;
        private CombatantDefinition createTemplateCombatant;
        private bool createTemplateHeroesOnly = true;

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

        [MenuItem("Clockwork Wasteland/Tools/Normalize Combat Unit Prefabs")]
        public static void NormalizeCombatUnitPrefabs()
        {
            RefreshCombatantPrefabsOnDisk();
            EditorUtility.DisplayDialog("Combat Unit Prefabs Normalized", "Rebuilt combat unit prefabs with a foot-based baseline.", "OK");
        }

        private void OnEnable()
        {
            EnsureCommonBuffAssets();
            RefreshCombatants();
            RefreshSkills();
            RefreshBuffs();
            createGrowthData = LoadOrCreateDefaultGrowthData();
        }

        private void OnDisable()
        {
            DestroyCachedEditor();
        }

        private void OnGUI()
        {
            DrawToolbarSafe();
            EditorGUILayout.Space(6f);
            if (tabIndex == 1)
            {
                DrawCreateTemplateImportBar();
                EditorGUILayout.Space(6f);
            }

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
                case 3:
                    DrawBuffsTab();
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

        private void DrawCreateTemplateImportBar()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("模板导入", EditorStyles.boldLabel);
                createTemplateHeroesOnly = EditorGUILayout.Toggle("仅英雄模板", createTemplateHeroesOnly);
                createTemplateCombatant = (CombatantDefinition)EditorGUILayout.ObjectField("模板单位", createTemplateCombatant, typeof(CombatantDefinition), false);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("从当前选中导入", GUILayout.Width(180f)))
                    {
                        ImportTemplateFromSelection();
                    }

                    if (GUILayout.Button("导入模板参数", GUILayout.Width(140f)))
                    {
                        ImportTemplateToCreateForm(createTemplateCombatant);
                    }
                }
            }
        }

        private void DrawToolbarSafe()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                tabIndex = GUILayout.Toolbar(tabIndex, new[] { "单位", "创建单位", "技能", "Buff", "被动" }, EditorStyles.toolbarButton, GUILayout.Width(460f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                {
                    RefreshCombatants();
                    RefreshSkills();
                    RefreshBuffs();
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

                    if (GUILayout.Button("删除单位", GUILayout.Width(120f)))
                    {
                        RunEditorAction("删除单位", () => DeleteCombatant(selectedCombatant));
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
                DrawSelectedCombatantProgressionSummary(selectedCombatant);
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
            createSpecialization = DrawSpecializationPopup("专精分支", createArchetype, createSpecialization);
            createPassive = (HeroPassive)EditorGUILayout.EnumPopup("固定被动", createPassive);
            createGrowthPassive = (HeroPassive)EditorGUILayout.EnumPopup("成长被动", createGrowthPassive);
            createGrowthData = (HeroGrowthData)EditorGUILayout.ObjectField("成长数据", createGrowthData, typeof(HeroGrowthData), false);

            EditorGUILayout.Space(6f);
            DrawCreateProgressionPreview();
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

        private void DrawBuffsTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawBuffListPane();
                DrawBuffDetailsPane();
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

                EditorGUILayout.Space(6f);
                DrawSkillLocalizedEditor(selectedSkill);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("保存技能", GUILayout.Width(120f)))
                    {
                        SaveSelectedSkill();
                    }

                    if (GUILayout.Button("删除技能", GUILayout.Width(120f)))
                    {
                        DeleteSelectedSkill();
                    }
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

        private void DrawBuffListPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(340f)))
            {
                EditorGUILayout.LabelField("Buff 列表", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("创建、筛选和管理 Buff 资产。", MessageType.None);
                buffSearch = EditorGUILayout.TextField("搜索", buffSearch);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("创建Buff", GUILayout.Width(120f)))
                    {
                        CreateBuffAssetFromDraft();
                    }

                    if (GUILayout.Button("复制当前Buff", GUILayout.Width(140f)))
                    {
                        DuplicateSelectedBuff();
                    }
                }

                buffListScroll = EditorGUILayout.BeginScrollView(buffListScroll, "box");
                foreach (var buff in buffs.Where(DoesBuffMatchSearch))
                {
                    if (buff == null)
                    {
                        continue;
                    }

                    var selected = selectedBuff == buff;
                    if (GUILayout.Toggle(selected, $"{buff.buffName}  [{buff.buffId}]", "Button"))
                    {
                        SelectBuff(buff);
                    }
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("新Buff草稿", EditorStyles.boldLabel);
                createBuffId = EditorGUILayout.TextField("Buff ID", createBuffId);
                createBuffName = EditorGUILayout.TextField("Buff 名称", createBuffName);
                createBuffDescription = EditorGUILayout.TextField("描述", createBuffDescription);
                createBuffTickDamage = Mathf.Max(0, EditorGUILayout.IntField("每回合伤害", createBuffTickDamage));
                createBuffStun = EditorGUILayout.Toggle("眩晕", createBuffStun);
                createBuffNameColor = EditorGUILayout.ColorField("名称颜色", createBuffNameColor);
                createBuffDamageColor = EditorGUILayout.ColorField("飘字颜色", createBuffDamageColor);
                createBuffIcon = (Sprite)EditorGUILayout.ObjectField("Buff Icon", createBuffIcon, typeof(Sprite), false);
            }
        }

        private void DrawBuffDetailsPane()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (selectedBuff == null)
                {
                    EditorGUILayout.LabelField("Buff 详情", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("请先在左侧选择一个 Buff。", MessageType.Info);
                    return;
                }

                EditorGUILayout.LabelField(selectedBuff.buffName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Buff ID", selectedBuff.buffId);
                EditorGUILayout.LabelField("资源路径", AssetDatabase.GetAssetPath(selectedBuff));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("定位Buff", GUILayout.Width(120f)))
                    {
                        EditorGUIUtility.PingObject(selectedBuff);
                        Selection.activeObject = selectedBuff;
                    }

                    if (GUILayout.Button("保存Buff", GUILayout.Width(120f)))
                    {
                        SaveSelectedBuff();
                    }

                    if (GUILayout.Button("删除Buff", GUILayout.Width(120f)))
                    {
                        DeleteSelectedBuff();
                    }
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("使用该Buff的技能", EditorStyles.boldLabel);
                foreach (var skill in GetSkillsUsingBuff(selectedBuff))
                {
                    if (skill == null)
                    {
                        continue;
                    }

                    if (GUILayout.Button($"{skill.skillName}  [{skill.skillId}]", GUILayout.Width(320f)))
                    {
                        tabIndex = 2;
                        SelectSkill(skill);
                    }
                }

                if (!GetSkillsUsingBuff(selectedBuff).Any())
                {
                    EditorGUILayout.HelpBox("当前没有技能引用该 Buff。", MessageType.None);
                }

                EditorGUILayout.Space(6f);
                DrawBuffLocalizedEditor(selectedBuff);

            }
        }

        private void DrawBuffLocalizedEditor(BuffData buff)
        {
            if (buff == null)
            {
                return;
            }

            EditorGUILayout.LabelField("中文参数说明", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Buff ID：内部唯一标识。\n" +
                "Buff 名称：战斗和管理器中显示名称。\n" +
                "描述：效果说明文本。\n" +
                "每回合伤害：回合开始结算的持续伤害。\n" +
                "眩晕：勾选后会使目标无法行动。",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            buff.buffId = EditorGUILayout.TextField("Buff ID", buff.buffId ?? string.Empty);
            buff.buffName = EditorGUILayout.TextField("Buff 名称", buff.buffName ?? string.Empty);
            buff.description = EditorGUILayout.TextField("描述", buff.description ?? string.Empty);
            buff.tickDamage = Mathf.Max(0, EditorGUILayout.IntField("每回合伤害", buff.tickDamage));
            buff.stun = EditorGUILayout.Toggle("眩晕", buff.stun);
            buff.nameTextColor = EditorGUILayout.ColorField("名称颜色", buff.nameTextColor);
            buff.damageTextColor = EditorGUILayout.ColorField("飘字颜色", buff.damageTextColor);
            buff.icon = (Sprite)EditorGUILayout.ObjectField("Buff Icon", buff.icon, typeof(Sprite), false);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(buff);
            }
        }

        private void DrawSelectedCombatantProgressionSummary(CombatantDefinition combatant)
        {
            if (combatant == null || !combatant.isHero)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("成长与被动", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("固定被动", GetPassiveDisplayName(combatant.passive));
                EditorGUILayout.HelpBox(GetPassiveRuleSummary(combatant.passive), MessageType.None);

                if (combatant.growthPassive != HeroPassive.None)
                {
                    EditorGUILayout.LabelField("成长被动", GetPassiveDisplayName(combatant.growthPassive));
                    EditorGUILayout.HelpBox(GetPassiveRuleSummary(combatant.growthPassive), MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("当前还没有选择成长被动。", MessageType.None);
                }

                EditorGUILayout.LabelField("2级专精路线", BuildSpecializationSummaryText(combatant.archetype));
                EditorGUILayout.LabelField("3级成长预览", BuildPassivePreviewText(combatant.archetype, combatant.specialization));
            }
        }

        private void DrawCreateProgressionPreview()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("成长配置预览", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("第一版先支持：固定被动、2级专精、3级成长被动。5/7/9级继续留接口。", MessageType.None);
                EditorGUILayout.LabelField("固定被动说明", GetPassiveRuleSummary(createPassive));

                if (createGrowthPassive != HeroPassive.None)
                {
                    EditorGUILayout.LabelField("成长被动说明", GetPassiveRuleSummary(createGrowthPassive));
                }
                else
                {
                    EditorGUILayout.LabelField("成长被动说明", "未指定");
                }

                EditorGUILayout.LabelField("2级专精路线", BuildSpecializationSummaryText(createArchetype));
                EditorGUILayout.LabelField("3级成长预览", BuildPassivePreviewText(createArchetype, createSpecialization));
            }
        }

        private static CombatSpecialization DrawSpecializationPopup(string label, CombatArchetype archetype, CombatSpecialization current)
        {
            var options = HeroProgressionDescriptions.GetAvailableSpecializations(archetype);
            if (options == null || options.Length == 0)
            {
                EditorGUILayout.LabelField(label, "当前职业没有可用专精");
                return CombatSpecialization.None;
            }

            var safeCurrent = options.Contains(current) ? current : options[0];
            var index = Array.IndexOf(options, safeCurrent);
            var names = options.Select(CombatantDefinition.GetSpecializationDisplayName).ToArray();
            index = EditorGUILayout.Popup(label, Mathf.Max(0, index), names);
            index = Mathf.Clamp(index, 0, options.Length - 1);
            return options[index];
        }

        private static string BuildSpecializationSummaryText(CombatArchetype archetype)
        {
            var options = HeroProgressionDescriptions.GetAvailableSpecializations(archetype);
            if (options == null || options.Length == 0)
            {
                return "当前职业原型没有配置专精。";
            }

            return string.Join(" | ", options.Select(specialization =>
                $"{CombatantDefinition.GetSpecializationDisplayName(specialization)}：{HeroProgressionDescriptions.GetSpecializationTrackLabel(specialization)}"));
        }

        private static string BuildPassivePreviewText(CombatArchetype archetype, CombatSpecialization specialization)
        {
            var preview = ScriptableObject.CreateInstance<CombatantDefinition>();
            preview.archetype = archetype;
            preview.specialization = specialization;
            var choices = HeroProgressionDescriptions.GetLevelThreePassiveChoices(preview);
            DestroyImmediate(preview);

            if (choices == null || choices.Length == 0)
            {
                return "当前尚未配置 3 级成长被动。";
            }

            return string.Join(" / ", choices.Select(GetPassiveDisplayName));
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
                var users = combatants.Where(combatant =>
                    combatant != null &&
                    combatant.isHero &&
                    (combatant.passive == selectedPassive || combatant.growthPassive == selectedPassive)).ToArray();
                foreach (var combatant in users)
                {
                    var passiveKind = combatant.passive == selectedPassive && combatant.growthPassive == selectedPassive
                        ? "固定+成长"
                        : combatant.passive == selectedPassive
                            ? "固定"
                            : "成长";
                    if (GUILayout.Button($"{combatant.displayName}  [{combatant.characterId}]  {combatant.ArchetypeDisplayName}/{combatant.SpecializationDisplayName}  ({passiveKind})", GUILayout.Width(460f)))
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
            combatant.passive = createPassive;
            combatant.growthPassive = createGrowthPassive;
            combatant.growthData = createIsHero ? (createGrowthData != null ? createGrowthData : LoadOrCreateDefaultGrowthData()) : null;
            combatant.idleAnimationFrames = idleFrames;
            combatant.battleSprite = idleFrames[0];
            combatant.portrait = idleFrames[0];
            combatant.skills = ResolveSkillSelection(createSkills);
            combatant.currentLevel = Mathf.Max(1, combatant.currentLevel);
            combatant.currentExperience = Mathf.Max(0, combatant.currentExperience);
            combatant.currentHealth = -1;

            CopyDefaultCharacterSpecificVfx(characterId, createAttackSprite, createHitSprite);
            CleanupLegacyCharacterArtOverlayFolders(characterId);
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

            CopyDefaultCharacterSpecificVfx(combatant.characterId, editAttackSprite, editHitSprite);
            CleanupLegacyCharacterArtOverlayFolders(combatant.characterId);
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

        private void DeleteCombatant(CombatantDefinition combatant)
        {
            if (combatant == null)
            {
                return;
            }

            var displayName = string.IsNullOrWhiteSpace(combatant.displayName) ? combatant.characterId : combatant.displayName;
            var choice = EditorUtility.DisplayDialogComplex(
                "删除单位",
                $"将删除单位“{displayName}”的定义、战斗预制体、角色美术目录和默认特效目录。\n此操作不可撤销。",
                "删除",
                "取消",
                "仅删除定义");

            if (choice == 1)
            {
                return;
            }

            var definitionPath = AssetDatabase.GetAssetPath(combatant);
            var prefabPath = GetUnitPrefabPath(combatant);
            var characterArtFolder = GetCharacterArtFolder(combatant.characterId);
            var vfxFolder = GetCharacterSpecificVfxFolder(combatant.characterId);

            SelectCombatant(null);

            if (choice == 0)
            {
                DeleteAssetIfExists(prefabPath);
                DeleteAssetIfExists(characterArtFolder);
                DeleteAssetIfExists(vfxFolder);
            }

            DeleteAssetIfExists(definitionPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshCombatants();
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

        private static string GetCharacterArtFolder(string characterId)
        {
            return $"{CharacterArtRoot}/{SanitizeId(characterId)}";
        }

        private static void CleanupLegacyCharacterArtOverlayFolders(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            DeleteAssetIfExists($"{GetCharacterArtFolder(characterId)}/Attack");
            DeleteAssetIfExists($"{GetCharacterArtFolder(characterId)}/Hit");
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

            var normalizedSourcePath = sourcePath.Replace('\\', '/');
            var normalizedTargetPath = targetPath.Replace('\\', '/');
            if (string.Equals(normalizedSourcePath, normalizedTargetPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            EnsureFolderPath(Path.GetDirectoryName(targetPath)?.Replace('\\', '/') ?? "Assets");
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetPath) != null)
            {
                AssetDatabase.DeleteAsset(targetPath);
            }

            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                throw new InvalidOperationException($"复制资源失败：{sourcePath} -> {targetPath}");
            }
        }

        private static void DeleteAssetIfExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null || AssetDatabase.IsValidFolder(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
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
                RemoveMissingScriptsRecursively(prefabRoot);

                var view = prefabRoot.GetComponent<CombatantView>() ?? prefabRoot.AddComponent<CombatantView>();
                var visualRoot = EnsureChild(prefabRoot.transform, "VisualRoot");
                visualRoot.localPosition = new Vector3(visualRoot.localPosition.x, VisualRootY, visualRoot.localPosition.z);
                var bodySpriteTransform = EnsureChild(visualRoot, "BodySprite");
                var bodyRenderer = EnsureSpriteRenderer(bodySpriteTransform, 2);
                bodyRenderer.sprite = combatant.idleAnimationFrames != null && combatant.idleAnimationFrames.Length > 0 ? combatant.idleAnimationFrames[0] : combatant.battleSprite;
                bodyRenderer.color = combatant.tint;
                var bodyOffsetDeltaY = NormalizeBodySpriteBaseline(bodySpriteTransform, bodyRenderer.sprite);

                var actionOverlayTransform = visualRoot.Find("ActionOverlay");
                var actionOverlayWasMissing = actionOverlayTransform == null;
                actionOverlayTransform = actionOverlayWasMissing ? EnsureChild(visualRoot, "ActionOverlay") : actionOverlayTransform;
                var actionOverlay = EnsureSpriteRenderer(actionOverlayTransform, 80);
                actionOverlay.enabled = false;
                if (!prefabExists || actionOverlayWasMissing)
                {
                    actionOverlayTransform.localPosition = GetDefaultOverlayPosition(combatant, bodySpriteTransform.localPosition.y);
                }
                else if (Mathf.Abs(bodyOffsetDeltaY) > 0.0001f)
                {
                    actionOverlayTransform.localPosition += new Vector3(0f, bodyOffsetDeltaY, 0f);
                }
                if (!prefabExists || actionOverlayWasMissing)
                {
                    actionOverlayTransform.localScale = GetDefaultOverlayScale(combatant);
                }

                var hitOverlayTransform = visualRoot.Find("HitOverlay");
                var hitOverlayWasMissing = hitOverlayTransform == null;
                hitOverlayTransform = hitOverlayWasMissing ? EnsureChild(visualRoot, "HitOverlay") : hitOverlayTransform;
                var hitOverlay = EnsureSpriteRenderer(hitOverlayTransform, 81);
                hitOverlay.enabled = false;
                if (!prefabExists || hitOverlayWasMissing)
                {
                    hitOverlayTransform.localPosition = actionOverlayTransform.localPosition;
                }
                else if (Mathf.Abs(bodyOffsetDeltaY) > 0.0001f)
                {
                    hitOverlayTransform.localPosition += new Vector3(0f, bodyOffsetDeltaY, 0f);
                }
                if (!prefabExists || hitOverlayWasMissing)
                {
                    hitOverlayTransform.localScale = actionOverlayTransform.localScale;
                }

                var nameplatePosition = EnsureChild(prefabRoot.transform, "NameplatePosition");
                nameplatePosition.localPosition = new Vector3(0f, NameplatePositionY, 0f);

                var colliderTransform = EnsureChild(prefabRoot.transform, "Collider");
                RemoveMissingScriptsRecursively(colliderTransform.gameObject);
                var clickCollider = EnsureBoxCollider2D(colliderTransform);
                if (!colliderTransform.TryGetComponent<CombatantClickProxy>(out var clickProxy) || clickProxy == null)
                {
                    clickProxy = colliderTransform.gameObject.AddComponent<CombatantClickProxy>();
                }

                BindCombatantViewReferences(view, visualRoot, bodyRenderer, actionOverlay, hitOverlay, nameplatePosition, clickCollider);
                CombatUnitFeaturePatchRegistry.ApplyAll(prefabRoot, combatant);
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

        private static void RefreshCombatantPrefabsOnDisk()
        {
            EnsureFolderPath(CombatantsPath);
            var guids = AssetDatabase.FindAssets("t:CombatantDefinition", new[] { CombatantsPath });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var combatant = AssetDatabase.LoadAssetAtPath<CombatantDefinition>(assetPath);
                if (combatant == null)
                {
                    continue;
                }

                combatant.unitPrefabPath = GetUnitPrefabPath(combatant);
                combatant.unitPrefab = CreateOrUpdateCombatUnitPrefab(combatant);
                EditorUtility.SetDirty(combatant);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void RemoveMissingScriptsRecursively(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            foreach (Transform child in root.transform)
            {
                RemoveMissingScriptsRecursively(child.gameObject);
            }
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

        private static BoxCollider2D EnsureBoxCollider2D(Transform transform)
        {
            if (transform == null)
            {
                throw new InvalidOperationException("Collider 目标节点不存在。");
            }

            if (!transform.TryGetComponent<Collider2D>(out var existingCollider) || existingCollider == null)
            {
                existingCollider = transform.gameObject.AddComponent<BoxCollider2D>();
            }

            if (existingCollider is BoxCollider2D boxCollider)
            {
                return boxCollider;
            }

            var upgradedCollider = transform.gameObject.AddComponent<BoxCollider2D>();
            if (upgradedCollider == null)
            {
                throw new InvalidOperationException($"无法在节点 {transform.name} 上创建 BoxCollider2D。");
            }

            return upgradedCollider;
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

        private void RefreshBuffs()
        {
            buffs = AssetDatabase.FindAssets("t:BuffData", new[] { BuffsPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<BuffData>)
                .Where(buff => buff != null)
                .OrderBy(buff => buff.buffName)
                .ThenBy(buff => buff.buffId)
                .ToArray();

            if (selectedBuff == null)
            {
                return;
            }

            selectedBuff = buffs.FirstOrDefault(buff => buff == selectedBuff || buff.buffId == selectedBuff.buffId);
            if (selectedBuff == null && selectedBuffEditor != null)
            {
                DestroyImmediate(selectedBuffEditor);
                selectedBuffEditor = null;
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

            if (selectedBuffEditor != null)
            {
                DestroyImmediate(selectedBuffEditor);
                selectedBuffEditor = null;
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

        private void SelectBuff(BuffData buff)
        {
            if (selectedBuff == buff)
            {
                return;
            }

            selectedBuff = buff;
            if (selectedBuffEditor != null)
            {
                DestroyImmediate(selectedBuffEditor);
                selectedBuffEditor = null;
            }
        }

        private void EnsureSelectedBuffEditor()
        {
            if (selectedBuff == null)
            {
                if (selectedBuffEditor != null)
                {
                    DestroyImmediate(selectedBuffEditor);
                    selectedBuffEditor = null;
                }

                return;
            }

            if (selectedBuffEditor == null || selectedBuffEditor.target != selectedBuff)
            {
                if (selectedBuffEditor != null)
                {
                    DestroyImmediate(selectedBuffEditor);
                }

                selectedBuffEditor = UnityEditor.Editor.CreateEditor(selectedBuff);
            }
        }

        private void DrawSkillLocalizedEditor(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            EditorGUILayout.LabelField("中文参数说明", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "基础值：技能基准强度。\n" +
                "倍率：最终强度乘数。\n" +
                "资源消耗：释放所需资源点。\n" +
                "冷却回合：释放后等待回合数。\n" +
                "施法站位：允许该技能出手的位置。\n" +
                "目标站位：允许命中的位置。",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            skill.skillId = EditorGUILayout.TextField("技能ID", skill.skillId ?? string.Empty);
            skill.skillName = EditorGUILayout.TextField("技能名", skill.skillName ?? string.Empty);
            skill.description = EditorGUILayout.TextField("描述", skill.description ?? string.Empty);
            skill.skillType = (SkillDataType)EditorGUILayout.EnumPopup("技能类型", skill.skillType);
            skill.targetType = (SkillDataTargetType)EditorGUILayout.EnumPopup("目标类型", skill.targetType);
            skill.baseValue = Mathf.Max(0, EditorGUILayout.IntField("基础值", skill.baseValue));
            skill.powerMultiplier = Mathf.Max(0.1f, EditorGUILayout.FloatField("倍率", skill.powerMultiplier));
            skill.manaCost = Mathf.Max(0, EditorGUILayout.IntField("资源消耗", skill.manaCost));
            skill.cooldown = Mathf.Max(0, EditorGUILayout.IntField("冷却回合", skill.cooldown));
            skill.skillSfx = (AudioClip)EditorGUILayout.ObjectField("技能音效", skill.skillSfx, typeof(AudioClip), false);
            skill.applyBuff = (BuffData)EditorGUILayout.ObjectField("附加Buff", skill.applyBuff, typeof(BuffData), false);
            if (skill.applyBuff != null)
            {
                skill.applyBuffDuration = Mathf.Max(1, EditorGUILayout.IntField("Buff持续回合", skill.applyBuffDuration));
            }

            DrawPositionArrayEditor("施法站位", ref skill.casterAllowedPositions);
            DrawPositionArrayEditor("目标站位", ref skill.targetAllowedPositions);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(skill);
            }
        }

        private static void DrawPositionArrayEditor(string label, ref int[] positions)
        {
            var source = (positions ?? Array.Empty<int>()).Where(position => position >= 1 && position <= 4).Distinct().ToArray();
            var flags = 0;
            foreach (var position in source)
            {
                flags |= 1 << (position - 1);
            }

            EditorGUILayout.LabelField(label);
            using (new EditorGUILayout.HorizontalScope())
            {
                var p1 = GUILayout.Toggle((flags & 1) != 0, "1", "Button", GUILayout.Width(36f));
                var p2 = GUILayout.Toggle((flags & 2) != 0, "2", "Button", GUILayout.Width(36f));
                var p3 = GUILayout.Toggle((flags & 4) != 0, "3", "Button", GUILayout.Width(36f));
                var p4 = GUILayout.Toggle((flags & 8) != 0, "4", "Button", GUILayout.Width(36f));

                var next = new List<int>(4);
                if (p1) next.Add(1);
                if (p2) next.Add(2);
                if (p3) next.Add(3);
                if (p4) next.Add(4);
                positions = next.Count > 0 ? next.ToArray() : new[] { 1, 2, 3, 4 };
            }
        }

        private void SaveSelectedSkill()
        {
            if (selectedSkill == null)
            {
                return;
            }

            EditorUtility.SetDirty(selectedSkill);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshSkills();
            SelectSkill(selectedSkill);
        }

        private void DeleteSelectedSkill()
        {
            if (selectedSkill == null)
            {
                return;
            }

            var users = GetCombatantsUsingSkill(selectedSkill).ToArray();
            if (users.Length > 0)
            {
                var message =
                    $"技能“{selectedSkill.skillName}”当前被 {users.Length} 个单位使用。\n" +
                    string.Join("\n", users.Select(combatant => $"- {combatant.displayName} [{combatant.characterId}]")) +
                    "\n\n删除前建议先解绑。是否自动解绑并删除？";
                var choice = EditorUtility.DisplayDialogComplex("删除技能", message, "解绑并删除", "取消", "仅删除技能资源");
                if (choice == 1)
                {
                    return;
                }

                if (choice == 0)
                {
                    foreach (var combatant in users)
                    {
                        if (combatant == null)
                        {
                            continue;
                        }

                        combatant.skills = (combatant.skills ?? Array.Empty<SkillData>())
                            .Where(skill => skill != selectedSkill)
                            .ToArray();
                        EditorUtility.SetDirty(combatant);
                    }
                }
            }
            else
            {
                var confirmed = EditorUtility.DisplayDialog("删除技能", $"确定删除技能“{selectedSkill.skillName}”吗？", "删除", "取消");
                if (!confirmed)
                {
                    return;
                }
            }

            var path = AssetDatabase.GetAssetPath(selectedSkill);
            SelectSkill(null);
            if (!string.IsNullOrWhiteSpace(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshSkills();
            RefreshCombatants();
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

        private static void EnsureCommonBuffAssets()
        {
            EnsureFolderPath(BuffsPath);
            CreateOrUpdateCommonBuff("bleed", "流血", "回合开始时受到流血伤害。", false, 3, new Color(0.95f, 0.22f, 0.22f), new Color(1f, 0.2f, 0.2f));
            CreateOrUpdateCommonBuff("poison", "中毒", "回合开始时受到中毒伤害。", false, 2, new Color(0.3f, 0.9f, 0.35f), new Color(0.28f, 0.95f, 0.38f));
            CreateOrUpdateCommonBuff("burn", "灼烧", "回合开始时受到灼烧伤害。", false, 4, new Color(1f, 0.55f, 0.18f), new Color(1f, 0.45f, 0.12f));
            CreateOrUpdateCommonBuff("shock", "感电", "回合开始时受到感电伤害。", false, 2, new Color(0.45f, 0.95f, 1f), new Color(0.35f, 0.9f, 1f));
            CreateOrUpdateCommonBuff("corrode", "腐蚀", "回合开始时受到腐蚀伤害。", false, 3, new Color(0.7f, 1f, 0.25f), new Color(0.8f, 1f, 0.3f));
            CreateOrUpdateCommonBuff("stun", "眩晕", "无法行动。", true, 0, new Color(1f, 0.92f, 0.3f), new Color(1f, 0.92f, 0.3f));
            AssetDatabase.SaveAssets();
        }

        private static void CreateOrUpdateCommonBuff(string buffId, string buffName, string description, bool stun, int tickDamage, Color nameColor, Color damageColor)
        {
            var assetPath = $"{BuffsPath}/{ToAssetName(buffName)}.asset";
            var buff = AssetDatabase.LoadAssetAtPath<BuffData>(assetPath);
            if (buff == null)
            {
                buff = ScriptableObject.CreateInstance<BuffData>();
                AssetDatabase.CreateAsset(buff, assetPath);
            }

            buff.buffId = buffId;
            buff.buffName = buffName;
            buff.description = description;
            buff.stun = stun;
            buff.tickDamage = tickDamage;
            buff.nameTextColor = nameColor;
            buff.damageTextColor = damageColor;
            EditorUtility.SetDirty(buff);
        }

        private bool DoesBuffMatchSearch(BuffData buff)
        {
            if (buff == null || string.IsNullOrWhiteSpace(buffSearch))
            {
                return buff != null;
            }

            return (buff.buffName ?? string.Empty).IndexOf(buffSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (buff.buffId ?? string.Empty).IndexOf(buffSearch, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private IEnumerable<SkillData> GetSkillsUsingBuff(BuffData buff)
        {
            if (buff == null)
            {
                return Enumerable.Empty<SkillData>();
            }

            return skills.Where(skill => skill != null && skill.applyBuff == buff);
        }

        private void CreateBuffAssetFromDraft()
        {
            EnsureFolderPath(BuffsPath);
            var sanitizedId = SanitizeId(createBuffId);
            if (string.IsNullOrWhiteSpace(sanitizedId))
            {
                EditorUtility.DisplayDialog("创建Buff", "Buff ID 不能为空。", "确定");
                return;
            }

            var assetPath = $"{BuffsPath}/{ToAssetName(createBuffName)}.asset";
            var buff = AssetDatabase.LoadAssetAtPath<BuffData>(assetPath);
            if (buff == null)
            {
                buff = ScriptableObject.CreateInstance<BuffData>();
                AssetDatabase.CreateAsset(buff, assetPath);
            }

            buff.buffId = sanitizedId;
            buff.buffName = string.IsNullOrWhiteSpace(createBuffName) ? sanitizedId : createBuffName.Trim();
            buff.description = createBuffDescription ?? string.Empty;
            buff.tickDamage = Mathf.Max(0, createBuffTickDamage);
            buff.stun = createBuffStun;
            buff.nameTextColor = createBuffNameColor;
            buff.damageTextColor = createBuffDamageColor;
            buff.icon = createBuffIcon;
            EditorUtility.SetDirty(buff);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshBuffs();
            SelectBuff(buff);
        }

        private void DuplicateSelectedBuff()
        {
            if (selectedBuff == null)
            {
                return;
            }

            EnsureFolderPath(BuffsPath);
            var path = AssetDatabase.GetAssetPath(selectedBuff);
            var duplicatedPath = AssetDatabase.GenerateUniqueAssetPath($"{BuffsPath}/{ToAssetName(selectedBuff.buffName)}_Copy.asset");
            if (!AssetDatabase.CopyAsset(path, duplicatedPath))
            {
                EditorUtility.DisplayDialog("复制Buff", "复制Buff资源失败。", "确定");
                return;
            }

            var duplicate = AssetDatabase.LoadAssetAtPath<BuffData>(duplicatedPath);
            if (duplicate != null)
            {
                duplicate.buffId = $"{SanitizeId(selectedBuff.buffId)}_copy";
                duplicate.buffName = $"{selectedBuff.buffName} Copy";
                EditorUtility.SetDirty(duplicate);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshBuffs();
            SelectBuff(duplicate);
        }

        private void SaveSelectedBuff()
        {
            if (selectedBuff == null)
            {
                return;
            }

            SyncBuffAssetFileName(selectedBuff);
            EditorUtility.SetDirty(selectedBuff);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshBuffs();
            SelectBuff(selectedBuff);
        }

        private static void SyncBuffAssetFileName(BuffData buff)
        {
            if (buff == null)
            {
                return;
            }

            var currentPath = AssetDatabase.GetAssetPath(buff);
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                return;
            }

            var expectedName = ToAssetName(string.IsNullOrWhiteSpace(buff.buffName) ? buff.buffId : buff.buffName);
            if (string.IsNullOrWhiteSpace(expectedName))
            {
                return;
            }

            var currentName = Path.GetFileNameWithoutExtension(currentPath);
            if (string.Equals(currentName, expectedName, StringComparison.Ordinal))
            {
                return;
            }

            var error = AssetDatabase.RenameAsset(currentPath, expectedName);
            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.LogWarning($"Failed to rename buff asset '{currentName}' to '{expectedName}': {error}");
            }
        }

        private void DeleteSelectedBuff()
        {
            if (selectedBuff == null)
            {
                return;
            }

            var users = GetSkillsUsingBuff(selectedBuff).ToArray();
            if (users.Length > 0)
            {
                var message =
                    $"Buff“{selectedBuff.buffName}”当前被 {users.Length} 个技能使用。\n" +
                    string.Join("\n", users.Select(skill => $"- {skill.skillName} [{skill.skillId}]")) +
                    "\n\n是否先清空技能引用再删除？";
                var choice = EditorUtility.DisplayDialogComplex("删除Buff", message, "清空引用并删除", "取消", "仅删除Buff资源");
                if (choice == 1)
                {
                    return;
                }

                if (choice == 0)
                {
                    foreach (var skill in users)
                    {
                        skill.applyBuff = null;
                        EditorUtility.SetDirty(skill);
                    }
                }
            }
            else
            {
                var confirmed = EditorUtility.DisplayDialog("删除Buff", $"确定删除Buff“{selectedBuff.buffName}”吗？", "删除", "取消");
                if (!confirmed)
                {
                    return;
                }
            }

            var path = AssetDatabase.GetAssetPath(selectedBuff);
            SelectBuff(null);
            if (!string.IsNullOrWhiteSpace(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshBuffs();
            RefreshSkills();
        }

        private static string GetPassiveDisplayName(HeroPassive passive)
        {
            return HeroProgressionDescriptions.GetPassiveDisplayName(passive);
        }

        private static string GetPassiveRuleSummary(HeroPassive passive)
        {
            return HeroProgressionDescriptions.GetPassiveDescription(passive);
        }

        private static string GetPassiveDesignNote(HeroPassive passive)
        {
            return HeroProgressionDescriptions.GetPassiveDesignNote(passive);
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

        private void ImportTemplateFromSelection()
        {
            var selected = Selection.activeObject as CombatantDefinition;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("导入模板", "当前选中的对象不是 CombatantDefinition。", "确定");
                return;
            }

            ImportTemplateToCreateForm(selected);
        }

        private void ImportTemplateToCreateForm(CombatantDefinition template)
        {
            if (template == null)
            {
                EditorUtility.DisplayDialog("导入模板", "请先指定一个模板单位。", "确定");
                return;
            }

            if (createTemplateHeroesOnly && !template.isHero)
            {
                EditorUtility.DisplayDialog("导入模板", "当前启用了“仅英雄模板”，请选择英雄单位。", "确定");
                return;
            }

            createTemplateCombatant = template;
            createCharacterId = template.characterId;
            createDisplayName = template.displayName;
            createIsHero = template.isHero;
            createStartUnlocked = template.isUnlocked;
            createRecruitPrice = template.recruitPrice;
            createMaxHealth = template.maxHealth;
            createSpeed = template.speed;
            createAttack = template.attack;
            createDefense = template.defense;
            createCorpseHealth = template.corpseHealth;
            createOccupiedSlotCount = template.occupiedSlotCount;
            createVisualScale = template.visualScale;
            createTint = template.tint;
            createArchetype = template.archetype;
            createPreferredRow = template.preferredRow;
            createSpecialization = template.specialization;
            createPassive = template.passive;
            createGrowthPassive = template.growthPassive;
            createGrowthData = template.growthData != null ? template.growthData : LoadOrCreateDefaultGrowthData();

            createSkills.Clear();
            if (template.skills != null && template.skills.Length > 0)
            {
                createSkills.AddRange(template.skills.Where(skill => skill != null));
            }

            createIdleFrames.Clear();
            createIdleSourceAsset = ResolveTemplateIdleSourceAsset(template);
            createAttackSprite = ResolveTemplateOverlaySprite(template.characterId, "default_attack");
            createHitSprite = ResolveTemplateOverlaySprite(template.characterId, "default_hit");
        }

        private static UnityEngine.Object ResolveTemplateIdleSourceAsset(CombatantDefinition template)
        {
            if (template == null || template.idleAnimationFrames == null || template.idleAnimationFrames.Length == 0)
            {
                return null;
            }

            var firstSprite = template.idleAnimationFrames.FirstOrDefault(sprite => sprite != null);
            if (firstSprite == null)
            {
                return null;
            }

            var spritePath = AssetDatabase.GetAssetPath(firstSprite);
            if (string.IsNullOrWhiteSpace(spritePath))
            {
                return null;
            }

            return AssetDatabase.LoadMainAssetAtPath(spritePath);
        }

        private static Sprite ResolveTemplateOverlaySprite(string characterId, string basenameWithoutExtension)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(basenameWithoutExtension))
            {
                return null;
            }

            var folder = GetCharacterSpecificVfxFolder(characterId);
            var guid = AssetDatabase.FindAssets($"{basenameWithoutExtension} t:Sprite", new[] { folder }).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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

        private static Vector3 GetDefaultOverlayPosition(CombatantDefinition combatant, float bodyBaselineY)
        {
            var basePosition = GetDefaultOverlayPosition(combatant);
            return new Vector3(basePosition.x, basePosition.y + bodyBaselineY, basePosition.z);
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

        private static float NormalizeBodySpriteBaseline(Transform bodySpriteTransform, Sprite sprite)
        {
            if (bodySpriteTransform == null || sprite == null)
            {
                return 0f;
            }

            var localPosition = bodySpriteTransform.localPosition;
            var desiredY = -sprite.bounds.min.y * bodySpriteTransform.localScale.y;
            var deltaY = desiredY - localPosition.y;
            bodySpriteTransform.localPosition = new Vector3(localPosition.x, desiredY, localPosition.z);
            return deltaY;
        }
    }
}
