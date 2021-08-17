using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Formats the provided descriptor into a piece-wise linear slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class PiecewiseLightUnitSlider : CoreLightUnitSlider
    {
        struct Piece
        {
            public Vector2 domain;
            public Vector2 range;

            public float directM;
            public float directB;
            public float inverseM;
            public float inverseB;
        }

        // Piecewise function indexed by value ranges.
        private readonly Dictionary<Vector2, Piece> m_PiecewiseFunctionMap = new Dictionary<Vector2, Piece>();

        static void ComputeTransformationParameters(float x0, float x1, float y0, float y1, out float m, out float b)
        {
            m = (y0 - y1) / (x0 - x1);
            b = (m * -x0) + y0;
        }

        static float DoTransformation(in float x, in float m, in float b) => (m * x) + b;

        // Ensure clamping to (0,1) as sometimes the function evaluates to slightly below 0 (breaking the handle).
        static float ValueToSlider(Piece p, float x) => Mathf.Clamp01(DoTransformation(x, p.inverseM, p.inverseB));
        static float SliderToValue(Piece p, float x) => DoTransformation(x, p.directM, p.directB);

        // Ideally we want a continuous, monotonically increasing function, but this is useful as we can easily fit a
        // distribution to a set of (huge) value ranges onto a slider.
        public PiecewiseLightUnitSlider(CoreLightUnitSliderUIDescriptor descriptor) : base(descriptor)
        {
            // Sort the ranges into ascending order
            var sortedRanges = m_Descriptor.valueRanges.OrderBy(x => x.value.x).ToArray();
            var sliderDistribution = m_Descriptor.sliderDistribution;

            // Compute the transformation for each value range.
            for (int i = 0; i < sortedRanges.Length; i++)
            {
                var r = sortedRanges[i].value;

                var x0 = sliderDistribution[i + 0];
                var x1 = sliderDistribution[i + 1];
                var y0 = r.x;
                var y1 = r.y;

                Piece piece;
                piece.domain = new Vector2(x0, x1);
                piece.range  = new Vector2(y0, y1);

                ComputeTransformationParameters(x0, x1, y0, y1, out piece.directM, out piece.directB);

                // Compute the inverse
                ComputeTransformationParameters(y0, y1, x0, x1, out piece.inverseM, out piece.inverseB);

                m_PiecewiseFunctionMap.Add(sortedRanges[i].value, piece);
            }
        }

        protected override float GetPositionOnSlider(float value, Vector2 valueRange)
        {
            if (!m_PiecewiseFunctionMap.TryGetValue(valueRange, out var piecewise))
                return -1f;

            return ValueToSlider(piecewise, value);
        }

        // Search for the corresponding piece-wise function to a value on the domain and update the input piece to it.
        // Returns true if search was successful and an update was made, false otherwise.
        bool UpdatePiece(ref Piece piece, float x)
        {
            foreach (var pair in m_PiecewiseFunctionMap)
            {
                var p = pair.Value;

                if (x >= p.domain.x && x <= p.domain.y)
                {
                    piece = p;

                    return true;
                }
            }

            return false;
        }

        void SliderOutOfBounds(Rect rect, ref float value)
        {
            EditorGUI.BeginChangeCheck();
            var internalValue = GUI.HorizontalSlider(rect, value, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                Piece p = new Piece();
                UpdatePiece(ref p, internalValue);
                value = SliderToValue(p, internalValue);
            }
        }

        protected override void DoSlider(Rect rect, ref float value, Vector2 sliderRange, Vector2 valueRange)
        {
            // Map the internal slider value to the current piecewise function
            if (!m_PiecewiseFunctionMap.TryGetValue(valueRange, out var piece))
            {
                // Assume that if the piece is not found, that means the unit value is out of bounds.
                SliderOutOfBounds(rect, ref value);
                return;
            }

            // Maintain an internal value to support a single linear continuous function
            EditorGUI.BeginChangeCheck();
            var internalValue = GUI.HorizontalSlider(rect, ValueToSlider(piece, value), 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                // Ensure that the current function piece is being used to transform the value
                UpdatePiece(ref piece, internalValue);
                value = SliderToValue(piece, internalValue);
            }
        }
    }

    /// <summary>
    /// Formats the provided descriptor into a punctual light unit slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class PunctualLightUnitSlider : PiecewiseLightUnitSlider
    {
        public PunctualLightUnitSlider(CoreLightUnitSliderUIDescriptor descriptor) : base(descriptor) {}

        private SerializedHDLight m_Light;
        private Editor m_Editor;
        private LightUnit m_Unit;
        private bool m_SpotReflectorEnabled;

        // Note: these should be in sync with LightUnit
        private static string[] k_UnitNames =
        {
            "Lumen",
            "Candela",
            "Lux",
            "Nits",
            "EV",
        };

        public void Setup(LightUnit unit, SerializedHDLight light, Editor owner)
        {
            m_Unit = unit;
            m_Light = light;
            m_Editor = owner;

            // Cache the spot reflector state as we will need to revert back to it after treating the slider as point light.
            m_SpotReflectorEnabled = light.enableSpotReflector.boolValue;
        }

        public override void Draw(Rect rect, SerializedProperty value, ref float floatValue)
        {
            // Convert the incoming unit value into Lumen as the punctual slider is always in these terms (internally)
            float convertedValue = UnitToLumen(floatValue);

            EditorGUI.BeginChangeCheck();
            base.Draw(rect, value, ref convertedValue);
            if (EditorGUI.EndChangeCheck())
                floatValue = LumenToUnit(convertedValue);
        }

        protected override GUIContent GetLightUnitTooltip(string baseTooltip, float value, string unit)
        {
            // Convert the internal lumens into the actual light unit value
            value = LumenToUnit(value);
            unit = k_UnitNames[(int)m_Unit];

            return base.GetLightUnitTooltip(baseTooltip, value, unit);
        }

        float UnitToLumen(float value)
        {
            if (m_Unit == LightUnit.Lumen)
                return value;

            // Punctual slider currently does not have any regard for spot shape/reflector.
            // Conversions need to happen as if light is a point, and this is the only setting that influences that.
            m_Light.enableSpotReflector.boolValue = false;

            return HDLightUI.ConvertLightIntensity(m_Unit, LightUnit.Lumen, m_Light, m_Editor, value);
        }

        float LumenToUnit(float value)
        {
            if (m_Unit == LightUnit.Lumen)
                return value;

            // Once again temporarily disable reflector in case we called this for tooltip or context menu preset.
            m_Light.enableSpotReflector.boolValue = false;

            value = HDLightUI.ConvertLightIntensity(LightUnit.Lumen, m_Unit, m_Light, m_Editor, value);

            // Restore the state of spot reflector on the light.
            m_Light.enableSpotReflector.boolValue = m_SpotReflectorEnabled;

            return value;
        }

        protected override void SetValueToPreset(SerializedProperty value, CoreLightUnitSliderUIRange preset)
        {
            m_Light?.Update();

            // Convert to the actual unit value.
            value.floatValue = LumenToUnit(preset.presetValue);

            m_Light?.Apply();
        }
    }

    internal class LightUnitSliderUIDrawer
    {
        static PiecewiseLightUnitSlider k_DirectionalLightUnitSlider;
        static PunctualLightUnitSlider  k_PunctualLightUnitSlider;
        static PiecewiseLightUnitSlider k_ExposureSlider;

        static LightUnitSliderUIDrawer()
        {
            // Maintain a unique slider for directional/lux.
            k_DirectionalLightUnitSlider = new PiecewiseLightUnitSlider(LightUnitSliderDescriptors.LuxDescriptor);

            // Internally, slider is always in terms of lumens, so that the slider is uniform for all light units.
            k_PunctualLightUnitSlider = new PunctualLightUnitSlider(LightUnitSliderDescriptors.LumenDescriptor);

            // Exposure is in EV100, but we load a separate due to the different icon set.
            k_ExposureSlider = new PiecewiseLightUnitSlider(LightUnitSliderDescriptors.ExposureDescriptor);
        }

        // Need to cache the serialized object on the slider, to add support for the preset selection context menu (need to apply changes to serialized)
        // TODO: This slider drawer is getting kind of bloated. Break up the implementation into where it is actually used?
        public void SetSerializedObject(SerializedObject serializedObject)
        {
            k_DirectionalLightUnitSlider.SetSerializedObject(serializedObject);
            k_PunctualLightUnitSlider.SetSerializedObject(serializedObject);
            k_ExposureSlider.SetSerializedObject(serializedObject);
        }

        public void Draw(HDLightType type, LightUnit lightUnit, SerializedProperty value, Rect rect, SerializedHDLight light, Editor owner)
        {
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                if (type == HDLightType.Directional)
                    DrawDirectionalUnitSlider(value, rect);
                else
                    DrawPunctualLightUnitSlider(lightUnit, value, rect, light, owner);
            }
        }

        void DrawDirectionalUnitSlider(SerializedProperty value, Rect rect)
        {
            float val = value.floatValue;
            k_DirectionalLightUnitSlider.Draw(rect, value, ref val);
            if (val != value.floatValue)
                value.floatValue = val;
        }

        void DrawPunctualLightUnitSlider(LightUnit lightUnit, SerializedProperty value, Rect rect, SerializedHDLight light, Editor owner)
        {
            k_PunctualLightUnitSlider.Setup(lightUnit, light, owner);

            float val = value.floatValue;
            k_PunctualLightUnitSlider.Draw(rect, value, ref val);
            if (val != value.floatValue)
                value.floatValue = val;
        }

        public void DrawExposureSlider(SerializedProperty value, Rect rect)
        {
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                float val = value.floatValue;
                k_ExposureSlider.Draw(rect, value, ref val);
                if (val != value.floatValue)
                    value.floatValue = val;
            }
        }
    }
}
