using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Contains a set of methods to help render the inspectors of Lights across SRP's
    /// </summary>
    public partial class LightUI
    {
        static class Styles
        {
            // in casse that you want to keep the indentation but have nothing to write
            public static readonly GUIContent empty = EditorGUIUtility.TrTextContent(" ");

            // Emission
            public static readonly GUIContent color = EditorGUIUtility.TrTextContent("Color", "Specifies the color this Light emits.");
            public static readonly GUIContent lightAppearance = EditorGUIUtility.TrTextContent("Light Appearance", "Specifies the mode for this Light's color is calculated.");
            public static readonly GUIContent[] lightAppearanceOptions = new[]
            {
                EditorGUIUtility.TrTextContent("Color"),
                EditorGUIUtility.TrTextContent("Filter and Temperature")
            };
            public static readonly GUIContent[] lightAppearanceUnits = new[]
            {
                EditorGUIUtility.TrTextContent("Kelvin")
            };
            public static readonly GUIContent colorFilter = EditorGUIUtility.TrTextContent("Filter", "Specifies a color which tints the Light source.");
            public static readonly GUIContent colorTemperature = EditorGUIUtility.TrTextContent("Temperature", "Specifies a temperature (in Kelvin) used to correlate a color for the Light. For reference, White is 6500K.");
        }
    }
}
