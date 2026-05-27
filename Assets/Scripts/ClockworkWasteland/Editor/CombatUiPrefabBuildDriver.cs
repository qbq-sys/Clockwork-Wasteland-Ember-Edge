using System.IO;
using ClockworkWasteland.Combat;
using UnityEditor;
using UnityEngine;

namespace ClockworkWasteland.EditorTools
{
    public static class CombatUiPrefabBuildDriver
    {
        private const string PrefabRootPath = "Assets/UI/Prefabs";
        private const string ScreenPrefabRootPath = PrefabRootPath + "/Screens";
        private const string RewardScreenPrefabPath = ScreenPrefabRootPath + "/RewardScreenUI.prefab";
        private const string ShopPrefabPath = ScreenPrefabRootPath + "/ShopUI.prefab";
        private const string InventoryPrefabPath = ScreenPrefabRootPath + "/InventoryUI.prefab";
        private const string RouteMapPrefabPath = ScreenPrefabRootPath + "/RouteMapUI.prefab";
        private const string RestNodePrefabPath = ScreenPrefabRootPath + "/RestNodeUI.prefab";

        [InitializeOnLoadMethod]
        private static void EnsureMissingCombatUiScreenPrefabsOnLoad()
        {
            if (Application.isBatchMode)
            {
                CreateMissingCombatUiScreenPrefabs();
                return;
            }

            EditorApplication.delayCall += CreateMissingCombatUiScreenPrefabs;
        }

        public static void CreateMissingCombatUiScreenPrefabs()
        {
            EnsureFolder("Assets", "UI");
            EnsureFolder("Assets/UI", "Prefabs");
            EnsureFolder(PrefabRootPath, "Screens");
            CreateGeneratedPrefabIfMissingOrStub<RewardScreenUI>(RewardScreenPrefabPath, "RewardScreenUI", screen => screen.RebuildLayoutFromCode(null));
            CreateGeneratedPrefabIfMissingOrStub<ShopUI>(ShopPrefabPath, "ShopUI", screen => screen.RebuildLayoutFromCode(null));
            CreateGeneratedPrefabIfMissingOrStub<InventoryUI>(InventoryPrefabPath, "InventoryUI", screen => screen.RebuildLayoutFromCode(null));
            CreateGeneratedPrefabIfMissingOrStub<RouteMapUI>(RouteMapPrefabPath, "RouteMapUI", screen => screen.RebuildLayoutFromCode(null));
            CreateGeneratedPrefabIfMissingOrStub<RestNodeUI>(RestNodePrefabPath, "RestNodeUI", screen => screen.RebuildLayoutFromCode(null));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateGeneratedPrefabIfMissingOrStub<T>(string path, string prefabName, System.Action<T> buildLayout) where T : CombatUIScreen
        {
            if (!ShouldCreateOrRepairPrefab<T>(path))
            {
                return;
            }

            var root = new GameObject(prefabName, typeof(RectTransform), typeof(T));
            try
            {
                var rect = root.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                }

                var screen = root.GetComponent<T>();
                buildLayout?.Invoke(screen);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"Created generated UI prefab: {path}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static bool ShouldCreateOrRepairPrefab<T>(string path) where T : CombatUIScreen
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) == null)
            {
                return true;
            }

            if (!File.Exists(path))
            {
                return true;
            }

            var info = new FileInfo(path);
            return info.Length <= 2000;
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
