using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.Rendering.CoreLightUnitSliderDescriptors;

namespace UnityEditor.Rendering.HighDefinition
{
    static class LightUnitSliderDescriptors
    {
        // Lux
        public static CoreLightUnitSliderUIDescriptor LuxDescriptor = new CoreLightUnitSliderUIDescriptor(
            LightUnitValueRanges.LuxValueTable,
            LightUnitSliderDistributions.LuxDistribution,
            LightUnitTooltips.k_SunCaution,
            "Lux"
        );

        // Lumen
        public static CoreLightUnitSliderUIDescriptor LumenDescriptor = new CoreLightUnitSliderUIDescriptor(
            LightUnitValueRanges.LumenValueTable,
            LightUnitSliderDistributions.LumenDistribution,
            LightUnitTooltips.k_PunctualCaution,
            "Lumen"
        );

        // Exposure
        public static CoreLightUnitSliderUIDescriptor ExposureDescriptor = new CoreLightUnitSliderUIDescriptor(
            LightUnitValueRanges.ExposureValueTable,
            CoreLightUnitSliderDistributions.ExposureDistribution,
            LightUnitTooltips.k_ExposureBelowCaution,
            LightUnitTooltips.k_ExposureAboveCaution,
            "EV"
        );

        private static class LightUnitValueRanges
        {
            public static readonly CoreLightUnitSliderUIRange[] LumenValueTable =
            {
                new CoreLightUnitSliderUIRange(LightUnitIcon.ExteriorLight,  LightUnitTooltips.k_PunctualExterior,   new Vector2(3000, 40000), 10000),
                new CoreLightUnitSliderUIRange(LightUnitIcon.InteriorLight,  LightUnitTooltips.k_PunctualInterior,   new Vector2(300,  3000),  1000),
                new CoreLightUnitSliderUIRange(LightUnitIcon.DecorativeLight, LightUnitTooltips.k_PunctualDecorative, new Vector2(15,   300),   100),
                new CoreLightUnitSliderUIRange(CoreLightUnitIcon.Candlelight,    LightUnitTooltips.k_PunctualCandle,     new Vector2(0,    15),    12.5f),
            };

            public static readonly CoreLightUnitSliderUIRange[] LuxValueTable =
            {
                new CoreLightUnitSliderUIRange(LightUnitIcon.BrightSky,     LightUnitTooltips.k_LuxBrightSky,     new Vector2(80000, 130000), 100000),
                new CoreLightUnitSliderUIRange(LightUnitIcon.Overcast,      LightUnitTooltips.k_LuxOvercastSky,   new Vector2(10000, 80000),  20000),
                new CoreLightUnitSliderUIRange(LightUnitIcon.SunriseSunset, LightUnitTooltips.k_LuxSunriseSunset, new Vector2(1,     10000),  5000),
                new CoreLightUnitSliderUIRange(LightUnitIcon.Moonlight,     LightUnitTooltips.k_LuxMoonlight,     new Vector2(0,     1),      0.5f),
            };

            public static readonly CoreLightUnitSliderUIRange[] ExposureValueTable =
            {
                new CoreLightUnitSliderUIRange(LightUnitIcon.BrightSky,     LightUnitTooltips.k_ExposureBrightSky,     new Vector2(12, 15), 13),
                new CoreLightUnitSliderUIRange(LightUnitIcon.Overcast,      LightUnitTooltips.k_ExposureOvercastSky,   new Vector2(8,  12)),
                new CoreLightUnitSliderUIRange(LightUnitIcon.SunriseSunset, LightUnitTooltips.k_ExposureSunriseSunset, new Vector2(6,   8)),
                new CoreLightUnitSliderUIRange(LightUnitIcon.InteriorLight, LightUnitTooltips.k_ExposureInterior,      new Vector2(3,   6)),
                new CoreLightUnitSliderUIRange(LightUnitIcon.Moonlight,     LightUnitTooltips.k_ExposureMoonlitSky,    new Vector2(0,   3)),
                new CoreLightUnitSliderUIRange(LightUnitIcon.MoonlessNight, LightUnitTooltips.k_ExposureMoonlessNight, new Vector2(-5,  0)),
            };
        }

