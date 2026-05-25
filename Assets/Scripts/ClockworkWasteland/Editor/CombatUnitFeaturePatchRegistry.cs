using System;
using System.Collections.Generic;
using System.Linq;
using ClockworkWasteland.Combat;
using UnityEditor;
using UnityEngine;

namespace ClockworkWasteland.EditorTools
{
    public static class CombatUnitFeaturePatchRegistry
    {
        private const string UnitPrefabsPath = "Assets/ClockworkWastelandDemo/Prefabs/CombatUnits";
        private const string SharedUnitPrefabPath = "Assets/ClockworkWastelandDemo/Prefabs/CombatUnit.prefab";
        private const string SessionSyncKey = "ClockworkWasteland.CombatUnitFeaturePatchRegistry.FeatureSignature";

        private static List<CombatUnitFeaturePatch> cachedPatches;

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            var signature = GetFeatureSignature();
            if (SessionState.GetString(SessionSyncKey, string.Empty) == signature)
            {
                return;
            }

            SessionState.SetString(SessionSyncKey, signature);
            EditorApplication.delayCall += SyncAllCombatUnitPrefabs;
        }

        [MenuItem("Clockwork Wasteland/Tools/Sync Combat Unit Features")]
        public static void SyncAllCombatUnitPrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var prefabPaths = new HashSet<string>(
                AssetDatabase.FindAssets("t:Prefab", new[] { UnitPrefabsPath })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => !string.IsNullOrWhiteSpace(path)));

            if (AssetDatabase.LoadAssetAtPath<GameObject>(SharedUnitPrefabPath) != null)
            {
                prefabPaths.Add(SharedUnitPrefabPath);
            }

            foreach (var prefabPath in prefabPaths.OrderBy(path => path))
            {
                ApplyAllToPrefabAsset(prefabPath, null);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static bool ApplyAll(GameObject prefabRoot, CombatantDefinition combatant)
        {
            var changed = false;
            foreach (var patch in GetPatches())
            {
                changed |= patch.Apply(prefabRoot, combatant);
            }

            return changed;
        }

        public static bool ApplyAllToPrefabAsset(string prefabPath, CombatantDefinition combatant)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return false;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return false;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var removedMissingScripts = RemoveMissingScriptsRecursively(prefabRoot);
                var patched = ApplyAll(prefabRoot, combatant);
                if (!removedMissingScripts && !patched)
                {
                    return false;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static IReadOnlyList<CombatUnitFeaturePatch> GetPatches()
        {
            if (cachedPatches != null)
            {
                return cachedPatches;
            }

            cachedPatches = TypeCache.GetTypesDerivedFrom<CombatUnitFeaturePatch>()
                .Where(type => type != null && !type.IsAbstract)
                .Select(type => Activator.CreateInstance(type) as CombatUnitFeaturePatch)
                .Where(patch => patch != null)
                .OrderBy(patch => patch.Order)
                .ThenBy(patch => patch.FeatureId)
                .ToList();
            return cachedPatches;
        }

        private static string GetFeatureSignature()
        {
            return string.Join("|", GetPatches().Select(patch => $"{patch.FeatureId}:{patch.Version}"));
        }

        private static bool RemoveMissingScriptsRecursively(GameObject root)
        {
            if (root == null)
            {
                return false;
            }

            var removedAny = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root) > 0;
            foreach (Transform child in root.transform)
            {
                removedAny |= RemoveMissingScriptsRecursively(child.gameObject);
            }

            return removedAny;
        }
    }
}
