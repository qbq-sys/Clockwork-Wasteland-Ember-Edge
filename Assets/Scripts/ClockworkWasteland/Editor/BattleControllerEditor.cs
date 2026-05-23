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
        private SerializedProperty battleUIPrefabProperty;
        private SerializedProperty defaultUnitPrefabProperty;
        private SerializedProperty nameplatePrefabProperty;
        private SerializedProperty heroVisualScaleProperty;
        private SerializedProperty battleBackgroundsProperty;
        private SerializedProperty battleBackgroundIndexProperty;
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

        private void OnEnable()
        {
            heroPartyProperty = serializedObject.FindProperty("heroParty");
            enemyPartyProperty = serializedObject.FindProperty("enemyParty");
            heroPoolConfigProperty = serializedObject.FindProperty("heroPoolConfig");
            battleUIPrefabProperty = serializedObject.FindProperty("battleUIPrefab");
            defaultUnitPrefabProperty = serializedObject.FindProperty("defaultUnitPrefab");
            nameplatePrefabProperty = serializedObject.FindProperty("nameplatePrefab");
            heroVisualScaleProperty = serializedObject.FindProperty("heroVisualScale");
            battleBackgroundsProperty = serializedObject.FindProperty("battleBackgrounds");
            battleBackgroundIndexProperty = serializedObject.FindProperty("battleBackgroundIndex");
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
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("这里主要是战斗演出与调试入口参数。改完后点击底部“立即保存参数”可直接保存当前场景。", MessageType.Info);

            DrawSection("队伍配置");
            DrawProperty(heroPartyProperty, "初始英雄队伍");
            DrawProperty(enemyPartyProperty, "场景敌人池");
            DrawProperty(heroPoolConfigProperty, "手工英雄池");

            DrawSection("表现资源");
            DrawProperty(battleUIPrefabProperty, "战斗UI预制体");
            DrawProperty(defaultUnitPrefabProperty, "默认单位预制体");
            DrawProperty(nameplatePrefabProperty, "血条名牌预制体");
            DrawProperty(heroVisualScaleProperty, "英雄整体缩放");
            DrawProperty(battleBackgroundsProperty, "战斗背景列表", true);
            DrawProperty(battleBackgroundIndexProperty, "默认背景索引");

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
            DrawProperty(attackFocusRecoilRatioProperty, "子弹时间后撤比例");
            DrawProperty(attackFocusReturnDurationProperty, "快速归位时长");
            DrawProperty(attackFocusSingleLungeRatioProperty, "单体突进比例");
            DrawProperty(attackFocusSingleMinDistanceProperty, "单体最小突进距离");
            DrawProperty(attackFocusSingleActorOffsetProperty, "单体攻击者固定偏移");
            DrawProperty(attackFocusSingleTargetOffsetProperty, "单体受击者固定偏移");
            DrawProperty(attackFocusFriendlyAdvanceOffsetProperty, "单友前冲固定左侧偏移");
            DrawProperty(attackFocusAoeActorOffsetProperty, "群攻施法者偏移");
            DrawProperty(attackFocusAoeTargetOffsetProperty, "群攻目标偏移");
            DrawProperty(attackFocusAoeTargetSpacingProperty, "群攻目标间距");

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10f);
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
