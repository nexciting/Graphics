using System;
using UnityEngine.Jobs;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.HighDefinition
{
    //Internal entity. An index to an array with the Light instance ID
    internal struct HDLightRenderEntity
    {
        public int entityIndex;
        public static readonly HDLightRenderEntity Invalid = new HDLightRenderEntity() { entityIndex = -1 };
        public bool valid { get { return entityIndex != -1; } }
    }

    internal struct HDLightRenderEntityData
    {
        public int dataIndex;
        public int lightInstanceID;
        public static readonly HDLightRenderEntityData Invalid = new HDLightRenderEntityData() { dataIndex = -1, lightInstanceID = -1 };
        public bool valid { get { return dataIndex != -1 && lightInstanceID != -1; } }
    }

    internal struct HDLightRenderData
    {
        public HDAdditionalLightData.PointLightHDType pointLightType;
        public SpotLightShape spotLightShape;
        public AreaLightShape areaLightShape;
        public LightLayerEnum lightLayer;
        public float fadeDistance;
        public float distance;
        public float angularDiameter;
        public float volumetricFadeDistance;
        public bool includeForRayTracing;
        public bool useScreenSpaceShadows;
        public bool useRayTracedShadows;
        public bool colorShadow;
        public float lightDimmer;
        public float volumetricDimmer;
        public float shadowDimmer;
        public float shadowFadeDistance;
        public float volumetricShadowDimmer;
        public float shapeWidth;
        public float shapeHeight;
        public float aspectRatio;
        public float innerSpotPercent;
        public float spotIESCutoffPercent;
        public float shapeRadius;
        public float barnDoorLength;
        public float barnDoorAngle;
        public float flareSize;
        public float flareFalloff;
        public bool affectDiffuse;
        public bool affectSpecular;
        public bool applyRangeAttenuation;
        public bool penumbraTint;
        public bool interactsWithSky;
        public Color surfaceTint;
        public Color shadowTint;
        public Color flareTint;
    }

    //Internal class with SoA for a pool of lights
    //Class representing a pool of lights in the world
    internal partial class HDLightRenderDatabase
    {
        private static HDLightRenderDatabase s_Instance = null;

        static public HDLightRenderDatabase instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new HDLightRenderDatabase();
                return s_Instance;
            }
        }

        public const int ArrayCapacity = 100;
        private int m_Capacity = 0;
        private int m_LightCount = 0;
        private HDLightRenderEntity m_DefaultLightEntity = HDLightRenderEntity.Invalid;

        List<HDLightRenderEntityData> m_LightEntities = new List<HDLightRenderEntityData>();
        Queue<int> m_FreeIndices = new Queue<int>();

        #region Structure of arrays for lights
        private GameObject[] m_AOVGameObjects = null;

        //TODO: Hack array just used for shadow allocation. Need to refactor this so we dont depend on hdAdditionalData
        private HDAdditionalLightData[] m_HDAdditionalLightData = null;
        private TransformAccessArray m_LightTransforms;
        private NativeArray<HDLightRenderData> m_LightData;
        private NativeArray<HDLightRenderEntity> m_OwnerEntity;
        private NativeArray<float3> m_LightPositions;

        private void ResizeArrays()
        {
            if (m_HDAdditionalLightData == null)
                m_HDAdditionalLightData = new HDAdditionalLightData[m_Capacity];
            else
                Array.Resize(ref m_HDAdditionalLightData, m_Capacity);

            if (m_AOVGameObjects == null)
                m_AOVGameObjects = new GameObject[m_Capacity];
            else
                Array.Resize(ref m_AOVGameObjects, m_Capacity);

            if (!m_LightTransforms.isCreated)
                m_LightTransforms = new TransformAccessArray(m_Capacity);
            else
                m_LightTransforms.ResizeArray(m_Capacity);

            m_LightData.ResizeArray(m_Capacity);
            m_OwnerEntity.ResizeArray(m_Capacity);
            m_LightPositions.ResizeArray(m_Capacity);
        }

        private void RemoveAtSwapBackArrays(int removeIndexAt)
        {
            int lastIndex = m_LightCount - 1;
            m_HDAdditionalLightData[removeIndexAt] = m_HDAdditionalLightData[lastIndex];
            m_HDAdditionalLightData[lastIndex] = null;

            m_AOVGameObjects[removeIndexAt] = m_AOVGameObjects[lastIndex];
            m_AOVGameObjects[lastIndex] = null;

            m_LightTransforms.RemoveAtSwapBack(removeIndexAt);
            m_LightData[removeIndexAt] = m_LightData[lastIndex];
            m_OwnerEntity[removeIndexAt] = m_OwnerEntity[lastIndex];
            m_LightPositions[removeIndexAt] = m_LightPositions[lastIndex];

            --m_LightCount;
            Assert.AreEqual(m_LightTransforms.length, m_LightCount, "Inconsistent sizes of internal SoAs for lights");
        }

        public void DeleteArrays()
        {
            if (m_Capacity == 0)
                return;

            m_HDAdditionalLightData = null;
            m_AOVGameObjects = null;
            m_LightTransforms.Dispose();
            m_LightData.Dispose();
            m_OwnerEntity.Dispose();
            m_LightPositions.Dispose();

            m_FreeIndices.Clear();
            m_LightEntities.Clear();

            m_Capacity = 0;
        }

        public int lightCount => m_LightCount;

        public NativeArray<HDLightRenderData> lightData => m_LightData;
        public NativeArray<HDLightRenderEntity> lightEntities => m_OwnerEntity;
        public HDAdditionalLightData[] hdAdditionalLightData => m_HDAdditionalLightData;
        public GameObject[] aovGameObjects => m_AOVGameObjects;
        public TransformAccessArray lightTransforms => m_LightTransforms;
        public NativeArray<float3> lightPositions => m_LightPositions;

        public ref HDLightRenderData GetLightData(in HDLightRenderEntity entity)
        {
            int dataIndex = m_LightEntities[entity.entityIndex].dataIndex;
            return ref GetLightData(dataIndex);
        }

        public ref HDLightRenderData GetLightData(int dataIndex)
        {
            if (dataIndex >= m_LightCount)
                throw new Exception("Entity passed in is out of bounds. Index requested " + dataIndex + " and maximum length is " + m_LightCount);

            unsafe
            {
                HDLightRenderData* data = (HDLightRenderData*)m_LightData.GetUnsafePtr<HDLightRenderData>() + dataIndex;
                return ref UnsafeUtility.AsRef<HDLightRenderData>(data);
            }
        }

        public void UpdateHDAdditionalLightData(in HDLightRenderEntity entity, HDAdditionalLightData val) { m_HDAdditionalLightData[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateAOVGameObject(in HDLightRenderEntity entity, GameObject val) { m_AOVGameObjects[m_LightEntities[entity.entityIndex].dataIndex] = val; }

        #endregion


        private Dictionary<int, HDLightRenderEntityData> m_LightsToEntityItem = new Dictionary<int, HDLightRenderEntityData>();

        public void Cleanup()
        {
            m_DefaultLightEntity = HDLightRenderEntity.Invalid;
            HDUtils.s_DefaultHDAdditionalLightData.DestroyHDLightRenderEntity();
            HDVisibleLightEntities.Cleanup();
        }

        public HDLightRenderEntity GetDefaultLightEntity()
        {
            if (!IsValid(m_DefaultLightEntity))
            {
                HDUtils.s_DefaultHDAdditionalLightData.CreateHDLightRenderEntity();
                m_DefaultLightEntity = HDUtils.s_DefaultHDAdditionalLightData.lightEntity;
            }

            return m_DefaultLightEntity;
        }

        public bool IsValid(HDLightRenderEntity entity)
        {
            return entity.valid && entity.entityIndex < m_LightEntities.Count;
        }

        public HDLightRenderEntityData GetEntityData(HDLightRenderEntity entity)
        {
            Assert.IsTrue(IsValid(entity));
            return m_LightEntities[entity.entityIndex];
        }

        public int GetEntityDataIndex(HDLightRenderEntity entity) => GetEntityData(entity).dataIndex;

        public HDLightRenderEntityData FindEntity(in VisibleLight visibleLight) => FindEntity(visibleLight.light);
        public HDLightRenderEntityData FindEntity(in Light light)
        {
            if (light != null && m_LightsToEntityItem.TryGetValue(light.GetInstanceID(), out var foundEntity))
                return foundEntity;

            return HDLightRenderEntityData.Invalid;
        }

        public HDLightRenderEntity CreateEntity(int instanceID, Transform transformObject)
        {
            HDLightRenderEntityData newData = AllocateEntityData(instanceID, transformObject);

            HDLightRenderEntity newLightEntity = HDLightRenderEntity.Invalid;
            if (m_FreeIndices.Count == 0)
            {
                newLightEntity.entityIndex = m_LightEntities.Count;
                m_LightEntities.Add(newData);
            }
            else
            {
                newLightEntity.entityIndex = m_FreeIndices.Dequeue();
                m_LightEntities[newLightEntity.entityIndex] = newData;
            }

            m_OwnerEntity[newData.dataIndex] = newLightEntity;
            m_LightsToEntityItem.Add(newData.lightInstanceID, newData);

            return newLightEntity;
        }

        public void DestroyEntity(HDLightRenderEntity lightEntity)
        {
            Assert.IsTrue(IsValid(lightEntity));

            m_FreeIndices.Enqueue(lightEntity.entityIndex);
            HDLightRenderEntityData entityData = m_LightEntities[lightEntity.entityIndex];
            m_LightsToEntityItem.Remove(entityData.lightInstanceID);

            RemoveAtSwapBackArrays(entityData.dataIndex);

            if (m_LightCount == 0)
            {
                DeleteArrays();
            }
            else
            {
                HDLightRenderEntity entityToUpdate = m_OwnerEntity[entityData.dataIndex];
                HDLightRenderEntityData dataToUpdate = m_LightEntities[entityToUpdate.entityIndex];
                dataToUpdate.dataIndex = entityData.dataIndex;
                m_LightEntities[entityToUpdate.entityIndex] = dataToUpdate;
                if (dataToUpdate.lightInstanceID != entityData.lightInstanceID)
                    m_LightsToEntityItem[dataToUpdate.lightInstanceID] = dataToUpdate;
            }
        }

        private HDLightRenderEntityData AllocateEntityData(int instanceID, Transform transformObject)
        {
            if (m_Capacity == 0 || m_LightCount == m_Capacity)
            {
                m_Capacity = Math.Max(Math.Max(m_Capacity * 2, m_LightCount), ArrayCapacity);
                ResizeArrays();
            }

            int newIndex = m_LightCount++;
            HDLightRenderEntityData newDataIndex = new HDLightRenderEntityData { dataIndex = newIndex, lightInstanceID = instanceID };
            m_LightTransforms.Add(transformObject);
            return newDataIndex;
        }

        ~HDLightRenderDatabase()
        {
            DeleteArrays();
        }
    }
}
