using System.IO;
using System.Text.RegularExpressions;
using ClockworkWasteland.Combat;
using UnityEditor;
using UnityEngine;

namespace ClockworkWasteland.Editor
{
    public static class SkillAssetScriptReferenceRepair
    {
        private const string SkillSearchRoot = "Assets";
        private static readonly Regex ScriptReferencePattern = new Regex(
            @"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*[^,\}]+,\s*type:\s*3\}",
            RegexOptions.Compiled);

        [InitializeOnLoadMethod]
        private static void RepairOnLoad()
        {
            RepairSkillAssetReferences(false);
        }

        [MenuItem("Clockwork Wasteland/Tools/Repair Skill Asset Script References")]
        public static void RepairSkillAssetReferencesFromMenu()
        {
            RepairSkillAssetReferences(true);
        }

        private static void RepairSkillAssetReferences(bool showDialog)
        {
            var skillScript = MonoScript.FromScriptableObject(ScriptableObject.CreateInstance<SkillData>());
            var buffScript = MonoScript.FromScriptableObject(ScriptableObject.CreateInstance<BuffData>());

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(skillScript, out var skillScriptGuid, out long _) ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(buffScript, out var buffScriptGuid, out long _))
            {
                Debug.LogWarning("Could not resolve SkillData/BuffData script GUIDs.");
                return;
            }

            var fixedCount = 0;
            fixedCount += RepairAssetsOfType<SkillData>(skillScriptGuid, LooksLikeSkillAsset);
            fixedCount += RepairAssetsOfType<BuffData>(buffScriptGuid, LooksLikeBuffAsset);

            if (fixedCount <= 0)
            {
                return;
            }

            AssetDatabase.Refresh();
            Debug.Log($"Repaired {fixedCount} combat data asset script reference(s).");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Skill Asset Repair", $"Repaired {fixedCount} asset script reference(s).", "OK");
            }
        }

        private static int RepairAssetsOfType<T>(string scriptGuid, System.Func<string, bool> assetMatcher) where T : ScriptableObject
        {
            var fixedCount = 0;
            var targetScriptLine = $"m_Script: {{fileID: 11500000, guid: {scriptGuid}, type: 3}}";

            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { SkillSearchRoot }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".asset"))
                {
                    continue;
                }

                fixedCount += RepairAssetFile(assetPath, targetScriptLine, assetMatcher);
            }

            foreach (var assetPath in Directory.GetFiles("Assets", "*.asset", SearchOption.AllDirectories))
            {
                fixedCount += RepairAssetFile(assetPath.Replace('\\', '/'), targetScriptLine, assetMatcher);
            }

            return fixedCount;
        }

        private static int RepairAssetFile(string assetPath, string targetScriptLine, System.Func<string, bool> assetMatcher)
        {
            var text = File.ReadAllText(assetPath);
            if (!ScriptReferencePattern.IsMatch(text))
            {
                return 0;
            }

            if (text.Contains(targetScriptLine))
            {
                return 0;
            }

            if (!assetMatcher(text))
            {
                return 0;
            }

            var repaired = ScriptReferencePattern.Replace(text, targetScriptLine, 1);
            File.WriteAllText(assetPath, repaired);
            return 1;
        }

        private static bool LooksLikeSkillAsset(string text)
        {
            return text.Contains("skillId:") && text.Contains("skillName:");
        }

        private static bool LooksLikeBuffAsset(string text)
        {
            return text.Contains("buffId:") && text.Contains("buffName:");
        }
    }
}
