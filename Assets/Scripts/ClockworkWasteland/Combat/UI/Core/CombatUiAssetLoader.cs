using System.IO;
using UnityEngine;

namespace ClockworkWasteland.Combat
{
    public static class CombatUiAssetLoader
    {
        public static Sprite LoadSprite(string assetPath, Vector4 border)
        {
            var fullPath = ResolveAssetPath(assetPath);
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
            {
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(fullPath)))
            {
                Object.Destroy(texture);
                return null;
            }

            texture.name = Path.GetFileNameWithoutExtension(assetPath);
            texture.filterMode = FilterMode.Bilinear;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        private static string ResolveAssetPath(string assetPath)
        {
            if (Path.IsPathRooted(assetPath))
            {
                return assetPath;
            }

            const string assetsPrefix = "Assets/";
            if (assetPath.StartsWith(assetsPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(Application.dataPath, assetPath.Substring(assetsPrefix.Length));
            }

            return Path.Combine(Application.dataPath, assetPath);
        }
    }
}
