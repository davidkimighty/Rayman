using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    public class RaymarchShapeGroup : RaymarchEntityGroup
    {
        private static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");
        
        [Header("Shape Group")]
        [SerializeField] private List<RaymarchShape> shapes = new();
#if UNITY_EDITOR
        [Header("Debugging")]
        [SerializeField] protected bool executeInEditor;
        [SerializeField] protected DebugModes debugMode = DebugModes.None;
        [SerializeField] protected bool drawGizmos;
        [SerializeField] protected int boundsDisplayThreshold = 300;
#endif
        private RaymarchShape[] activeShapes;
        private ShapeData[] shapeData;
        
        public bool IsInitialized => shapeData != null;
        
        public override void Setup()
        {
            activeShapes = shapes.Where(s => s != null && s.gameObject.activeInHierarchy).ToArray();
            int shapeCount = activeShapes.Length;
            if (shapeCount == 0) return;

            MaterialInstance = CreateMaterial(materialRef);
            
            shapeData = new ShapeData[shapeCount];
            EntityBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, shapeCount, ShapeData.Stride);
            MaterialInstance.SetBuffer(ShapeBufferId, EntityBuffer);
            
            spatialStructure.Setup(ref MaterialInstance, activeShapes);
        }

        public override void SetData()
        {
            for (int i = 0; i < activeShapes.Length; i++)
            {
                if (activeShapes[i] == null) continue;

                shapeData[i] = new ShapeData(activeShapes[i]);
            }
            EntityBuffer.SetData(shapeData);
            spatialStructure.SetData();
        }

        public override void Release()
        {
            if (MaterialInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(MaterialInstance);
                else
                    DestroyImmediate(MaterialInstance);
                MaterialInstance = null;
            }
            
            EntityBuffer?.Release();
            activeShapes = null;
            shapeData = null;
            spatialStructure.Release();
        }
        
        protected Material CreateMaterial(Material matRef)
        {
#if UNITY_EDITOR
            if (debugMode != DebugModes.None)
                return CoreUtils.CreateEngineMaterial("Rayman/RaymarchDebugLit");
#endif
            return matRef != null ? new Material(matRef) : CoreUtils.CreateEngineMaterial("Rayman/RaymarchLit");
        }
        
#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            
            if (!executeInEditor)
            {
                Gizmos.color = new Color(1, 1, 1, 0.3f);
                Gizmos.DrawSphere(transform.position, 0.1f);
            }
        }
        
        [ContextMenu("Find all entities")]
        protected void FindAllEntities()
        {
            shapes = RaymarchUtils.GetChildrenByHierarchical<RaymarchShape>(transform);
        }
#endif
    }
}
