using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Rendering.Universal
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UniversalAdditionalLightData))]
    public class UniversalAdditionLightDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }

        [MenuItem("CONTEXT/UniversalAdditionalLightData/Remove Component")]
        static void RemoveAdditionalComponent(MenuCommand command)
        {
            EditorUtility.DisplayDialog("Component Info", "You can not delete this component, you will have to remove the light.", "OK");
        }
    }
}
