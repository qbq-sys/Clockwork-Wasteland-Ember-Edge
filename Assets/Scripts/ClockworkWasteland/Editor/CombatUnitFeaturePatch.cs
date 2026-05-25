using System.Collections.Generic;
using System.IO;
using ClockworkWasteland.Combat;
using UnityEditor;
using UnityEngine;

namespace ClockworkWasteland.EditorTools
{
    public abstract class CombatUnitFeaturePatch
    {
        public abstract string FeatureId { get; }
        public abstract string TemplatePrefabPath { get; }
        public abstract string FeatureRootName { get; }
        public virtual int Order => 0;
        public virtual int Version => 1;
        protected virtual IEnumerable<string> LegacyFeatureRootNames => System.Array.Empty<string>();

        public virtual bool Apply(GameObject prefabRoot, CombatantDefinition combatant)
        {
            if (prefabRoot == null)
            {
                return false;
            }

            var templatePrefab = EnsureTemplatePrefab();
            if (templatePrefab == null)
            {
                return false;
            }

            var featuresRoot = EnsureChild(prefabRoot.transform, "Features", out var createdFeaturesRoot);
            var changed = createdFeaturesRoot;
            var acceptedNames = new HashSet<string>(LegacyFeatureRootNames ?? System.Array.Empty<string>())
            {
                FeatureRootName,
                templatePrefab.transform.name
            };

            var existingFeatureRoots = GetDirectChildrenByNames(featuresRoot, acceptedNames);
            if (existingFeatureRoots.Count == 0)
            {
                InstantiateTemplate(featuresRoot, templatePrefab, FeatureRootName);
                return true;
            }

            var existingFeatureRoot = existingFeatureRoots[0];
            for (var i = 1; i < existingFeatureRoots.Count; i++)
            {
                Object.DestroyImmediate(existingFeatureRoots[i].gameObject);
                changed = true;
            }

            var featureSource = PrefabUtility.GetCorrespondingObjectFromSource(existingFeatureRoot.gameObject);
            if (featureSource == templatePrefab)
            {
                PrefabUtility.RevertPrefabInstance(existingFeatureRoot.gameObject, InteractionMode.AutomatedAction);
                return changed;
            }

            Object.DestroyImmediate(existingFeatureRoot.gameObject);
            InstantiateTemplate(featuresRoot, templatePrefab, FeatureRootName);
            return true;
        }

        protected abstract GameObject CreateTemplateRoot();

        protected GameObject EnsureTemplatePrefab()
        {
            EnsureFolderPath(Path.GetDirectoryName(TemplatePrefabPath)?.Replace("\\", "/"));
            var templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefabPath);
            if (templatePrefab != null)
            {
                var templateRoot = PrefabUtility.LoadPrefabContents(TemplatePrefabPath);
                try
                {
                    if (UpgradeTemplatePrefab(templateRoot))
                    {
                        PrefabUtility.SaveAsPrefabAsset(templateRoot, TemplatePrefabPath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(templateRoot);
                }

                return templatePrefab;
            }

            var createdTemplateRoot = CreateTemplateRoot();
            try
            {
                PrefabUtility.SaveAsPrefabAsset(createdTemplateRoot, TemplatePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(createdTemplateRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefabPath);
        }

        protected virtual bool UpgradeTemplatePrefab(GameObject templateRoot)
        {
            return false;
        }

        protected static Transform EnsureChild(Transform parent, string childName, out bool created)
        {
            var existing = parent.Find(childName);
            if (existing != null)
            {
                created = false;
                return existing;
            }

            var child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            created = true;
            return child;
        }

        protected static void EnsureFolderPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var normalizedPath = assetPath.Replace("\\", "/");
            if (AssetDatabase.IsValidFolder(normalizedPath))
            {
                return;
            }

            var segments = normalizedPath.Split('/');
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

        private static void InstantiateTemplate(Transform featuresRoot, GameObject templatePrefab, string featureRootName)
        {
            var instance = PrefabUtility.InstantiatePrefab(templatePrefab) as GameObject;
            if (instance == null)
            {
                return;
            }

            instance.name = featureRootName;
            instance.transform.SetParent(featuresRoot, false);
        }

        private static System.Collections.Generic.List<Transform> GetDirectChildrenByNames(Transform parent, HashSet<string> childNames)
        {
            var matches = new System.Collections.Generic.List<Transform>();
            if (parent == null || childNames == null || childNames.Count == 0)
            {
                return matches;
            }

            foreach (Transform child in parent)
            {
                if (child != null && childNames.Contains(child.name))
                {
                    matches.Add(child);
                }
            }

            return matches;
        }
    }
}
