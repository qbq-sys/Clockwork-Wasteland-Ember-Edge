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
        private string createSkillName = "New Skill";
        private string createSkillDescription = "A combat action.";
        private SkillDataType createSkillType = SkillDataType.伤害;
        private SkillDataTargetType createSkillTargetType = SkillDataTargetType.单敌;
        private int createSkillBaseValue = 8;
        private float createSkillPowerMultiplier = 1f;
        private int createSkillManaCost;
        private int createSkillCooldown;

        private string createCharacterId = "new_unit";
        private string createDisplayName = "New Unit";
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
        private Sprite createAttackSprite;
        private Sprite createHitSprite;

        private Sprite editAttackSprite;
        private Sprite editHitSprite;

        [MenuItem("Clockwork Wasteland/Tools/Combat Content Manager")]
        public static void Open()
        {
            var window = GetWindow<CombatantManagerWindow>("Combat Content Manager");
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
                tabIndex = GUILayout.Toolbar(tabIndex, new[] { "Units", "Create Unit", "Skills", "Passives" }, EditorStyles.toolbarButton, GUILayout.Width(360f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(90f)))
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
                EditorGUILayout.LabelField("Combatants", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Select an existing unit to inspect its definition, ping its prefab, rebuild its combat prefab, or replace visuals.", MessageType.None);

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

                        GUILayout.Label(combatant.isHero ? "Hero" : "Enemy", GUILayout.Width(48f));
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
                    EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Choose a unit on the left.", MessageType.Info);
                    return;
                }

                EditorGUILayout.LabelField(selectedCombatant.displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Character ID", selectedCombatant.characterId);
                EditorGUILayout.LabelField("Prefab Path", GetUnitPrefabPath(selectedCombatant));
                EditorGUILayout.LabelField("VFX Folder", GetCharacterSpecificVfxFolder(selectedCombatant.characterId));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Ping Definition", GUILayout.Width(120f)))
                    {
                        EditorGUIUtility.PingObject(selectedCombatant);
                        Selection.activeObject = selectedCombatant;
                    }

                    if (GUILayout.Button("Ping Prefab", GUILayout.Width(120f)))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<CombatantView>(GetUnitPrefabPath(selectedCombatant));
                        if (prefab != null)
                        {
                            EditorGUIUtility.PingObject(prefab);
                            Selection.activeObject = prefab;
                        }
                    }

                    if (GUILayout.Button("Rebuild Prefab", GUILayout.Width(120f)))
                    {
                        RebuildCombatantPrefab(selectedCombatant);
                    }
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Replace Visuals", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Idle frames update portrait/battle sprite and prefab preview. Attack/Hit sprites are copied to the character-specific VFX folder as default overlays.", MessageType.None);

                DrawSpriteList("Idle Frames", editIdleFrames, allowCollectSelection: true);
                editAttackSprite = (Sprite)EditorGUILayout.ObjectField("Default Attack Sprite", editAttackSprite, typeof(Sprite), false);
                editHitSprite = (Sprite)EditorGUILayout.ObjectField("Default Hit Sprite", editHitSprite, typeof(Sprite), false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Collect Sprites From Selection", GUILayout.Width(220f)))
                    {
                        ReplaceSpriteListFromSelection(editIdleFrames);
                    }

                    if (GUILayout.Button("Apply Visuals To Selected Unit", GUILayout.Width(220f)))
                    {
                        ApplyVisualsToExistingCombatant(selectedCombatant);
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
            EditorGUILayout.LabelField("Create New Unit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Drag in cut idle frames plus default attack/hit sprites. The tool will create a CombatantDefinition, combat prefab, and character-specific VFX assets.", MessageType.None);

            createDisplayName = EditorGUILayout.TextField("Display Name", createDisplayName);
            createCharacterId = EditorGUILayout.TextField("Character ID", createCharacterId);
            createIsHero = EditorGUILayout.Toggle("Is Hero", createIsHero);
            createStartUnlocked = EditorGUILayout.Toggle("Start Unlocked", createStartUnlocked);
            createRecruitPrice = EditorGUILayout.IntField("Recruit Price", createRecruitPrice);
            createMaxHealth = EditorGUILayout.IntField("Max Health", createMaxHealth);
            createSpeed = EditorGUILayout.IntField("Speed", createSpeed);
            createAttack = EditorGUILayout.IntField("Attack", createAttack);
            createDefense = EditorGUILayout.IntField("Defense", createDefense);
            createCorpseHealth = EditorGUILayout.IntField("Corpse Health", createCorpseHealth);
            createOccupiedSlotCount = EditorGUILayout.IntField("Occupied Slot Count", createOccupiedSlotCount);
            createVisualScale = EditorGUILayout.FloatField("Visual Scale", createVisualScale);
            createTint = EditorGUILayout.ColorField("Tint", createTint);
            createArchetype = (CombatArchetype)EditorGUILayout.EnumPopup("Archetype", createArchetype);
            createPreferredRow = (CombatRowPreference)EditorGUILayout.EnumPopup("Preferred Row", createPreferredRow);
            createSpecialization = (CombatSpecialization)EditorGUILayout.EnumPopup("Specialization", createSpecialization);
            createGrowthData = (HeroGrowthData)EditorGUILayout.ObjectField("Growth Data", createGrowthData, typeof(HeroGrowthData), false);

            EditorGUILayout.Space(6f);
            DrawSkillList();
            EditorGUILayout.Space(6f);
            DrawSpriteList("Idle Frames", createIdleFrames, allowCollectSelection: true);

            createAttackSprite = (Sprite)EditorGUILayout.ObjectField("Default Attack Sprite", createAttackSprite, typeof(Sprite), false);
            createHitSprite = (Sprite)EditorGUILayout.ObjectField("Default Hit Sprite", createHitSprite, typeof(Sprite), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Collect Idle Frames From Selection", GUILayout.Width(220f)))
                {
                    ReplaceSpriteListFromSelection(createIdleFrames);
                }

                if (GUILayout.Button("Add Selected Skills", GUILayout.Width(180f)))
                {
                    AddSelectedSkills(createSkills);
                }
            }

            EditorGUILayout.Space(10f);
            if (GUILayout.Button("Create / Update Unit", GUILayout.Height(34f)))
            {
                CreateOrUpdateCombatant();
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
                EditorGUILayout.LabelField("Skills", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Create new skills, inspect existing assets, and see which units currently use them.", MessageType.None);
                skillSearch = EditorGUILayout.TextField("Search", skillSearch);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Skill", GUILayout.Width(140f)))
                    {
                        CreateSkillAssetFromDraft();
                    }

                    if (GUILayout.Button("Duplicate Selected", GUILayout.Width(160f)))
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
                EditorGUILayout.LabelField("New Skill Draft", EditorStyles.boldLabel);
                createSkillId = EditorGUILayout.TextField("Skill ID", createSkillId);
                createSkillName = EditorGUILayout.TextField("Display Name", createSkillName);
                createSkillDescription = EditorGUILayout.TextField("Description", createSkillDescription);
                createSkillType = (SkillDataType)EditorGUILayout.EnumPopup("Type", createSkillType);
                createSkillTargetType = (SkillDataTargetType)EditorGUILayout.EnumPopup("Target", createSkillTargetType);
                createSkillBaseValue = EditorGUILayout.IntField("Base Value", createSkillBaseValue);
                createSkillPowerMultiplier = EditorGUILayout.FloatField("Power Multiplier", createSkillPowerMultiplier);
                createSkillManaCost = EditorGUILayout.IntField("Mana Cost", createSkillManaCost);
                createSkillCooldown = EditorGUILayout.IntField("Cooldown", createSkillCooldown);
            }
        }

        private void DrawSkillDetailsPane()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (selectedSkill == null)
                {
                    EditorGUILayout.LabelField("Skill Details", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Choose a skill on the left.", MessageType.Info);
                    return;
                }

                EditorGUILayout.LabelField(selectedSkill.skillName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Skill ID", selectedSkill.skillId);
                EditorGUILayout.LabelField("Asset Path", AssetDatabase.GetAssetPath(selectedSkill));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Ping Skill", GUILayout.Width(120f)))
                    {
                        EditorGUIUtility.PingObject(selectedSkill);
                        Selection.activeObject = selectedSkill;
                    }

                    if (GUILayout.Button("Assign To Selected Unit", GUILayout.Width(180f)))
                    {
                        AssignSelectedSkillToSelectedCombatant();
                    }
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Used By", EditorStyles.boldLabel);
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
                    EditorGUILayout.HelpBox("No unit is currently using this skill.", MessageType.None);
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
                EditorGUILayout.LabelField("Passives", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Inspect current passive rules and see which heroes are using them.", MessageType.None);
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

                EditorGUILayout.LabelField("Assigned Heroes", EditorStyles.boldLabel);
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
                    EditorGUILayout.HelpBox("No hero is currently assigned to this passive.", MessageType.None);
                }

                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("Notes", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(GetPassiveDesignNote(selectedPassive), MessageType.None);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSkillList()
        {
            EditorGUILayout.LabelField("Skills", EditorStyles.boldLabel);
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

            if (GUILayout.Button("Add Skill Slot", GUILayout.Width(120f)))
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
                if (GUILayout.Button("Add Sprite Slot", GUILayout.Width(120f)))
                {
                    sprites.Add(null);
                }

                if (allowCollectSelection && GUILayout.Button("Replace From Selection", GUILayout.Width(160f)))
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
                EditorUtility.DisplayDialog("Create Unit", "Character ID is required.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(createDisplayName))
            {
                EditorUtility.DisplayDialog("Create Unit", "Display Name is required.", "OK");
                return;
            }

            var idleFrames = createIdleFrames.Where(sprite => sprite != null).ToArray();
            if (idleFrames.Length == 0)
            {
                EditorUtility.DisplayDialog("Create Unit", "At least one idle frame is required.", "OK");
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

            CopyDefaultCharacterSpecificVfx(characterId, createAttackSprite, createHitSprite);
            combatant.unitPrefabPath = GetUnitPrefabPath(combatant);
            combatant.unitPrefab = CreateOrUpdateCombatUnitPrefab(combatant);

            EditorUtility.SetDirty(combatant);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshCombatants();
            SelectCombatant(combatant);
            EditorUtility.DisplayDialog("Create Unit", $"Created/updated {combatant.displayName}.", "OK");
        }

        private void ApplyVisualsToExistingCombatant(CombatantDefinition combatant)
        {
            if (combatant == null)
            {
                return;
            }

            var idleFrames = editIdleFrames.Where(sprite => sprite != null).ToArray();
            if (idleFrames.Length > 0)
            {
                combatant.idleAnimationFrames = idleFrames;
                combatant.battleSprite = idleFrames[0];
                combatant.portrait = idleFrames[0];
            }

            CopyDefaultCharacterSpecificVfx(combatant.characterId, editAttackSprite, editHitSprite);
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

        private static void CopySpriteAsset(Sprite sprite, string targetPath)
        {
            var sourcePath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return;
            }

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
                var bodyRenderer = bodySpriteTransform.GetComponent<SpriteRenderer>() ?? bodySpriteTransform.gameObject.AddComponent<SpriteRenderer>();
                bodyRenderer.sortingOrder = bodyRenderer.sortingOrder == 0 ? 2 : bodyRenderer.sortingOrder;
                bodyRenderer.sprite = combatant.idleAnimationFrames != null && combatant.idleAnimationFrames.Length > 0 ? combatant.idleAnimationFrames[0] : combatant.battleSprite;
                bodyRenderer.color = combatant.tint;

                var actionOverlayTransform = EnsureChild(visualRoot, "ActionOverlay");
                var actionOverlay = actionOverlayTransform.GetComponent<SpriteRenderer>() ?? actionOverlayTransform.gameObject.AddComponent<SpriteRenderer>();
                actionOverlay.sortingOrder = actionOverlay.sortingOrder == 0 ? 80 : actionOverlay.sortingOrder;
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
                var hitOverlay = hitOverlayTransform.GetComponent<SpriteRenderer>() ?? hitOverlayTransform.gameObject.AddComponent<SpriteRenderer>();
                hitOverlay.sortingOrder = hitOverlay.sortingOrder == 0 ? 81 : hitOverlay.sortingOrder;
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
                    nameplatePosition.localPosition = new Vector3(0f, -0.24f, 0f);
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
            serializedView.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            serializedView.FindProperty("spriteRenderer").objectReferenceValue = bodyRenderer;
            serializedView.FindProperty("overlayRenderer").objectReferenceValue = actionOverlay;
            serializedView.FindProperty("hitOverlayRenderer").objectReferenceValue = hitOverlay;
            serializedView.FindProperty("nameplatePosition").objectReferenceValue = nameplatePosition;
            serializedView.FindProperty("clickCollider").objectReferenceValue = clickCollider;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
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
                EditorUtility.DisplayDialog("Create Skill", "Skill ID is required.", "OK");
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
                EditorUtility.DisplayDialog("Duplicate Skill", "Failed to duplicate skill asset.", "OK");
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
            target.AddRange(Selection.objects.OfType<Sprite>().OrderBy(sprite => sprite.name));
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
