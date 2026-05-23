using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClockworkWasteland.Combat;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ClockworkWasteland.Editor
{
    public sealed class AdventureMapManagerWindow : EditorWindow
    {
        private enum CombatantFilterMode
        {
            All,
            EnemiesOnly,
            HeroesOnly
        }

        private const string CatalogAssetPath = "Assets/Resources/Adventure/AdventureMapCatalog.asset";
        private const string MapsFolderPath = "Assets/Resources/Adventure/Maps";
        private const string PresetsFolderPath = "Assets/Resources/Adventure/Presets";

        private AdventureMapCatalog catalog;
        private Vector2 mapListScroll;
        private Vector2 detailScroll;
        private Vector2 combatantLibraryScroll;
        private string mapSearchText = string.Empty;
        private AdventureMapUnlockType mapUnlockFilter;
        private bool useUnlockFilter;
        private int selectedMapIndex = -1;
        private int selectedBattleIndex = -1;
        private ReorderableList mapReorderableList;
        private ReorderableList battleList;
        private CombatantFilterMode combatantFilterMode = CombatantFilterMode.EnemiesOnly;
        private string combatantSearchText = string.Empty;
        private CombatantDefinition selectedLibraryCombatant;
        private EnemyFormationPresetData selectedPreset;

        [MenuItem("Clockwork Wasteland/\u5730\u56fe\u6218\u6597\u5185\u5bb9\u7ba1\u7406\u5668")]
        private static void OpenWindow()
        {
            var window = GetWindow<AdventureMapManagerWindow>("\u5730\u56fe\u6218\u6597\u5185\u5bb9\u7ba1\u7406\u5668");
            window.minSize = new Vector2(1480f, 860f);
            window.Show();
        }

        private void OnEnable()
        {
            catalog = AdventureMapEditorAssetUtility.EnsureAdventureMapAssets();
            EnsureSelectionState();
            RebuildMapList();
            RebuildBattleList();
        }

        private void OnGUI()
        {
            if (catalog == null)
            {
                catalog = AdventureMapEditorAssetUtility.LoadAdventureMapCatalog();
                if (catalog == null)
                {
                    catalog = AdventureMapEditorAssetUtility.EnsureAdventureMapAssets();
                    RebuildMapList();
                    RebuildBattleList();
                }
            }

            EnsureSelectionState();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            DrawMapListPanel();
            DrawMapDetailPanel();
            EditorGUILayout.EndHorizontal();

            DrawBottomToolbar();

            if (EditorGUI.EndChangeCheck())
            {
                MarkDirty(catalog);
                MarkDirty(GetSelectedMap());
            }
        }

        private void DrawMapListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(360f));
            EditorGUILayout.LabelField("\u5730\u56fe\u5217\u8868", EditorStyles.boldLabel);
            mapSearchText = EditorGUILayout.TextField("\u641c\u7d22\u5730\u56fe", mapSearchText);
            useUnlockFilter = EditorGUILayout.Toggle("\u6309\u89e3\u9501\u7c7b\u578b\u7b5b\u9009", useUnlockFilter);
            if (useUnlockFilter)
            {
                mapUnlockFilter = (AdventureMapUnlockType)EditorGUILayout.Popup("\u89e3\u9501\u7c7b\u578b", (int)mapUnlockFilter, new[] { "\u9ed8\u8ba4\u89e3\u9501", "\u901a\u5173\u524d\u7f6e\u5730\u56fe" });
            }

            var useFilteredView = !string.IsNullOrWhiteSpace(mapSearchText) || useUnlockFilter;
            mapListScroll = EditorGUILayout.BeginScrollView(mapListScroll, "box");
            if (!useFilteredView)
            {
                mapReorderableList?.DoLayoutList();
            }
            else
            {
                var maps = GetFilteredMaps();
                for (var i = 0; i < maps.Count; i++)
                {
                    var map = maps[i];
                    if (map == null)
                    {
                        continue;
                    }

                    var sourceIndex = catalog.maps.IndexOf(map);
                    var buttonStyle = sourceIndex == selectedMapIndex ? EditorStyles.toolbarButton : EditorStyles.miniButton;
                    if (GUILayout.Button($"{map.displayName}  [{map.mapId}]", buttonStyle, GUILayout.Height(32f)))
                    {
                        selectedMapIndex = sourceIndex;
                        selectedBattleIndex = 0;
                        RebuildBattleList();
                        GUI.FocusControl(null);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("新增地图", GUILayout.Height(28f)))
                {
                    CreateMapAsset();
                }

                GUI.enabled = GetSelectedMap() != null;
                if (GUILayout.Button("删除地图", GUILayout.Height(28f)))
                {
                    DeleteSelectedMap();
                }

                GUI.enabled = true;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMapDetailPanel()
        {
            EditorGUILayout.BeginVertical("box");
            var map = GetSelectedMap();
            if (map == null)
            {
                EditorGUILayout.HelpBox("\u8bf7\u5148\u5728\u5de6\u4fa7\u9009\u62e9\u6216\u521b\u5efa\u4e00\u5f20\u5730\u56fe\u3002", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
            EditorGUILayout.LabelField("地图详情", EditorStyles.boldLabel);
            map.mapId = EditorGUILayout.TextField("地图ID", map.mapId);
            map.displayName = EditorGUILayout.TextField("地图名称", map.displayName);
            EditorGUILayout.LabelField("地图描述");
            map.description = EditorGUILayout.TextArea(map.description, GUILayout.MinHeight(56f));
            map.unlockType = (AdventureMapUnlockType)EditorGUILayout.Popup("解锁类型", (int)map.unlockType, new[] { "默认解锁", "通关前置地图" });
            if (map.unlockType == AdventureMapUnlockType.ClearPrerequisiteMap)
            {
                map.prerequisiteMap = (AdventureMapData)EditorGUILayout.ObjectField("前置地图", map.prerequisiteMap, typeof(AdventureMapData), false);
            }

            EditorGUILayout.LabelField("解锁说明");
            map.unlockDescription = EditorGUILayout.TextArea(map.unlockDescription, GUILayout.MinHeight(40f));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("背景配置", EditorStyles.boldLabel);
            map.backgroundSprite = (Sprite)EditorGUILayout.ObjectField("背景图片", map.backgroundSprite, typeof(Sprite), false);
            DrawSpritePreview(map.backgroundSprite, 260f);
            map.backgroundOffset.x = EditorGUILayout.Slider("背景偏移 X", map.backgroundOffset.x, -10f, 10f);
            map.backgroundOffset.y = EditorGUILayout.Slider("背景偏移 Y", map.backgroundOffset.y, -10f, 10f);
            map.backgroundScale = EditorGUILayout.Slider("背景缩放", map.backgroundScale, 0.2f, 3f);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("战斗列表", EditorStyles.boldLabel);
            battleList?.DoLayoutList();
            DrawSelectedBattleEditor(map);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedBattleEditor(AdventureMapData map)
        {
            if (map == null)
            {
                return;
            }

            var battle = GetSelectedBattle(map);
            if (battle == null)
            {
                EditorGUILayout.HelpBox("\u8be5\u5730\u56fe\u8fd8\u6ca1\u6709\u6218\u6597\u3002\u8bf7\u5148\u65b0\u589e\u4e00\u573a\u6218\u6597\u3002", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("战斗详情", EditorStyles.boldLabel);
            battle.battleId = EditorGUILayout.TextField("战斗ID", battle.battleId);
            battle.displayName = EditorGUILayout.TextField("战斗名称", battle.displayName);
            EditorGUILayout.LabelField("战斗描述");
            battle.description = EditorGUILayout.TextArea(battle.description, GUILayout.MinHeight(48f));
            battle.isBossBattle = EditorGUILayout.Toggle("\u662f\u5426Boss\u6218", battle.isBossBattle);
            battle.rewardDescription = EditorGUILayout.TextField("奖励说明", battle.rewardDescription);
            using (new EditorGUILayout.HorizontalScope())
            {
                battle.goldRewardMin = EditorGUILayout.IntField("\u91d1\u5e01\u6700\u5c0f\u503c", battle.goldRewardMin);
                battle.goldRewardMax = EditorGUILayout.IntField("\u91d1\u5e01\u6700\u5927\u503c", battle.goldRewardMax);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                battle.experienceRewardMin = EditorGUILayout.IntField("\u7ecf\u9a8c\u6700\u5c0f\u503c", battle.experienceRewardMin);
                battle.experienceRewardMax = EditorGUILayout.IntField("\u7ecf\u9a8c\u6700\u5927\u503c", battle.experienceRewardMax);
            }

            battle.backgroundOverride = (Sprite)EditorGUILayout.ObjectField("覆盖战斗背景", battle.backgroundOverride, typeof(Sprite), false);
            if (battle.backgroundOverride != null)
            {
                DrawSpritePreview(battle.backgroundOverride, 180f);
                battle.backgroundOffset.x = EditorGUILayout.Slider("战斗背景偏移 X", battle.backgroundOffset.x, -10f, 10f);
                battle.backgroundOffset.y = EditorGUILayout.Slider("战斗背景偏移 Y", battle.backgroundOffset.y, -10f, 10f);
                battle.backgroundScale = EditorGUILayout.Slider("战斗背景缩放", battle.backgroundScale, 0.2f, 3f);
            }

            EditorGUILayout.Space(8f);
            DrawFormationEditor(battle);
        }

        private void DrawFormationEditor(AdventureBattleConfig battle)
        {
            EditorGUILayout.LabelField("敌方阵容配置", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawCombatantLibrary();
                DrawFormationSlots(battle);
            }

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                selectedPreset = (EnemyFormationPresetData)EditorGUILayout.ObjectField("阵容预设", selectedPreset, typeof(EnemyFormationPresetData), false);
                if (GUILayout.Button("\u4ece\u9884\u8bbe\u8f7d\u5165", GUILayout.Width(120f)))
                {
                    LoadSelectedPresetIntoBattle(battle);
                }

                if (GUILayout.Button("\u4fdd\u5b58\u4e3a\u9884\u8bbe", GUILayout.Width(120f)))
                {
                    SaveBattleAsPreset(battle);
                }
            }
        }

        private void DrawCombatantLibrary()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(360f), GUILayout.MinHeight(360f));
            EditorGUILayout.LabelField("\u53ef\u7528\u5355\u4f4d\u5e93", EditorStyles.boldLabel);
            combatantFilterMode = (CombatantFilterMode)EditorGUILayout.Popup(
                "\u5355\u4f4d\u7b5b\u9009",
                (int)combatantFilterMode,
                new[] { "全部", "仅敌人", "仅英雄" });
            combatantSearchText = EditorGUILayout.TextField("搜索单位", combatantSearchText);

            combatantLibraryScroll = EditorGUILayout.BeginScrollView(combatantLibraryScroll);
            foreach (var combatant in LoadCombatantLibrary())
            {
                if (combatant == null)
                {
                    continue;
                }

                var rect = EditorGUILayout.GetControlRect(false, 34f);
                var label = $"{combatant.displayName}  [{combatant.characterId}]";
                if (GUI.Button(rect, label, selectedLibraryCombatant == combatant ? EditorStyles.toolbarButton : EditorStyles.miniButton))
                {
                    selectedLibraryCombatant = combatant;
                }

                HandleCombatantDrag(rect, combatant);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("\u652f\u6301\u62d6\u62fd\u5230\u53f3\u4fa7\u7ad9\u4f4d\uff0c\u4e5f\u652f\u6301\u5148\u70b9\u9009\u5355\u4f4d\u518d\u70b9\u7ad9\u4f4d\u8fdb\u884c\u653e\u7f6e\u3002", MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private void DrawFormationSlots(AdventureBattleConfig battle)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.MinHeight(360f));
            EditorGUILayout.LabelField("站位编辑", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("\u70b9\u51fb\u7ad9\u4f4d\u53ef\u653e\u5165\u5f53\u524d\u9009\u4e2d\u5355\u4f4d\uff1b\u70b9\u51fb\u5df2\u653e\u5355\u4f4d\u53ef\u8c03\u6574\u7b49\u7ea7\u6216\u79fb\u9664\u3002", EditorStyles.miniLabel);

            for (var row = 0; row < 2; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (var column = 0; column < 2; column++)
                    {
                        var slot = row * 2 + column + 1;
                        var placement = GetPlacement(battle, slot);
                        var rect = GUILayoutUtility.GetRect(220f, 150f, GUILayout.Width(220f), GUILayout.Height(150f));
                        GUI.Box(rect, GUIContent.none);

                        var titleRect = new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 20f);
                        EditorGUI.LabelField(titleRect, $"站位 {slot}", EditorStyles.boldLabel);

                        var dropRect = new Rect(rect.x + 8f, rect.y + 32f, rect.width - 16f, rect.height - 68f);
                        GUI.Box(dropRect, placement != null && placement.combatant != null
                            ? $"{placement.combatant.displayName}\n等级 {placement.level}"
                            : "\u62d6\u62fd\u5355\u4f4d\u5230\u6b64\u5904");

                        HandleSlotDrop(dropRect, battle, slot);

                        var buttonRect = new Rect(rect.x + 8f, rect.yMax - 30f, rect.width - 16f, 22f);
                        if (placement == null || placement.combatant == null)
                        {
                            if (GUI.Button(buttonRect, "放入当前选中单位") && selectedLibraryCombatant != null)
                            {
                                SetPlacement(battle, slot, selectedLibraryCombatant, 1);
                            }
                        }
                        else
                        {
                            var levelRect = new Rect(rect.x + 8f, rect.yMax - 56f, rect.width - 16f, 20f);
                            placement.level = Mathf.Max(1, EditorGUI.IntField(levelRect, "等级", placement.level));
                            if (GUI.Button(buttonRect, "\u79fb\u9664\u8be5\u5355\u4f4d"))
                            {
                                battle.enemies.Remove(placement);
                            }
                        }
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBottomToolbar()
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("\u4fdd\u5b58\u5168\u90e8\u5730\u56fe\u914d\u7f6e", EditorStyles.toolbarButton, GUILayout.Width(140f)))
                {
                    SaveAll();
                }

                if (GUILayout.Button("\u5b9a\u4f4d\u5730\u56fe\u76ee\u5f55", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                {
                    EditorUtility.RevealInFinder(MapsFolderPath);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("\u53c2\u6570\u5df2\u4e2d\u6587\u5316\uff0c\u4fdd\u7559\u4e86\u7f3a\u7701\u56de\u9000\u63a5\u53e3\u3002", EditorStyles.miniLabel);
            }
        }

        private void RebuildBattleList()
        {
            var map = GetSelectedMap();
            if (map == null)
            {
                battleList = null;
                return;
            }

            battleList = new ReorderableList(map.battles, typeof(AdventureBattleConfig), true, true, true, true);
            battleList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "地图战斗顺序");
            battleList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var battle = index >= 0 && index < map.battles.Count ? map.battles[index] : null;
                var label = battle == null ? "\u7a7a\u6218\u6597" : $"{battle.displayName}  [{battle.battleId}]";
                EditorGUI.LabelField(rect, label);
            };
            battleList.onSelectCallback = list => selectedBattleIndex = list.index;
            battleList.onAddCallback = list =>
            {
                map.battles.Add(new AdventureBattleConfig
                {
                    battleId = $"battle_{map.battles.Count + 1:00}",
                    displayName = $"战斗 {map.battles.Count + 1}",
                    description = "\u6218\u6597\u63cf\u8ff0\u9884\u7559\u3002",
                    rewardDescription = "\u6807\u51c6\u6218\u6597\u5956\u52b1\u3002"
                });
                selectedBattleIndex = map.battles.Count - 1;
                MarkDirty(map);
            };
            battleList.onRemoveCallback = list =>
            {
                if (list.index >= 0 && list.index < map.battles.Count)
                {
                    map.battles.RemoveAt(list.index);
                    selectedBattleIndex = Mathf.Clamp(selectedBattleIndex, 0, map.battles.Count - 1);
                    MarkDirty(map);
                }
            };
            battleList.onReorderCallback = _ => MarkDirty(map);
        }

        private void RebuildMapList()
        {
            if (catalog == null)
            {
                mapReorderableList = null;
                return;
            }

            mapReorderableList = new ReorderableList(catalog.maps, typeof(AdventureMapData), true, true, false, false);
            mapReorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "地图顺序（支持拖拽排序）");
            mapReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var map = index >= 0 && index < catalog.maps.Count ? catalog.maps[index] : null;
                var label = map == null ? "\u7a7a\u5730\u56fe" : $"{map.displayName}  [{map.mapId}]";
                EditorGUI.LabelField(rect, label);
            };
            mapReorderableList.onSelectCallback = list =>
            {
                selectedMapIndex = list.index;
                selectedBattleIndex = 0;
                RebuildBattleList();
            };
            mapReorderableList.onReorderCallback = _ =>
            {
                selectedMapIndex = Mathf.Clamp(selectedMapIndex, 0, catalog.maps.Count - 1);
                MarkDirty(catalog);
            };
        }

        private void EnsureSelectionState()
        {
            if (catalog == null)
            {
                selectedMapIndex = -1;
                selectedBattleIndex = -1;
                return;
            }

            if (catalog.maps == null)
            {
                catalog.maps = new List<AdventureMapData>();
            }

            if (catalog.maps.Count == 0)
            {
                selectedMapIndex = -1;
                selectedBattleIndex = -1;
                return;
            }

            selectedMapIndex = Mathf.Clamp(selectedMapIndex < 0 ? 0 : selectedMapIndex, 0, catalog.maps.Count - 1);
            var map = GetSelectedMap();
            if (map == null)
            {
                return;
            }

            if (map.battles == null)
            {
                map.battles = new List<AdventureBattleConfig>();
            }

            selectedBattleIndex = map.battles.Count == 0
                ? -1
                : Mathf.Clamp(selectedBattleIndex < 0 ? 0 : selectedBattleIndex, 0, map.battles.Count - 1);
        }

        private AdventureMapData GetSelectedMap()
        {
            return catalog != null && selectedMapIndex >= 0 && selectedMapIndex < catalog.maps.Count
                ? catalog.maps[selectedMapIndex]
                : null;
        }

        private AdventureBattleConfig GetSelectedBattle(AdventureMapData map)
        {
            return map != null && map.battles != null && selectedBattleIndex >= 0 && selectedBattleIndex < map.battles.Count
                ? map.battles[selectedBattleIndex]
                : null;
        }

        private List<AdventureMapData> GetFilteredMaps()
        {
            if (catalog == null || catalog.maps == null)
            {
                return new List<AdventureMapData>();
            }

            return catalog.maps
                .Where(map => map != null)
                .Where(map => string.IsNullOrWhiteSpace(mapSearchText)
                    || map.displayName.IndexOf(mapSearchText, System.StringComparison.OrdinalIgnoreCase) >= 0
                    || map.mapId.IndexOf(mapSearchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(map => !useUnlockFilter || map.unlockType == mapUnlockFilter)
                .ToList();
        }

        private void MoveMap(int fromIndex, int toIndex)
        {
            if (catalog == null || fromIndex < 0 || toIndex < 0 || fromIndex >= catalog.maps.Count || toIndex >= catalog.maps.Count)
            {
                return;
            }

            var map = catalog.maps[fromIndex];
            catalog.maps.RemoveAt(fromIndex);
            catalog.maps.Insert(toIndex, map);
            selectedMapIndex = toIndex;
            MarkDirty(catalog);
        }

        private void CreateMapAsset()
        {
            Directory.CreateDirectory(MapsFolderPath);
            var map = CreateInstance<AdventureMapData>();
            map.mapId = AdventureMapEditorAssetUtility.GenerateUniqueMapId(catalog, "map");
            map.displayName = "\u65b0\u5730\u56fe";
            map.description = "\u5730\u56fe\u63cf\u8ff0\u9884\u7559\u3002";
            map.unlockType = AdventureMapUnlockType.Default;
            map.unlockDescription = "默认解锁";
            map.backgroundScale = 1f;
            map.battles = new List<AdventureBattleConfig>
            {
                new AdventureBattleConfig
                {
                    battleId = "battle_01",
                    displayName = "战斗 1",
                    description = "\u9996\u573a\u6218\u6597\u3002",
                    rewardDescription = "\u6807\u51c6\u6218\u6597\u5956\u52b1\u3002"
                }
            };

            var assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(MapsFolderPath, "新地图.asset").Replace("\\", "/"));
            AssetDatabase.CreateAsset(map, assetPath);
            catalog.maps.Add(map);
            selectedMapIndex = catalog.maps.Count - 1;
            selectedBattleIndex = 0;
            MarkDirty(catalog);
            RebuildMapList();
            RebuildBattleList();
            SaveAll();
        }

        private void DeleteSelectedMap()
        {
            var map = GetSelectedMap();
            if (map == null)
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("\u5220\u9664\u5730\u56fe", $"\u786e\u5b9a\u5220\u9664\u5730\u56fe\u201c{map.displayName}\u201d\u5417\uff1f", "\u5220\u9664", "\u53d6\u6d88"))
            {
                return;
            }

            catalog.maps.Remove(map);
            var assetPath = AssetDatabase.GetAssetPath(map);
            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            selectedMapIndex = Mathf.Clamp(selectedMapIndex, 0, catalog.maps.Count - 1);
            selectedBattleIndex = 0;
            MarkDirty(catalog);
            RebuildMapList();
            RebuildBattleList();
            SaveAll();
        }

        private IEnumerable<CombatantDefinition> LoadCombatantLibrary()
        {
            var guids = AssetDatabase.FindAssets("t:CombatantDefinition", new[] { "Assets/ClockworkWastelandDemo/Data/Combatants" });
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CombatantDefinition>)
                .Where(combatant => combatant != null)
                .Where(combatant => string.IsNullOrWhiteSpace(combatantSearchText)
                    || combatant.displayName.IndexOf(combatantSearchText, System.StringComparison.OrdinalIgnoreCase) >= 0
                    || combatant.characterId.IndexOf(combatantSearchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(combatant => combatantFilterMode == CombatantFilterMode.All
                    || (combatantFilterMode == CombatantFilterMode.EnemiesOnly && !combatant.isHero)
                    || (combatantFilterMode == CombatantFilterMode.HeroesOnly && combatant.isHero))
                .OrderBy(combatant => combatant.characterId);
        }

        private static void HandleCombatantDrag(Rect rect, CombatantDefinition combatant)
        {
            var current = Event.current;
            if (combatant == null || current == null)
            {
                return;
            }

            if (current.type == EventType.MouseDrag && rect.Contains(current.mousePosition))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { combatant };
                DragAndDrop.StartDrag(combatant.displayName);
                current.Use();
            }
        }

        private void HandleSlotDrop(Rect rect, AdventureBattleConfig battle, int slot)
        {
            var current = Event.current;
            if (current == null || !rect.Contains(current.mousePosition))
            {
                return;
            }

            if (current.type == EventType.DragUpdated)
            {
                if (DragAndDrop.objectReferences.OfType<CombatantDefinition>().Any())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    current.Use();
                }
            }
            else if (current.type == EventType.DragPerform)
            {
                var dropped = DragAndDrop.objectReferences.OfType<CombatantDefinition>().FirstOrDefault();
                if (dropped != null)
                {
                    SetPlacement(battle, slot, dropped, 1);
                    DragAndDrop.AcceptDrag();
                    current.Use();
                }
            }
        }

        private static AdventureEnemyPlacement GetPlacement(AdventureBattleConfig battle, int slot)
        {
            return battle != null && battle.enemies != null
                ? battle.enemies.FirstOrDefault(entry => entry != null && Mathf.Clamp(entry.slot, 1, 4) == slot)
                : null;
        }

        private void SetPlacement(AdventureBattleConfig battle, int slot, CombatantDefinition combatant, int level)
        {
            if (battle == null || combatant == null)
            {
                return;
            }

            var existing = GetPlacement(battle, slot);
            if (existing == null)
            {
                battle.enemies.Add(new AdventureEnemyPlacement
                {
                    combatant = combatant,
                    slot = slot,
                    level = Mathf.Max(1, level)
                });
            }
            else
            {
                existing.combatant = combatant;
                existing.slot = slot;
                existing.level = Mathf.Max(1, level);
            }

            MarkDirty(GetSelectedMap());
        }

        private void LoadSelectedPresetIntoBattle(AdventureBattleConfig battle)
        {
            if (battle == null || selectedPreset == null)
            {
                return;
            }

            battle.enemies = selectedPreset.enemies == null
                ? new List<AdventureEnemyPlacement>()
                : selectedPreset.enemies.Where(entry => entry != null).Select(entry => entry.Clone()).ToList();
            MarkDirty(GetSelectedMap());
        }

        private void SaveBattleAsPreset(AdventureBattleConfig battle)
        {
            if (battle == null)
            {
                return;
            }

            Directory.CreateDirectory(PresetsFolderPath);
            var assetPath = EditorUtility.SaveFilePanelInProject("保存阵容预设", battle.displayName, "asset", "选择阵容预设保存位置", PresetsFolderPath);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var preset = AssetDatabase.LoadAssetAtPath<EnemyFormationPresetData>(assetPath);
            if (preset == null)
            {
                preset = CreateInstance<EnemyFormationPresetData>();
                AssetDatabase.CreateAsset(preset, assetPath);
            }

            preset.presetId = Path.GetFileNameWithoutExtension(assetPath);
            preset.displayName = battle.displayName;
            preset.CopyFrom(battle.enemies);
            selectedPreset = preset;
            MarkDirty(preset);
            SaveAll();
        }

        private void SaveAll()
        {
            if (catalog == null)
            {
                return;
            }

            foreach (var map in catalog.maps.Where(map => map != null))
            {
                SyncMapAssetName(map);
                EditorUtility.SetDirty(map);
            }

            EditorUtility.SetDirty(catalog);
            if (selectedPreset != null)
            {
                EditorUtility.SetDirty(selectedPreset);
            }

            AssetDatabase.SaveAssets();
        }

        private static void SyncMapAssetName(AdventureMapData map)
        {
            if (map == null)
            {
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(map);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var desiredName = string.IsNullOrWhiteSpace(map.displayName)
                ? map.mapId
                : map.displayName.Trim();

            if (string.IsNullOrWhiteSpace(desiredName))
            {
                desiredName = "Map";
            }

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                desiredName = desiredName.Replace(invalidChar, '_');
            }

            var currentName = Path.GetFileNameWithoutExtension(assetPath);
            if (string.Equals(currentName, desiredName, System.StringComparison.Ordinal))
            {
                return;
            }

            var renameError = AssetDatabase.RenameAsset(assetPath, desiredName);
            if (!string.IsNullOrWhiteSpace(renameError))
            {
                Debug.LogWarning($"Failed to rename map asset '{currentName}' to '{desiredName}'. {renameError}");
            }
        }

        private static void DrawSpritePreview(Sprite sprite, float height)
        {
            if (sprite == null)
            {
                return;
            }

            var rect = GUILayoutUtility.GetRect(height * 1.6f, height, GUILayout.ExpandWidth(true));
            EditorGUI.DrawPreviewTexture(rect, sprite.texture, null, ScaleMode.ScaleToFit);
        }

        private static void MarkDirty(Object target)
        {
            if (target != null)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }

    public static class AdventureMapEditorAssetUtility
    {
        private const string CatalogAssetPath = "Assets/Resources/Adventure/AdventureMapCatalog.asset";
        private const string MapsFolderPath = "Assets/Resources/Adventure/Maps";
        private const string PresetsFolderPath = "Assets/Resources/Adventure/Presets";

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            EnsureAdventureMapAssets();
        }

        public static AdventureMapCatalog EnsureAdventureMapAssets()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Adventure");
            EnsureFolder(MapsFolderPath);
            EnsureFolder(PresetsFolderPath);

            var catalog = AssetDatabase.LoadAssetAtPath<AdventureMapCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                var createdCatalog = ScriptableObject.CreateInstance<AdventureMapCatalog>();
                AssetDatabase.CreateAsset(createdCatalog, CatalogAssetPath);
                AssetDatabase.SaveAssets();
                catalog = AssetDatabase.LoadAssetAtPath<AdventureMapCatalog>(CatalogAssetPath);
                if (catalog == null)
                {
                    Debug.LogError($"Failed to create adventure map catalog at {CatalogAssetPath}.");
                    return null;
                }
            }

            if (catalog.maps == null)
            {
                catalog.maps = new List<AdventureMapData>();
            }

            catalog.maps = LoadAllMapAssets()
                .Where(map => map != null)
                .GroupBy(map => AssetDatabase.GetAssetPath(map))
                .Select(group => group.First())
                .OrderBy(map => map.displayName)
                .ThenBy(map => map.mapId)
                .ToList();

            EnsureSampleMap(catalog);
            if (catalog != null)
            {
                EditorUtility.SetDirty(catalog);
            }
            AssetDatabase.SaveAssets();
            return catalog;
        }

        public static AdventureMapCatalog LoadAdventureMapCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<AdventureMapCatalog>(CatalogAssetPath);
        }

        public static string GenerateUniqueMapId(AdventureMapCatalog catalog, string prefix)
        {
            var index = 1;
            var used = catalog != null && catalog.maps != null
                ? new HashSet<string>(catalog.maps.Where(map => map != null).Select(map => map.mapId))
                : new HashSet<string>();

            string mapId;
            do
            {
                mapId = $"{prefix}_{index:00}";
                index++;
            }
            while (used.Contains(mapId));

            return mapId;
        }

        private static void EnsureSampleMap(AdventureMapCatalog catalog)
        {
            if (catalog == null)
            {
                return;
            }

            var existingSample = catalog.maps.FirstOrDefault(map => map != null && map.mapId == "rust_wastes");
            if (existingSample != null)
            {
                return;
            }

            var map = ScriptableObject.CreateInstance<AdventureMapData>();
            map.mapId = "rust_wastes";
            map.displayName = "\u9508\u94c1\u8352\u539f";
            map.description = "\u6837\u4f8b\u5730\u56fe\uff1a\u6f14\u793a\u5730\u56fe\u89e3\u9501\u3001\u6218\u6597\u987a\u5e8f\u3001\u654c\u65b9\u9635\u5bb9\u4e0e\u80cc\u666f\u53c2\u6570\u3002";
            map.unlockType = AdventureMapUnlockType.Default;
            map.unlockDescription = "\u9ed8\u8ba4\u89e3\u9501";
            map.backgroundSprite = LoadSprite("Assets/Art/Environments/Backgrounds/battle_bg_01.png");
            map.backgroundScale = 1f;
            map.backgroundOffset = new Vector2(0f, 0f);
            map.battles = CreateSampleBattles();

            var mapAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{MapsFolderPath}/\u9508\u94c1\u8352\u539f.asset");
            AssetDatabase.CreateAsset(map, mapAssetPath);
            catalog.maps.Add(map);
        }

        private static IEnumerable<AdventureMapData> LoadAllMapAssets()
        {
            var guids = AssetDatabase.FindAssets("t:AdventureMapData", new[] { MapsFolderPath });
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AdventureMapData>)
                .Where(map => map != null);
        }

        private static List<AdventureBattleConfig> CreateSampleBattles()
        {
            var enemyPool = LoadCombatants().Where(combatant => combatant != null && !combatant.isHero).Take(4).ToArray();
            var firstBattle = new AdventureBattleConfig
            {
                battleId = "battle_01",
                displayName = "荒原巡逻队",
                description = "\u6837\u4f8b\u7b2c\u4e00\u6218\uff1a\u6807\u51c6\u7684\u4e24\u524d\u6392\u4e00\u540e\u6392\u654c\u65b9\u9635\u5bb9\u3002",
                rewardDescription = "金币 60-100，经验 10-16",
                goldRewardMin = 60,
                goldRewardMax = 100,
                experienceRewardMin = 10,
                experienceRewardMax = 16,
                enemies = new List<AdventureEnemyPlacement>()
            };

            var secondBattle = new AdventureBattleConfig
            {
                battleId = "battle_02",
                displayName = "\u5e9f\u589f\u538b\u5236\u6218",
                description = "\u6837\u4f8b\u7b2c\u4e8c\u6218\uff1a\u4eba\u6570\u66f4\u591a\uff0c\u4f5c\u4e3a\u540c\u5730\u56fe\u7684\u8fdb\u9636\u6218\u6597\u3002",
                rewardDescription = "金币 100-160，经验 16-22",
                goldRewardMin = 100,
                goldRewardMax = 160,
                experienceRewardMin = 16,
                experienceRewardMax = 22,
                enemies = new List<AdventureEnemyPlacement>()
            };

            for (var i = 0; i < enemyPool.Length; i++)
            {
                firstBattle.enemies.Add(new AdventureEnemyPlacement
                {
                    combatant = enemyPool[i],
                    slot = i + 1,
                    level = 1
                });
            }

            for (var i = 0; i < enemyPool.Length; i++)
            {
                secondBattle.enemies.Add(new AdventureEnemyPlacement
                {
                    combatant = enemyPool[i],
                    slot = i + 1,
                    level = i < 2 ? 2 : 1
                });
            }

            return new List<AdventureBattleConfig> { firstBattle, secondBattle };
        }

        private static IEnumerable<CombatantDefinition> LoadCombatants()
        {
            var guids = AssetDatabase.FindAssets("t:CombatantDefinition", new[] { "Assets/ClockworkWastelandDemo/Data/Combatants" });
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CombatantDefinition>)
                .Where(combatant => combatant != null)
                .OrderBy(combatant => combatant.characterId);
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            var folderName = Path.GetFileName(assetPath);
            if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            if (!string.IsNullOrWhiteSpace(parent) && !string.IsNullOrWhiteSpace(folderName))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
