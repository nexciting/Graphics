using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HDAdditionalLightData))]
    class HDAdditionalLightDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }

        [MenuItem("CONTEXT/HDAdditionalLightData/Remove Component")]
        static void RemoveAdditionalComponent(MenuCommand command)
        {
            EditorUtility.DisplayDialog("Component Info", "You can not delete this component, you will have to remove the light.", "OK");
        }
    }
}