        private static class LightUnitSliderDistributions
        {
            // Warning: All of these values need to be kept in sync with their associated descriptor's set of value ranges.
            public static readonly float[] LuxDistribution = {0.0f, 0.05f, 0.5f, 0.9f, 1.0f};

            private const float LumenStep = 1 / 4f;
            public static readonly float[] LumenDistribution =
            {
                0 * LumenStep,
                1 * LumenStep,
                2 * LumenStep,
                3 * LumenStep,
                4 * LumenStep
            };
        }

        private static class LightUnitIcon
        {
            static string GetLightUnitIconPath() => HDUtils.GetHDRenderPipelinePath() +
            "/Editor/RenderPipelineResources/Texture/LightUnitIcons/";

            // Note: We do not use the editor resource loading mechanism for light unit icons because we need to skin the icon correctly for the editor theme.
            // Maybe the resource reloader can be improved to support icon loading (thus supporting skinning)?
            static Texture2D GetLightUnitIcon(string name)
            {
                var path = GetLightUnitIconPath() + name + ".png";
                return EditorGUIUtility.TrIconContent(path).image as Texture2D;
            }

            // TODO: Move all light unit icons from the package into the built-in resources.
            public static Texture2D ClearSky         = GetLightUnitIcon("ClearSky");
            public static Texture2D DecorativeLight  = GetLightUnitIcon("DecorativeLight");
            public static Texture2D ExteriorLight    = GetLightUnitIcon("ExteriorLight");
            public static Texture2D InteriorLight    = GetLightUnitIcon("InteriorLight");
            public static Texture2D MediumAreaLight  = GetLightUnitIcon("MediumAreaLight");
            public static Texture2D MoonlessNight    = GetLightUnitIcon("MoonlessNight");
            public static Texture2D Moonlight        = GetLightUnitIcon("Moonlight");
            public static Texture2D Overcast         = GetLightUnitIcon("Overcast");
            public static Texture2D SoftAreaLight    = GetLightUnitIcon("SoftAreaLight");
            public static Texture2D SunriseSunset    = GetLightUnitIcon("SunriseSunset");
            public static Texture2D VeryBrightSun    = GetLightUnitIcon("VeryBrightSun");
            public static Texture2D BrightSky        = GetLightUnitIcon("BrightSky");
        }

        private static class LightUnitTooltips
        {
            // Caution
            public const string k_SunCaution           = "Higher than Sunlight";
            public const string k_PunctualCaution      = "Very high intensity light";
            public const string k_ExposureBelowCaution = "Lower than a moonless scene";
            public const string k_ExposureAboveCaution = "Higher than a sunlit scene";

            // Lux / Directional
            public const string k_LuxBrightSky       = "High Sun";
            public const string k_LuxOvercastSky     = "Cloudy";
            public const string k_LuxSunriseSunset   = "Low Sun";
            public const string k_LuxMoonlight       = "Moon";

            // Punctual
            public const string k_PunctualExterior   = "Exterior";
            public const string k_PunctualInterior   = "Interior";
            public const string k_PunctualDecorative = "Decorative";
            public const string k_PunctualCandle     = "Candle";

            // Exposure
            public const string k_ExposureBrightSky     = "Sunlit Scene";
            public const string k_ExposureOvercastSky   = "Cloudy Scene";
            public const string k_ExposureSunriseSunset = "Low Sun Scene";
            public const string k_ExposureInterior      = "Interior Scene";
            public const string k_ExposureMoonlitSky    = "Moonlit Scene";
            public const string k_ExposureMoonlessNight = "Moonless Scene";
        }
    }
}
