using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class LineObject : RaymarchObject
    {
        public const string GradientColorKeyword = "GRADIENT_COLOR";
        
        [SerializeField] private List<LineProvider> lines = new();
        [SerializeField] private bool useGradient;
        [SerializeField] private float syncThreshold;
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos;
#endif
        
        private LineProvider[] activeLines;
        private INodeBufferProvider nodeBufferProvider;
        private IBufferProvider<LineProvider> lineBufferProvider;
        private IBufferProvider<LineProvider> pointBufferProvider;
        private GraphicsBuffer nodeBuffer;
        private GraphicsBuffer lineBuffer;
        private GraphicsBuffer pointBuffer;
        
        private void LateUpdate()
        {
            if (!IsReady()) return;

            nodeBufferProvider.SyncBounds(activeLines, syncThreshold);
            nodeBufferProvider.SetData(ref nodeBuffer);
            lineBufferProvider.SetData(ref lineBuffer);
            pointBufferProvider.SetData(ref pointBuffer);
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
            activeLines = lines.Where(a => a && a.gameObject.activeInHierarchy).ToArray();
            if (activeLines.Length == 0) return null;

            MatInstance = new Material(shader);
            if (!MatInstance) return null;
            
            ProvideShaderProperties();

            nodeBufferProvider = new BvhNodeBufferProvider<Aabb, AabbNodeData>();
            nodeBuffer = nodeBufferProvider.InitializeBuffer(activeLines, ref MatInstance);
            
            if (useGradient)
                MatInstance.EnableKeyword(GradientColorKeyword);
            lineBufferProvider = useGradient
                ? new LineBufferProvider<LineData>()
                : new LineBufferProvider<GradientLineData>();
            lineBuffer = lineBufferProvider.InitializeBuffer(activeLines, ref MatInstance);
            
            pointBufferProvider = new PointBufferProvider<PointData>();
            pointBuffer = pointBufferProvider.InitializeBuffer(activeLines, ref MatInstance);
            
            InvokeOnSetup();
            return MatInstance;
        }

        public override void Cleanup()
        {
            if (Application.isEditor)
                DestroyImmediate(MatInstance);
            else
                Destroy(MatInstance);
            activeLines = null;
            
            nodeBufferProvider?.ReleaseData();
            nodeBufferProvider = null;
            lineBufferProvider?.ReleaseData();
            lineBufferProvider = null;
            pointBufferProvider?.ReleaseData();
            pointBufferProvider = null;
            
            nodeBuffer?.Release();
            lineBuffer?.Release();
            pointBuffer?.Release();
            InvokeOnCleanup();
        }
        
        public override bool IsReady() => MatInstance &&
            nodeBufferProvider != null && lineBufferProvider != null;
        
#if UNITY_EDITOR
        [ContextMenu("Find All Lines")]
        public void FindAllLines()
        {
            lines = RaymarchUtils.GetChildrenByHierarchical<LineProvider>(transform);
        }
#endif
    }
}
