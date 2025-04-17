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
    public class ShapeObject : RaymarchObject, IRaymarchDebug, ISpatialStructureDebug
    {
        public const string GradientColorKeyword = "GRADIENT_COLOR";
        
        [SerializeField] private List<ShapeProvider> shapes = new();
        
        [SerializeField] private ColorUsages colorUsage = ColorUsages.Color;
        [SerializeField] private float syncThreshold;
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos;
#endif
        
        private ShapeProvider[] activeShapes;
        private IRaymarchNodeBufferProvider<Aabb> nodeBufferProvider;
        private IRaymarchBufferProvider shapeBufferProvider;
        private GraphicsBuffer nodeBuffer;
        private GraphicsBuffer shapeBuffer;

        private void LateUpdate()
        {
            if (!IsReady()) return;

            for (int i = 0; i < activeShapes.Length; i++)
                nodeBufferProvider.SyncBounds(i, activeShapes[i].GetBounds<Aabb>(), syncThreshold);
            
            nodeBufferProvider.SetData(ref nodeBuffer);
            shapeBufferProvider.SetData(ref shapeBuffer);
        }
                
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsReady()) return;

            ProvideShaderProperties();
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || !IsReady()) return;
            
            nodeBufferProvider.DrawGizmos();
        }
#endif
        
        public override Material SetupMaterial()
        {
            activeShapes = shapes.Where(s => s && s.gameObject.activeInHierarchy).ToArray();
            if (activeShapes.Length == 0) return null;

            MatInstance = new Material(shader);
            if (!MatInstance) return null;
            
            ProvideShaderProperties();

            nodeBufferProvider = new BvhAabbNodeBufferProvider();
            nodeBuffer = nodeBufferProvider.InitializeBuffer(activeShapes.Select(s => s.GetBounds<Aabb>()).ToArray(),
                ref MatInstance);
            
            if (colorUsage == ColorUsages.Gradient)
                MatInstance.EnableKeyword(GradientColorKeyword);
            shapeBufferProvider = colorUsage switch
            {
                ColorUsages.Color => new ShapeBufferProvider<ColorShapeData>(),
                ColorUsages.Gradient => new ShapeBufferProvider<GradientColorShapeData>(),
                _ => new ShapeBufferProvider<ShapeData>()
            };
            shapeBuffer = shapeBufferProvider.InitializeBuffer(activeShapes, ref MatInstance);
            
            InvokeOnSetup();
            return MatInstance;
        }

        public override void Cleanup()
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
            InvokeOnCleanup();
        }
        
        public override bool IsReady() => MatInstance &&
            nodeBufferProvider != null && shapeBufferProvider != null;
        
        public int GetSdfCount() => activeShapes?.Length ?? 0;

        public int GetNodeCount() => nodeBufferProvider?.SpatialStructure.Count ?? 0;
        
        public int GetMaxHeight() => nodeBufferProvider?.SpatialStructure.MaxHeight ?? 0;
        
        public void AddShapeProvider(ShapeProvider provider)
        {
            if (!provider || shapes.Contains(provider)) return;
            
            shapes.Add(provider);
        }

        public void RemoveShapeProvider(ShapeProvider provider)
        {
            if (!provider || !shapes.Contains(provider)) return;
            
            shapes.Remove(provider);
        }
        
#if UNITY_EDITOR
        [ContextMenu("Find All Shapes")]
        public void FindAllShapes()
        {
            shapes = RaymarchUtils.GetChildrenByHierarchical<ShapeProvider>(transform);
        }
#endif
    }
}
