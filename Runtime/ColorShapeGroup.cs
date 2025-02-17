using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ColorShapeGroup : RaymarchGroup
    {
        protected static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        protected static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        protected static readonly int CullId = Shader.PropertyToID("_Cull");
        protected static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        
        protected static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        protected static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        protected static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        protected static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        
        [SerializeField] protected List<ColorShape> entities = new();
        [SerializeField] protected float updateBoundsThreshold;
        
        [Header("PBR")]
        [SerializeField] protected RenderStateData renderStateData;
        [SerializeField] protected Texture baseMap;
        [Range(0f, 1f), SerializeField] protected float metallic;
        [Range(0f, 1f), SerializeField] protected float smoothness = 0.5f;
        [ColorUsage(true, true), SerializeField] protected Color emissionColor;

        protected ColorShape[] activeEntities;
        protected IBufferProvider nodeBufferProvider;
        protected IBufferProvider shapeBufferProvider;

        private void LateUpdate()
        {
            if (!IsInitialized()) return;

            nodeBufferProvider.UpdateBufferData();
            shapeBufferProvider.UpdateBufferData();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsInitialized()) return;

            SetupShaderProperties(ref MatInstance);                
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || !IsInitialized()) return;
            
            nodeBufferProvider.DrawGizmos();
        }
#endif

        public override Material InitializeGroup()
        {
            activeEntities = entities.Where(s => s && s.gameObject.activeInHierarchy).ToArray();
            if (activeEntities.Length == 0) return null;

            MatInstance = new Material(shader);
            if (!MatInstance) return null;
            
            SetupShaderProperties(ref MatInstance);

            nodeBufferProvider = new BvhAabbNodeBufferProvider(updateBoundsThreshold);
            nodeBufferProvider.SetupBuffer(activeEntities, ref MatInstance);
            
            shapeBufferProvider = new ColorShapeBufferProvider();
            shapeBufferProvider.SetupBuffer(activeEntities, ref MatInstance);
            
            InvokeOnSetup();
            return MatInstance;
        }

        public override void ReleaseGroup()
        {
            if (Application.isEditor)
                DestroyImmediate(MatInstance);
            else
                Destroy(MatInstance);
            activeEntities = null;
            
            nodeBufferProvider?.ReleaseBuffer();
            nodeBufferProvider = null;
            shapeBufferProvider?.ReleaseBuffer();
            shapeBufferProvider = null;
            
            InvokeOnRelease();
        }
        
        public override bool IsInitialized() => MatInstance &&
            nodeBufferProvider != null && shapeBufferProvider != null;
        
        public override void SetupShaderProperties(ref Material material)
        {
            if (renderStateData)
            {
                material.SetFloat(SrcBlendId, (float)renderStateData.SrcBlend);
                material.SetFloat(DstBlendId, (float)renderStateData.DstBlend);
                material.SetInt(CullId, (int)renderStateData.Cull);
                material.SetFloat(ZWriteId, renderStateData.ZWrite ? 1f : 0f);
            }
            
            material.SetTexture(BaseMapId, baseMap);
            material.SetFloat(MetallicId, metallic);
            material.SetFloat(SmoothnessId, smoothness);
            material.SetColor(EmissionColorId, emissionColor);
        }

        public override void AddEntity(RaymarchEntity entity)
        {
            if (entities.Contains(entity)) return;

            ColorShape colorShape = entity as ColorShape;
            if (colorShape == null) return;
            
            entities.Add(colorShape);
        }

        public override void RemoveEntity(RaymarchEntity entity)
        {
            if (!entities.Contains(entity)) return;

            ColorShape colorShape = entity as ColorShape;
            if (colorShape == null) return;
            
            entities.Remove(colorShape);
        }

        public override int GetSdfCount() => activeEntities?.Length ?? 0;

        public override int GetNodeCount() => ((BvhAabbNodeBufferProvider)nodeBufferProvider)?.SpatialStructure.Count ?? 0;
        
        public override int GetMaxHeight() => ((BvhAabbNodeBufferProvider)nodeBufferProvider)?.SpatialStructure.MaxHeight ?? 0;

#if UNITY_EDITOR
        [ContextMenu("Find All Shapes")]
        public void FindAllShapes()
        {
            entities = RaymarchUtils.GetChildrenByHierarchical<ColorShape>(transform);
        }
#endif
    }
}
