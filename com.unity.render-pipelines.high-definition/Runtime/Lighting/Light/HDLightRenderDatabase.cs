using System;
using UnityEngine.Jobs;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.HighDefinition
{
    //Light rendering entity. This struct acts as a handle to set / get light render information into the database.
    internal struct HDLightRenderEntity
    {
        public int entityIndex;
        public static readonly HDLightRenderEntity Invalid = new HDLightRenderEntity() { entityIndex = HDLightRenderDatabase.InvalidDataIndex };
        public bool valid { get { return entityIndex != HDLightRenderDatabase.InvalidDataIndex; } }
    }

    //Data of the lights inside the database.
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

    //Class representing a rendering side database of lights in the world
    internal partial class HDLightRenderDatabase
    {
        #region internal HDRP API

        public static int InvalidDataIndex = -1;

        //total light count of all lights in the world.
        public int lightCount => m_LightCount;

        //gets the list of render light data. Use lightCount to iterate over all the world light data.
        public NativeArray<HDLightRenderData> lightData => m_LightData;

        //gets the light positions of all the lights in the world. This list shares the same indexing as lightData
        public NativeArray<float3> lightPositions => m_LightPositions;

        //gets the list of render light entities handles. Use this entities to access or set light data indirectly.
        public NativeArray<HDLightRenderEntity> lightEntities => m_OwnerEntity;

        //Attachments in case the rendering pipeline uses game objects
        public HDAdditionalLightData[] hdAdditionalLightData => m_HDAdditionalLightData;
        public GameObject[] aovGameObjects => m_AOVGameObjects;
        public TransformAccessArray lightTransforms => m_LightTransforms;

        //Access of main instance
        static public HDLightRenderDatabase instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new HDLightRenderDatabase();
                return s_Instance;
            }
        }

        //Gets a data reference from an entity.
        public ref HDLightRenderData GetLightData(in HDLightRenderEntity entity) => ref GetLightData(m_LightEntities[entity.entityIndex].dataIndex);

        //Gets integer data reference from an entity.
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

        // Creates a light render entity.
        public HDLightRenderEntity CreateEntity()
        {
            LightEntityInfo newData = AllocateEntityData();

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
            return newLightEntity;
        }

        //Must be called by game object so we can gather all the information needed,
        //This will prep this system when dots lights come in.
        //Dots lights wont have to use this and instead will have to set this data on their own.
        public void AttachGameObjectData(
            HDLightRenderEntity entity,
            int instanceID,
            Transform transformObject,
            HDAdditionalLightData additionalLightData,
            GameObject aovGameObject)
        {
            if (!IsValid(entity))
                return;

            var entityInfo = m_LightEntities[entity.entityIndex];
            int dataIndex = entityInfo.dataIndex;
            if (dataIndex == InvalidDataIndex)
                return;

            entityInfo.lightInstanceID = instanceID;
            m_LightEntities[entity.entityIndex] = entityInfo;

            m_LightsToEntityItem.Add(entityInfo.lightInstanceID, entityInfo);
            m_LightTransforms[dataIndex] = transformObject;
            m_HDAdditionalLightData[dataIndex] = additionalLightData;
            m_AOVGameObjects[dataIndex] = aovGameObject;
            ++m_AttachedGameObjects;
        }

        // Destroys a light render entity.
        public void DestroyEntity(HDLightRenderEntity lightEntity)
        {
            Assert.IsTrue(IsValid(lightEntity));

            m_FreeIndices.Enqueue(lightEntity.entityIndex);
            LightEntityInfo entityData = m_LightEntities[lightEntity.entityIndex];
            m_LightsToEntityItem.Remove(entityData.lightInstanceID);

            if (m_HDAdditionalLightData[entityData.dataIndex] != null)
                --m_AttachedGameObjects;

            RemoveAtSwapBackArrays(entityData.dataIndex);

            if (m_LightCount == 0)
            {
                DeleteArrays();
            }
            else
            {
                HDLightRenderEntity entityToUpdate = m_OwnerEntity[entityData.dataIndex];
                LightEntityInfo dataToUpdate = m_LightEntities[entityToUpdate.entityIndex];
                dataToUpdate.dataIndex = entityData.dataIndex;
                m_LightEntities[entityToUpdate.entityIndex] = dataToUpdate;
                if (dataToUpdate.lightInstanceID != entityData.lightInstanceID)
                    m_LightsToEntityItem[dataToUpdate.lightInstanceID] = dataToUpdate;
            }
        }

        //Must be called at the destruction of the rendering pipeline to delete all the internal buffers.
        public void Cleanup()
        {
            m_DefaultLightEntity = HDLightRenderEntity.Invalid;
            HDUtils.s_DefaultHDAdditionalLightData.DestroyHDLightRenderEntity();
            HDVisibleLightEntities.Cleanup();
        }

        // Gets a default entity. This is mostly used to be compatible with things like shuriken lights, which lack a proper HDAdditionalLightData
        public HDLightRenderEntity GetDefaultLightEntity()
        {
            if (!IsValid(m_DefaultLightEntity))
            {
                HDUtils.s_DefaultHDAdditionalLightData.CreateHDLightRenderEntity();
                m_DefaultLightEntity = HDUtils.s_DefaultHDAdditionalLightData.lightEntity;
            }

            return m_DefaultLightEntity;
        }

        // Returns true / false wether the entity has been destroyed or not.
        public bool IsValid(HDLightRenderEntity entity)
        {
            return entity.valid && entity.entityIndex < m_LightEntities.Count;
        }

        // Returns the index in data of an entity. Use this index to access lightData and lightPositions.
        // If the entity is invalid, it returns InvalidDataIndex
        public int GetEntityDataIndex(HDLightRenderEntity entity) => GetEntityData(entity).dataIndex;

        // Returns the index in data of an entity. Use this index to access lightData and lightPositions.
        // If the entity is invalid, it returns InvalidDataIndex
        public int FindEntityDataIndex(in VisibleLight visibleLight) => FindEntityDataIndex(visibleLight.light);

        // Returns the index in data of an entity. Use this index to access lightData and lightPositions.
        // If the entity is invalid, it returns InvalidDataIndex
        public int FindEntityDataIndex(in Light light)
        {
            if (light != null && m_LightsToEntityItem.TryGetValue(light.GetInstanceID(), out var foundEntity))
                return foundEntity.dataIndex;

            return -1;
        }

        #endregion

        #region private definitions 

        // Intermediate struct which holds the data index of an entity and other information.
        private struct LightEntityInfo
        {
            public int dataIndex;
            public int lightInstanceID;
            public static readonly LightEntityInfo Invalid = new LightEntityInfo() { dataIndex = InvalidDataIndex, lightInstanceID = -1 };
            public bool valid { get { return dataIndex != -1 && lightInstanceID != -1; } }
        }

        #endregion

        #region private members
        private const int ArrayCapacity = 100;
        private static HDLightRenderDatabase s_Instance = null;

        private int m_Capacity = 0;
        private int m_LightCount = 0;
        private int m_AttachedGameObjects = 0;
        private HDLightRenderEntity m_DefaultLightEntity = HDLightRenderEntity.Invalid;

        private List<LightEntityInfo> m_LightEntities = new List<LightEntityInfo>();
        private Queue<int> m_FreeIndices = new Queue<int>();
        private Dictionary<int, LightEntityInfo> m_LightsToEntityItem = new Dictionary<int, LightEntityInfo>();

        private NativeArray<HDLightRenderData> m_LightData;
        private NativeArray<HDLightRenderEntity> m_OwnerEntity;
        private NativeArray<float3> m_LightPositions;

        //TODO: Hack array just used for shadow allocation. Need to refactor this so we dont depend on hdAdditionalData
        private GameObject[] m_AOVGameObjects = null;
        private HDAdditionalLightData[] m_HDAdditionalLightData = null;
        private TransformAccessArray m_LightTransforms;

        #endregion

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

        private void DeleteArrays()
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

        private LightEntityInfo GetEntityData(HDLightRenderEntity entity)
        {
            Assert.IsTrue(IsValid(entity));
            return m_LightEntities[entity.entityIndex];
        }

        private LightEntityInfo AllocateEntityData()
        {
            if (m_Capacity == 0 || m_LightCount == m_Capacity)
            {
                m_Capacity = Math.Max(Math.Max(m_Capacity * 2, m_LightCount), ArrayCapacity);
                ResizeArrays();
            }

            int newIndex = m_LightCount++;
            LightEntityInfo newDataIndex = new LightEntityInfo { dataIndex = newIndex, lightInstanceID = -1 };
            m_LightTransforms.Add(null);
            return newDataIndex;
        }

        ~HDLightRenderDatabase()
        {
            DeleteArrays();
        }
    }
}
