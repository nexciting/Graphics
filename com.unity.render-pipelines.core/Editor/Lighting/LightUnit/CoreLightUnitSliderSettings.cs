using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public struct CoreLightUnitSliderUIDescriptor
    {
        public CoreLightUnitSliderUIDescriptor(CoreLightUnitSliderUIRange[] valueRanges, float[] sliderDistribution,
                                               string cautionTooltip, string unitName, bool hasMarkers = true, bool clampValue = false)
            : this(valueRanges, sliderDistribution, cautionTooltip, cautionTooltip, unitName, hasMarkers, clampValue)
        {}

        public CoreLightUnitSliderUIDescriptor(CoreLightUnitSliderUIRange[] valueRanges, float[] sliderDistribution, string belowRangeTooltip,
                                               string aboveRangeTooltip, string unitName, bool hasMarkers = true, bool clampValue = false)
        {
            this.valueRanges = valueRanges;
            this.belowRangeTooltip = belowRangeTooltip;
            this.aboveRangeTooltip = aboveRangeTooltip;
            this.sliderDistribution = sliderDistribution;
            this.unitName = unitName;
            this.hasMarkers = hasMarkers;
            this.clampValue = clampValue;

            sliderRange = new Vector2(
                this.valueRanges.Min(x => x.value.x),
                this.valueRanges.Max(x => x.value.y)
            );
        }

        public readonly float[] sliderDistribution;
        public readonly CoreLightUnitSliderUIRange[] valueRanges;
        public readonly Vector2 sliderRange;
        public readonly string belowRangeTooltip;
        public readonly string aboveRangeTooltip;
        public readonly string unitName;
        public readonly bool hasMarkers;
        public readonly bool clampValue;
    }

    public struct CoreLightUnitSliderUIRange
    {
        public CoreLightUnitSliderUIRange(Texture2D icon, string tooltip, Vector2 value)
        // If no preset value provided, then by default it is the average of the value range.
            : this(icon, tooltip, value, 0.5f * (value.x + value.y))
        {}

        public CoreLightUnitSliderUIRange(Texture2D icon, string tooltip, Vector2 value, float presetValue)
        {
            this.content = new GUIContent(icon, tooltip);
            this.value = value;

            Debug.Assert(presetValue > value.x && presetValue < value.y, "Preset value is outside the slider value range.");

            // Preset values are arbitrarily chosen by artist, and we must use it instead of
            // deriving it automatically (ie, the value range average).
            this.presetValue = presetValue;
        }

        public static CoreLightUnitSliderUIRange CautionRange(string tooltip, float value) => new CoreLightUnitSliderUIRange
        {
            // Load the buildin caution icon with provided tooltip.
            content = new GUIContent(EditorGUIUtility.TrIconContent("console.warnicon").image, tooltip),
            value = new Vector2(-1, value),
            presetValue = -1
        };

        public GUIContent content;
        public Vector2    value;
        public float      presetValue;
    }

    public static class CoreLightUnitSliderDescriptors
    {
        // Temperature
        public static CoreLightUnitSliderUIDescriptor TemperatureDescriptor = new CoreLightUnitSliderUIDescriptor(
            CoreLightUnitValueRanges.KelvinValueTableNew,
            CoreLightUnitSliderDistributions.ExposureDistribution,
            CoreLightUnitTooltips.k_TemperatureCaution,
            "Kelvin",
            false,
            true
        );

        private static class CoreLightUnitValueRanges
        {
            public static readonly CoreLightUnitSliderUIRange[] KelvinValueTableNew =
            {
                new CoreLightUnitSliderUIRange(CoreLightUnitIcon.BlueSky,          CoreLightUnitTooltips.k_TemperatureBlueSky,          new Vector2(10000, 20000), 15000),
                new CoreLightUnitSliderUIRange(CoreLightUnitIcon.Shade,            CoreLightUnitTooltips.k_TemperatureShade,            new Vector2(7000,  10000), 8000),
                new CoreLightUnitSliderUIRange(CoreLightUnitIcon.CloudySky,        CoreLightUnitTooltips.k_TemperatureCloudySky,        new Vector2(6000,   7000), 6500),
                new CoreLightUnitSliderUIRange(CoreLightUnitIcon.DirectSunlight,   CoreLightUnitTooltips.k_TemperatureDirectSunlight,   new Vector2(4500,   6000), 5500),
                new CoreLightUnitSliderUIRange(CoreLightUnitIcon.Fluorescent,      CoreLightUnitTooltips.k_TemperatureFluorescent,      new Vector2(3500,   4500), 4000),
                new CoreLightUnitSliderUIRange(CoreLightUnitIcon.IntenseAreaLight, CoreLightUnitTooltips.k_TemperatureIncandescent,     new Vector2(2500,   3500), 3000),
                new CoreLightUnitSliderUIRange(CoreLightUnitIcon.Candlelight,      CoreLightUnitTooltips.k_TemperatureCandle,           new Vector2(1500,   2500), 1900),
            };
        }

        public static class CoreLightUnitIcon
        {
            // Note: We do not use the editor resource loading mechanism for light unit icons because we need to skin the icon correctly for the editor theme.
            // Maybe the resource reloader can be improved to support icon loading (thus supporting skinning)?
            static Texture2D GetLightUnitIcon(string name)
            {
                return CoreEditorUtils.LoadIcon(@"Packages/com.unity.render-pipelines.core/Editor/Lighting/Icons/LightUnitIcons", name, ".png");
            }

            // TODO: Move all light unit icons from the package into the built-in resources.
            public static Texture2D BlueSky             = GetLightUnitIcon("BlueSky");
            public static Texture2D Shade               = GetLightUnitIcon("Shade");
            public static Texture2D CloudySky           = GetLightUnitIcon("CloudySky");
            public static Texture2D DirectSunlight      = GetLightUnitIcon("DirectSunlight");
            public static Texture2D Fluorescent         = GetLightUnitIcon("Fluorescent");
            public static Texture2D IntenseAreaLight    = GetLightUnitIcon("IntenseAreaLight");
            public static Texture2D Candlelight         = GetLightUnitIcon("Candlelight");
        }

        /// <summary>
        /// Distributions for light units
        /// </summary>
        public static class CoreLightUnitSliderDistributions
        {
            private const float ExposureStep = 1 / 6f;
            public static readonly float[] ExposureDistribution =
            {
                0 * ExposureStep,
                1 * ExposureStep,
                2 * ExposureStep,
                3 * ExposureStep,
                4 * ExposureStep,
                5 * ExposureStep,
                6 * ExposureStep
            };
        }

        private static class CoreLightUnitTooltips
        {
            // Caution
            public const string k_TemperatureCaution   = "";

            // Temperature
            public const string k_TemperatureBlueSky        = "Blue Sky";
            public const string k_TemperatureShade          = "Shade (Clear Sky)";
            public const string k_TemperatureCloudySky      = "Cloudy Skylight";
            public const string k_TemperatureDirectSunlight = "Direct Sunlight";
            public const string k_TemperatureFluorescent    = "Fluorescent Light";
            public const string k_TemperatureIncandescent   = "Incandescent Light";
            public const string k_TemperatureCandle         = "Candlelight";
        }
    }
}
