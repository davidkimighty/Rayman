using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    public enum ColorUsages
    {
        None,
        Color,
        Gradient,
    }
    
    [ExecuteInEditMode]
    public class ShapeGroup : RaymarchGroup
    {
        [SerializeField] private List<RaymarchShapeEntity> shapes = new();
        [SerializeField] private List<RaymarchDataProvider> dataProviders = new();
        [SerializeField] private ColorUsages ColorUsage = ColorUsages.Color;
        [SerializeField] private float updateBoundsThreshold;
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos;
#endif
        
        private RaymarchShapeEntity[] activeShapes;
        private IBufferProvider nodeBufferProvider;
        private IBufferProvider shapeBufferProvider;
        private GraphicsBuffer nodeBuffer;
        private GraphicsBuffer shapeBuffer;
        
        private void LateUpdate()
        {
            if (!IsInitialized()) return;

            nodeBufferProvider.SetData(ref nodeBuffer);
            shapeBufferProvider.SetData(ref shapeBuffer);
        }
                
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsInitialized()) return;

            foreach (RaymarchDataProvider provider in dataProviders)
                provider?.SetupShaderProperties(ref MatInstance);
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
            
            foreach (RaymarchDataProvider provider in dataProviders)
                provider?.SetupShaderProperties(ref MatInstance);

            nodeBufferProvider = new BvhAabbNodeBufferProvider(updateBoundsThreshold);
            nodeBuffer = nodeBufferProvider.InitializeBuffer(activeShapes, ref MatInstance);
            
            shapeBufferProvider = ColorUsage switch
            {
                ColorUsages.Color => new ShapeBufferProvider<ColorShapeData>(),
                _ => new ShapeBufferProvider<ShapeData>()
            };
            shapeBuffer = shapeBufferProvider.InitializeBuffer(activeShapes, ref MatInstance);
            
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
            
            nodeBufferProvider?.ReleaseData();
            nodeBufferProvider = null;
            shapeBufferProvider?.ReleaseData();
            shapeBufferProvider = null;
            
            nodeBuffer?.Release();
            shapeBuffer?.Release();
            InvokeOnRelease();
        }
        
        public override bool IsInitialized() => MatInstance &&
            nodeBufferProvider != null && shapeBufferProvider != null;
        
        public override void AddEntity(RaymarchEntity entity)
        {
            if (shapes.Contains(entity)) return;

            RaymarchShapeEntity shape = entity as RaymarchShapeEntity;
            if (shape == null) return;
            
            shapes.Add(shape);
        }

        public override void RemoveEntity(RaymarchEntity entity)
        {
            if (!shapes.Contains(entity)) return;

            RaymarchShapeEntity shape = entity as RaymarchShapeEntity;
            if (shape == null) return;
            
            shapes.Remove(shape);
        }
        
        public override int GetSdfCount() => activeShapes?.Length ?? 0;

        public override int GetNodeCount() => ((BvhAabbNodeBufferProvider)nodeBufferProvider)?.SpatialStructure.Count ?? 0;
        
        public override int GetMaxHeight() => ((BvhAabbNodeBufferProvider)nodeBufferProvider)?.SpatialStructure.MaxHeight ?? 0;
        
#if UNITY_EDITOR
        [ContextMenu("Find All Shapes")]
        public void FindAllShapes()
        {
            shapes = RaymarchUtils.GetChildrenByHierarchical<RaymarchShapeEntity>(transform);
        }
#endif
    }
}
