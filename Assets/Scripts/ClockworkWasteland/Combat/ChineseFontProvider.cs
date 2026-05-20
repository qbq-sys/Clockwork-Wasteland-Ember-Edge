using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ClockworkWasteland.Combat
{
    public static class ChineseFontProvider
    {
        private static TMP_FontAsset cachedFontAsset;
        private static Font cachedLegacyFont;
        private static readonly string[] ChineseFontNames =
        {
            "Microsoft YaHei",
            "Noto Sans SC",
            "SimHei",
            "SimSun",
            "DengXian",
            "Source Han Sans SC",
            "Arial Unicode MS"
        };

        public static Font LegacyFont
        {
            get
            {
                if (cachedLegacyFont != null)
                {
                    return cachedLegacyFont;
                }

                cachedLegacyFont = Font.CreateDynamicFontFromOSFont(ChineseFontNames, 24);
                return cachedLegacyFont;
            }
        }

        public static TMP_FontAsset FontAsset
        {
            get
            {
                if (cachedFontAsset != null)
                {
                    return cachedFontAsset;
                }

                var font = LegacyFont;

                if (font == null)
                {
                    return null;
                }

                try
                {
                    cachedFontAsset = TMP_FontAsset.CreateFontAsset(
                        font,
                        90,
                        9,
                        GlyphRenderMode.SDFAA,
                        1024,
                        1024,
                        AtlasPopulationMode.Dynamic);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning($"创建中文 TMP 字体失败，将使用默认字体。{exception.Message}");
                    cachedFontAsset = null;
                }

                if (cachedFontAsset == null)
                {
                    return null;
                }

                cachedFontAsset.name = "Runtime Chinese TMP Font";
                return cachedFontAsset;
            }
        }

        public static void Apply(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            var fontAsset = FontAsset;
            if (fontAsset != null)
            {
                text.font = fontAsset;
            }
        }
    }
}
