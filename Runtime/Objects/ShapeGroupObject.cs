using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ShapeGroupObject : RaymarchObject
    {
        public const string GradientColorKeyword = "GRADIENT_COLOR";
        
        [SerializeField] private List<ShapeGroupProvider> shapeGroups = new();
        [SerializeField] private bool useGradient;
        [SerializeField] private float syncThreshold;
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos;
#endif

        private ShapeGroupProvider[] activeGroups;
        private ShapeProvider[] activeShapes;
        private INodeBufferProvider nodeBufferProvider;
        private IBufferProvider<ShapeGroupProvider> shapeGroupBufferProvider;
        private IBufferProvider<ShapeProvider> shapeBufferProvider;
        private GraphicsBuffer nodeBuffer;
        private GraphicsBuffer groupedShapeBuffer;
        private GraphicsBuffer shapeBuffer;

        private void LateUpdate()
        {
            if (!IsReady()) return;

            nodeBufferProvider.SyncBounds(activeGroups, syncThreshold);
            nodeBufferProvider.SetData(ref nodeBuffer);
            shapeGroupBufferProvider.SetData(ref groupedShapeBuffer);
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
            (activeGroups, activeShapes) = GetActives(shapeGroups);
            if (activeGroups == null || activeGroups.Length == 0) return null;
            
            MatInstance = new Material(shader);
            if (!MatInstance) return null;
            
            ProvideShaderProperties();

            nodeBufferProvider = new BvhNodeBufferProvider<Aabb, AabbNodeData>();
            nodeBuffer = nodeBufferProvider.InitializeBuffer(activeGroups, ref MatInstance);

            shapeGroupBufferProvider = new ShapeGroupBufferProvider<ShapeGroupData>();
            groupedShapeBuffer = shapeGroupBufferProvider.InitializeBuffer(activeGroups, ref MatInstance);
            
            if (useGradient)
                MatInstance.EnableKeyword(GradientColorKeyword);
            shapeBufferProvider = useGradient
                ? new ShapeBufferProvider<GradientColorShapeData>()
                : new ShapeBufferProvider<ColorShapeData>();
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
            activeGroups = null;
            activeShapes = null;
            
            nodeBufferProvider?.ReleaseData();
            nodeBufferProvider = null;
            shapeGroupBufferProvider?.ReleaseData();
            shapeGroupBufferProvider = null;
            shapeBufferProvider?.ReleaseData();
            shapeBufferProvider = null;
            
            nodeBuffer?.Release();
            groupedShapeBuffer?.Release();
            shapeBuffer?.Release();
            InvokeOnCleanup();
        }
        
        public override bool IsReady() => MatInstance && nodeBufferProvider != null 
            && shapeGroupBufferProvider != null && shapeBufferProvider != null;

        private (ShapeGroupProvider[], ShapeProvider[]) GetActives(List<ShapeGroupProvider> groups)
        {
            List<ShapeGroupProvider> resultGroups = new();
            List<ShapeProvider> resultShapes = new();
            int prevCount = 0;

            foreach (ShapeGroupProvider group in groups)
            {
                if (group == null) continue;
                
                foreach (ShapeProvider shape in group.Shapes)
                {
                    if (shape && shape.gameObject.activeInHierarchy)
                        resultShapes.Add(shape);
                }
                
                if (prevCount != resultShapes.Count)
                    resultGroups.Add(group);
                prevCount = resultShapes.Count;
            }
            return (resultGroups.ToArray(), resultShapes.ToArray());
        }
    }
}
