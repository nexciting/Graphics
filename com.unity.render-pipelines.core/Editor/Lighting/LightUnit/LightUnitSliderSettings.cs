using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Descriptors for Light Units
    /// </summary>
    public struct LightUnitSliderUIDescriptor
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="valueRanges">The values</param>
        /// <param name="sliderDistribution">The distribution</param>
        /// <param name="cautionTooltip">The caution tooltip</param>
        /// <param name="unitName">The name of the units</param>
        /// <param name="hasMarkers">If has marker</param>
        /// <param name="clampValue">The value to clamp</param>
        public LightUnitSliderUIDescriptor(LightUnitSliderUIRange[] valueRanges, float[] sliderDistribution,
                                           string cautionTooltip, string unitName, bool hasMarkers = true, bool clampValue = false)
            : this(valueRanges, sliderDistribution, cautionTooltip, cautionTooltip, unitName, hasMarkers, clampValue)
        {}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="valueRanges">The values</param>
        /// <param name="sliderDistribution">The distribution</param>
        /// <param name="belowRangeTooltip">The tooltip if the value is below the range</param>
        /// <param name="aboveRangeTooltip">The tooltip if the value is above the range</param>
        /// <param name="unitName">The name of the units</param>
        /// <param name="hasMarkers">If has marker</param>
        /// <param name="clampValue">The value to clamp</param>
        public LightUnitSliderUIDescriptor(LightUnitSliderUIRange[] valueRanges, float[] sliderDistribution, string belowRangeTooltip,
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

        /// <summary>
        /// The array of the distribution
        /// </summary>
        public readonly float[] sliderDistribution;
        /// <summary>
        /// The value ranges of the UI
        /// </summary>
        public readonly LightUnitSliderUIRange[] valueRanges;
        /// <summary>
        /// The ranges (min, max)
        /// </summary>
        public readonly Vector2 sliderRange;

        /// <summary>
        /// Tooltip shown when the value is bellow the range
        /// </summary>
        public readonly string belowRangeTooltip;
        /// <summary>
        /// Tooltip shown when the value is above the range
        /// </summary>
        public readonly string aboveRangeTooltip;
        /// <summary>
        /// The name of the unit
        /// </summary>
        public readonly string unitName;
        /// <summary>
        /// If has marker or not
        /// </summary>
        public readonly bool hasMarkers;
        /// <summary>
        /// The value to clamp the setting
        /// </summary>
        public readonly bool clampValue;
    }

    /// <summary>
    /// Range for Light Unit Slider
    /// </summary>
    public struct LightUnitSliderUIRange
    {
        /// <summary>
        /// Constructs this range
        /// </summary>
        /// <param name="icon"><see cref="Texture2D"/>The icon</param>
        /// <param name="tooltip">The tooltip</param>
        /// <param name="value"><see cref="Vector2"/>The range of this slider</param>
        public LightUnitSliderUIRange(Texture2D icon, string tooltip, Vector2 value)
        // If no preset value provided, then by default it is the average of the value range.
            : this(icon, tooltip, value, 0.5f * (value.x + value.y))
        {}

        /// <summary>
        /// Constructs this range
        /// </summary>
        /// <param name="icon"><see cref="Texture2D"/>The icon</param>
        /// <param name="tooltip">The tooltip</param>
        /// <param name="value"><see cref="Vector2"/>The range of this slider</param>
        /// <param name="presetValue">Preset values are arbitrarily chosen by artist</param>
        public LightUnitSliderUIRange(Texture2D icon, string tooltip, Vector2 value, float presetValue)
        {
            this.content = new GUIContent(icon, tooltip);
            this.value = value;

            Debug.Assert(presetValue > value.x && presetValue < value.y, "Preset value is outside the slider value range.");

            // Preset values are arbitrarily chosen by artist, and we must use it instead of
            // deriving it automatically (ie, the value range average).
            this.presetValue = presetValue;
        }

        /// <summary>
        /// Creates one range with given tooltip and max range
        /// </summary>
        /// <param name="icon"><see cref="Texture2D"/>The icon</param>
        /// <param name="tooltip">The tooltip</param>
        /// <param name="value"><see cref="Vector2"/>The range of this slider</param>
        /// <param name="presetValue">Preset values are arbitrarily chosen by artist</param>
        public static LightUnitSliderUIRange CautionRange(string tooltip, float value) => new LightUnitSliderUIRange
        {
            // Load the buildin caution icon with provided tooltip.
            content = EditorGUIUtility.TrIconContent(CoreEditorStyles.iconWarn, tooltip),
            value = new Vector2(-1, value),
            presetValue = -1
        };

        /// <summary>
        /// The label
        /// </summary>
        public GUIContent content;

        /// <summary>
        /// The range
        /// </summary>
        public Vector2    value;

        /// <summary>
        /// The preset value
        /// </summary>
        public float      presetValue;
    }


    /// <summary>
    /// Helper class to store the <see cref="LightUnitSliderUIDescriptor"/>
    /// </summary>
    public static class LightUnitSliderDescriptors
    {
        // Temperature
        internal static LightUnitSliderUIDescriptor TemperatureDescriptor = new LightUnitSliderUIDescriptor(
            LightUnitValueRanges.KelvinValueTableNew,
            LightUnitSliderDistributions.ExposureDistribution,
            LightUnitTooltips.k_TemperatureCaution,
            "Kelvin",
            false,
            true
        );

        private static class LightUnitValueRanges
        {
            public static readonly LightUnitSliderUIRange[] KelvinValueTableNew =
            {
                new LightUnitSliderUIRange(LightUnitIcon.BlueSky,          LightUnitTooltips.k_TemperatureBlueSky,          new Vector2(10000, 20000), 15000),
                new LightUnitSliderUIRange(LightUnitIcon.Shade,            LightUnitTooltips.k_TemperatureShade,            new Vector2(7000,  10000), 8000),
                new LightUnitSliderUIRange(LightUnitIcon.CloudySky,        LightUnitTooltips.k_TemperatureCloudySky,        new Vector2(6000,   7000), 6500),
                new LightUnitSliderUIRange(LightUnitIcon.DirectSunlight,   LightUnitTooltips.k_TemperatureDirectSunlight,   new Vector2(4500,   6000), 5500),
                new LightUnitSliderUIRange(LightUnitIcon.Fluorescent,      LightUnitTooltips.k_TemperatureFluorescent,      new Vector2(3500,   4500), 4000),
                new LightUnitSliderUIRange(LightUnitIcon.IntenseAreaLight, LightUnitTooltips.k_TemperatureIncandescent,     new Vector2(2500,   3500), 3000),
                new LightUnitSliderUIRange(LightUnitIcon.Candlelight,      LightUnitTooltips.k_TemperatureCandle,           new Vector2(1500,   2500), 1900),
            };
        }

        /// <summary>
        /// Light Unit Icons
        /// </summary>
        public static class LightUnitIcon
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
        public static class LightUnitSliderDistributions
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

        private static class LightUnitTooltips
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
