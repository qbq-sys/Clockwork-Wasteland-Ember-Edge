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
        private const string GrowthsPath = Root + "/Data/Growth";
        private const string UnitPrefabsPath = Root + "/Prefabs/CombatUnits";
        private const string CharacterSpecificVfxRoot = "Assets/Art/VFX/Combat/CharacterSpecific";

        private readonly List<Sprite> createIdleFrames = new List<Sprite>();
        private readonly List<SkillData> createSkills = new List<SkillData>();
        private readonly List<Sprite> editIdleFrames = new List<Sprite>();

        private CombatantDefinition[] combatants = Array.Empty<CombatantDefinition>();
        private CombatantDefinition selectedCombatant;
        private UnityEditor.Editor selectedCombatantEditor;
        private Vector2 listScroll;
        private Vector2 detailsScroll;
        private Vector2 createScroll;
        private int tabIndex;

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
        private HeroGrowthData createGrowthData;
        private Sprite createAttackSprite;
        private Sprite createHitSprite;

        private Sprite editAttackSprite;
        private Sprite editHitSprite;

        [MenuItem("Clockwork Wasteland/Combatant Manager")]
        public static void Open()
        {
            var window = GetWindow<CombatantManagerWindow>("Combatant Manager");
            window.minSize = new Vector2(1180f, 700f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshCombatants();
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
                default:
                    DrawCreateUnitTab();
                    break;
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                tabIndex = GUILayout.Toolbar(tabIndex, new[] { "Existing Units", "Create Unit" }, EditorStyles.toolbarButton, GUILayout.Width(220f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                {
                    RefreshCombatants();
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
