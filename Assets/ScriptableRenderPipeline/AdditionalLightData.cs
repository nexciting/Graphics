﻿namespace UnityEngine.Experimental.Rendering
{
    public enum LightArchetype { Punctual, Area, Projector };

    //@TODO: We should continuously move these values
    // into the engine when we can see them being generally useful
    [RequireComponent(typeof(Light))]
    public class AdditionalLightData : MonoBehaviour
    {
        public const int DefaultShadowResolution = 512;

        public int shadowResolution = DefaultShadowResolution;

        public static int GetShadowResolution(AdditionalLightData lightData)
        {
            if (lightData != null)
                return lightData.shadowResolution;
            else
                return DefaultShadowResolution;
        }

        [Range(0.0F, 100.0F)]
        public float m_innerSpotPercent = 0.0f; // To display this field in the UI this need to be public

        public float GetInnerSpotPercent01()
        {
            return Mathf.Clamp(m_innerSpotPercent, 0.0f, 100.0f) / 100.0f;
        }

        [Range(0.0F, 1.0F)]
        public float shadowDimmer = 1.0f;
        [Range(0.0F, 1.0F)]
        public float lightDimmer = 1.0f;

        // Not used for directional lights.
        public float fadeDistance = 10000.0f;
        public float shadowFadeDistance = 10000.0f;

        public bool affectDiffuse = true;
        public bool affectSpecular = true;

        public LightArchetype archetype = LightArchetype.Punctual;
        public bool isDoubleSided = false; // Rectangular area lights only

        [Range(0.0f, 20.0f)]
        public float lightLength = 0.0f; // Area & projector lights

        [Range(0.0f, 20.0f)]
        public float lightWidth  = 0.0f; // Area & projector lights

        // shadow related parameters
        [System.Serializable]
        public struct ShadowData
        {
            public int   format;
            public int[] data;
        };

        [HideInInspector, SerializeField] private int           shadowAlgorithm;
        [HideInInspector, SerializeField] private int           shadowVariant;
        [HideInInspector, SerializeField] private int           shadowPrecision;
        [HideInInspector, SerializeField] private ShadowData    shadowData;
        [HideInInspector, SerializeField] private ShadowData[]  shadowDatas = new ShadowData[0];

        public void GetShadowAlgorithm( out int algorithm, out int variant, out int precision )    { algorithm = shadowAlgorithm; variant = shadowVariant; precision = shadowPrecision; }
        public void SetShadowAlgorithm( int algorithm, int variant, int precision, int format, int[] data )
        {
            shadowAlgorithm   = algorithm;
            shadowVariant     = variant;
            shadowPrecision   = precision;
            shadowData.format = format;
            shadowData.data   = data;

            int idx = FindShadowData( format );
            if( idx < 0 )
            {
                idx = shadowDatas.Length;
                ShadowData[] tmp = new ShadowData[idx+1];
                for( int i = 0; i < idx; ++i )
                    tmp[i] = shadowDatas[i];
                shadowDatas = tmp;
            }
            shadowDatas[idx].format = format;
            shadowDatas[idx].data   = data != null ? data : new int[0];
        }
        // Load a specific shadow data. Returns null if requested data is not present.
        public int[] GetShadowData( int shadowDataFormat )
        {
            if( shadowData.format == shadowDataFormat )
                return shadowData.data;

            int idx = FindShadowData( shadowDataFormat );
            return idx >= 0 ? shadowDatas[idx].data : null;
        }
        // Returns the currently set shadow data and format. Can return null.
        public int[] GetShadowData( out int shadowDataFormat )
        {
            shadowDataFormat = shadowData.format;
            return shadowData.data;
        }
        public void CompactShadowData()
        {
            shadowDatas = new ShadowData[0];
            UnityEditor.EditorUtility.SetDirty(this);
        }
        private int FindShadowData( int shadowDataFormat )
        {
            for( int i = 0; i < shadowDatas.Length; ++i )
            {
                if( shadowDatas[i].format == shadowDataFormat )
                    return i;
            }
            return -1;
        }
    }

    [UnityEditor.CustomEditor(typeof(AdditionalLightData))]
    [UnityEditor.CanEditMultipleObjects]
    public class AdditionalLightDataEditor : UnityEditor.Editor
    {
        static ShadowRegistry m_ShadowRegistry;

#pragma warning disable 414 // CS0414 The private field '...' is assigned but its value is never used
        UnityEditor.SerializedProperty m_ShadowAlgorithm;
        UnityEditor.SerializedProperty m_ShadowVariant;
        UnityEditor.SerializedProperty m_ShadowData;
        UnityEditor.SerializedProperty m_ShadowDatas;
#pragma warning restore 414

        public static void SetRegistry( ShadowRegistry registry ) { m_ShadowRegistry = registry; }

        void OnEnable()
        {
            m_ShadowAlgorithm = serializedObject.FindProperty( "shadowAlgorithm" );
            m_ShadowVariant   = serializedObject.FindProperty( "shadowVariant" );
            m_ShadowData      = serializedObject.FindProperty( "shadowData" );
            m_ShadowDatas     = serializedObject.FindProperty( "shadowDatas" );
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if( m_ShadowRegistry == null )
                return;

            AdditionalLightData ald = (AdditionalLightData) target;
            if( ald == null )
                return;

            UnityEditor.EditorGUI.BeginChangeCheck();
            m_ShadowRegistry.Draw( ald.gameObject.GetComponent<Light>() );
            serializedObject.Update();
            if( UnityEditor.EditorGUI.EndChangeCheck() )
            {
                UnityEditor.EditorUtility.SetDirty( ald );
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                UnityEditor.SceneView.RepaintAll();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
