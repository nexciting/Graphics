using System;
using UnityEngine.Jobs;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Rendering.HighDefinition
{
    //Internal entity. An index to an array with the Light instance ID
    internal struct HDLightEntity
    {
        public int entityIndex;
        public static readonly HDLightEntity Invalid = new HDLightEntity() { entityIndex = -1 };
        public bool valid { get { return entityIndex != -1; } }
    }

    internal struct HDLightEntityData
    {
        public int dataIndex;
        public int lightInstanceID;
        public static readonly HDLightEntityData Invalid = new HDLightEntityData() { dataIndex = -1, lightInstanceID = -1 };
        public bool valid { get { return dataIndex != -1 && lightInstanceID != -1; } }
    }

    //Internal class with SoA for a pool of lights
    //Class representing a pool of lights in the world
    internal partial class HDLightEntityCollection
    {
        private static HDLightEntityCollection s_Instance = null;

        static public HDLightEntityCollection instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new HDLightEntityCollection();
                return s_Instance;
            }
        }

        public const int ArrayCapacity = 100;
        private int m_Capacity = 0;
        private int m_LightCount = 0;
        private HDLightEntity m_DefaultLightEntity = HDLightEntity.Invalid;

        List<HDLightEntityData> m_LightEntities = new List<HDLightEntityData>();
        Queue<int> m_FreeIndices = new Queue<int>();

        #region Structure of arrays for lights
        private GameObject[] m_AOVGameObjects = null;

        //TODO: Hack array just used for shadow allocation. Need to refactor this so we dont depend on hdAdditionalData
        private HDAdditionalLightData[] m_HDAdditionalLightData = null;
        private TransformAccessArray m_LightTransforms;
        private NativeArray<HDLightEntity> m_OwnerEntity;
        private NativeArray<float3> m_LightPositions;
        private NativeArray<HDAdditionalLightData.PointLightHDType> m_PointLightType;
        private NativeArray<SpotLightShape> m_SpotLightShape;
        private NativeArray<AreaLightShape> m_AreaLightShape;
        private NativeArray<LightLayerEnum> m_LightLayers;
        private NativeArray<float> m_FadeDistances;
        private NativeArray<float> m_Distance;
        private NativeArray<float> m_AngularDiameter;
        private NativeArray<float> m_VolumetricFadeDistances;
        private NativeArray<bool> m_IncludeForRayTracings;
        private NativeArray<bool> m_UseScreenSpaceShadows;
        private NativeArray<bool> m_UseRayTracedShadows;
        private NativeArray<bool> m_ColorShadow;
        private NativeArray<float> m_LightDimmer;
        private NativeArray<float> m_VolumetricDimmer;
        private NativeArray<float> m_ShadowDimmer;
        private NativeArray<float> m_ShadowFadeDistance;
        private NativeArray<float> m_VolumetricShadowDimmer;
        private NativeArray<float> m_ShapeWidth;
        private NativeArray<float> m_ShapeHeight;
        private NativeArray<float> m_AspectRatio;
        private NativeArray<float> m_InnerSpotPercent;
        private NativeArray<float> m_SpotIESCutoffPercent;
        private NativeArray<float> m_ShapeRadius;
        private NativeArray<float> m_BarnDoorLength;
        private NativeArray<float> m_BarnDoorAngle;
        private NativeArray<float> m_FlareSize;
        private NativeArray<float> m_FlareFalloff;
        private NativeArray<bool> m_AffectDiffuse;
        private NativeArray<bool> m_AffectSpecular;
        private NativeArray<bool> m_ApplyRangeAttenuation;
        private NativeArray<bool> m_PenumbraTint;
        private NativeArray<bool> m_InteractsWithSky;
        private NativeArray<Color> m_SurfaceTint;
        private NativeArray<Color> m_ShadowTint;
        private NativeArray<Color> m_FlareTint;

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

            m_OwnerEntity.ResizeArray(m_Capacity);
            m_LightPositions.ResizeArray(m_Capacity);
            m_PointLightType.ResizeArray(m_Capacity);
            m_SpotLightShape.ResizeArray(m_Capacity);
            m_AreaLightShape.ResizeArray(m_Capacity);
            m_LightLayers.ResizeArray(m_Capacity);
            m_FadeDistances.ResizeArray(m_Capacity);
            m_Distance.ResizeArray(m_Capacity);
            m_AngularDiameter.ResizeArray(m_Capacity);
            m_VolumetricFadeDistances.ResizeArray(m_Capacity);
            m_IncludeForRayTracings.ResizeArray(m_Capacity);
            m_UseScreenSpaceShadows.ResizeArray(m_Capacity);
            m_UseRayTracedShadows.ResizeArray(m_Capacity);
            m_ColorShadow.ResizeArray(m_Capacity);
            m_LightDimmer.ResizeArray(m_Capacity);
            m_VolumetricDimmer.ResizeArray(m_Capacity);
            m_ShadowDimmer.ResizeArray(m_Capacity);
            m_ShadowFadeDistance.ResizeArray(m_Capacity);
            m_VolumetricShadowDimmer.ResizeArray(m_Capacity);
            m_ShapeWidth.ResizeArray(m_Capacity);
            m_ShapeHeight.ResizeArray(m_Capacity);
            m_AspectRatio.ResizeArray(m_Capacity);
            m_InnerSpotPercent.ResizeArray(m_Capacity);
            m_SpotIESCutoffPercent.ResizeArray(m_Capacity);
            m_ShapeRadius.ResizeArray(m_Capacity);
            m_BarnDoorLength.ResizeArray(m_Capacity);
            m_BarnDoorAngle.ResizeArray(m_Capacity);
            m_FlareSize.ResizeArray(m_Capacity);
            m_FlareFalloff.ResizeArray(m_Capacity);
            m_AffectDiffuse.ResizeArray(m_Capacity);
            m_AffectSpecular.ResizeArray(m_Capacity);
            m_ApplyRangeAttenuation.ResizeArray(m_Capacity);
            m_PenumbraTint.ResizeArray(m_Capacity);
            m_InteractsWithSky.ResizeArray(m_Capacity);
            m_SurfaceTint.ResizeArray(m_Capacity);
            m_ShadowTint.ResizeArray(m_Capacity);
            m_FlareTint.ResizeArray(m_Capacity);
        }

        private void RemoveAtSwapBackArrays(int removeIndexAt)
        {
            int lastIndex = m_LightCount - 1;
            m_HDAdditionalLightData[removeIndexAt] = m_HDAdditionalLightData[lastIndex];
            m_HDAdditionalLightData[lastIndex] = null;

            m_AOVGameObjects[removeIndexAt] = m_AOVGameObjects[lastIndex];
            m_AOVGameObjects[lastIndex] = null;

            m_LightTransforms.RemoveAtSwapBack(removeIndexAt);
            m_OwnerEntity[removeIndexAt] = m_OwnerEntity[lastIndex];
            m_LightPositions[removeIndexAt] = m_LightPositions[lastIndex];
            m_PointLightType[removeIndexAt] = m_PointLightType[lastIndex];
            m_SpotLightShape[removeIndexAt] = m_SpotLightShape[lastIndex];
            m_AreaLightShape[removeIndexAt] = m_AreaLightShape[lastIndex];
            m_LightLayers[removeIndexAt] = m_LightLayers[lastIndex];
            m_FadeDistances[removeIndexAt] = m_FadeDistances[lastIndex];
            m_Distance[removeIndexAt] = m_Distance[lastIndex];
            m_AngularDiameter[removeIndexAt] = m_FadeDistances[lastIndex];
            m_VolumetricFadeDistances[removeIndexAt] = m_VolumetricFadeDistances[lastIndex];
            m_IncludeForRayTracings[removeIndexAt] = m_IncludeForRayTracings[lastIndex];
            m_UseScreenSpaceShadows[removeIndexAt] = m_UseScreenSpaceShadows[lastIndex];
            m_UseRayTracedShadows[removeIndexAt] = m_UseRayTracedShadows[lastIndex];
            m_ColorShadow[removeIndexAt] = m_ColorShadow[lastIndex];
            m_LightDimmer[removeIndexAt] = m_LightDimmer[lastIndex];
            m_VolumetricDimmer[removeIndexAt] = m_VolumetricDimmer[lastIndex];
            m_ShadowDimmer[removeIndexAt] = m_ShadowDimmer[lastIndex];
            m_ShadowFadeDistance[removeIndexAt] = m_ShadowFadeDistance[lastIndex];
            m_VolumetricShadowDimmer[removeIndexAt] = m_VolumetricShadowDimmer[lastIndex];
            m_ShapeWidth[removeIndexAt] = m_ShapeWidth[lastIndex];
            m_ShapeHeight[removeIndexAt] = m_ShapeHeight[lastIndex];
            m_AspectRatio[removeIndexAt] = m_AspectRatio[lastIndex];
            m_InnerSpotPercent[removeIndexAt] = m_InnerSpotPercent[lastIndex];
            m_SpotIESCutoffPercent[removeIndexAt] = m_SpotIESCutoffPercent[lastIndex];
            m_ShapeRadius[removeIndexAt] = m_ShapeRadius[lastIndex];
            m_BarnDoorLength[removeIndexAt] = m_BarnDoorLength[lastIndex];
            m_BarnDoorAngle[removeIndexAt] = m_BarnDoorAngle[lastIndex];
            m_FlareSize[removeIndexAt] = m_FlareSize[lastIndex];
            m_FlareFalloff[removeIndexAt] = m_FlareSize[lastIndex];
            m_AffectDiffuse[removeIndexAt] = m_AffectDiffuse[lastIndex];
            m_AffectSpecular[removeIndexAt] = m_AffectSpecular[lastIndex];
            m_ApplyRangeAttenuation[removeIndexAt] = m_ApplyRangeAttenuation[lastIndex];
            m_PenumbraTint[removeIndexAt] = m_PenumbraTint[lastIndex];
            m_InteractsWithSky[removeIndexAt] = m_InteractsWithSky[lastIndex];
            m_SurfaceTint[removeIndexAt] = m_SurfaceTint[lastIndex];
            m_ShadowTint[removeIndexAt] = m_ShadowTint[lastIndex];
            m_FlareTint[removeIndexAt] = m_FlareTint[lastIndex];

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
            m_OwnerEntity.Dispose();
            m_LightPositions.Dispose();
            m_PointLightType.Dispose();
            m_SpotLightShape.Dispose();
            m_AreaLightShape.Dispose();
            m_LightLayers.Dispose();
            m_FadeDistances.Dispose();
            m_Distance.Dispose();
            m_AngularDiameter.Dispose();
            m_VolumetricFadeDistances.Dispose();
            m_IncludeForRayTracings.Dispose();
            m_UseScreenSpaceShadows.Dispose();
            m_UseRayTracedShadows.Dispose();
            m_ColorShadow.Dispose();
            m_LightDimmer.Dispose();
            m_VolumetricDimmer.Dispose();
            m_ShadowDimmer.Dispose();
            m_ShadowFadeDistance.Dispose();
            m_VolumetricShadowDimmer.Dispose();
            m_ShapeWidth.Dispose();
            m_ShapeHeight.Dispose();
            m_AspectRatio.Dispose();
            m_InnerSpotPercent.Dispose();
            m_SpotIESCutoffPercent.Dispose();
            m_ShapeRadius.Dispose();
            m_BarnDoorLength.Dispose();
            m_BarnDoorAngle.Dispose();
            m_FlareSize.Dispose();
            m_FlareFalloff.Dispose();
            m_AffectDiffuse.Dispose();
            m_AffectSpecular.Dispose();
            m_ApplyRangeAttenuation.Dispose();
            m_PenumbraTint.Dispose();
            m_InteractsWithSky.Dispose();
            m_SurfaceTint.Dispose();
            m_ShadowTint.Dispose();
            m_FlareTint.Dispose();

            m_FreeIndices.Clear();
            m_LightEntities.Clear();

            m_Capacity = 0;
        }

        public int lightCount => m_LightCount;

        public NativeArray<HDLightEntity> lightEntities => m_OwnerEntity;
        public HDAdditionalLightData[] hdAdditionalLightData => m_HDAdditionalLightData;
        public GameObject[] aovGameObjects => m_AOVGameObjects;
        public TransformAccessArray lightTransforms => m_LightTransforms;
        public NativeArray<float3> lightPositions => m_LightPositions;
        public NativeArray<HDAdditionalLightData.PointLightHDType> pointLightTypes => m_PointLightType;
        public NativeArray<SpotLightShape> spotLightShapes => m_SpotLightShape;
        public NativeArray<AreaLightShape> areaLightShapes => m_AreaLightShape;
        public NativeArray<LightLayerEnum> lightLayers => m_LightLayers;
        public NativeArray<float> fadeDistances => m_FadeDistances;
        public NativeArray<float> volumetricFadeDistances => m_VolumetricFadeDistances;
        public NativeArray<bool> includeForRayTracings => m_IncludeForRayTracings;
        public NativeArray<bool> useScreenSpaceShadows => m_UseScreenSpaceShadows;
        public NativeArray<bool> useRayTracedShadows => m_UseRayTracedShadows;
        public NativeArray<bool> colorShadow => m_ColorShadow;
        public NativeArray<float> lightDimmer => m_LightDimmer;
        public NativeArray<float> distance => m_Distance;
        public NativeArray<float> angularDiameter => m_AngularDiameter;
        public NativeArray<float> volumetricDimmer => m_VolumetricDimmer;
        public NativeArray<float> shadowDimmer => m_ShadowDimmer;
        public NativeArray<float> shadowFadeDistance => m_ShadowFadeDistance;
        public NativeArray<float> volumetricShadowDimmer => m_VolumetricShadowDimmer;
        public NativeArray<float> shapeWidth => m_ShapeWidth;
        public NativeArray<float> shapeHeight => m_ShapeHeight;
        public NativeArray<float> aspectRatio => m_AspectRatio;
        public NativeArray<float> innerSpotPercent => m_InnerSpotPercent;
        public NativeArray<float> spotIESCutoffPercent => m_SpotIESCutoffPercent;
        public NativeArray<float> shapeRadius => m_ShapeRadius;
        public NativeArray<float> barnDoorLength => m_BarnDoorLength;
        public NativeArray<float> barnDoorAngle => m_BarnDoorAngle;
        public NativeArray<float> flareSize => m_FlareSize;
        public NativeArray<float> flareFalloff => m_FlareFalloff;
        public NativeArray<bool> affectDiffuse => m_AffectDiffuse;
        public NativeArray<bool> affectSpecular => m_AffectSpecular;
        public NativeArray<bool> applyRangeAttenuation => m_ApplyRangeAttenuation;
        public NativeArray<bool> penumbraTint => m_PenumbraTint;
        public NativeArray<bool> interactsWithSky => m_InteractsWithSky;
        public NativeArray<Color> surfaceTint => m_SurfaceTint;
        public NativeArray<Color> shadowTint => m_ShadowTint;
        public NativeArray<Color> flareTint => m_FlareTint;

        public void UpdateHDAdditionalLightData(in HDLightEntity entity, HDAdditionalLightData val) { m_HDAdditionalLightData[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateAOVGameObject(in HDLightEntity entity, GameObject val) { m_AOVGameObjects[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdatePointLightType(in HDLightEntity entity, HDAdditionalLightData.PointLightHDType val) { m_PointLightType[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateSpotLightShape(in HDLightEntity entity, SpotLightShape val) { m_SpotLightShape[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateAreaLightShape(in HDLightEntity entity, AreaLightShape val) { m_AreaLightShape[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateLightLayer(in HDLightEntity entity, LightLayerEnum val) { m_LightLayers[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateFadeDistance(in HDLightEntity entity, float val) { m_FadeDistances[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateDistance(in HDLightEntity entity, float val) { m_Distance[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateAngularDiameter(in HDLightEntity entity, float val) { m_AngularDiameter[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateVolumetricFadeDistance(in HDLightEntity entity, float val) { m_VolumetricFadeDistances[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateIncludeForRayTracing(in HDLightEntity entity, bool val) { m_IncludeForRayTracings[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateUseScreenSpaceShadows(in HDLightEntity entity, bool val) { m_UseScreenSpaceShadows[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateUseRayTracedShadows(in HDLightEntity entity, bool val) { m_UseRayTracedShadows[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateColorShadow(in HDLightEntity entity, bool val) { m_ColorShadow[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateLightDimmer(in HDLightEntity entity, float val) { m_LightDimmer[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateVolumetricDimmer(in HDLightEntity entity, float val) { m_VolumetricDimmer[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateShadowDimmer(in HDLightEntity entity, float val) { m_ShadowDimmer[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateShapeWidth(in HDLightEntity entity, float val) { m_ShapeWidth[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateShapeHeight(in HDLightEntity entity, float val) { m_ShapeHeight[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateAspectRatio(in HDLightEntity entity, float val) { m_AspectRatio[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateInnerSpotPercent(in HDLightEntity entity, float val) { m_InnerSpotPercent[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateSpotIESCutoffPercent(in HDLightEntity entity, float val) { m_SpotIESCutoffPercent[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateShapeRadius(in HDLightEntity entity, float val) { m_ShapeRadius[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateBarnDoorLength(in HDLightEntity entity, float val) { m_BarnDoorLength[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateBarnDoorAngle(in HDLightEntity entity, float val) { m_BarnDoorAngle[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateFlareSize(in HDLightEntity entity, float val) { m_FlareSize[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateFlareFalloff(in HDLightEntity entity, float val) { m_FlareFalloff[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateShadowFadeDistance(in HDLightEntity entity, float val) { m_ShadowFadeDistance[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateVolumetricShadowDimmer(in HDLightEntity entity, float val) { m_VolumetricShadowDimmer[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateAffectDiffuse(in HDLightEntity entity, bool val) { m_AffectDiffuse[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateAffectSpecular(in HDLightEntity entity, bool val) { m_AffectSpecular[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateApplyRangeAttenuation(in HDLightEntity entity, bool val) { m_ApplyRangeAttenuation[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdatePenumbraTint(in HDLightEntity entity, bool val) { m_PenumbraTint[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateInteractsWithSky(in HDLightEntity entity, bool val) { m_InteractsWithSky[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateSurfaceTint(in HDLightEntity entity, in Color val) { m_SurfaceTint[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateShadowTint(in HDLightEntity entity, in Color val) { m_ShadowTint[m_LightEntities[entity.entityIndex].dataIndex] = val; }
        public void UpdateFlareTint(in HDLightEntity entity, in Color val) { m_FlareTint[m_LightEntities[entity.entityIndex].dataIndex] = val; }

        #endregion


        private Dictionary<int, HDLightEntityData> m_LightsToEntityItem = new Dictionary<int, HDLightEntityData>();

        public void Cleanup()
        {
            m_DefaultLightEntity = HDLightEntity.Invalid;
            HDUtils.s_DefaultHDAdditionalLightData.DestroyHDLightEntity();
            HDVisibleLightEntities.Cleanup();
        }

        public HDLightEntity GetDefaultLightEntity()
        {
            if (!IsValid(m_DefaultLightEntity))
            {
                HDUtils.s_DefaultHDAdditionalLightData.CreateHDLightEntity();
                m_DefaultLightEntity = HDUtils.s_DefaultHDAdditionalLightData.lightEntity;
            }

            return m_DefaultLightEntity;
        }

        public bool IsValid(HDLightEntity entity)
        {
            return entity.valid && entity.entityIndex < m_LightEntities.Count;
        }

        public HDLightEntityData GetEntityData(HDLightEntity entity)
        {
            Assert.IsTrue(IsValid(entity));
            return m_LightEntities[entity.entityIndex];
        }

        public int GetEntityDataIndex(HDLightEntity entity) => GetEntityData(entity).dataIndex;

        public HDLightEntityData FindEntity(in VisibleLight visibleLight) => FindEntity(visibleLight.light);
        public HDLightEntityData FindEntity(in Light light)
        {
            if (light != null && m_LightsToEntityItem.TryGetValue(light.GetInstanceID(), out var foundEntity))
                return foundEntity;

            return HDLightEntityData.Invalid;
        }

        public HDLightEntity CreateEntity(int instanceID, Transform transformObject)
        {
            HDLightEntityData newData = AllocateEntityData(instanceID, transformObject);

            HDLightEntity newLightEntity = HDLightEntity.Invalid;
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

        public void DestroyEntity(HDLightEntity lightEntity)
        {
            Assert.IsTrue(IsValid(lightEntity));

            m_FreeIndices.Enqueue(lightEntity.entityIndex);
            HDLightEntityData entityData = m_LightEntities[lightEntity.entityIndex];
            m_LightsToEntityItem.Remove(entityData.lightInstanceID);

            RemoveAtSwapBackArrays(entityData.dataIndex);

            if (m_LightCount == 0)
            {
                DeleteArrays();
            }
            else
            {
                HDLightEntity entityToUpdate = m_OwnerEntity[entityData.dataIndex];
                HDLightEntityData dataToUpdate = m_LightEntities[entityToUpdate.entityIndex];
                dataToUpdate.dataIndex = entityData.dataIndex;
                m_LightEntities[entityToUpdate.entityIndex] = dataToUpdate;
                if (dataToUpdate.lightInstanceID != entityData.lightInstanceID)
                    m_LightsToEntityItem[dataToUpdate.lightInstanceID] = dataToUpdate;
            }
        }

        private HDLightEntityData AllocateEntityData(int instanceID, Transform transformObject)
        {
            if (m_Capacity == 0 || m_LightCount == m_Capacity)
            {
                m_Capacity = Math.Max(Math.Max(m_Capacity * 2, m_LightCount), ArrayCapacity);
                ResizeArrays();
            }

            int newIndex = m_LightCount++;
            HDLightEntityData newDataIndex = new HDLightEntityData { dataIndex = newIndex, lightInstanceID = instanceID };
            m_LightTransforms.Add(transformObject);
            return newDataIndex;
        }

        ~HDLightEntityCollection()
        {
            DeleteArrays();
        }
    }
}
