using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ClockworkWasteland.Combat
{
    public static class EditorSpriteSheetLoader
    {
        public static Sprite[] LoadSprites(string assetPath)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => ExtractFrameIndex(sprite.name))
                .ToArray();
#else
            return new Sprite[0];
#endif
        }

        public static Sprite LoadSprite(string assetPath)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
            return null;
#endif
        }

        public static bool SpriteExists(string assetPath)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath) != null;
#else
            return false;
#endif
        }

        private static int ExtractFrameIndex(string spriteName)
        {
            var underscore = spriteName.LastIndexOf('_');
            if (underscore < 0 || underscore >= spriteName.Length - 1)
            {
                return 0;
            }

            return int.TryParse(spriteName.Substring(underscore + 1), out var index) ? index : 0;
        }
    }
}
