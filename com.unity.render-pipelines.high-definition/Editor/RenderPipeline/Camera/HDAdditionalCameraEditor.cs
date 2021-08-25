using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HDAdditionalCameraData))]
    class HDAdditionalCameraDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }

        [MenuItem("CONTEXT/HDAdditionalCameraData/Remove Component")]
        static void RemoveComponent(MenuCommand command)
        {
            EditorUtility.DisplayDialog("Component Info", "You can not delete this component, you will have to remove the camera.", "OK");
        }
    }
}
