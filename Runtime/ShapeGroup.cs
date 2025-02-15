using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ShapeGroup : RaymarchGroup
    {
        protected static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        protected static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        protected static readonly int CullId = Shader.PropertyToID("_Cull");
        protected static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        
        [SerializeField] protected List<RaymarchShape> shapes = new();
        [SerializeField] protected float updateBoundsThreshold;
        [SerializeField] protected RenderStateData renderStateData;
        
        protected RaymarchShape[] activeShapes;
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
            activeShapes = shapes.Where(s => s && s.gameObject.activeInHierarchy).ToArray();
            if (activeShapes.Length == 0) return null;

            MatInstance = new Material(shader);
            if (!MatInstance) return null;
            
            SetupShaderProperties(ref MatInstance);

            nodeBufferProvider = new BvhAabbNodeBufferProvider(updateBoundsThreshold);
            nodeBufferProvider.SetupBuffer(activeShapes, ref MatInstance);
            
            shapeBufferProvider = new ShapeBufferProvider();
            shapeBufferProvider.SetupBuffer(activeShapes, ref MatInstance);
            
            InvokeOnSetup();
            return MatInstance;
        }

        public override void ReleaseGroup()
        {
            if (Application.isEditor)
                DestroyImmediate(MatInstance);
            else
                Destroy(MatInstance);
            activeShapes = null;
            
            nodeBufferProvider?.ReleaseBuffer();
            nodeBufferProvider = null;
            shapeBufferProvider?.ReleaseBuffer();
            shapeBufferProvider = null;
            
            InvokeOnRelease();
        }
        
        public override bool IsInitialized() => MatInstance && nodeBufferProvider != null && shapeBufferProvider != null;
        
        public override void SetupShaderProperties(ref Material material)
        {
            if (renderStateData)
            {
                material.SetFloat(SrcBlendId, (float)renderStateData.SrcBlend);
                material.SetFloat(DstBlendId, (float)renderStateData.DstBlend);
                material.SetInt(CullId, (int)renderStateData.Cull);
                material.SetFloat(ZWriteId, renderStateData.ZWrite ? 1f : 0f);
            }
        }
        
#if UNITY_EDITOR
        [ContextMenu("Find All Shapes")]
        public void FindAllShapes()
        {
            shapes = RaymarchUtils.GetChildrenByHierarchical<RaymarchShape>(transform);
        }
#endif
    }
}
