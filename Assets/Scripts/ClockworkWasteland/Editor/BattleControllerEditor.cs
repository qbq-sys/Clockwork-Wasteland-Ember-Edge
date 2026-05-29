using ClockworkWasteland.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClockworkWasteland.EditorTools
{
    [CustomEditor(typeof(BattleController))]
    public sealed class BattleControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty heroPartyProperty;
        private SerializedProperty enemyPartyProperty;
        private SerializedProperty heroPoolConfigProperty;
        private SerializedProperty combatantYProperty;
        private SerializedProperty nameplatePositionYProperty;
        private SerializedProperty battleHudControllerPrefabProperty;
        private SerializedProperty defaultUnitPrefabProperty;
        private SerializedProperty nameplatePrefabProperty;
        private SerializedProperty turnIndicatorPrefabProperty;
        private SerializedProperty battleBackgroundsProperty;
        private SerializedProperty battleBackgroundIndexProperty;
        private SerializedProperty floatingTextBaseOffsetProperty;
        private SerializedProperty floatingTextBaseScaleProperty;
        private SerializedProperty floatingTextBurstWindowProperty;
        private SerializedProperty floatingTextQueueDelayProperty;
        private SerializedProperty floatingTextHorizontalSpacingProperty;
        private SerializedProperty floatingTextVerticalSpacingProperty;
        private SerializedProperty floatingTextAdditionalLiftPerTextProperty;
        private SerializedProperty attackFocusBlurMaterialProperty;
        private SerializedProperty attackFocusZoomRatioProperty;
        private SerializedProperty attackFocusCameraOffsetProperty;
        private SerializedProperty attackFocusScaleProperty;
        private SerializedProperty attackFocusBlurStrengthProperty;
        private SerializedProperty attackFocusBlurTintProperty;
        private SerializedProperty attackFocusLungeDurationProperty;
        private SerializedProperty attackFocusHitPauseProperty;
        private SerializedProperty attackFocusScaleRestoreDurationProperty;
        private SerializedProperty attackFocusRecoilRatioProperty;
        private SerializedProperty attackFocusReturnDurationProperty;
        private SerializedProperty attackFocusSingleLungeRatioProperty;
        private SerializedProperty attackFocusSingleMinDistanceProperty;
        private SerializedProperty attackFocusSingleActorOffsetProperty;
        private SerializedProperty attackFocusSingleTargetOffsetProperty;
        private SerializedProperty attackFocusFriendlyAdvanceOffsetProperty;
        private SerializedProperty attackFocusAoeActorOffsetProperty;
        private SerializedProperty attackFocusAoeTargetOffsetProperty;
        private SerializedProperty attackFocusAoeTargetSpacingProperty;
        private SerializedProperty debugTestingEnabledProperty;
        private SerializedProperty debugExperienceRewardMultiplierProperty;
        private SerializedProperty debugGoldRewardMultiplierProperty;
        private SerializedProperty debugChestGoldMultiplierProperty;
        private SerializedProperty debugUnlockAllMapsProperty;
        private SerializedProperty debugUnlockAllHeroesOnNewGameProperty;
        private SerializedProperty debugNewGameHeroLevelProperty;
        private SerializedProperty debugStartingGoldOverrideProperty;
        private SerializedProperty debugManualExperienceGrantProperty;

        private void OnEnable()
        {
            heroPartyProperty = serializedObject.FindProperty("heroParty");
            enemyPartyProperty = serializedObject.FindProperty("enemyParty");
            heroPoolConfigProperty = serializedObject.FindProperty("heroPoolConfig");
            combatantYProperty = serializedObject.FindProperty("combatantY");
            nameplatePositionYProperty = serializedObject.FindProperty("nameplatePositionY");
            battleHudControllerPrefabProperty = serializedObject.FindProperty("battleHudControllerPrefab");
            defaultUnitPrefabProperty = serializedObject.FindProperty("defaultUnitPrefab");
            nameplatePrefabProperty = serializedObject.FindProperty("nameplatePrefab");
            turnIndicatorPrefabProperty = serializedObject.FindProperty("turnIndicatorPrefab");
            battleBackgroundsProperty = serializedObject.FindProperty("battleBackgrounds");
            battleBackgroundIndexProperty = serializedObject.FindProperty("battleBackgroundIndex");
            floatingTextBaseOffsetProperty = serializedObject.FindProperty("floatingTextBaseOffset");
            floatingTextBaseScaleProperty = serializedObject.FindProperty("floatingTextBaseScale");
            floatingTextBurstWindowProperty = serializedObject.FindProperty("floatingTextBurstWindow");
            floatingTextQueueDelayProperty = serializedObject.FindProperty("floatingTextQueueDelay");
            floatingTextHorizontalSpacingProperty = serializedObject.FindProperty("floatingTextHorizontalSpacing");
            floatingTextVerticalSpacingProperty = serializedObject.FindProperty("floatingTextVerticalSpacing");
            floatingTextAdditionalLiftPerTextProperty = serializedObject.FindProperty("floatingTextAdditionalLiftPerText");
            attackFocusBlurMaterialProperty = serializedObject.FindProperty("attackFocusBlurMaterial");
            attackFocusZoomRatioProperty = serializedObject.FindProperty("attackFocusZoomRatio");
            attackFocusCameraOffsetProperty = serializedObject.FindProperty("attackFocusCameraOffset");
            attackFocusScaleProperty = serializedObject.FindProperty("attackFocusScale");
            attackFocusBlurStrengthProperty = serializedObject.FindProperty("attackFocusBlurStrength");
            attackFocusBlurTintProperty = serializedObject.FindProperty("attackFocusBlurTint");
            attackFocusLungeDurationProperty = serializedObject.FindProperty("attackFocusLungeDuration");
            attackFocusHitPauseProperty = serializedObject.FindProperty("attackFocusHitPause");
            attackFocusScaleRestoreDurationProperty = serializedObject.FindProperty("attackFocusScaleRestoreDuration");
            attackFocusRecoilRatioProperty = serializedObject.FindProperty("attackFocusRecoilRatio");
            attackFocusReturnDurationProperty = serializedObject.FindProperty("attackFocusReturnDuration");
            attackFocusSingleLungeRatioProperty = serializedObject.FindProperty("attackFocusSingleLungeRatio");
            attackFocusSingleMinDistanceProperty = serializedObject.FindProperty("attackFocusSingleMinDistance");
            attackFocusSingleActorOffsetProperty = serializedObject.FindProperty("attackFocusSingleActorOffset");
            attackFocusSingleTargetOffsetProperty = serializedObject.FindProperty("attackFocusSingleTargetOffset");
            attackFocusFriendlyAdvanceOffsetProperty = serializedObject.FindProperty("attackFocusFriendlyAdvanceOffset");
            attackFocusAoeActorOffsetProperty = serializedObject.FindProperty("attackFocusAoeActorOffset");
            attackFocusAoeTargetOffsetProperty = serializedObject.FindProperty("attackFocusAoeTargetOffset");
            attackFocusAoeTargetSpacingProperty = serializedObject.FindProperty("attackFocusAoeTargetSpacing");
            debugTestingEnabledProperty = serializedObject.FindProperty("debugTestingEnabled");
            debugExperienceRewardMultiplierProperty = serializedObject.FindProperty("debugExperienceRewardMultiplier");
            debugGoldRewardMultiplierProperty = serializedObject.FindProperty("debugGoldRewardMultiplier");
            debugChestGoldMultiplierProperty = serializedObject.FindProperty("debugChestGoldMultiplier");
            debugUnlockAllMapsProperty = serializedObject.FindProperty("debugUnlockAllMaps");
            debugUnlockAllHeroesOnNewGameProperty = serializedObject.FindProperty("debugUnlockAllHeroesOnNewGame");
            debugNewGameHeroLevelProperty = serializedObject.FindProperty("debugNewGameHeroLevel");
            debugStartingGoldOverrideProperty = serializedObject.FindProperty("debugStartingGoldOverride");
            debugManualExperienceGrantProperty = serializedObject.FindProperty("debugManualExperienceGrant");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("这里主要是战斗表现和测试调试参数。改完后点击底部“立即保存参数”可直接保存当前场景。", MessageType.Info);

            DrawSection("队伍配置");
            DrawProperty(heroPartyProperty, "初始英雄队伍");
            DrawProperty(enemyPartyProperty, "场景敌人池");
            DrawProperty(heroPoolConfigProperty, "手工英雄池");
            DrawProperty(combatantYProperty, "单位公共 Y");
            DrawProperty(nameplatePositionYProperty, "NameplatePosition Y");

            DrawSection("表现资源");
            DrawProperty(battleHudControllerPrefabProperty, "战斗 HUD 控制器预制体");
            DrawProperty(defaultUnitPrefabProperty, "默认单位预制体");
            DrawProperty(nameplatePrefabProperty, "血条名牌预制体");
            DrawProperty(turnIndicatorPrefabProperty, "当前回合标记预制体");
            DrawProperty(battleBackgroundsProperty, "战斗背景列表", true);
            DrawProperty(battleBackgroundIndexProperty, "默认背景索引");
            DrawProperty(floatingTextBaseOffsetProperty, "飘字初始偏移");
            DrawProperty(floatingTextBaseScaleProperty, "飘字基础缩放");
            DrawProperty(floatingTextBurstWindowProperty, "飘字分组窗口");
            DrawProperty(floatingTextQueueDelayProperty, "飘字排队延迟");
            DrawProperty(floatingTextHorizontalSpacingProperty, "飘字横向间距");
            DrawProperty(floatingTextVerticalSpacingProperty, "飘字纵向间距");
            DrawProperty(floatingTextAdditionalLiftPerTextProperty, "额外抬升");

            DrawSection("攻击特写");
            DrawProperty(attackFocusBlurMaterialProperty, "模糊材质");
            DrawProperty(attackFocusZoomRatioProperty, "镜头缩放比例");
            DrawProperty(attackFocusCameraOffsetProperty, "镜头聚焦偏移");
            DrawProperty(attackFocusScaleProperty, "角色特写放大");
            DrawProperty(attackFocusBlurStrengthProperty, "背景模糊强度");
            DrawProperty(attackFocusBlurTintProperty, "背景染色");
            DrawProperty(attackFocusLungeDurationProperty, "突进时长");
            DrawProperty(attackFocusHitPauseProperty, "命中停顿");
            DrawProperty(attackFocusScaleRestoreDurationProperty, "缩放恢复时长");
            DrawProperty(attackFocusRecoilRatioProperty, "后撤比例");
            DrawProperty(attackFocusReturnDurationProperty, "快速归位时长");
            DrawProperty(attackFocusSingleLungeRatioProperty, "单体突进比例");
            DrawProperty(attackFocusSingleMinDistanceProperty, "单体最小突进距离");
            DrawProperty(attackFocusSingleActorOffsetProperty, "单体攻击者偏移");
            DrawProperty(attackFocusSingleTargetOffsetProperty, "单体受击者偏移");
            DrawProperty(attackFocusFriendlyAdvanceOffsetProperty, "单友前冲偏移");
            DrawProperty(attackFocusAoeActorOffsetProperty, "群攻施法者偏移");
            DrawProperty(attackFocusAoeTargetOffsetProperty, "群攻目标偏移");
            DrawProperty(attackFocusAoeTargetSpacingProperty, "群攻目标间距");

            DrawSection("测试调试");
            DrawProperty(debugTestingEnabledProperty, "启用测试调试");
            DrawProperty(debugExperienceRewardMultiplierProperty, "经验奖励倍率");
            DrawProperty(debugGoldRewardMultiplierProperty, "战斗金币倍率");
            DrawProperty(debugChestGoldMultiplierProperty, "宝箱金币倍率");
            DrawProperty(debugUnlockAllMapsProperty, "地图全部解锁");
            DrawProperty(debugUnlockAllHeroesOnNewGameProperty, "新游戏全英雄解锁");
            DrawProperty(debugNewGameHeroLevelProperty, "新游戏英雄起始等级");
            DrawProperty(debugStartingGoldOverrideProperty, "新游戏起始金币(-1默认)");
            DrawProperty(debugManualExperienceGrantProperty, "调试发放经验值");

            serializedObject.ApplyModifiedProperties();

            var controller = target as BattleController;
            EditorGUILayout.Space(10f);
            using (new EditorGUI.DisabledScope(!Application.isPlaying || controller == null))
            {
                if (GUILayout.Button("调试：重置当前运行态", GUILayout.Height(28f)))
                {
                    controller.DebugResetCurrentRunWithTesting();
                }

                if (GUILayout.Button("调试：给当前英雄发经验", GUILayout.Height(28f)))
                {
                    controller.DebugGrantExperienceToCurrentHeroes();
                }
            }

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("立即保存参数", GUILayout.Height(32f)))
            {
                SaveTarget();
            }
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static void DrawProperty(SerializedProperty property, string label, bool includeChildren = false)
        {
            if (property == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(property, new GUIContent(label), includeChildren);
        }

        private void SaveTarget()
        {
            serializedObject.ApplyModifiedProperties();

            var controller = target as BattleController;
            if (controller == null)
            {
                return;
            }

            EditorUtility.SetDirty(controller);
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);

            if (controller.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
                EditorSceneManager.SaveScene(controller.gameObject.scene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("BattleController 参数已保存。", controller);
        }
    }
}
