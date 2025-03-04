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
    public class ShapeObject : RaymarchObject, IRaymarchElementControl, IRaymarchDebug, ISpatialStructureDebug
    {
        public const string GradientColorKeyword = "GRADIENT_COLOR";
        
        [SerializeField] private List<ShapeElement> shapes = new();
        
        [SerializeField] private ColorUsages ColorUsage = ColorUsages.Color;
        [SerializeField] private float syncThreshold;
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos;
#endif
        
        private ShapeElement[] activeShapes;
        private INodeBufferProvider<Aabb> nodeBufferProvider;
        private IRaymarchElementBufferProvider shapeBufferProvider;
        private GraphicsBuffer nodeBuffer;
        private GraphicsBuffer shapeBuffer;

        private void LateUpdate()
        {
            if (!IsInitialized()) return;

            for (int i = 0; i < activeShapes.Length; i++)
                nodeBufferProvider.SyncBounds(i, activeShapes[i].GetBounds<Aabb>(), syncThreshold);
            
            nodeBufferProvider.SetData(ref nodeBuffer);
            shapeBufferProvider.SetData(ref shapeBuffer);
        }
                
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsInitialized()) return;

            ProvideShaderProperties();
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || !IsInitialized()) return;
            
            nodeBufferProvider.DrawGizmos();
        }
#endif
        
        public override Material Initialize()
        {
            activeShapes = shapes.Where(s => s && s.gameObject.activeInHierarchy).ToArray();
            if (activeShapes.Length == 0) return null;

            MatInstance = new Material(shader);
            if (!MatInstance) return null;
            
            ProvideShaderProperties();

            nodeBufferProvider = new BvhAabbNodeBufferProvider();
            nodeBuffer = nodeBufferProvider.InitializeBuffer(
                activeShapes.Select(s => s.GetBounds<Aabb>()).ToArray(), ref MatInstance);
            
            if (ColorUsage == ColorUsages.Gradient)
                MatInstance.EnableKeyword(GradientColorKeyword);
            shapeBufferProvider = ColorUsage switch
            {
                ColorUsages.Color => new ShapeBufferProvider<ColorShapeData>(),
                ColorUsages.Gradient => new ShapeBufferProvider<GradientColorShapeData>(),
                _ => new ShapeBufferProvider<ShapeData>()
            };
            shapeBuffer = shapeBufferProvider.InitializeBuffer(activeShapes, ref MatInstance);
            
            InvokeOnSetup();
            return MatInstance;
        }

        public override void Release()
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
        
        public void AddEntity(RaymarchElement entity)
        {
            if (shapes.Contains(entity)) return;

            ShapeElement shape = entity as ShapeElement;
            if (!shape) return;
            
            shapes.Add(shape);
        }

        public void RemoveEntity(RaymarchElement entity)
        {
            if (!shapes.Contains(entity)) return;

            ShapeElement shape = entity as ShapeElement;
            if (shape == null) return;
            
            shapes.Remove(shape);
        }
        
        public int GetSdfCount() => activeShapes?.Length ?? 0;

        public int GetNodeCount() => nodeBufferProvider?.SpatialStructure.Count ?? 0;
        
        public int GetMaxHeight() => nodeBufferProvider?.SpatialStructure.MaxHeight ?? 0;
        
#if UNITY_EDITOR
        [ContextMenu("Find All Shapes")]
        public void FindAllShapes()
        {
            shapes = RaymarchUtils.GetChildrenByHierarchical<ShapeElement>(transform);
        }
#endif
    }
}
